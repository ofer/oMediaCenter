using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
	public class MediaFileStreamer : IMediaFileStreamer
	{
		public MediaFileStreamer(IMediaFileProber fileProber, IMediaFileConverter mediaFileConverter)
		{
			Prober = fileProber;
			Converter = mediaFileConverter;
		}

		public string MediaType { get { return "video/mp4"; } }

		public IMediaFileProber Prober { get; }
		public IMediaFileConverter Converter { get; }

		public Stream GetStream(IMediaFile selectedMediaFile)
		{
			if (selectedMediaFile.MediaFileRecord.MediaType == MediaType)
				return selectedMediaFile.GetMediaData();
			else
			{
				if (!Directory.Exists("cache"))
					Directory.CreateDirectory("cache");

				string filename = Path.Combine("cache", selectedMediaFile.MediaFileRecord.Hash + ".mp4");

				if (!File.Exists(filename))
				{
					// convert file to mp4 and send it along, h264 / aac
					// probe the file, see what conversion it needs
					MediaFileProbeInformation mfpi = Prober.GetProbeInfo(selectedMediaFile.GetFullFilePath());
					string targetVideoCodec = "copy";
					if (mfpi.VideoCodec != "h264")
						targetVideoCodec = "libx264";

					string targetAudioCodec = "copy";
					if (mfpi.AudioCodec != "aac")
						targetAudioCodec = "aac";

					Converter.Convert(selectedMediaFile.GetFullFilePath(), targetVideoCodec, targetAudioCodec, filename);
				}
				
				return File.OpenRead(filename);
			}
		}
	}
}
