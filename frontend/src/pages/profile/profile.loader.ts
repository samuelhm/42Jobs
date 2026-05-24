import type { ProfileData } from '../../types';

export interface ProfilePageData {
  profile: ProfileData | null;
}

export async function profileLoader(): Promise<ProfilePageData> {
  const res = await fetch('/api/profile').then(r => r.json());
  return { profile: res.success ? res.data : null };
}
