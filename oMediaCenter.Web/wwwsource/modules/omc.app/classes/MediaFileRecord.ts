export class MediaFileRecord {
    /// <summary>
    /// A short name to describe the media (i. e. movie name)
    /// </summary>
    name: string;

    /// <summary>
    /// A more detailed description of the media (i. e. plot summary)
    /// </summary>
    description: string;

    /// <summary>
    /// Unique hash for each media file
    /// </summary>
    hash: string;

    /// <summary>
    /// Mime type of the media file
    /// </summary>
    mediatype: string;

    /// <summary>
    /// Mime type of the media's thumbnail
    /// </summary>
    thumbnailtype: string;

    /// <summary>
    /// Extra text info i. e. for torrents it might be file size and
    /// list of files in the torrent (even though we only expose the main media and thumbnail)
    /// </summary>
    technicalinfo: string;
}
