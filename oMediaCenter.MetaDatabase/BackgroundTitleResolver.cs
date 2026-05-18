using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace oMediaCenter.MetaDatabase
{
	public class BackgroundTitleResolver : BackgroundService
	{
		private const string Prompt =
			"Given this video filename, identify the movie or TV show title. " +
			"Reply with ONLY the title, nothing else. If you cannot determine " +
			"the title, reply with exactly \"UNKNOWN\".";

		private readonly ConcurrentQueue<string> _queue = new();
		private readonly ConcurrentDictionary<string, byte> _pending = new();
		private readonly SemaphoreSlim _signal = new(0);
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly OpenAiOptions _options;
		private readonly TitleResolverCache _cache;
		private readonly ITitleResolutionNotifier _titleResolutionNotifier;
		private readonly ILogger<BackgroundTitleResolver> _logger;

		public BackgroundTitleResolver(
			IHttpClientFactory httpClientFactory,
			IOptions<OpenAiOptions> options,
			TitleResolverCache cache,
			ITitleResolutionNotifier titleResolutionNotifier,
			ILogger<BackgroundTitleResolver> logger)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value;
			_cache = cache;
			_titleResolutionNotifier = titleResolutionNotifier;
			_logger = logger;
		}

		public void Enqueue(string filename)
		{
			if (_pending.TryAdd(filename, 0))
			{
				_queue.Enqueue(filename);
				_signal.Release();
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					await _signal.WaitAsync(stoppingToken);
					if (_queue.TryDequeue(out var filename))
					{
						if (await ResolveAndCache(filename, stoppingToken))
							_pending.TryRemove(filename, out _);
						await Task.Delay(100, stoppingToken);
					}
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
			}
		}

		private async Task<bool> ResolveAndCache(string filename, CancellationToken cancellationToken)
		{
			try
			{
				var result = await CallOpenAi(filename, cancellationToken);
				if (string.IsNullOrWhiteSpace(result))
					return false;

				_cache.Set(filename, result);
				await _titleResolutionNotifier.MediaListUpdated(cancellationToken);
				return true;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogWarning(ex, "OpenAI title resolution failed for '{Filename}'", filename);
				return false;
			}
		}

		private async Task<string> CallOpenAi(string filename, CancellationToken cancellationToken)
		{
			var client = _httpClientFactory.CreateClient();
			client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

			var requestBody = new
			{
				model = _options.Model,
				messages = new[]
				{
					new { role = "system", content = Prompt },
					new { role = "user", content = $"Filename: \"{filename}\"" }
				},
				temperature = 0.0
			};

			var json = JsonSerializer.Serialize(requestBody);
			using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/v1/chat/completions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			if (!string.IsNullOrEmpty(_options.ApiKey))
				request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

			using var response = await client.SendAsync(request, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("OpenAI API returned {StatusCode} for '{Filename}'", response.StatusCode, filename);
				return null;
			}

			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

			var title = doc.RootElement
				.GetProperty("choices")[0]
				.GetProperty("message")
				.GetProperty("content")
				.GetString()
				?.Trim();

			return string.IsNullOrWhiteSpace(title) ? null : title;
		}
	}
}
