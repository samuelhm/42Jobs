import { useState, Fragment, useEffect, useRef, useMemo } from 'react';
import { useLoaderData, useSearchParams, useRevalidator } from 'react-router';
import { Pagination } from '../../components';

interface LogEntry {
  id: number;
  created_at: string;
  actor: string;
  action: string;
  payload1: string | null;
  payload2: string | null;
  payload3: string | null;
  correlation_id: string | null;
}

interface LoaderData {
  logs: LogEntry[];
  total: number;
  actors: string[];
  actions: string[];
  filters: { actor: string; action: string; payload2: string };
  page: number;
  limit: number;
}

function statusClass(payload3: string | null): string {
  if (!payload3) return '';
  if (payload3.startsWith('sent')) return 'log-sent';
  if (payload3.startsWith('received')) return 'log-received';
  if (payload3.startsWith('error')) return 'log-error';
  return '';
}

function formatJson(raw: string | null): string {
  if (!raw) return '';
  try {
    const obj = JSON.parse(raw);
    return JSON.stringify(obj, null, 2).replace(/\\n/g, '\n');
  } catch {
    return raw;
  }
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString();
}

interface LogGroup {
  id: number;
  sent: LogEntry;
  response: LogEntry | null;
}

function groupLogs(logs: LogEntry[]): LogGroup[] {
  const groups: LogGroup[] = [];
  const responseMap = new Map<string, LogEntry>();

  for (const log of logs) {
    if (log.payload3?.startsWith('sent')) continue;
    if (log.correlation_id) {
      responseMap.set(log.correlation_id, log);
    }
  }

  const pairedIds = new Set<number>();

  for (const log of logs) {
    if (!log.payload3?.startsWith('sent')) continue;

    const cid = log.correlation_id || '';
    const response = cid ? responseMap.get(cid) : null;
    groups.push({ id: log.id, sent: log, response: response || null });

    pairedIds.add(log.id);
    if (response) pairedIds.add(response.id);
  }

  for (const log of logs) {
    if (pairedIds.has(log.id)) continue;
    groups.push({ id: log.id, sent: log, response: null });
  }

  groups.sort((a, b) => new Date(b.sent.created_at).getTime() - new Date(a.sent.created_at).getTime());
  return groups;
}

export default function AdminLogs() {
  const { logs, total, actors, actions, filters, page, limit } = useLoaderData() as LoaderData;
  const [, setSearchParams] = useSearchParams();
  const [modalJson, setModalJson] = useState<string | null>(null);
  const [filterActor, setFilterActor] = useState(filters.actor);
  const [filterAction, setFilterAction] = useState(filters.action);
  const [filterPayload2, setFilterPayload2] = useState(filters.payload2);
  const revalidator = useRevalidator();
  const pollRef = useRef<ReturnType<typeof setInterval>>(undefined);
  const seenIds = useRef(new Set<number>());

  useEffect(() => {
    pollRef.current = setInterval(() => revalidator.revalidate(), 5000);
    return () => clearInterval(pollRef.current);
  }, [revalidator]);

  const dedupedLogs = useMemo(() => {
    seenIds.current.clear();
    return logs.filter(l => {
      if (seenIds.current.has(l.id)) return false;
      seenIds.current.add(l.id);
      return true;
    });
  }, [logs]);

  const groups = groupLogs(dedupedLogs);
  const totalPages = Math.ceil(total / limit);

  function navigateToPage(p: number) {
    const params = new URLSearchParams();
    if (filterActor) params.set('actor', filterActor);
    if (filterAction) params.set('action', filterAction);
    if (filterPayload2) params.set('payload2', filterPayload2);
    params.set('page', String(p));
    setSearchParams(params);
  }

  function applyFilters() {
    seenIds.current.clear();
    navigateToPage(1);
  }

  function renderRow(log: LogEntry, isResponse: boolean = false) {
    return (
      <tr key={log.id} className={`${statusClass(log.payload3)}${isResponse ? ' log-response' : ''}`}>
        <td className="log-time">{formatDate(log.created_at)}</td>
        <td className="log-actor">{log.actor}</td>
        <td className="log-action">{log.action}</td>
        <td>
          {log.payload1 ? (
            <button className="admin-btn" onClick={() => setModalJson(formatJson(log.payload1))}>
              view JSON
            </button>
          ) : (
            <span className="text-muted">—</span>
          )}
        </td>
        <td className="log-payload2">{log.payload2 || '—'}</td>
        <td className={`log-payload3 ${statusClass(log.payload3)}`}>{log.payload3 || '—'}</td>
      </tr>
    );
  }

  return (
    <div>
      <h2>Logs ({total})</h2>

      <div className="log-filters">
        <select id="filter-actor" value={filterActor} onChange={e => { setFilterActor(e.target.value); applyFilters(); }}>
          <option value="">All actors</option>
          {actors.map(a => <option key={a} value={a}>{a}</option>)}
        </select>

        <select id="filter-action" value={filterAction} onChange={e => { setFilterAction(e.target.value); applyFilters(); }}>
          <option value="">All actions</option>
          {actions.map(a => <option key={a} value={a}>{a}</option>)}
        </select>

        <input
          id="filter-payload2"
          type="text"
          placeholder="Filter by model / API..."
          value={filterPayload2}
          onChange={e => setFilterPayload2(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') applyFilters(); }}
        />

        <button className="admin-btn" onClick={applyFilters}>Filter</button>
        {(filters.actor || filters.action || filters.payload2) && (
          <button className="admin-btn" onClick={() => { seenIds.current.clear(); setFilterActor(''); setFilterAction(''); setFilterPayload2(''); setSearchParams({}); }}>Clear</button>
        )}
      </div>

      <Pagination page={page} totalPages={totalPages} onPageChange={navigateToPage} />

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
            {groups.map((group) => (
              <Fragment key={group.id}>
                {renderRow(group.sent)}
                {group.response && renderRow(group.response, true)}
              </Fragment>
            ))}
            {groups.length === 0 && (
              <tr><td colSpan={6} className="text-muted" style={{ textAlign: 'center', padding: '2rem' }}>No logs found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={page} totalPages={totalPages} onPageChange={navigateToPage} />

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
