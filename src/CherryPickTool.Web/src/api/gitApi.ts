import axios from 'axios';
import type { ApiResponse, Commit, CherryPickResult } from '../types';

const api = axios.create({
  baseURL: '/api',
});

export const gitApi = {
  openRepo: async (path: string): Promise<ApiResponse<boolean>> => {
    const { data } = await api.post<ApiResponse<boolean>>('/git/open', { path });
    return data;
  },

  getCommits: async (params: {
    sourceBranch: string;
    ticketId?: string;
    sha?: string;
    fromDate?: string;
    toDate?: string;
    jiraPattern?: string;
  }): Promise<ApiResponse<Commit[]>> => {
    const { data } = await api.post<ApiResponse<Commit[]>>('/git/commits', params);
    return data;
  },

  cherryPick: async (params: {
    repoPath: string;
    owner: string;
    repoName: string;
    gitHubToken: string;
    sourceBranch: string;
    targetBranch: string;
    commitShas: string[];
    prTitle: string;
    prDescription?: string;
    branchName?: string;
    jiraPattern?: string;
  }): Promise<ApiResponse<CherryPickResult>> => {
    const { data } = await api.post<ApiResponse<CherryPickResult>>('/git/cherry-pick', params);
    return data;
  },

  validateToken: async (token: string): Promise<ApiResponse<boolean>> => {
    const { data } = await api.post<ApiResponse<boolean>>('/git/validate-token', JSON.stringify(token), {
      headers: { 'Content-Type': 'application/json' },
    });
    return data;
  },
};
