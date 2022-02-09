using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using OSDBnet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace oMediaCenter.SubtitleProvidier
{
  public class OpenSubtitlesProvider : ISubtitleProvider
  {
    private IOsdbClient _service;
    private IMediaFileConverter _mediaFileConverter;
    private ILogger<OpenSubtitlesProvider> _logger;
    private ConcurrentBag<IMediaFile> _checkedMediaFileBag;

    public OpenSubtitlesProvider(IMediaFileConverter mediaFileConverter, ILoggerFactory loggerFactory)
    {
      _service = Osdb.Create("TemporaryUserAgent");
      _mediaFileConverter = mediaFileConverter;
      _logger = loggerFactory.CreateLogger<OpenSubtitlesProvider>();
      _checkedMediaFileBag = new ConcurrentBag<IMediaFile>();
    }

    public async Task<bool> GetSubtitleInformation(IMediaFile mf, string targetFilename)
    {
      if (File.Exists(targetFilename) || mf.MediaFileRecord.HasEmbeddedSubtitles)
        return true;

      try
      {
        if (_checkedMediaFileBag.Contains(mf))
          return false;

        var subtitles = await _service.SearchSubtitlesFromFile("english", mf.GetFullFilePath());

        int subtitlesCount = subtitles.Count;
        if (subtitlesCount == 0)
        {
          _logger.LogInformation("Could not find any subtitles in english for {0}", mf.MediaFileRecord.Name);
          _checkedMediaFileBag.Add(mf);
          return false;
        }
        var selectedSubtitle = subtitles.First();

        if (Path.GetExtension(selectedSubtitle.SubtitleFileName).Substring(1).ToLowerInvariant() != "vtt")
        {
          string subtitleFile = await _service.DownloadSubtitleToPath(Path.GetDirectoryName(targetFilename), selectedSubtitle);
          await _mediaFileConverter.ConvertSubtitles(subtitleFile, targetFilename);
        }
        else
          await _service.DownloadSubtitleToPath(Path.GetDirectoryName(targetFilename), selectedSubtitle, Path.GetFileName(targetFilename));

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Could not get subtitle information for target {0}", targetFilename);
        return false;
      }

      //string subtitleHash = MovieCollection.OpenSubtitles.OpenSubtitlesHasher.GetFileHash(mf.GetFullFilePath());

      //  var search = new NewSubtitleSearch { MovieHash = subtitleHash };
      //  var subtitleSearchResults = await _service.SearchSubtitlesAsync(search);
      //  if (subtitleSearchResults.TotalCount > 0)
      //  {
      //    int fileId = subtitleSearchResults.Data.First().Attributes.Files.First().FileId;
      //    var fileRecordResult = await _service.GetSubtitleForDownloadAsync(new NewDownload() { FileId = fileId });
      //    var webClient = new HttpClient();
      //    await File.WriteAllBytesAsync(targetFilename, await webClient.GetByteArrayAsync(fileRecordResult.Link));
      //    return true;
      //  }
      //  else
      //    return false;
    }
  }
}
