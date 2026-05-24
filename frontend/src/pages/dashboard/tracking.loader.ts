import { fetchWithAuth } from '../../utils/fetchWithAuth';

export interface TrackingData {
  jobs: Array<{ id: number; title: string; company_name: string; job_url: string }>;
}

export async function trackingLoader(): Promise<TrackingData> {
  const res = await fetchWithAuth('/api/tracking').then(r => r.json());
  return { jobs: res.success ? res.data : [] };
}
