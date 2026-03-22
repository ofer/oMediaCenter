using Microsoft.EntityFrameworkCore;
using oMediaCenter.Interfaces;
using System.Linq;

namespace oMediaCenter.MetaDatabase
{
	public abstract class MediaInformationProviderBase
	{
		private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

		protected MediaInformationProviderBase(IDbContextFactory<MetaDataContext> dbContextFactory)
		{
			_dbContextFactory = dbContextFactory;
		}

		protected MediaInformation SearchDatabaseForName(FileMetadata fileMetadata)
		{
			if (fileMetadata == null || string.IsNullOrWhiteSpace(fileMetadata.Title))
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

		protected MediaInformation CreateFallbackMediaInformation(FileMetadata fileMetadata)
		{
			return new MediaInformation
			{
				Episode = fileMetadata.Episode,
				Year = fileMetadata.Year,
				Title = fileMetadata.Title,
				Season = fileMetadata.Season
			};
		}
	}
}
