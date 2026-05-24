import { useState, useEffect } from 'react';
import type { ProfileData } from '../../types';

interface Props {
  profile: ProfileData;
  onSave: () => void;
}

export default function ProfileInfo({ profile, onSave }: Props) {
  const [form, setForm] = useState<ProfileData>({});
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState('');

  useEffect(() => { setForm(profile); }, [profile]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    const res = await fetch('/api/profile', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form),
    });
    const data = await res.json();
    if (data.success) { setMsg('Saved'); setTimeout(() => setMsg(''), 2000); onSave(); }
    else { setMsg('Error'); }
    setSaving(false);
  }

  return (
    <form onSubmit={handleSubmit} className="profile-form">
      <h2>Personal Information</h2>
      <div className="form-grid">
        <div className="form-field">
          <label>Name</label>
          <input value={form.name || ''} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        </div>
        <div className="form-field">
          <label>Last Name</label>
          <input value={form.last_name || ''} onChange={(e) => setForm({ ...form, last_name: e.target.value })} />
        </div>
        <div className="form-field">
          <label>Phone</label>
          <input value={form.phone || ''} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        </div>
        <div className="form-field">
          <label>Email</label>
          <input type="email" value={form.email || ''} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </div>
        <div className="form-field">
          <label>Address</label>
          <input value={form.address || ''} onChange={(e) => setForm({ ...form, address: e.target.value })} />
        </div>
        <div className="form-field">
          <label>LinkedIn URL</label>
          <input value={form.linkedin_url || ''} onChange={(e) => setForm({ ...form, linkedin_url: e.target.value })} />
        </div>
        <div className="form-field">
          <label>Website</label>
          <input value={form.website_url || ''} onChange={(e) => setForm({ ...form, website_url: e.target.value })} />
        </div>
        <div className="form-field">
          <label>GitHub</label>
          <input value={form.github_url || ''} onChange={(e) => setForm({ ...form, github_url: e.target.value })} />
        </div>
        <div className="form-field">
          <label>Profile</label>
          <select value={form.junior ? 'true' : 'false'} onChange={(e) => setForm({ ...form, junior: e.target.value === 'true' })}>
            <option value="true">Junior</option>
            <option value="false">Senior</option>
          </select>
        </div>
      </div>
      <div className="form-field full">
        <label>Presentation</label>
        <textarea value={form.presentation || ''} onChange={(e) => setForm({ ...form, presentation: e.target.value })} rows={4} />
      </div>
      <div className="form-grid">
        <div className="form-field">
          <label>Preferred Location</label>
          <input value={form.preferred_location || ''} onChange={(e) => setForm({ ...form, preferred_location: e.target.value })} placeholder="e.g. Barcelona" />
        </div>
        <div className="form-field">
          <label>Date Filter</label>
          <select value={form.preferred_date_posted || 'past-week'} onChange={(e) => setForm({ ...form, preferred_date_posted: e.target.value })}>
            <option value="past-week">Past Week</option>
            <option value="past-24h">Past 24 Hours</option>
          </select>
        </div>
      </div>
      <button type="submit" className="btn-confirm" disabled={saving}>
        {saving ? 'Saving...' : 'Save'}
      </button>
      {msg && <span className="pf-status">{msg}</span>}
    </form>
  );
}
