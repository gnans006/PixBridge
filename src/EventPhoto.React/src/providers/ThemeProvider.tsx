import { createContext, useCallback, useContext, useEffect, useState } from 'react';

export type Theme       = 'studio-dark' | 'studio-light' | 'studio-midnight';
export type AccentColor = 'indigo' | 'violet' | 'emerald';
export type Density     = 'comfortable' | 'compact';

export interface ThemeContextValue {
  theme:      Theme;
  accent:     AccentColor;
  density:    Density;
  setTheme:   (t: Theme) => void;
  setAccent:  (a: AccentColor) => void;
  setDensity: (d: Density) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

const ACCENT_VARS: Record<AccentColor, { primary: string; hover: string; accent: string }> = {
  indigo:  { primary: '99 102 241',  hover: '124 126 255', accent: '168 85 247' },
  violet:  { primary: '139 92 246',  hover: '167 139 250', accent: '99 102 241' },
  emerald: { primary: '16 185 129',  hover: '52 211 153',  accent: '34 197 94'  },
};

function applyAccent(accent: AccentColor) {
  const { primary, hover, accent: acc } = ACCENT_VARS[accent];
  const s = document.documentElement.style;
  s.setProperty('--pds-primary',       primary);
  s.setProperty('--pds-primary-hover', hover);
  s.setProperty('--pds-accent',        acc);
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme,   setThemeState]   = useState<Theme>(
    () => (localStorage.getItem('pds-theme') as Theme) ?? 'studio-dark',
  );
  const [accent,  setAccentState]  = useState<AccentColor>(
    () => (localStorage.getItem('pds-accent') as AccentColor) ?? 'indigo',
  );
  const [density, setDensityState] = useState<Density>(
    () => (localStorage.getItem('pds-density') as Density) ?? 'comfortable',
  );

  // Apply persisted values on mount
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-density', density);
    applyAccent(accent);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const setTheme = useCallback((t: Theme) => {
    setThemeState(t);
    localStorage.setItem('pds-theme', t);
    document.documentElement.setAttribute('data-theme', t);
  }, []);

  const setAccent = useCallback((a: AccentColor) => {
    setAccentState(a);
    localStorage.setItem('pds-accent', a);
    applyAccent(a);
  }, []);

  const setDensity = useCallback((d: Density) => {
    setDensityState(d);
    localStorage.setItem('pds-density', d);
    document.documentElement.setAttribute('data-density', d);
  }, []);

  return (
    <ThemeContext.Provider value={{ theme, accent, density, setTheme, setAccent, setDensity }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within <ThemeProvider>');
  return ctx;
}
