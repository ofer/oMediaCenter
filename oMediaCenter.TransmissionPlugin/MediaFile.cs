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
			MediaFileRecord.Hash = ti.ID + "aAaA" + string.Format("{0}", file.GetHashCode());
			MediaFileRecord.Name = file.Name;
			MediaFileRecord.TechnicalInfo = Path.Combine(ti.DownloadDir, file.Name.Replace('/','\\'));
		}

		public MediaFileRecord MediaFileRecord { get; private set; }

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
