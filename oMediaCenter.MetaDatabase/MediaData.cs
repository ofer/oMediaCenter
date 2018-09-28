using System;
using System.ComponentModel.DataAnnotations;
using oMediaCenter.Interfaces;

namespace oMediaCenter.MetaDatabase
{
	public class MediaData
	{
		[Key]
		public int Id { get; set; }

		public string LowercaseTitle { get; set; }

		public string Title { get; set; }

		public string OriginalString { get; set; }

		public MediaInformation ToMediaInformation()
		{
			MediaInformation result = new MediaInformation
			{
				Title = Title,
				OtherInfo = OriginalString
			};
			return result;
		}
	}
}