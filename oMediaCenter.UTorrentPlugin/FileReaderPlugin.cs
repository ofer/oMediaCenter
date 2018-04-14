using Microsoft.Extensions.Configuration;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UTorrent.Api;
using UTorrent.Api.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace oMediaCenter.UTorrentPlugin
{
    public class FileReaderPlugin : IFileReaderPlugin
    {
        UtorrentFileReaderPluginSettings _connectionInfo;
        static readonly char[] SPLIT_CHARS = new char[] { ' ', '.' };
        private ILogger _logger;

        public FileReaderPlugin(IConfigurationSection pluginConfigurationSection, ILoggerFactory loggerFactory)
        {
            if (pluginConfigurationSection != null)
            {
				_connectionInfo = new UtorrentFileReaderPluginSettings(pluginConfigurationSection);
            }
            _logger = loggerFactory.CreateLogger<FileReaderPlugin>();
        }

        public IEnumerable<IMediaFile> GetAll()
        {
            if (_connectionInfo != null)
            {
                UTorrent.Api.UTorrentClient utc = new UTorrent.Api.UTorrentClient(_connectionInfo.IP, _connectionInfo.Port, _connectionInfo.Login, _connectionInfo.Password);

                var torrentList = utc.GetList().Result.Torrents;
                return torrentList.Select(t => FromUtorrent(utc, t)).Where(mf => mf != null).ToArray();
            }
            else
                return new IMediaFile[0];
        }

        private MediaFile FromUtorrent(UTorrentClient utc, Torrent t)
        {
            if (t == null)
                return null;
            MediaFile mf = new MediaFile();
            if (t.Progress == 1000)
            {
                mf.MediaFileRecord.Hash = t.Hash;
                string keyword = t.Name.Split(SPLIT_CHARS)[0].ToLower();

                var candidateTorrent = utc.GetTorrent(t.Hash).Result;
                var candidateFiles = candidateTorrent.Files[t.Hash].Where(f => Matches(f.Name, keyword));

                UTorrent.Api.Data.File chosenCandidate = candidateFiles.FirstOrDefault();
                if (candidateFiles.Count() > 1)
                {
                    chosenCandidate = candidateFiles.OrderByDescending(cf => cf.Size).First();
                }

                if (chosenCandidate == null)
                    return null;

                mf.MediaFileRecord.Name = chosenCandidate.NameWithoutPath;
                mf.MediaFileRecord.MediaType = "video/" + Path.GetExtension(chosenCandidate.Name).ToLower().Substring(1);
                mf.FilePath = Path.Combine(t.Path, chosenCandidate.Name);
                return mf;
            }

            return null;
        }

        private bool Matches(string filename, string keyword)
        {

            if (filename.Split(SPLIT_CHARS)[0].ToLower() == keyword &&
                MediaFileRecord.VALID_EXTENSIONS.Contains(Path.GetExtension(filename).ToLower().Substring(1)))
                return true;
            else
                return false;
        }

        public IMediaFile GetByHash(string hash)
        {
            if (_connectionInfo != null)
            {
                UTorrent.Api.UTorrentClient utc = new UTorrent.Api.UTorrentClient(_connectionInfo.IP, _connectionInfo.Port, _connectionInfo.Login, _connectionInfo.Password);

                var query = utc.GetTorrent(hash);

                return FromUtorrent(utc, query.Result.Torrents.FirstOrDefault(t => t.Hash == hash));
            }
            else
            {
                return null;
            }
        }
    }
}
