namespace CherryPickTool.Core.Models;

/// <summary>
/// Result of a cherry-pick operation.
/// </summary>
public record CherryPickResult
{
    public bool Success { get; init; }
    public string? BranchName { get; init; }
    public string? PullRequestUrl { get; init; }
    public int PullRequestNumber { get; init; }
    public string? ErrorMessage { get; init; }
    public CommitInfo? FailedCommit { get; init; }

    public static CherryPickResult Succeeded(string branchName, string prUrl, int prNumber) => new()
    {
        Success = true,
        BranchName = branchName,
        PullRequestUrl = prUrl,
        PullRequestNumber = prNumber
    };

    public static CherryPickResult Failed(string error, CommitInfo? failedCommit = null) => new()
    {
        Success = false,
        ErrorMessage = error,
        FailedCommit = failedCommit
    };
}
