using oMediaCenter.Interfaces;
using System;
//using ShowInfo;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
  /// <summary>
  /// This class is responsible for reading files from the file readers that are set up in the system
  /// </summary>
  public class FileReader : IFileReader
  {
    private IFileReaderPlugin[] _fileReaderPlugins;
    private IMediaInformationProvider _showInfoProvider;
    private ISubtitleProvider _subtitleProvider;

    public FileReader(IFileReaderPluginLoader fileReaderPluginLoader, IMediaInformationProvider showInfoProvider, ISubtitleProvider subtitleProvider)
    {
      _fileReaderPlugins = fileReaderPluginLoader.GetPlugins();
      _showInfoProvider = showInfoProvider;
      _subtitleProvider = subtitleProvider;
    }

    public async Task<IEnumerable<IMediaFile>> GetAll()
    {
      var candidateMediaFiles = _fileReaderPlugins.SelectMany(frp => frp.GetAll());
      candidateMediaFiles = candidateMediaFiles.Where(mf => IsValidMediaFile(mf));
      return candidateMediaFiles.Select(mf => FillWithMetadata(mf));
    }

    private bool IsValidMediaFile(IMediaFile mf)
    {
      string filename = Path.GetFileName(mf.GetFullFilePath()).ToLower();
      if (filename.Contains("sample"))
        return false;
      return true;
    }

    private IMediaFile FillWithMetadata(IMediaFile mf)
    {
      if (_showInfoProvider != null)
      {
        var filename = Path.GetFileName(mf.GetFullFilePath());
        var info = _showInfoProvider.GetEpisodeInfoForFilename(filename);
        mf.Metadata = info;
      }
      return mf;
    }

    private async Task<IMediaFile> FillWithSubtitles(IMediaFile mf)
    {
      if (_subtitleProvider != null && !mf.MediaFileRecord.HasEmbeddedSubtitles)
      {
        if (await _subtitleProvider.GetSubtitleInformation(mf, mf.MediaFileRecord.Hash + ".vtt"))
          return new CachedMediaFile(mf, mf.MediaFileRecord.Hash + ".vtt");
        else
          return mf;
      }
      return mf;
    }

    public IMediaFile GetByHash(string hash, bool queryForMetaData = true)
    {
      foreach (IFileReaderPlugin frp in _fileReaderPlugins)
      {
        IMediaFile foundMediaFile = frp.GetByHash(hash);
        if (foundMediaFile != null)
        {
          if (queryForMetaData)
            FillWithMetadata(foundMediaFile);

          return foundMediaFile;
        }
      }

      return null;
    }

    public IMediaFile GetByHash(string hash)
    {
      return GetByHash(hash, false);
    }

    public IMediaFile GetMetadataByHash(string hash)
    {
      return GetByHash(hash);
    }
  }
}
