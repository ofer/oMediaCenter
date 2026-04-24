using Microsoft.Extensions.Logging;
using Moq;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Model;
using oMediaCenter.Web.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace oMediaCenter.Tests
{
  public class TranscodingJobManagerTests : IDisposable
  {
    private readonly Mock<ILogger<TranscodingJobManager>> _loggerMock;
    private readonly Mock<IMediaFileProber> _proberMock;
    private readonly TranscodingJobManager _manager;
    private readonly string _testOutputBase;

    public TranscodingJobManagerTests()
    {
      _loggerMock = new Mock<ILogger<TranscodingJobManager>>();
      _proberMock = new Mock<IMediaFileProber>();
      _manager = new TranscodingJobManager(_loggerMock.Object, _proberMock.Object);

      // Use a unique temp directory for each test run
      _testOutputBase = Path.Combine(Path.GetTempPath(), "oMediaCenter_tests", Guid.NewGuid().ToString());
      Directory.CreateDirectory(_testOutputBase);
      _manager.TranscodingOutputBasePath = _testOutputBase;

      // Disable idle cleanup timer interference during tests
      _manager.IdleCleanupIntervalMs = int.MaxValue;
    }

    public void Dispose()
    {
      _manager.Dispose();
      try
      {
        if (Directory.Exists(_testOutputBase))
          Directory.Delete(_testOutputBase, recursive: true);
      }
      catch { }
    }

    private MediaFileProbeInformation CreateProbeResult(string videoCodec = "h264", string audioCodec = "aac", int channels = 2)
    {
      return new MediaFileProbeInformation
      {
        VideoCodec = videoCodec,
        AudioCodec = audioCodec,
        NumberOfAudioChannels = channels,
        ContainsSubtitles = false
      };
    }

    /// <summary>
    /// Creates a fake source file that ffmpeg won't be able to process (so the process exits immediately),
    /// but allows us to verify job creation and process lifecycle.
    /// </summary>
    private string CreateFakeSourceFile()
    {
      string path = Path.Combine(_testOutputBase, "fake_source.mkv");
      File.WriteAllText(path, "not a real video file");
      return path;
    }

    // =========================================================
    // StartTranscoding tests
    // =========================================================

    [Fact]
    public void StartTranscoding_CreatesJobAndAddsToActiveJobs()
    {
      // Arrange
      string hash = "test_hash_1";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();

      // Act
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Assert
      Assert.NotNull(job);
      Assert.Equal(hash, job.MediaHash);
      Assert.Equal(hash, job.JobId);
      Assert.Equal(0, job.StartSegmentIndex);
      Assert.NotNull(job.FfmpegProcess);

      // Verify it's in active jobs
      var retrievedJob = _manager.GetJob(hash);
      Assert.Same(job, retrievedJob);
    }

    [Fact]
    public void StartTranscoding_DuplicateHash_StopsExistingFirst()
    {
      // Arrange
      string hash = "duplicate_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();

      // Act — start first job
      var firstJob = _manager.StartTranscoding(hash, sourceFile, 0, probe);
      var firstProcess = firstJob.FfmpegProcess;

      // Act — start second job (should stop the first)
      var secondJob = _manager.StartTranscoding(hash, sourceFile, 5, probe);

      // Assert
      Assert.NotSame(firstJob, secondJob);
      Assert.Equal(5, secondJob.StartSegmentIndex);

      // The active job should be the second one
      var activeJob = _manager.GetJob(hash);
      Assert.Same(secondJob, activeJob);
    }

    // =========================================================
    // StopTranscoding tests
    // =========================================================

    [Fact]
    public void StopTranscoding_KillsProcessAndRemovesJob()
    {
      // Arrange
      string hash = "stop_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Verify job exists
      Assert.NotNull(_manager.GetJob(hash));

      // Act
      _manager.StopTranscoding(hash);

      // Assert — job should be removed
      Assert.Null(_manager.GetJob(hash));
      Assert.False(_manager.HasActiveJob(hash));
    }

    [Fact]
    public void StopTranscoding_NonExistentHash_NoOp()
    {
      // Act & Assert — should not throw
      _manager.StopTranscoding("nonexistent_hash");
    }

    // =========================================================
    // GetJob tests
    // =========================================================

    [Fact]
    public void GetJob_ReturnsNullForUnknownHash()
    {
      // Act
      var job = _manager.GetJob("unknown_hash");

      // Assert
      Assert.Null(job);
    }

    [Fact]
    public void GetJob_ReturnsActiveJob()
    {
      // Arrange
      string hash = "active_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var createdJob = _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Act
      var retrievedJob = _manager.GetJob(hash);

      // Assert
      Assert.NotNull(retrievedJob);
      Assert.Same(createdJob, retrievedJob);
      Assert.Equal(hash, retrievedJob.MediaHash);
    }

    // =========================================================
    // HasActiveJob tests
    // =========================================================

    [Fact]
    public void HasActiveJob_ReturnsFalseForUnknownHash()
    {
      Assert.False(_manager.HasActiveJob("unknown"));
    }

    // =========================================================
    // Idle cleanup tests
    // =========================================================

    [Fact]
    public void IdleCleanup_KillsJobsExceedingTimeout()
    {
      // Arrange
      string hash = "idle_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Simulate the job being idle for longer than the timeout
      job.LastActivityUtc = DateTime.UtcNow.AddMilliseconds(-(_manager.IdleTimeoutMs + 1000));

      // Act — trigger cleanup directly
      _manager.CleanupIdleJobs();

      // Assert — job should have been cleaned up
      Assert.Null(_manager.GetJob(hash));
    }

    // =========================================================
    // GetOrWaitForSegment tests
    // =========================================================

    [Fact]
    public async Task GetOrWaitForSegment_ReturnsImmediately_WhenFileExists()
    {
      // Arrange
      string hash = "segment_exists_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Pre-create the segment file and the next segment file (to indicate completeness)
      string segmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 0);
      string nextSegmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 1);
      Directory.CreateDirectory(job.OutputPath);
      File.WriteAllText(segmentPath, "segment data");
      File.WriteAllText(nextSegmentPath, "next segment data");

      // Act
      var result = await _manager.GetOrWaitForSegment(hash, 0);

      // Assert
      Assert.Equal(segmentPath, result);
    }

    [Fact]
    public async Task GetOrWaitForSegment_Polls_UntilFileAppears()
    {
      // Arrange
      string hash = "poll_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);
      _manager.SegmentPollIntervalMs = 50;
      _manager.SegmentWaitTimeoutMs = 5000;

      string segmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 0);
      string nextSegmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 1);
      Directory.CreateDirectory(job.OutputPath);

      // Create segment files after a short delay on a background thread
      _ = Task.Run(async () =>
      {
        await Task.Delay(300);
        File.WriteAllText(segmentPath, "segment data");
        File.WriteAllText(nextSegmentPath, "next segment data");
      });

      // Act
      var result = await _manager.GetOrWaitForSegment(hash, 0);

      // Assert
      Assert.Equal(segmentPath, result);
    }

    [Fact]
    public async Task GetOrWaitForSegment_ReturnsWhenProcessExited_EvenWithoutNextSegment()
    {
      // Arrange — use a process that exits immediately (our fake source file will cause ffmpeg to error)
      string hash = "exited_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);
      _manager.SegmentPollIntervalMs = 50;
      _manager.SegmentWaitTimeoutMs = 5000;

      // Wait a bit for ffmpeg to exit (it will fail on the fake file)
      await Task.Delay(500);

      // Create just the segment file (no next segment) — process exited should be enough
      Directory.CreateDirectory(job.OutputPath);
      string segmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 0);
      File.WriteAllText(segmentPath, "last segment data");

      // Act
      var result = await _manager.GetOrWaitForSegment(hash, 0);

      // Assert
      Assert.Equal(segmentPath, result);
    }

    [Fact]
    public async Task GetOrWaitForSegment_ThrowsOnCancellation()
    {
      // Arrange
      string hash = "cancel_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      _manager.StartTranscoding(hash, sourceFile, 0, probe);
      _manager.SegmentPollIntervalMs = 50;

      using var cts = new CancellationTokenSource();
      cts.CancelAfter(200); // Cancel after 200ms

      // Act & Assert
      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        _manager.GetOrWaitForSegment(hash, 99, cts.Token));
    }

    [Fact]
    public async Task GetOrWaitForSegment_ThrowsOnTimeout()
    {
      // Arrange
      string hash = "timeout_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      _manager.StartTranscoding(hash, sourceFile, 0, probe);
      _manager.SegmentPollIntervalMs = 50;
      _manager.SegmentWaitTimeoutMs = 300; // Short timeout for test

      // Act & Assert — segment 99 will never appear
      await Assert.ThrowsAsync<TimeoutException>(() =>
        _manager.GetOrWaitForSegment(hash, 99));
    }

    [Fact]
    public async Task GetOrWaitForSegment_ThrowsForUnknownHash()
    {
      // Act & Assert
      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _manager.GetOrWaitForSegment("nonexistent", 0));
    }

    // =========================================================
    // TranscodingJob model tests
    // =========================================================

    [Fact]
    public void TranscodingJob_Constructor_SetsProperties()
    {
      // Act
      var job = new TranscodingJob("hash1", 5, 50_000_000L, "/tmp/test", 6);

      // Assert
      Assert.Equal("hash1", job.JobId);
      Assert.Equal("hash1", job.MediaHash);
      Assert.Equal(5, job.StartSegmentIndex);
      Assert.Equal(5, job.ActiveSegmentIndex);
      Assert.Equal(50_000_000L, job.StartTimeTicks);
      Assert.Equal("/tmp/test", job.OutputPath);
      Assert.Equal(6, job.SegmentLength);
      Assert.True((DateTime.UtcNow - job.LastActivityUtc).TotalSeconds < 5);
    }

    [Fact]
    public void TranscodingJob_HasExited_TrueWhenNoProcess()
    {
      var job = new TranscodingJob("hash", 0, 0, "/tmp/test");
      Assert.True(job.HasExited);
    }

    [Fact]
    public void TranscodingJob_Stop_NoProcess_NoThrow()
    {
      var job = new TranscodingJob("hash", 0, 0, "/tmp/test");
      // Should not throw
      job.Stop();
    }

    // =========================================================
    // GetSegmentPath tests
    // =========================================================

    [Fact]
    public void GetSegmentPath_ReturnsCorrectPath()
    {
      string path = TranscodingJobManager.GetSegmentPath("/tmp/output", 5);
      Assert.Equal(Path.Combine("/tmp/output", "segment5.ts"), path);
    }

    [Fact]
    public void GetSegmentPath_Segment0()
    {
      string path = TranscodingJobManager.GetSegmentPath("/tmp/output", 0);
      Assert.Equal(Path.Combine("/tmp/output", "segment0.ts"), path);
    }

    // =========================================================
    // Activity tracking tests
    // =========================================================

    [Fact]
    public async Task GetOrWaitForSegment_UpdatesLastActivity()
    {
      // Arrange
      string hash = "activity_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Set activity to the past
      var oldActivity = DateTime.UtcNow.AddMinutes(-5);
      job.LastActivityUtc = oldActivity;

      // Pre-create segment files
      Directory.CreateDirectory(job.OutputPath);
      string segmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 0);
      string nextSegmentPath = TranscodingJobManager.GetSegmentPath(job.OutputPath, 1);
      File.WriteAllText(segmentPath, "data");
      File.WriteAllText(nextSegmentPath, "data");

      // Act
      await _manager.GetOrWaitForSegment(hash, 0);

      // Assert — activity should have been updated
      Assert.True(job.LastActivityUtc > oldActivity);
    }

    [Fact]
    public async Task GetOrWaitForSegment_UpdatesActiveSegmentIndex()
    {
      // Arrange
      string hash = "index_hash";
      string sourceFile = CreateFakeSourceFile();
      var probe = CreateProbeResult();
      var job = _manager.StartTranscoding(hash, sourceFile, 0, probe);

      // Pre-create segment files for segment 3
      Directory.CreateDirectory(job.OutputPath);
      File.WriteAllText(TranscodingJobManager.GetSegmentPath(job.OutputPath, 3), "data");
      File.WriteAllText(TranscodingJobManager.GetSegmentPath(job.OutputPath, 4), "data");

      // Act
      await _manager.GetOrWaitForSegment(hash, 3);

      // Assert
      Assert.Equal(3, job.ActiveSegmentIndex);
    }
  }
}
