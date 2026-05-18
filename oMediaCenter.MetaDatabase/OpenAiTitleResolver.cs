using oMediaCenter.Interfaces;

namespace oMediaCenter.MetaDatabase
{
	public class OpenAiTitleResolver : ILlmTitleResolver
	{
		private readonly TitleResolverCache _cache;
		private readonly BackgroundTitleResolver _backgroundResolver;

		public OpenAiTitleResolver(
			TitleResolverCache cache,
			BackgroundTitleResolver backgroundResolver)
		{
			_cache = cache;
			_backgroundResolver = backgroundResolver;
		}

		public string ResolveTitleFromFilename(string filename)
		{
			if (_cache.ContainsKey(filename))
			{
				var cached = _cache.Get(filename);
				return cached == "UNKNOWN" ? null : cached;
			}

			_backgroundResolver.Enqueue(filename);
			return null;
		}
	}
}
