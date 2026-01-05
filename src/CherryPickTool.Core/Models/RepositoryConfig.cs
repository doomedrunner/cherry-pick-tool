namespace CherryPickTool.Core.Models;

/// <summary>
/// Configuration for a repository and the cherry-pick workflow.
/// </summary>
public class RepositoryConfig
{
    /// <summary>
    /// Local path to the Git repository.
    /// </summary>
    public required string LocalPath { get; set; }

    /// <summary>
    /// GitHub repository owner (username or organization).
    /// </summary>
    public required string Owner { get; set; }

    /// <summary>
    /// GitHub repository name.
    /// </summary>
    public required string RepoName { get; set; }

    /// <summary>
    /// Source branch to cherry-pick commits from (default: "main").
    /// </summary>
    public string SourceBranch { get; set; } = "main";

    /// <summary>
    /// Target branch to cherry-pick commits to (default: "stable").
    /// </summary>
    public string TargetBranch { get; set; } = "stable";

    /// <summary>
    /// GitHub Personal Access Token for API authentication.
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>
    /// Pattern to match JIRA ticket IDs in commit messages (default: "betty-\d+").
    /// </summary>
    public string JiraTicketPattern { get; set; } = @"betty-\d+";
}
