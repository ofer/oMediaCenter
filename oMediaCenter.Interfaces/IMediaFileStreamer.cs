using System.IO;

namespace oMediaCenter.Interfaces
{
	public interface IMediaFileStreamer
	{
		string MediaType { get; }

		Stream GetStream(IMediaFile selectedMediaFile);
	}
}