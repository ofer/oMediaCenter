using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Controllers;
using oMediaCenter.Web.Model;
using oMediaCenter.Web.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace oMediaCenter.Tests
{
  public class OnDemandSegmentTests : IDisposable
  {
    private readonly Mock<IFileReader> _fileReaderMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ISubtitleProvider> _subtitleProviderMock;
    private readonly Mock<IMediaFileStreamer> _mediaFileStreamerMock;
    private readonly Mock<IMediaFileProber> _proberMock;
    private readonly HlsPlaylistGenerator _playlistGenerator;
    private readonly Mock<ITranscodingJobManager> _jobManagerMock;
    private readonly string _testOutputDir;

    public OnDemandSegmentTests()
    {
      _fileReaderMock = new Mock<IFileReader>();
      _loggerFactoryMock = new Mock<ILoggerFactory>();
      _loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
      _subtitleProviderMock = new Mock<ISubtitleProvider>();
      _mediaFileStreamerMock = new Mock<IMediaFileStreamer>();
      _proberMock = new Mock<IMediaFileProber>();
      _playlistGenerator = new HlsPlaylistGenerator();
      _jobManagerMock = new Mock<ITranscodingJobManager>();

      _testOutputDir = Path.Combine(Path.GetTempPath(), "oMediaCenter_segment_tests", Guid.NewGuid().ToString());
      Directory.CreateDirectory(_testOutputDir);
      _activeProcesses = new List<Process>();
    }

    private readonly List<Process> _activeProcesses;

    public void Dispose()
    {
      foreach (var proc in _activeProcesses)
      {
        try { proc.Kill(); proc.Dispose(); } catch { }
      }
      try
      {
        if (Directory.Exists(_testOutputDir))
          Directory.Delete(_testOutputDir, recursive: true);
      }
      catch { }
    }

    private MediaListController CreateController()
    {
      // Pass null for MediaCenterContext — GetHlsSegment doesn't use it.
      return new MediaListController(
        _fileReaderMock.Object,
        _loggerFactoryMock.Object,
        _subtitleProviderMock.Object,
        null,
        _mediaFileStreamerMock.Object,
        _proberMock.Object,
        _playlistGenerator,
        _jobManagerMock.Object
      );
    }

    private MediaFileProbeInformation CreateProbeResult()
    {
      return new MediaFileProbeInformation
      {
        VideoCodec = "h264",
        AudioCodec = "aac",
        NumberOfAudioChannels = 2,
        ContainsSubtitles = false,
        Width = 1920,
        Height = 1080,
        VideoBitRate = 5_000_000,
        AudioBitRate = 128_000,
        DurationSeconds = 120.0,
        VideoProfile = "High",
        VideoLevel = 41
      };
    }

    private Mock<IMediaFile> SetupMediaFile(string hash, string filePath)
    {
      var mediaFileMock = new Mock<IMediaFile>();
      mediaFileMock.Setup(mf => mf.GetFullFilePath()).Returns(filePath);
      mediaFileMock.Setup(mf => mf.MediaFileRecord).Returns(new MediaFileRecord { Hash = hash, MediaType = "video/x-matroska" });
      _fileReaderMock.Setup(fr => fr.GetByHash(hash)).Returns(mediaFileMock.Object);
      return mediaFileMock;
    }

    /// <summary>
    /// Creates a TranscodingJob with a real long-running process so that HasExited returns false.
    /// The process is tracked for cleanup in Dispose().
    /// </summary>
    private TranscodingJob CreateActiveJob(string hash, string outputDir, int activeSegmentIndex, int startSegment = 0, int segmentLength = 6)
    {
      var process = Process.Start(new ProcessStartInfo
      {
        FileName = "sleep",
        Arguments = "300",
        CreateNoWindow = true,
        UseShellExecute = false
      });
      _activeProcesses.Add(process);

      var job = new TranscodingJob(hash, startSegment, 0, outputDir, segmentLength);
      job.FfmpegProcess = process;
      job.ActiveSegmentIndex = activeSegmentIndex;
      return job;
    }

    // =========================================================
    // Test 1: Cached segment returned immediately without starting ffmpeg
    // =========================================================

    [Fact]
    public async Task SegmentRequest_CachedSegment_ReturnsImmediately()
    {
      // Arrange
      string hash = "cached_hash";
      int segmentIndex = 5;
      string outputDir = Path.Combine(_testOutputDir, hash);
      Directory.CreateDirectory(outputDir);

      // Pre-create segment 5 and segment 6 (next segment proves current is complete)
      string segmentPath = TranscodingJobManager.GetSegmentPath(outputDir, segmentIndex);
      string nextSegmentPath = TranscodingJobManager.GetSegmentPath(outputDir, segmentIndex + 1);
      File.WriteAllText(segmentPath, "segment data 5");
      File.WriteAllText(nextSegmentPath, "segment data 6");

      SetupMediaFile(hash, "/fake/source.mkv");

      // Set up job manager to return an active job at segment 3 (behind our requested segment)
      var job = CreateActiveJob(hash, outputDir, activeSegmentIndex: 3);
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns(job);

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, segmentIndex, 0, 0, CancellationToken.None);

      // Assert — should return PhysicalFileResult (served from cache)
      Assert.IsType<PhysicalFileResult>(result);
      var physicalFileResult = (PhysicalFileResult)result;
      Assert.Equal(segmentPath, physicalFileResult.FileName);
      Assert.Equal("video/mp2t", physicalFileResult.ContentType);

      // StartTranscoding should NOT have been called
      _jobManagerMock.Verify(jm => jm.StartTranscoding(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<MediaFileProbeInformation>()), Times.Never);
      // GetOrWaitForSegment should NOT have been called
      _jobManagerMock.Verify(jm => jm.GetOrWaitForSegment(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================
    // Test 2: No active job — starts transcoding from requested position
    // =========================================================

    [Fact]
    public async Task SegmentRequest_NoActiveJob_StartsTranscodingFromRequestedPosition()
    {
      // Arrange
      string hash = "no_job_hash";
      int segmentIndex = 7;

      SetupMediaFile(hash, "/fake/source.mkv");

      var probeResult = CreateProbeResult();
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns((TranscodingJob)null);
      _jobManagerMock.Setup(jm => jm.GetOrCacheProbeResult(hash, "/fake/source.mkv")).Returns(probeResult);
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, segmentIndex, It.IsAny<CancellationToken>()))
        .ReturnsAsync("/tmp/fake/segment7.ts");

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, segmentIndex, 0, 0, CancellationToken.None);

      // Assert — StartTranscoding should be called with the correct startSegment
      _jobManagerMock.Verify(jm => jm.StartTranscoding(hash, "/fake/source.mkv", segmentIndex, probeResult), Times.Once);
      _jobManagerMock.Verify(jm => jm.GetOrWaitForSegment(hash, segmentIndex, It.IsAny<CancellationToken>()), Times.Once);

      // Result should be PhysicalFileResult
      Assert.IsType<PhysicalFileResult>(result);
    }

    // =========================================================
    // Test 3: Active job ahead — kills and restarts from backward seek position
    // =========================================================

    [Fact]
    public async Task SegmentRequest_ActiveJobAhead_KillsAndRestarts()
    {
      // Arrange: job is at segment 50, we request segment 10
      string hash = "ahead_hash";
      int requestedSegment = 10;
      string outputDir = Path.Combine(_testOutputDir, hash);
      Directory.CreateDirectory(outputDir);

      SetupMediaFile(hash, "/fake/source.mkv");

      var job = CreateActiveJob(hash, outputDir, activeSegmentIndex: 50, startSegment: 40);
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns(job);

      var probeResult = CreateProbeResult();
      _jobManagerMock.Setup(jm => jm.GetOrCacheProbeResult(hash, "/fake/source.mkv")).Returns(probeResult);
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, requestedSegment, It.IsAny<CancellationToken>()))
        .ReturnsAsync("/tmp/fake/segment10.ts");

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, requestedSegment, 0, 0, CancellationToken.None);

      // Assert — should restart transcoding at segment 10
      // Since segment 10 < active segment 50, and the file doesn't exist on disk, it triggers a restart
      _jobManagerMock.Verify(jm => jm.StartTranscoding(hash, "/fake/source.mkv", requestedSegment, probeResult), Times.Once);
      _jobManagerMock.Verify(jm => jm.GetOrWaitForSegment(hash, requestedSegment, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================
    // Test 4: Active job behind but within range — waits for segment
    // =========================================================

    [Fact]
    public async Task SegmentRequest_ActiveJobBehindWithinRange_WaitsForSegment()
    {
      // Arrange: job is at segment 5, we request segment 8 (gap = 3, well within 24)
      string hash = "behind_hash";
      int requestedSegment = 8;
      string outputDir = Path.Combine(_testOutputDir, hash);
      Directory.CreateDirectory(outputDir);

      SetupMediaFile(hash, "/fake/source.mkv");

      var job = CreateActiveJob(hash, outputDir, activeSegmentIndex: 5);
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns(job);
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, requestedSegment, It.IsAny<CancellationToken>()))
        .ReturnsAsync("/tmp/fake/segment8.ts");

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, requestedSegment, 0, 0, CancellationToken.None);

      // Assert — should NOT restart transcoding, only wait
      _jobManagerMock.Verify(jm => jm.StartTranscoding(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<MediaFileProbeInformation>()), Times.Never);
      _jobManagerMock.Verify(jm => jm.GetOrWaitForSegment(hash, requestedSegment, It.IsAny<CancellationToken>()), Times.Once);

      Assert.IsType<PhysicalFileResult>(result);
    }

    // =========================================================
    // Test 5: Active job too far behind — kills and restarts
    // =========================================================

    [Fact]
    public async Task SegmentRequest_ActiveJobTooFarBehind_KillsAndRestarts()
    {
      // Arrange: job is at segment 5, we request segment 50 (gap = 45, > 24 threshold)
      string hash = "far_behind_hash";
      int requestedSegment = 50;
      string outputDir = Path.Combine(_testOutputDir, hash);
      Directory.CreateDirectory(outputDir);

      SetupMediaFile(hash, "/fake/source.mkv");

      var job = CreateActiveJob(hash, outputDir, activeSegmentIndex: 5);
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns(job);

      var probeResult = CreateProbeResult();
      _jobManagerMock.Setup(jm => jm.GetOrCacheProbeResult(hash, "/fake/source.mkv")).Returns(probeResult);
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, requestedSegment, It.IsAny<CancellationToken>()))
        .ReturnsAsync("/tmp/fake/segment50.ts");

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, requestedSegment, 0, 0, CancellationToken.None);

      // Assert — should restart transcoding at the requested position
      _jobManagerMock.Verify(jm => jm.StartTranscoding(hash, "/fake/source.mkv", requestedSegment, probeResult), Times.Once);
      _jobManagerMock.Verify(jm => jm.GetOrWaitForSegment(hash, requestedSegment, It.IsAny<CancellationToken>()), Times.Once);

      Assert.IsType<PhysicalFileResult>(result);
    }

    // =========================================================
    // Test 6: Unknown hash returns 404
    // =========================================================

    [Fact]
    public async Task SegmentRequest_UnknownHash_ReturnsNotFound()
    {
      // Arrange — file reader returns null
      _fileReaderMock.Setup(fr => fr.GetByHash("nonexistent")).Returns((IMediaFile)null);

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment("nonexistent", 0, 0, 0, CancellationToken.None);

      // Assert
      Assert.IsType<NotFoundResult>(result);
    }

    // =========================================================
    // Test 7: Timeout returns 504
    // =========================================================

    [Fact]
    public async Task SegmentRequest_Timeout_Returns504()
    {
      // Arrange
      string hash = "timeout_hash";
      int segmentIndex = 0;

      SetupMediaFile(hash, "/fake/source.mkv");

      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns((TranscodingJob)null);
      _jobManagerMock.Setup(jm => jm.GetOrCacheProbeResult(hash, "/fake/source.mkv")).Returns(CreateProbeResult());
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, segmentIndex, It.IsAny<CancellationToken>()))
        .ThrowsAsync(new TimeoutException("Timed out"));

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, segmentIndex, 0, 0, CancellationToken.None);

      // Assert — 504 Gateway Timeout
      var objectResult = Assert.IsType<ObjectResult>(result);
      Assert.Equal(504, objectResult.StatusCode);
    }

    // =========================================================
    // Test 8: Client disconnect returns 499
    // =========================================================

    [Fact]
    public async Task SegmentRequest_ClientDisconnect_Returns499()
    {
      // Arrange
      string hash = "disconnect_hash";
      int segmentIndex = 0;

      SetupMediaFile(hash, "/fake/source.mkv");

      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns((TranscodingJob)null);
      _jobManagerMock.Setup(jm => jm.GetOrCacheProbeResult(hash, "/fake/source.mkv")).Returns(CreateProbeResult());
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, segmentIndex, It.IsAny<CancellationToken>()))
        .ThrowsAsync(new OperationCanceledException("Cancelled"));

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, segmentIndex, 0, 0, CancellationToken.None);

      // Assert — 499 Client Closed Request
      var objectResult = Assert.IsType<ObjectResult>(result);
      Assert.Equal(499, objectResult.StatusCode);
    }

    // =========================================================
    // Test 9: Exited job with no cached segment starts new job
    // =========================================================

    [Fact]
    public async Task SegmentRequest_ExitedJob_StartsNewJob()
    {
      // Arrange: job exists but has exited, and the segment file doesn't exist
      string hash = "exited_hash";
      int segmentIndex = 3;
      string outputDir = Path.Combine(_testOutputDir, hash);
      Directory.CreateDirectory(outputDir);

      SetupMediaFile(hash, "/fake/source.mkv");

      // Job with no process = HasExited is true
      var job = new TranscodingJob(hash, 0, 0, outputDir, 6);
      // No FfmpegProcess set, so HasExited = true
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns(job);

      var probeResult = CreateProbeResult();
      _jobManagerMock.Setup(jm => jm.GetOrCacheProbeResult(hash, "/fake/source.mkv")).Returns(probeResult);
      _jobManagerMock.Setup(jm => jm.GetOrWaitForSegment(hash, segmentIndex, It.IsAny<CancellationToken>()))
        .ReturnsAsync("/tmp/fake/segment3.ts");

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, segmentIndex, 0, 0, CancellationToken.None);

      // Assert — should start a new job since the previous one has exited
      _jobManagerMock.Verify(jm => jm.StartTranscoding(hash, "/fake/source.mkv", segmentIndex, probeResult), Times.Once);
    }

    // =========================================================
    // Test 10: Backward seek to cached segment is served from disk
    // =========================================================

    [Fact]
    public async Task SegmentRequest_BackwardSeek_CachedSegmentServedFromDisk()
    {
      // Arrange: job is at segment 20, we request segment 5 which still exists on disk
      string hash = "backward_cached_hash";
      int requestedSegment = 5;
      string outputDir = Path.Combine(_testOutputDir, hash);
      Directory.CreateDirectory(outputDir);

      // Segment 5 and 6 exist from a previous transcode
      string segPath = TranscodingJobManager.GetSegmentPath(outputDir, 5);
      string nextSegPath = TranscodingJobManager.GetSegmentPath(outputDir, 6);
      File.WriteAllText(segPath, "cached segment 5 data");
      File.WriteAllText(nextSegPath, "cached segment 6 data");

      SetupMediaFile(hash, "/fake/source.mkv");

      var job = CreateActiveJob(hash, outputDir, activeSegmentIndex: 20, startSegment: 15);
      _jobManagerMock.Setup(jm => jm.GetJob(hash)).Returns(job);

      var controller = CreateController();

      // Act
      var result = await controller.GetHlsSegment(hash, requestedSegment, 0, 0, CancellationToken.None);

      // Assert — should return from cache without restarting
      Assert.IsType<PhysicalFileResult>(result);
      var physicalResult = (PhysicalFileResult)result;
      Assert.Equal(segPath, physicalResult.FileName);

      // No restart or wait should occur
      _jobManagerMock.Verify(jm => jm.StartTranscoding(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<MediaFileProbeInformation>()), Times.Never);
      _jobManagerMock.Verify(jm => jm.GetOrWaitForSegment(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
  }
}
