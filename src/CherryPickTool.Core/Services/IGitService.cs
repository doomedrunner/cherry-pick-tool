using CherryPickTool.Core.Models;

namespace CherryPickTool.Core.Services;

/// <summary>
/// Service for local Git operations.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Opens a repository at the specified path.
    /// </summary>
    Task<bool> OpenRepositoryAsync(string path);

    /// <summary>
    /// Fetches the latest changes from the remote.
    /// </summary>
    Task FetchAsync();

    /// <summary>
    /// Gets commits from the source branch that are not in the target branch.
    /// </summary>
    Task<IReadOnlyList<CommitInfo>> GetCommitsAsync(
        string sourceBranch,
        string targetBranch,
        string? jiraTicketPattern = null);

    /// <summary>
    /// Searches commits by JIRA ticket ID pattern.
    /// </summary>
    Task<IReadOnlyList<CommitInfo>> SearchCommitsByTicketAsync(
        string sourceBranch,
        string ticketId,
        string jiraTicketPattern);

    /// <summary>
    /// Gets commits within a date range.
    /// </summary>
    Task<IReadOnlyList<CommitInfo>> GetCommitsByDateRangeAsync(
        string sourceBranch,
        DateTimeOffset from,
        DateTimeOffset to,
        string? jiraTicketPattern = null);

    /// <summary>
    /// Gets a commit by its SHA.
    /// </summary>
    Task<CommitInfo?> GetCommitByShaAsync(string sha, string? jiraTicketPattern = null);

    /// <summary>
    /// Creates a new branch from the target branch.
    /// </summary>
    Task<string> CreateBranchAsync(string branchName, string baseBranch);

    /// <summary>
    /// Cherry-picks commits onto the current branch.
    /// </summary>
    /// <returns>The commit that failed, or null if all succeeded.</returns>
    Task<CommitInfo?> CherryPickCommitsAsync(IEnumerable<CommitInfo> commits);

    /// <summary>
    /// Pushes the current branch to the remote.
    /// </summary>
    Task PushBranchAsync(string branchName, string? gitHubToken = null);

    /// <summary>
    /// Aborts the current cherry-pick operation and resets to the original state.
    /// </summary>
    Task AbortCherryPickAsync();

    /// <summary>
    /// Checks out a branch.
    /// </summary>
    Task CheckoutBranchAsync(string branchName);
}
