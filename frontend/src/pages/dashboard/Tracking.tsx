import { useState } from 'react';
import { useLoaderData } from 'react-router';
import { NotesModal, CvModal, KeywordTag } from '../../components';
import { fetchWithAuth, formatDescription, isRecent } from '../../utils';
import { useToast, useAuth } from '../../context';
import type { TrackingJob } from './tracking.types';
import type { TrackingData } from './tracking.loader';
import type { UserKeyword } from '../../types';

const SECTIONS: { status: string; label: string; desc: string }[] = [
  { status: 'entrevista_conseguida', label: 'Interview', desc: 'Active pipeline — prepare for the interview.' },
  { status: 'cv_enviado', label: 'CV Sent', desc: 'Waiting for a response from the company.' },
  { status: 'saved', label: 'Tracking', desc: 'Research the company and generate a tailored CV.' },
  { status: 'empleo_conseguido', label: 'Hired', desc: 'Successfully placed — archived.' },
  { status: 'rechazado', label: 'Rejected', desc: 'Not selected this time.' },
];

const STATUS_OPTIONS = [
  { value: 'saved', label: 'Saved', dot: 'saved' },
  { value: 'cv_enviado', label: 'CV sent', dot: 'cv_sent' },
  { value: 'entrevista_conseguida', label: 'Interview', dot: 'interview' },
  { value: 'empleo_conseguido', label: 'Hired', dot: 'hired' },
  { value: 'rechazado', label: 'Rejected', dot: 'rejected' },
];

export default function Tracking() {
  const { jobs: rawJobs, userKeywords: initialKeywords } = useLoaderData() as TrackingData;
  const [jobs, setJobs] = useState(rawJobs);
  const [userKeywords, setUserKeywords] = useState<Record<string, UserKeyword>>(initialKeywords);
  const [collapsed, setCollapsed] = useState<Set<string>>(() => {
    const c = new Set(SECTIONS.map(s => s.status));
    c.delete('saved');
    return c;
  });

  function toggleSection(status: string) {
    setCollapsed(prev => {
      const next = new Set(prev);
      if (next.has(status)) next.delete(status);
      else next.add(status);
      return next;
    });
  }

  function handleJobStatusChange(jobId: number, newStatus: string) {
    setJobs(prev => prev.map(j => j.job_id === jobId ? { ...j, status: newStatus as TrackingJob['status'] } : j));
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

  const grouped: Record<string, TrackingJob[]> = {};
  for (const s of SECTIONS) grouped[s.status] = [];
  for (const j of jobs) {
    if (grouped[j.status]) grouped[j.status].push(j);
    else grouped.saved.push(j);
  }

  const nonEmpty = SECTIONS.filter(s => grouped[s.status].length > 0);

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <h2 style={{ marginBottom: '1.5rem' }}>Tracking ({jobs.length})</h2>

      {jobs.length === 0 && (
        <div className="empty-state">
          <p>No tracked jobs yet. Browse Offers and use the <b>+</b> button to start tracking.</p>
        </div>
      )}

      {nonEmpty.map(s => (
        <TrackingSection key={s.status} section={s} jobs={grouped[s.status]} userKeywords={userKeywords} onKwStatusChange={handleKwStatusChange} onJobStatusChange={handleJobStatusChange} collapsed={collapsed.has(s.status)} onToggle={() => toggleSection(s.status)} />
      ))}
    </div>
  );
}

function TrackingSection({ section, jobs, userKeywords, onKwStatusChange, onJobStatusChange, collapsed, onToggle }: {
  section: typeof SECTIONS[0];
  jobs: TrackingJob[];
  userKeywords: Record<string, UserKeyword>;
  onKwStatusChange: (kwId: number, status: string) => void;
  onJobStatusChange: (jobId: number, status: string) => void;
  collapsed: boolean;
  onToggle: () => void;
}) {
  return (
    <div style={{ marginBottom: '1.25rem' }}>
      <div className="section-label" style={{ marginBottom: '0.5rem', cursor: 'pointer' }} onClick={onToggle}>
        <span className="indicator" style={{
          background: section.status === 'entrevista_conseguida' ? 'var(--amber)' :
                      section.status === 'cv_enviado' ? 'var(--teal)' :
                      section.status === 'empleo_conseguido' ? 'var(--success, #4ecf7d)' :
                      section.status === 'rechazado' ? 'var(--red)' :
                      'var(--info, #4a9eff)',
        }} />
        <h2>{section.label}</h2>
        <span className="count">{jobs.length}</span>
        <span style={{ marginLeft: 'auto', fontSize: '0.7rem', color: 'var(--text-dim)', transition: 'transform 0.2s', transform: collapsed ? 'rotate(-90deg)' : 'rotate(0deg)' }}>▼</span>
      </div>
      {!collapsed && (
        <>
          <p className="text-dim" style={{ fontSize: '0.7rem', marginBottom: '0.5rem', fontStyle: 'italic' }}>
            {section.desc}
          </p>
          <div id="ofertas-list">
            {jobs.map(job => (
              <JobCard key={job.job_id} job={job} userKeywords={userKeywords} onKwStatusChange={onKwStatusChange} onJobStatusChange={onJobStatusChange} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}

function JobCard({ job, userKeywords, onKwStatusChange, onJobStatusChange }: {
  job: TrackingJob;
  userKeywords: Record<string, UserKeyword>;
  onKwStatusChange: (kwId: number, status: string) => void;
  onJobStatusChange: (jobId: number, status: string) => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const [editingTitle, setEditingTitle] = useState<string | null>(null);
  const [notesOpen, setNotesOpen] = useState(false);
  const [cvOpen, setCvOpen] = useState(false);
  const { toast } = useToast();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';

  async function handleStatusChange(newStatus: string) {
    onJobStatusChange(job.job_id, newStatus);
    try {
      await fetchWithAuth(`/api/tracking/${job.job_id}/status`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: newStatus }),
      });
    } catch {
      onJobStatusChange(job.job_id, job.status);
      toast('tracking-error', 'Failed to update status', 'error');
    }
  }

  async function handleDelete() {
    if (!confirm(`Delete "${job.title}"?`)) return;
    await fetchWithAuth(`/api/jobs/${job.job_id}`, { method: 'DELETE' });
    window.location.reload();
  }

  async function handleRefresh() {
    toast(`refresh-${job.job_id}`, 'Refreshing job details...', 'info');
    try {
      const res = await fetchWithAuth(`/api/jobs/${job.job_id}/refresh`, { method: 'PATCH' });
      const data = await res.json();
      if (data.status === 'rate-limited') {
        toast(`refresh-${job.job_id}`, data.message, 'error');
      } else if (data.success) {
        toast(`refresh-${job.job_id}`, `Updated with ${data.data.keywords.length} keywords`, 'success');
        window.location.reload();
      }
    } catch {
      toast(`refresh-${job.job_id}`, 'Refresh failed', 'error');
    }
  }

  async function saveTitle(title: string) {
    await fetchWithAuth(`/api/jobs/${job.job_id}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title }),
    });
    setEditingTitle(null);
    window.location.reload();
  }

  const statusDot = STATUS_OPTIONS.find(o => o.value === job.status);

  return (
    <>
      <div
        className={`oferta-card${expanded ? ' expanded' : ''}${isRecent(job.posted_date) ? ' recent' : ''}`}
        onClick={() => setExpanded(!expanded)}
      >
        <div className="oferta-info">
          {editingTitle !== null ? (
            <input
              className="title-edit-input"
              value={editingTitle}
              onChange={e => setEditingTitle(e.target.value)}
              onBlur={() => saveTitle(editingTitle)}
              onKeyDown={e => { if (e.key === 'Enter') saveTitle(editingTitle); if (e.key === 'Escape') setEditingTitle(null); }}
              onClick={e => e.stopPropagation()}
              autoFocus
            />
          ) : (
            <h3>
              {job.title}
              {isRecent(job.posted_date) && <span className="recent-badge">New</span>}
              {expanded && (
                <button
                  className="title-edit-btn"
                  onClick={e => { e.stopPropagation(); setEditingTitle(job.title); }}
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
          <select
            className="tracking-status-select"
            value={job.status}
            onClick={e => e.stopPropagation()}
            onChange={e => handleStatusChange(e.target.value)}
          >
            {STATUS_OPTIONS.map(o => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
          <TrackingButtons
            job={job}
            onCvClick={() => { setCvOpen(true); }}
            onNotesClick={() => { setNotesOpen(true); }}
            onDelete={handleDelete}
            onRefresh={handleRefresh}
          />
        </div>

        {expanded && (
          <div className="oferta-accordion">
            <div className="accordion-grid">
              <div className="accordion-field"><div className="label">Location</div><div className="value">{job.location || '-'}</div></div>
              <div className="accordion-field"><div className="label">Date</div><div className="value">{job.posted_date || '-'}</div></div>
              <div className="accordion-field"><div className="label">Salary</div><div className="value">{job.salary || '-'}</div></div>
              <div className="accordion-field"><div className="label">Benefits</div><div className="value">{job.benefits || '-'}</div></div>
              <div className="accordion-field"><div className="label">Status</div><div className="value">{statusDot && <span className={`status-dot ${statusDot.dot}`}>{statusDot.label}</span>}</div></div>
            </div>
            {job.description && (
              <div className="accordion-field desc-field">
                <div className="label">Description</div>
                <div className="value desc-text">{formatDescription(job.description)}</div>
              </div>
            )}
            {job.keywords.length > 0 && (
              <div className="accordion-kw-list">
                {job.keywords.map(kw => {
                  const entry = userKeywords[kw];
                  return (
                    <KeywordTag
                      key={kw}
                      name={kw}
                      id={entry?.id || 0}
                      status={entry?.learning_status || 'not_learned'}
                      isAdmin={isAdmin}
                      onStatusChange={onKwStatusChange}
                      onDelete={() => {}}
                    />
                  );
                })}
              </div>
            )}
          </div>
        )}
      </div>

      {notesOpen && (
        <NotesModal
          jobId={job.job_id}
          jobTitle={job.title}
          initialNotes={job.notes}
          onClose={() => setNotesOpen(false)}
        />
      )}
      {cvOpen && (
        <CvModal
          jobId={job.job_id}
          jobTitle={job.title}
          onClose={() => setCvOpen(false)}
          onGenerated={() => setCvOpen(false)}
        />
      )}
    </>
  );
}

function TrackingButtons({ job, onCvClick, onNotesClick, onDelete, onRefresh }: {
  job: TrackingJob;
  onCvClick: () => void;
  onNotesClick: () => void;
  onDelete: () => void;
  onRefresh: () => void;
}) {
  return (
    <>
      <button className="notes-btn cv-btn" title="Generate CV" onClick={e => { e.stopPropagation(); onCvClick(); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" />
        </svg>
      </button>
      {job.job_url && (
        <a className="oferta-link" href={job.job_url} target="_blank" rel="noopener noreferrer" onClick={e => e.stopPropagation()}>ver</a>
      )}
      <button className="notes-btn refresh-btn" title="Refresh" onClick={e => { e.stopPropagation(); onRefresh(); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <polyline points="23 4 23 10 17 10" /><polyline points="1 20 1 14 7 14" /><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
        </svg>
      </button>
      <button className="notes-btn" title="Notes" onClick={e => { e.stopPropagation(); onNotesClick(); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
        </svg>
      </button>
      <button className="notes-btn delete-btn" title="Delete" onClick={e => { e.stopPropagation(); onDelete(); }}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
          <polyline points="3 6 5 6 21 6" /><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
        </svg>
      </button>
    </>
  );
}
