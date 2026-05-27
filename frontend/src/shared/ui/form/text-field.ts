import { ChangeDetectionStrategy, Component, computed, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let nextId = 0;

/**
 * Branded text input — a thin Angular wrapper over a native input, themed
 * entirely by the token layer (Career OS), built on primitives (no spartan-ng
 * yet — deferred to the first story needing overlay/select per Decision 1.4).
 * [AC6, AC10]
 *
 * Implements ControlValueAccessor so it drops into typed Reactive Forms.
 * Accessibility: label tied via `for`/`id`; error/hint tied via
 * `aria-describedby`; `aria-invalid` reflects the error state. The error text
 * lives in an `aria-live="polite"` region so changes are announced. [AC8]
 */
@Component({
  selector: 'ui-text-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TextFieldComponent),
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

      <input
        [attr.id]="inputId"
        [attr.name]="name()"
        [attr.type]="type()"
        [attr.autocomplete]="autoComplete()"
        [attr.maxlength]="maxLength()"
        [attr.aria-required]="required() ? 'true' : null"
        [attr.aria-invalid]="error() ? 'true' : null"
        [attr.aria-describedby]="describedBy()"
        [value]="value"
        [disabled]="disabled"
        (input)="onInput($event)"
        (blur)="onBlur()"
        class="rounded-md border bg-surface px-3 py-2 text-md text-text-primary placeholder:text-text-muted transition-colors duration-fast focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-border-focus"
        [class.border-border-default]="!error()"
        [class.border-status-danger]="!!error()"
      />

      @if (hint() && !error()) {
        <p [attr.id]="hintId" class="text-sm text-text-muted">{{ hint() }}</p>
      }

      <p
        [attr.id]="errorId"
        class="min-h-[1rem] text-sm text-status-danger"
        aria-live="polite"
      >
        {{ error() }}
      </p>
    </div>
  `,
})
export class TextFieldComponent implements ControlValueAccessor {
  /** Visible label, tied to the input via `for`/`id`. */
  readonly label = input.required<string>();
  /** Form control name attribute. */
  readonly name = input<string>('');
  /** Native input type (text, email, password, …). */
  readonly type = input<string>('text');
  /** autocomplete attribute. */
  readonly autoComplete = input<string>('');
  /** Marks the field visually + via aria-required. */
  readonly required = input<boolean>(false);
  /** Optional maxlength hint passed to the native input. */
  readonly maxLength = input<number | null>(null);
  /** Optional helper text shown when there is no error. */
  readonly hint = input<string>('');
  /** Current error message (empty when valid). Drives the error region + styling. */
  readonly error = input<string>('');

  protected readonly inputId = `ui-text-field-${nextId++}`;
  protected readonly hintId = `${this.inputId}-hint`;
  protected readonly errorId = `${this.inputId}-error`;

  /** Computes the aria-describedby id list (error wins; otherwise hint). */
  protected readonly describedBy = computed(() => {
    if (this.error()) {
      return this.errorId;
    }
    if (this.hint()) {
      return this.hintId;
    }
    return null;
  });

  protected value = '';
  protected disabled = false;

  // Use a signal so OnPush re-renders when CVA writes a value.
  private readonly valueSignal = signal('');

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value = value ?? '';
    this.valueSignal.set(this.value);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  protected onInput(event: Event): void {
    const next = (event.target as HTMLInputElement).value;
    this.value = next;
    this.valueSignal.set(next);
    this.onChange(next);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
