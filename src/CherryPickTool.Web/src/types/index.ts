export interface Commit {
  sha: string;
  shortSha: string;
  message: string;
  author: string;
  committedAt: string;
  jiraTicketId: string | null;
}

export interface CherryPickResult {
  success: boolean;
  branchName: string | null;
  pullRequestUrl: string | null;
  pullRequestNumber: number;
  errorMessage: string | null;
  failedCommit: Commit | null;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: string | null;
}

export interface RepoConfig {
  repoPath: string;
  owner: string;
  repoName: string;
  gitHubToken: string;
  sourceBranch: string;
  targetBranch: string;
  jiraPattern: string;
}

export type SearchMode = 'ticket' | 'sha' | 'date' | 'all';
