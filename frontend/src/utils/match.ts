import type { Job, UserKeyword } from '../types';

export function getMatchPct(job: Job, userKeywords: Record<string, UserKeyword | string>): number {
  if (!job.keywords || job.keywords.length === 0) return 0;
  let matchCount = 0;
  job.keywords.forEach((kw) => {
    const entry = userKeywords[kw];
    const status = typeof entry === 'string' ? entry : entry?.learning_status;
    if (status && status !== 'not_learned') matchCount++;
  });
  return Math.round((matchCount / job.keywords.length) * 100);
}

export function getMatchClass(pct: number): string {
  if (pct >= 50) return 'high';
  if (pct >= 20) return 'medium';
  return 'low';
}

export function isRecent(dateStr: string | null): boolean {
  if (!dateStr) return false;
  const d = new Date(dateStr + 'T00:00:00');
  const now = new Date();
  return (now.getTime() - d.getTime()) < 48 * 60 * 60 * 1000;
}
