import { useState, useRef } from 'react';

interface Props {
  jobId: number;
  jobTitle: string;
  initialNotes: string | null;
  onClose: () => void;
}

export default function NotesModal({ jobId, jobTitle, initialNotes, onClose }: Props) {
  const [notes, setNotes] = useState(initialNotes || '');
  const [saved, setSaved] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  function handleChange(value: string) {
    setNotes(value);
    setSaved(false);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(async () => {
      await fetch(`/api/jobs/${jobId}/notes`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ notes: value }),
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    }, 800);
  }

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog-box" onClick={(e) => e.stopPropagation()}>
        <h3>Notes</h3>
        <p>{jobTitle}</p>
        <textarea
          className="notes-textarea"
          value={notes}
          onChange={(e) => handleChange(e.target.value)}
          placeholder="Write your notes..."
          rows={8}
          autoFocus
        />
        {saved && <span className="notes-saved visible">Saved</span>}
        <div className="dialog-actions">
          <button className="btn-cancel" onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  );
}
