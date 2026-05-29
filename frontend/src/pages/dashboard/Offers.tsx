import { useState, useEffect } from 'react';
import { useLoaderData } from 'react-router';
import { CategoriesBar, NotesModal, KeywordTag, CvModal } from '../../components';
import { fetchWithAuth, formatDescription, getMatchPct, getMatchClass, isRecent } from '../../utils';
import { useToast, useAuth } from '../../context';
import type { Job, UserKeyword } from '../../types';
import type { OffersData } from './offers.loader';

export function OffersRoute() {
  return (
    <>
      <CategoriesBar availableOnly />
      <OffersContent />
    </>
  );
}

function OffersContent() {
  const { userKeywords: initialKeywords, jobs: initialJobs, categoryId, lastFetchedAt } = useLoaderData() as OffersData;

  const [userKeywords, setUserKeywords] = useState<Record<string, UserKeyword>>(initialKeywords);
  const [jobs, setJobs] = useState<Job[]>(initialJobs);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  useEffect(() => {
    setUserKeywords(initialKeywords);
    setJobs(initialJobs);
  }, [initialKeywords, initialJobs]);
  const [notesJob, setNotesJob] = useState<Job | null>(null);
  const [editingTitle, setEditingTitle] = useState<{ jobId: number; title: string } | null>(null);
  const [cvJob, setCvJob] = useState<Job | null>(null);
  const [seniorDiscarded, setSeniorDiscarded] = useState(0);
  const { toast } = useToast();

  useEffect(() => {
    if (!categoryId) return;
    fetchWithAuth(`/api/categories/${categoryId}/discarded-stats`)
      .then(r => r.json())
      .then(json => { if (json.success) setSeniorDiscarded(json.data.senior_only); })
      .catch(() => {});
  }, [categoryId]);

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

  async function handleDelete(job: Job) {
    if (!confirm(`Delete "${job.title}"?`)) return;
    const res = await fetchWithAuth(`/api/jobs/${job.id}`, { method: 'DELETE' });
    const data = await res.json();
    if (data.success) setJobs((prev) => prev.filter((j) => j.id !== job.id));
  }

  async function saveTitle() {
    if (!editingTitle) return;
    const { jobId, title } = editingTitle;
    await fetchWithAuth(`/api/jobs/${jobId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title }),
    });
    setJobs((prev) =>
      prev.map((j) => (j.id === jobId ? { ...j, title } : j))
    );
    setEditingTitle(null);
  }

  async function handleTrack(job: Job) {
    await fetchWithAuth(`/api/tracking/${job.id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status: 'saved' }),
    });
    setJobs((prev) => prev.filter((j) => j.id !== job.id));
    toast(`track-${job.id}`, 'Added to tracking', 'success');
  }

  if (!categoryId) {
    return (
      <div className="empty-state">
        <p>Select or add a category to view offers</p>
      </div>
    );
  }

  return (
    <>
      <div className="offers-main">
        <div className="section-label">
          <span className="indicator" />
          <h2>Job offers</h2>
          <span className="count">{jobs.length} offers</span>
        </div>

        {lastFetchedAt && (
          <p className="last-fetched" style={{ fontSize: '0.72rem', color: 'var(--text-dim)', marginBottom: '0.75rem' }}>
            Last updated {formatLastFetched(lastFetchedAt)}
            {seniorDiscarded > 0 && (
              <span style={{ color: 'var(--red)', marginLeft: '0.75rem' }}>
                {seniorDiscarded} discarded (senior only)
              </span>
            )}
          </p>
        )}

        <div id="ofertas-list">
          {jobs.map((job) => {
            const pct = getMatchPct(job, userKeywords);
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
                  <JobCardButtons job={job} onCvClick={setCvJob} onNotesClick={setNotesJob} onDelete={handleDelete} onTrack={handleTrack} />
                </div>

                <JobAccordion job={job} userKeywords={userKeywords} onStatusChange={handleKwStatusChange} />
              </div>
            );
          })}
        </div>
      </div>

      {notesJob && (
        <NotesModal
          jobId={notesJob.id}
          jobTitle={notesJob.title}
          initialNotes={notesJob.notes}
          onClose={() => setNotesJob(null)}
        />
      )}
      {cvJob && (
        <CvModal
          jobId={cvJob.id}
          jobTitle={cvJob.title}
          onClose={() => setCvJob(null)}
          onGenerated={() => {
            setCvJob(null);
          }}
        />
      )}
    </>
  );
}

function JobCardButtons({ job, onCvClick, onNotesClick, onDelete, onTrack }: {
  job: Job;
  onCvClick: (j: Job) => void;
  onNotesClick: (j: Job) => void;
  onDelete: (j: Job) => void;
  onTrack: (j: Job) => void;
}) {
  return (
    <>
      <button className="track-btn" onClick={e => { e.stopPropagation(); onTrack(job); }} title="Track this job">
        +
      </button>
      <button className="notes-btn cv-btn" title="Generate CV" onClick={(e) => { e.stopPropagation(); onCvClick(job); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" />
        </svg>
      </button>
      {job.job_url && (
        <a className="oferta-link" href={job.job_url} target="_blank" rel="noopener noreferrer" onClick={(e) => e.stopPropagation()}>ver</a>
      )}
      <button className="notes-btn" title="Notes" onClick={(e) => { e.stopPropagation(); onNotesClick(job); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
        </svg>
      </button>
      <button className="notes-btn delete-btn" title="Delete" onClick={(e) => { e.stopPropagation(); onDelete(job); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <polyline points="3 6 5 6 21 6" /><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
        </svg>
      </button>
    </>
  );
}

function JobAccordion({ job, userKeywords, onStatusChange }: {
  job: Job;
  userKeywords: Record<string, UserKeyword>;
  onStatusChange: (kwId: number, status: string) => void;
}) {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  return (
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
        {job.keywords.map((kw) => {
          const entry = userKeywords[kw];
          return (
            <KeywordTag
              key={kw}
              name={kw}
              id={entry?.id || 0}
              status={entry?.learning_status || 'not_learned'}
              isAdmin={isAdmin}
              onStatusChange={onStatusChange}
              onDelete={() => {}}
            />
          );
        })}
      </div>
    </div>
  );
}

function formatLastFetched(iso: string): string {
  const d = new Date(iso);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'just now';
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH}h ago`;
  const diffD = Math.floor(diffH / 24);
  if (diffD === 1) return 'yesterday';
  if (diffD < 7) return `${diffD}d ago`;
  return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
}

