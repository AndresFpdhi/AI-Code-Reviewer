using CodeReviewer.Core.Models;

namespace CodeReviewer.Core.Services;

public interface IPromptBuilder
{
    (string SystemPrompt, string UserPrompt) Build(PullRequestContext context);
}
