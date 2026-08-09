import { useApplicationSettings } from './useApplicationSettings';

export interface FeatureFlags {
  isFaceSearchEnabled: boolean;
  isWatermarkEnabled: boolean;
  [key: string]: boolean;
}

const DEFAULT_FLAGS: FeatureFlags = {
  isFaceSearchEnabled: true,
  isWatermarkEnabled: true,
};

/**
 * Returns globally-configured feature flags from ApplicationSettings.
 * Falls back to `true` while loading so nothing is hidden during initial render.
 */
export function useFeatureFlags(): FeatureFlags {
  const { data } = useApplicationSettings();
  if (!data) return DEFAULT_FLAGS;
  return {
    isFaceSearchEnabled: data.isFaceSearchEnabled,
    isWatermarkEnabled: data.isWatermarkEnabled,
  };
}
