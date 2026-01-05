using Octokit;

namespace CherryPickTool.Core.Services;

/// <summary>
/// GitHub API operations using Octokit.
/// </summary>
public class GitHubService : IGitHubService
{
    private GitHubClient? _client;

    public void Initialize(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("CherryPickTool"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<(int Number, string Url)> CreatePullRequestAsync(
        string owner,
        string repo,
        string title,
        string body,
        string headBranch,
        string baseBranch)
    {
        EnsureInitialized();

        var pr = await _client!.PullRequest.Create(owner, repo, new NewPullRequest(title, headBranch, baseBranch)
        {
            Body = body
        });

        return (pr.Number, pr.HtmlUrl);
    }

    public async Task<bool> ValidateTokenAsync()
    {
        EnsureInitialized();

        try
        {
            await _client!.User.Current();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateRepositoryAsync(string owner, string repo)
    {
        EnsureInitialized();

        try
        {
            await _client!.Repository.Get(owner, repo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureInitialized()
    {
        if (_client == null)
            throw new InvalidOperationException("GitHub client not initialized. Call Initialize first.");
    }
}
