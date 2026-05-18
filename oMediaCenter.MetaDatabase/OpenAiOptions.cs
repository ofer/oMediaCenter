namespace oMediaCenter.MetaDatabase
{
	public class OpenAiOptions
	{
		public string ApiKey { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = "https://api.openai.com";
		public string Model { get; set; } = "gpt-4.1-nano";
		public int TimeoutSeconds { get; set; } = 5;
	}
}
