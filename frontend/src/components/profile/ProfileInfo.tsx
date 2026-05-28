import { useState, useEffect } from 'react';
import { put, post, SPANISH_CITIES } from '../../utils';
import type { ProfileData } from '../../types';

interface Props {
  profile: ProfileData;
  onSave: () => void;
}

function resizePhoto(file: File, maxDim: number): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error('Failed to read file'));
    reader.onload = () => {
      const img = new Image();
      img.onerror = () => reject(new Error('Invalid image file'));
      img.onload = () => {
        let w = img.width;
        let h = img.height;
        if (w > maxDim || h > maxDim) {
          const ratio = Math.min(maxDim / w, maxDim / h);
          w = Math.round(w * ratio);
          h = Math.round(h * ratio);
        }
        const canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
        resolve(canvas.toDataURL(file.type || 'image/jpeg', 0.85));
      };
      img.src = reader.result as string;
    };
    reader.readAsDataURL(file);
  });
}

export default function ProfileInfo({ profile, onSave }: Props) {
  const [form, setForm] = useState<ProfileData>({});
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState('');
  const [photoUploading, setPhotoUploading] = useState(false);

  useEffect(() => { setForm(profile); }, [profile]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    const res = await put<ProfileData>('/api/profile', form);
    const extra = res as any;
    if (res.success) {
      if (extra.fetch_triggered) {
        setMsg(`Saved. Fetching jobs for ${extra.categories_fetched} categories in ${extra.location} — may take a few minutes.`);
        setTimeout(() => setMsg(''), 6000);
      } else {
        setMsg('Saved');
        setTimeout(() => setMsg(''), 2000);
      }
      onSave();
    }
    else { setMsg('Error'); }
    setSaving(false);
  }

  async function handlePhotoSelect(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setPhotoUploading(true);
    try {
      const dataUrl = await resizePhoto(file, 300);
      const res = await post<{ photo?: string }>('/api/profile/photo', { photo: dataUrl });
      if (res.success) {
        setForm((prev) => ({ ...prev, photo: dataUrl }));
        setMsg('Photo updated');
        setTimeout(() => setMsg(''), 2000);
      } else {
        setMsg(res.error || 'Photo upload failed');
      }
    } catch {
      setMsg('Photo upload failed');
    } finally {
      setPhotoUploading(false);
    }
  }

  async function handlePhotoRemove() {
    setPhotoUploading(true);
    try {
      const res = await post<{ photo?: string }>('/api/profile/photo', { photo: null });
      if (res.success) {
        setForm((prev) => ({ ...prev, photo: undefined }));
        setMsg('Photo removed');
        setTimeout(() => setMsg(''), 2000);
      } else {
        setMsg(res.error || 'Failed to remove photo');
      }
    } catch {
      setMsg('Failed to remove photo');
    } finally {
      setPhotoUploading(false);
    }
  }

  const currentPhoto = form.photo || profile.photo;

  return (
    <form onSubmit={handleSubmit} className="profile-form">
      <h2>Personal Information</h2>

      <div className="photo-section" style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
        {currentPhoto ? (
          <img src={currentPhoto} alt="Profile" style={{ width: 80, height: 80, borderRadius: '50%', objectFit: 'cover', border: '2px solid var(--border)' }} />
        ) : (
          <div style={{ width: 80, height: 80, borderRadius: '50%', background: 'var(--bg-dim)', border: '2px solid var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-dim)', fontSize: '0.75rem' }}>No photo</div>
        )}
        <div>
          <label className="btn-cancel" style={{ cursor: 'pointer', fontSize: '0.8rem', padding: '0.35rem 0.75rem' }}>
            {photoUploading ? 'Uploading...' : 'Choose photo'}
            <input type="file" accept="image/jpeg,image/png,image/webp" onChange={handlePhotoSelect} style={{ display: 'none' }} disabled={photoUploading} />
          </label>
          {currentPhoto && (
            <button type="button" className="btn-cancel" style={{ fontSize: '0.8rem', padding: '0.35rem 0.75rem', marginLeft: '0.5rem' }} onClick={handlePhotoRemove} disabled={photoUploading}>
              Remove
            </button>
          )}
        </div>
      </div>

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
          <select value={form.preferred_location || ''} onChange={(e) => setForm({ ...form, preferred_location: e.target.value })}>
            <option value="">-- Select a city --</option>
            {SPANISH_CITIES.map((city) => (
              <option key={city} value={city}>{city}</option>
            ))}
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
