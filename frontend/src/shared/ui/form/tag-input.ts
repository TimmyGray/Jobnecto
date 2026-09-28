import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed,
  forwardRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let nextId = 0;

/** Mirrors `CreateResumeCommandValidator`'s skills rule: non-empty, at most 30 chars. */
const MAX_TAG_LENGTH = 30;

/**
 * Branded tag input — a thin wrapper over a native text `<input>` plus a
 * rendered tag list, themed entirely by the token layer (Career OS), matching
 * `TextFieldComponent`'s pattern. Enter or comma adds the current text as a
 * tag; Backspace on an empty text box removes the last tag; every tag has an
 * explicit, keyboard-operable remove button (UX-DR13).
 *
 * A candidate tag is checked against the mirrored backend rule (non-empty
 * after trimming, at most 30 chars) before being added; a rejected tag shows
 * inline feedback and is NOT cleared from the text box, so the user can fix
 * it in place.
 *
 * Implements ControlValueAccessor over `string[]`.
 */
@Component({
  selector: 'ui-tag-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TagInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="flex flex-col gap-1">
      <label [attr.for]="inputId" class="text-sm font-medium text-text-primary">
        {{ label() }}
        @if (required()) {
          <span class="text-status-danger" aria-hidden="true">*</span>
        }
      </label>

      <div class="flex flex-wrap items-center gap-2 rounded-md border border-border-default bg-surface px-2 py-2">
        @for (tag of tags(); track tag; let i = $index) {
          <span
            class="inline-flex items-center gap-1 rounded-pill bg-canvas px-2 py-1 text-sm text-text-primary"
          >
            {{ tag }}
            <button
              type="button"
              data-testid="remove-tag"
              [attr.aria-label]="'Remove ' + tag"
              (click)="removeAt(i)"
              class="text-text-muted hover:text-status-danger"
            >
              &times;
            </button>
          </span>
        }

        <input
          [attr.id]="inputId"
          [attr.name]="name()"
          type="text"
          [attr.aria-describedby]="describedBy()"
          [attr.aria-invalid]="hasError() ? 'true' : null"
          [disabled]="disabled"
          [value]="draft()"
          (input)="onDraftInput($event)"
          (keydown)="onKeydown($event)"
          (blur)="onBlur()"
          class="min-w-[8ch] flex-1 border-none bg-transparent text-md text-text-primary outline-none placeholder:text-text-muted"
          placeholder="Type and press Enter"
        />
      </div>

      @if (hint() && !hasError()) {
        <p [attr.id]="hintId" class="text-sm text-text-muted">{{ hint() }}</p>
      }

      <p [attr.id]="errorId" class="min-h-[1rem] text-sm text-status-danger" aria-live="polite">
        {{ rejectionMessage() || error() }}
      </p>
    </div>
  `,
})
export class TagInputComponent implements ControlValueAccessor {
  /** Visible label, tied to the text box via `for`/`id`. */
  readonly label = input.required<string>();
  /** Form control name attribute. */
  readonly name = input<string>('');
  /** Marks the field visually + via aria-required. */
  readonly required = input<boolean>(false);
  /** Optional helper text shown when there is no error. */
  readonly hint = input<string>('');
  /** Current external (e.g. server) error message. */
  readonly error = input<string>('');

  protected readonly tags = signal<string[]>([]);
  protected readonly draft = signal('');
  protected readonly rejectionMessage = signal('');

  protected readonly inputId = `ui-tag-input-${nextId++}`;
  protected readonly hintId = `${this.inputId}-hint`;
  protected readonly errorId = `${this.inputId}-error`;

  protected readonly hasError = computed(() => !!this.error() || !!this.rejectionMessage());
  protected readonly describedBy = computed(() => {
    if (this.hasError()) {
      return this.errorId;
    }
    if (this.hint()) {
      return this.hintId;
    }
    return null;
  });

  protected disabled = false;
  private readonly cdr = inject(ChangeDetectorRef);

  private onChange: (value: string[]) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string[] | null): void {
    this.tags.set(value ?? []);
    // draft/rejectionMessage are local UI state, not part of the bound
    // string[] value — but a value set from outside (e.g. form.reset())
    // should still land on a clean control, not one still showing a
    // half-typed draft or a stale rejection message from before the reset.
    this.draft.set('');
    this.rejectionMessage.set('');
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (value: string[]) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.cdr.markForCheck();
  }

  protected onDraftInput(event: Event): void {
    this.draft.set((event.target as HTMLInputElement).value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.isComposing) {
      // Enter here is confirming an IME composition (CJK/other), not the
      // user submitting a tag — let the browser's IME handle it.
      return;
    }
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault();
      this.tryAddTag();
      return;
    }
    if (event.key === 'Backspace' && this.draft() === '' && this.tags().length > 0) {
      this.removeAt(this.tags().length - 1);
    }
  }

  protected removeAt(index: number): void {
    const next = this.tags().filter((_, i) => i !== index);
    this.tags.set(next);
    this.onChange(next);
  }

  private tryAddTag(): void {
    const candidate = this.draft().replace(/,$/, '').trim();
    if (candidate.length === 0 || candidate.length > MAX_TAG_LENGTH) {
      this.rejectionMessage.set(`Each skill must be 1-${MAX_TAG_LENGTH} characters long.`);
      return;
    }

    this.rejectionMessage.set('');
    const next = [...this.tags(), candidate];
    this.tags.set(next);
    this.draft.set('');
    this.onChange(next);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
