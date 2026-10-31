/**
 * Turns a PascalCase enum member into a spaced display label, e.g.
 * `OnSite` -> `On Site`, `LessThanOneYear` -> `Less Than One Year`.
 * Consecutive capitals (acronyms like `USD`) are left untouched.
 * @param value The raw PascalCase enum member name.
 * @returns The humanized label.
 */
export function humanizeEnumMember(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}
