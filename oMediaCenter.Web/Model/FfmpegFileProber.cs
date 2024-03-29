using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private Process _ffprobeProcess;

    const string ARGUMENT_MASK = "-show_streams \"{0}\" -of xml";

    public FfmpegFileProber(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<FfmpegFileProber>();
      _ffprobeProcess = new Process();
      ProcessStartInfo psi = new ProcessStartInfo();
      psi.CreateNoWindow = true;
      psi.UseShellExecute = false;
      psi.RedirectStandardOutput = true;
      psi.FileName = "ffprobe";
      _ffprobeProcess.StartInfo = psi;
    }

    public MediaFileProbeInformation GetProbeInfo(string filename)
    {
      _ffprobeProcess.StartInfo.Arguments = string.Format(ARGUMENT_MASK, filename);
      _logger.LogInformation("About to execute ffprobe with parameters: {0}", _ffprobeProcess.StartInfo.Arguments);
      _ffprobeProcess.Start();
      string output = _ffprobeProcess.StandardOutput.ReadToEnd();

      _ffprobeProcess.WaitForExit(1000);

      if (!_ffprobeProcess.HasExited)
        _ffprobeProcess.Kill();


      XmlSerializer serializer = new XmlSerializer(typeof(ffprobeType));
      ffprobeType ffprobeType = (ffprobeType)serializer.Deserialize(new StringReader(output));
      MediaFileProbeInformation result = new MediaFileProbeInformation();
      result.VideoCodec = ffprobeType.streams.FirstOrDefault(s => s.codec_type == "video").codec_name;
      var audioCodec = ffprobeType.streams.FirstOrDefault(s => s.codec_type == "audio");
      result.AudioCodec = audioCodec.codec_name;
      // only deals with single subtitle track, need to deal with more later
      result.ContainsSubtitles = ffprobeType.streams.FirstOrDefault(s => s.codec_type == "subtitle") != null;
      result.NumberOfAudioChannels = audioCodec.channels;

      return result;
    }
  }
}
