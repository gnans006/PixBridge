import { useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Globe, Loader2, Palette, Settings2, SlidersHorizontal } from 'lucide-react';
import { useApplicationSettings } from '../hooks/useApplicationSettings';
import { GeneralTab } from '../components/Settings/SystemSettings/GeneralTab';
import { NetworkTab } from '../components/Settings/SystemSettings/NetworkTab';
import { BrandingTab } from '../components/Settings/SystemSettings/BrandingTab';
import { DefaultsTab } from '../components/Settings/SystemSettings/DefaultsTab';

const TABS = [
  { id: 'general', label: 'General', icon: Settings2 },
  { id: 'network', label: 'Network', icon: Globe },
  { id: 'branding', label: 'Branding', icon: Palette },
  { id: 'defaults', label: 'Defaults', icon: SlidersHorizontal },
] as const;

type TabId = (typeof TABS)[number]['id'];

export default function SystemSettings() {
  const [activeTab, setActiveTab] = useState<TabId>('general');
  const { data: settings, isLoading, isError } = useApplicationSettings();

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
      </div>
    );
  }

  if (isError || !settings) {
    return (
      <div className="rounded-2xl border border-red-500/20 bg-red-500/5 p-6 text-center">
        <p className="text-sm text-red-400">Failed to load application settings.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">System Settings</h1>
        <p className="mt-1 text-sm text-slate-400">
          Central configuration for the PixBridge server — studio identity, public URL, and event defaults.
        </p>
      </div>

      {/* Tab bar */}
      <div className="flex gap-1 rounded-xl border border-slate-800 bg-slate-900 p-1">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`relative flex flex-1 items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-medium
              transition-colors ${
                activeTab === tab.id
                  ? 'text-white'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
          >
            {activeTab === tab.id && (
              <motion.div
                layoutId="system-tab-indicator"
                className="absolute inset-0 rounded-lg bg-slate-800"
                transition={{ type: 'spring', stiffness: 500, damping: 40 }}
              />
            )}
            <tab.icon className="relative z-10 h-4 w-4" />
            <span className="relative z-10 hidden sm:inline">{tab.label}</span>
          </button>
        ))}
      </div>

      {/* Tab content */}
      <AnimatePresence mode="wait">
        <motion.div
          key={activeTab}
          initial={{ opacity: 0, y: 6 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -6 }}
          transition={{ duration: 0.15 }}
        >
          {activeTab === 'general' && <GeneralTab settings={settings} />}
          {activeTab === 'network' && <NetworkTab settings={settings} />}
          {activeTab === 'branding' && <BrandingTab settings={settings} />}
          {activeTab === 'defaults' && <DefaultsTab settings={settings} />}
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
