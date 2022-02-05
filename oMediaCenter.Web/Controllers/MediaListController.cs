using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Model;
using oMediaCenter.Web.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

    public MediaListController(IFileReader fileReader, ILoggerFactory loggerFactory, MediaCenterContext dbContext, IMediaFileStreamer mediaFileStreamer)
    {
      _fileReader = fileReader;
      _logger = loggerFactory.CreateLogger<MediaListController>();
      _dbContext = dbContext;
      _mediaFileStreamer = mediaFileStreamer;
    }

    // GET api/values
    [HttpGet]
    [Route("media")]
    public IEnumerable<MediaFileRecord> Get()
    {
      var mediaRecords = _fileReader.GetAll().Select(mf => GetDecoratedMediaFileRecord(mf)).OrderBy(mf => mf.Name).OrderBy(mf => !mf.FoundMetadata);

      return mediaRecords;
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
      if (filePosition != null)
        result.LastPlayedTime = (float)filePosition.LastPlayedPosition.TotalSeconds;
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
        string targetFilename = Path.Combine(MediaFileStreamer.CACHE_DIR, hash);
        if (System.IO.File.Exists(targetFilename))
          return new FileContentResult(System.IO.File.ReadAllBytes(targetFilename), MediaFileStreamer.HLS_MEDIA_TYPE);
        else
          hash = hash.Substring(0, hash.Length - 5);
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

    [HttpGet]
    [Route("media/{hash}/subtitles")]
    public async Task<ActionResult> GetVideoSubtitles(string hash)
    {
      IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);


      if (selectedMediaFile != null && selectedMediaFile.GetFullSubtitleFilePath() != null)
      {
        string subtitleFile = await _mediaFileStreamer.GetSubtitleFilePath(selectedMediaFile);
        return File(System.IO.File.ReadAllBytes(subtitleFile), "text/vtt");
      }
      else
      {
        return StatusCode((int)HttpStatusCode.NoContent);
      }
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
