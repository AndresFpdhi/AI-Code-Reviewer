using System.Text.Json.Serialization;

namespace CodeReviewer.Core.Models;

public class AiReviewResult
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("comments")]
    public List<AiReviewComment> Comments { get; set; } = new();
}

public class AiReviewComment
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
