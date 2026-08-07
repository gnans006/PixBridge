
import type { UpsertWatermarkConfigRequest, WatermarkConfigResponse } from '../../types';

interface WatermarkPreviewProps {
  config: UpsertWatermarkConfigRequest | WatermarkConfigResponse;
  eventName: string;
  studioName?: string;
}

/** Returns the watermark text that would be rendered given current config. */
function resolveText(
  config: UpsertWatermarkConfigRequest | WatermarkConfigResponse,
  eventName: string,
  studioName: string,
): string {
  // includeStudioName / includeEventName default to true when undefined (initial load)
  const inclStudio = config.includeStudioName !== false;
  const inclEvent  = config.includeEventName  !== false;

  switch (config.mode) {
    case 'StudioBranding':
      return studioName;
    case 'EventBranding':
      return eventName;
    case 'StudioAndEvent': {
      const parts: string[] = [];
      if (inclStudio) parts.push(studioName);
      if (inclEvent)  parts.push(eventName);
      return parts.join(' · ') || studioName;
    }
    case 'CustomText':
      return config.customText ?? studioName;
    case 'DynamicTemplate':
      return (config.template ?? '{StudioName} · {EventName}')
        .replace('{StudioName}', inclStudio ? studioName : '')
        .replace('{EventName}',  inclEvent  ? eventName  : '')
        .replace('{EventDate}',  new Date().toLocaleDateString())
        .replace('{DownloadDate}', new Date().toLocaleDateString())
        .replace(/\s{2,}/g, ' ')
        .trim() || studioName;
    default:
      return studioName;
  }
}

/** Maps WatermarkStyle to CSS positioning classes. */
function styleToOverlayClass(style: string): string {
  switch (style) {
    case 'Center':        return 'absolute inset-0 flex items-center justify-center';
    case 'Diagonal':      return 'absolute inset-0 flex items-center justify-center rotate-[-30deg]';
    case 'BottomRibbon':  return 'absolute bottom-0 left-0 right-0 flex items-center justify-center py-2';
    case 'Corner':
    default:              return 'absolute bottom-3 right-3 flex items-end justify-end';
  }
}

export function WatermarkPreview({ config, eventName, studioName = 'PixBridge Studio' }: WatermarkPreviewProps) {
  // Recompute directly — no useMemo so it always reflects the latest watched values
  const text = resolveText(config, eventName, studioName);

  const isRepeated = config.style === 'RepeatedPattern';
  const textColor = config.textColor ?? '#FFFFFF';
  const bgOpacity = typeof config.backgroundOpacity === 'number' ? config.backgroundOpacity : 0.4;
  const opacity = typeof config.opacity === 'number' ? config.opacity : 0.6;
  const fontSizeClass = config.scale === 'Small' ? 'text-xs' : config.scale === 'Large' ? 'text-lg' : 'text-sm';

  // Show overlay whenever a mode is selected (not 'Disabled'), regardless of the enabled toggle,
  // so the user can design the watermark appearance before activating it.
  const showOverlay = config.mode !== 'Disabled';
  // Dim the overlay visually when the watermark is currently disabled
  const previewOpacity = config.enabled ? opacity : Math.min(opacity * 0.45, 0.35);

  return (
    <div className="overflow-hidden rounded-xl border border-slate-700">
      {/* Label */}
      <div className="flex items-center justify-between border-b border-slate-700 bg-slate-800 px-4 py-2">
        <span className="text-xs font-medium text-slate-400">Live Preview</span>
        {config.mode === 'Disabled' ? (
          <span className="rounded-full bg-slate-700 px-2 py-0.5 text-xs text-slate-400">Mode: Disabled</span>
        ) : config.enabled ? (
          <span className="rounded-full bg-amber-500/20 px-2 py-0.5 text-xs text-amber-400">
            {config.applyOnDownload ? 'Applied on download' : 'Preview only'}
          </span>
        ) : (
          <span className="rounded-full bg-slate-700 px-2 py-0.5 text-xs text-slate-500">Disabled — won't apply yet</span>
        )}
      </div>

      {/* Photo mock */}
      <div className="relative aspect-[4/3] bg-gradient-to-br from-slate-700 via-slate-800 to-slate-900">
        {/* Simulated photo content */}
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="grid grid-cols-3 gap-2 opacity-20">
            {Array.from({ length: 9 }).map((_, i) => (
              <div key={i} className="h-10 w-10 rounded-lg bg-slate-600" />
            ))}
          </div>
        </div>
        <div className="absolute inset-0 flex items-center justify-center">
          <p className="text-lg font-bold text-white/10">Sample Photo</p>
        </div>

        {/* Watermark overlay — rendered regardless of enabled so the user can design it */}
        {showOverlay && (
          <div
            className={styleToOverlayClass(config.style ?? 'Corner')}
            style={{ opacity: previewOpacity }}
          >
            {isRepeated ? (
              <div className="absolute inset-0 overflow-hidden">
                {Array.from({ length: 12 }).map((_, i) => (
                  <span
                    key={i}
                    className={`absolute whitespace-nowrap font-semibold ${fontSizeClass}`}
                    style={{
                      color: textColor,
                      top: `${(i % 4) * 28}%`,
                      left: `${Math.floor(i / 4) * 35 - 10}%`,
                      transform: 'rotate(-30deg)',
                      textShadow: '0 1px 3px rgba(0,0,0,0.8)',
                      backgroundColor: `rgba(0,0,0,${bgOpacity})`,
                      padding: '2px 8px',
                      borderRadius: '4px',
                    }}
                  >
                    {text}
                  </span>
                ))}
              </div>
            ) : (
              <span
                className={`font-semibold ${fontSizeClass} rounded px-2 py-1`}
                style={{
                  color: textColor,
                  backgroundColor: `rgba(0,0,0,${bgOpacity})`,
                  textShadow: '0 1px 2px rgba(0,0,0,0.8)',
                }}
              >
                {text}
              </span>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
