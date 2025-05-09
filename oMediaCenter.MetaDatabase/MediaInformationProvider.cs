using oMediaCenter.Interfaces;
using System.Linq;
using System.Text;

namespace oMediaCenter.MetaDatabase
{
	public class MediaInformationProvider : IMediaInformationProvider
	{
		private MetaDataContext _dbContext;

		public MediaInformationProvider(MetaDataContext dbContext)
		{
			_dbContext = dbContext;
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

			result.Title = mediaCandidateName.ToString();

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
			if (section.Length == 6)
			{
				result.Season = section.Substring(1, 2);
				result.Episode = section.Substring(4, 2);
			}
		}

		private bool IsEpisodeSection(string section)
		{
			if (section.Length == 6 && (section[0] == 'S' || section[0] == 's') &&
				char.IsDigit(section[1]) && char.IsDigit(section[2]) &&
				(section[3] == 'E' || section[3] == 'e') &&
				char.IsDigit(section[4]) && char.IsDigit(section[5]))
			{
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
			var mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == searchableTitle);

			if (mediaDatum.Count() == 0)
			{
				string prependedTitle = "the" + searchableTitle;
				mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
			}

			if (mediaDatum.Count() == 0)
			{
				string prependedTitle = "the" + searchableTitle + "movie";
				mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
			}

			if (mediaDatum.Count() == 0)
			{
				string prependedTitle = searchableTitle + "movie";
				mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
			}

			if (mediaDatum.Count() != 0 && !string.IsNullOrEmpty(fileMetadata.Year))
				mediaData = mediaDatum.ToList().FirstOrDefault(md => md.OriginalString.Split('	')[5] == fileMetadata.Year);

			if (mediaDatum.Count() != 0 && !string.IsNullOrEmpty(fileMetadata.Season))
				mediaData = mediaDatum.ToList().FirstOrDefault(md => md.OriginalString.Split('	')[1] == "tvSeries");


			return mediaData?.ToMediaInformation(fileMetadata);
		}
	}
}