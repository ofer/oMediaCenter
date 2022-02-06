using oMediaCenter.Interfaces;
using oMediaCenter.Web.Utilities;
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
    private ISubtitleProvider _subtitleProvider;
    object _readFileLock;

    public MediaFileStreamer(IMediaFileProber fileProber, IMediaFileConverter mediaFileConverter, ISubtitleProvider subtitleProvider)
    {
      Prober = fileProber;
      Converter = mediaFileConverter;
      _hashesRunning = new ConcurrentBag<string>();
      _subtitleProvider = subtitleProvider;

      _readFileLock = new object();
    }

    const string MP4_MEDIA_TYPE = "video/mp4";
    public const string HLS_MEDIA_TYPE = "application/vnd.apple.mpegurl";

    const string FILENAME_TEMPLATE = "{0}.m3u8";

    public IMediaFileProber Prober { get; }
    public IMediaFileConverter Converter { get; }

    public StreamingFile GetStream(IMediaFile selectedMediaFile)
    {
      if (selectedMediaFile.MediaFileRecord.MediaType == MP4_MEDIA_TYPE)
        return new StreamingFile(File.OpenRead(selectedMediaFile.GetFullFilePath()), MP4_MEDIA_TYPE);
      else
      {
        string filename = string.Format(FILENAME_TEMPLATE, selectedMediaFile.MediaFileRecord.Hash);
        string filePath = filename.ToCacheDirectoryFile();

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

            selectedMediaFile.MediaFileRecord.HasEmbeddedSubtitles = mfpi.ContainsSubtitles;

            Converter.Convert(selectedMediaFile.GetFullFilePath(), targetVideoCodec, targetAudioCodec, filename, mfpi.NumberOfAudioChannels == 6, mfpi.ContainsSubtitles);
            _hashesRunning.Add(selectedMediaFile.MediaFileRecord.Hash);
          }
        }
        return new StreamingFile(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), HLS_MEDIA_TYPE);
      }
    }

    public async Task<string> GetSubtitleFilePath(IMediaFile selectedMediaFile)
    {
      string cachedSubtitlePath = selectedMediaFile.MediaFileRecord.Hash.ToCacheDirectoryFile(".vtt");
      if (File.Exists(cachedSubtitlePath))
        return cachedSubtitlePath;
      else
      {
        string subtitlePath = selectedMediaFile.GetFullSubtitleFilePath();
        if (subtitlePath == null)
        {
          // attempt to get a subtitle file online
          if (await _subtitleProvider.GetSubtitleInformation(selectedMediaFile, cachedSubtitlePath))
            return cachedSubtitlePath;

          return null;
        }

        if (Path.GetExtension(subtitlePath).ToLowerInvariant() == ".vtt")
          return subtitlePath;

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
