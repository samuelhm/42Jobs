import { useState } from 'react';
import { useLoaderData, useSearchParams } from 'react-router';

interface LogEntry {
  id: number;
  created_at: string;
  actor: string;
  action: string;
  payload1: string | null;
  payload2: string | null;
  payload3: string | null;
}

interface LoaderData {
  logs: LogEntry[];
  actors: string[];
  actions: string[];
  filters: { actor: string; action: string; payload2: string };
}

function statusClass(payload3: string | null): string {
  if (!payload3) return '';
  if (payload3.startsWith('sent')) return 'log-sent';
  if (payload3.startsWith('received')) return 'log-received';
  if (payload3.startsWith('error')) return 'log-error';
  return '';
}

function statusLabel(payload3: string | null): string {
  if (!payload3) return '';
  if (payload3.startsWith('sent')) return 'sent';
  if (payload3.startsWith('received')) return 'received';
  if (payload3.startsWith('error')) return 'error';
  return '';
}

function formatJson(raw: string | null): string {
  if (!raw) return '';
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString();
}

export default function AdminLogs() {
  const { logs, actors, actions, filters } = useLoaderData() as LoaderData;
  const [, setSearchParams] = useSearchParams();
  const [modalJson, setModalJson] = useState<string | null>(null);

  function applyFilters() {
    const params = new URLSearchParams();
    const actor = (document.getElementById('filter-actor') as HTMLSelectElement).value;
    const action = (document.getElementById('filter-action') as HTMLSelectElement).value;
    const payload2 = (document.getElementById('filter-payload2') as HTMLInputElement).value;
    if (actor) params.set('actor', actor);
    if (action) params.set('action', action);
    if (payload2) params.set('payload2', payload2);
    setSearchParams(params);
  }

  return (
    <div>
      <h2>Logs</h2>

      <div className="log-filters">
        <select id="filter-actor" defaultValue={filters.actor} onChange={applyFilters}>
          <option value="">All actors</option>
          {actors.map(a => <option key={a} value={a}>{a}</option>)}
        </select>

        <select id="filter-action" defaultValue={filters.action} onChange={applyFilters}>
          <option value="">All actions</option>
          {actions.map(a => <option key={a} value={a}>{a}</option>)}
        </select>

        <input
          id="filter-payload2"
          type="text"
          placeholder="Filter by model / API..."
          defaultValue={filters.payload2}
          onKeyDown={e => { if (e.key === 'Enter') applyFilters(); }}
        />

        <button className="admin-btn" onClick={applyFilters}>Filter</button>
        {(filters.actor || filters.action || filters.payload2) && (
          <button className="admin-btn" onClick={() => setSearchParams({})}>Clear</button>
        )}
      </div>

      <div className="log-table-wrap">
        <table className="log-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Actor</th>
              <th>Action</th>
              <th>Payload 1</th>
              <th>Payload 2</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id} className={statusClass(log.payload3)}>
                <td className="log-time">{formatDate(log.created_at)}</td>
                <td className="log-actor">{log.actor}</td>
                <td className="log-action">{log.action}</td>
                <td>
                  {log.payload1 ? (
                    <button
                      className="admin-btn"
                      onClick={() => setModalJson(formatJson(log.payload1))}
                    >
                      view JSON
                    </button>
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </td>
                <td className="log-payload2">{log.payload2 || '—'}</td>
                <td>
                  <span className={`log-status-badge ${statusClass(log.payload3)}`}>
                    {statusLabel(log.payload3)}
                    {log.payload3 && !log.payload3.startsWith('sent') && !log.payload3.startsWith('received') && !log.payload3.startsWith('error')
                      ? log.payload3
                      : ''}
                  </span>
                </td>
              </tr>
            ))}
            {logs.length === 0 && (
              <tr><td colSpan={6} className="text-muted" style={{ textAlign: 'center', padding: '2rem' }}>No logs found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {modalJson && (
        <div className="dialog-overlay" onClick={() => setModalJson(null)}>
          <div className="dialog-box log-modal" onClick={e => e.stopPropagation()}>
            <h3>Payload</h3>
            <pre className="log-json">{modalJson}</pre>
            <div className="dialog-actions">
              <button className="btn-cancel" onClick={() => setModalJson(null)}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
