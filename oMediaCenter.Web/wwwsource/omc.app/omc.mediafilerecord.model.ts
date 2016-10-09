export class MediaFileRecord {
    constructor(
        /// <summary>
        /// A short name to describe the media (i. e. movie name)
        /// </summary>
        public name: string,

        /// <summary>
        /// A more detailed description of the media (i. e. plot summary)
        /// </summary>
        public description: string,

        /// <summary>
        /// Unique hash for each media file
        /// </summary>
        public hash: string,

        /// <summary>
        /// Mime type of the media file
        /// </summary>
        public mediatype: string,

        /// <summary>
        /// Mime type of the media's thumbnail
        /// </summary>
        public thumbnailtype: string,

        /// <summary>
        /// Extra text info i. e. for torrents it might be file size and
        /// list of files in the torrent (even though we only expose the main media and thumbnail)
        /// </summary>
        public technicalinfo: string,
        public lastPlayedTime: number) { }
}
