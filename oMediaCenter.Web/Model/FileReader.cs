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
      return _fileReaderPlugins.SelectMany(frp => frp.GetAll()).Select(mf => SetMetadata(mf));
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

    public IMediaFile GetByHash(string hash)
    {
      foreach (IFileReaderPlugin frp in _fileReaderPlugins)
      {
        IMediaFile foundMediaFile = frp.GetByHash(hash);
        if (foundMediaFile != null)
        {
          SetMetadata(foundMediaFile);
          return foundMediaFile;
        }
      }

      return null;
    }
  }
}
