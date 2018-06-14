using System.IO;

namespace oMediaCenter.Interfaces
{
	public interface IMediaFileStreamer
	{
		StreamingFile GetStream(IMediaFile selectedMediaFile);
	}
}