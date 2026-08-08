import { Palette, Monitor, Sun, Moon, Check } from 'lucide-react';
import { useTheme, type Theme, type AccentColor, type Density } from '../../providers/ThemeProvider';
import { PageShell } from '../../components/UI/PageShell';
import { Card } from '../../components/UI/Card';

const THEMES: { id: Theme; label: string; desc: string; icon: typeof Monitor; preview: string[] }[] = [
  {
    id: 'studio-dark',
    label: 'Studio Dark',
    desc: 'Deep navy workspace — the default studio experience.',
    icon: Moon,
    preview: ['#0B0F19', '#121826', '#1A2235', '#6366F1'],
  },
  {
    id: 'studio-light',
    label: 'Studio Light',
    desc: 'Clean light canvas for bright working environments.',
    icon: Sun,
    preview: ['#F8FAFC', '#FFFFFF', '#F1F5F9', '#6366F1'],
  },
  {
    id: 'studio-midnight',
    label: 'Studio Midnight',
    desc: 'Ultra-deep dark theme for low-light studio sessions.',
    icon: Monitor,
    preview: ['#050810', '#090D1A', '#0F1525', '#7C7EFF'],
  },
];

const ACCENTS: { id: AccentColor; label: string; color: string }[] = [
  { id: 'indigo',  label: 'Indigo',  color: '#6366F1' },
  { id: 'violet',  label: 'Violet',  color: '#8B5CF6' },
  { id: 'emerald', label: 'Emerald', color: '#10B981' },
];

const DENSITIES: { id: Density; label: string; desc: string }[] = [
  { id: 'comfortable', label: 'Comfortable', desc: 'More space between elements — better for large screens.' },
  { id: 'compact',     label: 'Compact',     desc: 'Tighter layout — fits more content on screen.' },
];

function SectionHeader({ children }: { children: React.ReactNode }) {
  return (
    <div className="mb-4 flex items-center gap-2">
      <h2 className="text-sm font-semibold uppercase tracking-widest text-pds-text-muted">{children}</h2>
      <div className="flex-1 border-t border-pds-border" />
    </div>
  );
}

export default function AppearancePage() {
  const { theme, setTheme, accent, setAccent, density, setDensity } = useTheme();

  return (
    <PageShell
      title="Appearance"
      description="Customize the PixBridge Studio OS visual experience. Changes apply instantly."
    >
      <div className="max-w-2xl space-y-10">

        {/* Theme */}
        <section>
          <SectionHeader>Theme</SectionHeader>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            {THEMES.map(({ id, label, desc, icon: Icon, preview }) => (
              <button
                key={id}
                onClick={() => setTheme(id)}
                className={`group relative flex flex-col gap-3 rounded-2xl border p-4 text-left transition-all duration-150 ${
                  theme === id
                    ? 'border-pds-primary bg-pds-primary/8 shadow-pds-glow-sm'
                    : 'border-pds-border bg-pds-card hover:border-pds-primary/50'
                }`}
              >
                {theme === id && (
                  <span className="absolute right-3 top-3 flex h-5 w-5 items-center justify-center rounded-full bg-pds-primary">
                    <Check className="h-3 w-3 text-white" />
                  </span>
                )}
                {/* Colour preview */}
                <div className="flex gap-1.5">
                  {preview.map((c, i) => (
                    <div
                      key={i}
                      className="h-6 flex-1 rounded-lg"
                      style={{ backgroundColor: c }}
                    />
                  ))}
                </div>
                <div className="flex items-center gap-2">
                  <Icon className="h-4 w-4 text-pds-text-muted flex-none" />
                  <span className="text-sm font-semibold text-pds-text">{label}</span>
                </div>
                <p className="text-xs text-pds-text-muted leading-relaxed">{desc}</p>
              </button>
            ))}
          </div>
        </section>

        {/* Accent */}
        <section>
          <SectionHeader>Accent Color</SectionHeader>
          <div className="flex gap-3">
            {ACCENTS.map(({ id, label, color }) => (
              <button
                key={id}
                onClick={() => setAccent(id)}
                className={`flex items-center gap-2.5 rounded-xl border px-4 py-2.5 text-sm font-medium transition-all duration-150 ${
                  accent === id
                    ? 'border-pds-primary bg-pds-primary/10 text-pds-text shadow-pds-glow-sm'
                    : 'border-pds-border bg-pds-card text-pds-text-muted hover:border-pds-primary/50 hover:text-pds-text'
                }`}
              >
                <span className="h-4 w-4 rounded-full flex-none" style={{ backgroundColor: color }} />
                {label}
                {accent === id && <Check className="h-3.5 w-3.5 text-pds-primary ml-auto" />}
              </button>
            ))}
          </div>
        </section>

        {/* Density */}
        <section>
          <SectionHeader>Density</SectionHeader>
          <div className="flex gap-3">
            {DENSITIES.map(({ id, label, desc }) => (
              <button
                key={id}
                onClick={() => setDensity(id)}
                className={`flex flex-1 flex-col gap-1 rounded-2xl border p-4 text-left transition-all duration-150 ${
                  density === id
                    ? 'border-pds-primary bg-pds-primary/8 shadow-pds-glow-sm'
                    : 'border-pds-border bg-pds-card hover:border-pds-primary/50'
                }`}
              >
                <div className="flex items-center justify-between">
                  <span className="text-sm font-semibold text-pds-text">{label}</span>
                  {density === id && (
                    <span className="flex h-4 w-4 items-center justify-center rounded-full bg-pds-primary">
                      <Check className="h-2.5 w-2.5 text-white" />
                    </span>
                  )}
                </div>
                <p className="text-xs text-pds-text-muted">{desc}</p>
              </button>
            ))}
          </div>
        </section>

        {/* Sidebar */}
        <section>
          <SectionHeader>About PixBridge Design System</SectionHeader>
          <Card padding>
            <div className="flex items-start gap-3">
              <Palette className="h-5 w-5 text-pds-primary flex-none mt-0.5" />
              <div>
                <p className="text-sm font-semibold text-pds-text mb-1">PDS — PixBridge Design System</p>
                <p className="text-sm text-pds-text-muted leading-relaxed">
                  All visual tokens (colors, spacing, typography, shadows) are managed centrally.
                  Theme changes propagate instantly via CSS custom properties — no page refresh needed.
                  Your preferences are saved to localStorage and persist across sessions.
                </p>
              </div>
            </div>
          </Card>
        </section>
      </div>
    </PageShell>
  );
}
