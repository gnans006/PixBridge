import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Key, ShieldCheck, AlertTriangle, Clock, Zap, Building2, Users, CalendarDays, Save, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import { subscriptionApi } from '../../api/subscription';

const PLAN_COLORS: Record<string, string> = {
  Trial:         'text-gray-400 bg-gray-400/10 border-gray-400/20',
  ExtendedTrial: 'text-blue-400 bg-blue-400/10 border-blue-400/20',
  Professional:  'text-indigo-400 bg-indigo-400/10 border-indigo-400/20',
  Premium:       'text-purple-400 bg-purple-400/10 border-purple-400/20',
};

const STATE_CONFIG: Record<string, { color: string; icon: React.ComponentType<{ className?: string }>; label: string }> = {
  Trial:        { color: 'text-gray-400',   icon: Clock,         label: 'Free Trial'   },
  Active:       { color: 'text-green-400',  icon: ShieldCheck,   label: 'Active'       },
  GracePeriod:  { color: 'text-yellow-400', icon: AlertTriangle, label: 'Grace Period' },
  Expired:      { color: 'text-red-400',    icon: AlertTriangle, label: 'Expired'      },
  Cancelled:    { color: 'text-red-400',    icon: AlertTriangle, label: 'Cancelled'    },
};

const PLAN_FEATURES: Record<string, string[]> = {
  Trial:         ['5 events', '3 studio users', 'Find My Photos™', 'AI Studio', 'Branding', 'Deployment Center'],
  ExtendedTrial: ['5 events', '3 studio users', 'Find My Photos™', 'AI Studio', 'Branding', 'Deployment Center'],
  Professional:  ['100 events', '10 studio users', 'Find My Photos™', 'AI Studio', 'Branding', 'Deployment Center', 'Guest Uploads'],
  Premium:       ['Unlimited events', 'Unlimited users', 'All Professional features', 'Future: Analytics', 'Future: Multi-Branch'],
};

function StatCard({ icon: Icon, label, value, sub }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string; sub?: string }) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-2xl p-4">
      <div className="flex items-center gap-2 mb-2">
        <Icon className="w-4 h-4 text-indigo-400" />
        <span className="text-xs text-gray-400">{label}</span>
      </div>
      <p className="text-xl font-bold text-white">{value}</p>
      {sub && <p className="text-xs text-gray-500 mt-0.5">{sub}</p>}
    </div>
  );
}

export default function SubscriptionPage() {
  const qc = useQueryClient();

  const [form, setForm] = useState({ licenseKey: '', studioEmail: '' });

  const { data: sub, isLoading } = useQuery({
    queryKey: ['subscription'],
    queryFn: subscriptionApi.get,
  });

  const activateMut = useMutation({
    mutationFn: () => subscriptionApi.activate({
      licenseKey:  form.licenseKey,
      studioEmail: form.studioEmail,
    }),
    onSuccess: () => {
      toast.success('License activated!');
      qc.invalidateQueries({ queryKey: ['subscription'] });
      setForm({ licenseKey: '', studioEmail: '' });
    },
    onError: () => toast.error('Activation failed. Check your license key.'),
  });

  const extendTrialMut = useMutation({
    mutationFn: subscriptionApi.extendTrial,
    onSuccess: () => {
      toast.success('Trial extended by 15 days!');
      qc.invalidateQueries({ queryKey: ['subscription'] });
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.error ?? 'Extension failed.';
      toast.error(msg);
    },
  });

  if (isLoading || !sub) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const stateInfo        = STATE_CONFIG[sub.state] ?? STATE_CONFIG.Trial;
  const StateIcon        = stateInfo.icon;
  const features         = PLAN_FEATURES[sub.plan] ?? PLAN_FEATURES.Trial;
  const maxEventsLabel   = sub.maxEvents === 0 ? 'Unlimited' : String(sub.maxEvents);
  const maxUsersLabel    = sub.maxUsersPerStudio === 0 ? 'Unlimited' : String(sub.maxUsersPerStudio);
  const daysLabel        = sub.daysRemaining !== null ? `${sub.daysRemaining} day${sub.daysRemaining !== 1 ? 's' : ''} left` : '—';
  const isTrialPlan      = sub.plan === 'Trial' || sub.plan === 'ExtendedTrial';
  const canExtendTrial   = isTrialPlan && !sub.hasUsedTrialExtension &&
                           (sub.state === 'Trial' || sub.state === 'GracePeriod');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">Licensing</h1>
        <p className="text-sm text-gray-400 mt-1">Manage your PixBridge Studio license</p>
      </div>

      {/* State Banners */}
      {sub.state === 'GracePeriod' && (
        <div className="bg-yellow-950/40 border border-yellow-700/40 rounded-2xl px-5 py-4 flex items-start gap-3">
          <AlertTriangle className="w-5 h-5 text-yellow-400 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-semibold text-yellow-300">Grace Period Active</p>
            <p className="text-xs text-yellow-200/70 mt-0.5">
              Your license expired but you have {sub.gracePeriodDaysRemaining} day
              {sub.gracePeriodDaysRemaining !== 1 ? 's' : ''} of full access remaining.
              Activate a new license to continue without interruption.
            </p>
          </div>
        </div>
      )}
      {sub.state === 'Expired' && (
        <div className="bg-red-950/40 border border-red-700/40 rounded-2xl px-5 py-4 flex items-start gap-3">
          <AlertTriangle className="w-5 h-5 text-red-400 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-semibold text-red-300">Subscription Expired</p>
            <p className="text-xs text-red-200/70 mt-0.5">
              Your subscription has expired. Existing events and photos remain accessible.
              Activate a license key to restore full functionality.
            </p>
          </div>
        </div>
      )}

      {/* Status + Plan */}
      <div className="grid grid-cols-2 gap-4">
        <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5">
          <p className="text-xs text-gray-400 mb-2">Current Status</p>
          <div className="flex items-center gap-3">
            <StateIcon className={`w-8 h-8 ${stateInfo.color}`} />
            <div>
              <p className={`text-lg font-bold ${stateInfo.color}`}>{stateInfo.label}</p>
              {sub.studioEmail && <p className="text-xs text-gray-500">{sub.studioEmail}</p>}
            </div>
          </div>
        </div>

        <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5">
          <p className="text-xs text-gray-400 mb-2">Plan</p>
          <div className="flex items-center gap-3">
            <Zap className="w-8 h-8 text-indigo-400" />
            <div>
              <div className="flex items-center gap-2">
                <span className="text-lg font-bold text-white">
                  {sub.plan === 'ExtendedTrial' ? 'Extended Trial' : sub.plan}
                </span>
                <span className={`text-xs px-2 py-0.5 rounded border ${PLAN_COLORS[sub.plan] ?? PLAN_COLORS.Trial}`}>
                  {sub.plan === 'ExtendedTrial' ? 'Extended Trial' : sub.plan}
                </span>
              </div>
              {sub.expiresAt && (
                <p className="text-xs text-gray-500">
                  Expires {new Date(sub.expiresAt).toLocaleDateString()}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Limits */}
      <div className="grid grid-cols-4 gap-4">
        <StatCard icon={CalendarDays} label="Days Remaining" value={daysLabel}
          sub={sub.durationDays ? `${sub.durationDays}-day license` : undefined} />
        <StatCard icon={CalendarDays} label="Max Events"    value={maxEventsLabel} />
        <StatCard icon={Users}        label="Max Users"     value={maxUsersLabel} />
        <StatCard icon={Building2}    label="Activated"
          value={sub.activatedAt ? new Date(sub.activatedAt).toLocaleDateString() : '—'}
          sub={sub.activatedAt ? 'License active' : 'Trial mode'} />
      </div>

      {/* Trial Extension */}
      {canExtendTrial && (
        <div className="bg-blue-950/30 border border-blue-700/30 rounded-2xl px-5 py-4 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <RefreshCw className="w-5 h-5 text-blue-400 flex-shrink-0" />
            <div>
              <p className="text-sm font-semibold text-blue-300">Free Trial Extension Available</p>
              <p className="text-xs text-blue-200/70 mt-0.5">
                Need more time? Extend your trial by 15 days — one time only.
              </p>
            </div>
          </div>
          <button
            onClick={() => extendTrialMut.mutate()}
            disabled={extendTrialMut.isPending}
            className="flex-shrink-0 flex items-center gap-2 px-4 py-2 bg-blue-700 hover:bg-blue-600 disabled:opacity-50 text-white rounded-xl text-sm font-medium transition-colors"
          >
            <RefreshCw className={`w-4 h-4 ${extendTrialMut.isPending ? 'animate-spin' : ''}`} />
            {extendTrialMut.isPending ? 'Extending…' : 'Extend Trial'}
          </button>
        </div>
      )}
      {isTrialPlan && sub.hasUsedTrialExtension && (
        <div className="bg-gray-900 border border-gray-700 rounded-2xl px-5 py-3">
          <p className="text-xs text-gray-400">
            ✓ Trial extension used — you received an extra 15 days.
          </p>
        </div>
      )}

      {/* Included Features */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5">
        <h2 className="text-sm font-semibold text-white mb-3">
          Included in {sub.plan === 'ExtendedTrial' ? 'Extended Trial' : sub.plan}
        </h2>
        <ul className="grid grid-cols-2 gap-2">
          {features.map(f => (
            <li key={f} className="flex items-center gap-2 text-sm text-gray-300">
              <ShieldCheck className="w-4 h-4 text-green-400 flex-shrink-0" />
              {f}
            </li>
          ))}
        </ul>
      </div>

      {/* Activate / Renew */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5">
        <h2 className="text-sm font-semibold text-white mb-4 flex items-center gap-2">
          <Key className="w-4 h-4 text-indigo-400" />
          {sub.state === 'Trial' || sub.state === 'GracePeriod' ? 'Activate License' : 'Renew License'}
        </h2>
        <p className="text-xs text-gray-400 mb-4">
          Enter the license key provided by PixBridge Support. The plan and duration are
          encoded inside your key and verified automatically.
        </p>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs text-gray-400 mb-1.5">License Key</label>
            <input
              type="text"
              value={form.licenseKey}
              onChange={e => setForm(f => ({ ...f, licenseKey: e.target.value }))}
              placeholder="PXBR-1-…"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-xl text-sm text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1.5">Studio Email</label>
            <input
              type="email"
              value={form.studioEmail}
              onChange={e => setForm(f => ({ ...f, studioEmail: e.target.value }))}
              placeholder="studio@example.com"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-xl text-sm text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500"
            />
          </div>
        </div>
        <button
          onClick={() => activateMut.mutate()}
          disabled={activateMut.isPending || !form.licenseKey.trim() || !form.studioEmail.trim()}
          className="mt-4 flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white rounded-xl text-sm font-medium transition-colors"
        >
          <Save className="w-4 h-4" />
          {activateMut.isPending ? 'Activating…' : 'Activate License'}
        </button>
      </div>
    </div>
  );
}