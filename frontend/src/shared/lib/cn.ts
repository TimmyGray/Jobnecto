/**
 * Tiny class-name joiner: filters falsy values and joins with spaces.
 * Keeps component templates readable when composing token-mapped Tailwind
 * classes conditionally, without pulling in a runtime dependency.
 */
export function cn(...values: Array<string | false | null | undefined>): string {
  return values.filter(Boolean).join(' ');
}
