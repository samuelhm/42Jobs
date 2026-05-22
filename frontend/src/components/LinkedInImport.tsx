import { useState } from 'react';

interface Props {
  endpoint: string;
  placeholder: string;
  onImported: (count: number) => void;
}

export default function LinkedInImport({ endpoint, placeholder, onImported }: Props) {
  const [text, setText] = useState('');
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState('');

  async function handleImport() {
    const raw = text.trim();
    if (!raw) return;
    setLoading(true);
    setMsg('Analyzing with Gemini...');
    try {
      const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ raw_text: raw }),
      });
      const data = await res.json();
      if (data.success) {
        setMsg(`${data.imported} entries imported`);
        setText('');
        onImported(data.imported);
      } else {
        setMsg(data.error || 'Import failed');
      }
    } catch {
      setMsg('Connection error');
    } finally {
      setLoading(false);
      setTimeout(() => setMsg(''), 4000);
    }
  }

  return (
    <div className="linkedin-import">
      <h3>Import from LinkedIn</h3>
      <p className="hint">
        Copy your raw LinkedIn data by selecting the text directly on your profile page
        (Ctrl+A, Ctrl+C). Do not export or download — just copy the visible text.
        If the import fails, try pasting one entry at a time.
      </p>
      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder={placeholder}
        rows={8}
      />
      <div className="linkedin-import-actions">
        <button className="btn-confirm" onClick={handleImport} disabled={loading || !text.trim()}>
          {loading ? 'Importing...' : 'Import from LinkedIn'}
        </button>
        {msg && <span className="linkedin-import-msg">{msg}</span>}
      </div>
    </div>
  );
}
