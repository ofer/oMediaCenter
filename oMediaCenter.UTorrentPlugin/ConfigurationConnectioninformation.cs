using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.UTorrentPlugin
{
    public class ConfigurationConnectionInformation : IConnectionInformation
    {

        public ConfigurationConnectionInformation(IConfigurationSection pluginConfigurationSection)
        {
            var utorrentInformation = pluginConfigurationSection.GetSection("UTorrentConnectionInformation");
            if (utorrentInformation.GetChildren().Count() != 0)
            {
                IP = utorrentInformation["IP"];
                Login = utorrentInformation["Login"];
                Password = utorrentInformation["Password"];
                Port = int.Parse(utorrentInformation["Port"]);
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
            get; private set;
        }

        public string Login
        {
            get; private set;
        }

        public string Password
        {
            get; private set;
        }

        public int Port
        {
            get; private set;
        }
    }
}
