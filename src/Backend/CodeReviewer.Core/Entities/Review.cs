using System.ComponentModel.DataAnnotations;

namespace CodeReviewer.Core.Entities;

public class Review
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string RepoOwner { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string RepoName { get; set; } = string.Empty;

    public int PrNumber { get; set; }

    [Required, MaxLength(60)]
    public string HeadSha { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string PrTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string PrUrl { get; set; } = string.Empty;

    public int Score { get; set; }

    [Required]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string RawJson { get; set; } = string.Empty;

    public int CommentCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
