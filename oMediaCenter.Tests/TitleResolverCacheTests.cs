using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace oMediaCenter.Tests
{
	public class TitleResolverCacheTests : IDisposable
	{
		private readonly string _tempDir;
		private readonly string _cachePath;
		private readonly Mock<ILogger<MetaDatabase.TitleResolverCache>> _loggerMock;

		public TitleResolverCacheTests()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), $"cache-test-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_tempDir);
			_cachePath = Path.Combine(_tempDir, "cache.json");
			_loggerMock = new Mock<ILogger<MetaDatabase.TitleResolverCache>>();
		}

		public void Dispose()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		}

		private MetaDatabase.TitleResolverCache CreateCache(string path = null) =>
			new MetaDatabase.TitleResolverCache(path ?? _cachePath, _loggerMock.Object);

		[Fact]
		public void ReturnsNullForUncachedFilename()
		{
			var cache = CreateCache();
			Assert.Null(cache.Get("nonexistent.mkv"));
		}

		[Fact]
		public void ReturnsCachedTitleForPreviouslyStoredFilename()
		{
			var cache = CreateCache();
			cache.Set("movie.mkv", "The Movie");
			Assert.Equal("The Movie", cache.Get("movie.mkv"));
		}

		[Fact]
		public void PersistsCacheToDiskAndReloadsOnNewInstance()
		{
			var cache1 = CreateCache();
			cache1.Set("movie.mkv", "The Movie");

			var cache2 = CreateCache();
			Assert.Equal("The Movie", cache2.Get("movie.mkv"));
		}

		[Fact]
		public void HandlesCorruptedCacheFileGracefully()
		{
			File.WriteAllText(_cachePath, "not valid json!!!");
			var cache = CreateCache();
			Assert.Null(cache.Get("anything.mkv"));
		}

		[Fact]
		public void HandlesMissingCacheFileGracefully()
		{
			var cache = CreateCache(Path.Combine(_tempDir, "does-not-exist.json"));
			Assert.Null(cache.Get("anything.mkv"));
		}

		[Fact]
		public void CachesUnknownResponses()
		{
			var cache = CreateCache();
			cache.Set("junk.mp4", "UNKNOWN");
			Assert.Equal("UNKNOWN", cache.Get("junk.mp4"));
			Assert.True(cache.ContainsKey("junk.mp4"));
		}

		[Fact]
		public void ThreadSafeConcurrentReadsAndWrites()
		{
			var cache = CreateCache();
			var tasks = new Task[20];
			for (int i = 0; i < 20; i++)
			{
				int index = i;
				tasks[i] = Task.Run(() =>
				{
					cache.Set($"file{index}.mkv", $"Title {index}");
					cache.Get($"file{index}.mkv");
					cache.ContainsKey($"file{index}.mkv");
				});
			}
			Task.WaitAll(tasks);

			for (int i = 0; i < 20; i++)
			{
				Assert.Equal($"Title {i}", cache.Get($"file{i}.mkv"));
			}
		}
	}
}
