using oMediaCenter.Interfaces;
using System;
//using ShowInfo;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace oMediaCenter.Web.Model
{
  /// <summary>
  /// This class is responsible for reading files from the file readers that are set up in the system
  /// </summary>
  public class FileReader : IFileReader
  {
    private IFileReaderPlugin[] _fileReaderPlugins;
    private IMediaInformationProvider _showInfoProvider;

    public FileReader(IFileReaderPluginLoader fileReaderPluginLoader, IMediaInformationProvider showInfoProvider)
    {
      _fileReaderPlugins = fileReaderPluginLoader.GetPlugins();
      _showInfoProvider = showInfoProvider;
    }

    public IEnumerable<IMediaFile> GetAll()
    {
      var candidateMediaFiles = _fileReaderPlugins.SelectMany(frp => frp.GetAll());
      candidateMediaFiles = candidateMediaFiles.Where(mf => IsValidMediaFile(mf));
      return candidateMediaFiles.Select(mf => SetMetadata(mf));
    }

    private bool IsValidMediaFile(IMediaFile mf)
    {
      string filename = Path.GetFileName(mf.GetFullFilePath()).ToLower();
      if (filename.Contains("sample"))
        return false;
      return true;
    }

    private IMediaFile SetMetadata(IMediaFile mf)
    {
      if (_showInfoProvider != null)
      {
        var filename = Path.GetFileName(mf.GetFullFilePath());
        var info = _showInfoProvider.GetEpisodeInfoForFilename(filename);
        mf.Metadata = info;
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
            SetMetadata(foundMediaFile);

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
