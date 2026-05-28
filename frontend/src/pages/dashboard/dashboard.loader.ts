import { get } from '../../utils/api';
import type { DashboardData, UserKeyword } from './dashboard.types';

function getCategoryId(url: string): string | null {
  return new URL(url).searchParams.get('category');
}

export async function dashboardLoader({ request }: { request: Request }): Promise<DashboardData> {
  const categoryId = getCategoryId(request.url);

  const [kwRes, jobsRes] = await Promise.all([
    get<UserKeyword[]>('/api/keywords'),
    categoryId
      ? get<any[]>(`/api/categories/${categoryId}/jobs?showTracked=true`)
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
