import { fetchWithAuth } from '../../utils/fetchWithAuth';
import type { UserKeyword, Job } from '../../types';

export interface OffersData {
  userKeywords: Record<string, UserKeyword>;
  jobs: Job[];
  categoryId: string | null;
}

function getCategoryId(url: string): string | null {
  return new URL(url).searchParams.get('category');
}

export async function offersLoader({ request }: { request: Request }): Promise<OffersData> {
  const categoryId = getCategoryId(request.url);

  const [kwRes, jobsRes] = await Promise.all([
    fetchWithAuth('/api/keywords').then(r => r.json()),
    categoryId
      ? fetchWithAuth(`/api/categories/${categoryId}/jobs`).then(r => r.json())
      : Promise.resolve({ success: true, data: [] }),
  ]);

  const userKeywords: Record<string, UserKeyword> = {};
  if (kwRes.success) {
    kwRes.data.forEach((k: UserKeyword) => { userKeywords[k.name] = k; });
  }

  return {
    userKeywords,
    jobs: jobsRes.success ? jobsRes.data : [],
    categoryId,
  };
}
