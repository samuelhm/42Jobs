import { useState, useEffect } from 'react';

interface Props {
  jobId: number;
  jobTitle: string;
  onClose: () => void;
}

export default function CvModal({ jobId, jobTitle, onClose }: Props) {
  const [html, setHtml] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [model, setModel] = useState('');

  useEffect(() => {
    checkExisting();
  }, []);

  async function checkExisting() {
    try {
      const res = await fetch(`/api/resumes/job/${jobId}`);
      const data = await res.json();
      if (data.html) {
        setHtml(data.html);
        setModel(data.model || '');
        setLoading(false);
      } else {
        generateCv();
      }
    } catch {
      setLoading(false);
    }
  }

  async function generateCv() {
    setLoading(true);
    setError('');
    try {
      const res = await fetch(`/api/resumes/${jobId}`, { method: 'POST' });
      const data = await res.json();
      if (data.cached) {
        const r2 = await fetch(`/api/resumes/job/${jobId}`);
        const d2 = await r2.json();
        setHtml(d2.html);
        setModel(d2.model || '');
      } else if (data.html) {
        setHtml(data.html);
        setModel('gpt-5.4-mini');
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
        {html && <div className="cv-content" dangerouslySetInnerHTML={{ __html: html }} />}
      </div>
    </div>
  );
}
