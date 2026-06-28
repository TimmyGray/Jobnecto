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
    let captured = '';
    fixture.componentInstance.registerOnChange((v: string) => (captured = v));
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
