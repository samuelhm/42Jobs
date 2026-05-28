import { get } from '../../utils/api';
import type { ProfileData } from '../../types';

export interface ProfilePageData {
  profile: ProfileData | null;
}

export async function profileLoader(): Promise<ProfilePageData> {
  const res = await get<ProfileData>('/api/profile');
  return { profile: res.success ? res.data : null };
}
