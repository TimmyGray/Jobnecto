import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { ProblemDetails } from '@shared/api';
import { ResumeFormComponent } from './resume-form';
import type { CreateResumeCommand } from '@entities/resume';

describe('ResumeFormComponent', () => {
  let fixture: ComponentFixture<ResumeFormComponent>;
  let component: ResumeFormComponent;

  beforeEach(() => {
    fixture = TestBed.createComponent(ResumeFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function submitButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]');
  }

  it('renders the title field, skills tag input, and a submit button', () => {
    expect(fixture.nativeElement.querySelector('ui-text-field')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('ui-tag-input')).toBeTruthy();
    expect(submitButton()).toBeTruthy();
  });

  it('disables submit while the form is pristine', () => {
    expect(component.canSubmit).toBe(false);
    expect(submitButton().disabled).toBe(true);
  });

  it('enables submit once the form is dirty and valid', () => {
    component.form.controls.title.setValue('Staff Engineer');
    component.form.controls.title.markAsDirty();
    fixture.detectChanges();

    expect(component.canSubmit).toBe(true);
    expect(submitButton().disabled).toBe(false);
  });

  it('rejects a title over 500 characters and disables submit', () => {
    component.form.controls.title.setValue('x'.repeat(501));
    component.form.controls.title.markAsDirty();
    fixture.detectChanges();

    expect(component.canSubmit).toBe(false);
    expect(component.errorFor('title')).toContain('500');
  });

  it('rejects a negative salary and disables submit', () => {
    component.form.controls.salary.setValue(-1);
    component.form.controls.salary.markAsDirty();
    fixture.detectChanges();

    expect(component.canSubmit).toBe(false);
    expect(component.errorFor('salary')).toBeTruthy();
  });

  it('does not submit while submitting() is true, even if valid', () => {
    fixture.componentRef.setInput('submitting', true);
    component.form.controls.title.setValue('Staff Engineer');
    component.form.controls.title.markAsDirty();
    fixture.detectChanges();

    expect(component.canSubmit).toBe(false);
    expect(submitButton().disabled).toBe(true);
  });

  it('emits create with the mapped command on a valid submit', () => {
    let emitted: CreateResumeCommand | undefined;
    component.create.subscribe((cmd: CreateResumeCommand) => (emitted = cmd));

    component.form.controls.title.setValue('Staff Engineer');
    component.form.controls.skills.setValue(['TypeScript', 'Angular']);
    component.form.controls.workLocationType.setValue('Remote');
    component.form.controls.salary.setValue(120000);
    component.form.controls.currency.setValue('USD');
    component.form.markAsDirty();

    component.onSubmit();

    expect(emitted).toEqual({
      title: 'Staff Engineer',
      skills: ['TypeScript', 'Angular'],
      workLocationType: 'Remote',
      experience: undefined,
      currency: 'USD',
      salary: 120000,
      locations: [],
      languages: [],
    });
  });

  it('rejects a locations value that is not a valid Location member', () => {
    component.form.controls.locations.setValue(['NotACountry'] as never);
    component.form.controls.locations.markAsDirty();
    fixture.detectChanges();

    expect(component.canSubmit).toBe(false);
    expect(component.errorFor('locations')).toBeTruthy();
  });

  it('rejects a language row with an invalid language or level value', () => {
    component.addLanguageRow();
    const row = component.form.controls.languages.at(0);
    row.controls.language.setValue('Klingon' as never);
    row.controls.level.setValue('Native');
    component.form.markAsDirty();

    expect(component.canSubmit).toBe(false);
  });

  it('does not submit an invalid form, and marks all controls touched', () => {
    let emitted = false;
    component.create.subscribe(() => (emitted = true));

    component.form.controls.salary.setValue(-5);
    component.form.markAsDirty();
    component.onSubmit();

    expect(emitted).toBe(false);
    expect(component.form.controls.salary.touched).toBe(true);
  });

  describe('language rows', () => {
    it('addLanguageRow appends a row; removeLanguageRow removes it', () => {
      expect(component.form.controls.languages.length).toBe(0);

      component.addLanguageRow();
      expect(component.form.controls.languages.length).toBe(1);

      component.removeLanguageRow(0);
      expect(component.form.controls.languages.length).toBe(0);
    });

    it('the "Add language" and row "Remove" buttons drive the same behavior through the DOM', () => {
      const buttons = () => Array.from(fixture.nativeElement.querySelectorAll('button[type="button"]')) as HTMLButtonElement[];
      const addButton = buttons().find((b) => b.textContent?.includes('Add language'))!;

      addButton.click();
      fixture.detectChanges();
      expect(component.form.controls.languages.length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll('ui-select-field').length).toBeGreaterThanOrEqual(2);

      const removeButton = buttons().find((b) => b.textContent?.includes('Remove'))!;
      removeButton.click();
      fixture.detectChanges();
      expect(component.form.controls.languages.length).toBe(0);
    });

    it('an unmapped (out-of-range) language-row server error does not disturb an existing row\'s rendering', () => {
      component.addLanguageRow();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll('ui-select-field').length).toBeGreaterThanOrEqual(2);

      component.applyServerErrors({
        status: 400,
        title: 'Validation failed',
        errors: { 'Languages[99].Language': ['nope'] },
      });
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll('ui-select-field').length).toBeGreaterThanOrEqual(2);
    });

    it('a half-filled row (language chosen, level left blank) blocks submit instead of silently dropping it', () => {
      let emitted: CreateResumeCommand | undefined;
      component.create.subscribe((cmd: CreateResumeCommand) => (emitted = cmd));

      component.form.controls.title.setValue('Staff Engineer');
      component.addLanguageRow();
      const row = component.form.controls.languages.at(0);
      row.controls.language.setValue('English');
      component.form.markAsDirty();

      expect(component.canSubmit).toBe(false);

      component.onSubmit();

      expect(emitted).toBeUndefined();
    });

    it('renders the incomplete-row message in the DOM once the row is dirty', () => {
      component.addLanguageRow();
      const row = component.form.controls.languages.at(0);
      row.controls.language.setValue('English');
      row.markAsDirty();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Pick both a language and a level');
    });

    it('includes filled language rows in the emitted command', () => {
      let emitted: CreateResumeCommand | undefined;
      component.create.subscribe((cmd: CreateResumeCommand) => (emitted = cmd));

      component.addLanguageRow();
      const row = component.form.controls.languages.at(0);
      row.controls.language.setValue('English');
      row.controls.level.setValue('Native');
      component.form.markAsDirty();

      component.onSubmit();

      expect(emitted?.languages).toEqual([{ language: 'English', level: 'Native' }]);
    });
  });

  describe('applyServerErrors', () => {
    it('maps a recognized field error onto its control', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { title: ['title must be at most 500 characters long.'] },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges();

      expect(component.errorFor('title')).toBe('title must be at most 500 characters long.');
    });

    it('maps a per-skill indexed key (FluentValidation RuleForEach shape "Skills[0]") onto the skills control', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { 'Skills[0]': ['each skill must be at most 30 characters long.'] },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges();

      expect(component.errorFor('skills')).toBe('each skill must be at most 30 characters long.');
      expect(component.generalError()).toBe('');
    });

    it('renders the skills error inline in the template, not just via errorFor()', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { 'Skills[1]': ['skills cannot contain empty values.'] },
      };

      component.applyServerErrors(problem);
      component.form.controls.skills.markAsTouched();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('skills cannot contain empty values.');
    });

    it('maps every field when several fields fail at once', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: {
          title: ['title must be at most 500 characters long.'],
          salary: ['salary cannot be negative.'],
        },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges();

      expect(component.errorFor('title')).toBe('title must be at most 500 characters long.');
      expect(component.errorFor('salary')).toBe('salary cannot be negative.');
      expect(component.generalError()).toBe('');
    });

    it('maps a locations indexed key onto the locations control', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { 'Locations[0]': ['locations must contain valid Location enum values.'] },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges();

      expect(component.errorFor('locations')).toBe('locations must contain valid Location enum values.');
      expect(component.generalError()).toBe('');
    });

    it('maps a nested language-row key ("Languages[0].Language") onto that row\'s language control', () => {
      component.addLanguageRow();
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { 'Languages[0].Language': ['language must be a valid Language enum value.'] },
      };

      component.applyServerErrors(problem);
      const row = component.form.controls.languages.at(0);

      expect(component.languageRowErrorFor(row, 'language')).toBe(
        'language must be a valid Language enum value.',
      );
      expect(component.generalError()).toBe('');
    });

    it('the mapped language-row error actually survives a render pass and shows up in the DOM', () => {
      // Regression test: a manually setErrors()'d nested FormArray-row control
      // does NOT survive a subsequent render pass (verified empirically —
      // FormControlDirective.ngOnChanges re-runs updateValueAndValidity() on
      // it, discarding the manual error). languageRowServerErrors (a plain
      // field, not control.errors) is what this test proves actually survives.
      component.addLanguageRow();
      fixture.detectChanges();
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { 'Languages[0].Language': ['language must be a valid Language enum value.'] },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges(); // the render pass a real HTTP error callback would trigger

      const row = component.form.controls.languages.at(0);
      expect(component.languageRowErrorFor(row, 'language')).toBe(
        'language must be a valid Language enum value.',
      );
      expect(fixture.nativeElement.textContent).toContain(
        'language must be a valid Language enum value.',
      );
    });

    it('falls back to a general banner for an out-of-range language-row index', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        errors: { 'Languages[0].Language': ['nope'] },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges();

      expect(component.generalError()).toBe('Validation failed');
    });

    it('falls back to a general banner for an unrecognized field', () => {
      const problem: ProblemDetails = {
        status: 400,
        title: 'Validation failed',
        detail: 'Unrecognized field failed validation.',
        errors: { someUnknownField: ['nope'] },
      };

      component.applyServerErrors(problem);
      fixture.detectChanges();

      expect(component.generalError()).toBe('Unrecognized field failed validation.');
    });
  });

  describe('resetForm', () => {
    it('clears field values, language rows, and pristine state', () => {
      component.form.controls.title.setValue('Staff Engineer');
      component.addLanguageRow();
      component.form.markAsDirty();

      component.resetForm();

      expect(component.form.controls.title.value).toBe('');
      expect(component.form.controls.languages.length).toBe(0);
      expect(component.form.dirty).toBe(false);
    });
  });
});
