using System;
using System.IO;
using System.Linq;
using oMediaCenter.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace oMediaCenter.DirectoryScanPlugin
{
    internal class MediaFile : IMediaFile
    {
        private FileInfo _fileInfo;

        public MediaFile(string fn)
        {
            _fileInfo = new FileInfo(fn);
        }

        public MediaFileRecord MediaFileRecord
        {
            get
            {
                MD5 md5 = MD5.Create();

                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(_fileInfo.FullName);
                byte[] hash = md5.ComputeHash(inputBytes);

                StringBuilder hashString = new StringBuilder();
                foreach (byte hashChar in hash)
                    hashString.Append(hashChar.ToString("X2"));


                return new MediaFileRecord()
                {
                    Description = _fileInfo.Name,
                    Hash = hashString.ToString(),
                    MediaType = "video/" + _fileInfo.Extension.Substring(1, _fileInfo.Extension.Length - 1),
                    Name = _fileInfo.Name,
                    TechnicalInfo = string.Empty,
                    ThumbnailType = null
                };
            }
        }

		public MediaInformation Metadata { get; set; }

		public string GetFullFilePath()
		{
			return _fileInfo.FullName;
		}

		public Stream GetMediaData()
        {
            return _fileInfo.OpenRead();
        }

        public Stream GetThumbnailData()
        {
            return null;
        }
    }
}