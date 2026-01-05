namespace CherryPickTool.Core.Models;

/// <summary>
/// Request to cherry-pick commits and create a pull request.
/// </summary>
public class CherryPickRequest
{
    /// <summary>
    /// Repository configuration.
    /// </summary>
    public required RepositoryConfig Config { get; init; }

    /// <summary>
    /// Commits to cherry-pick (in order).
    /// </summary>
    public required IReadOnlyList<CommitInfo> Commits { get; init; }

    /// <summary>
    /// Name for the new branch (auto-generated if null).
    /// </summary>
    public string? BranchName { get; init; }

    /// <summary>
    /// Title for the pull request.
    /// </summary>
    public required string PrTitle { get; init; }

    /// <summary>
    /// Description for the pull request body.
    /// </summary>
    public string? PrDescription { get; init; }
}
