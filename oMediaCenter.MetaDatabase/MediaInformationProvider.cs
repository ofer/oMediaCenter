using Microsoft.EntityFrameworkCore;
using oMediaCenter.Interfaces;
using System.Linq;
using System.Text;

namespace oMediaCenter.MetaDatabase
{
	public class MediaInformationProvider : IMediaInformationProvider
	{
		private IDbContextFactory<MetaDataContext> _dbContextFactory;

		public MediaInformationProvider(IDbContextFactory<MetaDataContext>  dbContextFactory)
		{
			_dbContextFactory = dbContextFactory;
		}

		public FileMetadata GetFileMetadataFromFilename(string filename)
		{
			FileMetadata result = new FileMetadata();

			// look for something to search for
			string[] splitname = filename.Split('.', ' ');
			StringBuilder mediaCandidateName = new StringBuilder();
			bool hitYearSection = false;
			bool firstSection = true;
			foreach (string section in splitname)
			{
				if (IsYearSection(section))
				{
					hitYearSection = true;
					result.Year = ExtractYear(section);
					break;
				}
				else if (IsEpisodeSection(section))
				{
					hitYearSection = true;
					ExtractEpisodeSeason(section, result);
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

			result.Title = mediaCandidateName.ToString().TrimEnd(' ', '-');

			if (!hitYearSection)
			{
				// try Naruto style parsing (Naruto Shippuden Episode 001 Homecoming.mkv)
				int episodeIndex = filename.IndexOf("Episode");
				if (episodeIndex != -1)
				{
					if (int.TryParse(filename.Substring(episodeIndex + 7, 3), out int episodeNumber))
					{
						result.Episode = episodeNumber.ToString();
						result.Title = filename.Substring(0, episodeIndex);
						result.Episode = filename.Substring(episodeIndex + 7);
					}
				}

				// try using [] with no space in the year
				int yearStartCandidate = filename.IndexOfAny(new char[] { '[', '{' });
				if (yearStartCandidate > 0)
				{
					string yearSection = filename.Substring(yearStartCandidate, 6);
					if (yearSection.Last() == ']' || yearSection.Last() == '}')
					{
						result.Title = filename.Substring(0, yearStartCandidate);
						result.Year = yearSection.Substring(1, 4);
					}
				}
			}

			return result;
		}

		private void ExtractEpisodeSeason(string section, FileMetadata result)
		{
			// S01E02 format (6 chars)
			if (section.Length == 6 && (section[0] == 'S' || section[0] == 's'))
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
			// Match S01E02 format (6 chars)
			if (section.Length == 6 && (section[0] == 'S' || section[0] == 's') &&
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
			if (databaseCandidate == null)
			{
				MediaInformation nonDatabaseMediaInformation = new MediaInformation();
				nonDatabaseMediaInformation.Episode = movieCandidate.Episode;
				nonDatabaseMediaInformation.Year = movieCandidate.Year;
				nonDatabaseMediaInformation.Title = movieCandidate.Title;
				nonDatabaseMediaInformation.Season = movieCandidate.Season;
				return nonDatabaseMediaInformation;
			}
			else
				return databaseCandidate;
		}

		private MediaInformation SearchDatabaseForName(FileMetadata fileMetadata)
		{
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