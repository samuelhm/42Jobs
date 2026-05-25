import type { ApiResponse } from '../types';
import { fetchWithAuth } from './fetchWithAuth';

async function api<T>(url: string, options?: RequestInit): Promise<ApiResponse<T>> {
  const res = await fetchWithAuth(url, { headers: { 'Content-Type': 'application/json' }, ...options });
  if (res.status === 204) return { success: true, data: null as T };
  try { return await res.json(); } catch { return { success: false, data: null as T, error: `HTTP ${res.status}` }; }
}

export async function get<T>(url: string) { return api<T>(url); }
export async function post<T>(url: string, body: unknown) { return api<T>(url, { method: 'POST', body: JSON.stringify(body) }); }
export async function put<T>(url: string, body: unknown) { return api<T>(url, { method: 'PUT', body: JSON.stringify(body) }); }
export async function patch<T>(url: string, body: unknown) { return api<T>(url, { method: 'PATCH', body: JSON.stringify(body) }); }
export async function del<T>(url: string) { return api<T>(url, { method: 'DELETE' }); }
