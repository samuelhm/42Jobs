import { useEffect, useState } from 'react';
import { get, put } from '../../utils';

interface Prompt { id: number; functionality: string; name: string; description: string | null; system_prompt: string; user_prompt_template: string; is_active: boolean; default_model_id: number | null; default_model_name: string | null; default_model_service: string | null; }

function PromptCard({ p }: { p: Prompt }) {
  const [system, setSystem] = useState(p.system_prompt);
  const [user, setUser] = useState(p.user_prompt_template);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  async function handleSave() {
    setSaving(true);
    setSaved(false);
    await put(`/api/admin/ai-prompts/${p.id}`, {
      system_prompt: system, user_prompt_template: user, is_active: p.is_active,
      default_model_id: p.default_model_id,
    });
    setSaving(false);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  return (
    <div className="service-card">
      <div className="service-header">
        <div>
          <h3>{p.functionality}</h3>
          <span className="service-count">{p.name}</span>
        </div>
        {p.default_model_name && (
          <span style={{ fontSize: '0.65rem', color: 'var(--teal)', fontFamily: "'JetBrains Mono', monospace", border: '1px solid rgba(77,184,160,0.25)', borderRadius: 'var(--radius)', padding: '0.15rem 0.5rem', background: 'rgba(77,184,160,0.08)' }}>
            {p.default_model_name}
          </span>
        )}
      </div>
      <label className="form-label">System Prompt</label>
      <textarea className="input" rows={5} value={system}
        onChange={e => setSystem(e.target.value)} />
      <label className="form-label mt-2">User Prompt Template</label>
      <textarea className="input" rows={8} value={user}
        onChange={e => setUser(e.target.value)} />
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.75rem' }}>
        <button className="admin-btn" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
        {saved && <span className="apikey-saved">Saved</span>}
      </div>
    </div>
  );
}

export default function AdminAiPrompts() {
  const [prompts, setPrompts] = useState<Prompt[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    get<Prompt[]>('/api/admin/ai-prompts').then(res => {
      if (res.success) setPrompts(res.data);
    }).catch(() => {}).finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Prompts</h2>
      <p className="text-muted">Edit the system prompt and user prompt template for each operation. Assign models in <b>AI Models</b> via drag & drop.</p>
      <div className="service-grid">
        {prompts.map(p => <PromptCard key={p.id} p={p} />)}
      </div>
    </div>
  );
}
