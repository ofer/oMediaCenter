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
		private readonly Mock<ILogger<OpenAiTitleResolver>> _resolverLoggerMock;

		public OpenAiTitleResolverTests()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), $"resolver-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempDir);
			_cachePath = Path.Combine(_tempDir, "cache.json");
			_cacheLoggerMock = new Mock<ILogger<TitleResolverCache>>();
			_resolverLoggerMock = new Mock<ILogger<OpenAiTitleResolver>>();
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		}

		private TitleResolverCache CreateCache() =>
			new TitleResolverCache(_cachePath, _cacheLoggerMock.Object);

		private IOptions<OpenAiOptions> CreateOptions(string apiKey = "test-key", int timeout = 5) =>
			Options.Create(new OpenAiOptions { ApiKey = apiKey, Model = "gpt-4.1-nano", TimeoutSeconds = timeout });

		private IHttpClientFactory CreateMockFactory(HttpMessageHandler handler)
		{
			var factory = new Mock<IHttpClientFactory>();
			factory.Setup(f => f.CreateClient(It.IsAny<string>()))
				.Returns(new HttpClient(handler));
			return factory.Object;
		}

		private Mock<HttpMessageHandler> CreateMockHandler(HttpResponseMessage response)
		{
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
				.Setup<HttpResponseMessage>("Send",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.Returns(response);
			return handler;
		}

		private string CreateOpenAiResponse(string title)
		{
			return JsonSerializer.Serialize(new
			{
				choices = new[]
				{
					new { message = new { content = title } }
				}
			});
		}

		[Fact]
		public void ReturnsParsedTitleFromWellFormedResponse()
		{
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
			});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), CreateCache(), _resolverLoggerMock.Object);

			var result = resolver.ResolveTitleFromFilename("Sherlock.Holms.2009.mp4");
			Assert.Equal("Sherlock Holmes", result);
		}

		[Fact]
		public void WorksWithoutApiKey()
		{
			HttpRequestMessage capturedRequest = null;
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
				.Setup<HttpResponseMessage>("Send",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.Returns((HttpRequestMessage req, CancellationToken _) =>
				{
					capturedRequest = req;
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(CreateOpenAiResponse("Something"))
					};
				});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(apiKey: ""), CreateCache(), _resolverLoggerMock.Object);

			var result = resolver.ResolveTitleFromFilename("test.mp4");
			Assert.Equal("Something", result);
			Assert.False(capturedRequest.Headers.Contains("Authorization"));
		}

		[Fact]
		public void ReturnsNullWhenApiReturnsHttpError()
		{
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), CreateCache(), _resolverLoggerMock.Object);

			Assert.Null(resolver.ResolveTitleFromFilename("test.mp4"));
		}

		[Fact]
		public void ReturnsNullWhenApiRequestTimesOut()
		{
			var cache = CreateCache();
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
				.Setup<HttpResponseMessage>("Send",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.Throws(new TaskCanceledException("Timeout"));
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(timeout: 1), cache, _resolverLoggerMock.Object);

			Assert.Null(resolver.ResolveTitleFromFilename("test.mp4"));
			Assert.Equal("UNKNOWN", cache.Get("test.mp4"));
		}

		[Fact]
		public void CachesUnknownOnExceptionAndSkipsApiOnRetry()
		{
			var cache = CreateCache();
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
				.Setup<HttpResponseMessage>("Send",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.Throws(new HttpRequestException("Connection refused"));
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), cache, _resolverLoggerMock.Object);

			// First call: exception thrown, result cached as UNKNOWN
			Assert.Null(resolver.ResolveTitleFromFilename("fail.mp4"));
			Assert.Equal("UNKNOWN", cache.Get("fail.mp4"));

			// Second call: should use cache and not call API again
			Assert.Null(resolver.ResolveTitleFromFilename("fail.mp4"));
			handler.Protected().Verify("Send", Times.Once(),
				ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public void ReturnsNullWhenApiReturnsMalformedJson()
		{
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("not json at all")
			});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), CreateCache(), _resolverLoggerMock.Object);

			Assert.Null(resolver.ResolveTitleFromFilename("test.mp4"));
		}

		[Fact]
		public void ReturnsNullWhenApiReturnsUnknown()
		{
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("UNKNOWN"))
			});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), CreateCache(), _resolverLoggerMock.Object);

			Assert.Null(resolver.ResolveTitleFromFilename("junk.mp4"));
		}

		[Fact]
		public void DoesNotCallApiWhenResultIsAlreadyCached()
		{
			var cache = CreateCache();
			cache.Set("cached.mp4", "Cached Movie");

			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("Something Else"))
			});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), cache, _resolverLoggerMock.Object);

			var result = resolver.ResolveTitleFromFilename("cached.mp4");
			Assert.Equal("Cached Movie", result);

			handler.Protected().Verify("Send", Times.Never(),
				ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public void CachesSuccessfulResultsAfterApiCall()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("The Real Title"))
			});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), cache, _resolverLoggerMock.Object);

			resolver.ResolveTitleFromFilename("movie.mp4");
			Assert.Equal("The Real Title", cache.Get("movie.mp4"));
		}

		[Fact]
		public void CachesUnknownResultsAfterApiCall()
		{
			var cache = CreateCache();
			var handler = CreateMockHandler(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(CreateOpenAiResponse("UNKNOWN"))
			});
			var resolver = new OpenAiTitleResolver(
				CreateMockFactory(handler.Object), CreateOptions(), cache, _resolverLoggerMock.Object);

			resolver.ResolveTitleFromFilename("junk.mp4");
			Assert.Equal("UNKNOWN", cache.Get("junk.mp4"));
		}
	}
}
