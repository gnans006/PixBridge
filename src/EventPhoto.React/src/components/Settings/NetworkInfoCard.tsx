import { useQuery } from '@tanstack/react-query';
import { Check, Copy, Globe, HardDrive, Loader2, Network, QrCode, Server, Wifi } from 'lucide-react';
import { useState } from 'react';
import { systemApi } from '../../api/system';

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <button
      onClick={handleCopy}
      title="Copy to clipboard"
      className="ml-2 rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-700 hover:text-slate-200"
    >
      {copied
        ? <Check className="h-3.5 w-3.5 text-emerald-400" />
        : <Copy className="h-3.5 w-3.5" />}
    </button>
  );
}

function InfoRow({ icon: Icon, label, value, copyable = false, highlight = false }: {
  icon: typeof Server;
  label: string;
  value: string;
  copyable?: boolean;
  highlight?: boolean;
}) {
  return (
    <div className="flex items-center justify-between rounded-xl bg-slate-800/50 px-4 py-3">
      <div className="flex items-center gap-3">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-700">
          <Icon className="h-4 w-4 text-slate-400" />
        </div>
        <span className="text-sm text-slate-400">{label}</span>
      </div>
      <div className="flex items-center gap-1">
        <span className={`font-mono text-sm ${highlight ? 'font-semibold text-indigo-300' : 'text-slate-200'}`}>
          {value}
        </span>
        {copyable && <CopyButton text={value} />}
      </div>
    </div>
  );
}

export function NetworkInfoCard() {
  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['system-network'],
    queryFn: () => systemApi.getNetworkInfo(),
    refetchInterval: 30_000,
    retry: 1,
  });

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
      {/* Header */}
      <div className="mb-5 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-500/20">
            <Network className="h-5 w-5 text-indigo-400" />
          </div>
          <div>
            <h2 className="text-base font-semibold text-slate-100">Network Information</h2>
            <p className="text-xs text-slate-500">LAN access details for guest connections</p>
          </div>
        </div>
        <button
          onClick={() => void refetch()}
          className="rounded-lg px-3 py-1.5 text-xs font-medium text-slate-400 transition-colors hover:bg-slate-800 hover:text-slate-200"
        >
          Refresh
        </button>
      </div>

      {isLoading && (
        <div className="flex h-40 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
        </div>
      )}

      {isError && (
        <div className="flex h-40 items-center justify-center rounded-xl border border-rose-800/50 bg-rose-900/20">
          <p className="text-sm text-rose-400">Could not load network info — check API connectivity.</p>
        </div>
      )}

      {data && (
        <div className="space-y-2">
          <InfoRow icon={Server}    label="Host Name"    value={data.hostname} />
          <InfoRow icon={Wifi}      label="Server IP"    value={data.primaryIp} copyable />
          <InfoRow icon={HardDrive} label="API Port"     value={String(data.port)} />
          <InfoRow icon={Globe}     label="Access URL"   value={data.accessibleUrl}  copyable highlight />
          <InfoRow icon={Globe}     label="Public URL"   value={data.publicBaseUrl}  copyable highlight />
          <InfoRow icon={QrCode}    label="QR Base URL"  value={data.qrBaseUrl}      copyable />

          {/* All IPs */}
          {data.allIpAddresses.length > 1 && (
            <div className="rounded-xl bg-slate-800/50 px-4 py-3">
              <div className="mb-2 flex items-center gap-3">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-700">
                  <Network className="h-4 w-4 text-slate-400" />
                </div>
                <span className="text-sm text-slate-400">All Network Addresses</span>
              </div>
              <div className="ml-11 space-y-1">
                {data.allIpAddresses.map((ip) => (
                  <div key={ip} className="flex items-center gap-2">
                    <span className="font-mono text-sm text-slate-300">{ip}</span>
                    <CopyButton text={`http://${ip}:${data.port}`} />
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Guest access tip */}
          <div className="mt-4 rounded-xl border border-indigo-800/40 bg-indigo-900/20 px-4 py-3">
            <p className="text-xs font-medium text-indigo-300">Guest Access</p>
            <p className="mt-0.5 text-xs text-slate-400">
              Guests on the same WiFi network can open{' '}
              <span className="font-mono font-semibold text-indigo-300">{data.accessibleUrl}</span>{' '}
              in their browser or scan a QR code to view their gallery.
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
