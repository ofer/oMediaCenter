using oMediaCenter.Interfaces;
using oMediaCenter.UTorrentPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
    public class ConnectionInformation : IConnectionInformation
    {
        public string IP
        {
            get
            {
                return "192.168.1.100";
            }
        }

        public string Login
        {
            get
            {
                return "admin";
            }
        }

        public string Password
        {
            get
            {
                return "admin";
            }
        }

        public int Port
        {
            get
            {
                return 8080;
            }
        }
    }

    public class SimpleFileReaderPluginLoader : IFileReaderPluginLoader
    {
        public IFileReaderPlugin[] GetPlugins()
        {
            return new IFileReaderPlugin[] 
            {
                (IFileReaderPlugin)new UTorrentPlugin.FileReaderPlugin(new ConnectionInformation()),
            (IFileReaderPlugin)new oMediaCenter.DirectoryScanPlugin.FileReaderPlugin()};
        }
    }
}
