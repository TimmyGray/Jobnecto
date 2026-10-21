import type { ResumeResult, UpdateResumeCommand } from '@entities/resume';

/** One language-proficiency row as the form holds it (either half may still be unselected). */
export interface LanguageRowValue {
  language: string;
  level: string;
}

/** The editable résumé fields in the shape the form works with (`''` / `null` / `[]` mean "empty"). */
export interface ResumeFormValue {
  title: string;
  skills: string[];
  workLocationType: string;
  experience: string;
  currency: string;
  salary: number | null;
  locations: string[];
  languages: LanguageRowValue[];
}

/** Result of comparing the form against the loaded résumé. */
export interface UpdateDiff {
  /** Only the fields that changed and that the API is able to apply. */
  command: UpdateResumeCommand;
  /**
   * Fields the user emptied that `PATCH /resumes/{id}` cannot clear (it treats
   * `null` as "unchanged" and rejects `[]` / `""`): skills, salary, currency,
   * workLocationType, experience. They are never placed in {@link command}.
   */
  blockedClears: string[];
}

const completeRows = (rows: LanguageRowValue[]): LanguageRowValue[] =>
  rows
    .filter((row) => row.language && row.level)
    .map(({ language, level }) => ({ language, level }));

const sameList = <T>(a: readonly T[], b: readonly T[], equal: (x: T, y: T) => boolean): boolean =>
  a.length === b.length && a.every((item, i) => equal(item, b[i]));

/**
 * Maps a loaded résumé onto the form's value shape. The generated schema marks
 * everything optional/nullable and widens `salary` to `number | string`, so this
 * is the single place those are normalised to the form's empty values.
 * @param resume The résumé returned by the API.
 * @returns The value to populate the form with and to diff against.
 */
export function resumeToFormValue(resume: ResumeResult): ResumeFormValue {
  const rawSalary = resume.salary;
  const salary =
    rawSalary === null ||
    rawSalary === undefined ||
    (typeof rawSalary === 'string' && rawSalary.trim() === '')
      ? NaN
      : Number(rawSalary);
  return {
    title: resume.title ?? '',
    skills: [...(resume.skills ?? [])],
    workLocationType: resume.workLocationType ?? '',
    experience: resume.experience ?? '',
    currency: resume.currency ?? '',
    salary: Number.isFinite(salary) ? salary : null,
    locations: [...(resume.locations ?? [])],
    languages: completeRows(
      (resume.languages ?? []).map((l) => ({ language: l.language ?? '', level: l.level ?? '' })),
    ),
  };
}

/**
 * Compares the form with the loaded résumé and builds a minimal PATCH body.
 *
 * Arrays are compared by value (order significant) and sent whole when they
 * change, because the API replaces them. Incomplete language rows are ignored
 * on both sides. `projects`, `certifications` and `excludedWords` have no form
 * fields and are never emitted. See {@link UpdateDiff.blockedClears} for the
 * fields the API cannot empty.
 * @param original The résumé as last loaded or saved.
 * @param current The form's current value.
 * @returns The changed fields plus any clears the API cannot honour.
 */
export function buildUpdateCommand(original: ResumeResult, current: ResumeFormValue): UpdateDiff {
  const before = resumeToFormValue(original);
  const command: UpdateResumeCommand = {};
  const blockedClears: string[] = [];

  if (current.title !== before.title) {
    command.title = current.title;
  }

  if (!sameList(current.skills, before.skills, Object.is)) {
    if (current.skills.length === 0) {
      blockedClears.push('skills');
    } else {
      command.skills = current.skills;
    }
  }

  if (current.salary !== before.salary) {
    if (current.salary === null) {
      blockedClears.push('salary');
    } else {
      command.salary = current.salary;
    }
  }

  for (const field of ['currency', 'workLocationType', 'experience'] as const) {
    if (current[field] === before[field]) {
      continue;
    }
    if (current[field] === '') {
      blockedClears.push(field);
    } else {
      command[field] = current[field];
    }
  }

  if (!sameList(current.locations, before.locations, Object.is)) {
    command.locations = current.locations as UpdateResumeCommand['locations'];
  }

  const rows = completeRows(current.languages);
  const sameRow = (x: LanguageRowValue, y: LanguageRowValue) =>
    x.language === y.language && x.level === y.level;
  if (!sameList(rows, before.languages, sameRow)) {
    command.languages = rows as UpdateResumeCommand['languages'];
  }

  return { command, blockedClears };
}
