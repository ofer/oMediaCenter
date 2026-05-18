using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace oMediaCenter.MetaDatabase
{
	public class MediaInformationProvider : IMediaInformationProvider
	{
		private IDbContextFactory<MetaDataContext> _dbContextFactory;
		private ILlmTitleResolver _titleResolver;
		private ILogger<MediaInformationProvider> _logger;

		// Known junk tokens that indicate the title has ended and technical info follows.
		// These are standard scene release naming conventions.
		private static readonly HashSet<string> _junkTokens = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
		{
			// Video codecs
			"xvid", "x264", "x265", "h264", "h265", "hevc", "divx", "vp9",
			// Audio codecs
			"aac", "ac3", "dts", "mp3", "flac", "dd51",
			// Source/quality
			"dvdscr", "dvdrip", "dvd", "bdrip", "brrip", "bluray", "blu-ray",
			"webrip", "web-dl", "webdl", "hdrip", "hdtv", "pdtv", "sdtv",
			"cam", "ts", "tc", "screener", "r5", "dvdr",
			// Resolution
			"720p", "1080p", "1080i", "2160p", "4k",
			// Release groups and common tags
			"yify", "yts", "rarbg", "ettv", "eztv", "evo", "sparks",
			"axxo", "fgt", "fleet", "geckos", "stuttershit", "cmrg",
			"asap", "avs", "lol", "dimension", "killers",
			// Container hints (when split into tokens)
			"avi", "mkv", "mp4",
			// Other common tags
			"extended", "unrated", "remastered", "proper", "internal",
			"dubbed", "subbed", "multi", "dual", "readnfo",
		};

		public MediaInformationProvider(IDbContextFactory<MetaDataContext> dbContextFactory,  ILogger<MediaInformationProvider> logger, ILlmTitleResolver titleResolver = null)
		{
			_dbContextFactory = dbContextFactory;
			_titleResolver = titleResolver;
			_logger = logger;
		}

		/// <summary>
		/// Pre-cleans a filename by stripping bracketed blocks, extracting embedded years,
		/// and removing release group prefixes before the main parsing loop.
		/// </summary>
		private string PreCleanFilename(string filename, FileMetadata result)
		{
			string cleaned = filename;

			// Step 1: Extract years from brackets/parens/braces before stripping them.
			// Handles [2006], {2006}, and (1996) (even when glued to a word like Gilmore(1996)).
			// Everything after the year is considered junk (quality tags, codec info, etc.)
			var bracketYearMatch = Regex.Match(cleaned, @"[\[\(\{](\d{4})[\]\)\}]");
			if (bracketYearMatch.Success)
			{
				result.Year = bracketYearMatch.Groups[1].Value;
				cleaned = cleaned.Substring(0, bracketYearMatch.Index);
			}

			// Step 2: Strip remaining bracketed blocks like [www.UsaBit.com] or {release-group}
			// These are typically website watermarks or release group tags.
			cleaned = Regex.Replace(cleaned, @"\[.*?\]", " ");
			cleaned = Regex.Replace(cleaned, @"\{.*?\}", " ");

			// Step 3: Strip file extension
			cleaned = Regex.Replace(cleaned, @"\.(avi|mkv|mp4|mov|wmv|flv|webm|m4v|mpg|mpeg)$", "", RegexOptions.IgnoreCase);

			// Step 4: Detect scene release naming pattern (all dashes, no spaces/dots)
			// e.g., "sprinter-12angrymen-cd1" → group="sprinter", title="12angrymen"
			// Only triggers when: no spaces, no dots, at least 2 dashes, and first segment is all lowercase
			if (!cleaned.Contains(' ') && !cleaned.Contains('.'))
			{
				var dashParts = cleaned.Split('-');
				if (dashParts.Length >= 3 && dashParts[0].Length > 0 && dashParts[0].All(c => char.IsLower(c)))
				{
					// Skip first part (release group), stop before cdN disc indicators
					var titleParts = dashParts.Skip(1)
						.TakeWhile(p => !Regex.IsMatch(p, @"^cd\d+$", RegexOptions.IgnoreCase))
						.ToArray();
					if (titleParts.Length > 0)
						cleaned = string.Join(" ", titleParts);
				}
			}

			// Step 5: Strip release group prefix like "wtf-", "fgt-" (short lowercase prefix before a dash)
			// Only strip if the prefix is short (1-4 chars) and all lowercase, suggesting a scene group tag.
			var groupPrefixMatch = Regex.Match(cleaned.TrimStart(), @"^([a-z]{1,4})-(?=[A-Za-z0-9])");
			if (groupPrefixMatch.Success)
			{
				cleaned = cleaned.TrimStart().Substring(groupPrefixMatch.Groups[1].Length + 1);
			}

			// Step 6: Collapse multiple spaces and trim leading/trailing junk
			cleaned = Regex.Replace(cleaned, @"\s+", " ");
			cleaned = cleaned.Trim(' ', '-', '_');

			return cleaned;
		}

		public FileMetadata GetFileMetadataFromFilename(string filename)
		{
			FileMetadata result = new FileMetadata();
			string originalFilename = filename;
			filename = GetLeafFilename(filename);

			// Pre-clean the filename before main parsing
			string cleanedFilename = PreCleanFilename(filename, result);

			// look for something to search for
			string[] splitname = cleanedFilename.Split('.', ' ');
			StringBuilder mediaCandidateName = new StringBuilder();
			bool hitStopToken = false;
			bool firstSection = true;
			foreach (string section in splitname)
			{
				if (string.IsNullOrWhiteSpace(section))
					continue;

				if (IsYearSection(section))
				{
					hitStopToken = true;
					result.Year = ExtractYear(section);
					break;
				}
				else if (IsEpisodeSection(section))
				{
					hitStopToken = true;
					ExtractEpisodeSeason(section, result);
					break;
				}
				else if (_junkTokens.Contains(section))
				{
					hitStopToken = true;
					break;
				}
				else if (firstSection)
				{
					firstSection = false;
				}
				else
					mediaCandidateName.Append(" ");

				mediaCandidateName.Append(section);
			}

			// Only overwrite title from the main loop if we didn't already set it,
			// or if the main loop found a better (non-empty) title
			string parsedTitle = mediaCandidateName.ToString().TrimEnd(' ', '-');
			if (!string.IsNullOrWhiteSpace(parsedTitle))
				result.Title = parsedTitle;

			if (!hitStopToken)
			{
				// try Naruto style parsing (Naruto Shippuden Episode 001 Homecoming.mkv)
				int episodeIndex = cleanedFilename.IndexOf("Episode");
				if (episodeIndex != -1)
				{
					if (int.TryParse(cleanedFilename.Substring(episodeIndex + 7, 3), out int episodeNumber))
					{
						result.Episode = episodeNumber.ToString();
						result.Title = cleanedFilename.Substring(0, episodeIndex);
						result.Episode = cleanedFilename.Substring(episodeIndex + 7);
					}
				}

				// try using [] with no space in the year
				int yearStartCandidate = cleanedFilename.IndexOfAny(new char[] { '[', '{' });
				if (yearStartCandidate > 0)
				{
					string yearSection = cleanedFilename.Substring(yearStartCandidate, 6);
					if (yearSection.Last() == ']' || yearSection.Last() == '}')
					{
						result.Title = cleanedFilename.Substring(0, yearStartCandidate);
						result.Year = yearSection.Substring(1, 4);
					}
				}
			}

			FillMissingMetadataFromPath(originalFilename, result);

			return result;
		}

		private string GetLeafFilename(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return filename;

			return Path.GetFileName(filename.Replace('\\', '/'));
		}

		private void FillMissingMetadataFromPath(string filename, FileMetadata result)
		{
			if (string.IsNullOrWhiteSpace(filename))
				return;

			string normalizedPath = filename.Replace('\\', '/');
			string[] pathSections = normalizedPath.Split('/').Where(section => !string.IsNullOrWhiteSpace(section)).ToArray();
			if (pathSections.Length <= 1)
				return;

			for (int sectionIndex = pathSections.Length - 2; sectionIndex >= 0; sectionIndex--)
			{
				FillMissingMetadataFromPathSection(pathSections[sectionIndex], result);
			}
		}

		private void FillMissingMetadataFromPathSection(string section, FileMetadata result)
		{
			if (string.IsNullOrWhiteSpace(result.Year))
			{
				var yearMatch = Regex.Match(section, @"(?:^|[^0-9])((?:19|20)\d{2})(?:[^0-9]|$)");
				if (yearMatch.Success)
					result.Year = yearMatch.Groups[1].Value;
			}

			foreach (string token in Regex.Split(section, @"[\.\s_\-]+"))
			{
				if (!IsEpisodeSection(token))
					continue;

				var pathMetadata = new FileMetadata();
				ExtractEpisodeSeason(token, pathMetadata);
				if (string.IsNullOrWhiteSpace(result.Season))
					result.Season = pathMetadata.Season;
				if (string.IsNullOrWhiteSpace(result.Episode))
					result.Episode = pathMetadata.Episode;
			}

			if (string.IsNullOrWhiteSpace(result.Season))
			{
				var seasonMatch = Regex.Match(section, @"(?:^|[\.\s_\-])(?:season|series)[\.\s_\-]*(\d{1,2})(?:$|[\.\s_\-])", RegexOptions.IgnoreCase);
				if (!seasonMatch.Success)
					seasonMatch = Regex.Match(section, @"(?:^|[\.\s_\-])S(\d{1,2})(?:$|[\.\s_\-])", RegexOptions.IgnoreCase);

				if (seasonMatch.Success)
					result.Season = seasonMatch.Groups[1].Value;
			}

			if (string.IsNullOrWhiteSpace(result.Episode))
			{
				var episodeMatch = Regex.Match(section, @"(?:^|[\.\s_\-])(?:episode|ep)[\.\s_\-]*(\d{1,3})(?:$|[\.\s_\-])", RegexOptions.IgnoreCase);
				if (episodeMatch.Success)
					result.Episode = episodeMatch.Groups[1].Value;
			}
		}

		private void ExtractEpisodeSeason(string section, FileMetadata result)
		{
			// S01E02 or multi-episode S01E02+E03, S01E01E02 format
			if (section.Length >= 6 && (section[0] == 'S' || section[0] == 's') &&
				char.IsDigit(section[1]) && char.IsDigit(section[2]) &&
				(section[3] == 'E' || section[3] == 'e') &&
				char.IsDigit(section[4]) && char.IsDigit(section[5]))
			{
				result.Season = section.Substring(1, 2);
				result.Episode = section.Substring(4, 2);
				return;
			}

			// NxNN format (e.g., 1x01, 12x03)
			int xIndex = section.IndexOf('x');
			if (xIndex > 0)
			{
				result.Season = section.Substring(0, xIndex);
				result.Episode = section.Substring(xIndex + 1);
			}
		}

		private bool IsEpisodeSection(string section)
		{
			// Match S01E02 format (exactly 6 chars) or multi-episode formats:
			// S01E02+E03, S01E01E02, S03E01+E02+E03, etc.
			if (section.Length >= 6 && (section[0] == 'S' || section[0] == 's') &&
				char.IsDigit(section[1]) && char.IsDigit(section[2]) &&
				(section[3] == 'E' || section[3] == 'e') &&
				char.IsDigit(section[4]) && char.IsDigit(section[5]))
			{
				return true;
			}

			// Match NxNN format (e.g., 1x01, 12x03)
			int xIndex = section.IndexOf('x');
			if (xIndex > 0 && xIndex < section.Length - 1)
			{
				string seasonPart = section.Substring(0, xIndex);
				string episodePart = section.Substring(xIndex + 1);
				if (seasonPart.All(c => char.IsDigit(c)) && episodePart.All(c => char.IsDigit(c)))
					return true;
			}

			return false;
		}

		private string ExtractYear(string section)
		{
			if (section.Length == 4)
				return section;
			return section.Substring(1, 4);
		}

		private bool IsYearSection(string section)
		{
			return (section.All(sc => char.IsDigit(sc)) && section.Length == 4) ||
				(section.Length == 6 && section[0] == '(' && section[5] == ')' && section.Substring(1, 4).All(sc => char.IsDigit(sc)));
		}

		public MediaInformation GetEpisodeInfoForFilename(string filename)
		{
			var movieCandidate = GetFileMetadataFromFilename(filename);
			var databaseCandidate = SearchDatabaseForName(movieCandidate);
			if (databaseCandidate != null)
			{
				_logger.LogDebug("Found candidate");
				return databaseCandidate;
			}
			_logger.LogInformation("No candidate found");

			// Fallback: try LLM title resolution
			if (_titleResolver != null)
			{
				_logger.LogInformation("Looking for AI candidate fix");
				var resolvedTitle = _titleResolver.ResolveTitleFromFilename(filename);
				if (!string.IsNullOrWhiteSpace(resolvedTitle))
				{
					// Re-search DB with the resolved title
					var resolvedMetadata = new FileMetadata
					{
						Title = resolvedTitle,
						Year = movieCandidate.Year,
						Season = movieCandidate.Season,
						Episode = movieCandidate.Episode
					};
					var resolvedDbCandidate = SearchDatabaseForName(resolvedMetadata);
					if (resolvedDbCandidate != null)
						return resolvedDbCandidate;

					// No DB match -- use the resolved title directly
					return new MediaInformation
					{
						Title = resolvedTitle,
						Year = movieCandidate.Year,
						Season = movieCandidate.Season,
						Episode = movieCandidate.Episode
					};
				}
			}

			// Final fallback: use parsed title as-is
			return new MediaInformation
			{
				Episode = movieCandidate.Episode,
				Year = movieCandidate.Year,
				Title = movieCandidate.Title,
				Season = movieCandidate.Season
			};
		}

		private MediaInformation SearchDatabaseForName(FileMetadata fileMetadata)
		{
			if (string.IsNullOrWhiteSpace(fileMetadata.Title))
				return null;

			if (_dbContextFactory == null)
				return null;

			MediaData mediaData = null;

			string searchableTitle = fileMetadata.Title.ToSearchableString();
			using (var dbContext = _dbContextFactory.CreateDbContext())
			{
				var mediaDatum = dbContext.MediaDatum.Where(md => md.LowercaseTitle == searchableTitle);

				if (mediaDatum.Count() == 0)
				{
					string prependedTitle = "the" + searchableTitle;
					mediaDatum = dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
				}

				if (mediaDatum.Count() == 0)
				{
					string prependedTitle = "the" + searchableTitle + "movie";
					mediaDatum = dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
				}

				if (mediaDatum.Count() == 0)
				{
					string prependedTitle = searchableTitle + "movie";
					mediaDatum = dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
				}

				if (mediaDatum.Count() != 0 && !string.IsNullOrEmpty(fileMetadata.Year))
					mediaData = mediaDatum.ToList().FirstOrDefault(md => md.OriginalString.Split('	')[5] == fileMetadata.Year);

				if (mediaDatum.Count() != 0 && !string.IsNullOrEmpty(fileMetadata.Season))
					mediaData = mediaDatum.ToList().FirstOrDefault(md => md.OriginalString.Split('	')[1] == "tvSeries");
			}

			return mediaData?.ToMediaInformation(fileMetadata);
		}
	}
}
