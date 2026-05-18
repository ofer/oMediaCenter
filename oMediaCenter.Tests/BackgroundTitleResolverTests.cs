using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using oMediaCenter.MetaDatabase;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace oMediaCenter.Tests
{
	public class BackgroundTitleResolverTests : IDisposable
	{
		private readonly string _tempDir;
		private readonly string _cachePath;
		private readonly Mock<ILogger<TitleResolverCache>> _cacheLoggerMock = new();
		private readonly Mock<ILogger<BackgroundTitleResolver>> _resolverLoggerMock = new();

		public BackgroundTitleResolverTests()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), $"background-resolver-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempDir);
			_cachePath = Path.Combine(_tempDir, "cache.json");
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		}

		private TitleResolverCache CreateCache() =>
			new TitleResolverCache(_cachePath, _cacheLoggerMock.Object);

		private static IOptions<OpenAiOptions> CreateOptions(string apiKey = "test-key") =>
			Options.Create(new OpenAiOptions
			{
				ApiKey = apiKey,
				BaseUrl = "https://openai.test",
				Model = "gpt-4.1-nano",
				TimeoutSeconds = 5
			});

		private static string CreateOpenAiResponse(string title)
		{
			return JsonSerializer.Serialize(new
			{
				choices = new[]
				{
					new { message = new { content = title } }
				}
			});
		}

		private static Mock<HttpMessageHandler> CreateMockHandler(HttpResponseMessage response)
		{
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(response);
			return handler;
		}

		private static IHttpClientFactory CreateMockFactory(HttpMessageHandler handler)
		{
			var factory = new Mock<IHttpClientFactory>();
			factory.Setup(f => f.CreateClient(It.IsAny<string>()))
				.Returns(() => new HttpClient(handler));
			return factory.Object;
		}

		private BackgroundTitleResolver CreateResolver(
			TitleResolverCache cache,
			HttpMessageHandler handler,
			TestTitleResolutionNotifier notifier = null,
			IOptions<OpenAiOptions> options = null)
		{
			return new BackgroundTitleResolver(
				CreateMockFactory(handler),
				options ?? CreateOptions(),
				cache,
				notifier ?? new TestTitleResolutionNotifier(),
				_resolverLoggerMock.Object);
		}

		private static async Task WaitFor(Func<bool> condition)
		{
			var timeoutAt = DateTime.UtcNow.AddSeconds(3);
			while (!condition())
			{
				if (DateTime.UtcNow > timeoutAt)
					throw new TimeoutException("Timed out waiting for condition.");

				await Task.Delay(20);
			}
		}

		[Fact]
		public async Task EnqueuedFilenameIsResolvedAndCachedAfterProcessing()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var resolver = CreateResolver(cache, handler.Object);

			await resolver.StartAsync(CancellationToken.None);
			try
			{
				resolver.Enqueue("Sherlock.Holms.2009.mp4");
				await WaitFor(() => cache.Get("Sherlock.Holms.2009.mp4") == "Sherlock Holmes");
			}
			finally
			{
				await resolver.StopAsync(CancellationToken.None);
			}
		}

		[Fact]
		public async Task DuplicateEnqueueIsIgnored()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var resolver = CreateResolver(cache, handler.Object);

			resolver.Enqueue("Sherlock.Holms.2009.mp4");
			resolver.Enqueue("Sherlock.Holms.2009.mp4");

			await resolver.StartAsync(CancellationToken.None);
			try
			{
				await WaitFor(() => cache.Get("Sherlock.Holms.2009.mp4") == "Sherlock Holmes");
			}
			finally
			{
				await resolver.StopAsync(CancellationToken.None);
			}

			handler.Protected().Verify("SendAsync", Times.Once(),
				ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public async Task MediaListUpdatedIsSentAfterSuccessfulCacheWrite()
		{
			var cache = CreateCache();
			var notifier = new TestTitleResolutionNotifier();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var resolver = CreateResolver(cache, handler.Object, notifier);

			await resolver.StartAsync(CancellationToken.None);
			try
			{
				resolver.Enqueue("Sherlock.Holms.2009.mp4");
				await WaitFor(() => notifier.CallCount == 1);
			}
			finally
			{
				await resolver.StopAsync(CancellationToken.None);
			}
		}

		[Fact]
		public async Task ApiErrorDoesNotCrashServiceAndProcessingContinues()
		{
			var cache = CreateCache();
			var handler = new ErrorThenSuccessHandler(CreateOpenAiResponse("Second Movie"));
			var resolver = CreateResolver(cache, handler);

			await resolver.StartAsync(CancellationToken.None);
			try
			{
				resolver.Enqueue("bad.mp4");
				resolver.Enqueue("second.mp4");
				await WaitFor(() => cache.Get("second.mp4") == "Second Movie");
			}
			finally
			{
				await resolver.StopAsync(CancellationToken.None);
			}

			Assert.Equal(2, handler.CallCount);
			Assert.Null(cache.Get("bad.mp4"));
			Assert.Equal("Second Movie", cache.Get("second.mp4"));
		}

		[Fact]
		public async Task UnknownResponsesAreCached()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("UNKNOWN"))
			});
			var resolver = CreateResolver(cache, handler.Object);

			await resolver.StartAsync(CancellationToken.None);
			try
			{
				resolver.Enqueue("junk.mp4");
				await WaitFor(() => cache.Get("junk.mp4") == "UNKNOWN");
			}
			finally
			{
				await resolver.StopAsync(CancellationToken.None);
			}
		}

		[Fact]
		public async Task ServiceStopsCleanlyOnCancellation()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var resolver = CreateResolver(cache, handler.Object);

			await resolver.StartAsync(CancellationToken.None);
			await resolver.StopAsync(CancellationToken.None);
		}

		private class TestTitleResolutionNotifier : ITitleResolutionNotifier
		{
			public int CallCount { get; private set; }

			public Task MediaListUpdated(CancellationToken cancellationToken)
			{
				CallCount++;
				return Task.CompletedTask;
			}
		}

		private class ErrorThenSuccessHandler : HttpMessageHandler
		{
			private readonly string _successResponse;
			private int _callCount;

			public int CallCount => _callCount;

			public ErrorThenSuccessHandler(string successResponse)
			{
				_successResponse = successResponse;
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var callCount = Interlocked.Increment(ref _callCount);
				return Task.FromResult(callCount == 1
					? new HttpResponseMessage(HttpStatusCode.InternalServerError)
					: new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(_successResponse)
					});
			}
		}
	}
}
