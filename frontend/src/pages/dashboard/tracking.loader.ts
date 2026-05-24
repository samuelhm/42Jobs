import { fetchWithAuth } from '../../utils/fetchWithAuth';
import type { TrackingJob } from './tracking.types';

export interface TrackingData {
  jobs: TrackingJob[];
}

export async function trackingLoader(): Promise<TrackingData> {
  const res = await fetchWithAuth('/api/tracking').then(r => r.json());
  return { jobs: res.success ? res.data : [] };
}
