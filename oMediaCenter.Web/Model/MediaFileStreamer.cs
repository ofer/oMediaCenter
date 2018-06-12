﻿using oMediaCenter.Interfaces;
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

        const string MP4_MEDIA_TYPE = "video/mp4";
        const string HLS_MEDIA_TYPE = "application/vnd.apple.mpegurl";

        const string CACHE_DIR = "wwwroot\\cache";

        const string FILENAME_TEMPLATE = "{0}.m3u8";

        public string MediaType { get { return HLS_MEDIA_TYPE; } }

        public IMediaFileProber Prober { get; }
        public IMediaFileConverter Converter { get; }

        public Stream GetStream(IMediaFile selectedMediaFile)
        {
            if (selectedMediaFile.MediaFileRecord.MediaType == MediaType)
                return selectedMediaFile.GetMediaData();
            else
            {
                if (!Directory.Exists(CACHE_DIR))
                    Directory.CreateDirectory(CACHE_DIR);

                string filename = Path.Combine(CACHE_DIR, string.Format(FILENAME_TEMPLATE, selectedMediaFile.MediaFileRecord.Hash));

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
                return File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }
    }
}
