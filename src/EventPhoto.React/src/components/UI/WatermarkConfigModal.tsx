import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Droplets, Eye, Info, Layers, Palette, Type } from 'lucide-react';
import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { watermarkApi } from '../../api/watermark';
import type {
  UpsertWatermarkConfigRequest,
  WatermarkMode,
  WatermarkScale,
  WatermarkStyle,
} from '../../types';
import { Button } from './Button';
import { Modal } from './Modal';
import { Spinner } from './Spinner';

// ── Constants ─────────────────────────────────────────────────────────────────

const MODES: { value: WatermarkMode; label: string; description: string }[] = [
  { value: 'Disabled', label: 'Disabled', description: 'No watermark is applied.' },
  {
    value: 'StudioBranding',
    label: 'Studio Branding',
    description: 'Studio name and optional logo.',
  },
  { value: 'EventBranding', label: 'Event Branding', description: 'Event name and date.' },
  {
    value: 'StudioAndEvent',
    label: 'Studio + Event',
    description: 'Studio name, logo, and event name combined.',
  },
  { value: 'CustomText', label: 'Custom Text', description: 'A fixed text string you type.' },
  {
    value: 'DynamicTemplate',
    label: 'Dynamic Template',
    description: 'Token-based template resolved at download time.',
  },
];

const STYLES: { value: WatermarkStyle; label: string; icon: string; premium?: boolean }[] = [
  { value: 'BottomRibbon', label: 'Bottom Ribbon', icon: '▬', premium: true },
  { value: 'Corner', label: 'Bottom-Right Corner', icon: '↘' },
  { value: 'Center', label: 'Centre', icon: '⊕' },
  { value: 'Diagonal', label: 'Diagonal', icon: '↗' },
  { value: 'RepeatedPattern', label: 'Repeated Pattern', icon: '⊞' },
];

const SCALES: { value: WatermarkScale; label: string }[] = [
  { value: 'Small', label: 'Small (3%)' },
  { value: 'Medium', label: 'Medium (5%)' },
  { value: 'Large', label: 'Large (8%)' },
  { value: 'Auto', label: 'Auto' },
];

const FONTS: { value: string; label: string }[] = [
  { value: '', label: 'Auto (system default)' },
  { value: 'Montserrat', label: 'Montserrat ★' },
  { value: 'Poppins', label: 'Poppins ★' },
  { value: 'Arial', label: 'Arial' },
  { value: 'Verdana', label: 'Verdana' },
  { value: 'Tahoma', label: 'Tahoma' },
  { value: 'Trebuchet MS', label: 'Trebuchet MS' },
  { value: 'Georgia', label: 'Georgia' },
  { value: 'Times New Roman', label: 'Times New Roman' },
  { value: 'Courier New', label: 'Courier New' },
  { value: 'Impact', label: 'Impact' },
];

const TOKENS = [
  '{StudioName}',
  '{EventName}',
  '{EventDate}',
  '{DownloadDate}',
  '{DownloadTime}',
  '{PhotoName}',
  '{SessionId}',
];

/** Converts a 6-digit hex colour + opacity fraction to a CSS rgba() string. */
function hexToRgba(hex: string, alpha: number): string {
  const m = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  if (!m) return `rgba(255,255,255,${alpha})`;
  return `rgba(${parseInt(m[1], 16)},${parseInt(m[2], 16)},${parseInt(m[3], 16)},${alpha})`;
}

// ── Props ──────────────────────────────────────────────────────────────────────

interface WatermarkConfigModalProps {
  eventId: string;
  eventName: string;
  isOpen: boolean;
  onClose: () => void;
}

// ── Component ──────────────────────────────────────────────────────────────────

export function WatermarkConfigModal({
  eventId,
  eventName,
  isOpen,
  onClose,
}: WatermarkConfigModalProps) {
  const queryClient = useQueryClient();

  // ── Remote state ──────────────────────────────────────────────────────────
  const { data: configData, isLoading } = useQuery({
    queryKey: ['watermark-config', eventId],
    queryFn: async () => {
      const response = await watermarkApi.getConfig(eventId);
      return response.data;
    },
    enabled: isOpen && Boolean(eventId),
  });

  // ── Local form state ──────────────────────────────────────────────────────
  const [enabled, setEnabled] = useState(false);
  const [mode, setMode] = useState<WatermarkMode>('StudioBranding');
  const [style, setStyle] = useState<WatermarkStyle>('Corner');
  const [opacity, setOpacity] = useState(0.6);
  const [scale, setScale] = useState<WatermarkScale>('Medium');
  const [customText, setCustomText] = useState('');
  const [template, setTemplate] = useState('');
  const [logoPath, setLogoPath] = useState('');
  const [includeStudioName, setIncludeStudioName] = useState(true);
  const [includeEventName, setIncludeEventName] = useState(false);
  const [includeDownloadDate, setIncludeDownloadDate] = useState(false);
  const [applyOnDownload, setApplyOnDownload] = useState(true);
  const [textColor, setTextColor] = useState('#ffffff');
  const [fontName, setFontName] = useState('Montserrat');
  const [backgroundOpacity, setBackgroundOpacity] = useState(0.20);
  const [applyOnPreview, setApplyOnPreview] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  // Sync form when remote config loads
  useEffect(() => {
    if (configData) {
      setEnabled(configData.enabled);
      setMode(configData.mode);
      setStyle(configData.style);
      setOpacity(configData.opacity);
      setScale(configData.scale);
      setCustomText(configData.customText ?? '');
      setTemplate(configData.template ?? '');
      setLogoPath(configData.logoPath ?? '');
      setIncludeStudioName(configData.includeStudioName);
      setIncludeEventName(configData.includeEventName);
      setIncludeDownloadDate(configData.includeDownloadDate);
      setApplyOnDownload(configData.applyOnDownload);
      setTextColor(configData.textColor ?? '#ffffff');
      setFontName(configData.fontName ?? 'Montserrat');
      setBackgroundOpacity(configData.backgroundOpacity ?? 0.20);
      setApplyOnPreview(configData.applyOnPreview ?? false);
    }
  }, [configData]);

  // ── Save mutation ─────────────────────────────────────────────────────────
  const saveMutation = useMutation({
    mutationFn: (data: UpsertWatermarkConfigRequest) =>
      watermarkApi.upsertConfig(eventId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['watermark-config', eventId] });
      toast.success('Watermark configuration saved.');
      onClose();
    },
    onError: () => toast.error('Failed to save watermark configuration.'),
  });

  const handleSave = () => {
    saveMutation.mutate({
      enabled,
      mode,
      style,
      opacity,
      scale,
      customText: mode === 'CustomText' ? customText : undefined,
      template: mode === 'DynamicTemplate' ? template : undefined,
      logoPath: logoPath || undefined,
      includeStudioName,
      includeEventName,
      includeDownloadDate,
      applyOnDownload,
      textColor: textColor || '#ffffff',
      fontName: fontName || undefined,
      backgroundOpacity,
      applyOnPreview,
    });
  };

  const insertToken = (token: string) => {
    setTemplate((prev) => prev + token);
  };

  // ── Preview lines — each branding part on its own line ────────────────────
  const previewLines = (() => {
    const today = new Date().toISOString().split('T')[0];
    if (mode === 'CustomText') return [customText || '(your text)'];
    if (mode === 'DynamicTemplate') {
      const resolved = (template || '(your template)')
        .replace('{StudioName}', 'My Studio')
        .replace('{EventName}', eventName)
        .replace('{EventDate}', today)
        .replace('{DownloadDate}', today)
        .replace('{DownloadTime}', '14:30')
        .replace('{PhotoName}', 'IMG_0001')
        .replace('{SessionId}', 'abc123');
      return [resolved];
    }
    const parts: string[] = [];
    if ((mode === 'StudioBranding' || mode === 'StudioAndEvent') && includeStudioName)
      parts.push('My Studio');
    if ((mode === 'EventBranding' || mode === 'StudioAndEvent') && includeEventName)
      parts.push(eventName);
    if (includeDownloadDate) parts.push(today);
    return parts.length > 0 ? parts : ['(preview)'];
  })();

  const isBrandingMode =
    mode === 'StudioBranding' || mode === 'EventBranding' || mode === 'StudioAndEvent';

  return (
    <Modal isOpen={isOpen} title="Watermark Configuration" onClose={onClose}>
      {isLoading ? (
        <div className="flex justify-center py-8">
          <Spinner size="lg" />
        </div>
      ) : (
        <div className="max-h-[75vh] space-y-6 overflow-y-auto pr-1">

          {/* Enable toggle */}
          <div className="flex items-center justify-between rounded-xl border border-gray-200 bg-gray-50 p-4">
            <div>
              <p className="font-medium text-gray-900">Enable Watermarking</p>
              <p className="text-sm text-gray-500">
                Apply watermark to all downloads for this event
              </p>
            </div>
            <Toggle value={enabled} onChange={setEnabled} label="Toggle watermark" />
          </div>

          {enabled && (
            <>
              {/* Apply on Download */}
              <div className="flex items-center justify-between rounded-xl border border-gray-200 p-4">
                <div>
                  <p className="text-sm font-medium text-gray-800">Apply on Download</p>
                  <p className="text-xs text-gray-500">
                    Temporarily bypass watermarking without deleting this configuration
                  </p>
                </div>
                <Toggle value={applyOnDownload} onChange={setApplyOnDownload} label="Toggle apply on download" />
              </div>

              {/* Apply on Preview */}
              <div className="flex items-center justify-between rounded-xl border border-gray-200 p-4">
                <div>
                  <p className="text-sm font-medium text-gray-800">Apply on Preview</p>
                  <p className="text-xs text-gray-500">
                    Also apply watermark when images are viewed inline (lightbox / gallery preview)
                  </p>
                </div>
                <Toggle value={applyOnPreview} onChange={setApplyOnPreview} label="Toggle apply on preview" />
              </div>

              {/* Watermark Mode */}
              <Section icon={<Layers className="h-4 w-4" />} title="Watermark Mode">
                <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                  {MODES.map((m) => (
                    <button
                      key={m.value}
                      type="button"
                      onClick={() => setMode(m.value)}
                      className={`rounded-lg border p-3 text-left text-sm transition-colors ${
                        mode === m.value
                          ? 'border-blue-500 bg-blue-50 text-blue-700'
                          : 'border-gray-200 text-gray-700 hover:border-gray-300 hover:bg-gray-50'
                      }`}
                    >
                      <p className="font-medium">{m.label}</p>
                      <p className="mt-0.5 text-xs text-gray-500">{m.description}</p>
                    </button>
                  ))}
                </div>
              </Section>

              {/* Custom Text */}
              {mode === 'CustomText' && (
                <Section icon={<Type className="h-4 w-4" />} title="Custom Text">
                  <input
                    type="text"
                    value={customText}
                    onChange={(e) => setCustomText(e.target.value)}
                    maxLength={500}
                    placeholder="e.g. © My Photography Studio"
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                  <p className="mt-1 text-xs text-gray-400">{customText.length}/500 characters</p>
                </Section>
              )}

              {/* Dynamic Template */}
              {mode === 'DynamicTemplate' && (
                <Section icon={<Type className="h-4 w-4" />} title="Dynamic Template">
                  <textarea
                    value={template}
                    onChange={(e) => setTemplate(e.target.value)}
                    maxLength={1000}
                    rows={2}
                    placeholder="e.g. © {StudioName}&#10;{EventName} — {DownloadDate}"
                    className="w-full resize-none rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                  <p className="mb-2 mt-1 text-xs text-gray-400">{template.length}/1000 characters</p>
                  <div className="flex flex-wrap gap-1">
                    {TOKENS.map((token) => (
                      <button
                        key={token}
                        type="button"
                        onClick={() => insertToken(token)}
                        className="rounded bg-gray-100 px-2 py-0.5 text-xs text-gray-600 transition-colors hover:bg-blue-100 hover:text-blue-700"
                      >
                        {token}
                      </button>
                    ))}
                  </div>
                </Section>
              )}

              {/* Branding options */}
              {isBrandingMode && (
                <Section icon={<Info className="h-4 w-4" />} title="Include in Watermark">
                  <div className="space-y-2">
                    {(mode === 'StudioBranding' || mode === 'StudioAndEvent') && (
                      <CheckboxRow
                        label="Studio Name"
                        checked={includeStudioName}
                        onChange={setIncludeStudioName}
                      />
                    )}
                    {(mode === 'EventBranding' || mode === 'StudioAndEvent') && (
                      <CheckboxRow
                        label="Event Name"
                        checked={includeEventName}
                        onChange={setIncludeEventName}
                      />
                    )}
                    <CheckboxRow
                      label="Download Date"
                      checked={includeDownloadDate}
                      onChange={setIncludeDownloadDate}
                    />
                  </div>
                </Section>
              )}

              {/* Logo Path */}
              {isBrandingMode && (
                <Section icon={<Palette className="h-4 w-4" />} title="Logo (optional)">
                  <input
                    type="text"
                    value={logoPath}
                    onChange={(e) => setLogoPath(e.target.value)}
                    maxLength={1024}
                    placeholder="Absolute path to logo file, e.g. C:\Logos\studio.png"
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                  <p className="mt-1 text-xs text-gray-400">
                    PNG with transparency recommended. Leave blank to omit logo.
                  </p>
                </Section>
              )}

              {/* Placement Style */}
              <Section icon={<Layers className="h-4 w-4" />} title="Placement Style">
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                  {STYLES.map((s) => (
                    <button
                      key={s.value}
                      type="button"
                      onClick={() => setStyle(s.value)}
                      className={`relative rounded-lg border p-2 text-center text-xs transition-colors ${
                        style === s.value
                          ? 'border-blue-500 bg-blue-50 text-blue-700'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300 hover:bg-gray-50'
                      }`}
                    >
                      {s.premium && (
                        <span className="absolute -right-1 -top-1 rounded-full bg-amber-400 px-1 py-0.5 text-[9px] font-bold text-white leading-none">
                          PRO
                        </span>
                      )}
                      <div className="text-base">{s.icon}</div>
                      <div className="mt-0.5">{s.label}</div>
                    </button>
                  ))}
                </div>
              </Section>

              {/* Ribbon Background Opacity — only for BottomRibbon */}
              {style === 'BottomRibbon' && (
                <Section icon={<Droplets className="h-4 w-4" />} title={`Ribbon Background — ${Math.round(backgroundOpacity * 100)}% opacity`}>
                  <input
                    type="range"
                    min={0}
                    max={1}
                    step={0.05}
                    value={backgroundOpacity}
                    onChange={(e) => setBackgroundOpacity(Number(e.target.value))}
                    className="w-full accent-slate-600"
                  />
                  <div className="mt-1 flex justify-between text-xs text-gray-400">
                    <span>0% (transparent)</span>
                    <span>100% (solid black)</span>
                  </div>
                </Section>
              )}

              {/* Opacity */}
              <Section icon={<Droplets className="h-4 w-4" />} title={`Opacity — ${Math.round(opacity * 100)}%`}>
                <input
                  type="range"
                  min={0}
                  max={1}
                  step={0.05}
                  value={opacity}
                  onChange={(e) => setOpacity(Number(e.target.value))}
                  className="w-full accent-blue-600"
                />
                <div className="mt-1 flex justify-between text-xs text-gray-400">
                  <span>0% (invisible)</span>
                  <span>100% (solid)</span>
                </div>
              </Section>

              {/* Text Size */}
              <Section title="Text Size">
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                  {SCALES.map((s) => (
                    <button
                      key={s.value}
                      type="button"
                      onClick={() => setScale(s.value)}
                      className={`rounded-lg border p-2 text-center text-xs transition-colors ${
                        scale === s.value
                          ? 'border-blue-500 bg-blue-50 text-blue-700'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300 hover:bg-gray-50'
                      }`}
                    >
                      {s.label}
                    </button>
                  ))}
                </div>
              </Section>

              {/* Text Colour */}
              <Section icon={<Palette className="h-4 w-4" />} title="Text Colour">
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <input
                      type="color"
                      id="wm-text-color"
                      value={textColor}
                      onChange={(e) => setTextColor(e.target.value)}
                      className="h-9 w-14 cursor-pointer rounded-lg border border-gray-300 p-0.5"
                      title="Pick watermark text colour"
                    />
                    <label htmlFor="wm-text-color" className="text-xs text-gray-500">
                      Click to pick
                    </label>
                  </div>
                  <div className="flex flex-1 items-center gap-2 rounded-lg border border-gray-200 px-3 py-1.5">
                    <span
                      className="inline-block h-5 w-5 flex-shrink-0 rounded border border-gray-300"
                      style={{ backgroundColor: textColor }}
                    />
                    <span className="font-mono text-sm text-gray-700">{textColor.toUpperCase()}</span>
                  </div>
                </div>
                {/* Quick-pick swatches */}
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {[
                    { hex: '#ffffff', label: 'White' },
                    { hex: '#000000', label: 'Black' },
                    { hex: '#fbbf24', label: 'Gold' },
                    { hex: '#f97316', label: 'Orange' },
                    { hex: '#ef4444', label: 'Red' },
                    { hex: '#3b82f6', label: 'Blue' },
                    { hex: '#10b981', label: 'Green' },
                    { hex: '#a855f7', label: 'Purple' },
                  ].map(({ hex, label }) => (
                    <button
                      key={hex}
                      type="button"
                      title={label}
                      onClick={() => setTextColor(hex)}
                      className={`h-6 w-6 rounded-full border-2 transition-transform hover:scale-110 ${
                        textColor.toLowerCase() === hex ? 'border-blue-500 scale-110' : 'border-gray-200'
                      }`}
                      style={{ backgroundColor: hex }}
                    />
                  ))}
                </div>
              </Section>

              {/* Font */}
              <Section icon={<Type className="h-4 w-4" />} title="Font">
                <select
                  value={fontName}
                  onChange={(e) => setFontName(e.target.value)}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  style={{ fontFamily: fontName ? `"${fontName}", sans-serif` : undefined }}
                >
                  {FONTS.map((f) => (
                    <option key={f.value} value={f.value} style={{ fontFamily: f.value || undefined }}>
                      {f.label}
                    </option>
                  ))}
                </select>
                <p className="mt-1 text-xs text-gray-400">
                  Applied on the server if installed. Falls back to Arial → system default when unavailable.
                </p>
              </Section>

              {/* Live Preview */}
              <Section icon={<Eye className="h-4 w-4" />} title="Live Preview">
                <button
                  type="button"
                  onClick={() => setShowPreview((v) => !v)}
                  className="mb-3 text-xs text-blue-600 underline hover:text-blue-700"
                >
                  {showPreview ? 'Hide preview' : 'Show preview'}
                </button>

                {showPreview && (
                  <WatermarkPreview
                    lines={previewLines}
                    style={style}
                    opacity={opacity}
                    scale={scale}
                    textColor={textColor}
                    fontName={fontName}
                    backgroundOpacity={backgroundOpacity}
                  />
                )}
              </Section>
            </>
          )}

          {/* Action buttons */}
          <div className="flex justify-end gap-3 border-t border-gray-200 pt-4">
            <Button variant="secondary" onClick={onClose} disabled={saveMutation.isPending}>
              Cancel
            </Button>
            <Button onClick={handleSave} isLoading={saveMutation.isPending}>
              Save Configuration
            </Button>
          </div>
        </div>
      )}
    </Modal>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

interface SectionProps {
  icon?: React.ReactNode;
  title: string;
  children: React.ReactNode;
}

function Section({ icon, title, children }: SectionProps) {
  return (
    <div>
      <div className="mb-2 flex items-center gap-2">
        {icon && <span className="text-gray-500">{icon}</span>}
        <p className="text-sm font-semibold text-gray-700">{title}</p>
      </div>
      {children}
    </div>
  );
}

interface ToggleProps {
  value: boolean;
  onChange: (v: boolean) => void;
  label: string;
}

function Toggle({ value, onChange, label }: ToggleProps) {
  return (
    <button
      type="button"
      onClick={() => onChange(!value)}
      className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 ${
        value ? 'bg-blue-600' : 'bg-gray-300'
      }`}
      aria-label={label}
    >
      <span
        className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
          value ? 'translate-x-6' : 'translate-x-1'
        }`}
      />
    </button>
  );
}

interface CheckboxRowProps {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}

function CheckboxRow({ label, checked, onChange }: CheckboxRowProps) {
  return (
    <label className="flex cursor-pointer items-center gap-2 text-sm text-gray-700">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
      />
      {label}
    </label>
  );
}

// ── Preview canvas ────────────────────────────────────────────────────────────

interface PreviewProps {
  lines: string[];
  style: WatermarkStyle;
  opacity: number;
  scale: WatermarkScale;
  textColor: string;
  fontName: string;
  backgroundOpacity: number;
}

function WatermarkPreview({ lines, style, opacity, scale, textColor, fontName, backgroundOpacity }: PreviewProps) {
  const fontSizeNumMap: Record<WatermarkScale, number> = {
    Small: 10,
    Medium: 13,
    Large: 18,
    Auto: 12,
  };

  const primarySize   = fontSizeNumMap[scale];
  const secondarySize = Math.round(primarySize * 0.70);
  const cssColor      = hexToRgba(textColor || '#ffffff', opacity);
  const cssFontFamily = fontName ? `"${fontName}", Arial, sans-serif` : 'Arial, sans-serif';
  const textShadow    = '0 1px 2px rgba(0,0,0,0.8)';

  const baseStyle = (size: number): React.CSSProperties => ({
    color: cssColor,
    fontSize: size,
    fontWeight: 700,
    fontFamily: cssFontFamily,
    textShadow,
    pointerEvents: 'none',
    lineHeight: 1.5,
    whiteSpace: 'nowrap',
  });

  // Standard (non-ribbon) block — all lines same size
  const LineBlock = () => (
    <div>
      {lines.map((line, i) => (
        <div key={i} style={baseStyle(primarySize)}>
          {line}
        </div>
      ))}
    </div>
  );

  // Premium ribbon block — first line larger, rest smaller
  const RibbonContent = () => (
    <div style={{ textAlign: 'center' }}>
      <div style={baseStyle(primarySize)}>{lines[0]}</div>
      {lines.slice(1).map((line, i) => (
        <div key={i} style={baseStyle(secondarySize)}>{line}</div>
      ))}
    </div>
  );

  return (
    <div
      className="relative overflow-hidden rounded-xl border border-gray-200 bg-gradient-to-br from-slate-700 to-slate-900"
      style={{ height: 180 }}
    >
      {/* Simulated photo content */}
      <div className="absolute inset-0 flex items-center justify-center select-none">
        <div className="text-xs text-slate-600">Photo preview area</div>
      </div>

      {style === 'BottomRibbon' && (
        <div
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            background: `rgba(0,0,0,${backgroundOpacity})`,
            padding: '10px 16px',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
          }}
        >
          <RibbonContent />
        </div>
      )}

      {style === 'Corner' && (
        <div style={{ position: 'absolute', bottom: 8, right: 8, textAlign: 'right' }}>
          <LineBlock />
        </div>
      )}

      {style === 'Center' && (
        <div
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%,-50%)',
            textAlign: 'center',
          }}
        >
          <LineBlock />
        </div>
      )}

      {style === 'Diagonal' && (
        <div
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%,-50%) rotate(-45deg)',
            textAlign: 'center',
          }}
        >
          <LineBlock />
        </div>
      )}

      {style === 'RepeatedPattern' && (
        <>
          {[
            { top: '8%', left: '4%' },
            { top: '8%', left: '52%' },
            { top: '54%', left: '4%' },
            { top: '54%', left: '52%' },
          ].map((pos, i) => (
            <div key={i} style={{ position: 'absolute', transform: 'rotate(-20deg)', ...pos }}>
              <LineBlock />
            </div>
          ))}
        </>
      )}
    </div>
  );
}
