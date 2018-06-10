using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Transmission.API.RPC.Entity;

namespace oMediaCenter.TransmissionPlugin
{
	public class MediaFile : IMediaFile
	{
		public MediaFile(TorrentInfo ti, TransmissionTorrentFiles file)
		{
			MediaFileRecord = new MediaFileRecord();
			MediaFileRecord.Description = ti.Comment;
			MediaFileRecord.Hash = ti.ID + "aAaA" + string.Format("{0}", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(file.Name)));
			MediaFileRecord.Name = file.Name;
			MediaFileRecord.TechnicalInfo = Path.Combine(ti.DownloadDir, file.Name.Replace('/','\\'));
			MediaFileRecord.MediaType = "video/" + Path.GetExtension(file.Name).ToLower().Substring(1);
		}

		public MediaFileRecord MediaFileRecord { get; private set; }

		public string GetFullFilePath()
		{
			return MediaFileRecord.TechnicalInfo;
		}

		public Stream GetMediaData()
		{
			return new FileStream(MediaFileRecord.TechnicalInfo, FileMode.Open, FileAccess.Read);
		}

		public Stream GetThumbnailData()
		{
			return null;
		}
	}
}
