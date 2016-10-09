﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using oMediaCenter.Web.Model;
using Microsoft.AspNetCore.Http;
using System.IO;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Utilities;
using Microsoft.Extensions.Logging;

namespace oMediaCenter.Web.Controllers
{
    [Route("api/v1")]
    public class MediaListController : Controller
    {
        FileReader _fileReader;
        ILogger _logger;
        MediaCenterContext _dbContext;

        public MediaListController(IFileReaderPluginLoader fileReaderPluginLoader, ILoggerFactory loggerFactory, MediaCenterContext dbContext)
        {
            _fileReader = new Model.FileReader(fileReaderPluginLoader);
            _logger = loggerFactory.CreateLogger<MediaListController>();
            _dbContext = dbContext;
        }

        // GET api/values
        [HttpGet]
        [Route("media")]
        public IEnumerable<MediaFileRecord> Get()
        {
            return _fileReader.GetAll().Select(mf => GetFilePositions(mf.MediaFileRecord)).OrderBy(mf => mf.Name);
        }

        private MediaFileRecord GetFilePositions(MediaFileRecord mediaFileRecord)
        {
            FilePosition filePosition = _dbContext.FilePositions.FirstOrDefault(fp => fp.FileHash == mediaFileRecord.Hash);
            if (filePosition != null)
                mediaFileRecord.LastPlayedTime = (float)filePosition.LastPlayedPosition.TotalSeconds;
            else
                mediaFileRecord.LastPlayedTime = 0;

            return mediaFileRecord;
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
            IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);

            if (selectedMediaFile != null)
            {
                Stream stream = selectedMediaFile.GetMediaData();
                return new ByteRangeStreamResult(stream, selectedMediaFile.MediaFileRecord.MediaType);
            }
            else
            {
                return null;
            }
        }

        [HttpPut]
        [Route("media/{hash}")]
        public async void UpdateCurrentTime(string hash, [FromBody]MediaUpdateMessage mediaUpdateMessage)
        {
            _logger.LogDebug("Recieved from {0} current time {1}", hash, mediaUpdateMessage.CurrentTime);
            FilePosition foundPosition = _dbContext.FilePositions.FirstOrDefault(fp => fp.FileHash == hash);
            if (foundPosition == null) {
                foundPosition = new FilePosition();
                foundPosition.FileHash = hash;
                _dbContext.FilePositions.Add(foundPosition);
            }

            foundPosition.LastPlayedPosition = TimeSpan.FromSeconds(mediaUpdateMessage.CurrentTime);

            await _dbContext.SaveChangesAsync();
        }

        [HttpGet]
        [Route("media/{hash}/technical")]
        public string GetTechnicalInfo(string hash)
        {
            IMediaFile selectedMediaFile = _fileReader.GetByHash(hash);

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
