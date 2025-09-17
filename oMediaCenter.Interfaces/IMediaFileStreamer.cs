using System.IO;
using System.Threading.Tasks;

namespace oMediaCenter.Interfaces
{
	public interface IMediaFileStreamer
	{
		StreamingFile GetStream(IMediaFile selectedMediaFile);
		// Task<string> GetSubtitleFilePath(IMediaFile selectedMediaFile, SubtitleRecord selectedSubtitle);
		Task<ISubtitleRecord[]> GetSubtitleList(IMediaFile selectedMediaFile);
	}

	public interface ISubtitleRecord
	{
		public string Language { get; set; }
		public string FilePath { get; set; }
	}
}