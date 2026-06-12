import { getCachedKeywords } from '../../utils/keywordsCache';

interface KeywordItem {
  id: number;
  name: string;
  learning_status: string | null;
}

export interface KeywordsPageData {
  keywords: KeywordItem[];
}

export async function keywordsPageLoader(): Promise<KeywordsPageData> {
  const keywords = await getCachedKeywords();
  return { keywords };
}
