import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TagInputComponent } from './tag-input';

describe('TagInputComponent', () => {
  let fixture: ComponentFixture<TagInputComponent>;
  let textbox: HTMLInputElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(TagInputComponent);
    fixture.componentRef.setInput('label', 'Skills');
    fixture.detectChanges();
    textbox = fixture.nativeElement.querySelector('input[type="text"]');
  });

  function typeAndPressEnter(text: string): void {
    textbox.value = text;
    textbox.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    textbox.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();
  }

  it('renders the label tied to the text box', () => {
    const label: HTMLLabelElement = fixture.nativeElement.querySelector('label');
    expect(label.textContent).toContain('Skills');
    expect(label.getAttribute('for')).toBe(textbox.getAttribute('id'));
  });

  it('adds a tag on Enter and clears the text box', () => {
    let captured: string[] = [];
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));

    typeAndPressEnter('TypeScript');

    expect(captured).toEqual(['TypeScript']);
    expect(textbox.value).toBe('');
    expect(fixture.nativeElement.textContent).toContain('TypeScript');
  });

  it('does not add a tag when Enter confirms IME composition (CJK input), not the user submitting', () => {
    let captured: string[] | undefined;
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));

    textbox.value = '日本語';
    textbox.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    textbox.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', isComposing: true }));
    fixture.detectChanges();

    expect(captured).toBeUndefined();
    expect(textbox.value).toBe('日本語');
  });

  it('adds a tag on comma', () => {
    let captured: string[] = [];
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));

    textbox.value = 'Angular,';
    textbox.dispatchEvent(new Event('input'));
    textbox.dispatchEvent(new KeyboardEvent('keydown', { key: ',' }));
    fixture.detectChanges();

    expect(captured).toEqual(['Angular']);
  });

  it('rejects an empty (whitespace-only) tag without clearing invalid input state', () => {
    let captured: string[] | undefined;
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));

    typeAndPressEnter('   ');

    expect(captured).toBeUndefined();
    expect(fixture.nativeElement.textContent).toContain('30 characters');
  });

  it('rejects a tag longer than 30 characters and keeps the input text', () => {
    let captured: string[] | undefined;
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));

    const tooLong = 'x'.repeat(31);
    typeAndPressEnter(tooLong);

    expect(captured).toBeUndefined();
    expect(textbox.value).toBe(tooLong);
    expect(fixture.nativeElement.textContent).toContain('30 characters');
  });

  it('accepts a tag exactly 30 characters long', () => {
    let captured: string[] = [];
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));

    typeAndPressEnter('x'.repeat(30));

    expect(captured).toEqual(['x'.repeat(30)]);
  });

  it('removes the last tag on Backspace when the text box is empty', () => {
    let captured: string[] = [];
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));
    fixture.componentInstance.writeValue(['React', 'Angular']);
    fixture.detectChanges();

    textbox.dispatchEvent(new KeyboardEvent('keydown', { key: 'Backspace' }));
    fixture.detectChanges();

    expect(captured).toEqual(['React']);
  });

  it('does not remove a tag on Backspace when the text box has content', () => {
    let captured: string[] | undefined;
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));
    fixture.componentInstance.writeValue(['React']);
    fixture.detectChanges();

    textbox.value = 'a';
    textbox.dispatchEvent(new Event('input'));
    textbox.dispatchEvent(new KeyboardEvent('keydown', { key: 'Backspace' }));
    fixture.detectChanges();

    expect(captured).toBeUndefined();
  });

  it('removes a tag via its remove button', () => {
    let captured: string[] = [];
    fixture.componentInstance.registerOnChange((v: string[]) => (captured = v));
    fixture.componentInstance.writeValue(['React', 'Angular']);
    fixture.detectChanges();

    const removeButtons = fixture.nativeElement.querySelectorAll('button[data-testid="remove-tag"]');
    (removeButtons[0] as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(captured).toEqual(['Angular']);
  });

  it('writeValue clears a stale rejection message and draft text from a previous failed add', () => {
    typeAndPressEnter('x'.repeat(31)); // rejected: too long, draft + error message linger by design
    expect(fixture.nativeElement.textContent).toContain('30 characters');

    fixture.componentInstance.writeValue([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('30 characters');
    expect(textbox.value).toBe('');
  });

  it('writeValue(null) clears the tag list', () => {
    fixture.componentInstance.writeValue(['React']);
    fixture.detectChanges();
    fixture.componentInstance.writeValue(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('button[data-testid="remove-tag"]').length).toBe(0);
  });

  it('notifies touched through registerOnTouched on blur', () => {
    let touched = false;
    fixture.componentInstance.registerOnTouched(() => (touched = true));
    textbox.dispatchEvent(new Event('blur'));
    expect(touched).toBe(true);
  });

  it('setDisabledState disables the text box', () => {
    fixture.componentInstance.setDisabledState(true);
    fixture.detectChanges();
    expect(textbox.disabled).toBe(true);
  });
});
