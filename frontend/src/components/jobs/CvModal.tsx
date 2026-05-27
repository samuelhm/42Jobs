import { useState, useEffect } from 'react';
import { fetchWithAuth } from '../../utils/fetchWithAuth';
import AiNotConfiguredModal from '../ui/AiNotConfiguredModal';
import { useAuth } from '../../context';

interface Template {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
}

interface Props {
  jobId: number;
  jobTitle: string;
  onClose: () => void;
  onGenerated?: () => void;
}

function sanitizeHtml(html: string): string {
  return html
    .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
    .replace(/<([\w-]+)([^>]*?)>/gi, (_m, tag, attrs) => {
      const cleaned = attrs
        .replace(/\s*on\w+\s*=\s*("[^"]*"|'[^']*'|[^\s>]*)/gi, '')
        .replace(/(?:href|src|xlink:href)\s*=\s*["']javascript:[^"']*["']/gi, 'href=""');
      return `<${tag}${cleaned}>`;
    });
}

export default function CvModal({ jobId, jobTitle, onClose }: Props) {
  const { user } = useAuth();
  const [html, setHtml] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [model, setModel] = useState('');
  const [exists, setExists] = useState(false);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState<number | null>(null);
  const [aiError, setAiError] = useState('');

  useEffect(() => {
    checkExisting();
  }, []);

  async function checkExisting() {
    try {
      const [res, tRes] = await Promise.all([
        fetchWithAuth(`/api/resumes/job/${jobId}`),
        fetchWithAuth('/api/resumes/templates'),
      ]);
      if (tRes.ok) {
        const tData = await tRes.json();
        if (tData.data) setTemplates(tData.data);
      }
      if (res.ok) {
        const data = await res.json();
        if (data.html) {
          setHtml(sanitizeHtml(data.html));
          setModel(data.model || '');
          setExists(true);
          if (data.templateId != null) setSelectedTemplateId(data.templateId);
        }
      }
    } catch { /* no CV yet */ }
    setLoading(false);
  }

  async function generateCv() {
    setLoading(true);
    setError('');
    try {
      const res = await fetchWithAuth(`/api/resumes/${jobId}`, {
        method: 'POST',
      });
      if (res.status === 503) {
        const data = await res.json();
        setAiError(data.error || 'AI not configured');
        setLoading(false);
        return;
      }
      const data = await res.json();
      if (data.html) {
        setHtml(sanitizeHtml(data.html));
        setModel(data.model || '');
        setExists(true);
      } else {
        setError(data.error || 'Generation failed');
      }
    } catch {
      setError('Connection error');
    } finally {
      setLoading(false);
    }
  }

  async function regenerateCv() {
    setLoading(true);
    setError('');
    try {
      const res = await fetchWithAuth(`/api/resumes/${jobId}/regenerate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ templateId: selectedTemplateId }),
      });
      const data = await res.json();
      if (data.html) {
        setHtml(sanitizeHtml(data.html));
        setModel(data.model || '');
        if (data.templateId != null) setSelectedTemplateId(data.templateId);
        setExists(true);
      } else {
        setError(data.error || 'Regeneration failed');
      }
    } catch {
      setError('Connection error');
    } finally {
      setLoading(false);
    }
  }

  async function forceRegenerate() {
    setLoading(true);
    setError('');
    try {
      const res = await fetchWithAuth(`/api/resumes/${jobId}?force=true`, {
        method: 'POST',
      });
      const data = await res.json();
      if (data.html) {
        setHtml(sanitizeHtml(data.html));
        setModel(data.model || '');
        setExists(true);
      } else {
        setError(data.error || 'Force regeneration failed');
      }
    } catch {
      setError('Connection error');
    } finally {
      setLoading(false);
    }
  }

  function download() {
    const blob = new Blob([html], { type: 'text/html' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `cv_${jobTitle.replace(/\s+/g, '_').toLowerCase()}.html`;
    a.click();
  }

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="cv-preview" onClick={(e) => e.stopPropagation()}>
        <div className="cv-toolbar">
          <h3>CV Preview {model && <span style={{ fontSize: '0.7rem', color: 'var(--text-dim)' }}>({model})</span>}</h3>
          <div>
            {html && <button className="btn-cancel" onClick={download}>Download HTML</button>}
            <button className="btn-cancel" onClick={onClose}>Close</button>
          </div>
        </div>

        {loading && <div className="loading">Generating CV with GPT...</div>}
        {error && <div className="loading" style={{ color: 'var(--red)' }}>{error}</div>}

        {!loading && !error && !html && !exists && (
          <div style={{ padding: '2rem', textAlign: 'center' }}>
            <p style={{ marginBottom: '1rem', color: 'var(--text-dim)' }}>No CV generated yet.</p>
            <button className="btn-confirm" onClick={() => generateCv()}>
              Generate CV
            </button>
          </div>
        )}

        {!loading && !error && exists && (
          <>
            <div className="cv-content" dangerouslySetInnerHTML={{ __html: html }} />
            {templates.length > 0 && (
              <div style={{ padding: '0.5rem 1.5rem', borderTop: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: '0.5rem', justifyContent: 'center' }}>
                <label className="form-label" style={{ margin: 0 }}>Template</label>
                <select
                  className="input"
                  style={{ width: 'auto', minWidth: 180, fontSize: '0.75rem', padding: '0.3rem 0.5rem' }}
                  value={selectedTemplateId ?? ''}
                  onChange={(e) => setSelectedTemplateId(e.target.value ? Number(e.target.value) : null)}
                >
                  <option value="">Active template (default)</option>
                  {templates.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}{t.isActive ? ' (active)' : ''}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <div style={{ padding: '0.75rem 1.5rem', borderTop: '1px solid var(--border)', textAlign: 'center', display: 'flex', gap: '0.5rem', justifyContent: 'center' }}>
              <button className="btn-cancel" style={{ fontSize: '0.75rem' }} onClick={() => { setExists(false); setHtml(''); regenerateCv(); }}>
                Regenerate CV
              </button>
              {user?.role === 'Admin' && (
                <button className="btn-cancel" style={{ fontSize: '0.75rem', color: 'var(--red)' }} onClick={() => forceRegenerate()}>
                  Force Regenerate (AI)
                </button>
              )}
            </div>
          </>
        )}
      </div>
      {aiError && <AiNotConfiguredModal message={aiError} onClose={() => setAiError('')} />}
    </div>
  );
}
