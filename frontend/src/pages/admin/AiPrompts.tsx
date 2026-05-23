import { useEffect, useState } from 'react';
import { get, put } from './api';

interface AiPrompt {
  id: number; functionality: string; name: string; description: string | null;
  system_prompt: string; user_prompt_template: string; is_active: boolean;
  schema_id: number | null; schema_name: string | null;
}

export default function AdminAiPrompts() {
  const [prompts, setPrompts] = useState<AiPrompt[]>([]);
  const [edit, setEdit] = useState<AiPrompt | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<AiPrompt[]>('/api/admin/ai-prompts');
    if (res.success) setPrompts(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function save() {
    if (!edit) return;
    await put(`/api/admin/ai-prompts/${edit.id}`, {
      system_prompt: edit.system_prompt,
      user_prompt_template: edit.user_prompt_template,
      is_active: edit.is_active,
      schema_id: edit.schema_id,
    });
    setEdit(null);
    load();
  }

  if (loading) return <div className="p-4">Loading...</div>;

  return (
    <div>
      <h2>AI Prompts</h2>

      {edit && (
        <div className="card mb-4" style={{ padding: '1rem' }}>
          <h3>Edit: {edit.functionality}</h3>
          <label className="form-label">System Prompt</label>
          <textarea className="input" rows={4} value={edit.system_prompt}
            onChange={e => setEdit({ ...edit, system_prompt: e.target.value })} />
          <label className="form-label">User Prompt Template</label>
          <textarea className="input" rows={8} value={edit.user_prompt_template}
            onChange={e => setEdit({ ...edit, user_prompt_template: e.target.value })} />
          <div className="form-row mt-2">
            <label className="checkbox-label">
              <input type="checkbox" checked={edit.is_active} onChange={e => setEdit({ ...edit, is_active: e.target.checked })} /> Active
            </label>
            <button className="btn btn-primary" onClick={save}>Save</button>
            <button className="btn" onClick={() => setEdit(null)}>Cancel</button>
          </div>
        </div>
      )}

      <table className="admin-table">
        <thead>
          <tr><th>Functionality</th><th>Name</th><th>Schema</th><th>Active</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {prompts.map(p => (
            <tr key={p.id}>
              <td><code>{p.functionality}</code></td>
              <td>{p.name}</td>
              <td>{p.schema_name || '—'}</td>
              <td>{p.is_active ? '✅' : '❌'}</td>
              <td><button className="btn btn-sm" onClick={() => setEdit(p)}>Edit</button></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
