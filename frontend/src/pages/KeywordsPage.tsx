import { useEffect, useState } from 'react';
import KeywordTag from '../components/KeywordTag';

interface KeywordItem {
  id: number;
  name: string;
  learning_status: string;
}

export default function KeywordsPage() {
  const [keywords, setKeywords] = useState<KeywordItem[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await fetch('/api/keywords');
    const data = await res.json();
    if (data.success) setKeywords(data.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  function handleStatusChange(keywordId: number, newStatus: string) {
    setKeywords((prev) =>
      prev.map((k) => (k.id === keywordId ? { ...k, learning_status: newStatus } : k))
    );
  }

  const unassigned = keywords.filter((k) => k.learning_status === 'not_learned');
  const school = keywords.filter((k) => k.learning_status === 'learned_in_school');
  const personal = keywords.filter((k) => k.learning_status === 'learned_personal_project');

  if (loading) return <div className="loading">Loading keywords...</div>;

  return (
    <div className="keywords-page">
      <h2>Keywords ({keywords.length})</h2>

      <section>
        <h3 className="kw-section-title not-learned">Not learned ({unassigned.length})</h3>
        <div className="kw-section-list">
          {unassigned.length === 0 && <p className="text-dim">All keywords assigned</p>}
          {unassigned.map((k) => (
            <KeywordTag key={k.id} name={k.name} id={k.id} status={k.learning_status} onStatusChange={handleStatusChange} />
          ))}
        </div>
      </section>

      <section>
        <h3 className="kw-section-title learned">Learned at 42 Barcelona ({school.length})</h3>
        <div className="kw-section-list">
          {school.length === 0 && <p className="text-dim">None yet</p>}
          {school.map((k) => (
            <KeywordTag key={k.id} name={k.name} id={k.id} status={k.learning_status} onStatusChange={handleStatusChange} />
          ))}
        </div>
      </section>

      <section>
        <h3 className="kw-section-title project">Personal projects ({personal.length})</h3>
        <div className="kw-section-list">
          {personal.length === 0 && <p className="text-dim">None yet</p>}
          {personal.map((k) => (
            <KeywordTag key={k.id} name={k.name} id={k.id} status={k.learning_status} onStatusChange={handleStatusChange} />
          ))}
        </div>
      </section>
    </div>
  );
}
