import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import CategoriesBar from '../components/CategoriesBar';
import NotesModal from '../components/NotesModal';
import KeywordModal from '../components/KeywordModal';
import { formatDescription } from '../utils';
import { useToast } from '../context/ToastContext';

interface Job {
  id: number;
  title: string;
  description: string | null;
  location: string | null;
  posted_date: string | null;
  salary: string | null;
  benefits: string | null;
  job_type: string | null;
  experience_level: string | null;
  job_url: string | null;
  company_name: string | null;
  company_type: string | null;
  keywords: string[];
  notes: string | null;
  created_at: string;
}

interface UserKeyword {
  id: number;
  name: string;
  learning_status: string;
}

export default function Offers() {
  const [searchParams] = useSearchParams();
  const categoryId = searchParams.get('category');
  const refreshKey = searchParams.get('_t');

  const [jobs, setJobs] = useState<Job[]>([]);
  const [userKeywords, setUserKeywords] = useState<Record<string, UserKeyword>>({});
  const [loading, setLoading] = useState(false);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [notesJob, setNotesJob] = useState<Job | null>(null);
  const [kwModal, setKwModal] = useState<{ id: number; name: string; status: string } | null>(null);
  const [editingTitle, setEditingTitle] = useState<{ jobId: number; title: string } | null>(null);
  const { toast } = useToast();

  useEffect(() => {
    fetch('/api/keywords')
      .then((r) => r.json())
      .then((json) => {
        if (json.success) {
          const map: Record<string, UserKeyword> = {};
          json.data.forEach((k: UserKeyword) => { map[k.name] = k; });
          setUserKeywords(map);
        }
      });
  }, []);

  useEffect(() => {
    if (!categoryId) { setJobs([]); return; }
    setLoading(true);
    fetch(`/api/categories/${categoryId}/jobs`)
      .then((r) => r.json())
      .then((json) => { if (json.success) setJobs(json.data); })
      .finally(() => setLoading(false));
  }, [categoryId, refreshKey]);

  function getMatchPct(job: Job): number {
    if (!job.keywords || job.keywords.length === 0) return 0;
    let matchCount = 0;
    job.keywords.forEach((kw) => {
      const entry = userKeywords[kw];
      if (entry && entry.learning_status !== 'not_learned') matchCount++;
    });
    return Math.round((matchCount / job.keywords.length) * 100);
  }

  function getMatchClass(pct: number): string {
    if (pct >= 50) return 'high';
    if (pct >= 20) return 'medium';
    return 'low';
  }

  function getKwClass(kw: string): string {
    const entry = userKeywords[kw];
    return entry ? entry.learning_status : 'not_learned';
  }

  function handleKwClick(kw: string, e: React.MouseEvent) {
    e.stopPropagation();
    const entry = userKeywords[kw];
    setKwModal({
      id: entry?.id || 0,
      name: kw,
      status: entry?.learning_status || 'not_learned',
    });
  }

  function handleKwStatusChange(keywordId: number, newStatus: string) {
    setUserKeywords((prev) => {
      const next = { ...prev };
      for (const key of Object.keys(next)) {
        if (next[key].id === keywordId) {
          next[key] = { ...next[key], learning_status: newStatus };
          break;
        }
      }
      return next;
    });
  }

  function isRecent(dateStr: string | null): boolean {
    if (!dateStr) return false;
    const d = new Date(dateStr + 'T00:00:00');
    const now = new Date();
    return (now.getTime() - d.getTime()) < 48 * 60 * 60 * 1000;
  }

  async function handleDelete(job: Job) {
    if (!confirm(`Delete "${job.title}"?`)) return;
    const res = await fetch(`/api/jobs/${job.id}`, { method: 'DELETE' });
    const data = await res.json();
    if (data.success) setJobs((prev) => prev.filter((j) => j.id !== job.id));
  }

  async function handleRefresh(job: Job) {
    toast(`refresh-${job.id}`, 'Refreshing job details...', 'info');
    try {
      const res = await fetch(`/api/jobs/${job.id}/refresh`, { method: 'PATCH' });
      const data = await res.json();
      if (data.status === 'rate-limited') {
        toast(`refresh-${job.id}`, data.message, 'error');
      } else if (data.success) {
        setJobs((prev) => prev.map((j) => j.id === job.id
          ? { ...j, description: data.data.description, keywords: data.data.keywords }
          : j));
        toast(`refresh-${job.id}`, `Updated with ${data.data.keywords.length} keywords`, 'success');
      }
    } catch {
      toast(`refresh-${job.id}`, 'Refresh failed', 'error');
    }
  }

  async function saveTitle() {
    if (!editingTitle) return;
    const { jobId, title } = editingTitle;
    await fetch(`/api/jobs/${jobId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title }),
    });
    setJobs((prev) =>
      prev.map((j) => (j.id === jobId ? { ...j, title } : j))
    );
    setEditingTitle(null);
  }

  return (
    <>
      <CategoriesBar />

      {!categoryId ? (
        <div className="empty-state">
          <p>Select or add a category to view offers</p>
        </div>
      ) : (
        <div className="offers-main">
          <div className="section-label">
            <span className="indicator" />
            <h2>Job offers</h2>
            <span className="count">{jobs.length} offers</span>
          </div>

          {loading && <div className="loading">Loading jobs...</div>}

          <div id="ofertas-list">
            {jobs.map((job) => {
              const pct = getMatchPct(job);
              const matchClass = getMatchClass(pct);

              return (
                <div
                  key={job.id}
                  className={`oferta-card${expandedId === job.id ? ' expanded' : ''}${isRecent(job.posted_date) ? ' recent' : ''}`}
                  onClick={() => setExpandedId(expandedId === job.id ? null : job.id)}
                >
                  <div className="oferta-info">
                    {editingTitle?.jobId === job.id ? (
                      <input
                        className="title-edit-input"
                        value={editingTitle.title}
                        onChange={(e) => setEditingTitle({ jobId: job.id, title: e.target.value })}
                        onBlur={saveTitle}
                        onKeyDown={(e) => { if (e.key === 'Enter') saveTitle(); if (e.key === 'Escape') setEditingTitle(null); }}
                        onClick={(e) => e.stopPropagation()}
                        autoFocus
                      />
                    ) : (
                      <h3>
                        {job.title}
                        {isRecent(job.posted_date) && <span className="recent-badge">New</span>}
                        {expandedId === job.id && (
                          <button
                            className="title-edit-btn"
                            onClick={(e) => { e.stopPropagation(); setEditingTitle({ jobId: job.id, title: job.title }); }}
                            title="Edit title"
                          >
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="13" height="13">
                              <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
                            </svg>
                          </button>
                        )}
                      </h3>
                    )}
                    <div className="oferta-meta">
                      <span className="empresa">{job.company_name || 'Unknown'}</span>
                      {job.company_type && (
                        <span className={`badge ${job.company_type}`}>{job.company_type}</span>
                      )}
                    </div>
                  </div>

                  <div className="card-controls">
                    <span className={`match-badge ${matchClass}`}>{pct}%</span>

                    {job.job_url && (
                      <a className="oferta-link" href={job.job_url} target="_blank" rel="noopener noreferrer" onClick={(e) => e.stopPropagation()}>
                        ver
                      </a>
                    )}

                    <button
                      className="notes-btn refresh-btn"
                      title="Refresh"
                      onClick={(e) => { e.stopPropagation(); handleRefresh(job); }}
                    >
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
                        <polyline points="23 4 23 10 17 10" /><polyline points="1 20 1 14 7 14" /><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
                      </svg>
                    </button>

                    <button
                      className="notes-btn"
                      title="Notes"
                      onClick={(e) => { e.stopPropagation(); setNotesJob(job); }}
                    >
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
                        <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
                      </svg>
                    </button>

                    <button
                      className="notes-btn delete-btn"
                      title="Delete"
                      onClick={(e) => { e.stopPropagation(); handleDelete(job); }}
                    >
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
                        <polyline points="3 6 5 6 21 6" /><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                      </svg>
                    </button>
                  </div>

                  <div className="oferta-accordion">
                    <div className="accordion-grid">
                      <div className="accordion-field"><div className="label">Location</div><div className="value">{job.location || '-'}</div></div>
                      <div className="accordion-field"><div className="label">Date</div><div className="value">{job.posted_date || '-'}</div></div>
                      <div className="accordion-field"><div className="label">Salary</div><div className="value">{job.salary || '-'}</div></div>
                      <div className="accordion-field"><div className="label">Benefits</div><div className="value">{job.benefits || '-'}</div></div>
                    </div>

                    {job.description && (
                      <div className="accordion-field desc-field">
                        <div className="label">Description</div>
                        <div className="value desc-text">{formatDescription(job.description)}</div>
                      </div>
                    )}

                    <div className="accordion-kw-list">
                      {job.keywords.map((kw) => (
                        <span
                          key={kw}
                          className={`kw-tag ${getKwClass(kw)}`}
                          onClick={(e) => handleKwClick(kw, e)}
                        >
                          {kw}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
      {notesJob && (
        <NotesModal
          jobId={notesJob.id}
          jobTitle={notesJob.title}
          initialNotes={notesJob.notes}
          onClose={() => setNotesJob(null)}
        />
      )}
      {kwModal && (
        <KeywordModal
          keywordName={kwModal.name}
          keywordId={kwModal.id}
          currentStatus={kwModal.status}
          onStatusChange={handleKwStatusChange}
          onClose={() => setKwModal(null)}
        />
      )}
    </>
  );
}
