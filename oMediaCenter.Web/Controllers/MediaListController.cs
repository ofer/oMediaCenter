using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Model;
using oMediaCenter.Web.Services;
using oMediaCenter.Web.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Controllers
{
  [Route("api/v1")]
  public class MediaListController : Controller
  {
    IFileReader _fileReader;
    ILogger _logger;
    MediaCenterContext _dbContext;
    IMediaFileStreamer _mediaFileStreamer;
    private ISubtitleProvider _subtitleProvider;
    private IMediaFileProber _prober;
    private HlsPlaylistGenerator _playlistGenerator;
    private ITranscodingJobManager _jobManager;

    /// <summary>
    /// Maximum gap in segments before killing and restarting ffmpeg (matches Jellyfin's threshold).
    /// If the requested segment is more than this many segments ahead of the current transcoding position,
    /// the job is killed and restarted from the requested position.
    /// </summary>
    private const int SegmentGapRequiringRestart = 24;

    public MediaListController(
      IFileReader fileReader,
      ILoggerFactory loggerFactory,
      ISubtitleProvider subtitleProvider,
      MediaCenterContext dbContext,
      IMediaFileStreamer mediaFileStreamer,
      IMediaFileProber prober,
      HlsPlaylistGenerator playlistGenerator,
      ITranscodingJobManager jobManager)
    {
      _fileReader = fileReader;
      _logger = loggerFactory.CreateLogger<MediaListController>();
      _dbContext = dbContext;
      _mediaFileStreamer = mediaFileStreamer;
      _subtitleProvider = subtitleProvider;
      _prober = prober;
      _playlistGenerator = playlistGenerator;
      _jobManager = jobManager;
    }

    // GET api/values
    [HttpGet]
    [Route("media")]
    public async Task<IEnumerable<MediaFileRecord>> Get()
    {
      var mediaRecords = await _fileReader.GetAll();

      return mediaRecords.Select(mf => GetDecoratedMediaFileRecord(mf)).OrderBy(mf => mf.Name).OrderBy(mf => !mf.FoundMetadata);
    }

    private MediaFileRecord GetDecoratedMediaFileRecord(IMediaFile mediaFile)
    {
      MediaFileRecord result = new MediaFileRecord();

      var mediaFileRecord = mediaFile.MediaFileRecord;
      var metadata = mediaFile.Metadata;

      if (metadata != null)
      {
        result.FoundMetadata = true;
        result.Name = metadata.Title;
        result.Description = metadata.OtherInfo;
        result.Episode = metadata.Episode;
        result.ImdbNumber = metadata.ImdbNumber;
        result.Season = metadata.Season;
        result.Year = metadata.Year;
        result.Genres = metadata.Genres;
      }
      else
      {
        result.Name = mediaFileRecord.Name;
        result.Description = mediaFileRecord.Description;
        result.FoundMetadata = false;
      }

      result.Hash = mediaFileRecord.Hash;
      result.MediaType = mediaFileRecord.MediaType;
      result.TechnicalInfo = mediaFileRecord.TechnicalInfo;
      result.ThumbnailType = mediaFileRecord.ThumbnailType;

      FilePosition filePosition = _dbContext.FilePositions.FirstOrDefault(fp => fp.FileHash == result.Hash);
      if (filePosition != null) { 
        result.LastPlayedTime = (float)filePosition.LastPlayedPosition.TotalSeconds;
        result.LastPlayedDate = filePosition.LastPlayed;
      }
      else
        result.LastPlayedTime = 0;

      return result;
    }

    [HttpGet]
    [Route("media/{hash}/thumbnail")]
    public FileContentResult GetThumbnail(string hash)
    {
      IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);

      if (selectedMediaFile != null)
      {
        Stream stream = selectedMediaFile.GetThumbnailData();
        using (BinaryReader br = new BinaryReader(stream))
        {
          FileContentResult fcr = new FileContentResult(br.ReadBytes((int)stream.Length), selectedMediaFile.MediaFileRecord.ThumbnailType);

          return fcr;
        }
      }
      else
      {
        return null;
      }
    }

    [HttpGet]
    [Route("media/{hash}")]
    public ActionResult GetVideo(string hash)
    {
      if (hash.EndsWith(".m3u8"))
      {
        // Strip the .m3u8 extension to get the actual hash
        string actualHash = hash.Substring(0, hash.Length - 5);

        // Generate master playlist in-memory from probe data (no disk write, no ffmpeg launch)
        IMediaFile mediaFile = _fileReader.GetByHash(actualHash);
        if (mediaFile == null)
          return NotFound();

        if (mediaFile.MediaFileRecord.MediaType == "video/mp4")
        {
          // MP4 files don't need HLS — redirect to direct download
          return Redirect($"/api/v1/media/{actualHash}");
        }

        MediaFileProbeInformation probeResult = _prober.GetProbeInfo(mediaFile.GetFullFilePath());
        bool hasSubtitles = probeResult.ContainsSubtitles
          || mediaFile.GetFullSubtitleFilePath() != null
          || System.IO.File.Exists(actualHash.ToCacheDirectoryFile(".vtt"));
        string masterPlaylist = _playlistGenerator.GenerateMasterPlaylist(probeResult, actualHash, hasSubtitles);

        return Content(masterPlaylist, MediaFileStreamer.HLS_MEDIA_TYPE, Encoding.UTF8);
      }

      IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);

      if (selectedMediaFile != null)
      {
        StreamingFile stream = _mediaFileStreamer.GetStream(selectedMediaFile);
        return new ByteRangeStreamResult(stream.Stream, stream.MediaType);
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Returns the variant HLS playlist for a specific media file.
    /// Lists all segments upfront as a VOD manifest with startTicks/durationTicks query params.
    /// No segments are pre-generated — this is just the manifest.
    /// </summary>
    [HttpGet]
    [Route("media/{hash}/hls/variant.m3u8")]
    public ActionResult GetVariantPlaylist(string hash)
    {
      IMediaFile mediaFile = _fileReader.GetByHash(hash);
      if (mediaFile == null)
        return NotFound();

      MediaFileProbeInformation probeResult = _prober.GetProbeInfo(mediaFile.GetFullFilePath());
      string variantPlaylist = _playlistGenerator.GenerateVariantPlaylist(probeResult, hash);

      return Content(variantPlaylist, MediaFileStreamer.HLS_MEDIA_TYPE, Encoding.UTF8);
    }

    [HttpGet]
    [Route("media/{hash}/hls/subtitles.m3u8")]
    public async Task<ActionResult> GetHlsSubtitlePlaylist(string hash)
    {
      IMediaFile mediaFile = _fileReader.GetByHash(hash);
      if (mediaFile == null)
        return NotFound();

      string subtitleFile = await _mediaFileStreamer.GetSubtitleFilePath(mediaFile);
      if (subtitleFile == null || !System.IO.File.Exists(subtitleFile))
        return StatusCode((int)HttpStatusCode.NoContent);

      MediaFileProbeInformation probeResult = _prober.GetProbeInfo(mediaFile.GetFullFilePath());
      string subtitlePlaylist = _playlistGenerator.GenerateSubtitlePlaylist(probeResult, hash);

      return Content(subtitlePlaylist, MediaFileStreamer.HLS_MEDIA_TYPE, Encoding.UTF8);
    }

    /// <summary>
    /// Returns an individual HLS segment (.ts file) for a specific media file.
    /// Implements on-demand transcoding: if the segment already exists on disk it is
    /// returned immediately; otherwise ffmpeg is started (or waited on) to produce it.
    /// </summary>
    [HttpGet]
    [Route("media/{hash}/hls/{segmentIndex}.ts")]
    public async Task<ActionResult> GetHlsSegment(string hash, int segmentIndex, [FromQuery] long startTicks = 0, [FromQuery] long durationTicks = 0, CancellationToken cancellationToken = default)
    {
      _logger.LogInformation("Segment request: hash={Hash}, segment={Segment}, startTicks={StartTicks}, durationTicks={DurationTicks}",
        hash, segmentIndex, startTicks, durationTicks);

      // 1. Resolve the media file
      IMediaFile mediaFile = _fileReader.GetByHash(hash);
      if (mediaFile == null)
        return NotFound();

      string sourceFile = mediaFile.GetFullFilePath();

      // 2. Check if the segment already exists on disk (cached from a previous transcode)
      var existingJob = _jobManager.GetJob(hash);
      if (existingJob != null)
      {
        string cachedSegmentPath = TranscodingJobManager.GetSegmentPath(existingJob.OutputPath, segmentIndex);
        string cachedNextSegmentPath = TranscodingJobManager.GetSegmentPath(existingJob.OutputPath, segmentIndex + 1);
        if (System.IO.File.Exists(cachedSegmentPath) && (System.IO.File.Exists(cachedNextSegmentPath) || existingJob.HasExited))
        {
          _logger.LogInformation("Segment {Segment} for hash {Hash} served from cache", segmentIndex, hash);
          existingJob.LastActivityUtc = DateTime.UtcNow;
          return PhysicalFile(cachedSegmentPath, "video/mp2t");
        }
      }

      // 3. Determine whether to wait for an active job, or kill+restart, or start fresh
      bool needsNewJob = false;
      if (existingJob == null || existingJob.HasExited)
      {
        // No active job — need to start one
        needsNewJob = true;
      }
      else
      {
        int currentIndex = existingJob.ActiveSegmentIndex;

        if (segmentIndex < currentIndex)
        {
          // Backward seek: requested segment is behind the transcoding position.
          // Check if the segment file still exists from a previous run.
          string segPath = TranscodingJobManager.GetSegmentPath(existingJob.OutputPath, segmentIndex);
          string nextSegPath = TranscodingJobManager.GetSegmentPath(existingJob.OutputPath, segmentIndex + 1);
          if (System.IO.File.Exists(segPath) && (System.IO.File.Exists(nextSegPath) || existingJob.HasExited))
          {
            _logger.LogInformation("Backward seek to segment {Segment} for hash {Hash} — serving from existing file", segmentIndex, hash);
            existingJob.LastActivityUtc = DateTime.UtcNow;
            return PhysicalFile(segPath, "video/mp2t");
          }

          // File doesn't exist — need to restart
          _logger.LogInformation("Backward seek to segment {Segment} for hash {Hash} — restarting transcode", segmentIndex, hash);
          needsNewJob = true;
        }
        else if (segmentIndex - currentIndex > SegmentGapRequiringRestart)
        {
          // Forward gap too large — kill and restart from the requested position
          _logger.LogInformation("Large forward seek to segment {Segment} (current: {Current}) for hash {Hash} — restarting transcode",
            segmentIndex, currentIndex, hash);
          needsNewJob = true;
        }
        // else: segment is within range ahead of current position — wait for it
      }

      if (needsNewJob)
      {
        // Probe the file for codec decisions (cached per hash to avoid re-running ffprobe)
        MediaFileProbeInformation probeResult = _jobManager.GetOrCacheProbeResult(hash, sourceFile);
        _jobManager.StartTranscoding(hash, sourceFile, segmentIndex, probeResult);
      }

      // 4. Wait for the segment to become ready
      try
      {
        string segmentPath = await _jobManager.GetOrWaitForSegment(hash, segmentIndex, cancellationToken);
        return PhysicalFile(segmentPath, "video/mp2t");
      }
      catch (TimeoutException ex)
      {
        _logger.LogWarning(ex, "Timeout waiting for segment {Segment} of hash {Hash}", segmentIndex, hash);
        return StatusCode(504, $"Timeout waiting for segment {segmentIndex} to be transcoded.");
      }
      catch (OperationCanceledException)
      {
        _logger.LogInformation("Client disconnected while waiting for segment {Segment} of hash {Hash}", segmentIndex, hash);
        return StatusCode(499, "Client closed request.");
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Error serving segment {Segment} of hash {Hash}", segmentIndex, hash);
        return StatusCode(500, ex.Message);
      }
    }

    [HttpGet]
    [Route("media/{hash}/file")]
    public ActionResult DownloadFile(string hash)
    {
      IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);
      
      return File(System.IO.File.ReadAllBytes(selectedMediaFile.GetFullFilePath()), "application/octet-stream", Path.GetFileName(selectedMediaFile.GetFullFilePath()));
    }

    [HttpGet]
    [Route("media/{hash}/subtitles")]
    public async Task<ActionResult> GetVideoSubtitles(string hash)
    {
      IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);

      if (selectedMediaFile == null)
        return NotFound();

      string subtitleFile = await _mediaFileStreamer.GetSubtitleFilePath(selectedMediaFile);
      if (subtitleFile == null || !System.IO.File.Exists(subtitleFile))
        return StatusCode((int)HttpStatusCode.NoContent);

      _logger.LogInformation("Subtitle file found at {0}, outputting it", subtitleFile);
      return File(System.IO.File.ReadAllBytes(subtitleFile), "text/vtt");
    }


    [HttpPut]
    [Route("media/{hash}")]
    public async void UpdateCurrentTime(string hash, [FromBody] MediaUpdateMessage mediaUpdateMessage)
    {
      if (mediaUpdateMessage == null)
        return;
      _logger.LogDebug("Recieved from {0} current time {1}", hash, mediaUpdateMessage.CurrentTime);
      try
      {
        using (var transaction = _dbContext.Database.BeginTransaction())
        {
          FilePosition foundPosition = _dbContext.FilePositions.FirstOrDefault(fp => fp.FileHash == hash);
          if (foundPosition == null)
          {
            foundPosition = new FilePosition();
            foundPosition.FileHash = hash;
            _dbContext.FilePositions.Add(foundPosition);
          }

          foundPosition.LastPlayedPosition = TimeSpan.FromSeconds(mediaUpdateMessage.CurrentTime);
          foundPosition.LastPlayed = DateTime.UtcNow;

          await _dbContext.SaveChangesAsync();

          transaction.Commit();
        }
      }
      catch (Exception e)
      {
        _logger.LogWarning(1, e, "Could not update current time");
      }
    }

    [HttpGet]
    [Route("media/{hash}/technical")]
    public string GetTechnicalInfo(string hash)
    {
      IMediaFile selectedMediaFile = _fileReader.GetMetadataByHash(hash);

      if (selectedMediaFile != null)
      {
        return selectedMediaFile.MediaFileRecord.TechnicalInfo;
      }
      else
      {
        return null;
      }
    }
  }
}
