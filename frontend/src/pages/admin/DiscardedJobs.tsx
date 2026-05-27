import { useEffect, useState } from 'react';
import { get } from '../../utils';

interface DiscardedJob {
  id: number;
  external_id: string;
  title: string | null;
  company_name: string | null;
  location: string | null;
  posted_date: string | null;
  description: string | null;
  filter_reasons: string | null;
  category_name: string | null;
  created_at: string;
}

export default function DiscardedJobs() {
  const [jobs, setJobs] = useState<DiscardedJob[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  async function load() {
    try {
      const res = await get<DiscardedJob[]>('/api/admin/discarded-jobs');
      if (res.success) setJobs(res.data);
    } catch {}
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  if (loading) return null;

  return (
    <div>
      <h2>Discarded Jobs ({jobs.length})</h2>
      <p className="text-muted">Jobs filtered out by AI — not relevant or senior-only. Kept to avoid re-fetching details and re-running AI on future searches.</p>

      {jobs.length === 0 ? (
        <div className="empty-state" style={{ marginTop: '1.5rem' }}>
          <p>No discarded jobs yet.</p>
        </div>
      ) : (
        <div style={{ marginTop: '1rem' }}>
          {jobs.map(job => {
            let reasons: Record<string, string> = {};
            try { reasons = JSON.parse(job.filter_reasons || '{}'); } catch {}

            return (
              <div key={job.id} className="service-card" style={{ marginBottom: '0.75rem', cursor: 'pointer' }}
                onClick={() => setExpandedId(expandedId === job.id ? null : job.id)}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <div>
                    <strong style={{ color: 'var(--text-bright)' }}>{job.title || 'Untitled'}</strong>
                    <span style={{ marginLeft: '0.75rem', color: 'var(--text-dim)', fontSize: '0.8rem' }}>
                      {job.company_name}{job.location ? ` — ${job.location}` : ''}
                    </span>
                  </div>
                  <div style={{ display: 'flex', gap: '0.4rem', flexShrink: 0 }}>
                    {reasons.relevant === 'no' && (
                      <span style={{ background: 'rgba(239,68,68,0.12)', color: 'var(--red)', padding: '0.15rem 0.5rem', borderRadius: '4px', fontSize: '0.7rem', fontWeight: 600 }}>
                        Not relevant{job.category_name ? ` for ${job.category_name}` : ''}
                      </span>
                    )}
                    {reasons.juniorFriendly === 'no' && (
                      <span style={{ background: 'rgba(245,158,11,0.12)', color: 'var(--amber)', padding: '0.15rem 0.5rem', borderRadius: '4px', fontSize: '0.7rem', fontWeight: 600 }}>Senior only</span>
                    )}
                  </div>
                </div>

                {expandedId === job.id && job.description && (
                  <div style={{ marginTop: '0.75rem', padding: '0.75rem', background: 'var(--bg)', borderRadius: 'var(--radius)', border: '1px solid var(--border)', fontSize: '0.78rem', color: 'var(--text-dim)', lineHeight: 1.5, maxHeight: '16rem', overflowY: 'auto' }}>
                    {job.description}
                  </div>
                )}

                <div style={{ marginTop: '0.35rem', fontSize: '0.68rem', color: 'var(--text-dim)' }}>
                  Posted: {job.posted_date || '?'} · Discarded: {new Date(job.created_at).toLocaleDateString('es-ES')}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
