import { describe, expect, it, beforeEach } from 'vitest';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UserService } from '@entities/user';
import type { UserProfile } from '@entities/user';
import { DashboardPage } from './dashboard.page';

describe('DashboardPage', () => {
  const profile = signal<UserProfile | null>(null);

  beforeEach(() => {
    profile.set(null);
    TestBed.configureTestingModule({
      providers: [{ provide: UserService, useValue: { profile } }],
    });
  });

  it('shows a generic welcome and "being prepared" copy when there is no profile', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('Welcome');
    expect(text).toContain('Your dashboard is being prepared.');
  });

  it('greets the hydrated user by login name and shows their email', () => {
    profile.set({ loginName: 'ada', email: 'ada@example.com' } as UserProfile);

    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('ada');
    expect(text).toContain("You're signed in as ada@example.com.");
  });

  it('exposes the service profile signal on the component', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    expect(fixture.componentInstance.profile).toBe(profile);
  });
});
