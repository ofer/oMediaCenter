using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using oMediaCenter.Web.Model;
using Microsoft.AspNetCore.Http;
using System.IO;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Utilities;

namespace oMediaCenter.Web.Controllers
{
    [Route("api/v1")]
    public class MediaListController : Controller
    {
        FileReader _fileReader;

        public MediaListController(IFileReaderPluginLoader fileReaderPluginLoader)
        {
            _fileReader = new Model.FileReader(fileReaderPluginLoader);
        }

        // GET api/values
        [HttpGet]
        [Route("media")]
        public IEnumerable<MediaFileRecord> Get()
        {
            return _fileReader.GetAll().Select(mf => mf.MediaFileRecord);
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
                //var requestedRanges = HttpContext.Request.GetTypedHeaders().Range;
                //if (requestedRanges != null && requestedRanges.Ranges.Count > 0)
                //{
                //    var requestedRange = requestedRanges.Ranges.First();

                //    stream.Seek(requestedRange.From.Value, SeekOrigin.Begin);
                //    long length = stream.Length - requestedRange.From.Value;

                //    if (requestedRange.To != null)
                //        length = requestedRange.To.Value - requestedRange.From.Value;

                //    using (BinaryReader br = new BinaryReader(stream))
                //    {
                //        FileContentResult fcr = new FileContentResult(br.ReadBytes((int)length), selectedMediaFile.MediaFileRecord.MediaType);
                //        return fcr;
                //    }
                //}
                //else
                //{
                //    FileStreamResult fsr = new FileStreamResult(selectedMediaFile.GetMediaData(), selectedMediaFile.MediaFileRecord.MediaType);
                //    return fsr;
                //}
            }
            else
            {
                return null;
            }
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
