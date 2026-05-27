import { describe, expect, it, beforeEach } from 'vitest';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmptyStateComponent } from './empty-state';
import { ErrorStateComponent } from './error-state';
import { NotFoundStateComponent } from './not-found-state';
import { SkeletonComponent } from './skeleton';

describe('EmptyStateComponent', () => {
  it('renders headline and guidance with role=status', async () => {
    @Component({
      imports: [EmptyStateComponent],
      template: `<ui-empty-state headline="No resumes yet" guidance="Create your first one." />`,
    })
    class Host {}

    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('[role="status"]')).toBeTruthy();
    expect(el.textContent).toContain('No resumes yet');
    expect(el.textContent).toContain('Create your first one.');
  });
});

describe('ErrorStateComponent', () => {
  let fixture: ComponentFixture<ErrorStateComponent>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ErrorStateComponent);
    fixture.detectChanges();
  });

  it('uses role=alert with aria-live=assertive', () => {
    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).toBeTruthy();
    expect(alert.getAttribute('aria-live')).toBe('assertive');
  });

  it('emits retry when the Retry button is clicked', () => {
    let emitted = false;
    fixture.componentInstance.retry.subscribe(() => (emitted = true));
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.click();
    expect(emitted).toBe(true);
  });
});

describe('NotFoundStateComponent', () => {
  it('emits back when the back button is clicked', () => {
    const fixture = TestBed.createComponent(NotFoundStateComponent);
    fixture.detectChanges();
    let emitted = false;
    fixture.componentInstance.back.subscribe(() => (emitted = true));
    fixture.nativeElement.querySelector('button').click();
    expect(emitted).toBe(true);
  });
});

describe('SkeletonComponent', () => {
  it('renders the requested number of placeholder lines and is aria-hidden', () => {
    const fixture = TestBed.createComponent(SkeletonComponent);
    fixture.componentRef.setInput('lines', 4);
    fixture.detectChanges();
    const container = fixture.nativeElement.querySelector('[aria-hidden="true"]');
    expect(container).toBeTruthy();
    expect(container.children.length).toBe(4);
  });
});
