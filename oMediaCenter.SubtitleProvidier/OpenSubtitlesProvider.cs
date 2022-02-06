using oMediaCenter.Interfaces;
using OSDBnet;
using System;
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

    public OpenSubtitlesProvider(IMediaFileConverter mediaFileConverter)
    {
      _service = Osdb.Create("TemporaryUserAgent");
      _mediaFileConverter = mediaFileConverter;
    }

    public async Task<bool> GetSubtitleInformation(IMediaFile mf, string targetFilename)
    {
      if (File.Exists(targetFilename) || mf.MediaFileRecord.HasEmbeddedSubtitles)
        return true;

      try
      {
        var subtitles = await _service.SearchSubtitlesFromFile("english", mf.GetFullFilePath());

        int subtitlesCount = subtitles.Count;
        if (subtitlesCount == 0)
        {
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
