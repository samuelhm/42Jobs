import { useState, useEffect } from 'react';

interface Props {
  jobId: number;
  jobTitle: string;
  onClose: () => void;
  onGenerated?: () => void;
}

export default function CvModal({ jobId, jobTitle, onClose }: Props) {
  const [html, setHtml] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [model, setModel] = useState('');
  const [exists, setExists] = useState(false);

  useEffect(() => {
    checkExisting();
  }, []);

  async function checkExisting() {
    try {
      const res = await fetch(`/api/resumes/job/${jobId}`);
      if (res.ok) {
        const data = await res.json();
        if (data.html) {
          setHtml(data.html);
          setModel(data.model || '');
          setExists(true);
        }
      }
    } catch { /* no CV yet */ }
    setLoading(false);
  }

  async function generateCv(cvModel: string) {
    setLoading(true);
    setError('');
    try {
      const res = await fetch(`/api/resumes/${jobId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ model: cvModel }),
      });
      const data = await res.json();
      if (data.cached) {
        const r2 = await fetch(`/api/resumes/job/${jobId}`);
        const d2 = await r2.json();
        setHtml(d2.html);
        setModel(d2.model || '');
        setExists(true);
      } else if (data.html) {
        setHtml(data.html);
        setModel(cvModel);
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
            <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center' }}>
              <button className="btn-confirm" onClick={() => generateCv('gpt-5.4-mini')}>
                Generate with GPT-5.4-mini
              </button>
              <button className="btn-confirm" style={{ background: 'var(--amber-dim)' }} onClick={() => generateCv('gpt-5.5')}>
                Generate with GPT-5.5
              </button>
            </div>
          </div>
        )}

        {!loading && !error && exists && (
          <>
            <div className="cv-content" dangerouslySetInnerHTML={{ __html: html }} />
            <div style={{ padding: '0.75rem 1.5rem', borderTop: '1px solid var(--border)', textAlign: 'center' }}>
              <button className="btn-cancel" style={{ fontSize: '0.75rem' }} onClick={() => { setExists(false); setHtml(''); generateCv('gpt-5.4-mini'); }}>
                Regenerate with GPT-5.4-mini
              </button>
              {' '}
              <button className="btn-cancel" style={{ fontSize: '0.75rem', color: 'var(--amber)' }} onClick={() => { setExists(false); setHtml(''); generateCv('gpt-5.5'); }}>
                Regenerate with GPT-5.5
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
