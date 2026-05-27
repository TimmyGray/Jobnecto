/**
 * Tailwind v3 theme wired to the Career-OS token layer.
 *
 * Single source of truth: src/shared/config/tokens.ts emits CSS custom
 * properties (see src/styles.scss `:root`). This config maps Tailwind theme
 * keys onto those CSS vars, so utility classes (e.g. `bg-canvas`,
 * `text-primary`, `rounded-md`) resolve to token values and components never
 * hardcode hex/px. [AC2, UX-DR1]
 */
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    // Career OS breakpoints (min-width).
    screens: {
      sm: '480px',
      md: '768px',
      lg: '1024px',
      xl: '1440px',
    },
    extend: {
      colors: {
        canvas: 'var(--color-bg-canvas)',
        surface: 'var(--color-bg-surface)',
        inverse: 'var(--color-bg-inverse)',
        'text-primary': 'var(--color-text-primary)',
        'text-secondary': 'var(--color-text-secondary)',
        'text-muted': 'var(--color-text-muted)',
        'text-inverse': 'var(--color-text-inverse)',
        'action-primary': 'var(--color-action-primary)',
        'action-primary-hover': 'var(--color-action-primary-hover)',
        'brand-accent': 'var(--color-brand-accent)',
        'brand-accent-hover': 'var(--color-brand-accent-hover)',
        'brand-spark': 'var(--color-brand-spark)',
        'status-success': 'var(--color-status-success)',
        'status-warning': 'var(--color-status-warning)',
        'status-warning-bg': 'var(--color-status-warning-bg)',
        'status-danger': 'var(--color-status-danger)',
        'status-info': 'var(--color-status-info)',
        'border-default': 'var(--color-border-default)',
        'border-strong': 'var(--color-border-strong)',
        'border-focus': 'var(--color-border-focus)',
      },
      fontFamily: {
        sans: 'var(--font-family-sans)',
        serif: 'var(--font-family-serif)',
        mono: 'var(--font-family-mono)',
      },
      fontSize: {
        xs: 'var(--font-size-xs)',
        sm: 'var(--font-size-sm)',
        md: 'var(--font-size-md)',
        lg: 'var(--font-size-lg)',
        xl: 'var(--font-size-xl)',
        xxl: 'var(--font-size-xxl)',
        display: 'var(--font-size-display)',
      },
      spacing: {
        1: 'var(--space-1)',
        2: 'var(--space-2)',
        3: 'var(--space-3)',
        4: 'var(--space-4)',
        5: 'var(--space-5)',
        6: 'var(--space-6)',
        8: 'var(--space-8)',
        10: 'var(--space-10)',
        12: 'var(--space-12)',
      },
      borderRadius: {
        sm: 'var(--radius-sm)',
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        pill: 'var(--radius-pill)',
      },
      boxShadow: {
        sm: 'var(--shadow-sm)',
        md: 'var(--shadow-md)',
        lg: 'var(--shadow-lg)',
      },
      transitionDuration: {
        fast: '120ms',
        standard: '180ms',
        emphasis: '260ms',
      },
      transitionTimingFunction: {
        standard: 'cubic-bezier(0.2, 0, 0, 1)',
        exit: 'cubic-bezier(0.4, 0, 1, 1)',
      },
      zIndex: {
        base: '1',
        dropdown: '1000',
        sticky: '1100',
        modal: '1200',
        toast: '1300',
      },
    },
  },
  plugins: [],
};
