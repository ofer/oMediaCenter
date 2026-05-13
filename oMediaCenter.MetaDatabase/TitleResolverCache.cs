using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace oMediaCenter.MetaDatabase
{
	public class TitleResolverCache
	{
		private readonly string _cacheFilePath;
		private readonly ILogger<TitleResolverCache> _logger;
		private readonly object _lock = new();
		private Dictionary<string, string> _cache;

		public TitleResolverCache(string cacheFilePath, ILogger<TitleResolverCache> logger)
		{
			_cacheFilePath = cacheFilePath;
			_logger = logger;
		}

		public string Get(string filename)
		{
			lock (_lock)
			{
				EnsureLoaded();
				return _cache.TryGetValue(filename, out var title) ? title : null;
			}
		}

		public void Set(string filename, string title)
		{
			lock (_lock)
			{
				EnsureLoaded();
				_cache[filename] = title;
				Save();
			}
		}

		public bool ContainsKey(string filename)
		{
			lock (_lock)
			{
				EnsureLoaded();
				return _cache.ContainsKey(filename);
			}
		}

		private void EnsureLoaded()
		{
			if (_cache != null)
				return;

			_cache = new Dictionary<string, string>();

			if (!File.Exists(_cacheFilePath))
				return;

			try
			{
				var json = File.ReadAllText(_cacheFilePath);
				var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
				if (loaded != null)
					_cache = loaded;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to load title resolver cache from {Path}, starting empty", _cacheFilePath);
				_cache = new Dictionary<string, string>();
			}
		}

		private void Save()
		{
			try
			{
				var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(_cacheFilePath, json);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to save title resolver cache to {Path}", _cacheFilePath);
			}
		}
	}
}
