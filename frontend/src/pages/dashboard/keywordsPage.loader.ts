import { get } from '../../utils/api';

interface KeywordItem {
  id: number;
  name: string;
  learning_status: string | null;
}

export interface KeywordsPageData {
  keywords: KeywordItem[];
}

export async function keywordsPageLoader(): Promise<KeywordsPageData> {
  const res = await get<KeywordItem[]>('/api/keywords');
  return { keywords: res.success ? res.data : [] };
}
