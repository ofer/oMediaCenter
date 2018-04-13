﻿using Microsoft.Extensions.Configuration;
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
			Client client = new Client("HOST", "PARAM_SESSION_ID", "PARAM_LOGIN", "PARAM_PASS");

			//After initialization, client can call methods:
			var sessionInfo = client.GetSessionInformation();
			var selectedTorrent = client.TorrentGet(TorrentFields.ALL_FIELDS, int.Parse(idSplit[0]));

			var foundFile = selectedTorrent.Torrents.First().Files.First(f => f.Name.GetHashCode() == int.Parse(idSplit.Last()));

			return new MediaFile(selectedTorrent.Torrents.First(), foundFile);
		}
	}
}