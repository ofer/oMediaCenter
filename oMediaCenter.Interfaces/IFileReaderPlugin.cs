using System.Collections.Generic;

namespace oMediaCenter.Interfaces
{
    public interface IFileReaderPlugin
    {
        IEnumerable<IMediaFile> GetAll();
        IMediaFile GetByHash(string hash);
    }
}