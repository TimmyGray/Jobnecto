/**
 * Career-OS design tokens for TypeScript consumers.
 *
 * Token values are defined across three hand-synced surfaces:
 * `src/shared/config/tokens.ts`, `src/styles.scss` (`:root` CSS vars), and
 * `tailwind.config.js` (Tailwind theme mapping). There is currently no generator,
 * so edits must be mirrored across all three files.
 *
 * Components MUST reference tokens (Tailwind classes mapped to CSS vars, or the
 * CSS vars directly) and never hardcode hex/px values. [AC2, UX-DR1]
 *
 * Palette is the Career OS palette from ux-design-specification.md#Color-System,
 * which overrides the FE Guide's LinkedIn-blue values. The token *scale* (spacing,
 * radius, shadow, type sizes, motion) is preserved from the FE Guide.
 */

/** Color tokens — Career OS palette (warm cream canvas, near-black primary, royal-blue accent). */
export const color = {
  bg: {
    canvas: '#F6F4EE', // warm cream
    surface: '#FFFFFF',
    inverse: '#0E0F12',
  },
  text: {
    primary: '#14151A',
    secondary: '#44474F',
    muted: '#8A8D96',
    inverse: '#FAFAF7',
  },
  action: {
    primary: '#14151A', // near-black — primary buttons & active nav
    primaryHover: '#000000',
  },
  brand: {
    accent: '#2348E0', // royal blue — links, emphasis, focus, italic wordmark
    accentHover: '#1B36B8',
    spark: '#8FE34A', // lime — decorative / on-dark ONLY (fails contrast on light)
  },
  status: {
    success: '#15803D',
    warning: '#B45309',
    warningBg: '#FDF1D6',
    danger: '#B42318',
    info: '#1D4ED8',
  },
  border: {
    default: '#E6E2D8', // warm hairline
    strong: '#C9C4B6',
    focus: '#2348E0',
  },
} as const;

/** Typography tokens — three families and a fixed size scale. */
export const font = {
  family: {
    // Manrope: ~95% of text (sans).
    sans: "'Manrope', 'Segoe UI', system-ui, sans-serif",
    // Serif italic display: wordmark, accent words, greetings ONLY — never body/controls.
    serif: "'Newsreader', 'Fraunces', Georgia, serif",
    // IBM Plex Mono: uppercase eyebrow labels, entity IDs.
    mono: "'IBM Plex Mono', 'Consolas', monospace",
  },
  size: {
    xs: '12px',
    sm: '14px',
    md: '16px', // body >= 16px
    lg: '20px',
    xl: '28px',
    xxl: '36px',
    display: '44px',
  },
  lineHeight: {
    tight: '1.2',
    base: '1.5',
    relaxed: '1.7',
  },
  weight: {
    regular: '400',
    medium: '500',
    semibold: '600',
    bold: '700',
  },
} as const;

/** Spacing scale (base-4). */
export const space = {
  1: '4px',
  2: '8px',
  3: '12px',
  4: '16px',
  5: '20px',
  6: '24px',
  8: '32px',
  10: '40px',
  12: '48px',
} as const;

/** Border radius scale. */
export const radius = {
  sm: '6px',
  md: '10px',
  lg: '14px',
  pill: '999px',
} as const;

/** Box shadow scale. */
export const shadow = {
  sm: '0 1px 2px rgba(14, 15, 18, 0.06)',
  md: '0 6px 18px rgba(14, 15, 18, 0.08)',
  lg: '0 12px 32px rgba(14, 15, 18, 0.12)',
} as const;

/** Motion tokens — durations and easings. */
export const motion = {
  duration: {
    fast: '120ms',
    standard: '180ms',
    emphasis: '260ms',
  },
  easing: {
    standard: 'cubic-bezier(0.2, 0, 0, 1)',
    exit: 'cubic-bezier(0.4, 0, 1, 1)',
  },
} as const;

/** Z-index scale. */
export const z = {
  base: '1',
  dropdown: '1000',
  sticky: '1100',
  modal: '1200',
  toast: '1300',
} as const;

/** Responsive breakpoints (min-width lower bounds). */
export const breakpoint = {
  xs: '0px',
  sm: '480px',
  md: '768px',
  lg: '1024px',
  xl: '1440px',
} as const;

export const tokens = {
  color,
  font,
  space,
  radius,
  shadow,
  motion,
  z,
  breakpoint,
} as const;

export type Tokens = typeof tokens;
