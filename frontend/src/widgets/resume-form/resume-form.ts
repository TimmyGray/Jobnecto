import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed,
  effect,
  inject,
  output,
  input,
  signal,
  untracked,
} from '@angular/core';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  CURRENCY_OPTIONS,
  EXPERIENCE_OPTIONS,
  LANGUAGE_LEVEL_OPTIONS,
  LANGUAGE_OPTIONS,
  LOCATION_OPTIONS,
  WORK_LOCATION_TYPE_OPTIONS,
  CURRENCIES,
  EXPERIENCES,
  LANGUAGE_LEVELS,
  LANGUAGES,
  LOCATIONS,
  WORK_LOCATION_TYPES,
} from '@entities/resume';
import type { CreateResumeCommand, ResumeResult, UpdateResumeCommand } from '@entities/resume';
import type { ProblemDetails } from '@shared/api';
import { SelectFieldComponent, TagInputComponent, TextFieldComponent } from '@shared/ui';
import { buildUpdateCommand, resumeToFormValue } from './resume-form.diff';
import {
  enumArrayMemberValidator,
  enumMemberValidator,
  languageRowValidator,
  nonNegativeValidator,
  resumeTitleValidator,
} from './resume-form.validators';

type WorkLocationType = (typeof WORK_LOCATION_TYPES)[number];
type Experience = (typeof EXPERIENCES)[number];
type Currency = (typeof CURRENCIES)[number];
type Language = (typeof LANGUAGES)[number];
type LanguageLevel = (typeof LANGUAGE_LEVELS)[number];
type Location = (typeof LOCATIONS)[number];

interface LanguageRowForm {
  language: FormControl<Language | ''>;
  level: FormControl<LanguageLevel | ''>;
}

interface ResumeFormShape {
  title: FormControl<string>;
  skills: FormControl<string[]>;
  workLocationType: FormControl<WorkLocationType | ''>;
  experience: FormControl<Experience | ''>;
  currency: FormControl<Currency | ''>;
  salary: FormControl<number | null>;
  locations: FormControl<Location[]>;
  languages: FormArray<FormGroup<LanguageRowForm>>;
}

/** Every field this form maps onto a `ProblemDetails.errors` key, lowercased. */
type SimpleFieldKey =
  | 'title'
  | 'skills'
  | 'worklocationtype'
  | 'experience'
  | 'currency'
  | 'salary'
  | 'locations';

function buildLanguageRow(): FormGroup<LanguageRowForm> {
  return new FormGroup<LanguageRowForm>(
    {
      language: new FormControl<Language | ''>('', {
        nonNullable: true,
        validators: [enumMemberValidator(LANGUAGES)],
      }),
      level: new FormControl<LanguageLevel | ''>('', {
        nonNullable: true,
        validators: [enumMemberValidator(LANGUAGE_LEVELS)],
      }),
    },
    { validators: [languageRowValidator()] },
  );
}

/**
 * Résumé create/edit form (Story 2.1 AC1, AC3; Story 2.3). Owns its typed
 * `FormGroup` and client validation; never injects `HttpClient` — the host page
 * owns the network call and calls {@link applyServerErrors} on a `400`
 * (feature-sliced rule: a widget must not call HttpClient directly — Story 1.6
 * Dev Notes).
 *
 * Create mode (no {@link resume}) emits {@link create}. Edit mode (a loaded
 * {@link resume}) emits {@link update} with only the fields that differ from
 * that résumé.
 */
@Component({
  selector: 'widget-resume-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TextFieldComponent, TagInputComponent, SelectFieldComponent],
  templateUrl: './resume-form.html',
})
export class ResumeFormComponent {
  /** Disables the submit button while a create request is in flight. */
  readonly submitting = input<boolean>(false);
  /** Emits the mapped command shape on a valid submit (create mode). */
  readonly create = output<CreateResumeCommand>();
  /**
   * The résumé being edited, or null to create a new one. Assigning a new value
   * (e.g. the response to a save) repopulates the form and becomes the baseline
   * the next diff is computed against.
   */
  readonly resume = input<ResumeResult | null>(null);
  /** Emits only the changed fields on a valid submit (edit mode). */
  readonly update = output<UpdateResumeCommand>();

  /** True when a résumé was supplied, i.e. the form edits rather than creates. */
  readonly editing = computed(() => this.resume() !== null);

  protected readonly workLocationTypeOptions = WORK_LOCATION_TYPE_OPTIONS;
  protected readonly experienceOptions = EXPERIENCE_OPTIONS;
  protected readonly currencyOptions = CURRENCY_OPTIONS;
  protected readonly languageOptions = LANGUAGE_OPTIONS;
  protected readonly languageLevelOptions = LANGUAGE_LEVEL_OPTIONS;
  protected readonly locationOptions = LOCATION_OPTIONS;

  readonly form = new FormGroup<ResumeFormShape>({
    title: new FormControl('', { nonNullable: true, validators: [resumeTitleValidator()] }),
    skills: new FormControl<string[]>([], { nonNullable: true }),
    workLocationType: new FormControl<WorkLocationType | ''>('', {
      nonNullable: true,
      validators: [enumMemberValidator(WORK_LOCATION_TYPES)],
    }),
    experience: new FormControl<Experience | ''>('', {
      nonNullable: true,
      validators: [enumMemberValidator(EXPERIENCES)],
    }),
    currency: new FormControl<Currency | ''>('', {
      nonNullable: true,
      validators: [enumMemberValidator(CURRENCIES)],
    }),
    salary: new FormControl<number | null>(null, { validators: [nonNegativeValidator()] }),
    locations: new FormControl<Location[]>([], {
      nonNullable: true,
      validators: [enumArrayMemberValidator(LOCATIONS)],
    }),
    languages: new FormArray<FormGroup<LanguageRowForm>>([]),
  });

  /** General, non-field-specific error banner (unmapped 400 fields, or a generic failure). */
  readonly generalError = signal('');

  /**
   * Server-set errors for language rows, keyed by `${index}.${field}` —
   * deliberately kept OUTSIDE Angular's own `AbstractControl.errors`.
   * `FormControlDirective.ngOnChanges` re-runs a control's validators
   * (`this.form.updateValueAndValidity(...)`) whenever it treats its bound
   * `[formControl]` as newly (re)attached — verified this fires again for a
   * dynamically-pushed `FormArray` row's controls on a render pass *after*
   * the row was already showing, silently discarding a manually
   * `setErrors()`'d value.
   *
   * A plain field, not a `signal()` — a `signal()` read from inside the
   * `@for` row loop's own per-item template was empirically verified to make
   * Angular's `@for` reconciliation treat the iterable as empty on the very
   * next change-detection pass. Every write to this field is paired with an
   * explicit `this.cdr.markForCheck()` (this component is `OnPush`; a plain
   * field mutation alone doesn't notify the change detector the way a
   * `signal()` write normally does).
   */
  private languageRowServerErrors: Record<string, string> = {};
  /** The form's value when the last update was emitted, to tell edits made while it was in flight. */
  private submittedSnapshot: string | null = null;
  /** Id of the résumé the form was last populated from. */
  private populatedId: string | undefined;
  private readonly cdr = inject(ChangeDetectorRef);

  constructor() {
    effect(() => {
      const resume = this.resume();
      if (resume) {
        untracked(() => this.adopt(resume));
      }
    });
  }

  /**
   * Create mode: enabled when the form is dirty, valid, and not submitting.
   * Edit mode: enabled when it is valid, not submitting, differs from the loaded
   * résumé (so edit-then-revert disables it again). Emptying a field is a change.
   */
  get canSubmit(): boolean {
    if (this.submitting() || !this.form.valid) {
      return false;
    }
    const diff = this.currentDiff();
    if (diff === null) {
      return this.form.dirty;
    }
    return this.form.dirty && Object.keys(diff).length > 0;
  }

  /** Appends an empty language-proficiency row. */
  addLanguageRow(): void {
    this.form.controls.languages.push(buildLanguageRow());
    this.form.controls.languages.markAsDirty();
    // Row indices shift on add/remove; a stale index→error mapping could point at the wrong row.
    this.languageRowServerErrors = {};
    this.cdr.markForCheck();
  }

  /** Removes the language-proficiency row at the given index. */
  removeLanguageRow(index: number): void {
    this.form.controls.languages.removeAt(index);
    this.form.controls.languages.markAsDirty();
    this.languageRowServerErrors = {};
    this.cdr.markForCheck();
  }

  /**
   * Returns the first plain-language error for a field, but only once the
   * control is touched/dirty (validate on blur + submit) — mirrors
   * `SignInPage.errorFor`.
   */
  errorFor(field: SimpleFieldKey): string {
    const control = this.simpleControl(field);
    if (!control.errors || !(control.touched || control.dirty)) {
      return '';
    }
    return this.messageForErrors(control.errors);
  }

  /** Same as {@link errorFor}, for a `language`/`level` control inside a language-proficiency row. */
  languageRowErrorFor(row: FormGroup<LanguageRowForm>, field: 'language' | 'level'): string {
    const index = this.form.controls.languages.controls.indexOf(row);
    const serverError = this.languageRowServerErrors[`${index}.${field}`];
    if (serverError) {
      return serverError;
    }

    const control = row.controls[field];
    if (!control.errors || !(control.touched || control.dirty)) {
      return '';
    }
    return this.messageForErrors(control.errors);
  }

  /** Submits the form. Mirrors client validation first (AC1). */
  onSubmit(): void {
    if (this.submitting()) {
      return;
    }

    this.generalError.set('');
    this.clearServerErrors();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const diff = this.currentDiff();
    if (diff === null) {
      this.create.emit(this.buildCommand());
      return;
    }
    if (Object.keys(diff).length === 0) {
      this.form.markAllAsTouched();
      return;
    }
    this.submittedSnapshot = JSON.stringify(this.form.getRawValue());
    this.update.emit(diff);
  }

  /**
   * Maps a `400` {@link ProblemDetails} onto the right controls (AC3),
   * falling back to a general banner for any field it can't attribute.
   * Mirrors `SignInPage.handleError`'s `400` branch.
   */
  applyServerErrors(problem: ProblemDetails): void {
    if (problem.status !== 400 || !problem.errors) {
      this.generalError.set(problem.detail ?? problem.title);
      return;
    }

    let hasUnmappedField = false;
    const rowErrors: Record<string, string> = {};
    const languageRowCount = this.form.controls.languages.length;

    for (const [field, messages] of Object.entries(problem.errors)) {
      if (messages.length === 0) {
        continue;
      }

      const rowMatch = /^languages\[(\d+)\]\.(language|level)$/.exec(field.toLowerCase());
      if (rowMatch) {
        const index = Number(rowMatch[1]);
        if (index < languageRowCount) {
          rowErrors[`${index}.${rowMatch[2]}`] = messages[0];
        } else {
          hasUnmappedField = true;
        }
        continue;
      }

      const control = this.matchControl(field);
      if (control) {
        control.setErrors({ server: messages[0] });
        control.markAsTouched();
      } else {
        hasUnmappedField = true;
      }
    }

    this.languageRowServerErrors = rowErrors;
    this.cdr.markForCheck();

    if ((!this.anyFieldHasServerError() && Object.keys(rowErrors).length === 0) || hasUnmappedField) {
      this.generalError.set(problem.detail ?? problem.title);
    }
  }

  /** Resets the form to a pristine, empty state (Story 2.1 Decision Record: stay on page after success). */
  resetForm(): void {
    this.form.controls.languages.clear();
    this.languageRowServerErrors = {};
    this.cdr.markForCheck();
    this.form.reset({
      title: '',
      skills: [],
      workLocationType: '',
      experience: '',
      currency: '',
      salary: null,
      locations: [],
    });
    this.generalError.set('');
  }

  /** The diff against the loaded résumé, or null in create mode. */
  private currentDiff(): UpdateResumeCommand | null {
    const resume = this.resume();
    return resume ? buildUpdateCommand(resume, this.form.getRawValue()) : null;
  }

  /**
   * Takes a new `resume` input as the baseline. Normally that repopulates the
   * form, but if the user kept editing after the last update was sent, the
   * response must not wipe those edits: the form is left alone and only the
   * baseline (which the diff reads from the input) moves, so the next Save sends
   * just the newer edits.
   */
  private adopt(resume: ResumeResult): void {
    const sameResume = resume.id !== undefined && resume.id === this.populatedId;
    const editedSinceSend =
      this.submittedSnapshot !== null && this.submittedSnapshot !== JSON.stringify(this.form.getRawValue());
    this.submittedSnapshot = null;
    this.populatedId = resume.id;
    if (sameResume && editedSinceSend) {
      this.cdr.markForCheck();
      return;
    }
    this.populate(resume);
  }

  /**
   * Fills the form from a loaded résumé, leaving it pristine and untouched so
   * Save stays disabled until the user changes something. Language rows are
   * rebuilt after the reset (the reset would otherwise blank them).
   */
  private populate(resume: ResumeResult): void {
    const value = resumeToFormValue(resume);
    // Reset first: form.reset() also resets every row already in the FormArray,
    // which would blank rows built beforehand.
    this.form.reset({
      title: value.title,
      skills: value.skills,
      workLocationType: value.workLocationType as WorkLocationType | '',
      experience: value.experience as Experience | '',
      currency: value.currency as Currency | '',
      salary: value.salary,
      locations: value.locations as Location[],
    });
    const languages = this.form.controls.languages;
    languages.clear();
    for (const row of value.languages) {
      const group = buildLanguageRow();
      group.setValue({ language: row.language as Language, level: row.level as LanguageLevel });
      languages.push(group);
    }
    this.languageRowServerErrors = {};
    this.generalError.set('');
    // A stored value the form considers invalid (e.g. an unknown enum string)
    // would otherwise disable Save with no visible reason.
    if (this.form.invalid) {
      this.form.markAllAsTouched();
    }
    this.cdr.markForCheck();
  }

  private buildCommand(): CreateResumeCommand {
    const raw = this.form.getRawValue();
    return {
      title: raw.title || undefined,
      skills: raw.skills.length > 0 ? raw.skills : undefined,
      workLocationType: raw.workLocationType || undefined,
      experience: raw.experience || undefined,
      currency: raw.currency || undefined,
      salary: raw.salary ?? undefined,
      locations: raw.locations,
      languages: raw.languages
        .filter((row) => row.language && row.level)
        .map((row) => ({ language: row.language as Language, level: row.level as LanguageLevel })),
    };
  }

  private simpleControl(field: SimpleFieldKey): AbstractControl {
    switch (field) {
      case 'title':
        return this.form.controls.title;
      case 'skills':
        return this.form.controls.skills;
      case 'worklocationtype':
        return this.form.controls.workLocationType;
      case 'experience':
        return this.form.controls.experience;
      case 'currency':
        return this.form.controls.currency;
      case 'salary':
        return this.form.controls.salary;
      case 'locations':
        return this.form.controls.locations;
    }
  }

  /**
   * Maps a raw `ProblemDetails.errors` key to the control it describes.
   * FluentValidation's `RuleForEach` (Skills, and — if a future story adds
   * one — Locations) emits one key per invalid element: `"Skills[0]"`,
   * never bare `"skills"`. None of these are bare field names, so a plain
   * `known.includes(key)` check can never match them. (A nested
   * `"Languages[0].Language"`-shaped key is handled separately in
   * {@link applyServerErrors}, via {@link languageRowServerErrors} rather
   * than this control lookup — see that field's doc comment for why.)
   */
  private matchControl(field: string): AbstractControl | null {
    const key = field.toLowerCase();

    if (/^skills(\[\d+\])?$/.test(key)) {
      return this.simpleControl('skills');
    }
    if (/^locations(\[\d+\])?$/.test(key)) {
      return this.simpleControl('locations');
    }

    const known: SimpleFieldKey[] = ['title', 'worklocationtype', 'experience', 'currency', 'salary'];
    return (known as string[]).includes(key) ? this.simpleControl(key as SimpleFieldKey) : null;
  }

  /** Every control `applyServerErrors`/`clearServerErrors` can set a `{server: ...}` error on directly. */
  private allMappableControls(): AbstractControl[] {
    return [
      this.form.controls.title,
      this.form.controls.skills,
      this.form.controls.workLocationType,
      this.form.controls.experience,
      this.form.controls.currency,
      this.form.controls.salary,
      this.form.controls.locations,
    ];
  }

  private clearServerErrors(): void {
    for (const control of this.allMappableControls()) {
      if (control.errors?.['server']) {
        control.updateValueAndValidity();
      }
    }
    this.languageRowServerErrors = {};
    this.cdr.markForCheck();
  }

  private anyFieldHasServerError(): boolean {
    return this.allMappableControls().some((c) => c.errors?.['server']);
  }

  private messageForErrors(errors: Record<string, unknown>): string {
    if (errors['server']) {
      return errors['server'] as string;
    }
    if (errors['maxlength']) {
      return 'Must be at most 500 characters.';
    }
    if (errors['min']) {
      return 'Must not be negative.';
    }
    if (errors['number']) {
      return 'Must be a number.';
    }
    if (errors['enum']) {
      return 'Please choose a valid option.';
    }
    return 'Please check this field.';
  }
}
