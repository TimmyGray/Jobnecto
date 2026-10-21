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
    expect(buildUpdateCommand(ORIGINAL, same())).toEqual({ command: {}, blockedClears: [] });
  });

  it('includes exactly the one field that changed', () => {
    expect(diff({ title: 'Principal Engineer' })).toEqual({
      command: { title: 'Principal Engineer' },
      blockedClears: [],
    });
  });

  it('includes several changed fields and nothing else', () => {
    const { command } = diff({ currency: 'EUR', salary: 6000, experience: 'MoreThanFiveYears' });
    expect(command).toEqual({ currency: 'EUR', salary: 6000, experience: 'MoreThanFiveYears' });
  });

  it('is empty again after editing and reverting a field', () => {
    expect(diff({ title: 'Changed' }).command).toEqual({ title: 'Changed' });
    expect(diff({ title: 'Staff Engineer' }).command).toEqual({});
  });

  it('never emits projects, certifications or excludedWords', () => {
    const { command } = diff({ title: 'X', skills: ['A'] });
    expect(command).not.toHaveProperty('projects');
    expect(command).not.toHaveProperty('certifications');
    expect(command).not.toHaveProperty('excludedWords');
  });

  describe('arrays', () => {
    it('sends the whole skills array when it changes', () => {
      expect(diff({ skills: ['TypeScript', 'Angular', 'RxJS'] }).command).toEqual({
        skills: ['TypeScript', 'Angular', 'RxJS'],
      });
    });

    it('treats a different skills order as a change', () => {
      expect(diff({ skills: ['Angular', 'TypeScript'] }).command).toEqual({
        skills: ['Angular', 'TypeScript'],
      });
    });

    it('does not report an identical-by-value skills array as a change', () => {
      expect(diff({ skills: ['TypeScript', 'Angular'] }).command).toEqual({});
    });

    it('sends locations whole, including an empty array to clear them', () => {
      expect(diff({ locations: ['Germany'] }).command).toEqual({ locations: ['Germany'] });
      expect(diff({ locations: [] })).toEqual({ command: { locations: [] }, blockedClears: [] });
    });

    it('sends languages whole, including an empty array to clear them', () => {
      expect(diff({ languages: [{ language: 'English', level: 'Native' }] }).command).toEqual({
        languages: [{ language: 'English', level: 'Native' }],
      });
      expect(diff({ languages: [] })).toEqual({ command: { languages: [] }, blockedClears: [] });
    });

    it('ignores half-filled and empty language rows when comparing and sending', () => {
      const rows = [
        { language: 'English', level: 'Advanced' },
        { language: '', level: '' },
        { language: 'French', level: '' },
      ] as ResumeFormValue['languages'];
      expect(diff({ languages: rows }).command).toEqual({});

      const withNew = [
        ...rows,
        { language: 'German', level: 'Beginner' },
      ] as ResumeFormValue['languages'];
      expect(diff({ languages: withNew }).command).toEqual({
        languages: [
          { language: 'English', level: 'Advanced' },
          { language: 'German', level: 'Beginner' },
        ],
      });
    });
  });

  describe('fields the API can clear', () => {
    it('sends an empty string to clear the title', () => {
      expect(diff({ title: '' })).toEqual({ command: { title: '' }, blockedClears: [] });
    });

    it('does not report an already-empty title as a change', () => {
      expect(buildUpdateCommand({ id: 'r' }, resumeToFormValue({ id: 'r' })).command).toEqual({});
    });
  });

  describe('fields the API cannot clear', () => {
    it.each([
      ['skills', { skills: [] }],
      ['salary', { salary: null }],
      ['currency', { currency: '' }],
      ['workLocationType', { workLocationType: '' }],
      ['experience', { experience: '' }],
    ] as const)('blocks clearing %s and leaves it out of the command', (field, change) => {
      const result = diff(change as Partial<ResumeFormValue>);

      expect(result.blockedClears).toEqual([field]);
      expect(result.command).toEqual({});
    });

    it('reports every blocked clear alongside the legitimate changes', () => {
      const result = diff({ title: 'New', salary: null, currency: '' });

      expect(result.command).toEqual({ title: 'New' });
      expect(result.blockedClears).toEqual(['salary', 'currency']);
    });

    it('does not block setting a field that was previously empty', () => {
      const empty: ResumeResult = { id: 'r-3' };
      const result = buildUpdateCommand(empty, {
        ...resumeToFormValue(empty),
        salary: 100,
        currency: 'USD',
      });

      expect(result).toEqual({ command: { salary: 100, currency: 'USD' }, blockedClears: [] });
    });

    it('treats a salary of 0 as a value, not as cleared', () => {
      expect(diff({ salary: 0 })).toEqual({ command: { salary: 0 }, blockedClears: [] });
      const zero: ResumeResult = { id: 'r-4', salary: 0 };
      expect(
        buildUpdateCommand(zero, { ...resumeToFormValue(zero), salary: null }).blockedClears,
      ).toEqual(['salary']);
    });
  });
});
