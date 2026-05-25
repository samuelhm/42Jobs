import { useEffect, useState } from 'react';
import { get, post, patch, del } from '../../utils';

interface AiModel { id: number; name: string; ai_service_name: string; ai_service_id: number; is_active: boolean; used_by: string[]; }
interface AiService { id: number; name: string; }
interface PromptOp { id: number; functionality: string; default_model_id: number | null; }

export default function AdminAiModels() {
  const [models, setModels] = useState<AiModel[]>([]);
  const [services, setServices] = useState<AiService[]>([]);
  const [prompts, setPrompts] = useState<PromptOp[]>([]);
  const [loading, setLoading] = useState(true);
  const [newName, setNewName] = useState('');
  const [newServiceId, setNewServiceId] = useState<number | null>(null);
  const [dragOver, setDragOver] = useState<number | null>(null);

  async function load() {
    const [mRes, sRes, pRes] = await Promise.all([
      get<AiModel[]>('/api/admin/ai-models'),
      get<AiService[]>('/api/admin/ai-services'),
      get<PromptOp[]>('/api/admin/ai-prompts'),
    ]);
    if (mRes.success) setModels(mRes.data);
    if (sRes.success) setServices(sRes.data);
    if (pRes.success) setPrompts(pRes.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  const assigned = (modelId: number) => prompts.filter(p => p.default_model_id === modelId);
  const unassigned = prompts.filter(p => !p.default_model_id);

  async function addModel() {
    if (!newName.trim() || !newServiceId) return;
    await post('/api/admin/ai-models', { name: newName.trim(), ai_service_id: newServiceId, is_active: true });
    setNewName('');
    load();
  }

  async function deleteModel(id: number) {
    if (!confirm('Delete this model?')) return;
    const opsToUnassign = prompts.filter(p => p.default_model_id === id);
    await Promise.all(opsToUnassign.map(p => patch(`/api/admin/ai-prompts/${p.id}/model`, { default_model_id: null })));
    setPrompts(prev => prev.map(p => p.default_model_id === id ? { ...p, default_model_id: null } : p));
    await del(`/api/admin/ai-models/${id}`);
    load();
  }

  function handleDragStart(e: React.DragEvent, id: number) {
    e.dataTransfer.setData('text/plain', String(id));
    e.dataTransfer.effectAllowed = 'move';
  }

  function handleDragOver(e: React.DragEvent, modelId: number) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOver(modelId);
  }

  function handleDragLeave() {
    setDragOver(null);
  }

  async function handleDrop(e: React.DragEvent, modelId: number | null) {
    e.preventDefault();
    setDragOver(null);
    const promptId = Number(e.dataTransfer.getData('text/plain'));
    if (!promptId) return;
    setPrompts(prev => prev.map(p => p.id === promptId ? { ...p, default_model_id: modelId } : p));
    await patch(`/api/admin/ai-prompts/${promptId}/model`, { default_model_id: modelId });
  }

  async function removeAssignment(promptId: number) {
    setPrompts(prev => prev.map(p => p.id === promptId ? { ...p, default_model_id: null } : p));
    await patch(`/api/admin/ai-prompts/${promptId}/model`, { default_model_id: null });
  }

  const grouped = models.reduce((acc: Record<string, AiModel[]>, m) => {
    (acc[m.ai_service_name] ??= []).push(m);
    return acc;
  }, {});

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Models</h2>
      <p className="text-muted">Add or remove models. Drag operation tags into a model to assign them.</p>

      <div className="service-card" style={{ marginBottom: '1rem' }}>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <select className="input" style={{ maxWidth: 200 }}
            value={newServiceId ?? ''}
            onChange={e => setNewServiceId(e.target.value ? +e.target.value : null)}>
            <option value="">— service —</option>
            {services.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <input className="input" style={{ maxWidth: 250 }} placeholder="Model name (e.g. gpt-5.4)"
            value={newName} onChange={e => setNewName(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && addModel()} />
          <button className="admin-btn" onClick={addModel}>Add</button>
        </div>
      </div>

      {unassigned.length > 0 && (
        <div
          className="model-drop-zone unassigned-zone"
          onDragOver={(e) => { e.preventDefault(); e.dataTransfer.dropEffect = 'move'; setDragOver(-1); }}
          onDragLeave={() => setDragOver(null)}
          onDrop={(e) => handleDrop(e, null)}
        >
          <span className="drop-label">Unassigned — drag here to detach</span>
          <div className="drop-tags">
            {unassigned.map(p => (
              <span
                key={p.id}
                className="op-tag draggable"
                draggable
                onDragStart={(e) => handleDragStart(e, p.id)}
                title="Drag to a model to assign"
              >
                <span className="drag-handle">⠿</span>
                {p.functionality}
              </span>
            ))}
          </div>
        </div>
      )}

      <div className="service-grid" style={{ marginTop: '0.75rem' }}>
        {Object.entries(grouped).map(([service, items]) => (
          <div key={service} className="service-card">
            <h3>{service}</h3>
            <div className="model-list">
              {items.map(m => (
                <div key={m.id} className={`model-row${dragOver === m.id ? ' drag-over' : ''}`}
                  onDragOver={(e) => handleDragOver(e, m.id)}
                  onDragLeave={handleDragLeave}
                  onDrop={(e) => handleDrop(e, m.id)}
                >
                  <div className="model-info">
                    <span className="model-name">{m.name}</span>
                    {!m.is_active && <span style={{ fontSize: '0.6rem', color: 'var(--red)', marginLeft: '0.5rem' }}>inactive</span>}
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', flex: 1, justifyContent: 'flex-end' }}>
                    <div className="model-usage">
                      {assigned(m.id).length > 0
                        ? assigned(m.id).map(p => (
                            <span key={p.id} className="op-tag assigned draggable"
                              draggable
                              onDragStart={(e) => handleDragStart(e, p.id)}
                              onClick={(e) => { e.stopPropagation(); removeAssignment(p.id); }}
                              title="Click to detach, drag to reassign"
                            >
                              <span className="drag-handle">⠿</span>
                              {p.functionality}
                            </span>
                          ))
                        : <span className="text-dim" style={{ fontSize: '0.7rem' }}>drop operations here</span>}
                    </div>
                    <button className="btn-delete" onClick={() => deleteModel(m.id)}>Delete</button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
