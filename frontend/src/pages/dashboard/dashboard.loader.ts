import { get } from '../../utils/api';
import { getCachedKeywords } from '../../utils/keywordsCache';
import type { DashboardData, UserKeyword } from './dashboard.types';

function getCategoryId(url: string): string | null {
  return new URL(url).searchParams.get('category');
}

export async function dashboardLoader({ request }: { request: Request }): Promise<DashboardData> {
  const categoryId = getCategoryId(request.url);

  const [keywords, jobsRes] = await Promise.all([
    getCachedKeywords(),
    categoryId
      ? get<any[]>(`/api/categories/${categoryId}/jobs?showTracked=true`)
      : Promise.resolve({ success: true, data: [] }),
  ]);

  const userKeywords: Record<string, string> = {};
  keywords.forEach((k: UserKeyword) => { userKeywords[k.name] = k.learning_status; });

  return {
    userKeywords,
    jobs: jobsRes.success ? jobsRes.data : [],
    categoryId,
  };
}
