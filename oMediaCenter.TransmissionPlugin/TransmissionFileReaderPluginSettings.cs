using Microsoft.Extensions.Configuration;
using System.Linq;

namespace oMediaCenter.TransmissionPlugin
{
	internal class TransmissionFileReaderPluginSettings
	{

		public TransmissionFileReaderPluginSettings(IConfigurationSection pluginConfigurationSection)
		{
			if (pluginConfigurationSection.GetChildren().Count() != 0)
			{
				IP = pluginConfigurationSection["IP"];
			}
			else
			{
				IP = "localhost";
			}
		}

		public string IP { get; }
	}
}