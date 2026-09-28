import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SelectFieldComponent } from './select-field';

const OPTIONS = [
  { value: 'Remote', label: 'Remote' },
  { value: 'OnSite', label: 'On Site' },
  { value: 'Hybrid', label: 'Hybrid' },
];

describe('SelectFieldComponent (single mode)', () => {
  let fixture: ComponentFixture<SelectFieldComponent<string>>;
  let select: HTMLSelectElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(SelectFieldComponent);
    fixture.componentRef.setInput('label', 'Work location');
    fixture.componentRef.setInput('options', OPTIONS);
    fixture.detectChanges();
    select = fixture.nativeElement.querySelector('select');
  });

  it('renders the label tied to the select', () => {
    const label: HTMLLabelElement = fixture.nativeElement.querySelector('label');
    expect(label.textContent).toContain('Work location');
    expect(label.getAttribute('for')).toBe(select.getAttribute('id'));
  });

  it('renders one option per entry, in order, with the given labels', () => {
    const options = Array.from(select.querySelectorAll('option')).filter((o) => o.value !== '');
    expect(options.map((o) => o.value)).toEqual(['Remote', 'OnSite', 'Hybrid']);
    expect(options.map((o) => o.textContent?.trim())).toEqual(['Remote', 'On Site', 'Hybrid']);
  });

  it('the empty placeholder option has a visible hint, not a blank row', () => {
    const placeholder = select.querySelector('option[value=""]');
    expect(placeholder?.textContent?.trim().length).toBeGreaterThan(0);
  });

  it('is not a multi-select by default', () => {
    expect(select.multiple).toBe(false);
  });

  it('writeValue reflects the selected value', () => {
    fixture.componentInstance.writeValue('OnSite');
    fixture.detectChanges();
    expect(select.value).toBe('OnSite');
  });

  it('writeValue(null) resets to the empty placeholder', () => {
    fixture.componentInstance.writeValue('OnSite');
    fixture.detectChanges();
    fixture.componentInstance.writeValue(null);
    fixture.detectChanges();
    expect(select.value).toBe('');
  });

  it('emits the selected value through registerOnChange', () => {
    let captured: string | string[] = '';
    fixture.componentInstance.registerOnChange((v) => (captured = v));
    select.value = 'Hybrid';
    select.dispatchEvent(new Event('change'));
    expect(captured).toBe('Hybrid');
  });

  it('notifies touched through registerOnTouched on blur', () => {
    let touched = false;
    fixture.componentInstance.registerOnTouched(() => (touched = true));
    select.dispatchEvent(new Event('blur'));
    expect(touched).toBe(true);
  });

  it('setDisabledState disables the select', () => {
    fixture.componentInstance.setDisabledState(true);
    fixture.detectChanges();
    expect(select.disabled).toBe(true);
  });

  it('marks aria-invalid and points aria-describedby at the error region when in error', () => {
    fixture.componentRef.setInput('error', 'Required');
    fixture.detectChanges();
    expect(select.getAttribute('aria-invalid')).toBe('true');
    expect(select.getAttribute('aria-describedby')).toContain('-error');
    expect(fixture.nativeElement.textContent).toContain('Required');
  });
});

describe('SelectFieldComponent (multiple mode)', () => {
  let fixture: ComponentFixture<SelectFieldComponent<string>>;
  let select: HTMLSelectElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(SelectFieldComponent);
    fixture.componentRef.setInput('label', 'Preferred locations');
    fixture.componentRef.setInput('options', OPTIONS);
    fixture.componentRef.setInput('multiple', true);
    fixture.detectChanges();
    select = fixture.nativeElement.querySelector('select');
  });

  it('renders as a multi-select with no empty placeholder option', () => {
    expect(select.multiple).toBe(true);
    const options = Array.from(select.querySelectorAll('option'));
    expect(options.every((o) => o.value !== '')).toBe(true);
  });

  it('writeValue selects every given value', () => {
    fixture.componentInstance.writeValue(['Remote', 'Hybrid']);
    fixture.detectChanges();
    const selected = Array.from(select.selectedOptions).map((o) => o.value);
    expect(selected).toEqual(['Remote', 'Hybrid']);
  });

  it('writeValue(null) selects nothing', () => {
    fixture.componentInstance.writeValue(['Remote']);
    fixture.detectChanges();
    fixture.componentInstance.writeValue(null);
    fixture.detectChanges();
    expect(select.selectedOptions.length).toBe(0);
  });

  it('emits every selected value through registerOnChange', () => {
    let captured: string | string[] = [];
    fixture.componentInstance.registerOnChange((v) => (captured = v));
    select.options[0].selected = true;
    select.options[2].selected = true;
    select.dispatchEvent(new Event('change'));
    expect(captured).toEqual(['Remote', 'Hybrid']);
  });
});
