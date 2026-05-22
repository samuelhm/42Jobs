import { useEffect, useState } from 'react';
import CvModal from '../components/CvModal';

interface Job {
  id: number;
  title: string;
  company_name: string;
  job_url: string;
}

export default function Tracking() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [cvJob, setCvJob] = useState<Job | null>(null);

  useEffect(() => {
    fetch('/api/tracking')
      .then((r) => r.json())
      .then((data) => {
        if (data.success) setJobs(data.data);
        setLoading(false);
      });
  }, []);

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="tracking-page">
      <h2>Tracked Jobs ({jobs.length})</h2>
      {jobs.length === 0 && <p className="text-dim">No tracked jobs yet. Generate a CV to track a job.</p>}
      <div id="ofertas-list">
        {jobs.map((job) => (
          <div key={job.id} className="oferta-card">
            <div className="oferta-info">
              <h3>{job.title}</h3>
              <div className="oferta-meta">
                <span className="empresa">{job.company_name || 'Unknown'}</span>
              </div>
            </div>
            <div className="card-controls">
              <button className="notes-btn cv-btn" onClick={(e) => { e.stopPropagation(); setCvJob(job); }}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="12" height="12">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" />
                </svg>
              </button>
              {job.job_url && (
                <a className="oferta-link" href={job.job_url} target="_blank" rel="noopener noreferrer" onClick={(e) => e.stopPropagation()}>
                  ver
                </a>
              )}
            </div>
          </div>
        ))}
      </div>
      {cvJob && (
        <CvModal jobId={cvJob.id} jobTitle={cvJob.title} onClose={() => setCvJob(null)} />
      )}
    </div>
  );
}
