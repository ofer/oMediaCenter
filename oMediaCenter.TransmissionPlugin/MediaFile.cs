using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using Transmission.API.RPC.Entity;

namespace oMediaCenter.TransmissionPlugin
{
  public class MediaFile : IMediaFile
  {
    readonly string[] SUBTITLE_EXTENSION_LIST = new string[] { ".srt", ".ssa", ".ttml", ".sbv", ".vtt" };
    private string _subtitleFile;
    ILogger _logger;

    public MediaFile(ILoggerFactory loggerFactory, TorrentInfo ti, TransmissionTorrentFiles file)
    {
      _logger = loggerFactory.CreateLogger<MediaFile>();
      _subtitleFile = null;
      MediaFileRecord = new MediaFileRecord();
      MediaFileRecord.Description = ti.Comment;
      MediaFileRecord.Hash = $"TP{ti.ID}aAaA{Array.IndexOf<TransmissionTorrentFiles>(ti.Files, file)}";
      MediaFileRecord.Name = file.Name;
      FullFilePath = Path.Combine(ti.DownloadDir, file.Name.Replace('/', Path.DirectorySeparatorChar));
      MediaFileRecord.TechnicalInfo = Path.Combine(ti.DownloadDir, file.Name.Replace('/', Path.DirectorySeparatorChar));
      MediaFileRecord.MediaType = "video/" + Path.GetExtension(file.Name).ToLower().Substring(1);

      var filenameWithoutExtention = (string)Path.GetFileNameWithoutExtension(file.Name);

      // find any subtitle file
      var subtitleFile = ti.Files.FirstOrDefault(f => SUBTITLE_EXTENSION_LIST.Select(ext => filenameWithoutExtention + ext).Any(pf => pf == f.Name));
      if (subtitleFile != null)
        _subtitleFile = Path.Combine(ti.DownloadDir, subtitleFile.Name.Replace('/', Path.DirectorySeparatorChar));

    }

    string FullFilePath { get; set; }

    public MediaFileRecord MediaFileRecord { get; private set; }

    public MediaInformation Metadata { get; set; }

    public string GetFullFilePath()
    {
      _logger.LogDebug("Full filepath {0}", FullFilePath);
      return FullFilePath;
    }

    public Stream GetMediaData()
    {
      return new FileStream(FullFilePath, FileMode.Open, FileAccess.Read);
    }

    public Stream GetThumbnailData()
    {
      return null;
    }

    public string GetFullSubtitleFilePath()
    {
      return _subtitleFile;
    }
  }
}
