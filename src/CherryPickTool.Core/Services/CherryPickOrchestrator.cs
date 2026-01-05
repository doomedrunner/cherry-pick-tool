using CherryPickTool.Core.Models;

namespace CherryPickTool.Core.Services;

/// <summary>
/// Orchestrates the cherry-pick workflow: branch creation, cherry-pick, push, and PR creation.
/// </summary>
public class CherryPickOrchestrator
{
    private readonly IGitService _gitService;
    private readonly IGitHubService _gitHubService;

    public event Action<string>? OnProgress;

    public CherryPickOrchestrator(IGitService gitService, IGitHubService gitHubService)
    {
        _gitService = gitService;
        _gitHubService = gitHubService;
    }

    /// <summary>
    /// Executes the full cherry-pick workflow.
    /// </summary>
    public async Task<CherryPickResult> ExecuteAsync(CherryPickRequest request)
    {
        var branchName = request.BranchName ?? GenerateBranchName(request);

        try
        {
            // Step 1: Open repository
            ReportProgress("Opening repository...");
            var opened = await _gitService.OpenRepositoryAsync(request.Config.LocalPath);
            if (!opened)
            {
                return CherryPickResult.Failed($"Could not open repository at '{request.Config.LocalPath}'");
            }

            // Step 2: Initialize GitHub client
            ReportProgress("Initializing GitHub client...");
            if (string.IsNullOrEmpty(request.Config.GitHubToken))
            {
                return CherryPickResult.Failed("GitHub token is required");
            }
            _gitHubService.Initialize(request.Config.GitHubToken);

            // Validate token
            if (!await _gitHubService.ValidateTokenAsync())
            {
                return CherryPickResult.Failed("Invalid GitHub token");
            }

            // Step 3: Fetch latest changes
            ReportProgress("Fetching latest changes from remote...");
            await _gitService.FetchAsync();

            // Step 4: Create new branch from target branch
            ReportProgress($"Creating branch '{branchName}' from '{request.Config.TargetBranch}'...");
            await _gitService.CreateBranchAsync(branchName, request.Config.TargetBranch);

            // Step 5: Cherry-pick commits
            ReportProgress($"Cherry-picking {request.Commits.Count} commit(s)...");
            var failedCommit = await _gitService.CherryPickCommitsAsync(request.Commits);

            if (failedCommit != null)
            {
                // Abort and report failure
                ReportProgress("Cherry-pick failed, aborting...");
                await _gitService.AbortCherryPickAsync();
                return CherryPickResult.Failed(
                    $"Cherry-pick failed on commit '{failedCommit.ShortSha}': {failedCommit.Message}",
                    failedCommit);
            }

            // Step 6: Push branch to remote
            ReportProgress($"Pushing branch '{branchName}' to remote...");
            await _gitService.PushBranchAsync(branchName, request.Config.GitHubToken);

            // Step 7: Create pull request
            ReportProgress("Creating pull request...");
            var prBody = BuildPrBody(request);
            var (prNumber, prUrl) = await _gitHubService.CreatePullRequestAsync(
                request.Config.Owner,
                request.Config.RepoName,
                request.PrTitle,
                prBody,
                branchName,
                request.Config.TargetBranch);

            ReportProgress($"Pull request created: {prUrl}");
            return CherryPickResult.Succeeded(branchName, prUrl, prNumber);
        }
        catch (Exception ex)
        {
            ReportProgress($"Error: {ex.Message}");
            try
            {
                await _gitService.AbortCherryPickAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
            return CherryPickResult.Failed(ex.Message);
        }
    }

    private static string GenerateBranchName(CherryPickRequest request)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var tickets = request.Commits
            .Where(c => !string.IsNullOrEmpty(c.JiraTicketId))
            .Select(c => c.JiraTicketId)
            .Distinct()
            .Take(3);

        var ticketPart = string.Join("-", tickets);
        return string.IsNullOrEmpty(ticketPart)
            ? $"cherry-pick/{timestamp}"
            : $"cherry-pick/{ticketPart}-{timestamp}";
    }

    private static string BuildPrBody(CherryPickRequest request)
    {
        var body = request.PrDescription ?? "Cherry-picked commits from main to stable.";
        body += "\n\n## Commits\n";

        foreach (var commit in request.Commits)
        {
            var ticket = !string.IsNullOrEmpty(commit.JiraTicketId)
                ? $"[{commit.JiraTicketId}] "
                : "";
            body += $"- `{commit.ShortSha}` {ticket}{commit.Message}\n";
        }

        return body;
    }

    private void ReportProgress(string message)
    {
        OnProgress?.Invoke(message);
    }
}
