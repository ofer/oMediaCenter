using oMediaCenter.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
  public class MediaFileStreamer : IMediaFileStreamer
  {
    ConcurrentBag<string> _hashesRunning;
    object _readFileLock;

    public MediaFileStreamer(IMediaFileProber fileProber, IMediaFileConverter mediaFileConverter)
    {
      Prober = fileProber;
      Converter = mediaFileConverter;
      _hashesRunning = new ConcurrentBag<string>();
      _readFileLock = new object();
    }

    const string MP4_MEDIA_TYPE = "video/mp4";
    public const string HLS_MEDIA_TYPE = "application/vnd.apple.mpegurl";

    public const string CACHE_DIR = "wwwroot\\cache";

    const string FILENAME_TEMPLATE = "{0}.m3u8";

    public IMediaFileProber Prober { get; }
    public IMediaFileConverter Converter { get; }

    public StreamingFile GetStream(IMediaFile selectedMediaFile)
    {
      if (selectedMediaFile.MediaFileRecord.MediaType == MP4_MEDIA_TYPE)
        return new StreamingFile(File.OpenRead(selectedMediaFile.GetFullFilePath()), MP4_MEDIA_TYPE);
      else
      {
        if (!Directory.Exists(CACHE_DIR))
          Directory.CreateDirectory(CACHE_DIR);

        string filename = string.Format(FILENAME_TEMPLATE, selectedMediaFile.MediaFileRecord.Hash);
        string filePath = Path.Combine(CACHE_DIR, filename);

        lock (_readFileLock)
        {
          if (!File.Exists(filePath) && !_hashesRunning.Contains(selectedMediaFile.MediaFileRecord.Hash))
          {
            // convert file to mp4 and send it along, h264 / aac
            // probe the file, see what conversion it needs
            MediaFileProbeInformation mfpi = Prober.GetProbeInfo(selectedMediaFile.GetFullFilePath());
            string targetVideoCodec = "copy";
            if (mfpi.VideoCodec != "h264")
              targetVideoCodec = "libx264";

            string targetAudioCodec = "copy";
            if (mfpi.AudioCodec != "aac")
              targetAudioCodec = "aac";

            Converter.Convert(selectedMediaFile.GetFullFilePath(), targetVideoCodec, targetAudioCodec, filename, CACHE_DIR, mfpi.NumberOfAudioChannels == 6, mfpi.ContainsSubtitles);
            _hashesRunning.Add(selectedMediaFile.MediaFileRecord.Hash);
          }
        }
        return new StreamingFile(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), HLS_MEDIA_TYPE);
      }
    }

    public async Task<string> GetSubtitleFilePath(IMediaFile selectedMediaFile)
    {
      string cachedSubtitlePath = Path.Combine(CACHE_DIR, selectedMediaFile.MediaFileRecord.Hash + ".vtt");
      if (File.Exists(cachedSubtitlePath))
        return cachedSubtitlePath;
      else
      {
        string subtitlePath = selectedMediaFile.GetFullSubtitleFilePath();
        if (subtitlePath == null)
          return null;

        if (Path.GetExtension(subtitlePath).ToLowerInvariant() == ".vtt")
          return subtitlePath;

        if (!Directory.Exists(CACHE_DIR))
          Directory.CreateDirectory(CACHE_DIR);

        if (!_hashesRunning.Contains(selectedMediaFile.MediaFileRecord.Hash + ".vtt"))
        {
          lock (_readFileLock)
          {
            _hashesRunning.Add(selectedMediaFile.MediaFileRecord.Hash + ".vtt");
          }
          await Converter.ConvertSubtitles(subtitlePath, cachedSubtitlePath);
        }

        return cachedSubtitlePath;
      }
    }
  }
}
