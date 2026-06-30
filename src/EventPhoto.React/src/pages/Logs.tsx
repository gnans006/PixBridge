import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw, AlertTriangle, Info, AlertCircle, Bug } from 'lucide-react';
import { apiClient } from '../api/client';
import { Card } from '../components/UI/Card';
import { Button } from '../components/UI/Button';
import { Badge } from '../components/UI/Badge';
import { Spinner } from '../components/UI/Spinner';

interface LogEntry {
  timestamp: string;
  level: string;
  message: string;
  exception?: string;
  properties?: Record<string, unknown>;
}

const LEVEL_STYLES: Record<string, { color: 'green' | 'blue' | 'yellow' | 'red' | 'gray'; icon: typeof Info }> = {
  Information: { color: 'blue', icon: Info },
  Warning: { color: 'yellow', icon: AlertTriangle },
  Error: { color: 'red', icon: AlertCircle },
  Fatal: { color: 'red', icon: AlertCircle },
  Debug: { color: 'gray', icon: Bug },
  Verbose: { color: 'gray', icon: Bug },
};

async function fetchLogs(): Promise<LogEntry[]> {
  try {
    const res = await apiClient.get<{ success: boolean; data: LogEntry[] }>('/logs/recent');
    return res.data?.data ?? [];
  } catch {
    return [];
  }
}

export default function Logs() {
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [filter, setFilter] = useState<string>('All');
  const bottomRef = useRef<HTMLDivElement>(null);

  const { data: logs = [], isLoading, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['logs'],
    queryFn: fetchLogs,
    refetchInterval: autoRefresh ? 5000 : false,
    staleTime: 0,
  });

  useEffect(() => {
    if (autoRefresh) bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [logs, autoRefresh]);

  const levels = ['All', 'Information', 'Warning', 'Error', 'Fatal'];
  const filtered = filter === 'All' ? logs : logs.filter(l => l.level === filter);

  const counts = logs.reduce<Record<string, number>>((acc, l) => {
    acc[l.level] = (acc[l.level] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <div className="flex flex-col h-full space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Application Logs</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {dataUpdatedAt ? `Last refreshed: ${new Date(dataUpdatedAt).toLocaleTimeString()}` : 'Loading…'}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant={autoRefresh ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setAutoRefresh(v => !v)}
          >
            <RefreshCw className={`h-4 w-4 ${autoRefresh ? 'animate-spin' : ''}`} />
            {autoRefresh ? 'Live' : 'Paused'}
          </Button>
          <Button variant="secondary" size="sm" onClick={() => refetch()}>
            <RefreshCw className="h-4 w-4" /> Refresh
          </Button>
        </div>
      </div>

      {/* Level summary */}
      <div className="flex flex-wrap gap-2">
        {levels.map(level => (
          <button
            key={level}
            onClick={() => setFilter(level)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors border ${
              filter === level
                ? 'bg-primary-600 text-white border-primary-600'
                : 'bg-white text-gray-600 border-gray-200 hover:bg-gray-50'
            }`}
          >
            {level}
            {level !== 'All' && counts[level] != null && (
              <span className="ml-1.5 text-xs opacity-75">({counts[level]})</span>
            )}
          </button>
        ))}
      </div>

      {/* Log entries */}
      <Card className="flex-1 overflow-hidden">
        {isLoading ? (
          <div className="flex justify-center py-12"><Spinner size="lg" /></div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-gray-400">
            <Info className="h-10 w-10 mb-3" />
            <p className="font-medium">No log entries found</p>
            <p className="text-sm mt-1">
              {filter !== 'All' ? `No ${filter} level entries.` : 'Logs will appear here once the application generates them.'}
            </p>
          </div>
        ) : (
          <div className="overflow-y-auto max-h-[calc(100vh-320px)] font-mono text-xs">
            <table className="w-full">
              <thead className="sticky top-0 bg-gray-50 border-b border-gray-200 z-10">
                <tr>
                  <th className="text-left px-4 py-2 text-gray-500 font-medium w-40">Timestamp</th>
                  <th className="text-left px-4 py-2 text-gray-500 font-medium w-24">Level</th>
                  <th className="text-left px-4 py-2 text-gray-500 font-medium">Message</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.map((entry, i) => {
                  const style = LEVEL_STYLES[entry.level] ?? LEVEL_STYLES.Information;
                  const Icon = style.icon;
                  return (
                    <tr key={i} className={`hover:bg-gray-50 ${entry.level === 'Error' || entry.level === 'Fatal' ? 'bg-red-50' : ''}`}>
                      <td className="px-4 py-2 text-gray-400 whitespace-nowrap">
                        {new Date(entry.timestamp).toLocaleTimeString()}
                      </td>
                      <td className="px-4 py-2">
                        <div className="flex items-center gap-1.5">
                          <Icon className="h-3 w-3 flex-shrink-0" />
                          <Badge label={entry.level} color={style.color} />
                        </div>
                      </td>
                      <td className="px-4 py-2 text-gray-800 break-all">
                        {entry.message}
                        {entry.exception && (
                          <details className="mt-1">
                            <summary className="cursor-pointer text-red-600 text-xs">Show exception</summary>
                            <pre className="mt-1 p-2 bg-red-50 rounded text-red-700 overflow-x-auto whitespace-pre-wrap">{entry.exception}</pre>
                          </details>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            <div ref={bottomRef} />
          </div>
        )}
      </Card>

      <p className="text-xs text-gray-400 text-center">
        Showing {filtered.length} entries · Log files stored in <code className="bg-gray-100 px-1 rounded">logs/</code> folder next to the API executable.
        Configure Serilog in <code className="bg-gray-100 px-1 rounded">appsettings.json</code> to adjust log levels.
      </p>
    </div>
  );
}
