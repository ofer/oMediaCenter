using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace oMediaCenter.DirectoryScanPlugin
{
    public class FileReaderPlugin : IFileReaderPlugin
    {
        private string[] _scanDirectories;
        private ILogger _logger;

        public FileReaderPlugin(IConfigurationSection pluginConfigurationSection, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileReaderPlugin>();
            try
            {
                _scanDirectories = pluginConfigurationSection.GetSection("Directories").GetChildren().Select(s => s.Value).ToArray();
            }
            catch (Exception e)
            {
                _logger.LogWarning(1, e, "Could not get scan directories from configuration");
                _scanDirectories = new string[0];
            }
        }

        /// <summary>
        /// Gets all files in the directories specified in the Directories configuration section
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IMediaFile> GetAll()
        {
            List<IMediaFile> result = new List<IMediaFile>();
            foreach (string directory in _scanDirectories)
            {
                try
                {
                    result.AddRange(Directory.GetFiles(directory, "*.mkv").Select(fn => new MediaFile(fn)));
                    result.AddRange(Directory.GetFiles(directory, "*.mp4").Select(fn => new MediaFile(fn)));
                }
                catch (Exception e)
                {
                    _logger.LogInformation(1, "Could not read directory information from {0}", directory);
                }
            }
            return result;
        }

        public IMediaFile GetByHash(string hash)
        {
            return GetAll().FirstOrDefault(mf => mf.MediaFileRecord.Hash == hash);
        }
    }
}
