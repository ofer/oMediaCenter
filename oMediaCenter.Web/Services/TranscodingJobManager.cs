using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Model;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Services
{
  /// <summary>
  /// Centralized manager for ffmpeg transcoding processes.
  /// Maintains a dictionary of active jobs keyed by media hash, handles starting/stopping
  /// ffmpeg, polling for segment readiness, and automatic cleanup of idle jobs.
  /// </summary>
  public class TranscodingJobManager : ITranscodingJobManager, IDisposable
  {
    private readonly ConcurrentDictionary<string, TranscodingJob> _activeJobs = new();
    private readonly ConcurrentDictionary<string, MediaFileProbeInformation> _probeCache = new();
    private readonly ILogger<TranscodingJobManager> _logger;
    private readonly Timer _idleCleanupTimer;
    private readonly IMediaFileProber _prober;

    /// <summary>Timeout in milliseconds after which idle jobs are killed (default 60s).</summary>
    public int IdleTimeoutMs { get; set; } = 60_000;

    /// <summary>Polling interval in milliseconds when waiting for a segment file (default 100ms).</summary>
    public int SegmentPollIntervalMs { get; set; } = 100;

    /// <summary>Maximum time in milliseconds to wait for a segment before timing out (default 30s).</summary>
    public int SegmentWaitTimeoutMs { get; set; } = 30_000;

    /// <summary>Interval in milliseconds for the idle cleanup timer (default 10s).</summary>
    public int IdleCleanupIntervalMs { get; set; } = 10_000;

    /// <summary>Default HLS segment length in seconds. Must match HlsPlaylistGenerator.DefaultSegmentLength.</summary>
    public int DefaultSegmentLength { get; set; } = 6;

    /// <summary>Base directory for transcoding output. Each job gets a subdirectory.</summary>
    public string TranscodingOutputBasePath { get; set; }

    public TranscodingJobManager(ILogger<TranscodingJobManager> logger, IMediaFileProber prober)
    {
      _logger = logger;
      _prober = prober;
      TranscodingOutputBasePath = Path.Combine(Path.GetTempPath(), "oMediaCenter", "hls");

      _idleCleanupTimer = new Timer(CleanupIdleJobs, null, IdleCleanupIntervalMs, IdleCleanupIntervalMs);
    }

    /// <summary>
    /// Returns the active transcoding job for the given media hash, or null if none exists.
    /// </summary>
    public TranscodingJob GetJob(string hash)
    {
      _activeJobs.TryGetValue(hash, out var job);
      return job;
    }

    /// <summary>
    /// Returns cached probe results for the given hash, or probes the file and caches the result.
    /// </summary>
    public MediaFileProbeInformation GetOrCacheProbeResult(string hash, string sourceFile)
    {
      return _probeCache.GetOrAdd(hash, _ => _prober.GetProbeInfo(sourceFile));
    }

    /// <summary>
    /// Returns true if there is an active (non-exited) transcoding job for the given hash.
    /// </summary>
    public bool HasActiveJob(string hash)
    {
      return _activeJobs.TryGetValue(hash, out var job) && !job.HasExited;
    }

    /// <summary>
    /// Starts a new ffmpeg transcoding job for the given media file.
    /// If a job already exists for this hash, it is stopped first.
    /// </summary>
    /// <param name="hash">Media file hash (used as job key).</param>
    /// <param name="sourceFile">Full path to the source media file.</param>
    /// <param name="startSegment">Segment index to start transcoding from.</param>
    /// <param name="probeResult">Probe information for codec decisions.</param>
    /// <returns>The newly created TranscodingJob.</returns>
    public TranscodingJob StartTranscoding(string hash, string sourceFile, int startSegment, MediaFileProbeInformation probeResult)
    {
      // Stop any existing job for this hash
      StopTranscoding(hash);

      string outputPath = Path.Combine(TranscodingOutputBasePath, hash);
      Directory.CreateDirectory(outputPath);

      long startTimeTicks = (long)startSegment * DefaultSegmentLength * TimeSpan.TicksPerSecond;
      double startTimeSeconds = (double)startTimeTicks / TimeSpan.TicksPerSecond;

      var job = new TranscodingJob(hash, startSegment, startTimeTicks, outputPath, DefaultSegmentLength);

      // Determine codecs
      string videoCodec = probeResult.VideoCodec == "h264" ? "copy" : "libx264";
      string audioCodec = probeResult.AudioCodec == "aac" ? "copy" : "aac";

      string audioArgs = audioCodec;
      if (probeResult.NumberOfAudioChannels == 6)
        audioArgs += " -af \"pan=stereo|FL< 1.0*FL + 0.707*FC + 0.707*BL|FR< 1.0*FR + 0.707*FC + 0.707*BR\"";

      string segmentFilenamePattern = Path.Combine(outputPath, "segment%d.ts");
      string playlistPath = Path.Combine(outputPath, "playlist.m3u8");

      // Build ffmpeg arguments for on-demand HLS generation
      string seekArg = startTimeSeconds > 0 ? $"-ss {startTimeSeconds:F3}" : "";
      string forceKeyframeArg = videoCodec != "copy"
        ? $"-force_key_frames \"expr:gte(t,n_forced*{DefaultSegmentLength})\""
        : "";

      string arguments = string.Join(" ",
        seekArg,
        $"-i \"{sourceFile}\"",
        $"-vcodec {videoCodec}",
        $"-acodec {audioArgs}",
        forceKeyframeArg,
        "-copyts",
        "-avoid_negative_ts disabled",
        $"-start_number {startSegment}",
        $"-hls_time {DefaultSegmentLength}",
        "-hls_list_size 0",
        $"-hls_segment_filename \"{segmentFilenamePattern}\"",
        "-f hls",
        $"\"{playlistPath}\""
      ).Trim();

      // Remove double spaces from optional args
      while (arguments.Contains("  "))
        arguments = arguments.Replace("  ", " ");

      var processStartInfo = new ProcessStartInfo
      {
        FileName = "ffmpeg",
        Arguments = arguments,
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = false,
        RedirectStandardError = false
      };

      _logger.LogInformation("Starting ffmpeg for hash {Hash} from segment {Segment}: ffmpeg {Args}", hash, startSegment, arguments);

      var process = Process.Start(processStartInfo);
      job.FfmpegProcess = process;

      _activeJobs[hash] = job;

      return job;
    }

    /// <summary>
    /// Stops the transcoding job for the given hash, killing the ffmpeg process
    /// and removing the job from the active dictionary.
    /// </summary>
    public void StopTranscoding(string hash)
    {
      if (_activeJobs.TryRemove(hash, out var job))
      {
        _logger.LogInformation("Stopping transcoding job for hash {Hash}", hash);
        job.Stop();
        job.Dispose();
      }
    }

    /// <summary>
    /// Waits for a specific segment file to become ready (i.e., fully written).
    /// A segment is considered ready when its file exists AND the next segment file also exists
    /// (or the ffmpeg process has exited, meaning the current segment is the last one).
    /// </summary>
    /// <param name="hash">Media file hash.</param>
    /// <param name="segmentIndex">The segment index to wait for.</param>
    /// <param name="cancellationToken">Cancellation token for client disconnects.</param>
    /// <returns>Full path to the ready segment file.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    /// <exception cref="TimeoutException">Thrown when the segment doesn't appear within the timeout.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active job for the hash.</exception>
    public async Task<string> GetOrWaitForSegment(string hash, int segmentIndex, CancellationToken cancellationToken = default)
    {
      if (!_activeJobs.TryGetValue(hash, out var job))
        throw new InvalidOperationException($"No active transcoding job for hash '{hash}'.");

      // Update activity timestamp
      job.LastActivityUtc = DateTime.UtcNow;

      string segmentPath = GetSegmentPath(job.OutputPath, segmentIndex);
      string nextSegmentPath = GetSegmentPath(job.OutputPath, segmentIndex + 1);

      var timeoutCts = new CancellationTokenSource(SegmentWaitTimeoutMs);
      using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

      while (true)
      {
        linkedCts.Token.ThrowIfCancellationRequested();

        if (File.Exists(segmentPath))
        {
          // Segment exists — check if it's fully written
          bool nextExists = File.Exists(nextSegmentPath);
          bool processExited = job.HasExited;

          if (nextExists || processExited)
          {
            // Update active segment index tracking
            if (segmentIndex > job.ActiveSegmentIndex)
              job.ActiveSegmentIndex = segmentIndex;

            return segmentPath;
          }
        }

        try
        {
          await Task.Delay(SegmentPollIntervalMs, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
          if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException("Client disconnected.", cancellationToken);

          throw new TimeoutException($"Timed out waiting for segment {segmentIndex} of hash '{hash}' after {SegmentWaitTimeoutMs}ms.");
        }
      }
    }

    /// <summary>
    /// Returns the file path for a segment file given the output directory and segment index.
    /// </summary>
    public static string GetSegmentPath(string outputPath, int segmentIndex)
    {
      return Path.Combine(outputPath, $"segment{segmentIndex}.ts");
    }

    /// <summary>
    /// Checks for and kills idle transcoding jobs.
    /// A job is considered idle if LastActivityUtc is older than IdleTimeoutMs.
    /// Also used as the timer callback.
    /// </summary>
    public void CleanupIdleJobs(object state = null)
    {
      var now = DateTime.UtcNow;
      foreach (var kvp in _activeJobs)
      {
        var job = kvp.Value;
        var idleTime = (now - job.LastActivityUtc).TotalMilliseconds;

        if (idleTime > IdleTimeoutMs)
        {
          _logger.LogInformation("Killing idle transcoding job for hash {Hash} (idle for {IdleMs}ms)", kvp.Key, idleTime);
          StopTranscoding(kvp.Key);

          // Clean up output directory
          try
          {
            if (Directory.Exists(job.OutputPath))
              Directory.Delete(job.OutputPath, recursive: true);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to clean up output directory {Path} for idle job {Hash}", job.OutputPath, kvp.Key);
          }
        }
      }
    }

    /// <summary>
    /// Cleans up all active jobs and disposes resources.
    /// </summary>
    public void Dispose()
    {
      _idleCleanupTimer?.Dispose();

      foreach (var kvp in _activeJobs)
      {
        try
        {
          kvp.Value.Stop();
          kvp.Value.Dispose();

          if (Directory.Exists(kvp.Value.OutputPath))
            Directory.Delete(kvp.Value.OutputPath, recursive: true);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Error disposing job {Hash}", kvp.Key);
        }
      }

      _activeJobs.Clear();
    }
  }
}
