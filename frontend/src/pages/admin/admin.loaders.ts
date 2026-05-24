export async function aiServicesLoader() {
  const res = await fetch('/api/admin/ai-services').then(r => r.json());
  return { services: res.success ? res.data : [] };
}
