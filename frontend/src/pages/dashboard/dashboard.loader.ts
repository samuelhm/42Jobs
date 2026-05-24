import { fetchWithAuth } from '../../utils/fetchWithAuth';
import type { DashboardData, UserKeyword } from './dashboard.types';

function getCategoryId(url: string): string | null {
  return new URL(url).searchParams.get('category');
}

export async function dashboardLoader({ request }: { request: Request }): Promise<DashboardData> {
  const categoryId = getCategoryId(request.url);

  const [kwRes, jobsRes] = await Promise.all([
    fetchWithAuth('/api/keywords').then(r => r.json()),
    categoryId
      ? fetchWithAuth(`/api/categories/${categoryId}/jobs`).then(r => r.json())
      : Promise.resolve({ success: true, data: [] }),
  ]);

  const userKeywords: Record<string, string> = {};
  if (kwRes.success) {
    kwRes.data.forEach((k: UserKeyword) => { userKeywords[k.name] = k.learning_status; });
  }

  return {
    userKeywords,
    jobs: jobsRes.success ? jobsRes.data : [],
    categoryId,
  };
}
