using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace oMediaCenter.TransmissionPlugin
{
    public class FileReaderPlugin : IFileReaderPlugin
    {
        private ILogger<FileReaderPlugin> _logger;
        private TransmissionFileReaderPluginSettings _connectionInfo;
        ILoggerFactory _loggerFactory;

        /// <summary>
        /// All fields to request from the Transmission RPC API.
        /// </summary>
        private static readonly string[] AllFields = new[]
        {
            TorrentFields.ID,
            TorrentFields.NAME,
            TorrentFields.COMMENT,
            TorrentFields.DOWNLOAD_DIR,
            TorrentFields.PERCENT_DONE,
            TorrentFields.FILES,
        };

        public FileReaderPlugin(IConfigurationSection pluginConfigurationSection, ILoggerFactory loggerFactory)
        {
            if (pluginConfigurationSection != null)
            {
                _connectionInfo = new TransmissionFileReaderPluginSettings(pluginConfigurationSection);
            }
            _logger = loggerFactory.CreateLogger<FileReaderPlugin>();
            _loggerFactory = loggerFactory;
        }

        public IEnumerable<IMediaFile> GetAll()
        {
            string host = _connectionInfo.IP;
            //Create Transsmission.API.RPC.Client (set host, optional session id,optional login and optional pass).
            Client client = new Client(host);

            //After initialization, client can call methods:
            var allTorrents = client.TorrentGetAsync(AllFields, ids: null).GetAwaiter().GetResult();

            // get all completed torrents
            var completedTorrents = allTorrents.Torrents.Where(t => t.PercentDone == 1);

            // get all files from completed torrents that have valid file extensions
            var validFiles = completedTorrents.SelectMany(ti => ti.Files.Where(f => MediaFileRecord.VALID_EXTENSIONS.Contains(Path.GetExtension(f.Name).Substring(1))).Select(f => new MediaFile(_loggerFactory, ti, f)));

            return validFiles;
        }

        public IMediaFile GetByHash(string hash)
        {
            string[] idSplit = hash.Split("aAaA");

            if (idSplit.Length != 2 || idSplit[0].Length < 3
                || !int.TryParse(idSplit[0].Substring(2), out int torrentId)
                || !int.TryParse(idSplit[1], out int fileIndex))
            {
                return null;
            }

            //Create Transsmission.API.RPC.Client (set host, optional session id,optional login and optional pass).
            Client client = new Client(_connectionInfo.IP);

            //After initialization, client can call methods:
            var selectedTorrent = client.TorrentGetAsync(AllFields, new[] { torrentId }).GetAwaiter().GetResult();

            if (selectedTorrent.Torrents == null || !selectedTorrent.Torrents.Any()
                || fileIndex < 0 || fileIndex >= selectedTorrent.Torrents.First().Files.Length)
            {
                return null;
            }

            var foundFile = selectedTorrent.Torrents.First().Files[fileIndex];

            return new MediaFile(_loggerFactory, selectedTorrent.Torrents.First(), foundFile);
        }
    }
}
