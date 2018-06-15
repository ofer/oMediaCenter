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
    const string HLS_MEDIA_TYPE = "application/vnd.apple.mpegurl";

    const string CACHE_DIR = "wwwroot\\cache";

    const string FILENAME_TEMPLATE = "{0}.m3u8";

    public IMediaFileProber Prober { get; }
    public IMediaFileConverter Converter { get; }

    public StreamingFile GetStream(IMediaFile selectedMediaFile)
    {
      if (selectedMediaFile.MediaFileRecord.MediaType == MP4_MEDIA_TYPE)
        return new StreamingFile(selectedMediaFile.GetMediaData(), MP4_MEDIA_TYPE);
      else
      {
        if (!Directory.Exists(CACHE_DIR))
          Directory.CreateDirectory(CACHE_DIR);

        string filename = Path.Combine(CACHE_DIR, string.Format(FILENAME_TEMPLATE, selectedMediaFile.MediaFileRecord.Hash));

        lock (_readFileLock)
        {
          if (!File.Exists(filename) && !_hashesRunning.Contains(selectedMediaFile.MediaFileRecord.Hash))
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

            Converter.Convert(selectedMediaFile.GetFullFilePath(), targetVideoCodec, targetAudioCodec, filename);
            _hashesRunning.Add(selectedMediaFile.MediaFileRecord.Hash);
          }
        }
        return new StreamingFile(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), HLS_MEDIA_TYPE);
      }
    }
  }
}
