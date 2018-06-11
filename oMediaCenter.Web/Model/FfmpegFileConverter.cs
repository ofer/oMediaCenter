using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
	public class FfmpegFileConverter : IMediaFileConverter
	{
		private ILogger<FfmpegFileConverter> _logger;
		private Process _ffmpegProcess;

		const string ARGUMENT_MASK = "-i \"{0}\" -vcodec {1} -acodec {2} -f mp4 {3}";

		public FfmpegFileConverter(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<FfmpegFileConverter>();
			_ffmpegProcess = new Process();
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.CreateNoWindow = false;
			psi.UseShellExecute = false;
			psi.RedirectStandardOutput = false;
			psi.FileName = "ffmpeg";
			_ffmpegProcess.StartInfo = psi;
		}


		public void Convert(string sourceFile, string targetVideoCodec, string targetAudioCodec, string targetFile)
		{
			_ffmpegProcess.StartInfo.Arguments = string.Format(ARGUMENT_MASK, sourceFile, targetVideoCodec, targetAudioCodec, targetFile);
			_ffmpegProcess.Start();
            System.Threading.Thread.Sleep(1000);
//			_ffmpegProcess.WaitForExit();
		}
	}
}
