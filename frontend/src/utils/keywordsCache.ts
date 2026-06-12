import { get } from './api';
import type { UserKeyword } from '../types';

let cached: UserKeyword[] | null = null;
let promise: Promise<UserKeyword[]> | null = null;

export function clearKeywordsCache() {
  cached = null;
  promise = null;
}

export async function getCachedKeywords(): Promise<UserKeyword[]> {
  if (cached) return cached;
  if (promise) return promise;

  promise = get<UserKeyword[]>('/api/keywords').then((res) => {
    cached = res.success ? res.data : [];
    promise = null;
    return cached;
  });

  return promise;
}
