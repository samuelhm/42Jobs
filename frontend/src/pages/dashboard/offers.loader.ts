import { fetchWithAuth } from '../../utils/fetchWithAuth';
import type { UserKeyword, Job, Category } from '../../types';

export interface OffersData {
  userKeywords: Record<string, UserKeyword>;
  jobs: Job[];
  categoryId: string | null;
  lastFetchedAt: string | null;
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

  let lastFetchedAt: string | null = null;
  if (categoryId) {
    try {
      const catRes = await fetchWithAuth('/api/categories');
      const catData = await catRes.json();
      if (catData.success) {
        const cat = catData.data.find((c: Category) => String(c.id) === categoryId);
        lastFetchedAt = cat?.last_fetched_at ?? null;
      }
    } catch { /* non-critical */ }
  }

  return {
    userKeywords,
    jobs: jobsRes.success ? jobsRes.data : [],
    categoryId,
    lastFetchedAt,
  };
}
