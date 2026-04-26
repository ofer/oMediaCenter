using System;
using System.Diagnostics;
using System.Threading;

namespace oMediaCenter.Web.Model
{
  /// <summary>
  /// Represents an active ffmpeg transcoding job for a specific media file.
  /// Tracks the ffmpeg process, current segment position, and activity for idle cleanup.
  /// </summary>
  public class TranscodingJob : IDisposable
  {
    /// <summary>Unique identifier for this job (typically the media hash).</summary>
    public string JobId { get; }

    /// <summary>Hash of the media file being transcoded.</summary>
    public string MediaHash { get; }

    /// <summary>The ffmpeg process performing the transcode.</summary>
    public Process FfmpegProcess { get; set; }

    /// <summary>The segment index ffmpeg is currently writing (or last completed).</summary>
    public int ActiveSegmentIndex { get; set; }

    /// <summary>The segment index from which this job started transcoding.</summary>
    public int StartSegmentIndex { get; }

    /// <summary>Start time in ticks for the ffmpeg seek position.</summary>
    public long StartTimeTicks { get; }

    /// <summary>Directory where segment files are written.</summary>
    public string OutputPath { get; }

    /// <summary>HLS segment length in seconds.</summary>
    public int SegmentLength { get; }

    /// <summary>UTC timestamp of the last client activity (segment request, ping, etc.).</summary>
    public DateTime LastActivityUtc { get; set; }

    /// <summary>Semaphore for coordinating access to job state during transitions.</summary>
    public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);

    /// <summary>Whether the ffmpeg process has exited.</summary>
    public bool HasExited => FfmpegProcess == null || FfmpegProcess.HasExited;

    public TranscodingJob(string mediaHash, int startSegmentIndex, long startTimeTicks, string outputPath, int segmentLength = 10)
    {
      JobId = mediaHash;
      MediaHash = mediaHash;
      StartSegmentIndex = startSegmentIndex;
      ActiveSegmentIndex = startSegmentIndex;
      StartTimeTicks = startTimeTicks;
      OutputPath = outputPath;
      SegmentLength = segmentLength;
      LastActivityUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Stops the ffmpeg process gracefully by sending "q" to stdin, 
    /// falling back to Kill if it doesn't exit within the timeout.
    /// </summary>
    public void Stop(int gracefulTimeoutMs = 5000)
    {
      if (FfmpegProcess == null || FfmpegProcess.HasExited)
        return;

      try
      {
        // Try graceful stop via stdin "q" command
        if (FfmpegProcess.StartInfo.RedirectStandardInput)
        {
          FfmpegProcess.StandardInput.WriteLine("q");
          if (FfmpegProcess.WaitForExit(gracefulTimeoutMs))
            return;
        }

        // Graceful stop failed or stdin not redirected — force kill
        FfmpegProcess.Kill(entireProcessTree: true);
        FfmpegProcess.WaitForExit(2000);
      }
      catch (InvalidOperationException)
      {
        // Process already exited
      }
    }

    public void Dispose()
    {
      Stop();
      Lock?.Dispose();
      FfmpegProcess?.Dispose();
    }
  }
}
