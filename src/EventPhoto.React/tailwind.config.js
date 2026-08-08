/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
      },
      colors: {
        primary: {
          50: '#eef2ff',
          100: '#e0e7ff',
          200: '#c7d2fe',
          500: '#6366f1',
          600: '#4f46e5',
          700: '#4338ca',
          900: '#312e81',
        },
        // ── PixBridge Design System tokens (CSS-variable driven) ─────────────
        pds: {
          bg:            'rgb(var(--pds-bg) / <alpha-value>)',
          surface:       'rgb(var(--pds-surface) / <alpha-value>)',
          card:          'rgb(var(--pds-card) / <alpha-value>)',
          elevated:      'rgb(var(--pds-elevated) / <alpha-value>)',
          border:        'rgb(var(--pds-border) / <alpha-value>)',
          primary:       'rgb(var(--pds-primary) / <alpha-value>)',
          'primary-hov': 'rgb(var(--pds-primary-hover) / <alpha-value>)',
          accent:        'rgb(var(--pds-accent) / <alpha-value>)',
          success:       'rgb(var(--pds-success) / <alpha-value>)',
          warning:       'rgb(var(--pds-warning) / <alpha-value>)',
          danger:        'rgb(var(--pds-danger) / <alpha-value>)',
          text:          'rgb(var(--pds-text) / <alpha-value>)',
          'text-2':      'rgb(var(--pds-text-secondary) / <alpha-value>)',
          'text-muted':  'rgb(var(--pds-text-muted) / <alpha-value>)',
        },
      },
      height: {
        18: '72px',
      },
      borderRadius: {
        '2xl': '20px',
      },
      boxShadow: {
        'pds-card':    '0 1px 3px rgba(0,0,0,0.4), 0 1px 2px rgba(0,0,0,0.3)',
        'pds-modal':   '0 25px 50px rgba(0,0,0,0.5)',
        'pds-glow-sm': '0 0 12px rgba(99,102,241,0.2)',
        'pds-glow':    '0 0 24px rgba(99,102,241,0.3)',
      },
      animation: {
        'fade-in':    'fadeIn 0.4s ease-out both',
        'slide-up':   'slideUp 0.45s ease-out both',
        'count-up':   'countUp 0.5s ease-out both',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'shimmer':    'shimmer 1.8s ease-in-out infinite',
      },
      keyframes: {
        fadeIn: {
          '0%':   { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%':   { opacity: '0', transform: 'translateY(16px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        countUp: {
          '0%':   { opacity: '0', transform: 'scale(0.85)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        shimmer: {
          '0%':   { backgroundPosition: '-800px 0' },
          '100%': { backgroundPosition: '800px 0' },
        },
      },
    },
  },
  plugins: [],
}

