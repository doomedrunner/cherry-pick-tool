import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { gitApi } from './api/gitApi';
import type { Commit, RepoConfig, SearchMode } from './types';

function App() {
  // Config state
  const [config, setConfig] = useState<RepoConfig>({
    repoPath: '',
    owner: '',
    repoName: '',
    gitHubToken: '',
    sourceBranch: 'main',
    targetBranch: 'stable',
    jiraPattern: 'betty-\\d+',
  });

  // Search state
  const [searchMode, setSearchMode] = useState<SearchMode>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  // Commits state
  const [commits, setCommits] = useState<Commit[]>([]);
  const [selectedShas, setSelectedShas] = useState<Set<string>>(new Set());

  // PR state
  const [prTitle, setPrTitle] = useState('');
  const [prDescription, setPrDescription] = useState('');
  const [lastPrUrl, setLastPrUrl] = useState<string | null>(null);

  // Status
  const [status, setStatus] = useState('Ready');

  // Mutations
  const openRepoMutation = useMutation({
    mutationFn: () => gitApi.openRepo(config.repoPath),
    onSuccess: (data) => {
      if (data.success) {
        setStatus('Repository opened successfully');
      } else {
        setStatus(`Error: ${data.error}`);
      }
    },
  });

  const loadCommitsMutation = useMutation({
    mutationFn: () => {
      const params: {
        sourceBranch: string;
        ticketId?: string;
        sha?: string;
        fromDate?: string;
        toDate?: string;
        jiraPattern?: string;
      } = {
        sourceBranch: config.sourceBranch,
        jiraPattern: config.jiraPattern,
      };

      if (searchMode === 'ticket' && searchQuery) {
        params.ticketId = searchQuery;
      } else if (searchMode === 'sha' && searchQuery) {
        params.sha = searchQuery;
      } else if (searchMode === 'date' && fromDate && toDate) {
        params.fromDate = fromDate;
        params.toDate = toDate;
      }

      return gitApi.getCommits(params);
    },
    onSuccess: (data) => {
      if (data.success && data.data) {
        setCommits(data.data);
        setStatus(`Found ${data.data.length} commits`);
      } else {
        setStatus(`Error: ${data.error}`);
      }
    },
  });

  const cherryPickMutation = useMutation({
    mutationFn: () =>
      gitApi.cherryPick({
        repoPath: config.repoPath,
        owner: config.owner,
        repoName: config.repoName,
        gitHubToken: config.gitHubToken,
        sourceBranch: config.sourceBranch,
        targetBranch: config.targetBranch,
        commitShas: Array.from(selectedShas),
        prTitle: prTitle || generatePrTitle(),
        prDescription,
        jiraPattern: config.jiraPattern,
      }),
    onSuccess: (data) => {
      if (data.success && data.data) {
        setStatus('PR created successfully!');
        setLastPrUrl(data.data.pullRequestUrl);
      } else {
        setStatus(`Error: ${data.error}`);
      }
    },
  });

  const generatePrTitle = () => {
    const tickets = commits
      .filter((c) => selectedShas.has(c.sha) && c.jiraTicketId)
      .map((c) => c.jiraTicketId);
    const uniqueTickets = [...new Set(tickets)];
    return uniqueTickets.length > 0
      ? `Cherry-pick: ${uniqueTickets.join(', ')}`
      : `Cherry-pick ${selectedShas.size} commit(s) to ${config.targetBranch}`;
  };

  const toggleCommit = (sha: string) => {
    setSelectedShas((prev) => {
      const next = new Set(prev);
      if (next.has(sha)) {
        next.delete(sha);
      } else {
        next.add(sha);
      }
      return next;
    });
  };

  const selectAll = () => setSelectedShas(new Set(commits.map((c) => c.sha)));
  const deselectAll = () => setSelectedShas(new Set());

  const isLoading =
    openRepoMutation.isPending || loadCommitsMutation.isPending || cherryPickMutation.isPending;

  return (
    <div className="min-h-screen bg-gray-100 p-6">
      <div className="max-w-6xl mx-auto space-y-6">
        <h1 className="text-3xl font-bold text-gray-800">Cherry-Pick Tool</h1>

        {/* Config Section */}
        <div className="bg-white rounded-lg shadow p-6 space-y-4">
          <h2 className="text-xl font-semibold text-gray-700">Repository Settings</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-600">Repo Path</label>
              <input
                type="text"
                value={config.repoPath}
                onChange={(e) => setConfig({ ...config, repoPath: e.target.value })}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                placeholder="/path/to/repo"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600">GitHub Token</label>
              <input
                type="password"
                value={config.gitHubToken}
                onChange={(e) => setConfig({ ...config, gitHubToken: e.target.value })}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                placeholder="ghp_xxxxx"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600">Owner</label>
              <input
                type="text"
                value={config.owner}
                onChange={(e) => setConfig({ ...config, owner: e.target.value })}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                placeholder="username or org"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600">Repository Name</label>
              <input
                type="text"
                value={config.repoName}
                onChange={(e) => setConfig({ ...config, repoName: e.target.value })}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                placeholder="repo-name"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600">Source Branch</label>
              <input
                type="text"
                value={config.sourceBranch}
                onChange={(e) => setConfig({ ...config, sourceBranch: e.target.value })}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600">Target Branch</label>
              <input
                type="text"
                value={config.targetBranch}
                onChange={(e) => setConfig({ ...config, targetBranch: e.target.value })}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          <button
            onClick={() => openRepoMutation.mutate()}
            disabled={isLoading || !config.repoPath}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            Open Repository
          </button>
        </div>

        {/* Search Section */}
        <div className="bg-white rounded-lg shadow p-6 space-y-4">
          <h2 className="text-xl font-semibold text-gray-700">Search Commits</h2>
          <div className="flex gap-4 items-end">
            <div>
              <label className="block text-sm font-medium text-gray-600">Search Mode</label>
              <select
                value={searchMode}
                onChange={(e) => setSearchMode(e.target.value as SearchMode)}
                className="mt-1 px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
              >
                <option value="all">All New Commits</option>
                <option value="ticket">By Ticket ID</option>
                <option value="sha">By SHA</option>
                <option value="date">By Date Range</option>
              </select>
            </div>

            {(searchMode === 'ticket' || searchMode === 'sha') && (
              <div className="flex-1">
                <label className="block text-sm font-medium text-gray-600">
                  {searchMode === 'ticket' ? 'Ticket ID' : 'Commit SHA'}
                </label>
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                  placeholder={searchMode === 'ticket' ? 'betty-1234' : 'abc123...'}
                />
              </div>
            )}

            {searchMode === 'date' && (
              <>
                <div>
                  <label className="block text-sm font-medium text-gray-600">From</label>
                  <input
                    type="date"
                    value={fromDate}
                    onChange={(e) => setFromDate(e.target.value)}
                    className="mt-1 px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-600">To</label>
                  <input
                    type="date"
                    value={toDate}
                    onChange={(e) => setToDate(e.target.value)}
                    className="mt-1 px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </>
            )}

            <button
              onClick={() => loadCommitsMutation.mutate()}
              disabled={isLoading}
              className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
            >
              Search
            </button>
          </div>
        </div>

        {/* Commits List */}
        <div className="bg-white rounded-lg shadow p-6 space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="text-xl font-semibold text-gray-700">
              Commits ({commits.length}) - Selected: {selectedShas.size}
            </h2>
            <div className="space-x-2">
              <button
                onClick={selectAll}
                className="px-3 py-1 text-sm bg-gray-200 rounded hover:bg-gray-300"
              >
                Select All
              </button>
              <button
                onClick={deselectAll}
                className="px-3 py-1 text-sm bg-gray-200 rounded hover:bg-gray-300"
              >
                Deselect All
              </button>
            </div>
          </div>

          <div className="border rounded-md max-h-80 overflow-y-auto">
            {commits.length === 0 ? (
              <p className="p-4 text-gray-500 text-center">No commits loaded. Search or load commits first.</p>
            ) : (
              commits.map((commit) => (
                <div
                  key={commit.sha}
                  onClick={() => toggleCommit(commit.sha)}
                  className={`flex items-center gap-4 p-3 border-b cursor-pointer hover:bg-gray-50 ${
                    selectedShas.has(commit.sha) ? 'bg-blue-50' : ''
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={selectedShas.has(commit.sha)}
                    onChange={() => toggleCommit(commit.sha)}
                    className="w-4 h-4"
                  />
                  <code className="text-sm text-blue-600 font-mono">{commit.shortSha}</code>
                  <span className="flex-1 truncate">{commit.message}</span>
                  {commit.jiraTicketId && (
                    <span className="px-2 py-1 text-xs bg-green-100 text-green-800 rounded">
                      {commit.jiraTicketId}
                    </span>
                  )}
                  <span className="text-sm text-gray-500">
                    {new Date(commit.committedAt).toLocaleDateString()}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>

        {/* PR Creation */}
        <div className="bg-white rounded-lg shadow p-6 space-y-4">
          <h2 className="text-xl font-semibold text-gray-700">Create Pull Request</h2>
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-600">PR Title</label>
              <input
                type="text"
                value={prTitle}
                onChange={(e) => setPrTitle(e.target.value)}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                placeholder="Auto-generated if empty"
              />
            </div>
            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-600">Description</label>
              <textarea
                value={prDescription}
                onChange={(e) => setPrDescription(e.target.value)}
                className="mt-1 w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500"
                rows={3}
                placeholder="Optional description"
              />
            </div>
          </div>

          <div className="flex gap-4 items-center">
            <button
              onClick={() => cherryPickMutation.mutate()}
              disabled={isLoading || selectedShas.size === 0 || !config.gitHubToken}
              className="px-6 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 disabled:opacity-50"
            >
              Create Pull Request
            </button>

            {lastPrUrl && (
              <a
                href={lastPrUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="px-4 py-2 bg-gray-800 text-white rounded-md hover:bg-gray-900"
              >
                Open PR
              </a>
            )}

            {isLoading && (
              <span className="text-gray-500">Processing...</span>
            )}
          </div>
        </div>

        {/* Status Bar */}
        <div className="bg-gray-800 text-white rounded-lg p-4">
          <strong>Status:</strong> {status}
        </div>
      </div>
    </div>
  );
}

export default App;
