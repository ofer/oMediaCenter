using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace oMediaCenter.MetaDatabase
{
  public class AiMediaInformationProvider : MediaInformationProviderBase, IMediaInformationProvider
  {
    private const int DefaultMaxBatchSize = 50;

    private readonly ILogger<AiMediaInformationProvider> _logger;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxBatchSize;

    public AiMediaInformationProvider(
      IConfiguration configuration,
      IDbContextFactory<MetaDataContext> dbContextFactory,
      ILogger<AiMediaInformationProvider> logger)
      : base(dbContextFactory)
    {
      _logger = logger;

      var section = configuration.GetSection("AiProvider");

      _endpoint = section["Endpoint"] ?? string.Empty;
      _apiKey = section["ApiKey"] ?? "none";
      _model = section["Model"] ?? string.Empty;

      if (!int.TryParse(section["MaxBatchSize"], out _maxBatchSize) || _maxBatchSize <= 0)
        _maxBatchSize = DefaultMaxBatchSize;
    }

    public MediaInformation GetEpisodeInfoForFilename(string filename)
    {
      if (string.IsNullOrWhiteSpace(filename))
        return new MediaInformation();

      var results = GetEpisodeInfoForFilenames(new[] { filename });
      if (results.TryGetValue(filename, out var info))
        return info;

      return new MediaInformation();
    }

    public IDictionary<string, MediaInformation> GetEpisodeInfoForFilenames(IEnumerable<string> filenames)
    {
      var nonEmptyFilenames = filenames
        .Where(f => !string.IsNullOrWhiteSpace(f))
        .Distinct(StringComparer.Ordinal)
        .ToList();

      var result = new Dictionary<string, MediaInformation>(StringComparer.Ordinal);
      foreach (var batch in nonEmptyFilenames.Chunk(_maxBatchSize))
      {
        try
        {
          var batchResult = CallAiForFilenames(batch);
          foreach (var kvp in batchResult)
            result[kvp.Key] = kvp.Value;
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "AI metadata extraction failed for batch of {BatchSize} filenames", batch.Length);

          foreach (var filename in batch)
            result[filename] = new MediaInformation();
        }
      }

      // Ensure every requested filename has a result even if the AI response is partial.
      foreach (var filename in nonEmptyFilenames)
      {
        if (!result.ContainsKey(filename))
          result[filename] = new MediaInformation();
      }

      return result;
    }

    private IDictionary<string, MediaInformation> CallAiForFilenames(IEnumerable<string> filenames)
    {
      var filenameList = filenames.ToList();
      var result = new Dictionary<string, MediaInformation>(StringComparer.Ordinal);

      if (!filenameList.Any() || string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_model))
        return result;

      var endpointUri = new Uri(_endpoint);
      var chatClient = new ChatClient(
        _model,
        new ApiKeyCredential(_apiKey),
        new OpenAI.OpenAIClientOptions { Endpoint = endpointUri });

      var systemPrompt = "You are a video file metadata extractor. Given a list of video filenames, extract metadata for each filename. Return ONLY a JSON object with a \"results\" array. Each entry must have: \"filename\" (exact input filename), \"title\" (clean movie/show name), \"year\" (4-digit string or null), \"season\" (2-digit string or null), \"episode\" (episode string or null).";
      var userPrompt = BuildUserPrompt(filenameList);

      _logger.LogInformation(
        "Sending AI metadata extraction request to {Endpoint} using model {Model}. System prompt: {SystemPrompt} User prompt: {UserPrompt}",
        _endpoint,
        _model,
        systemPrompt,
        userPrompt);

      var messages = new List<ChatMessage>
      {
        new SystemChatMessage(systemPrompt),
        new UserChatMessage(userPrompt)
      };

      var options = new ChatCompletionOptions
      {
        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
      };

      var completion = chatClient.CompleteChat(messages, options);
      var responseText = string.Concat(completion.Value.Content.Select(c => c.Text));

      _logger.LogInformation("Received AI metadata extraction response: {ResponseText}", responseText);

      if (string.IsNullOrWhiteSpace(responseText))
        return result;

      var parsed = JsonSerializer.Deserialize<AiResponse>(responseText, new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      });

      if (parsed?.Results == null)
        return result;

      foreach (var item in parsed.Results)
      {
        if (string.IsNullOrWhiteSpace(item.Filename))
          continue;

        var fileMetadata = new FileMetadata
        {
          Title = Normalize(item.Title),
          Year = Normalize(item.Year),
          Season = Normalize(item.Season),
          Episode = Normalize(item.Episode)
        };

        if (!string.IsNullOrWhiteSpace(fileMetadata.Title))
        {
          var databaseCandidate = SearchDatabaseForName(fileMetadata);
          if (databaseCandidate != null)
          {
            result[item.Filename] = databaseCandidate;
            continue;
          }
        }

        result[item.Filename] = CreateFallbackMediaInformation(fileMetadata);
      }

      return result;
    }

    private static string BuildUserPrompt(IReadOnlyList<string> filenames)
    {
      var numberedLines = filenames
        .Select((filename, index) => $"{index + 1}. {filename}");
      return "Filenames:\n" + string.Join("\n", numberedLines);
    }

    private static string Normalize(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return null;

      var trimmed = value.Trim();
      return string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
    }

    private class AiResponse
    {
      public List<AiFilenameResult> Results { get; set; }
    }

    private class AiFilenameResult
    {
      public string Filename { get; set; }
      public string Title { get; set; }
      public string Year { get; set; }
      public string Season { get; set; }
      public string Episode { get; set; }
    }
  }
}
