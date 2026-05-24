export interface TrackingData {
  jobs: Array<{ id: number; title: string; company_name: string; job_url: string }>;
}

export async function trackingLoader(): Promise<TrackingData> {
  const res = await fetch('/api/tracking').then(r => r.json());
  return { jobs: res.success ? res.data : [] };
}
