using oMediaCenter.Interfaces;

public interface ISubtitleFileCache
{
	ISubtitleRecord[] GetCachedSubtitleRecords(string hash);
}

public class OpenSubtitleRecord : ISubtitleRecord
{
	public string Language { get; set; }
	public string FilePath { get; set; }
}