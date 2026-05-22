import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import CategoriesBar from '../components/CategoriesBar';
import KeywordsChart from '../components/KeywordsChart';

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

export default function Dashboard() {
  const [searchParams] = useSearchParams();
  const categoryId = searchParams.get('category');
  const refreshKey = searchParams.get('_t');

  const [jobs, setJobs] = useState<Job[]>([]);
  const [userKeywords, setUserKeywords] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [highlightKw, setHighlightKw] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/keywords')
      .then((r) => r.json())
      .then((json) => {
        if (json.success) {
          const map: Record<string, string> = {};
          json.data.forEach((k: UserKeyword) => { map[k.name] = k.learning_status; });
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
      const status = userKeywords[kw];
      if (status && status !== 'not_learned') matchCount++;
    });
    return Math.round((matchCount / job.keywords.length) * 100);
  }

  function getMatchClass(pct: number): string {
    if (pct >= 50) return 'high';
    if (pct >= 20) return 'medium';
    return 'low';
  }

  function isRecent(dateStr: string | null): boolean {
    if (!dateStr) return false;
    const d = new Date(dateStr + 'T00:00:00');
    const now = new Date();
    return (now.getTime() - d.getTime()) < 48 * 60 * 60 * 1000;
  }

  return (
    <>
      <CategoriesBar />

      {!categoryId ? (
        <div className="empty-state">
          <p>Select or add a category to get started</p>
        </div>
      ) : (
        <main>
          <div className="left-col">
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
                    className={`oferta-card${expandedId === job.id ? ' expanded' : ''}${isRecent(job.posted_date) ? ' recent' : ''}${highlightKw && job.keywords.some((k) => k.toLowerCase() === highlightKw.toLowerCase()) ? ' highlighted' : highlightKw ? ' dimmed' : ''}`}
                    onClick={() => setExpandedId(expandedId === job.id ? null : job.id)}
                  >
                    <div className="oferta-info">
                      <h3>{job.title}{isRecent(job.posted_date) && <span className="recent-badge">New</span>}</h3>
                      <div className="oferta-meta">
                        <span className="empresa">{job.company_name || 'Unknown'}</span>
                        {job.company_type && <span className={`badge ${job.company_type}`}>{job.company_type}</span>}
                      </div>
                    </div>
                    <div className="card-controls">
                      <span className={`match-badge ${matchClass}`}>{pct}%</span>
                    </div>
                    <div className="oferta-accordion">
                      <div className="accordion-grid">
                        <div className="accordion-field">
                          <div className="label">Location</div>
                          <div className="value">{job.location || '-'}</div>
                        </div>
                        <div className="accordion-field">
                          <div className="label">Date</div>
                          <div className="value">{job.posted_date || '-'}</div>
                        </div>
                      </div>
                      {job.description && (
                        <div className="accordion-field desc-field">
                          <div className="label">Description</div>
                          <div className="value desc-text">{job.description}</div>
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="right-col">
            <div className="section-label">
              <span className="indicator" style={{ background: 'var(--teal)', boxShadow: '0 0 8px rgba(77,184,160,0.4)' }} />
              <h2>Keywords</h2>
            </div>
            <KeywordsChart categoryId={categoryId} onHover={setHighlightKw} />
          </div>
        </main>
      )}
    </>
  );
}
