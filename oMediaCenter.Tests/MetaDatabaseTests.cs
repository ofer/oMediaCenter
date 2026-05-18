using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using oMediaCenter.Interfaces;
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
	public class MetaDatabaseTests
	{
		private readonly ILogger<MediaInformationProvider> _logger = new Mock<ILogger<MediaInformationProvider>>().Object;

		[InlineData("Despicable.Me.2.2013.720p.BluRay.x264.YIFY.mp4", "Despicable Me 2", "2013")]
		[InlineData("Zootopia 2016 1080p HDRip x264 AC3-JYK.mkv", "Zootopia", "2016")]
		[InlineData("Cars[2006]DvDrip[Eng]-aXXo.avi","Cars","2006")]
		[InlineData("Home Again (2017) English HD - Rip x264 1CD - AAC - Yify - films.com.mp4", "Home Again", "2017")]
		[InlineData("Three.Thousand.Years.of.Longing.2022.HDRip.XviD.AC3-EVO.avi", "Three Thousand Years of Longing", "2022")]
		[InlineData("[ www UsaBit com ] - Happy Gilmore(1996).avi", "Happy Gilmore", "1996")]
		[Theory]
		public void ShouldFindCorrectMovieNameWithParanSurroundedYearInFilename(string inputFilename, string moviename, string year)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null, _logger);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(year, metaData.Year);
		}

		[InlineData("Suits.S05E03.HDTV.x264-ASAP.mp4", "Suits", "05", "03")]
		[InlineData("Suits.S07E11.720p.HDTV.x264-AVS.mkv", "Suits", "07","11")]
		[InlineData("Dora the Explorer - 1x01 - The Legend of the Big Red Chicken [Mischief].avi", "Dora the Explorer", "1", "01")]
		[Theory]
		public void ShouldFindCorrectShowNameWithEpisodeAndSeason(string inputFilename, string moviename, string season, string episode)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null, _logger);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(episode, metaData.Episode);
			Assert.Equal(season, metaData.Season);
		}

		[InlineData("wtf-argo dvdscr xvid avi", "argo")]
		[InlineData("fgt-inception.dvdrip.xvid.avi", "inception")]
		[InlineData("sprinter-12angrymen-cd1.avi", "12angrymen")]
		[InlineData("sprinter-12angrymen-cd2.avi", "12angrymen")]
		[Theory]
		public void ShouldStripReleaseGroupPrefixAndJunkTokens(string inputFilename, string moviename)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null, _logger);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
		}

		[Fact]
		public void FullPathInputsUseFilenameForTitleAndPathForYear()
		{
			MediaInformationProvider mip = new MediaInformationProvider(null, _logger);
			FileMetadata metaData = mip.GetFileMetadataFromFilename("[ UsaBit.com ] - 12.Angry.Men.1997.DVDRip.XviD-SPRiNTER/CD1/sprinter-12angrymen-cd1.avi");

			Assert.Equal("12angrymen", metaData.Title);
			Assert.Equal("1997", metaData.Year);
		}

		[Theory]
		[InlineData("The.Middle/Season 03/The.Middle.hdtv.x264.mkv", "The Middle", "03", null)]
		[InlineData("The.Middle/S03/The.Middle.hdtv.x264.mkv", "The Middle", "03", null)]
		[InlineData("The.Middle/Season 03/Episode 02/The.Middle.hdtv.x264.mkv", "The Middle", "03", "02")]
		[InlineData("The.Middle/S03E02/The.Middle.hdtv.x264.mkv", "The Middle", "03", "02")]
		public void FullPathInputsUseFilenameForTitleAndPathForTvMetadata(string inputFilename, string moviename, string season, string episode)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null, _logger);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);

			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(season, metaData.Season);
			Assert.Equal(episode, metaData.Episode);
		}

		[InlineData("The.Middle.S03E01+E02.Forced.Family.Fun.Pt.1.+.Pt.2.mp4", "The Middle", "03", "01")]
		[InlineData("The.Middle.S04E01+E02.Last.Whiff.of.Summer.Pt.1.+.Pt.2.mp4", "The Middle", "04", "01")]
		[InlineData("The.Middle.S09E23+E24.A.Heck.of.a.Ride.Pt.1.+.Pt.2.mp4", "The Middle", "09", "23")]
		[Theory]
		public void ShouldHandleMultiEpisodeFormats(string inputFilename, string moviename, string season, string episode)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null, _logger);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(season, metaData.Season);
			Assert.Equal(episode, metaData.Episode);
		}

		// -- Fallback integration tests --

		[Fact]
		public void WhenDbLookupFailsAndResolverReturnsTitleTheResolvedTitleIsUsed()
		{
			var resolver = new Mock<ILlmTitleResolver>();
			resolver.Setup(r => r.ResolveTitleFromFilename(It.IsAny<string>()))
				.Returns("Sherlock Holmes");

			// No DB (null factory) so DB lookup always fails
			var mip = new MediaInformationProvider(null, _logger, resolver.Object);
			var result = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");

			Assert.Equal("Sherlock Holmes", result.Title);
			Assert.Equal("2009", result.Year);
		}

		[Fact]
		public void WhenDbLookupFailsAndResolverReturnsNullParsedTitleIsUsed()
		{
			var resolver = new Mock<ILlmTitleResolver>();
			resolver.Setup(r => r.ResolveTitleFromFilename(It.IsAny<string>()))
				.Returns((string)null);

			var mip = new MediaInformationProvider(null, _logger, resolver.Object);
			var result = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");

			Assert.Equal("Sherlock Holms", result.Title);
			Assert.Equal("2009", result.Year);
		}

		[Fact]
		public void WhenResolverIsNullBehaviorIsIdenticalToCurrent()
		{
			var mip = new MediaInformationProvider(null, _logger);
			var result = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");

			Assert.Equal("Sherlock Holms", result.Title);
			Assert.Equal("2009", result.Year);
		}

		[Fact]
		public void WhenResolverIsNotInjectedBehaviorIsIdenticalToCurrent()
		{
			var mip = new MediaInformationProvider(null, _logger);
			var result = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");

			Assert.Equal("Sherlock Holms", result.Title);
			Assert.Equal("2009", result.Year);
		}

		[InlineData("Sherlock.Holms.2009.mp4", "Sherlock Holmes", "Sherlock Holmes", "2009")]
		[InlineData("Z00topia.2.2025.mkv", "Zootopia 2", "Zootopia 2", "2025")]
		[InlineData("A.Few.Good.Me.1992.BrRip.mp4", "A Few Good Men", "A Few Good Men", "1992")]
		[Theory]
		public void ResolverCorrectedTitlesAreUsedForEdgeCases(string filename, string resolvedTitle, string expectedTitle, string expectedYear)
		{
			var resolver = new Mock<ILlmTitleResolver>();
			resolver.Setup(r => r.ResolveTitleFromFilename(filename))
				.Returns(resolvedTitle);

			var mip = new MediaInformationProvider(null, _logger, resolver.Object);
			var result = mip.GetEpisodeInfoForFilename(filename);

			Assert.Equal(expectedTitle, result.Title);
			Assert.Equal(expectedYear, result.Year);
		}

		[Fact]
		public void AiFallbackReceivesOriginalFullPathInput()
		{
			const string fullPath = "[ UsaBit.com ] - 12.Angry.Men.1997.DVDRip.XviD-SPRiNTER/CD1/sprinter-12angrymen-cd1.avi";
			var resolver = new Mock<ILlmTitleResolver>();
			resolver.Setup(r => r.ResolveTitleFromFilename(fullPath))
				.Returns("12 Angry Men");

			var mip = new MediaInformationProvider(null, _logger, resolver.Object);
			var result = mip.GetEpisodeInfoForFilename(fullPath);

			Assert.Equal("12 Angry Men", result.Title);
			Assert.Equal("1997", result.Year);
			resolver.Verify(r => r.ResolveTitleFromFilename(fullPath), Times.Once);
		}

		[Fact]
		public void FirstCallForUnresolvedFilenameReturnsParsedTitle()
		{
			var resolver = new Mock<ILlmTitleResolver>();
			resolver.Setup(r => r.ResolveTitleFromFilename(It.IsAny<string>()))
				.Returns((string)null);

			var mip = new MediaInformationProvider(null, _logger, resolver.Object);
			var result = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");

			Assert.Equal("Sherlock Holms", result.Title);
			Assert.Equal("2009", result.Year);
		}

		[Fact]
		public async Task AfterBackgroundProcessingSecondCallReturnsAiResolvedTitleFromCache()
		{
			var tempDir = Path.Combine(Path.GetTempPath(), $"metadata-resolver-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(tempDir);
			try
			{
				var cache = new TitleResolverCache(
					Path.Combine(tempDir, "cache.json"),
					new Mock<ILogger<TitleResolverCache>>().Object);
				var handler = new Mock<HttpMessageHandler>();
				handler.Protected()
					.Setup<Task<HttpResponseMessage>>("SendAsync",
						ItExpr.IsAny<HttpRequestMessage>(),
						ItExpr.IsAny<CancellationToken>())
					.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(CreateOpenAiResponse("Sherlock Holmes"))
					});
				var httpClientFactory = new Mock<IHttpClientFactory>();
				httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
					.Returns(() => new HttpClient(handler.Object));
				var backgroundResolver = new BackgroundTitleResolver(
					httpClientFactory.Object,
					Options.Create(new OpenAiOptions { BaseUrl = "https://openai.test", ApiKey = "test-key" }),
					cache,
					new TestTitleResolutionNotifier(),
					new Mock<ILogger<BackgroundTitleResolver>>().Object);
				var resolver = new OpenAiTitleResolver(cache, backgroundResolver);
				var mip = new MediaInformationProvider(null, _logger, resolver);

				var firstResult = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");
				Assert.Equal("Sherlock Holms", firstResult.Title);

				await backgroundResolver.StartAsync(CancellationToken.None);
				try
				{
					await WaitFor(() => cache.Get("Sherlock.Holms.2009.mp4") == "Sherlock Holmes");
				}
				finally
				{
					await backgroundResolver.StopAsync(CancellationToken.None);
				}

				var secondResult = mip.GetEpisodeInfoForFilename("Sherlock.Holms.2009.mp4");
				Assert.Equal("Sherlock Holmes", secondResult.Title);
				Assert.Equal("2009", secondResult.Year);
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
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

		private class TestTitleResolutionNotifier : ITitleResolutionNotifier
		{
			public Task MediaListUpdated(CancellationToken cancellationToken) => Task.CompletedTask;
		}
	}
}
