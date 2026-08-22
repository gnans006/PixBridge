import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { Check, Copy, Download, ExternalLink, Loader2, QrCode, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import { eventsApi } from '../../api/events';
import { apiError } from '../../utils/errorHandler';
import { useApplicationSettings } from '../../hooks/useApplicationSettings';
import { useConfirm } from '../../hooks/useConfirm';
import type { EventWorkspaceResponse } from '../../types';

interface EventQrAccessTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventQrAccessTab({ workspace }: EventQrAccessTabProps) {
  const qc = useQueryClient();
  const confirm = useConfirm();
  const [qrBust, setQrBust] = useState<number | undefined>(undefined);
  const [copiedKey, setCopiedKey] = useState<string | null>(null);
  const { data: appSettings } = useApplicationSettings();

  const refreshMutation = useMutation({
    mutationFn: () => eventsApi.refreshQr(workspace.id),
    onSuccess: () => {
      const bust = Date.now();
      setQrBust(bust);
      qc.invalidateQueries({ queryKey: ['workspace', workspace.id] });
      toast.success('QR code refreshed.');
    },
    onError: (e) => apiError(e, 'Failed to refresh QR code.'),
  });

  const qrImageUrl = eventsApi.getQrCodeUrl(workspace.id, qrBust);
  // Use PublicBaseUrl from ApplicationSettings so the URL shown to admins matches what
  // guests see (router IP / custom domain), not the admin's local LAN address.
  const origin = appSettings?.publicBaseUrl?.replace(/\/$/, '') ?? window.location.origin;
  const galleryUrl = `${origin}/gallery/${workspace.id}`;
  const faceSearchUrl = workspace.enableFaceRecognition
    ? `${origin}/gallery/${workspace.id}/find`
    : null;

  const copy = (text: string, key: string) => {
    navigator.clipboard.writeText(text).then(() => {
      setCopiedKey(key);
      toast.success('Copied to clipboard.');
      setTimeout(() => setCopiedKey(null), 2000);
    });
  };

  const downloadQr = () => {
    if (!workspace.qrCodeUrl) { toast.error('No QR code available.'); return; }
    const a = document.createElement('a');
    a.href = qrImageUrl;
    a.download = `qr-${workspace.name.replace(/\s+/g, '-').toLowerCase()}.png`;
    a.click();
  };

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
      {/* QR display */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-4 flex items-center gap-2">
          <QrCode className="h-4 w-4 text-indigo-400" />
          <h2 className="text-sm font-semibold text-white">Gallery QR Code</h2>
        </div>

        {workspace.qrCodeUrl ? (
          <motion.div
            key={qrBust}
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="flex flex-col items-center gap-5"
          >
            <div className="overflow-hidden rounded-2xl border border-slate-700 bg-white p-4">
              <img
                src={qrImageUrl}
                alt="Gallery QR Code"
                className="h-48 w-48 object-contain"
                onError={(e) => ((e.currentTarget as HTMLImageElement).style.display = 'none')}
              />
            </div>

            <div className="flex flex-wrap justify-center gap-2">
              <button
                onClick={async () => {
                  const ok = await confirm({
                    title: 'Regenerate QR Code?',
                    message: 'The current QR code will stop working immediately. Any printed materials using this QR will need to be reprinted.',
                    confirmLabel: 'Regenerate',
                    variant: 'warning',
                  });
                  if (!ok) return;
                  refreshMutation.mutate();
                }}
                disabled={refreshMutation.isPending}
                className="inline-flex items-center gap-2 rounded-xl border border-slate-700 px-4 py-2 text-sm font-medium text-slate-300 transition-colors hover:border-indigo-500 hover:text-white disabled:opacity-50"
              >
                {refreshMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
                Refresh QR
              </button>
              <button
                onClick={downloadQr}
                className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-indigo-500"
              >
                <Download className="h-4 w-4" /> Download QR
              </button>
            </div>
          </motion.div>
        ) : (
          <div className="flex h-48 flex-col items-center justify-center gap-3 text-slate-500">
            <QrCode className="h-12 w-12 opacity-30" />
            <p className="text-sm">No QR code generated yet.</p>
            <button
              onClick={async () => {
                const ok = await confirm({
                  title: 'Generate QR Code?',
                  message: 'This will generate a new QR code for this event.',
                  confirmLabel: 'Generate',
                  variant: 'info',
                });
                if (!ok) return;
                refreshMutation.mutate();
              }}
              className="text-sm text-indigo-400 hover:text-indigo-300"
            >
              Generate now
            </button>
          </div>
        )}
      </div>

      {/* URLs */}
      <div className="space-y-4">
        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <h2 className="mb-4 text-sm font-semibold text-white">Shareable Links</h2>

          <div className="space-y-3">
            <UrlRow
              label="Gallery URL"
              url={galleryUrl}
              copiedKey={copiedKey}
              urlKey="gallery"
              onCopy={copy}
            />

            <UrlRow
              label="QR Code URL"
              url={workspace.qrCodeUrl ?? '—'}
              copiedKey={copiedKey}
              urlKey="qrcode"
              disabled={!workspace.qrCodeUrl}
              onCopy={copy}
            />

            {faceSearchUrl && (
              <UrlRow
                label="Face Search URL"
                url={faceSearchUrl}
                copiedKey={copiedKey}
                urlKey="facesearch"
                onCopy={copy}
              />
            )}
          </div>
        </div>

        {/* QR info */}
        <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-5">
          <p className="text-xs font-semibold text-slate-400">About QR Codes</p>
          <p className="mt-1.5 text-xs text-slate-500">
            QR codes point to the gallery URL. Refreshing generates a new URL, invalidating old
            printed QR codes. Use with care at live events.
          </p>
        </div>
      </div>
    </div>
  );
}

function UrlRow({
  label,
  url,
  copiedKey,
  urlKey,
  disabled,
  onCopy,
}: {
  label: string;
  url: string;
  copiedKey: string | null;
  urlKey: string;
  disabled?: boolean;
  onCopy: (url: string, key: string) => void;
}) {
  const copied = copiedKey === urlKey;
  return (
    <div className="rounded-xl border border-slate-700/50 p-3">
      <p className="mb-1.5 text-xs font-medium text-slate-400">{label}</p>
      <div className="flex items-center gap-2">
        <span className="flex-1 truncate font-mono text-xs text-slate-300">{url}</span>
        <button
          disabled={disabled}
          onClick={() => onCopy(url, urlKey)}
          className="shrink-0 rounded-lg border border-slate-700 p-1.5 text-slate-400 transition-colors hover:border-indigo-500 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
        >
          {copied ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
        </button>
        {!disabled && (
          <a
            href={url}
            target="_blank"
            rel="noopener noreferrer"
            className="shrink-0 rounded-lg border border-slate-700 p-1.5 text-slate-400 transition-colors hover:border-indigo-500 hover:text-white"
          >
            <ExternalLink className="h-3.5 w-3.5" />
          </a>
        )}
      </div>
    </div>
  );
}
