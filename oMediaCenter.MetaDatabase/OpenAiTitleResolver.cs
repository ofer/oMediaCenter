using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using oMediaCenter.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace oMediaCenter.MetaDatabase
{
	public class OpenAiTitleResolver : ILlmTitleResolver
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly OpenAiOptions _options;
		private readonly TitleResolverCache _cache;
		private readonly ILogger<OpenAiTitleResolver> _logger;

		private const string Prompt =
			"Given this video filename, identify the movie or TV show title. " +
			"Reply with ONLY the title, nothing else. If you cannot determine " +
			"the title, reply with exactly \"UNKNOWN\".";

		public OpenAiTitleResolver(
			IHttpClientFactory httpClientFactory,
			IOptions<OpenAiOptions> options,
			TitleResolverCache cache,
			ILogger<OpenAiTitleResolver> logger)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value;
			_cache = cache;
			_logger = logger;
		}

		public string ResolveTitleFromFilename(string filename)
		{
			// Check cache first
			if (_cache.ContainsKey(filename))
			{
				var cached = _cache.Get(filename);
				return cached == "UNKNOWN" ? null : cached;
			}

			try
			{
				var result = CallOpenAi(filename);

				if (result == null)
					return null;

				// Cache the result (including "UNKNOWN")
				_cache.Set(filename, result);

				return result == "UNKNOWN" ? null : result;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "OpenAI title resolution failed for '{Filename}'", filename);
				return null;
			}
		}

		private string CallOpenAi(string filename)
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
			var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/v1/chat/completions")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			if (!string.IsNullOrEmpty(_options.ApiKey))
				request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

			var response = client.Send(request);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("OpenAI API returned {StatusCode} for '{Filename}'", response.StatusCode, filename);
				return null;
			}

			using var stream = response.Content.ReadAsStream();
			using var doc = JsonDocument.Parse(stream);

			var title = doc.RootElement
				.GetProperty("choices")[0]
				.GetProperty("message")
				.GetProperty("content")
				.GetString()
				?.Trim();

			if (string.IsNullOrWhiteSpace(title))
				return null;

			return title;
		}
	}
}
