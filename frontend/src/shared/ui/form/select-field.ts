import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed,
  forwardRef,
  inject,
  input,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let nextId = 0;

/** One selectable option: the value sent on submit and its display label. */
export interface SelectOption<T extends string> {
  value: T;
  label: string;
}

/**
 * Branded select — a thin wrapper over a native `<select>`, themed entirely
 * by the token layer (Career OS), matching `TextFieldComponent`'s pattern (no
 * spartan-ng yet — Decision Record, Story 2.1). Supports both a single-value
 * select and, via `multiple`, a native multi-select (Story 2.1 AC1).
 *
 * Implements ControlValueAccessor so it drops into typed Reactive Forms. In
 * single mode the value is `T | ''`; in multi mode it is `T[]`.
 * Accessibility: label tied via `for`/`id`; error tied via `aria-describedby`;
 * `aria-invalid` reflects the error state; the error text lives in an
 * `aria-live="polite"` region.
 */
@Component({
  selector: 'ui-select-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectFieldComponent),
      multi: true,
    },
  ],
  template: `
    <div class="flex flex-col gap-1">
      <label [attr.for]="selectId" class="text-sm font-medium text-text-primary">
        {{ label() }}
        @if (required()) {
          <span class="text-status-danger" aria-hidden="true">*</span>
        }
      </label>

      <select
        [attr.id]="selectId"
        [attr.name]="name()"
        [multiple]="multiple()"
        [attr.aria-required]="required() ? 'true' : null"
        [attr.aria-invalid]="error() ? 'true' : null"
        [attr.aria-describedby]="describedBy()"
        [disabled]="disabled"
        (change)="onChangeEvent($event)"
        (blur)="onBlur()"
        class="rounded-md border bg-surface px-3 py-2 text-md text-text-primary transition-colors duration-fast focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-border-focus"
        [class.border-border-default]="!error()"
        [class.border-status-danger]="!!error()"
      >
        @if (!multiple()) {
          <option value="" [selected]="singleValue === ''">— Select —</option>
        }
        @for (option of options(); track option.value) {
          <option [value]="option.value" [selected]="isSelected(option.value)">
            {{ option.label }}
          </option>
        }
      </select>

      @if (hint() && !error()) {
        <p [attr.id]="hintId" class="text-sm text-text-muted">{{ hint() }}</p>
      }

      <p [attr.id]="errorId" class="min-h-[1rem] text-sm text-status-danger" aria-live="polite">
        {{ error() }}
      </p>
    </div>
  `,
})
export class SelectFieldComponent<T extends string> implements ControlValueAccessor {
  /** Visible label, tied to the select via `for`/`id`. */
  readonly label = input.required<string>();
  /** The selectable options, in display order. */
  readonly options = input.required<SelectOption<T>[]>();
  /** When true, renders a native multi-select and the value is `T[]`. */
  readonly multiple = input<boolean>(false);
  /** Form control name attribute. */
  readonly name = input<string>('');
  /** Marks the field visually + via aria-required. */
  readonly required = input<boolean>(false);
  /** Optional helper text shown when there is no error. */
  readonly hint = input<string>('');
  /** Current error message (empty when valid). Drives the error region + styling. */
  readonly error = input<string>('');

  protected readonly selectId = `ui-select-field-${nextId++}`;
  protected readonly hintId = `${this.selectId}-hint`;
  protected readonly errorId = `${this.selectId}-error`;

  protected readonly describedBy = computed(() => {
    if (this.error()) {
      return this.errorId;
    }
    if (this.hint()) {
      return this.hintId;
    }
    return null;
  });

  /** Selected value in single mode (empty string = nothing selected). */
  protected singleValue: T | '' = '';
  /** Selected values in multi mode. */
  protected multiValues: T[] = [];
  protected disabled = false;
  private readonly cdr = inject(ChangeDetectorRef);

  private onChange: (value: T | '' | T[]) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: T | T[] | null): void {
    if (this.multiple()) {
      this.multiValues = Array.isArray(value) ? value : [];
    } else {
      this.singleValue = Array.isArray(value) ? '' : (value ?? '');
    }
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (value: T | '' | T[]) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.cdr.markForCheck();
  }

  protected isSelected(value: T): boolean {
    return this.multiple() ? this.multiValues.includes(value) : this.singleValue === value;
  }

  protected onChangeEvent(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (this.multiple()) {
      this.multiValues = Array.from(select.selectedOptions).map((o) => o.value as T);
      this.onChange(this.multiValues);
    } else {
      this.singleValue = select.value as T | '';
      this.onChange(this.singleValue);
    }
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
