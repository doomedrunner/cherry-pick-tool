using System.Collections.ObjectModel;
using CherryPickTool.Core.Models;
using CherryPickTool.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CherryPickTool.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGitService _gitService;
    private readonly IGitHubService _gitHubService;
    private readonly CherryPickOrchestrator _orchestrator;

    public MainViewModel(IGitService gitService, IGitHubService gitHubService, CherryPickOrchestrator orchestrator)
    {
        _gitService = gitService;
        _gitHubService = gitHubService;
        _orchestrator = orchestrator;
        _orchestrator.OnProgress += message => StatusMessage = message;
    }

    // Settings
    [ObservableProperty]
    private string _repoPath = string.Empty;

    [ObservableProperty]
    private string _gitHubToken = string.Empty;

    [ObservableProperty]
    private string _owner = string.Empty;

    [ObservableProperty]
    private string _repoName = string.Empty;

    [ObservableProperty]
    private string _sourceBranch = "main";

    [ObservableProperty]
    private string _targetBranch = "stable";

    [ObservableProperty]
    private string _jiraPattern = @"betty-\d+";

    // Search
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Now.AddDays(-30);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Now;

    [ObservableProperty]
    private int _selectedSearchMode; // 0=Ticket, 1=Hash, 2=DateRange

    // Commits
    public ObservableCollection<SelectableCommit> Commits { get; } = [];

    [ObservableProperty]
    private string _prTitle = string.Empty;

    [ObservableProperty]
    private string _prDescription = string.Empty;

    // Status
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _lastPrUrl;

    [RelayCommand]
    private async Task BrowseRepoAsync()
    {
        // In a real app, use FilePicker. For now, user types path manually.
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task LoadCommitsAsync()
    {
        if (string.IsNullOrWhiteSpace(RepoPath))
        {
            StatusMessage = "Please enter repository path";
            return;
        }

        IsLoading = true;
        Commits.Clear();

        try
        {
            var opened = await _gitService.OpenRepositoryAsync(RepoPath);
            if (!opened)
            {
                StatusMessage = "Could not open repository";
                return;
            }

            await _gitService.FetchAsync();
            StatusMessage = "Fetched latest changes";

            // Get commits not in target branch
            var commits = await _gitService.GetCommitsAsync(SourceBranch, TargetBranch, JiraPattern);

            foreach (var commit in commits)
            {
                Commits.Add(new SelectableCommit(commit));
            }

            StatusMessage = $"Found {commits.Count} commits not in {TargetBranch}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchCommitsAsync()
    {
        if (string.IsNullOrWhiteSpace(RepoPath))
        {
            StatusMessage = "Please enter repository path";
            return;
        }

        IsLoading = true;
        Commits.Clear();

        try
        {
            var opened = await _gitService.OpenRepositoryAsync(RepoPath);
            if (!opened)
            {
                StatusMessage = "Could not open repository";
                return;
            }

            IReadOnlyList<CommitInfo> commits;

            switch (SelectedSearchMode)
            {
                case 0: // Ticket search
                    if (string.IsNullOrWhiteSpace(SearchQuery))
                    {
                        StatusMessage = "Enter a ticket ID (e.g., betty-1234)";
                        return;
                    }
                    commits = await _gitService.SearchCommitsByTicketAsync(SourceBranch, SearchQuery, JiraPattern);
                    break;

                case 1: // SHA search
                    if (string.IsNullOrWhiteSpace(SearchQuery))
                    {
                        StatusMessage = "Enter a commit SHA";
                        return;
                    }
                    var commit = await _gitService.GetCommitByShaAsync(SearchQuery, JiraPattern);
                    commits = commit != null ? [commit] : [];
                    break;

                case 2: // Date range
                    commits = await _gitService.GetCommitsByDateRangeAsync(
                        SourceBranch,
                        new DateTimeOffset(FromDate),
                        new DateTimeOffset(ToDate.AddDays(1)),
                        JiraPattern);
                    break;

                default:
                    commits = [];
                    break;
            }

            foreach (var c in commits)
            {
                Commits.Add(new SelectableCommit(c));
            }

            StatusMessage = $"Found {commits.Count} commits";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var commit in Commits)
            commit.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var commit in Commits)
            commit.IsSelected = false;
    }

    [RelayCommand]
    private async Task CreatePullRequestAsync()
    {
        var selectedCommits = Commits.Where(c => c.IsSelected).Select(c => c.Commit).ToList();

        if (selectedCommits.Count == 0)
        {
            StatusMessage = "Select at least one commit";
            return;
        }

        if (string.IsNullOrWhiteSpace(GitHubToken))
        {
            StatusMessage = "GitHub token is required";
            return;
        }

        if (string.IsNullOrWhiteSpace(Owner) || string.IsNullOrWhiteSpace(RepoName))
        {
            StatusMessage = "Owner and repository name are required";
            return;
        }

        if (string.IsNullOrWhiteSpace(PrTitle))
        {
            // Auto-generate PR title from tickets
            var tickets = selectedCommits
                .Where(c => !string.IsNullOrEmpty(c.JiraTicketId))
                .Select(c => c.JiraTicketId)
                .Distinct();
            PrTitle = tickets.Any()
                ? $"Cherry-pick: {string.Join(", ", tickets)}"
                : $"Cherry-pick {selectedCommits.Count} commit(s) to {TargetBranch}";
        }

        IsLoading = true;
        LastPrUrl = null;

        try
        {
            var config = new RepositoryConfig
            {
                LocalPath = RepoPath,
                Owner = Owner,
                RepoName = RepoName,
                SourceBranch = SourceBranch,
                TargetBranch = TargetBranch,
                GitHubToken = GitHubToken,
                JiraTicketPattern = JiraPattern
            };

            var request = new CherryPickRequest
            {
                Config = config,
                Commits = selectedCommits,
                PrTitle = PrTitle,
                PrDescription = PrDescription
            };

            var result = await _orchestrator.ExecuteAsync(request);

            if (result.Success)
            {
                StatusMessage = $"PR created successfully!";
                LastPrUrl = result.PullRequestUrl;
            }
            else
            {
                StatusMessage = $"Failed: {result.ErrorMessage}";
                if (result.FailedCommit != null)
                {
                    StatusMessage += $" (commit: {result.FailedCommit.ShortSha})";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenPrAsync()
    {
        if (!string.IsNullOrEmpty(LastPrUrl))
        {
            await Launcher.OpenAsync(new Uri(LastPrUrl));
        }
    }
}

public partial class SelectableCommit : ObservableObject
{
    public CommitInfo Commit { get; }

    [ObservableProperty]
    private bool _isSelected;

    public SelectableCommit(CommitInfo commit)
    {
        Commit = commit;
    }

    public string ShortSha => Commit.ShortSha;
    public string Message => Commit.Message;
    public string Author => Commit.Author;
    public string Date => Commit.CommittedAt.ToString("yyyy-MM-dd HH:mm");
    public string? JiraTicket => Commit.JiraTicketId;
}
