import { get } from '../../utils/api';
import { getCachedKeywords } from '../../utils/keywordsCache';
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

  const [keywords, jobsRes] = await Promise.all([
    getCachedKeywords(),
    categoryId
      ? get<Job[]>(`/api/categories/${categoryId}/jobs`)
      : Promise.resolve({ success: true, data: [] }),
  ]);

  const userKeywords: Record<string, UserKeyword> = {};
  keywords.forEach((k: UserKeyword) => { userKeywords[k.name] = k; });

  let lastFetchedAt: string | null = null;
  if (categoryId) {
    try {
      const catRes = await get<Category[]>('/api/categories');
      if (catRes.success) {
        const cat = catRes.data.find((c: Category) => String(c.id) === categoryId);
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
