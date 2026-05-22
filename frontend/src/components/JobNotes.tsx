import { useState, useEffect, useRef } from 'react';

interface Props {
  jobId: number;
  initialNotes: string | null;
}

export default function JobNotes({ jobId, initialNotes }: Props) {
  const [notes, setNotes] = useState(initialNotes || '');
  const [saved, setSaved] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, []);

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
    <div className="offer-notes">
      <textarea
        className="notes-textarea"
        value={notes}
        onChange={(e) => handleChange(e.target.value)}
        onClick={(e) => e.stopPropagation()}
        placeholder="Notes..."
        rows={3}
      />
      {saved && <span className="notes-saved visible">Saved</span>}
    </div>
  );
}
