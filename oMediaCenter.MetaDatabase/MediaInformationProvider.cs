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
			StringBuilder movieCandidateName = new StringBuilder();
			bool hitYearSection = false;
			bool firstSection = true;
			foreach (string section in splitname)
			{
				if (section.All(sc => char.IsDigit(sc)) && section.Length == 4)
				{
					hitYearSection = true;
					result.Year = section;
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

			if (!hitYearSection)
			{
				// try using () in the year

			}

			result.Title = movieCandidateName.ToString();

			return result;
		}

		public MediaInformation GetEpisodeInfoForFilename(string filename)
		{
			var movieCandidate = GetFileMetadataFromFilename(filename);
			return SearchDatabaseForName(movieCandidate);
		}

		private MediaInformation SearchDatabaseForName(FileMetadata movieCandidate)
		{
			var mediaDatum = _dbContext.MediaDatum.Where(md => md.LowercaseTitle == movieCandidate.Title.ToLower()).ToList();
			//5 is year
			MediaData mediaData = mediaDatum.FirstOrDefault(md => movieCandidate.Equals(md));

			if (mediaData == null)
				mediaData = new MediaData() { Title = movieCandidate.Title, OriginalString = movieCandidate.Year };

			return mediaData?.ToMediaInformation();
		}
	}

}