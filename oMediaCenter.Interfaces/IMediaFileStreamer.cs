using System.IO;
using System.Threading.Tasks;

namespace oMediaCenter.Interfaces
{
	public interface IMediaFileStreamer
	{
		StreamingFile GetStream(IMediaFile selectedMediaFile);
    Task<string> GetSubtitleFilePath(IMediaFile selectedMediaFile);
  }
}