using System.IO;

namespace oMediaCenter.Interfaces
{
	public class StreamingFile
	{
		public StreamingFile(Stream stream, string mediaType)
		{
			Stream = stream;
			MediaType = mediaType;
		}

		public Stream Stream { get; }
		public string MediaType { get; }
	}
}