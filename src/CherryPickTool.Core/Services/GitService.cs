using System.Text.RegularExpressions;
using CherryPickTool.Core.Models;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace CherryPickTool.Core.Services;

/// <summary>
/// Git operations using LibGit2Sharp.
/// </summary>
public class GitService : IGitService, IDisposable
{
    private Repository? _repository;
    private string? _originalBranch;

    public Task<bool> OpenRepositoryAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                _repository?.Dispose();
                _repository = new Repository(path);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public Task FetchAsync()
    {
        return Task.Run(() =>
        {
            EnsureRepository();
            var remote = _repository!.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(_repository, remote.Name, refSpecs, new FetchOptions(), null);
        });
    }

    public Task<IReadOnlyList<CommitInfo>> GetCommitsAsync(
        string sourceBranch,
        string targetBranch,
        string? jiraTicketPattern = null)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            var sourceTip = GetBranchTip(sourceBranch);
            var targetTip = GetBranchTip(targetBranch);

            if (sourceTip == null || targetTip == null)
                return Array.Empty<CommitInfo>() as IReadOnlyList<CommitInfo>;

            // Get commits in source that are not in target
            var filter = new CommitFilter
            {
                IncludeReachableFrom = sourceTip,
                ExcludeReachableFrom = targetTip,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commits = _repository!.Commits.QueryBy(filter)
                .Select(c => ToCommitInfo(c, jiraTicketPattern))
                .ToList();

            return commits as IReadOnlyList<CommitInfo>;
        });
    }

    public Task<IReadOnlyList<CommitInfo>> SearchCommitsByTicketAsync(
        string sourceBranch,
        string ticketId,
        string jiraTicketPattern)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            var sourceTip = GetBranchTip(sourceBranch);
            if (sourceTip == null)
                return Array.Empty<CommitInfo>() as IReadOnlyList<CommitInfo>;

            var filter = new CommitFilter
            {
                IncludeReachableFrom = sourceTip,
                SortBy = CommitSortStrategies.Time
            };

            var ticketRegex = new Regex(ticketId, RegexOptions.IgnoreCase);

            var commits = _repository!.Commits.QueryBy(filter)
                .Where(c => ticketRegex.IsMatch(c.Message))
                .Take(100) // Limit results
                .Select(c => ToCommitInfo(c, jiraTicketPattern))
                .ToList();

            return commits as IReadOnlyList<CommitInfo>;
        });
    }

    public Task<IReadOnlyList<CommitInfo>> GetCommitsByDateRangeAsync(
        string sourceBranch,
        DateTimeOffset from,
        DateTimeOffset to,
        string? jiraTicketPattern = null)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            var sourceTip = GetBranchTip(sourceBranch);
            if (sourceTip == null)
                return Array.Empty<CommitInfo>() as IReadOnlyList<CommitInfo>;

            var filter = new CommitFilter
            {
                IncludeReachableFrom = sourceTip,
                SortBy = CommitSortStrategies.Time
            };

            var commits = _repository!.Commits.QueryBy(filter)
                .Where(c => c.Author.When >= from && c.Author.When <= to)
                .Select(c => ToCommitInfo(c, jiraTicketPattern))
                .ToList();

            return commits as IReadOnlyList<CommitInfo>;
        });
    }

    public Task<CommitInfo?> GetCommitByShaAsync(string sha, string? jiraTicketPattern = null)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            try
            {
                var commit = _repository!.Lookup<Commit>(sha);
                return commit != null ? ToCommitInfo(commit, jiraTicketPattern) : null;
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<string> CreateBranchAsync(string branchName, string baseBranch)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            // Store original branch
            _originalBranch = _repository!.Head.FriendlyName;

            // Get the base branch tip
            var baseTip = GetBranchTip(baseBranch);
            if (baseTip == null)
                throw new InvalidOperationException($"Branch '{baseBranch}' not found");

            // Create and checkout the new branch
            var branch = _repository.CreateBranch(branchName, baseTip);
            Commands.Checkout(_repository, branch);

            return branchName;
        });
    }

    public Task<CommitInfo?> CherryPickCommitsAsync(IEnumerable<CommitInfo> commits)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            var signature = _repository!.Config.BuildSignature(DateTimeOffset.Now);

            foreach (var commitInfo in commits)
            {
                var commit = _repository.Lookup<Commit>(commitInfo.Sha);
                if (commit == null)
                    return commitInfo;

                try
                {
                    var result = _repository.CherryPick(commit, signature);
                    if (result.Status == CherryPickStatus.Conflicts)
                    {
                        return commitInfo;
                    }
                }
                catch
                {
                    return commitInfo;
                }
            }

            return null; // All succeeded
        });
    }

    public Task PushBranchAsync(string branchName, string? gitHubToken = null)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            var remote = _repository!.Network.Remotes["origin"];
            var branch = _repository.Branches[branchName];

            var pushOptions = new PushOptions();

            if (!string.IsNullOrEmpty(gitHubToken))
            {
                pushOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials
                    {
                        Username = gitHubToken,
                        Password = string.Empty
                    };
            }

            _repository.Network.Push(branch, pushOptions);
        });
    }

    public Task AbortCherryPickAsync()
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            // Reset to HEAD to abort any in-progress cherry-pick
            _repository!.Reset(ResetMode.Hard);

            // Checkout original branch if we have one
            if (!string.IsNullOrEmpty(_originalBranch))
            {
                var branch = _repository.Branches[_originalBranch];
                if (branch != null)
                {
                    Commands.Checkout(_repository, branch);
                }
            }
        });
    }

    public Task CheckoutBranchAsync(string branchName)
    {
        return Task.Run(() =>
        {
            EnsureRepository();

            var branch = _repository!.Branches[branchName]
                ?? _repository.Branches[$"origin/{branchName}"];

            if (branch == null)
                throw new InvalidOperationException($"Branch '{branchName}' not found");

            Commands.Checkout(_repository, branch);
        });
    }

    private Commit? GetBranchTip(string branchName)
    {
        var branch = _repository!.Branches[branchName]
            ?? _repository.Branches[$"origin/{branchName}"];
        return branch?.Tip;
    }

    private static CommitInfo ToCommitInfo(Commit commit, string? jiraTicketPattern)
    {
        string? jiraTicket = null;
        if (!string.IsNullOrEmpty(jiraTicketPattern))
        {
            var match = Regex.Match(commit.Message, jiraTicketPattern, RegexOptions.IgnoreCase);
            if (match.Success)
                jiraTicket = match.Value;
        }

        return new CommitInfo
        {
            Sha = commit.Sha,
            Message = commit.MessageShort,
            Author = commit.Author.Name,
            CommittedAt = commit.Author.When,
            JiraTicketId = jiraTicket
        };
    }

    private void EnsureRepository()
    {
        if (_repository == null)
            throw new InvalidOperationException("Repository not opened. Call OpenRepositoryAsync first.");
    }

    public void Dispose()
    {
        _repository?.Dispose();
    }
}
