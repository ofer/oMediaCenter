using oMediaCenter.Interfaces;
using oMediaCenter.Web.Services;
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
    private readonly ITranscodingJobManager _jobManager;
    private readonly ConcurrentDictionary<string, bool> _subtitleConversionsRunning = new();
    private ISubtitleProvider _subtitleProvider;
    object _readFileLock;

    public MediaFileStreamer(IMediaFileProber fileProber, IMediaFileConverter mediaFileConverter, ISubtitleProvider subtitleProvider, ITranscodingJobManager jobManager)
    {
      Prober = fileProber;
      Converter = mediaFileConverter;
      _subtitleProvider = subtitleProvider;
      _jobManager = jobManager;

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
        string hash = selectedMediaFile.MediaFileRecord.Hash;
        string filename = string.Format(FILENAME_TEMPLATE, hash);
        string filePath = filename.ToCacheDirectoryFile();

        lock (_readFileLock)
        {
          if (!File.Exists(filePath) && !_jobManager.HasActiveJob(hash))
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

            // Use the TranscodingJobManager to start the transcoding job
            _jobManager.StartTranscoding(hash, selectedMediaFile.GetFullFilePath(), 0, mfpi);

            // Also run the legacy converter for backward compatibility during transition
            Converter.Convert(selectedMediaFile.GetFullFilePath(), targetVideoCodec, targetAudioCodec, filename, mfpi.NumberOfAudioChannels == 6, mfpi.ContainsSubtitles);
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

      string subtitlePath = selectedMediaFile.GetFullSubtitleFilePath();
      if (subtitlePath != null)
      {
        if (Path.GetExtension(subtitlePath).ToLowerInvariant() == ".vtt")
          return subtitlePath;

        return await ConvertSubtitleFile(subtitlePath, cachedSubtitlePath);
      }

      MediaFileProbeInformation probeInfo = Prober.GetProbeInfo(selectedMediaFile.GetFullFilePath());
      selectedMediaFile.MediaFileRecord.HasEmbeddedSubtitles = probeInfo.ContainsSubtitles;
      if (probeInfo.ContainsSubtitles)
        return await ConvertSubtitleFile(selectedMediaFile.GetFullFilePath(), cachedSubtitlePath);

      // attempt to get a subtitle file online
      if (_subtitleProvider != null && await _subtitleProvider.GetSubtitleInformation(selectedMediaFile, cachedSubtitlePath) && File.Exists(cachedSubtitlePath))
        return cachedSubtitlePath;

      return null;
    }

    private async Task<string> ConvertSubtitleFile(string sourcePath, string cachedSubtitlePath)
    {
      string subtitleKey = cachedSubtitlePath;
      if (_subtitleConversionsRunning.TryAdd(subtitleKey, true))
      {
        try
        {
          await Converter.ConvertSubtitles(sourcePath, cachedSubtitlePath);
        }
        finally
        {
          _subtitleConversionsRunning.TryRemove(subtitleKey, out _);
        }
      }
      else
      {
        while (_subtitleConversionsRunning.ContainsKey(subtitleKey))
          await Task.Delay(100);
      }

      if (File.Exists(cachedSubtitlePath))
        return cachedSubtitlePath;

      return null;
    }
  }
}
