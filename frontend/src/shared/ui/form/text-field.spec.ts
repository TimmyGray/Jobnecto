import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TextFieldComponent } from './text-field';

describe('TextFieldComponent', () => {
  let fixture: ComponentFixture<TextFieldComponent>;
  let input: HTMLInputElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(TextFieldComponent);
    fixture.componentRef.setInput('label', 'Email');
    fixture.detectChanges();
    input = fixture.nativeElement.querySelector('input');
  });

  it('renders the label tied to the input', () => {
    const label: HTMLLabelElement = fixture.nativeElement.querySelector('label');
    expect(label.textContent).toContain('Email');
    expect(label.getAttribute('for')).toBe(input.getAttribute('id'));
  });

  it('shows a required marker and aria-required when required', () => {
    fixture.componentRef.setInput('required', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('label').textContent).toContain('*');
    expect(input.getAttribute('aria-required')).toBe('true');
  });

  it('writeValue reflects the value into the input', () => {
    fixture.componentInstance.writeValue('hello');
    fixture.detectChanges();
    expect(input.value).toBe('hello');
  });

  it('writeValue(null) clears the value', () => {
    fixture.componentInstance.writeValue('x');
    fixture.detectChanges();
    fixture.componentInstance.writeValue(null);
    fixture.detectChanges();
    expect(input.value).toBe('');
  });

  it('emits changes through registerOnChange when the input fires', () => {
    let captured: string | number | null = '';
    fixture.componentInstance.registerOnChange((v) => (captured = v));
    input.value = 'typed';
    input.dispatchEvent(new Event('input'));
    expect(captured).toBe('typed');
  });

  it('notifies touched through registerOnTouched on blur', () => {
    let touched = false;
    fixture.componentInstance.registerOnTouched(() => (touched = true));
    input.dispatchEvent(new Event('blur'));
    expect(touched).toBe(true);
  });

  it('setDisabledState disables the input', () => {
    fixture.componentInstance.setDisabledState(true);
    fixture.detectChanges();
    expect(input.disabled).toBe(true);
  });

  describe('type="number"', () => {
    let numberFixture: ComponentFixture<TextFieldComponent>;
    let numberInput: HTMLInputElement;

    beforeEach(() => {
      numberFixture = TestBed.createComponent(TextFieldComponent);
      numberFixture.componentRef.setInput('label', 'Salary');
      numberFixture.componentRef.setInput('type', 'number');
      numberFixture.detectChanges();
      numberInput = numberFixture.nativeElement.querySelector('input');
    });

    it('emits a real number through registerOnChange, not the raw string', () => {
      let captured: unknown;
      numberFixture.componentInstance.registerOnChange((v) => (captured = v));
      numberInput.value = '120000';
      numberInput.dispatchEvent(new Event('input'));
      expect(captured).toBe(120000);
      expect(typeof captured).toBe('number');
    });

    it('emits null (not an empty string) when the field is cleared', () => {
      let captured: unknown = 'unset';
      numberFixture.componentInstance.registerOnChange((v) => (captured = v));
      numberInput.value = '';
      numberInput.dispatchEvent(new Event('input'));
      expect(captured).toBeNull();
    });

    it('a syntactically out-of-range value ("1e400") is sanitized to empty by the native input itself, not this component', () => {
      // Verified: HTML5 number-input value-sanitization (matched by jsdom)
      // resets .value to '' for non-finite-parsing input before any (input)
      // handler runs — Number(next) here can never see NaN/Infinity from a
      // real <input type="number">. This locks that contract in.
      numberInput.value = '1e400';
      expect(numberInput.value).toBe('');
    });

    it('writeValue(0) displays 0, not an empty field', () => {
      numberFixture.componentInstance.writeValue(0);
      numberFixture.detectChanges();
      expect(numberInput.value).toBe('0');
    });

    it('a text-type field still emits the raw string (unchanged behavior)', () => {
      let captured: unknown;
      fixture.componentInstance.registerOnChange((v) => (captured = v));
      input.value = 'hello';
      input.dispatchEvent(new Event('input'));
      expect(captured).toBe('hello');
    });
  });

  it('marks aria-invalid and points aria-describedby at the error region when in error', () => {
    fixture.componentRef.setInput('error', 'Required');
    fixture.detectChanges();
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.getAttribute('aria-describedby')).toContain('-error');
    expect(fixture.nativeElement.textContent).toContain('Required');
  });

  it('points aria-describedby at the hint when a hint is present and there is no error', () => {
    fixture.componentRef.setInput('hint', 'We never share it');
    fixture.detectChanges();
    expect(input.getAttribute('aria-describedby')).toContain('-hint');
    expect(fixture.nativeElement.textContent).toContain('We never share it');
  });

  it('has no aria-describedby when there is neither hint nor error', () => {
    expect(input.getAttribute('aria-describedby')).toBeNull();
  });
});
