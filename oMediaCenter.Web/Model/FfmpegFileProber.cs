using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace oMediaCenter.Web.Model
{
  public class FfmpegFileProber : IMediaFileProber
  {
    private ILogger<FfmpegFileProber> _logger;

    const string ARGUMENT_MASK = "-show_streams -show_format \"{0}\" -of xml";

    public FfmpegFileProber(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<FfmpegFileProber>();
    }

    public MediaFileProbeInformation GetProbeInfo(string filename)
    {
      var psi = new ProcessStartInfo
      {
        FileName = "ffprobe",
        Arguments = string.Format(ARGUMENT_MASK, filename),
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true
      };

      _logger.LogInformation("About to execute ffprobe with parameters: {0}", psi.Arguments);

      using var process = new Process { StartInfo = psi };
      process.Start();
      string output = process.StandardOutput.ReadToEnd();
      process.WaitForExit(5000);

      if (!process.HasExited)
        process.Kill();

      XmlSerializer serializer = new XmlSerializer(typeof(ffprobeType));
      ffprobeType ffprobeType = (ffprobeType)serializer.Deserialize(new StringReader(output));

      var videoStream = ffprobeType.streams.FirstOrDefault(s => s.codec_type == "video");
      var audioStream = ffprobeType.streams.FirstOrDefault(s => s.codec_type == "audio");

      MediaFileProbeInformation result = new MediaFileProbeInformation();
      result.VideoCodec = videoStream?.codec_name;
      result.AudioCodec = audioStream?.codec_name;

      // only deals with single subtitle track, need to deal with more later
      result.ContainsSubtitles = ffprobeType.streams.FirstOrDefault(s => s.codec_type == "subtitle") != null;
      result.NumberOfAudioChannels = audioStream?.channels ?? 0;

      // Extract video resolution
      if (videoStream != null)
      {
        result.Width = videoStream.widthSpecified ? videoStream.width : 0;
        result.Height = videoStream.heightSpecified ? videoStream.height : 0;
        result.VideoBitRate = videoStream.bit_rateSpecified ? videoStream.bit_rate : 0;
        result.VideoProfile = videoStream.profile;
        result.VideoLevel = videoStream.levelSpecified ? videoStream.level : 0;
      }

      // Extract audio bitrate
      if (audioStream != null)
      {
        result.AudioBitRate = audioStream.bit_rateSpecified ? audioStream.bit_rate : 0;
      }

      // Extract duration — prefer format-level duration, fall back to video stream duration
      if (ffprobeType.format != null && ffprobeType.format.durationSpecified)
      {
        result.DurationSeconds = ffprobeType.format.duration;
      }
      else if (videoStream != null && videoStream.durationSpecified)
      {
        result.DurationSeconds = videoStream.duration;
      }

      // If video bitrate not available from stream, estimate from format-level bitrate minus audio
      if (result.VideoBitRate == 0 && ffprobeType.format != null && ffprobeType.format.bit_rateSpecified)
      {
        result.VideoBitRate = ffprobeType.format.bit_rate - result.AudioBitRate;
        if (result.VideoBitRate < 0)
          result.VideoBitRate = ffprobeType.format.bit_rate;
      }

      return result;
    }
  }
}
