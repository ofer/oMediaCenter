using oMediaCenter.Interfaces;
using System.IO;

namespace oMediaCenter.Web.Model
{
  internal class CachedMediaFile : IMediaFile
  {

    public CachedMediaFile(IMediaFile mf, string subtitleFile)
    {
      _fullFilePath = mf.GetFullFilePath();
      MediaFileRecord = mf.MediaFileRecord;
      Metadata = mf.Metadata;

      _fullSubtitlePath = subtitleFile;
      _thumbnailData = null;
    }

    private string _fullFilePath;
    private string _fullSubtitlePath;
    private Stream _thumbnailData;

    public MediaFileRecord MediaFileRecord { get; set; }

    public MediaInformation Metadata { get; set; }


    public string GetFullFilePath()
    {
      return _fullFilePath;
    }

    public string GetFullSubtitleFilePath()
    {
      return _fullSubtitlePath;
    }

    public Stream GetThumbnailData()
    {
      return _thumbnailData;
    }
  }
}
