namespace CherryPickTool.Api.Models;

public record OpenRepoRequest(string Path);

public record SearchCommitsRequest(
    string SourceBranch,
    string? TicketId = null,
    string? Sha = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string JiraPattern = @"betty-\d+"
);

public record CherryPickApiRequest(
    string RepoPath,
    string Owner,
    string RepoName,
    string GitHubToken,
    string SourceBranch,
    string TargetBranch,
    List<string> CommitShas,
    string PrTitle,
    string? PrDescription = null,
    string? BranchName = null,
    string JiraPattern = @"betty-\d+"
);

public record CommitDto(
    string Sha,
    string ShortSha,
    string Message,
    string Author,
    DateTimeOffset CommittedAt,
    string? JiraTicketId
);

public record CherryPickResultDto(
    bool Success,
    string? BranchName,
    string? PullRequestUrl,
    int PullRequestNumber,
    string? ErrorMessage,
    CommitDto? FailedCommit
);

public record ApiResponse<T>(bool Success, T? Data, string? Error = null);
