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
		[InlineData("[ www UsaBit com ] - Happy Gilmore(1996).avi", "Happy Gilmore", "1996")]
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

		[InlineData("wtf-argo dvdscr xvid avi", "argo")]
		[InlineData("fgt-inception.dvdrip.xvid.avi", "inception")]
		[InlineData("sprinter-12angrymen-cd1.avi", "12angrymen")]
		[InlineData("sprinter-12angrymen-cd2.avi", "12angrymen")]
		[Theory]
		public void ShouldStripReleaseGroupPrefixAndJunkTokens(string inputFilename, string moviename)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
		}

		[InlineData("The.Middle.S03E01+E02.Forced.Family.Fun.Pt.1.+.Pt.2.mp4", "The Middle", "03", "01")]
		[InlineData("The.Middle.S04E01+E02.Last.Whiff.of.Summer.Pt.1.+.Pt.2.mp4", "The Middle", "04", "01")]
		[InlineData("The.Middle.S09E23+E24.A.Heck.of.a.Ride.Pt.1.+.Pt.2.mp4", "The Middle", "09", "23")]
		[Theory]
		public void ShouldHandleMultiEpisodeFormats(string inputFilename, string moviename, string season, string episode)
		{
			MediaInformationProvider mip = new MediaInformationProvider(null);
			FileMetadata metaData = mip.GetFileMetadataFromFilename(inputFilename);
			Assert.Equal(moviename, metaData.Title);
			Assert.Equal(season, metaData.Season);
			Assert.Equal(episode, metaData.Episode);
		}

	}
}
