namespace oMediaCenter.Interfaces
{
	public interface ILlmTitleResolver
	{
		/// <summary>
		/// Attempts to resolve a movie/TV show title from a raw filename.
		/// Returns null when resolution is unavailable (no API key, API error, timeout).
		/// </summary>
		string ResolveTitleFromFilename(string filename);
	}
}
