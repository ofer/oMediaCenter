using oMediaCenter.Interfaces;

namespace oMediaCenter.SubtitleProvider
{
	public class SubDlProvider : ISubtitleProvider
	{
		public Task<bool> GetSubtitleInformation(IMediaFile mf, string targetFilename)
		{ 

		}

		public Task<ISubtitleRecord[]> GetSubtitleList(IMediaFile selectedMediaFile)
		{

		}
	}
}