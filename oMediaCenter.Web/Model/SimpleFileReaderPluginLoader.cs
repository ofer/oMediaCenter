using oMediaCenter.Interfaces;
using oMediaCenter.UTorrentPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace oMediaCenter.Web.Model
{
    public class SimpleFileReaderPluginLoader : IFileReaderPluginLoader
    {
        private IConfigurationSection _pluginConfigurationSection;
        private ILoggerFactory _loggerFactory;

        public SimpleFileReaderPluginLoader(IConfigurationSection pluginConfigurationSection)
        {
            _pluginConfigurationSection = pluginConfigurationSection;
        }

        public IFileReaderPlugin[] GetPlugins()
        {
            return new IFileReaderPlugin[]
            {
                (IFileReaderPlugin)new UTorrentPlugin.FileReaderPlugin(_pluginConfigurationSection, _loggerFactory),
                (IFileReaderPlugin)new oMediaCenter.DirectoryScanPlugin.FileReaderPlugin(_pluginConfigurationSection, _loggerFactory)
            };
        }

        public void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
    }
}
