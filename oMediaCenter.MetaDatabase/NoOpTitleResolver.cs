using oMediaCenter.Interfaces;

namespace oMediaCenter.MetaDatabase
{
	public class NoOpTitleResolver : ILlmTitleResolver
	{
		public string ResolveTitleFromFilename(string filename) => null;
	}
}
