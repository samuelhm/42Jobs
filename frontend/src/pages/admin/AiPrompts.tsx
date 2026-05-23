import { useEffect, useState, useCallback } from 'react';
import { get, put } from './api';

interface Prompt { id: number; functionality: string; name: string; description: string | null; system_prompt: string; user_prompt_template: string; is_active: boolean; schema_id: number | null; schema_name: string | null; default_model_id: number | null; default_model_name: string | null; default_model_service: string | null; }
interface AiModel { id: number; name: string; ai_service_name: string; }
interface Schema { id: number; name: string; description: string | null; json_schema: any; }

function useDebouncedSave(delay = 1000) {
  const [timer, setTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  return useCallback((fn: () => void) => {
    if (timer) clearTimeout(timer);
    setTimer(setTimeout(fn, delay));
  }, [delay]);
}

function PromptCard({ p, models, schema }: { p: Prompt; models: AiModel[]; schema?: Schema }) {
  const [system, setSystem] = useState(p.system_prompt);
  const [user, setUser] = useState(p.user_prompt_template);
  const [modelId, setModelId] = useState(p.default_model_id);
  const [saved, setSaved] = useState(false);
  const debounce = useDebouncedSave();

  function doSave(s: string, u: string, m: number | null) {
    setSaved(false);
    debounce(async () => {
      await put(`/api/admin/ai-prompts/${p.id}`, {
        system_prompt: s, user_prompt_template: u, is_active: p.is_active,
        schema_id: p.schema_id, default_model_id: m
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

  return (
    <div className="service-card">
      <div className="service-header">
        <div>
          <h3>{p.functionality}</h3>
          <span className="service-count">{p.name}{p.schema_name ? ` · schema: ${p.schema_name}` : ''}</span>
        </div>
        <div className="model-selector">
          <select className="input" value={modelId ?? ''}
            onChange={e => { const id = e.target.value ? +e.target.value : null; setModelId(id); doSave(system, user, id); }}>
            <option value="">— select model —</option>
            {models.map(m => (
              <option key={m.id} value={m.id}>{m.name} ({m.ai_service_name})</option>
            ))}
          </select>
        </div>
      </div>
      <label className="form-label">System Prompt</label>
      <textarea className="input" rows={5} value={system}
        onChange={e => { setSystem(e.target.value); doSave(e.target.value, user, modelId); }} />
      <label className="form-label mt-2">User Prompt Template</label>
      <textarea className="input" rows={8} value={user}
        onChange={e => { setUser(e.target.value); doSave(system, e.target.value, modelId); }} />
      {saved && <span className="apikey-saved">Saved</span>}
      {schema && (
        <details className="schema-accordion">
          <summary className="schema-summary">
            <span className="schema-icon">&#9660;</span>
            View Schema: {schema.name} {schema.description ? ` — ${schema.description}` : ''}
          </summary>
          <pre className="schema-body">
            {JSON.stringify(schema.json_schema, null, 2)}
          </pre>
        </details>
      )}
    </div>
  );
}

export default function AdminAiPrompts() {
  const [prompts, setPrompts] = useState<Prompt[]>([]);
  const [models, setModels] = useState<AiModel[]>([]);
  const [schemas, setSchemas] = useState<Schema[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      get<Prompt[]>('/api/admin/ai-prompts'),
      get<AiModel[]>('/api/admin/ai-models'),
      get<Schema[]>('/api/admin/ai-schemas')
    ]).then(([pRes, mRes, sRes]) => {
      if (pRes.success) setPrompts(pRes.data);
      if (mRes.success) setModels(mRes.data);
      if (sRes.success) setSchemas(sRes.data);
      setLoading(false);
    });
  }, []);

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Prompts</h2>
      <p className="text-muted">Assign a model to each operation and edit the prompt templates.</p>
      <div className="service-grid">
        {prompts.map(p => <PromptCard key={p.id} p={p} models={models} schema={schemas.find(s => s.id === p.schema_id)} />)}
      </div>
    </div>
  );
}
