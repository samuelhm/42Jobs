import { useState } from 'react';
import { useLoaderData } from 'react-router';
import { CategoriesBar, KeywordsChart } from '../../components';
import { getMatchPct, getMatchClass, isRecent } from '../../utils';
import type { DashboardData } from './dashboard.types';

export default function Dashboard() {
  const { userKeywords, jobs, categoryId } = useLoaderData() as DashboardData;
  const [highlightKw, setHighlightKw] = useState<string | null>(null);

  if (!categoryId) {
    return (
      <>
        <CategoriesBar />
        <div className="empty-state">
          <p>Select or add a category to get started</p>
        </div>
      </>
    );
  }

  return (
    <>
      <CategoriesBar />
      <main>
        <div className="left-col">
          <div className="section-label">
            <span className="indicator" />
            <h2>Job offers</h2>
            <span className="count">{jobs.length} offers</span>
          </div>

          <div id="ofertas-list">
            {jobs.map((job) => {
              const pct = getMatchPct(job, userKeywords);
              const matchClass = getMatchClass(pct);

              return (
                <div
                  key={job.id}
                  className={`oferta-card dashboard-card${isRecent(job.posted_date) ? ' recent' : ''}${highlightKw && job.keywords.some((k) => k.toLowerCase() === highlightKw.toLowerCase()) ? ' highlighted' : highlightKw ? ' dimmed' : ''}`}
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
    </>
  );
}
