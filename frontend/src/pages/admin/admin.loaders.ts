import { fetchWithAuth } from '../../utils/fetchWithAuth';

export async function aiServicesLoader() {
  const res = await fetchWithAuth('/api/admin/ai-services').then(r => r.json());
  return { services: res.success ? res.data : [] };
}

export async function adminLogsLoader({ request }: { request: Request }) {
  const url = new URL(request.url);
  const params = new URLSearchParams();
  const actor = url.searchParams.get('actor');
  const action = url.searchParams.get('action');
  const payload2 = url.searchParams.get('payload2');
  const page = parseInt(url.searchParams.get('page') || '1');
  const limit = 100;
  const offset = (page - 1) * limit;

  if (actor) params.set('actor', actor);
  if (action) params.set('action', action);
  if (payload2) params.set('payload2', payload2);
  params.set('offset', String(offset));
  params.set('limit', String(limit));

  const res = await fetchWithAuth(`/api/admin/logs?${params.toString()}`).then(r => r.json());
  return {
    logs: res.success ? res.data : [],
    total: res.success ? res.total : 0,
    actors: res.success ? res.actors : [],
    actions: res.success ? res.actions : [],
    filters: { actor: actor || '', action: action || '', payload2: payload2 || '' },
    page,
    limit,
  };
}
