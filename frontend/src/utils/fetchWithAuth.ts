export async function fetchWithAuth(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const res = await fetch(input, init);

  if (res.status === 401) {
    await fetch('/api/users/logout', { method: 'POST' }).catch(() => {});
    window.location.href = '/login';
    return new Promise(() => {});
  }

  return res;
}
