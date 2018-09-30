using oMediaCenter.Interfaces;
using System;
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
			StringBuilder movieCandidateName = new StringBuilder();
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
				else
				{
					if (firstSection)
					{
						firstSection = false;
					}
					else
						movieCandidateName.Append(" ");
				}

				movieCandidateName.Append(section);
			}

			result.Title = movieCandidateName.ToString();

			if (!hitYearSection)
			{
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
			return SearchDatabaseForName(movieCandidate);
		}

		private MediaInformation SearchDatabaseForName(FileMetadata fileMetadata)
		{
			MediaData mediaData = null;

			string lowercaseTitle = fileMetadata.Title.ToLower();
			var mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == lowercaseTitle);

			if (mediaDatum.Count() == 0)
			{
				string prependedTitle = "the " + lowercaseTitle;
				mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
			}

			if (mediaDatum.Count() == 0)
			{
				string prependedTitle = "the " + lowercaseTitle + " movie";
				mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
			}

			if (mediaDatum.Count() == 0)
			{
				string prependedTitle = lowercaseTitle + " movie";
				mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == prependedTitle);
			}

			if (mediaDatum.Count() != 0)
				mediaData = mediaDatum.ToList().FirstOrDefault(md => md.OriginalString.Split('	')[5] == fileMetadata.Year);

			return mediaData?.ToMediaInformation();
		}
	}
}