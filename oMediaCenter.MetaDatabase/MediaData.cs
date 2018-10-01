using oMediaCenter.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace oMediaCenter.MetaDatabase
{
	public class MediaData
	{
		[Key]
		public int Id { get; set; }

		public string LowercaseTitle { get; set; }

		public string Title { get; set; }

		public string OriginalString { get; set; }

		public MediaInformation ToMediaInformation(FileMetadata fileMetadata)
		{
			string[] sections = OriginalString.Split('	');

			MediaInformation result = new MediaInformation
			{
				Title = Title,
				Year = sections[5],
				Episode = fileMetadata.Episode,
				Season = fileMetadata.Season,
				ImdbNumber = sections[0],
				VideoType = sections[1],
				Genres = sections[8],
				OtherInfo = OriginalString
			};
			return result;
		}
	}
}