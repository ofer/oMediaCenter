using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
  public class FfmpegFileConverter : IMediaFileConverter
  {
    private ILogger<FfmpegFileConverter> _logger;
    private ProcessStartInfo _ffmpegProcessInfo;

    private object _ffmpegStartLock = new object();

    const string MP4_ARGUMENT_MASK = "-i \"{0}\" -vcodec {1} -acodec {2} -f mp4 {3}";
    const string HLS_ARGUMENT_MASK = "-i \"{0}\" -bsf:v h264_mp4toannexb -vcodec {1} -acodec {2} -master_pl_name {4} -hls_list_size 0 -hls_base_url {5} -hls_time 10 -f hls {3}.video.m3u8";
    const string SUBTITLES_ARGUMENT_MASK = "-map 0:v -map 0:a:0 -map 0:s:0 -var_stream_map \"v:0,a:0,s:0,sgroup:subtitle\"";
    const string SIX_CHANNEL_DOWNSAMPLE_FILTER = "-af \"pan=stereo|FL< 1.0*FL + 0.707*FC + 0.707*BL|FR< 1.0*FR + 0.707*FC + 0.707*BR\"";
    const string SUBTITLE_CONVERSION_MASK = "-i \"{0}\" \"{1}\"";

    public FfmpegFileConverter(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<FfmpegFileConverter>();
      _ffmpegProcessInfo = new ProcessStartInfo();
      _ffmpegProcessInfo.CreateNoWindow = false;
      _ffmpegProcessInfo.UseShellExecute = false;
      _ffmpegProcessInfo.RedirectStandardOutput = false;
      _ffmpegProcessInfo.FileName = "ffmpeg";
    }


    public void Convert(string sourceFile, string targetVideoCodec, string targetAudioCodec, string targetFile, bool forceStereo, bool hasSubtitles)
    {
      if (forceStereo)
        targetAudioCodec = targetAudioCodec + " " + SIX_CHANNEL_DOWNSAMPLE_FILTER;
      if (hasSubtitles)
        targetAudioCodec = targetAudioCodec + " " + SUBTITLES_ARGUMENT_MASK;

      lock (_ffmpegStartLock)
      {
        _ffmpegProcessInfo.Arguments = string.Format(HLS_ARGUMENT_MASK, sourceFile, targetVideoCodec, targetAudioCodec, targetFile.ToCacheDirectoryFile(), targetFile, "/cache/");
        Process.Start(_ffmpegProcessInfo);
      }
      System.Threading.Thread.Sleep(1000);
      //			_ffmpegProcess.WaitForExit();
    }

    public async Task ConvertSubtitles(string sourceFile, string targetFile)
    {
      string subtitleType = Path.GetExtension(sourceFile).ToLower().Substring(1);

      if (subtitleType == "vtt")
        return;

      _ffmpegProcessInfo.Arguments = string.Format(SUBTITLE_CONVERSION_MASK, sourceFile, targetFile);
      await Task.Run(() =>
      {
        var proc = Process.Start(_ffmpegProcessInfo);
        proc.WaitForExit();
      });
    }
  }
}
