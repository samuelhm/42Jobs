import type { ApiResponse } from '../types';
import { fetchWithAuth } from './fetchWithAuth';

async function api<T>(url: string, options?: RequestInit): Promise<ApiResponse<T>> {
  const res = await fetchWithAuth(url, { headers: { 'Content-Type': 'application/json' }, ...options });
  return res.json();
}

export async function get<T>(url: string) { return api<T>(url); }
export async function post<T>(url: string, body: unknown) { return api<T>(url, { method: 'POST', body: JSON.stringify(body) }); }
export async function put<T>(url: string, body: unknown) { return api<T>(url, { method: 'PUT', body: JSON.stringify(body) }); }
export async function del<T>(url: string) { return api<T>(url, { method: 'DELETE' }); }
