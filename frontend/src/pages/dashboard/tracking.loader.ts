import { fetchWithAuth } from '../../utils/fetchWithAuth';
import type { UserKeyword } from '../../types';
import type { TrackingJob } from './tracking.types';

export interface TrackingData {
  jobs: TrackingJob[];
  userKeywords: Record<string, UserKeyword>;
}

export async function trackingLoader(): Promise<TrackingData> {
  const [trackRes, kwRes] = await Promise.all([
    fetchWithAuth('/api/tracking').then(r => r.json()),
    fetchWithAuth('/api/keywords').then(r => r.json()),
  ]);

  const userKeywords: Record<string, UserKeyword> = {};
  if (kwRes.success) {
    kwRes.data.forEach((k: UserKeyword) => { userKeywords[k.name] = k; });
  }

  return {
    jobs: trackRes.success ? trackRes.data : [],
    userKeywords,
  };
}
