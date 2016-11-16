using Microsoft.Extensions.Configuration;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.UTorrentPlugin
{
	public class UtorrentFileReaderPluginSettings
	{
		public UtorrentFileReaderPluginSettings()
		{
			//IConfigurationSection pluginConfigurationSection

			//var utorrentInformation = pluginConfigurationSection.GetSection("UTorrentConnectionInformation");
			//         if (utorrentInformation.GetChildren().Count() != 0)
			//         {
			//             IP = utorrentInformation["IP"];
			//             Login = utorrentInformation["Login"];
			//             Password = utorrentInformation["Password"];
			//             Port = int.Parse(utorrentInformation["Port"]);
			//         }
			//         else
			//         {
			//             IP = "localhost";
			//             Login = "admin";
			//             Password = "admin";
			//             Port = 8080;
			//         }
		}

		public UtorrentFileReaderPluginSettings(IConfigurationSection pluginConfigurationSection)
		{
			if (pluginConfigurationSection.GetChildren().Count() != 0)
			{
				IP = pluginConfigurationSection["IP"];
				Login = pluginConfigurationSection["Login"];
				Password = pluginConfigurationSection["Password"];
				Port = int.Parse(pluginConfigurationSection["Port"]);
			}
			else
			{
				IP = "localhost";
				Login = "admin";
				Password = "admin";
				Port = 8080;
			}
		}

		public string IP
		{
			get; set;
		}

		public string Login
		{
			get; set;
		}

		public string Password
		{
			get; set;
		}

		public int Port
		{
			get; set;
		}
	}
}
