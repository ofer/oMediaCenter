using System.Threading.Tasks;

namespace oMediaCenter.Interfaces
{
  public interface ISubtitleProvider
  {
    Task<bool> GetSubtitleInformation(IMediaFile mf, string targetFilename);
		Task<ISubtitleRecord[]> GetSubtitleList(IMediaFile selectedMediaFile);
	}
}