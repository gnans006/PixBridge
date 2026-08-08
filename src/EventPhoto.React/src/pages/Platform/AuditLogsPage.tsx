import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Shield, Search } from 'lucide-react';
import { apiClient } from '../../api/client';
import type { ApiResponse } from '../../types';

interface AuditEntry {
  id: string;
  actorName: string;
  entityType: string;
  entityId?: string;
  action: string;
  description: string;
  timestamp: string;
}

interface AuditPage { items: AuditEntry[]; total: number; }

const ACTION_COLORS: Record<string, string> = {
  Login:           'text-emerald-400 bg-emerald-500/10',
  Created:         'text-blue-400 bg-blue-500/10',
  Updated:         'text-amber-400 bg-amber-500/10',
  Deleted:         'text-red-400 bg-red-500/10',
  Deactivated:     'text-red-400 bg-red-500/10',
  Activated:       'text-emerald-400 bg-emerald-500/10',
  RoleChanged:     'text-purple-400 bg-purple-500/10',
  SettingsUpdated: 'text-cyan-400 bg-cyan-500/10',
  QrGenerated:     'text-indigo-400 bg-indigo-500/10',
};

export default function AuditLogsPage() {
  const [page, setPage] = useState(1);
  const [entityType, setEntityType] = useState('');
  const [action, setAction] = useState('');
  const pageSize = 30;

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', page, entityType, action],
    queryFn: () =>
      apiClient.get<ApiResponse<AuditPage>>('/platform/audit', {
        params: { page, pageSize, entityType: entityType || undefined, action: action || undefined },
      }).then(r => r.data.data),
  });

  const entries = data?.items ?? [];
  const total   = data?.total ?? 0;
  const pages   = Math.ceil(total / pageSize);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-100 flex items-center gap-2">
            <Shield className="h-6 w-6 text-primary-400" /> Audit Logs
          </h1>
          <p className="mt-1 text-sm text-gray-400">Complete record of platform actions and changes.</p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-3">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-500" />
          <select value={entityType} onChange={e => { setEntityType(e.target.value); setPage(1); }}
            className="pl-8 pr-4 py-2 rounded-lg border border-gray-700 bg-gray-800 text-sm text-gray-300 focus:border-primary-500 focus:outline-none">
            <option value="">All Entity Types</option>
            {['User', 'Event', 'Settings', 'Platform'].map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </div>
        <select value={action} onChange={e => { setAction(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg border border-gray-700 bg-gray-800 text-sm text-gray-300 focus:border-primary-500 focus:outline-none">
          <option value="">All Actions</option>
          {Object.keys(ACTION_COLORS).map(a => <option key={a} value={a}>{a}</option>)}
        </select>
      </div>

      {/* Table */}
      <div className="rounded-xl border border-gray-800 bg-gray-900 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-sm text-gray-500">Loading audit logs…</div>
        ) : entries.length === 0 ? (
          <div className="p-8 text-center text-sm text-gray-500">No audit entries found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 text-left text-xs text-gray-500 uppercase tracking-wider">
                <th className="px-5 py-3">Timestamp</th>
                <th className="px-5 py-3">Actor</th>
                <th className="px-5 py-3">Action</th>
                <th className="px-5 py-3">Entity</th>
                <th className="px-5 py-3">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-800">
              {entries.map(entry => (
                <tr key={entry.id} className="hover:bg-gray-800/40 transition-colors">
                  <td className="px-5 py-3 text-xs text-gray-500 whitespace-nowrap">
                    {new Date(entry.timestamp).toLocaleString()}
                  </td>
                  <td className="px-5 py-3 text-gray-300 font-medium">{entry.actorName}</td>
                  <td className="px-5 py-3">
                    <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${ACTION_COLORS[entry.action] ?? 'text-gray-400 bg-gray-500/10'}`}>
                      {entry.action}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-400 text-xs">
                    <span className="font-medium">{entry.entityType}</span>
                    {entry.entityId && <span className="ml-1 font-mono opacity-50">#{entry.entityId.slice(0, 8)}</span>}
                  </td>
                  <td className="px-5 py-3 text-gray-400 text-xs max-w-xs truncate">{entry.description}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {pages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-500">
          <span>{total} total entries</span>
          <div className="flex gap-2">
            <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
              className="rounded-lg border border-gray-700 px-3 py-1.5 hover:bg-gray-800 disabled:opacity-40">Previous</button>
            <span className="px-2 py-1.5">Page {page} of {pages}</span>
            <button disabled={page >= pages} onClick={() => setPage(p => p + 1)}
              className="rounded-lg border border-gray-700 px-3 py-1.5 hover:bg-gray-800 disabled:opacity-40">Next</button>
          </div>
        </div>
      )}
    </div>
  );
}
