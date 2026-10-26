import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { ProblemDetails } from '@shared/api';
import { ResumeFormComponent } from './resume-form';
import type { CreateResumeCommand, ResumeResult, UpdateResumeCommand } from '@entities/resume';

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

  it('omits skills and workLocationType from the request body when only a title is filled in', () => {
    let emitted: CreateResumeCommand | undefined;
    component.create.subscribe((cmd: CreateResumeCommand) => (emitted = cmd));

    component.form.controls.title.setValue('Only a title');
    component.form.markAsDirty();
    component.onSubmit();

    // What the API receives: undefined members are dropped by JSON serialisation.
    const body = JSON.parse(JSON.stringify(emitted));
    expect(body).toEqual({ title: 'Only a title', locations: [], languages: [] });
    expect(body).not.toHaveProperty('skills');
    expect(body).not.toHaveProperty('workLocationType');
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

describe('ResumeFormComponent — edit mode', () => {
  let fixture: ComponentFixture<ResumeFormComponent>;
  let component: ResumeFormComponent;
  let updates: UpdateResumeCommand[];
  let creates: CreateResumeCommand[];

  const LOADED: ResumeResult = {
    id: 'r-1',
    title: 'Staff Engineer',
    skills: ['TypeScript', 'Angular'],
    workLocationType: 'Remote',
    experience: 'ThreeToFiveYears',
    currency: 'USD',
    salary: 5000,
    locations: ['Germany'],
    languages: [{ language: 'English', level: 'Advanced' }],
  };

  beforeEach(() => {
    fixture = TestBed.createComponent(ResumeFormComponent);
    component = fixture.componentInstance;
    updates = [];
    creates = [];
    component.update.subscribe((u) => updates.push(u));
    component.create.subscribe((c) => creates.push(c));
    fixture.componentRef.setInput('resume', LOADED);
    fixture.detectChanges();
  });

  const submitButton = (): HTMLButtonElement =>
    fixture.nativeElement.querySelector('button[type="submit"]');

  /** Simulates the user changing a control. */
  function edit<T>(control: { setValue(v: T): void; markAsDirty(): void }, value: T): void {
    control.setValue(value);
    control.markAsDirty();
    fixture.detectChanges();
  }

  it('populates every control from the résumé, including language rows and locations', () => {
    const value = component.form.getRawValue();

    expect(value).toMatchObject({
      title: 'Staff Engineer',
      skills: ['TypeScript', 'Angular'],
      workLocationType: 'Remote',
      experience: 'ThreeToFiveYears',
      currency: 'USD',
      salary: 5000,
      locations: ['Germany'],
      languages: [{ language: 'English', level: 'Advanced' }],
    });
    expect(fixture.nativeElement.querySelectorAll('fieldset ui-select-field').length).toBe(2);
  });

  it('is pristine after loading, with Save disabled and labelled "Save changes"', () => {
    expect(component.form.pristine).toBe(true);
    expect(component.canSubmit).toBe(false);
    expect(submitButton().disabled).toBe(true);
    expect(submitButton().textContent?.trim()).toBe('Save changes');
  });

  it('enables Save after a real change and disables it again after reverting', () => {
    edit(component.form.controls.title, 'Principal Engineer');
    expect(component.canSubmit).toBe(true);
    expect(submitButton().disabled).toBe(false);

    edit(component.form.controls.title, 'Staff Engineer');
    expect(component.form.dirty).toBe(true);
    expect(component.canSubmit).toBe(false);
    expect(submitButton().disabled).toBe(true);
  });

  it('emits update with only the changed fields, and never create', () => {
    edit(component.form.controls.currency, 'EUR');
    edit(component.form.controls.salary, 6000);

    component.onSubmit();

    expect(updates).toEqual([{ currency: 'EUR', salary: 6000 }]);
    expect(creates).toEqual([]);
  });

  it('sends a changed language row whole', () => {
    edit(component.form.controls.languages.at(0).controls.level, 'Native');

    component.onSubmit();

    expect(updates).toEqual([{ languages: [{ language: 'English', level: 'Native' }] }]);
  });

  it('does not emit anything when nothing changed', () => {
    component.onSubmit();

    expect(updates).toEqual([]);
    expect(creates).toEqual([]);
  });

  it('does not emit while another save is in flight', () => {
    fixture.componentRef.setInput('submitting', true);
    edit(component.form.controls.title, 'Changed');

    component.onSubmit();

    expect(updates).toEqual([]);
    expect(submitButton().disabled).toBe(true);
  });

  it('does not emit an invalid edit and surfaces the validation error', () => {
    edit(component.form.controls.title, 'x'.repeat(501));

    component.onSubmit();

    expect(updates).toEqual([]);
    expect(component.errorFor('title')).toContain('500');
  });

  it.each([
    ['skills', 'skills', [], { skills: [] }],
    ['salary', 'salary', null, { clearSalary: true }],
    ['currency', 'currency', '', { currency: '' }],
    ['workLocationType', 'worklocationtype', '', { workLocationType: '' }],
    ['experience', 'experience', '', { experience: '' }],
  ] as const)(
    'clearing %s enables Save, shows no error, and emits the clear encoding',
    (control, key, empty, expected) => {
      edit(component.form.controls[control] as never, empty as never);

      expect(component.errorFor(key)).toBe('');
      expect(component.canSubmit).toBe(true);
      expect(submitButton().disabled).toBe(false);
      component.onSubmit();
      expect(updates).toEqual([expected]);
    },
  );

  it('emits a clear together with a value change, and renders no field error', () => {
    edit(component.form.controls.title, 'New title');
    edit(component.form.controls.salary, null);
    edit(component.form.controls.skills, []);

    const shownErrors = Array.from<HTMLElement>(
      fixture.nativeElement.querySelectorAll('[id$="-error"], [role="alert"]'),
    ).filter((n) => n.textContent?.trim());
    expect(shownErrors).toEqual([]);
    component.onSubmit();

    expect(updates).toEqual([{ title: 'New title', clearSalary: true, skills: [] }]);
  });

  it('allows clearing the title, locations and languages', () => {
    edit(component.form.controls.title, '');
    edit(component.form.controls.locations, []);
    component.removeLanguageRow(0);
    component.form.controls.languages.markAsDirty();
    fixture.detectChanges();

    component.onSubmit();

    expect(updates).toEqual([{ title: '', locations: [], languages: [] }]);
  });

  it('adopts a new résumé as the baseline: pristine again, and the next diff is against it', () => {
    edit(component.form.controls.title, 'Principal Engineer');
    const saved: ResumeResult = { ...LOADED, title: 'Principal Engineer' };

    fixture.componentRef.setInput('resume', saved);
    fixture.detectChanges();

    expect(component.form.pristine).toBe(true);
    expect(component.canSubmit).toBe(false);

    edit(component.form.controls.currency, 'EUR');
    component.onSubmit();
    expect(updates).toEqual([{ currency: 'EUR' }]);
  });

  it('discards unsaved edits when handed a different résumé', () => {
    edit(component.form.controls.title, 'Unsaved');

    fixture.componentRef.setInput('resume', { ...LOADED, id: 'r-2', title: 'Other' });
    fixture.detectChanges();

    expect(component.form.controls.title.value).toBe('Other');
    expect(component.form.pristine).toBe(true);
  });

  it('maps a server 400 inline in edit mode and keeps the edits', () => {
    edit(component.form.controls.title, 'Changed');

    component.applyServerErrors({
      status: 400,
      title: 'Validation failed',
      errors: { Title: ['title is not allowed.'] },
    } as ProblemDetails);

    expect(component.errorFor('title')).toBe('title is not allowed.');
    expect(component.form.controls.title.value).toBe('Changed');
  });

  it('loads a résumé with unrecognised enum strings without throwing', () => {
    expect(() => {
      fixture.componentRef.setInput('resume', {
        ...LOADED,
        experience: 'SomethingNew',
        currency: 'XXX',
        workLocationType: 'Moon',
      });
      fixture.detectChanges();
    }).not.toThrow();
    expect(component.form.controls.experience.value).toBe('SomethingNew');
  });

  it('loads a résumé with null optional fields as empty controls', () => {
    fixture.componentRef.setInput('resume', { id: 'r-9', title: null, skills: null, salary: null });
    fixture.detectChanges();

    const value = component.form.getRawValue();
    expect(value.title).toBe('');
    expect(value.skills).toEqual([]);
    expect(value.salary).toBeNull();
    expect(value.languages).toEqual([]);
  });

  it('treats unparseable salary input as an error, not as a clear', () => {
    edit(component.form.controls.salary, NaN);

    expect(component.errorFor('salary')).toBe('Must be a number.');
    expect(component.canSubmit).toBe(false);
    component.onSubmit();
    expect(updates).toEqual([]);
  });

  it('takes a saved résumé with cleared fields as the new baseline', () => {
    edit(component.form.controls.skills, []);
    edit(component.form.controls.salary, null);
    component.onSubmit();

    fixture.componentRef.setInput('resume', { ...LOADED, skills: [], salary: null });
    fixture.detectChanges();

    expect(component.form.controls.skills.value).toEqual([]);
    expect(component.form.controls.salary.value).toBeNull();
    expect(component.form.pristine).toBe(true);
    expect(component.canSubmit).toBe(false);
  });

  it('keeps Save disabled for an invalid edit', () => {
    edit(component.form.controls.title, 'x'.repeat(501));

    expect(component.canSubmit).toBe(false);
    expect(submitButton().disabled).toBe(true);
  });

  it('requires the form to be dirty even when the values differ from the baseline', () => {
    component.form.controls.title.setValue('Changed programmatically');
    fixture.detectChanges();

    expect(component.form.dirty).toBe(false);
    expect(component.canSubmit).toBe(false);
  });

  it('clears a stale general banner and row errors when the saved résumé arrives', () => {
    edit(component.form.controls.title, 'Changed');
    component.applyServerErrors({
      status: 400,
      title: 'Validation failed',
      errors: { Mystery: ['nope'] },
    } as ProblemDetails);
    expect(component.generalError()).not.toBe('');

    fixture.componentRef.setInput('resume', { ...LOADED, title: 'Changed' });
    fixture.detectChanges();

    expect(component.generalError()).toBe('');
  });

  it('clears the general banner when handed a different résumé', () => {
    component.applyServerErrors({ status: 500, title: 'Boom' } as ProblemDetails);
    expect(component.generalError()).toBe('Boom');

    fixture.componentRef.setInput('resume', { ...LOADED, id: 'r-2' });
    fixture.detectChanges();

    expect(component.generalError()).toBe('');
  });

  it('after a 400 Save stays disabled until the rejected field is edited again', () => {
    edit(component.form.controls.title, 'Rejected');
    component.onSubmit();
    component.applyServerErrors({
      status: 400,
      title: 'Validation failed',
      errors: { Title: ['title is not allowed.'] },
    } as ProblemDetails);
    fixture.detectChanges();
    expect(component.canSubmit).toBe(false);

    edit(component.form.controls.title, 'Accepted');
    expect(component.canSubmit).toBe(true);
  });

  it('treats removing a language row as an edit that enables Save', () => {
    component.removeLanguageRow(0);
    fixture.detectChanges();

    expect(component.form.dirty).toBe(true);
    expect(component.canSubmit).toBe(true);
    component.onSubmit();
    expect(updates).toEqual([{ languages: [] }]);
  });

  it('adding an empty language row alone is not a change', () => {
    component.addLanguageRow();
    fixture.detectChanges();

    expect(component.canSubmit).toBe(false);
  });

  it('shows the validation error for a stored value the form rejects, instead of a silently disabled Save', () => {
    fixture.componentRef.setInput('resume', { ...LOADED, experience: 'SomethingNew' });
    fixture.detectChanges();

    expect(component.form.pristine).toBe(true);
    expect(component.canSubmit).toBe(false);
    expect(component.errorFor('experience')).toBe('Please choose a valid option.');
  });

  it('does not show errors on load when every stored value is valid', () => {
    expect(component.form.touched).toBe(false);
  });

  describe('edits made while an update is in flight', () => {
    it('keeps the newer edits when the saved résumé arrives, and sends only them next', () => {
      edit(component.form.controls.title, 'Sent title');
      component.onSubmit();
      expect(updates).toEqual([{ title: 'Sent title' }]);

      edit(component.form.controls.currency, 'EUR');
      fixture.componentRef.setInput('resume', { ...LOADED, title: 'Sent title' });
      fixture.detectChanges();

      expect(component.form.controls.currency.value).toBe('EUR');
      expect(component.form.controls.title.value).toBe('Sent title');
      expect(component.canSubmit).toBe(true);

      component.onSubmit();
      expect(updates[1]).toEqual({ currency: 'EUR' });
    });

    it('resets normally when nothing was edited after sending', () => {
      edit(component.form.controls.title, 'Sent title');
      component.onSubmit();

      fixture.componentRef.setInput('resume', { ...LOADED, title: 'Sent title' });
      fixture.detectChanges();

      expect(component.form.pristine).toBe(true);
      expect(component.canSubmit).toBe(false);
    });

    it('always repopulates when handed a different résumé, even after edits since sending', () => {
      edit(component.form.controls.title, 'Sent title');
      component.onSubmit();
      edit(component.form.controls.currency, 'EUR');

      fixture.componentRef.setInput('resume', { ...LOADED, id: 'r-2', title: 'Other', currency: 'USD' });
      fixture.detectChanges();

      expect(component.form.controls.title.value).toBe('Other');
      expect(component.form.controls.currency.value).toBe('USD');
      expect(component.form.pristine).toBe(true);
    });
  });

  it('still emits create (not update) when no résumé is supplied', () => {
    const create = TestBed.createComponent(ResumeFormComponent);
    const createEmits: CreateResumeCommand[] = [];
    const updateEmits: UpdateResumeCommand[] = [];
    create.componentInstance.create.subscribe((c) => createEmits.push(c));
    create.componentInstance.update.subscribe((u) => updateEmits.push(u));
    create.detectChanges();
    create.componentInstance.form.controls.title.setValue('Fresh');
    create.componentInstance.form.controls.title.markAsDirty();

    create.componentInstance.onSubmit();

    expect(createEmits).toHaveLength(1);
    expect(updateEmits).toEqual([]);
    expect(create.nativeElement.querySelector('button[type="submit"]').textContent.trim()).toBe(
      'Create resume',
    );
  });
});
