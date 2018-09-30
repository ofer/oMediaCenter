namespace oMediaCenter.Interfaces
{

	public class MediaFileRecord
    {
		public static readonly string[] VALID_EXTENSIONS = new string[] { "mp4", "avi", "m4v", "mkv" };

		/// <summary>
		/// A short name to describe the media (i. e. movie name)
		/// </summary>
		public string Name { get; set; }

        /// <summary>
        /// A more detailed description of the media (i. e. plot summary)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Unique hash for each media file
        /// </summary>
        public string Hash { get; set; }
    
        /// <summary>
        /// Mime type of the media file
        /// </summary>
        public string MediaType { get; set; }

        /// <summary>
        /// Mime type of the media's thumbnail
        /// </summary>
        public string ThumbnailType { get; set; }

        /// <summary>
        /// Extra text info i. e. for torrents it might be file size and
        /// list of files in the torrent (even though we only expose the main media and thumbnail)
        /// </summary>
        public string TechnicalInfo { get; set; }

        public float LastPlayedTime { get; set; }

		public bool FoundMetadata { get; set; }
    }
}