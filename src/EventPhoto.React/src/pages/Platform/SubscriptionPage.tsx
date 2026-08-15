import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Key, ShieldCheck, AlertTriangle, Clock, Zap, Building2, Users, CalendarDays, Save } from 'lucide-react';
import toast from 'react-hot-toast';
import { subscriptionApi } from '../../api/subscription';

const PLAN_COLORS: Record<string, string> = {
  Trial:        'text-gray-400 bg-gray-400/10 border-gray-400/20',
  Starter:      'text-blue-400 bg-blue-400/10 border-blue-400/20',
  Professional: 'text-indigo-400 bg-indigo-400/10 border-indigo-400/20',
  Enterprise:   'text-purple-400 bg-purple-400/10 border-purple-400/20',
};

const STATE_CONFIG: Record<string, { color: string; icon: React.ComponentType<{ className?: string }>; label: string }> = {
  Trial:        { color: 'text-gray-400',   icon: Clock,         label: 'Free Trial'     },
  Active:       { color: 'text-green-400',  icon: ShieldCheck,   label: 'Active'         },
  GracePeriod:  { color: 'text-yellow-400', icon: AlertTriangle, label: 'Grace Period'   },
  Expired:      { color: 'text-red-400',    icon: AlertTriangle, label: 'Expired'        },
  Cancelled:    { color: 'text-red-400',    icon: AlertTriangle, label: 'Cancelled'      },
};

const PLAN_FEATURES: Record<string, string[]> = {
  Trial:        ['5 events', '2 studio users', 'Core gallery features', 'Basic QR codes'],
  Starter:      ['20 events', '5 studio users', 'Guest Uploads™', 'Custom branding'],
  Professional: ['100 events', 'Unlimited users', 'AI Face Search', 'Gallery themes', 'Priority support'],
  Enterprise:   ['Unlimited events', 'Unlimited users', 'All features', 'White-label', 'Dedicated support'],
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

  const [form, setForm] = useState({
    licenseKey:  '',
    studioEmail: '',
    plan:        'Professional',
    expiresAt:   new Date(Date.now() + 365 * 24 * 3600 * 1000).toISOString().slice(0, 10),
  });

  const { data: sub, isLoading } = useQuery({
    queryKey: ['subscription'],
    queryFn: subscriptionApi.get,
  });

  const activateMut = useMutation({
    mutationFn: () => subscriptionApi.activate({
      licenseKey:  form.licenseKey,
      studioEmail: form.studioEmail,
      plan:        form.plan,
      expiresAt:   new Date(form.expiresAt).toISOString(),
    }),
    onSuccess: () => {
      toast.success('License activated!');
      qc.invalidateQueries({ queryKey: ['subscription'] });
      setForm(f => ({ ...f, licenseKey: '' }));
    },
    onError: () => toast.error('Activation failed. Check your license key.'),
  });

  if (isLoading || !sub) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const stateInfo  = STATE_CONFIG[sub.state] ?? STATE_CONFIG.Trial;
  const StateIcon  = stateInfo.icon;
  const features   = PLAN_FEATURES[sub.plan] ?? PLAN_FEATURES.Trial;
  const maxEventsLabel     = sub.maxEvents === 0 ? 'Unlimited' : String(sub.maxEvents);
  const maxUsersLabel      = sub.maxUsersPerStudio === 0 ? 'Unlimited' : String(sub.maxUsersPerStudio);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">Subscription Center</h1>
        <p className="text-sm text-gray-400 mt-1">Manage your PixBridge Studio OS license</p>
      </div>

      {/* State Banner */}
      {sub.state === 'GracePeriod' && (
        <div className="bg-yellow-950/40 border border-yellow-700/40 rounded-2xl px-5 py-4 flex items-center gap-3">
          <AlertTriangle className="w-5 h-5 text-yellow-400 flex-shrink-0" />
          <div>
            <p className="text-sm font-semibold text-yellow-300">Grace Period Active</p>
            <p className="text-xs text-yellow-200/70 mt-0.5">
              Your license expired but you have {sub.gracePeriodDaysRemaining} day
              {sub.gracePeriodDaysRemaining !== 1 ? 's' : ''} of full access remaining.
              Please renew to avoid service interruption.
            </p>
          </div>
        </div>
      )}
      {sub.state === 'Expired' && (
        <div className="bg-red-950/40 border border-red-700/40 rounded-2xl px-5 py-4 flex items-center gap-3">
          <AlertTriangle className="w-5 h-5 text-red-400 flex-shrink-0" />
          <p className="text-sm text-red-300">
            Your subscription has expired. Enter a new license key to restore full access.
          </p>
        </div>
      )}

      {/* Status + Plan Cards */}
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
                <span className="text-lg font-bold text-white">{sub.plan}</span>
                <span className={`text-xs px-2 py-0.5 rounded border ${PLAN_COLORS[sub.plan]}`}>
                  {sub.plan}
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
      <div className="grid grid-cols-3 gap-4">
        <StatCard icon={CalendarDays} label="Max Events" value={maxEventsLabel}
          sub={sub.maxEvents === 0 ? 'No limit' : undefined} />
        <StatCard icon={Users} label="Max Users" value={maxUsersLabel}
          sub={sub.maxUsersPerStudio === 0 ? 'No limit' : undefined} />
        <StatCard icon={Building2} label="Activated"
          value={sub.activatedAt ? new Date(sub.activatedAt).toLocaleDateString() : '—'}
          sub={sub.activatedAt ? 'License active' : 'Trial mode'} />
      </div>

      {/* Included Features */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5">
        <h2 className="text-sm font-semibold text-white mb-3">Included in {sub.plan}</h2>
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
          {sub.state === 'Trial' ? 'Activate License' : 'Renew / Change Plan'}
        </h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs text-gray-400 mb-1.5">License Key</label>
            <input
              type="text"
              value={form.licenseKey}
              onChange={e => setForm(f => ({ ...f, licenseKey: e.target.value }))}
              placeholder="XXXX-XXXX-XXXX-XXXX"
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
          <div>
            <label className="block text-xs text-gray-400 mb-1.5">Plan</label>
            <select
              value={form.plan}
              onChange={e => setForm(f => ({ ...f, plan: e.target.value }))}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-xl text-sm text-white focus:outline-none focus:border-indigo-500"
            >
              {['Starter', 'Professional', 'Enterprise'].map(p => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1.5">Expiry Date</label>
            <input
              type="date"
              value={form.expiresAt}
              onChange={e => setForm(f => ({ ...f, expiresAt: e.target.value }))}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-xl text-sm text-white focus:outline-none focus:border-indigo-500"
            />
          </div>
        </div>
        <button
          onClick={() => activateMut.mutate()}
          disabled={activateMut.isPending || !form.licenseKey || !form.studioEmail}
          className="mt-4 flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white rounded-xl text-sm font-medium transition-colors"
        >
          <Save className="w-4 h-4" />
          {activateMut.isPending ? 'Activating…' : 'Activate License'}
        </button>
      </div>
    </div>
  );
}
