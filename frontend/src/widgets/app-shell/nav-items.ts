/** A single primary-navigation entry: its sidebar label and target route. */
export interface NavItem {
  readonly label: string;
  readonly path: string;
}

/**
 * The flat primary-nav item list, in display order (UX-DR14). Shared by the
 * desktop sidebar and the mobile off-canvas drawer so both stay in sync.
 */
export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Profile', path: '/profile' },
  { label: 'Resumes', path: '/resumes' },
  { label: 'Education', path: '/educations' },
  { label: 'Vacancies', path: '/vacancies' },
  { label: 'Cover Letters', path: '/cover-letters' },
  { label: 'Settings', path: '/settings' },
];
