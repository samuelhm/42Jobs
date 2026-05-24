import type { UserKeyword, Job } from '../../types';

export interface DashboardData {
  userKeywords: Record<string, string>;
  jobs: Job[];
  categoryId: string | null;
  error?: string;
}

export type { UserKeyword, Job };
