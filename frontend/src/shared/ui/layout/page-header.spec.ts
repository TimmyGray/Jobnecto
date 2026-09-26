import { describe, expect, it } from 'vitest';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PageHeaderComponent } from './page-header';

@Component({
  imports: [PageHeaderComponent],
  template: `<ui-page-header eyebrow="Dashboard">Welcome</ui-page-header>`,
})
class BareHostComponent {}

@Component({
  imports: [PageHeaderComponent],
  template: `
    <ui-page-header eyebrow="Dashboard" subtitle="You have 3 things to do">
      Welcome
    </ui-page-header>
  `,
})
class WithSubtitleHostComponent {}

@Component({
  imports: [PageHeaderComponent],
  template: `
    <ui-page-header eyebrow="Dashboard">
      Welcome
      <button primaryAction>Do it</button>
    </ui-page-header>
  `,
})
class WithPrimaryActionHostComponent {}

describe('PageHeaderComponent', () => {
  it('renders the mono eyebrow label', () => {
    const fixture = TestBed.createComponent(BareHostComponent);
    fixture.detectChanges();
    const eyebrow: HTMLElement = fixture.nativeElement.querySelector('[data-testid="eyebrow"]');
    expect(eyebrow.textContent).toContain('Dashboard');
  });

  it('renders exactly one h1 with the projected heading content', () => {
    const fixture = TestBed.createComponent(BareHostComponent);
    fixture.detectChanges();
    const headings = fixture.nativeElement.querySelectorAll('h1');
    expect(headings.length).toBe(1);
    expect(headings[0].textContent).toContain('Welcome');
  });

  it('does not render a subtitle when none is provided', () => {
    const fixture = TestBed.createComponent(BareHostComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="subtitle"]')).toBeNull();
  });

  it('renders the subtitle when provided', () => {
    const fixture = TestBed.createComponent(WithSubtitleHostComponent);
    fixture.detectChanges();
    const subtitle: HTMLElement = fixture.nativeElement.querySelector('[data-testid="subtitle"]');
    expect(subtitle.textContent).toContain('You have 3 things to do');
  });

  it('does not render a primary action when none is projected', () => {
    const fixture = TestBed.createComponent(BareHostComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('renders the projected primaryAction content when provided', () => {
    const fixture = TestBed.createComponent(WithPrimaryActionHostComponent);
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Do it');
  });
});
