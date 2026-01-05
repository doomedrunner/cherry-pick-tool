namespace CherryPickTool.Core.Models;

/// <summary>
/// Represents a Git commit with metadata for display and selection.
/// </summary>
public record CommitInfo
{
    public required string Sha { get; init; }
    public string ShortSha => Sha.Length >= 7 ? Sha[..7] : Sha;
    public required string Message { get; init; }
    public required string Author { get; init; }
    public required DateTimeOffset CommittedAt { get; init; }

    /// <summary>
    /// Extracted JIRA ticket ID (e.g., "betty-1234") from the commit message, if found.
    /// </summary>
    public string? JiraTicketId { get; init; }

    /// <summary>
    /// Whether this commit is selected for cherry-picking.
    /// </summary>
    public bool IsSelected { get; set; }
}
