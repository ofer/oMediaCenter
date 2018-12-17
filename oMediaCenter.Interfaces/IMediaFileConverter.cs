namespace oMediaCenter.Interfaces
{
	public interface IMediaFileConverter
	{
		void Convert(string v, string targetVideoCodec, string targetAudioCodec, string filename, bool forceStereo);
	}
}