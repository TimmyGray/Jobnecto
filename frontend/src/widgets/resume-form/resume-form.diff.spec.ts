import { describe, expect, it } from 'vitest';
import type { ResumeResult } from '@entities/resume';
import { buildUpdateCommand, resumeToFormValue } from './resume-form.diff';
import type { ResumeFormValue } from './resume-form.diff';

const ORIGINAL: ResumeResult = {
  id: 'r-1',
  title: 'Staff Engineer',
  skills: ['TypeScript', 'Angular'],
  workLocationType: 'Remote',
  experience: 'ThreeToFiveYears',
  currency: 'USD',
  salary: 5000,
  locations: ['Germany', 'Poland'],
  languages: [{ language: 'English', level: 'Advanced' }],
  projects: ['Hidden project'],
  certifications: ['Hidden cert'],
  excludedWords: ['hidden'],
};

/** The form value that exactly mirrors {@link ORIGINAL}. */
function same(): ResumeFormValue {
  return resumeToFormValue(ORIGINAL);
}

function diff(change: Partial<ResumeFormValue>) {
  return buildUpdateCommand(ORIGINAL, { ...same(), ...change });
}

describe('resumeToFormValue', () => {
  it('maps every editable field', () => {
    expect(resumeToFormValue(ORIGINAL)).toEqual({
      title: 'Staff Engineer',
      skills: ['TypeScript', 'Angular'],
      workLocationType: 'Remote',
      experience: 'ThreeToFiveYears',
      currency: 'USD',
      salary: 5000,
      locations: ['Germany', 'Poland'],
      languages: [{ language: 'English', level: 'Advanced' }],
    });
  });

  it('turns missing/null fields into the form’s empty values', () => {
    expect(resumeToFormValue({ id: 'r-2' })).toEqual({
      title: '',
      skills: [],
      workLocationType: '',
      experience: '',
      currency: '',
      salary: null,
      locations: [],
      languages: [],
    });
  });

  it('coerces a numeric-string salary and treats garbage as null', () => {
    expect(resumeToFormValue({ salary: '4200' }).salary).toBe(4200);
    expect(resumeToFormValue({ salary: 'abc' }).salary).toBeNull();
    expect(resumeToFormValue({ salary: 0 }).salary).toBe(0);
    expect(resumeToFormValue({ salary: '' }).salary).toBeNull();
    expect(resumeToFormValue({ salary: '   ' }).salary).toBeNull();
  });

  it('drops incomplete server language entries', () => {
    const value = resumeToFormValue({
      languages: [
        { language: 'English' },
        { language: 'French', level: 'Beginner' },
      ] as ResumeResult['languages'],
    });
    expect(value.languages).toEqual([{ language: 'French', level: 'Beginner' }]);
  });
});

describe('buildUpdateCommand', () => {
  it('returns an empty command for an unchanged form', () => {
    expect(buildUpdateCommand(ORIGINAL, same())).toEqual({});
  });

  it('includes exactly the one field that changed', () => {
    expect(diff({ title: 'Principal Engineer' })).toEqual({ title: 'Principal Engineer' });
  });

  it('includes several changed fields and nothing else', () => {
    const command = diff({ currency: 'EUR', salary: 6000, experience: 'MoreThanFiveYears' });
    expect(command).toEqual({ currency: 'EUR', salary: 6000, experience: 'MoreThanFiveYears' });
  });

  it('is empty again after editing and reverting a field', () => {
    expect(diff({ title: 'Changed' })).toEqual({ title: 'Changed' });
    expect(diff({ title: 'Staff Engineer' })).toEqual({});
  });

  it('never emits projects, certifications or excludedWords', () => {
    const command = diff({ title: 'X', skills: ['A'] });
    expect(command).not.toHaveProperty('projects');
    expect(command).not.toHaveProperty('certifications');
    expect(command).not.toHaveProperty('excludedWords');
  });

  describe('arrays', () => {
    it('sends the whole skills array when it changes', () => {
      expect(diff({ skills: ['TypeScript', 'Angular', 'RxJS'] })).toEqual({
        skills: ['TypeScript', 'Angular', 'RxJS'],
      });
    });

    it('treats a different skills order as a change', () => {
      expect(diff({ skills: ['Angular', 'TypeScript'] })).toEqual({
        skills: ['Angular', 'TypeScript'],
      });
    });

    it('does not report an identical-by-value skills array as a change', () => {
      expect(diff({ skills: ['TypeScript', 'Angular'] })).toEqual({});
    });

    it('sends locations whole, including an empty array to clear them', () => {
      expect(diff({ locations: ['Germany'] })).toEqual({ locations: ['Germany'] });
      expect(diff({ locations: [] })).toEqual({ locations: [] });
    });

    it('sends languages whole, including an empty array to clear them', () => {
      expect(diff({ languages: [{ language: 'English', level: 'Native' }] })).toEqual({
        languages: [{ language: 'English', level: 'Native' }],
      });
      expect(diff({ languages: [] })).toEqual({ languages: [] });
    });

    it('ignores half-filled and empty language rows when comparing and sending', () => {
      const rows = [
        { language: 'English', level: 'Advanced' },
        { language: '', level: '' },
        { language: 'French', level: '' },
      ] as ResumeFormValue['languages'];
      expect(diff({ languages: rows })).toEqual({});

      const withNew = [
        ...rows,
        { language: 'German', level: 'Beginner' },
      ] as ResumeFormValue['languages'];
      expect(diff({ languages: withNew })).toEqual({
        languages: [
          { language: 'English', level: 'Advanced' },
          { language: 'German', level: 'Beginner' },
        ],
      });
    });
  });

  describe('clearing the title', () => {
    it('sends an empty string to clear the title', () => {
      expect(diff({ title: '' })).toEqual({ title: '' });
    });

    it('does not report an already-empty title as a change', () => {
      expect(buildUpdateCommand({ id: 'r' }, resumeToFormValue({ id: 'r' }))).toEqual({});
    });
  });

  describe('clearing optional fields', () => {
    it.each([
      ['skills', { skills: [] }, { skills: [] }],
      ['salary', { salary: null }, { clearSalary: true }],
      ['currency', { currency: '' }, { currency: '' }],
      ['workLocationType', { workLocationType: '' }, { workLocationType: '' }],
      ['experience', { experience: '' }, { experience: '' }],
    ] as const)('sends the clear encoding for an emptied %s', (_field, change, expected) => {
      expect(diff(change as Partial<ResumeFormValue>)).toEqual(expected);
    });

    it('sends a clear together with a value change in one command', () => {
      expect(diff({ title: 'New', salary: null, currency: '', skills: [] })).toEqual({
        title: 'New',
        clearSalary: true,
        currency: '',
        skills: [],
      });
    });

    it('sends nothing when an already-empty field is still empty', () => {
      const empty: ResumeResult = { id: 'r-5' };
      expect(buildUpdateCommand(empty, resumeToFormValue(empty))).toEqual({});
    });

    it('sets a field that was previously empty', () => {
      const empty: ResumeResult = { id: 'r-3' };
      const result = buildUpdateCommand(empty, {
        ...resumeToFormValue(empty),
        salary: 100,
        currency: 'USD',
      });

      expect(result).toEqual({ salary: 100, currency: 'USD' });
    });

    it('treats a salary of 0 as a value, not as cleared', () => {
      expect(diff({ salary: 0 })).toEqual({ salary: 0 });
      const zero: ResumeResult = { id: 'r-4', salary: 0 };
      expect(buildUpdateCommand(zero, { ...resumeToFormValue(zero), salary: null })).toEqual({
        clearSalary: true,
      });
    });

    it('never sends clearSalary together with salary', () => {
      for (const salary of [null, 0, 6000]) {
        const command = diff({ salary });
        expect('clearSalary' in command && 'salary' in command).toBe(false);
      }
    });
  });
});
