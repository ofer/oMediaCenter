using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UTorrent.Api;
using UTorrent.Api.Data;

namespace oMediaCenter.UTorrentPlugin
{
    public class FileReaderPlugin : IFileReaderPlugin
    {
        IConnectionInformation _connectionInfo;
        static readonly char[] SPLIT_CHARS = new char[] { ' ', '.' };
        static readonly string[] VALID_EXTENSIONS = new string[] { "mp4", "avi", "m4v", "mkv" };

        public FileReaderPlugin(IConnectionInformation connectionInformation)
        {
            _connectionInfo = connectionInformation;
        }

        public IEnumerable<IMediaFile> GetAll()
        {
            UTorrent.Api.UTorrentClient utc = new UTorrent.Api.UTorrentClient(_connectionInfo.IP, _connectionInfo.Port, _connectionInfo.Login, _connectionInfo.Password);

            var torrentList = utc.GetList().Result.Torrents;
            return torrentList.Select(t => FromUtorrent(utc, t)).Where(mf => mf != null).ToArray();
        }

        private MediaFile FromUtorrent(UTorrentClient utc, Torrent t)
        {
            MediaFile mf = new MediaFile();
            if (t.Progress == 1000)
            {
                mf.MediaFileRecord.Hash = t.Hash;
                string keyword = t.Name.Split(SPLIT_CHARS)[0].ToLower();

                var candidateFiles = utc.GetTorrent(t.Hash).Result.Files[t.Hash].Where(f => Matches(f.Name, keyword));

                UTorrent.Api.Data.File chosenCandidate = candidateFiles.FirstOrDefault();
                if (candidateFiles.Count() > 1)
                {
                    chosenCandidate = candidateFiles.OrderByDescending(cf => cf.Size).First();
                }

                if (chosenCandidate == null)
                    return null;

                mf.MediaFileRecord.Name = chosenCandidate.NameWithoutPath;
                mf.MediaFileRecord.MediaType = "video/" + Path.GetExtension(chosenCandidate.Name).ToLower().Substring(1);
                mf.FilePath = chosenCandidate.Name;
                return mf;
            }

            return null;
        }

        private bool Matches(string filename, string keyword)
        {

            if (filename.Split(SPLIT_CHARS)[0].ToLower() == keyword &&
                VALID_EXTENSIONS.Contains(Path.GetExtension(filename).ToLower().Substring(1)))
                return true;
            else
                return false;
        }

        public IMediaFile GetByHash(string hash)
        {
            UTorrent.Api.UTorrentClient utc = new UTorrent.Api.UTorrentClient(_connectionInfo.IP, _connectionInfo.Port, _connectionInfo.Login, _connectionInfo.Password);

            var query = utc.GetTorrent(hash);

            return FromUtorrent(utc, query.Result.Torrents.First());
        }
    }
}
