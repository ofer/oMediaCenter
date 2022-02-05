using System.IO;

namespace oMediaCenter.Interfaces
{
  public interface IMediaFile
  {
    MediaFileRecord MediaFileRecord { get; }

    MediaInformation Metadata { get; set; }

    string GetFullFilePath();

    string GetFullSubtitleFilePath();

    /// <summary>
    /// Gets the thumbnail data, null if no thumbnail exists
    /// </summary>
    /// <returns></returns>
    Stream GetThumbnailData();
  }
}