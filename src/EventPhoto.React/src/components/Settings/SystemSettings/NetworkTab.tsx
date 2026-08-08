import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import {
  CheckCircle2, Copy, Check, ExternalLink, Globe, Loader2,
  Network, QrCode, RefreshCw, Server, Wifi, XCircle
} from 'lucide-react';
import type { ApplicationSettings } from '../../../api/applicationSettings';
import {
  useNetworkInformation,
  useTestPublicUrl,
  useUpdateApplicationSettings,
} from '../../../hooks/useApplicationSettings';
import { Button } from '../../UI/Button';
import { Input } from '../../UI/Input';

interface Props {
  settings: ApplicationSettings;
}

interface FormValues {
  publicBaseUrl: string;
  serverPort: number;
}

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      onClick={async () => {
        await navigator.clipboard.writeText(text);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      }}
      className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-700 hover:text-white"
    >
      {copied ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
    </button>
  );
}

function InfoRow({
  icon: Icon,
  label,
  value,
  highlight = false,
  badge,
}: {
  icon: typeof Server;
  label: string;
  value: string;
  highlight?: boolean;
  badge?: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between rounded-xl bg-slate-800/50 px-4 py-3">
      <div className="flex items-center gap-3">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-700/60">
          <Icon className="h-4 w-4 text-slate-400" />
        </div>
        <span className="text-sm text-slate-400">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        {badge}
        <span className={`font-mono text-sm ${highlight ? 'font-semibold text-indigo-300' : 'text-slate-200'}`}>
          {value}
        </span>
        <CopyButton text={value} />
      </div>
    </div>
  );
}

export function NetworkTab({ settings }: Props) {
  const { data: netInfo, isLoading: netLoading, refetch } = useNetworkInformation(settings.serverPort);
  const { mutate: testUrl, data: testResult, isPending: isTesting, reset: resetTest } = useTestPublicUrl();
  const { mutate: save, isPending: isSaving } = useUpdateApplicationSettings();

  const { register, handleSubmit, reset, watch, formState: { errors, isDirty } } = useForm<FormValues>({
    defaultValues: { publicBaseUrl: settings.publicBaseUrl, serverPort: settings.serverPort },
  });

  useEffect(() => {
    reset({ publicBaseUrl: settings.publicBaseUrl, serverPort: settings.serverPort });
  }, [settings, reset]);

  const currentUrl = watch('publicBaseUrl');

  const onSubmit = (values: FormValues) => {
    resetTest();
    save({ ...settings, publicBaseUrl: values.publicBaseUrl, serverPort: Number(values.serverPort) });
  };

  const handleTest = () => {
    if (currentUrl) testUrl(currentUrl.trim());
  };

  return (
    <div className="space-y-6">
      {/* Live network panel */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-violet-500/10">
              <Network className="h-5 w-5 text-violet-400" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-white">Live Network Information</h2>
              <p className="text-xs text-slate-400">Auto-detected from the server OS</p>
            </div>
          </div>
          <button
            type="button"
            onClick={() => void refetch()}
            className="rounded-lg p-2 text-slate-400 transition-colors hover:bg-slate-800 hover:text-white"
          >
            <RefreshCw className={`h-4 w-4 ${netLoading ? 'animate-spin' : ''}`} />
          </button>
        </div>

        {netLoading ? (
          <div className="flex h-32 items-center justify-center">
            <Loader2 className="h-5 w-5 animate-spin text-slate-500" />
          </div>
        ) : netInfo ? (
          <div className="space-y-2">
            <InfoRow icon={Server} label="Server Name" value={settings.serverName} />
            <InfoRow icon={Server} label="Machine Name" value={netInfo.machineName} />
            <InfoRow icon={Wifi} label="Primary LAN IP" value={netInfo.primaryIpAddress} highlight />
            <InfoRow icon={Server} label="Port" value={String(netInfo.port)} />
            <InfoRow
              icon={Globe}
              label="LAN Access URL"
              value={netInfo.accessibleLanUrl}
              highlight
              badge={
                <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium
                  ${netInfo.isLanReachable ? 'bg-emerald-500/10 text-emerald-400' : 'bg-red-500/10 text-red-400'}`}>
                  {netInfo.isLanReachable
                    ? <><CheckCircle2 className="h-3 w-3" /> Online</>
                    : <><XCircle className="h-3 w-3" /> Offline</>}
                </span>
              }
            />

            {netInfo.allIpAddresses.length > 1 && (
              <div className="rounded-xl bg-slate-800/50 px-4 py-3">
                <p className="mb-2 text-xs text-slate-500">All detected addresses</p>
                <div className="flex flex-wrap gap-2">
                  {netInfo.allIpAddresses.map(ip => (
                    <span key={ip} className="rounded-md bg-slate-700 px-2 py-0.5 font-mono text-xs text-slate-300">
                      {ip}
                    </span>
                  ))}
                </div>
              </div>
            )}

            <div className="rounded-xl border border-indigo-500/20 bg-indigo-500/5 px-4 py-3">
              <p className="text-xs text-indigo-300">
                <QrCode className="mr-1 inline h-3.5 w-3.5" />
                Guest tip — share this URL or scan any event QR code:
                <span className="ml-1 font-mono font-semibold">{netInfo.accessibleLanUrl}</span>
              </p>
            </div>
          </div>
        ) : (
          <p className="text-center text-sm text-slate-500">Could not detect network information.</p>
        )}
      </div>

      {/* Public Base URL form */}
      <form onSubmit={handleSubmit(onSubmit)} className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-500/10">
            <Globe className="h-5 w-5 text-emerald-400" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-white">Public Base URL</h2>
            <p className="text-xs text-slate-400">
              All QR codes, gallery links, and download URLs are built from this value.
              Change this once when switching servers or moving to a domain name.
            </p>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label className="mb-1.5 block text-xs font-medium text-slate-300">URL</label>
            <div className="flex gap-2">
              <Input
                {...register('publicBaseUrl', {
                  required: 'Public base URL is required.',
                  validate: (v) => {
                    try {
                      const u = new URL(v.trim());
                      return ['http:', 'https:'].includes(u.protocol) || 'Must be http:// or https://';
                    } catch {
                      return 'Must be a valid URL.';
                    }
                  },
                })}
                placeholder="http://192.168.0.59:5000  or  https://photos.studio.com"
                className="flex-1 font-mono text-sm"
              />
              <button
                type="button"
                onClick={handleTest}
                disabled={isTesting || !currentUrl}
                className="flex items-center gap-2 rounded-xl border border-slate-700 bg-slate-800 px-4 py-2 text-sm text-slate-300
                  transition-colors hover:border-slate-600 hover:text-white disabled:opacity-40"
              >
                {isTesting ? <Loader2 className="h-4 w-4 animate-spin" /> : <ExternalLink className="h-4 w-4" />}
                Test
              </button>
            </div>
            {errors.publicBaseUrl && (
              <p className="mt-1 text-xs text-red-400">{errors.publicBaseUrl.message}</p>
            )}
          </div>

          {/* Test result */}
          {testResult && (
            <div className={`flex items-start gap-3 rounded-xl px-4 py-3 text-sm
              ${testResult.isReachable
                ? 'border border-emerald-500/20 bg-emerald-500/5'
                : 'border border-red-500/20 bg-red-500/5'}`}>
              {testResult.isReachable
                ? <CheckCircle2 className="mt-0.5 h-4 w-4 flex-shrink-0 text-emerald-400" />
                : <XCircle className="mt-0.5 h-4 w-4 flex-shrink-0 text-red-400" />}
              <div>
                <p className={`font-medium ${testResult.isReachable ? 'text-emerald-300' : 'text-red-300'}`}>
                  {testResult.isReachable ? 'URL is reachable' : 'URL is not reachable'}
                </p>
                {testResult.statusCode && (
                  <p className="text-xs text-slate-400">
                    HTTP {testResult.statusCode}
                    {testResult.responseTimeMs !== null && ` · ${testResult.responseTimeMs}ms`}
                  </p>
                )}
                {testResult.errorMessage && (
                  <p className="text-xs text-red-400">{testResult.errorMessage}</p>
                )}
              </div>
            </div>
          )}

          <div>
            <label className="mb-1.5 block text-xs font-medium text-slate-300">Server Port</label>
            <Input
              type="number"
              {...register('serverPort', {
                required: 'Port is required.',
                min: { value: 1, message: 'Minimum port is 1.' },
                max: { value: 65535, message: 'Maximum port is 65535.' },
              })}
              className="w-32"
            />
            {errors.serverPort && (
              <p className="mt-1 text-xs text-red-400">{errors.serverPort.message}</p>
            )}
          </div>
        </div>

        <div className="mt-6 flex justify-end">
          <Button type="submit" disabled={!isDirty || isSaving}>
            {isSaving ? 'Saving…' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </div>
  );
}
