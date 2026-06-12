import { get } from '../../utils/api';
import { getCachedKeywords } from '../../utils/keywordsCache';
import type { UserKeyword } from '../../types';
import type { TrackingJob } from './tracking.types';

export interface TrackingData {
  jobs: TrackingJob[];
  userKeywords: Record<string, UserKeyword>;
}

export async function trackingLoader(): Promise<TrackingData> {
  const [trackRes, keywords] = await Promise.all([
    get<TrackingJob[]>('/api/tracking'),
    getCachedKeywords(),
  ]);

  const userKeywords: Record<string, UserKeyword> = {};
  keywords.forEach((k: UserKeyword) => { userKeywords[k.name] = k; });

  return {
    jobs: trackRes.success ? trackRes.data : [],
    userKeywords,
  };
}
