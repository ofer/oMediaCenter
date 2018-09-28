using System.IO;

namespace oMediaCenter.Interfaces
{
    public interface IMediaFile
    {
        MediaFileRecord MediaFileRecord { get; }

		MediaInformation Metadata { get; set; }

		string GetFullFilePath();

        /// <summary>
        /// Gets the actual data, null if no data exists
        /// </summary>
        /// <returns></returns>
        Stream GetMediaData();

        /// <summary>
        /// Gets the thumbnail data, null if no thumbnail exists
        /// </summary>
        /// <returns></returns>
        Stream GetThumbnailData();
    }
}