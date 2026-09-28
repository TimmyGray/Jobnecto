import { components } from '@shared/api/generated/schema';

/**
 * Résumé enum option lists, hand-declared but type-tethered to the generated
 * OpenAPI schema wherever one exists (`satisfies <GeneratedUnionType>[]`), so
 * an added/removed/renamed backend enum member fails the build instead of
 * silently drifting. TypeScript types don't exist at runtime, so a literal
 * array is unavoidable — the `satisfies` tether is the closest achievable
 * meaning of "generated from backend enums, never hardcoded" (AR17, AC1).
 *
 * `Experience` is the exception: it is never exposed as an actual
 * `Experience`-typed property anywhere in the API (always plain `string`), so
 * there is no generated union to tether it to — its array is `satisfies
 * string[]` only, verified by hand against `Experience.cs`.
 */

/** A single `SelectField<T>` option. */
export interface EnumOption<T extends string> {
  value: T;
  label: string;
}

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

function toOptions<T extends string>(members: readonly T[]): EnumOption<T>[] {
  return members.map((value) => ({ value, label: humanizeEnumMember(value) }));
}

/** [Source: backend/src/JobNecto.Domain/Enums/WorkLocationType.cs] */
export const WORK_LOCATION_TYPES = [
  'OnSite',
  'Remote',
  'Hybrid',
] as const satisfies components['schemas']['WorkLocationType'][];
export const WORK_LOCATION_TYPE_OPTIONS = toOptions(WORK_LOCATION_TYPES);

/** [Source: backend/src/JobNecto.Domain/Enums/Experience.cs] — no generated union; see file header. */
export const EXPERIENCES = [
  'LessThanOneYear',
  'OneToThreeYears',
  'ThreeToFiveYears',
  'MoreThanFiveYears',
] as const satisfies string[];
export const EXPERIENCE_OPTIONS = toOptions(EXPERIENCES);

/** [Source: backend/src/JobNecto.Domain/Enums/Currency.cs] */
export const CURRENCIES = [
  'USD', 'EUR', 'GBP', 'CAD', 'AUD', 'CHF', 'JPY', 'CNY', 'INR',
  'RUB', 'UAH', 'PLN', 'SEK', 'NOK', 'DKK', 'NZD', 'MXN', 'BRL',
] as const satisfies components['schemas']['Currency'][];
export const CURRENCY_OPTIONS = toOptions(CURRENCIES);

/** [Source: backend/src/JobNecto.Domain/Enums/Language.cs] */
export const LANGUAGES = [
  'English', 'Russian', 'Ukrainian', 'Polish', 'Spanish', 'French', 'German',
  'Italian', 'Portuguese', 'Dutch', 'Turkish', 'Arabic', 'Chinese', 'Japanese',
  'Korean', 'Hindi', 'Bengali', 'Punjabi', 'Marathi', 'Tamil', 'Telugu',
  'Indonesian', 'Thai', 'Vietnamese', 'Swahili', 'Persian', 'Romanian',
  'Bulgarian', 'Serbian', 'Czech', 'Hungarian', 'Finnish', 'Swedish',
  'Norwegian', 'Danish', 'Malay', 'Tagalog', 'Urdu',
] as const satisfies components['schemas']['Language'][];
export const LANGUAGE_OPTIONS = toOptions(LANGUAGES);

/** [Source: backend/src/JobNecto.Domain/Enums/Language.cs] (same file as Language) */
export const LANGUAGE_LEVELS = [
  'Beginner',
  'Intermediate',
  'Advanced',
  'Native',
] as const satisfies components['schemas']['LanguageLevel'][];
export const LANGUAGE_LEVEL_OPTIONS = toOptions(LANGUAGE_LEVELS);

/** [Source: backend/src/JobNecto.Domain/Enums/Location.cs] */
export const LOCATIONS = [
  'Ukraine', 'Poland', 'Germany', 'France', 'Italy', 'Spain', 'Portugal',
  'Greece', 'Netherlands', 'Belgium', 'Switzerland', 'Austria', 'Hungary',
  'CzechRepublic', 'Slovakia', 'Croatia', 'Slovenia', 'BosniaAndHerzegovina',
  'Macedonia', 'Montenegro', 'Serbia', 'Kosovo', 'Albania', 'Moldova',
  'Georgia', 'Armenia', 'Azerbaijan', 'Turkmenistan', 'Uzbekistan',
  'Tajikistan', 'Kyrgyzstan', 'Kazakhstan', 'Mongolia', 'China', 'Japan',
  'Korea', 'Vietnam', 'Thailand', 'Malaysia', 'Indonesia', 'Philippines',
  'Singapore', 'HongKong', 'Macau', 'Taiwan', 'India', 'Pakistan',
  'Bangladesh', 'SriLanka', 'Nepal', 'Bhutan', 'Maldives', 'Iran', 'Iraq',
  'Syria', 'Lebanon', 'Jordan', 'Israel', 'Palestine', 'Egypt', 'Sudan',
  'Ethiopia', 'Somalia', 'Kenya', 'Uganda', 'Tanzania', 'Nigeria', 'Ghana',
  'Benin', 'BurkinaFaso', 'CapeVerde', 'CentralAfricanRepublic', 'Chad',
  'Comoros', 'Congo', 'DemocraticRepublicOfTheCongo', 'EquatorialGuinea',
  'Gabon', 'Guinea', 'GuineaBissau', 'IvoryCoast', 'Liberia', 'Madagascar',
  'Malawi', 'Mali', 'Mauritania', 'Mauritius', 'Morocco', 'Mozambique',
  'Namibia', 'Niger', 'Oman', 'Qatar', 'SaudiArabia', 'Senegal', 'Seychelles',
  'SierraLeone', 'Turkey', 'UnitedArabEmirates', 'Yemen', 'Zimbabwe',
  'Canada', 'UnitedStates', 'Mexico', 'Brazil', 'Argentina', 'Colombia',
  'Peru', 'Chile', 'Ecuador', 'Bolivia', 'Paraguay', 'Uruguay', 'Venezuela',
  'Cuba', 'DominicanRepublic', 'Honduras', 'Guatemala', 'ElSalvador',
  'Nicaragua', 'CostaRica', 'Panama',
] as const satisfies components['schemas']['Location'][];
export const LOCATION_OPTIONS = toOptions(LOCATIONS);
