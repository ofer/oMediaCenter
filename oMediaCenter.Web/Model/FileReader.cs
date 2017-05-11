using oMediaCenter.Interfaces;
using ShowInfo;
using System;
using System.Collections.Generic;
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
		private IShowInformationManager _showInfoManager;

        public FileReader(IFileReaderPluginLoader fileReaderPluginLoader, IShowInformationManager showInfoManager)
        {
			_fileReaderPlugins = fileReaderPluginLoader.GetPlugins();
			_showInfoManager = showInfoManager;
		}

        public IEnumerable<IMediaFile> GetAll()
        {
            return _fileReaderPlugins.SelectMany(frp => frp.GetAll());
        }

        public IMediaFile GetByHash(string hash)
        {
            foreach (IFileReaderPlugin frp in _fileReaderPlugins)
            {
                IMediaFile foundMediaFile = frp.GetByHash(hash);
                if (foundMediaFile != null)
                {
					if (_showInfoManager != null)
					{
						var info = _showInfoManager.GetEpisodeInfoForFilename(foundMediaFile.MediaFileRecord.Name);
						foundMediaFile.MediaFileRecord.TechnicalInfo = info.EpisodeSummary;
					}
                    return foundMediaFile;
                }
            }

            return null;
        }
    }
}
