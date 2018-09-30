using oMediaCenter.MetaDatabase;
using Xunit;

namespace oMediaCenter.Tests
{
	public class MetaDatabaseTests
	{
		[InlineData("Despicable.Me.2.2013.720p.BluRay.x264.YIFY.mp4", "Despicable Me 2", "2013")]
		[InlineData("Zootopia 2016 1080p HDRip x264 AC3-JYK.mkv", "Zootopia", "2016")]
		[InlineData("Cars[2006]DvDrip[Eng]-aXXo.avi","Cars","2006")]
		[InlineData("Home Again (2017) English HD - Rip x264 1CD - AAC - Yify - films.com.mp4", "Home Again", "2017")]
		[Theory]
		public void ShouldFindCorrectMovieNameWithParanSurroundedYearInFilename(string inputFilename, string moviename, string year)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(year, metaData.Year);
		}
	}
}
