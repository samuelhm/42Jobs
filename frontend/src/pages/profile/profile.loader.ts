import { fetchWithAuth } from '../../utils/fetchWithAuth';
import type { ProfileData } from '../../types';

export interface ProfilePageData {
  profile: ProfileData | null;
}

export async function profileLoader(): Promise<ProfilePageData> {
  const res = await fetchWithAuth('/api/profile').then(r => r.json());
  return { profile: res.success ? res.data : null };
}
