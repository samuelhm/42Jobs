interface KeywordItem {
  id: number;
  name: string;
  learning_status: string | null;
}

export interface KeywordsPageData {
  keywords: KeywordItem[];
}

export async function keywordsPageLoader(): Promise<KeywordsPageData> {
  const res = await fetch('/api/keywords').then(r => r.json());
  return { keywords: res.success ? res.data : [] };
}
