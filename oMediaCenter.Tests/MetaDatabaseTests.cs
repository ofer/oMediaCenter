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
		[InlineData("Three.Thousand.Years.of.Longing.2022.HDRip.XviD.AC3-EVO.avi", "Three Thousand Years of Longing", "2022")]
		[Theory]
		public void ShouldFindCorrectMovieNameWithParanSurroundedYearInFilename(string inputFilename, string moviename, string year)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
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
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(episode, metaData.Episode);
			Assert.Equal(season, metaData.Season);
		}

	}
}
