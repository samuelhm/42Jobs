import { useState, useEffect } from 'react';
import { fetchWithAuth } from '../../utils/fetchWithAuth';

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
      const res = await fetchWithAuth(`/api/resumes/job/${jobId}`);
      if (res.ok) {
        const data = await res.json();
        if (data.html) {
          setHtml(sanitizeHtml(data.html));
          setModel(data.model || '');
          setExists(true);
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
        headers: { 'Content-Type': 'application/json' },
      });
      const data = await res.json();
      if (data.cached) {
        setHtml(sanitizeHtml(data.html));
        setModel(data.model || '');
        setExists(true);
      } else if (data.html) {
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
            <div style={{ padding: '0.75rem 1.5rem', borderTop: '1px solid var(--border)', textAlign: 'center' }}>
              <button className="btn-cancel" style={{ fontSize: '0.75rem' }} onClick={() => { setExists(false); setHtml(''); generateCv(); }}>
                Regenerate CV
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
