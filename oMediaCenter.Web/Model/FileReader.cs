using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
    /// <summary>
    /// This class is responsible for reading files from the file readers that are set up in the system
    /// </summary>
    public class FileReader
    {
        private IFileReaderPlugin[] _fileReaderPlugins;

        public FileReader(IFileReaderPluginLoader fileReaderPluginLoader)
        {
            _fileReaderPlugins = fileReaderPluginLoader.GetPlugins();
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
                    return foundMediaFile;
                }
            }

            return null;
        }
    }
}
