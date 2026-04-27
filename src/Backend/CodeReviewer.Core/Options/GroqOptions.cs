namespace CodeReviewer.Core.Options;

public class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public int MaxDiffChars { get; set; } = 60_000;
    public int MaxRetries { get; set; } = 3;
}
