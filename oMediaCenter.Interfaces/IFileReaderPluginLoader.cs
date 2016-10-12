using Microsoft.Extensions.Logging;

namespace oMediaCenter.Interfaces
{
    public interface IFileReaderPluginLoader
    {
        IFileReaderPlugin[] GetPlugins();
        void SetLoggerFactory(ILoggerFactory loggerFactory);
    }
}