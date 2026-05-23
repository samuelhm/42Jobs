import { useState } from 'react';
import { post } from './api';

export default function AdminDedup() {
  const [msg, setMsg] = useState('');
  const [running, setRunning] = useState(false);

  async function runDedup() {
    setRunning(true);
    setMsg('Running dedup...');
    const res = await post<{ message: string; merged: number }>('/api/admin/dedup-keywords', {});
    setMsg(res.success ? res.data.message : 'Error');
    setRunning(false);
  }

  return (
    <div>
      <h2>Deduplicate Keywords</h2>
      <p className="text-muted">Uses AI to find and merge duplicate/similar keywords across all tables.</p>
      <button className="btn btn-primary" onClick={runDedup} disabled={running}>
        {running ? 'Running...' : 'Run Dedup'}
      </button>
      {msg && <div className="card mt-3" style={{ padding: '1rem' }}>{msg}</div>}
    </div>
  );
}
