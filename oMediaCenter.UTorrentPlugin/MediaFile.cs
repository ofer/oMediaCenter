using System;
using System.IO;
using oMediaCenter.Interfaces;

namespace oMediaCenter.UTorrentPlugin
{
    public class MediaFile : IMediaFile
    {
        public MediaFile()
        {
            MediaFileRecord = new MediaFileRecord();
        }

        public MediaFileRecord MediaFileRecord
        {
            get; private set;
        }

        internal string FilePath { get; set; }

        public Stream GetMediaData()
        {
            return new FileStream(FilePath, FileMode.Open);
        }

        internal string ThumbnailPath { get; set; }

        public Stream GetThumbnailData()
        {
            if (ThumbnailPath != null)
                return new FileStream(ThumbnailPath, FileMode.Open);
            else
                return null;
        }
    }
}