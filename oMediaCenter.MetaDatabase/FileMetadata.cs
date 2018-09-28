namespace oMediaCenter.MetaDatabase
{
	public class FileMetadata
	{
		public string Title { get; internal set; }
		public string Year { get; internal set; }

		public bool Equals(MediaData mediaData)
		{
			return (mediaData.Title == Title && mediaData.OriginalString.Split('	')[5] == Year);
		}
	}
}