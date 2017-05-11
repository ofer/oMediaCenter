using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.DirectoryScanPlugin
{
    public class FileReaderPlugin : IFileReaderPlugin
    {
        const string JSON_FILELIST_NAME = "directorylist.json";

        public FileReaderPlugin()
        {
        }

        public IEnumerable<IMediaFile> GetAll()
        {
            try
            {
                return
                    Directory.GetFiles(@"c:\users\ofer achler\Videos", "*.mkv").Select(fn => new MediaFile(fn));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public IMediaFile GetByHash(string hash)
        {
            return GetAll().FirstOrDefault(mf => mf.MediaFileRecord.Hash == hash);
        }
    }
}
