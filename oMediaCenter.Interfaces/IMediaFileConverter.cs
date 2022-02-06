using System.Threading.Tasks;

namespace oMediaCenter.Interfaces
{
	public interface IMediaFileConverter
	{
		void Convert(string sourceFile, string targetVideoCodec, string targetAudioCodec, string targetFilename, bool forceStereo, bool containsSubtitles);

		Task ConvertSubtitles(string sourcefile, string targetFile);
	}
}