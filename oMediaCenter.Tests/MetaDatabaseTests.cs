using oMediaCenter.MetaDatabase;
using Xunit;

namespace oMediaCenter.Tests
{
	public class MetaDatabaseTests
	{
		[Fact]
		public void ShouldFindCorrectMovieNameWithDotsInFilename()
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(@"Despicable.Me.2.2013.720p.BluRay.x264.YIFY.mp4");
			Assert.Equal("Despicable Me 2", metaData.Title);
			Assert.Equal("2013", metaData.Year);
		}

		[Fact]
		public void ShouldFindCorrectMovieNameWithSpacesInFilename()
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(@"Zootopia 2016 1080p HDRip x264 AC3-JYK.mkv");
			Assert.Equal("Zootopia", metaData.Title);
			Assert.Equal("2016", metaData.Year);
		}

		[Fact]
		public void ShouldFindCorrectMovieNameWithParanSurroundedYearInFilename()
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(@"Home Again(2017) English HD - Rip x264 1CD - AAC - Yify - films.com.mp4");
			Assert.Equal("Home Again", metaData.Title);
			Assert.Equal("2017", metaData.Year);
		}
	}
}
