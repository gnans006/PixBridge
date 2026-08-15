import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { Upload, Plus, X, Check, QrCode } from 'lucide-react';
import toast from 'react-hot-toast';
import { guestUploadApi, type GuestUploadSession } from '../../api/guestUploads';
import { useApplicationSettings } from '../../hooks/useApplicationSettings';

const STATUS_COLORS: Record<string, string> = {
  Pending:  'text-yellow-400 bg-yellow-400/10 border-yellow-400/20',
  Approved: 'text-green-400 bg-green-400/10 border-green-400/20',
  Rejected: 'text-red-400 bg-red-400/10 border-red-400/20',
};

const SESSION_STATUS_COLORS: Record<string, string> = {
  Active: 'text-green-400 bg-green-400/10 border-green-400/20',
  Closed: 'text-gray-400 bg-gray-400/10 border-gray-400/20',
};

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function GuestUploadsPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const qc = useQueryClient();
  const { data: settings } = useApplicationSettings();

  const [newTitle, setNewTitle]           = useState('');
  const [showCreate, setShowCreate]       = useState(false);
  const [activeSession, setActiveSession] = useState<GuestUploadSession | null>(null);
  const [statusFilter, setStatusFilter]   = useState<string>('');
  const [rejectionInput, setRejectionInput] = useState<Record<string, string>>({});

  // Sessions
  const { data: sessions = [], isLoading: sessionsLoading } = useQuery({
    queryKey: ['guest-sessions', eventId],
    queryFn: () => guestUploadApi.getSessions(eventId!),
    enabled: !!eventId,
  });

  // Uploads (for selected session's event)
  const { data: uploads = [], isLoading: uploadsLoading } = useQuery({
    queryKey: ['guest-uploads', eventId, statusFilter],
    queryFn: () => guestUploadApi.getUploads(eventId!, statusFilter || undefined),
    enabled: !!eventId,
  });

  const createMut = useMutation({
    mutationFn: () => guestUploadApi.createSession(eventId!, newTitle || undefined),
    onSuccess: (session) => {
      toast.success('Upload session created!');
      qc.invalidateQueries({ queryKey: ['guest-sessions', eventId] });
      setNewTitle('');
      setShowCreate(false);
      setActiveSession(session);
    },
    onError: () => toast.error('Failed to create session'),
  });

  const closeMut = useMutation({
    mutationFn: (sessionId: string) => guestUploadApi.closeSession(eventId!, sessionId),
    onSuccess: () => {
      toast.success('Session closed');
      qc.invalidateQueries({ queryKey: ['guest-sessions', eventId] });
    },
    onError: () => toast.error('Failed to close session'),
  });

  const moderateMut = useMutation({
    mutationFn: ({ uploadId, approve, reason }: { uploadId: string; approve: boolean; reason?: string }) =>
      guestUploadApi.moderate(eventId!, uploadId, approve, reason),
    onSuccess: (_, { approve }) => {
      toast.success(approve ? 'Photo approved' : 'Photo rejected');
      qc.invalidateQueries({ queryKey: ['guest-uploads', eventId] });
    },
    onError: () => toast.error('Moderation failed'),
  });

  const pending   = uploads.filter(u => u.moderationStatus === 'Pending').length;
  const approved  = uploads.filter(u => u.moderationStatus === 'Approved').length;
  const rejected  = uploads.filter(u => u.moderationStatus === 'Rejected').length;

  const uploadUrl = activeSession
    ? `${settings?.publicBaseUrl ?? ''}/upload/${activeSession.sessionCode}`
    : null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Guest Uploads</h1>
          <p className="text-sm text-gray-400 mt-1">Share Your Moments™ — manage guest photo submissions</p>
        </div>
        <button
          onClick={() => setShowCreate(v => !v)}
          className="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-xl text-sm font-medium transition-colors"
        >
          <Plus className="w-4 h-4" />
          New Session
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Pending Review', value: pending,  color: 'text-yellow-400' },
          { label: 'Approved',       value: approved, color: 'text-green-400'  },
          { label: 'Rejected',       value: rejected, color: 'text-red-400'    },
        ].map(s => (
          <div key={s.label} className="bg-gray-900 border border-gray-800 rounded-2xl p-4">
            <p className="text-xs text-gray-400">{s.label}</p>
            <p className={`text-3xl font-bold mt-1 ${s.color}`}>{s.value}</p>
          </div>
        ))}
      </div>

      {/* Create Session Form */}
      {showCreate && (
        <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5">
          <h2 className="text-sm font-semibold text-white mb-3">Create Upload Session</h2>
          <div className="flex gap-3">
            <input
              value={newTitle}
              onChange={e => setNewTitle(e.target.value)}
              placeholder="Session label (e.g. Wedding Guests)"
              className="flex-1 px-4 py-2 bg-gray-800 border border-gray-700 rounded-xl text-sm text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500"
            />
            <button
              onClick={() => createMut.mutate()}
              disabled={createMut.isPending}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white rounded-xl text-sm font-medium transition-colors"
            >
              {createMut.isPending ? 'Creating…' : 'Create'}
            </button>
          </div>
        </div>
      )}

      {/* Active Session QR / Link Display */}
      {activeSession && uploadUrl && (
        <div className="bg-indigo-950/40 border border-indigo-800/40 rounded-2xl p-5 flex items-start gap-4">
          <QrCode className="w-8 h-8 text-indigo-400 flex-shrink-0 mt-0.5" />
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-white">
              Session "{activeSession.title ?? activeSession.sessionCode}" is live
            </p>
            <p className="text-xs text-gray-400 mt-1 mb-2">Share this link or session code with your guests:</p>
            <div className="flex items-center gap-2">
              <code className="flex-1 text-xs bg-gray-900 border border-gray-700 rounded-lg px-3 py-1.5 text-indigo-300 truncate">
                {uploadUrl}
              </code>
              <span className="text-sm font-mono font-bold text-white bg-indigo-600 px-3 py-1.5 rounded-lg">
                {activeSession.sessionCode}
              </span>
            </div>
          </div>
          <button
            onClick={() => setActiveSession(null)}
            className="text-gray-500 hover:text-gray-300 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Sessions List */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl overflow-hidden">
        <div className="px-5 py-4 border-b border-gray-800">
          <h2 className="text-sm font-semibold text-white">Upload Sessions</h2>
        </div>
        {sessionsLoading ? (
          <div className="px-5 py-8 text-center text-gray-500 text-sm">Loading sessions…</div>
        ) : sessions.length === 0 ? (
          <div className="px-5 py-8 text-center text-gray-500 text-sm">
            No sessions yet. Create one to start collecting guest photos.
          </div>
        ) : (
          <div className="divide-y divide-gray-800">
            {sessions.map(s => (
              <div key={s.id} className="px-5 py-4 flex items-center gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-white">{s.title ?? '(untitled)'}</span>
                    <code className="text-xs text-indigo-300 bg-indigo-950/50 px-2 py-0.5 rounded">
                      {s.sessionCode}
                    </code>
                    <span className={`text-xs px-2 py-0.5 rounded border ${SESSION_STATUS_COLORS[s.status]}`}>
                      {s.status}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-0.5">
                    {s.photoCount} photos · Created {new Date(s.createdAt).toLocaleDateString()}
                    {s.closedAt && ` · Closed ${new Date(s.closedAt).toLocaleDateString()}`}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  {s.status === 'Active' && (
                    <>
                      <button
                        onClick={() => setActiveSession(prev => prev?.id === s.id ? null : s)}
                        className="text-xs text-indigo-400 hover:text-indigo-300 transition-colors flex items-center gap-1"
                      >
                        <QrCode className="w-3.5 h-3.5" />
                        Share
                      </button>
                      <button
                        onClick={() => closeMut.mutate(s.id)}
                        disabled={closeMut.isPending}
                        className="text-xs text-red-400 hover:text-red-300 transition-colors"
                      >
                        Close
                      </button>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Uploads Moderation */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl overflow-hidden">
        <div className="px-5 py-4 border-b border-gray-800 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-white">Photo Submissions</h2>
          <div className="flex gap-2">
            {['', 'Pending', 'Approved', 'Rejected'].map(f => (
              <button
                key={f || 'all'}
                onClick={() => setStatusFilter(f)}
                className={`px-3 py-1 rounded-lg text-xs font-medium transition-colors ${
                  statusFilter === f
                    ? 'bg-indigo-600 text-white'
                    : 'text-gray-400 hover:text-white hover:bg-gray-800'
                }`}
              >
                {f || 'All'}
              </button>
            ))}
          </div>
        </div>

        {uploadsLoading ? (
          <div className="px-5 py-8 text-center text-gray-500 text-sm">Loading uploads…</div>
        ) : uploads.length === 0 ? (
          <div className="px-5 py-8 text-center text-gray-500 text-sm">
            No uploads{statusFilter ? ` with status "${statusFilter}"` : ''} yet.
          </div>
        ) : (
          <div className="divide-y divide-gray-800">
            {uploads.map(u => (
              <div key={u.id} className="px-5 py-4">
                <div className="flex items-start gap-4">
                  {/* File icon / thumb placeholder */}
                  <div className="w-12 h-12 rounded-xl bg-gray-800 flex items-center justify-center flex-shrink-0">
                    <Upload className="w-5 h-5 text-gray-500" />
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-sm font-medium text-white truncate max-w-[240px]">
                        {u.originalFileName}
                      </span>
                      <span className={`text-xs px-2 py-0.5 rounded border ${STATUS_COLORS[u.moderationStatus]}`}>
                        {u.moderationStatus}
                      </span>
                    </div>
                    <p className="text-xs text-gray-500 mt-0.5">
                      {formatBytes(u.fileSizeBytes)} · {u.contentType} ·{' '}
                      {new Date(u.uploadedAt).toLocaleString()}
                    </p>
                    {u.rejectionReason && (
                      <p className="text-xs text-red-400 mt-1">Reason: {u.rejectionReason}</p>
                    )}

                    {/* Reject reason input for pending */}
                    {u.moderationStatus === 'Pending' && (
                      <div className="flex items-center gap-2 mt-2">
                        <input
                          value={rejectionInput[u.id] ?? ''}
                          onChange={e => setRejectionInput(prev => ({ ...prev, [u.id]: e.target.value }))}
                          placeholder="Rejection reason (optional)"
                          className="flex-1 text-xs px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500"
                        />
                        <button
                          onClick={() => moderateMut.mutate({ uploadId: u.id, approve: true })}
                          disabled={moderateMut.isPending}
                          className="flex items-center gap-1 px-3 py-1.5 bg-green-600/20 hover:bg-green-600/30 border border-green-600/30 text-green-400 rounded-lg text-xs font-medium transition-colors"
                        >
                          <Check className="w-3.5 h-3.5" />
                          Approve
                        </button>
                        <button
                          onClick={() => moderateMut.mutate({
                            uploadId: u.id,
                            approve: false,
                            reason: rejectionInput[u.id],
                          })}
                          disabled={moderateMut.isPending}
                          className="flex items-center gap-1 px-3 py-1.5 bg-red-600/20 hover:bg-red-600/30 border border-red-600/30 text-red-400 rounded-lg text-xs font-medium transition-colors"
                        >
                          <X className="w-3.5 h-3.5" />
                          Reject
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
