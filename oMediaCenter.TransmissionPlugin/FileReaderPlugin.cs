using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace oMediaCenter.TransmissionPlugin
{
	public class FileReaderPlugin : IFileReaderPlugin
	{
		private ILogger<FileReaderPlugin> _logger;
		private TransmissionFileReaderPluginSettings _connectionInfo;

		public FileReaderPlugin(IConfigurationSection pluginConfigurationSection, ILoggerFactory loggerFactory)
		{
			if (pluginConfigurationSection != null)
			{
				_connectionInfo = new TransmissionFileReaderPluginSettings(pluginConfigurationSection);
			}
			_logger = loggerFactory.CreateLogger<FileReaderPlugin>();
		}

		public IEnumerable<IMediaFile> GetAll()
		{
			string host = _connectionInfo.IP;
			//Create Transsmission.API.RPC.Client (set host, optional session id,optional login and optional pass).
			Client client = new Client(host);

			//After initialization, client can call methods:
			//var sessionInfo = client.GetSessionInformation();
			var allTorrents = client.TorrentGet(TorrentFields.ALL_FIELDS);

			// get all completed torrents
			var completedTorrents = allTorrents.Torrents.Where(t => t.PercentDone == 1);

			// get all files from completed torrents that have valid file extensions
			var validFiles = completedTorrents.SelectMany(ti => ti.Files.Where(f => MediaFileRecord.VALID_EXTENSIONS.Contains(Path.GetExtension(f.Name).Substring(1))).Select(f => new MediaFile(ti, f)));

			return validFiles;
		}

		public IMediaFile GetByHash(string hash)
		{
			string[] idSplit = hash.Split("aAaA");

			//Create Transsmission.API.RPC.Client (set host, optional session id,optional login and optional pass).
			Client client = new Client(_connectionInfo.IP);

			//After initialization, client can call methods:
			var selectedTorrent = client.TorrentGet(TorrentFields.ALL_FIELDS, int.Parse(idSplit[0].Substring(2)));

            var foundFile = selectedTorrent.Torrents.First().Files[int.Parse(idSplit[1])];

			return new MediaFile(selectedTorrent.Torrents.First(), foundFile);
		}
	}
}
