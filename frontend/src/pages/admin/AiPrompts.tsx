import { useEffect, useState, useCallback } from 'react';
import { get, put } from './api';

interface Prompt { id: number; functionality: string; name: string; description: string | null; system_prompt: string; user_prompt_template: string; is_active: boolean; schema_id: number | null; schema_name: string | null; }

function useDebouncedSave(delay = 1000) {
  const [timer, setTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  return useCallback((fn: () => void) => {
    if (timer) clearTimeout(timer);
    setTimer(setTimeout(fn, delay));
  }, [delay]);
}

function PromptCard({ p }: { p: Prompt }) {
  const [system, setSystem] = useState(p.system_prompt);
  const [user, setUser] = useState(p.user_prompt_template);
  const [active, setActive] = useState(p.is_active);
  const [saved, setSaved] = useState(false);
  const debounce = useDebouncedSave();

  function save(s: string, u: string, a: boolean) {
    setSaved(false);
    debounce(async () => {
      await put(`/api/admin/ai-prompts/${p.id}`, { system_prompt: s, user_prompt_template: u, is_active: a, schema_id: p.schema_id });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

  return (
    <div className={`service-card ${active ? '' : 'inactive'}`}>
      <div className="service-header">
        <div>
          <h3>{p.functionality}</h3>
          <span className="service-count">{p.name} {p.schema_name ? `· schema: ${p.schema_name}` : ''}</span>
        </div>
        <button className={`toggle-switch ${active ? 'on' : ''}`}
          onClick={() => { const next = !active; setActive(next); save(system, user, next); }}>
          <span className="toggle-knob" />
        </button>
      </div>
      <label className="form-label">System Prompt</label>
      <textarea className="input" rows={5} value={system}
        onChange={e => { setSystem(e.target.value); save(e.target.value, user, active); }} />
      <label className="form-label mt-2">User Prompt Template</label>
      <textarea className="input" rows={8} value={user}
        onChange={e => { setUser(e.target.value); save(system, e.target.value, active); }} />
      {saved && <span className="apikey-saved">Saved</span>}
    </div>
  );
}

export default function AdminAiPrompts() {
  const [prompts, setPrompts] = useState<Prompt[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    get<Prompt[]>('/api/admin/ai-prompts').then(res => {
      if (res.success) setPrompts(res.data);
      setLoading(false);
    });
  }, []);

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Prompts</h2>
      <p className="text-muted">Edit the prompt templates used for each AI operation. Placeholders like {'{{keyword}}'} are filled at runtime.</p>
      <div className="service-grid">
        {prompts.map(p => <PromptCard key={p.id} p={p} />)}
      </div>
    </div>
  );
}
