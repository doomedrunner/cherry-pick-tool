using CherryPickTool.Api.Models;
using CherryPickTool.Core.Models;
using CherryPickTool.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CherryPickTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitController : ControllerBase
{
    private readonly IGitService _gitService;
    private readonly IGitHubService _gitHubService;
    private readonly CherryPickOrchestrator _orchestrator;

    public GitController(IGitService gitService, IGitHubService gitHubService, CherryPickOrchestrator orchestrator)
    {
        _gitService = gitService;
        _gitHubService = gitHubService;
        _orchestrator = orchestrator;
    }

    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<bool>>> OpenRepository([FromBody] OpenRepoRequest request)
    {
        try
        {
            var result = await _gitService.OpenRepositoryAsync(request.Path);
            if (result)
            {
                await _gitService.FetchAsync();
            }
            return Ok(new ApiResponse<bool>(result, result, result ? null : "Could not open repository"));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<bool>(false, false, ex.Message));
        }
    }

    [HttpPost("commits")]
    public async Task<ActionResult<ApiResponse<List<CommitDto>>>> GetCommits([FromBody] SearchCommitsRequest request)
    {
        try
        {
            IReadOnlyList<CommitInfo> commits;

            if (!string.IsNullOrEmpty(request.Sha))
            {
                var commit = await _gitService.GetCommitByShaAsync(request.Sha, request.JiraPattern);
                commits = commit != null ? [commit] : [];
            }
            else if (!string.IsNullOrEmpty(request.TicketId))
            {
                commits = await _gitService.SearchCommitsByTicketAsync(
                    request.SourceBranch,
                    request.TicketId,
                    request.JiraPattern);
            }
            else if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                commits = await _gitService.GetCommitsByDateRangeAsync(
                    request.SourceBranch,
                    new DateTimeOffset(request.FromDate.Value),
                    new DateTimeOffset(request.ToDate.Value.AddDays(1)),
                    request.JiraPattern);
            }
            else
            {
                commits = await _gitService.GetCommitsAsync(
                    request.SourceBranch,
                    "stable",
                    request.JiraPattern);
            }

            var dtos = commits.Select(c => new CommitDto(
                c.Sha,
                c.ShortSha,
                c.Message,
                c.Author,
                c.CommittedAt,
                c.JiraTicketId
            )).ToList();

            return Ok(new ApiResponse<List<CommitDto>>(true, dtos));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<List<CommitDto>>(false, null, ex.Message));
        }
    }

    [HttpPost("cherry-pick")]
    public async Task<ActionResult<ApiResponse<CherryPickResultDto>>> CherryPick([FromBody] CherryPickApiRequest request)
    {
        try
        {
            // Open repo first
            var opened = await _gitService.OpenRepositoryAsync(request.RepoPath);
            if (!opened)
            {
                return Ok(new ApiResponse<CherryPickResultDto>(false, null, "Could not open repository"));
            }

            // Get commit info for each SHA
            var commits = new List<CommitInfo>();
            foreach (var sha in request.CommitShas)
            {
                var commit = await _gitService.GetCommitByShaAsync(sha, request.JiraPattern);
                if (commit != null)
                {
                    commits.Add(commit);
                }
            }

            if (commits.Count == 0)
            {
                return Ok(new ApiResponse<CherryPickResultDto>(false, null, "No valid commits found"));
            }

            var config = new RepositoryConfig
            {
                LocalPath = request.RepoPath,
                Owner = request.Owner,
                RepoName = request.RepoName,
                SourceBranch = request.SourceBranch,
                TargetBranch = request.TargetBranch,
                GitHubToken = request.GitHubToken,
                JiraTicketPattern = request.JiraPattern
            };

            var cherryPickRequest = new CherryPickRequest
            {
                Config = config,
                Commits = commits,
                PrTitle = request.PrTitle,
                PrDescription = request.PrDescription,
                BranchName = request.BranchName
            };

            var result = await _orchestrator.ExecuteAsync(cherryPickRequest);

            var dto = new CherryPickResultDto(
                result.Success,
                result.BranchName,
                result.PullRequestUrl,
                result.PullRequestNumber,
                result.ErrorMessage,
                result.FailedCommit != null ? new CommitDto(
                    result.FailedCommit.Sha,
                    result.FailedCommit.ShortSha,
                    result.FailedCommit.Message,
                    result.FailedCommit.Author,
                    result.FailedCommit.CommittedAt,
                    result.FailedCommit.JiraTicketId
                ) : null
            );

            return Ok(new ApiResponse<CherryPickResultDto>(result.Success, dto, result.ErrorMessage));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<CherryPickResultDto>(false, null, ex.Message));
        }
    }

    [HttpPost("validate-token")]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateToken([FromBody] string token)
    {
        try
        {
            _gitHubService.Initialize(token);
            var valid = await _gitHubService.ValidateTokenAsync();
            return Ok(new ApiResponse<bool>(valid, valid, valid ? null : "Invalid token"));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<bool>(false, false, ex.Message));
        }
    }
}
