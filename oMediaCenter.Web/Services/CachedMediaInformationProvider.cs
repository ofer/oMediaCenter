using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using oMediaCenter.MetaDatabase;
using oMediaCenter.Web.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Services
{
    public class CachedMediaInformationProvider : IMediaInformationProvider
    {
        private const int CacheMissBatchSize = 10;

        private readonly IDbContextFactory<MediaCenterContext> _dbContextFactory;
        private readonly AiMediaInformationProvider _innerProvider;
        private readonly ILogger<CachedMediaInformationProvider> _logger;

        public CachedMediaInformationProvider(
            IDbContextFactory<MediaCenterContext> dbContextFactory,
            AiMediaInformationProvider innerProvider,
            ILogger<CachedMediaInformationProvider> logger)
        {
            _dbContextFactory = dbContextFactory;
            _innerProvider = innerProvider;
            _logger = logger;
        }

        public MediaInformation GetEpisodeInfoForFilename(string filename)
        {
            var results = GetEpisodeInfoForFilenames(new[] { filename });
            if (results.TryGetValue(filename, out var info))
                return info;

            return new MediaInformation();
        }

        public IDictionary<string, MediaInformation> GetEpisodeInfoForFilenames(IEnumerable<string> filenames)
        {
            var nonEmptyFilenames = filenames
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (!nonEmptyFilenames.Any())
                return new Dictionary<string, MediaInformation>(StringComparer.Ordinal);

            var result = new Dictionary<string, MediaInformation>(StringComparer.Ordinal);

            using (var context = _dbContextFactory.CreateDbContext())
            {
                var cachedEntries = context.CachedMediaInformationRecords
                    .Where(c => nonEmptyFilenames.Contains(c.Filename))
                    .ToList();

                var cachedDictionary = cachedEntries.ToDictionary(
                    c => c.Filename,
                    c => ConvertToMediaInformation(c),
                    StringComparer.Ordinal);

                foreach (var entry in cachedDictionary)
                {
                    result[entry.Key] = entry.Value;
                }

                var cacheMisses = nonEmptyFilenames
                    .Where(f => !cachedDictionary.ContainsKey(f))
                    .ToList();

		_logger.LogInformation("Total of {0} cache misses, chunk batch size {1}", cacheMisses.Count(), CacheMissBatchSize);

                foreach (var cacheMissBatch in cacheMisses.Chunk(CacheMissBatchSize))
                {
                    var emptyFilenames = new List<string>();
                    var freshResults = _innerProvider.GetEpisodeInfoForFilenames(cacheMissBatch);

                    foreach (var kvp in freshResults)
                    {
                        result[kvp.Key] = kvp.Value;

                        if (IsEmptyResult(kvp.Value))
                        {
                            emptyFilenames.Add(kvp.Key);
                        }
                    }

                    if (emptyFilenames.Any())
                    {
                        _logger.LogInformation(
                            "Empty metadata results for filenames: {Filenames}",
                            string.Join(", ", emptyFilenames));
                    }

                    var cacheRecords = freshResults
                        .Select(kvp => CreateCacheRecord(kvp.Key, kvp.Value))
                        .ToList();

                    if (cacheRecords.Any())
                    {
                        context.CachedMediaInformationRecords.AddRange(cacheRecords);
                        context.SaveChanges();

                        _logger.LogInformation(
                            "Cached {Count} AI metadata results for cache-miss batch",
                            cacheRecords.Count);
                    }
                }
            }

            return result;
        }

        private static CachedMediaInformation CreateCacheRecord(string filename, MediaInformation info)
        {
            return new CachedMediaInformation
            {
                Filename = filename,
                Title = info?.Title,
                OtherInfo = info?.OtherInfo,
                Year = info?.Year,
                ImdbNumber = info?.ImdbNumber,
                VideoType = info?.VideoType,
                Episode = info?.Episode,
                Season = info?.Season,
                Genres = info?.Genres
            };
        }

        private static bool IsEmptyResult(MediaInformation info)
        {
            return info == null ||
                   string.IsNullOrWhiteSpace(info.Title) &&
                   string.IsNullOrWhiteSpace(info.OtherInfo) &&
                   string.IsNullOrWhiteSpace(info.Year) &&
                   string.IsNullOrWhiteSpace(info.ImdbNumber) &&
                   string.IsNullOrWhiteSpace(info.VideoType) &&
                   string.IsNullOrWhiteSpace(info.Episode) &&
                   string.IsNullOrWhiteSpace(info.Season) &&
                   string.IsNullOrWhiteSpace(info.Genres);
        }

        private static MediaInformation ConvertToMediaInformation(CachedMediaInformation cached)
        {
            return new MediaInformation
            {
                Title = cached.Title,
                OtherInfo = cached.OtherInfo,
                Year = cached.Year,
                ImdbNumber = cached.ImdbNumber,
                VideoType = cached.VideoType,
                Episode = cached.Episode,
                Season = cached.Season,
                Genres = cached.Genres
            };
        }
    }
}
