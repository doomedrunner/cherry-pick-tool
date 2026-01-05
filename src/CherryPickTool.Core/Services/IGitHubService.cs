namespace CherryPickTool.Core.Services;

/// <summary>
/// Service for GitHub API operations.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Initializes the GitHub client with the provided token.
    /// </summary>
    void Initialize(string token);

    /// <summary>
    /// Creates a pull request on GitHub.
    /// </summary>
    /// <returns>Tuple of (PR number, PR URL)</returns>
    Task<(int Number, string Url)> CreatePullRequestAsync(
        string owner,
        string repo,
        string title,
        string body,
        string headBranch,
        string baseBranch);

    /// <summary>
    /// Validates the GitHub token by attempting to get the authenticated user.
    /// </summary>
    Task<bool> ValidateTokenAsync();

    /// <summary>
    /// Gets repository information to validate owner/repo.
    /// </summary>
    Task<bool> ValidateRepositoryAsync(string owner, string repo);
}
