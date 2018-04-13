using oMediaCenter.Interfaces;
using oMediaCenter.UTorrentPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using oMediaCenter.DirectoryScanPlugin;

namespace oMediaCenter.Web.Model
{
	public class SimpleFileReaderPluginLoader : IFileReaderPluginLoader
	{
		private ILoggerFactory _loggerFactory;
		private IConfiguration _configuration;

		public SimpleFileReaderPluginLoader(IConfiguration pluginConfigurationSection, ILoggerFactory loggerFactory)
		{
			_configuration = pluginConfigurationSection.GetSection("Plugins");
			_loggerFactory = loggerFactory;
		}

		public IFileReaderPlugin[] GetPlugins()
		{
			List<IFileReaderPlugin> plugins = new List<IFileReaderPlugin>();
			foreach (var setting in _configuration.GetChildren())
			{
				if (setting.Key == "TransmissionPlugin")
					plugins.Add((IFileReaderPlugin)new TransmissionPlugin.FileReaderPlugin(setting, _loggerFactory));
				if (setting.Key== "UTorrentPlugin")
					plugins.Add((IFileReaderPlugin)new UTorrentPlugin.FileReaderPlugin(setting, _loggerFactory));
				if (setting.Key == "DirectoryScanPlugin")
					plugins.Add((IFileReaderPlugin)new oMediaCenter.DirectoryScanPlugin.FileReaderPlugin(setting, _loggerFactory));
			}
			return plugins.ToArray();
		}
	}
}
