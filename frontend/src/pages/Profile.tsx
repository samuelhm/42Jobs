import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router';
import ProfileInfo from '../components/ProfileInfo';
import ProfileList from '../components/ProfileList';

const STEPS = ['Personal', 'Experience', 'Education', 'Projects', 'Other'];

export default function Profile() {
  const [searchParams, setSearchParams] = useSearchParams();
  const step = Number(searchParams.get('step') || '0');
  const [profile, setProfile] = useState<Record<string, unknown> | null>(null);

  async function loadProfile() {
    const res = await fetch('/api/profile');
    const data = await res.json();
    if (data.success) setProfile(data.data);
  }

  useEffect(() => { loadProfile(); }, []);

  function goTo(s: number) {
    setSearchParams({ step: String(s) });
  }

  if (!profile) return <div className="loading">Loading profile...</div>;

  return (
    <div className="profile-page">
      <div className="profile-stepper">
        {STEPS.map((label, i) => (
          <button
            key={i}
            className={`step-dot${i === step ? ' active' : ''}`}
            onClick={() => goTo(i)}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="step-content">
        {step === 0 && <ProfileInfo profile={profile as ProfileData} onSave={loadProfile} />}
        {step === 1 && (
          <ProfileList
            title="Work Experience"
            fields={[
              { key: 'company', label: 'Company' },
              { key: 'position', label: 'Position' },
              { key: 'start_date', label: 'Start', type: 'date' },
              { key: 'end_date', label: 'End', type: 'date' },
              { key: 'description', label: 'Description', type: 'textarea' },
            ]}
            fetchUrl="/api/experiences"
            createUrl="/api/experiences"
            updateUrl={(id) => `/api/experiences/${id}`}
            deleteUrl={(id) => `/api/experiences/${id}`}
            bodyBuilder={(f) => ({
              company: f.company,
              position: f.position || null,
              start_date: f.start_date || null,
              end_date: f.end_date || null,
              description: f.description || null,
            })}
            renderItem={(item) => (
              <div>
                <strong>{item.position || 'Role'} at {item.company}</strong>
                <span className="pf-type">({item.start_date || '?'} - {item.end_date || '?'})</span>
                {item.description ? <p className="pf-desc">{String(item.description)}</p> : null}
              </div>
            )}
          />
        )}
        {step === 2 && (
          <ProfileList
            title="Education"
            fields={[
              { key: 'degree', label: 'Degree' },
              { key: 'institution', label: 'Institution' },
              { key: 'start_year', label: 'Start year', type: 'number' },
              { key: 'end_year', label: 'End year', type: 'number' },
            ]}
            fetchUrl="/api/education"
            createUrl="/api/education"
            updateUrl={(id) => `/api/education/${id}`}
            deleteUrl={(id) => `/api/education/${id}`}
            bodyBuilder={(f) => ({
              degree: f.degree,
              institution: f.institution || null,
              start_year: f.start_year ? Number(f.start_year) : null,
              end_year: f.end_year ? Number(f.end_year) : null,
            })}
            renderItem={(item) => (
              <div>
                <strong>{item.degree}</strong> – {item.institution || 'Unknown'}
                <span className="pf-type"> ({item.start_year || '?'}-{item.end_year || '?'})</span>
              </div>
            )}
          />
        )}
        {step === 3 && (
          <ProfileList
            title="Projects"
            fields={[
              { key: 'name', label: 'Name' },
              { key: 'description', label: 'Description', type: 'textarea' },
              { key: 'type', label: 'Type', type: 'select', options: [{ value: 'personal', label: 'Personal' }, { value: 'school', label: 'School' }] },
            ]}
            fetchUrl="/api/projects"
            createUrl="/api/projects"
            updateUrl={(id) => `/api/projects/${id}`}
            deleteUrl={(id) => `/api/projects/${id}`}
            bodyBuilder={(f) => ({
              name: f.name,
              description: f.description || null,
              type: f.type || 'personal',
            })}
            renderItem={(item) => (
              <div>
                <strong>{item.name}</strong>
                <span className="pf-type"> ({item.type})</span>
                {item.description ? <p className="pf-desc">{String(item.description)}</p> : null}
                {item.keywords ? (
                  <div className="pf-kw-list">
                    {(item.keywords as string[]).map((k) => <span key={k} className="pf-kw">{k}</span>)}
                  </div>
                ) : null}
              </div>
            )}
          />
        )}
        {step === 4 && (
          <div className="profile-section">
            <ProfileList
              title="Languages"
              fields={[
                { key: 'name', label: 'Language' },
                { key: 'level', label: 'Level (e.g. B2, Native)' },
              ]}
              fetchUrl="/api/languages"
              createUrl="/api/languages"
              updateUrl={(id) => `/api/languages/${id}`}
              deleteUrl={(id) => `/api/languages/${id}`}
              bodyBuilder={(f) => ({ name: f.name, level: f.level })}
              renderItem={(item) => (
                <span>{item.name} – {item.level}</span>
              )}
            />
            <ProfileList
              title="Certifications"
              fields={[
                { key: 'name', label: 'Name' },
                { key: 'entity', label: 'Entity' },
                { key: 'date_obtained', label: 'Date', type: 'date' },
              ]}
              fetchUrl="/api/certifications"
              createUrl="/api/certifications"
              updateUrl={(id) => `/api/certifications/${id}`}
              deleteUrl={(id) => `/api/certifications/${id}`}
              bodyBuilder={(f) => ({
                name: f.name,
                entity: f.entity || null,
                date_obtained: f.date_obtained || null,
              })}
              renderItem={(item) => (
                <span>
                  {item.name}{item.entity ? ` – ${item.entity}` : ''}
                  {item.date_obtained ? ` (${item.date_obtained})` : ''}
                </span>
              )}
            />
          </div>
        )}
      </div>

      <div className="step-nav">
        <button className="btn-cancel" disabled={step === 0} onClick={() => goTo(step - 1)}>
          ← Back
        </button>
        <span className="step-indicator">{step + 1} / {STEPS.length}</span>
        <button className="btn-confirm" disabled={step === STEPS.length - 1} onClick={() => goTo(step + 1)}>
          Next →
        </button>
      </div>
    </div>
  );
}

interface ProfileData {
  name?: string;
  last_name?: string;
  phone?: string;
  email?: string;
  address?: string;
  linkedin_url?: string;
  website_url?: string;
  github_url?: string;
  junior?: boolean;
  presentation?: string;
}
