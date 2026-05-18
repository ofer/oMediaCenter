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
	public class OpenAiTitleResolverTests : IDisposable
	{
		private readonly string _tempDir;
		private readonly string _cachePath;
		private readonly Mock<ILogger<TitleResolverCache>> _cacheLoggerMock;
		private readonly Mock<ILogger<BackgroundTitleResolver>> _backgroundLoggerMock;

		public OpenAiTitleResolverTests()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), $"resolver-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempDir);
			_cachePath = Path.Combine(_tempDir, "cache.json");
			_cacheLoggerMock = new Mock<ILogger<TitleResolverCache>>();
			_backgroundLoggerMock = new Mock<ILogger<BackgroundTitleResolver>>();
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		}

		private TitleResolverCache CreateCache() =>
			new TitleResolverCache(_cachePath, _cacheLoggerMock.Object);

		private static IOptions<OpenAiOptions> CreateOptions() =>
			Options.Create(new OpenAiOptions
			{
				ApiKey = "test-key",
				BaseUrl = "https://openai.test",
				Model = "gpt-4.1-nano",
				TimeoutSeconds = 5
			});

		private static IHttpClientFactory CreateMockFactory(HttpMessageHandler handler)
		{
			var factory = new Mock<IHttpClientFactory>();
			factory.Setup(f => f.CreateClient(It.IsAny<string>()))
				.Returns(() => new HttpClient(handler));
			return factory.Object;
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

		private BackgroundTitleResolver CreateBackgroundResolver(TitleResolverCache cache, HttpMessageHandler handler)
		{
			return new BackgroundTitleResolver(
				CreateMockFactory(handler),
				CreateOptions(),
				cache,
				new TestTitleResolutionNotifier(),
				_backgroundLoggerMock.Object);
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
		public void ReturnsCachedTitleWhenPresent()
		{
			var cache = CreateCache();
			cache.Set("cached.mp4", "Cached Movie");
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Something Else"))
			});
			var backgroundResolver = CreateBackgroundResolver(cache, handler.Object);
			var resolver = new OpenAiTitleResolver(cache, backgroundResolver);

			var result = resolver.ResolveTitleFromFilename("cached.mp4");

			Assert.Equal("Cached Movie", result);
			handler.Protected().Verify("SendAsync", Times.Never(),
				ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public void ReturnsNullForCachedUnknown()
		{
			var cache = CreateCache();
			cache.Set("junk.mp4", "UNKNOWN");
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Something Else"))
			});
			var backgroundResolver = CreateBackgroundResolver(cache, handler.Object);
			var resolver = new OpenAiTitleResolver(cache, backgroundResolver);

			var result = resolver.ResolveTitleFromFilename("junk.mp4");

			Assert.Null(result);
			handler.Protected().Verify("SendAsync", Times.Never(),
				ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public async Task ReturnsNullAndEnqueuesWhenNotInCache()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var backgroundResolver = CreateBackgroundResolver(cache, handler.Object);
			var resolver = new OpenAiTitleResolver(cache, backgroundResolver);

			var result = resolver.ResolveTitleFromFilename("Sherlock.Holms.2009.mp4");

			Assert.Null(result);
			Assert.Null(cache.Get("Sherlock.Holms.2009.mp4"));

			await backgroundResolver.StartAsync(CancellationToken.None);
			try
			{
				await WaitFor(() => cache.Get("Sherlock.Holms.2009.mp4") == "Sherlock Holmes");
			}
			finally
			{
				await backgroundResolver.StopAsync(CancellationToken.None);
			}
		}

		[Fact]
		public void DoesNotMakeHttpCallsDirectly()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var backgroundResolver = CreateBackgroundResolver(cache, handler.Object);
			var resolver = new OpenAiTitleResolver(cache, backgroundResolver);

			var result = resolver.ResolveTitleFromFilename("Sherlock.Holms.2009.mp4");

			Assert.Null(result);
			handler.Protected().Verify("SendAsync", Times.Never(),
				ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		private class TestTitleResolutionNotifier : ITitleResolutionNotifier
		{
			public Task MediaListUpdated(CancellationToken cancellationToken) => Task.CompletedTask;
		}
	}
}
