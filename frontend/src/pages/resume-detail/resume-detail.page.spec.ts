import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { By } from '@angular/platform-browser';
import { ResumeService } from '@entities/resume';
import type { ResumeResult } from '@entities/resume';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { ToastService } from '@shared/ui';
import { ResumeFormComponent } from '@widgets/resume-form';
import { ResumeDetailPage } from './resume-detail.page';

/** Stands in for the résumé list, the destination of the recovery CTAs. */
@Component({ template: 'list' })
class ListStub {}

const LOADED: ResumeResult = {
  id: 'r-1',
  title: 'Staff Engineer',
  skills: ['TypeScript', 'Angular'],
  workLocationType: 'Remote',
  experience: 'ThreeToFiveYears',
  currency: 'USD',
  salary: 5000,
  locations: ['Germany'],
  languages: [{ language: 'English', level: 'Advanced' }],
};

describe('ResumeDetailPage', () => {
  let harness: RouterTestingHarness;
  let httpMock: HttpTestingController;
  let toasts: ToastService;
  let router: Router;

  const base = `${env.apiBaseUrl}/resumes`;
  const expectGet = (id: string): TestRequest =>
    httpMock.expectOne((req) => req.method === 'GET' && req.url === `${base}/${id}`);
  const expectPatch = (id: string): TestRequest =>
    httpMock.expectOne((req) => req.method === 'PATCH' && req.url === `${base}/${id}`);
  const el = (): HTMLElement => harness.routeNativeElement as HTMLElement;
  const text = (): string => el().textContent ?? '';
  const form = (): ResumeFormComponent =>
    harness.routeDebugElement!.query(By.directive(ResumeFormComponent)).componentInstance;
  const problem = (req: TestRequest, status: number, body: object = {}): void =>
    req.flush({ title: `Error ${status}`, status, ...body }, { status, statusText: `E${status}` });
  const detect = () => harness.detectChanges();

  /** Navigates to the detail route and returns the pending GET. */
  async function open(id: string): Promise<TestRequest> {
    await harness.navigateByUrl(`/resumes/${id}`, ResumeDetailPage);
    return expectGet(id);
  }

  /** Simulates the user changing a form control. */
  function edit<T>(control: { setValue(v: T): void; markAsDirty(): void }, value: T): void {
    control.setValue(value);
    control.markAsDirty();
    detect();
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'resumes/:id', component: ResumeDetailPage },
          { path: 'resumes', component: ListStub },
        ]),
      ],
    });
    harness = await RouterTestingHarness.create();
    httpMock = TestBed.inject(HttpTestingController);
    toasts = TestBed.inject(ToastService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } finally {
      (document.activeElement as HTMLElement | null)?.blur?.();
    }
  });

  describe('loading and showing the résumé (AC1)', () => {
    it('shows a labelled loading region first, then the populated form', async () => {
      const req = await open('r-1');
      expect(el().querySelector('[data-testid="resume-skeleton"]')).not.toBeNull();
      expect(el().querySelector('widget-resume-form')).toBeNull();

      req.flush(LOADED);
      detect();

      expect(el().querySelector('[data-testid="resume-skeleton"]')).toBeNull();
      expect(form().form.controls.title.value).toBe('Staff Engineer');
      expect(form().form.controls.languages.length).toBe(1);
      expect(form().form.pristine).toBe(true);
    });

    it('GETs the résumé with credentials', async () => {
      const req = await open('r-1');
      expect(req.request.withCredentials).toBe(true);
      req.flush(LOADED);
    });

    it('renders exactly one h1 showing the résumé title', async () => {
      (await open('r-1')).flush(LOADED);
      detect();

      expect(el().querySelectorAll('h1')).toHaveLength(1);
      expect(el().querySelector('h1')?.textContent).toContain('Staff Engineer');
    });

    it('falls back to "Untitled résumé" for a blank title', async () => {
      (await open('r-1')).flush({ ...LOADED, title: '  ' });
      detect();

      expect(el().querySelector('h1')?.textContent).toContain('Untitled résumé');
    });

    it('offers a way back to the list', async () => {
      (await open('r-1')).flush(LOADED);
      detect();

      expect(el().querySelector('a[href="/resumes"]')).not.toBeNull();
    });

    it('reloads when the id changes, cancelling the previous request', async () => {
      const first = await open('r-1');

      await harness.navigateByUrl('/resumes/r-2', ResumeDetailPage);
      expect(first.cancelled).toBe(true);
      expectGet('r-2').flush({ ...LOADED, id: 'r-2', title: 'Other' });
      detect();

      expect(form().form.controls.title.value).toBe('Other');
    });

    it('discards the previous résumé while the next one loads', async () => {
      (await open('r-1')).flush(LOADED);
      detect();

      await harness.navigateByUrl('/resumes/r-2', ResumeDetailPage);
      detect();

      expect(el().querySelector('widget-resume-form')).toBeNull();
      expect(el().querySelector('[data-testid="resume-skeleton"]')).not.toBeNull();
      expectGet('r-2').flush({ ...LOADED, id: 'r-2' });
    });
  });

  describe('saving only the changed fields (AC2)', () => {
    beforeEach(async () => {
      (await open('r-1')).flush(LOADED);
      detect();
    });

    it('PATCHes exactly the changed fields', async () => {
      edit(form().form.controls.title, 'Principal Engineer');
      edit(form().form.controls.salary, 7000);

      form().onSubmit();

      const req = expectPatch('r-1');
      expect(req.request.body).toEqual({ title: 'Principal Engineer', salary: 7000 });
      expect(req.request.withCredentials).toBe(true);
      req.flush({ ...LOADED, title: 'Principal Engineer', salary: 7000 });
    });

    it('does not PATCH when nothing changed', () => {
      form().onSubmit();

      httpMock.expectNone((req) => req.method === 'PATCH');
    });

    it('disables Save while the request is in flight', () => {
      edit(form().form.controls.title, 'Changed');
      form().onSubmit();
      detect();

      expect(el().querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(true);
      form().onSubmit();
      expectPatch('r-1').flush({ ...LOADED, title: 'Changed' });
    });

    it('on 200: toasts, adopts the response as the new baseline and leaves the form pristine', () => {
      edit(form().form.controls.title, 'Principal Engineer');
      form().onSubmit();
      expectPatch('r-1').flush({ ...LOADED, title: 'Principal Engineer' });
      detect();

      expect(toasts.toasts().map((t) => t.message)).toContain('Résumé saved');
      expect(form().form.pristine).toBe(true);
      expect(el().querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(true);
      expect(el().querySelector('h1')?.textContent).toContain('Principal Engineer');
    });

    it('a second save sends only what changed since the first', () => {
      edit(form().form.controls.title, 'Principal Engineer');
      form().onSubmit();
      expectPatch('r-1').flush({ ...LOADED, title: 'Principal Engineer' });
      detect();

      edit(form().form.controls.currency, 'EUR');
      form().onSubmit();

      const req = expectPatch('r-1');
      expect(req.request.body).toEqual({ currency: 'EUR' });
      req.flush({ ...LOADED, title: 'Principal Engineer', currency: 'EUR' });
    });

    it('invalidates the cached list after a successful save', () => {
      const service = TestBed.inject(ResumeService);
      service.list(5).subscribe();
      httpMock
        .expectOne((req) => req.method === 'GET' && req.url === base)
        .flush({
          items: [],
          totalCount: 0,
          lastSeenId: null,
          lastSeenUpdatedAt: null,
          pageSize: 5,
          hasNext: false,
        });
      expect(service.page()).not.toBeNull();

      edit(form().form.controls.title, 'Changed');
      form().onSubmit();
      expectPatch('r-1').flush({ ...LOADED, title: 'Changed' });

      expect(service.page()).toBeNull();
    });
  });

  describe('save failures (AC4, AC5)', () => {
    beforeEach(async () => {
      (await open('r-1')).flush(LOADED);
      detect();
      edit(form().form.controls.title, 'Changed');
      form().onSubmit();
    });

    it('400: maps the error inline, keeps the edits and shows no toast', () => {
      problem(expectPatch('r-1'), 400, { errors: { Title: ['title is not allowed.'] } });
      detect();

      expect(form().errorFor('title')).toBe('title is not allowed.');
      expect(form().form.controls.title.value).toBe('Changed');
      expect(toasts.toasts()).toHaveLength(0);
      expect(el().querySelector('widget-resume-form')).not.toBeNull();
    });

    it('400 with an unmapped field falls back to the form summary', () => {
      problem(expectPatch('r-1'), 400, { errors: { Mystery: ['nope'] } });
      detect();

      expect(form().generalError()).not.toBe('');
    });

    it('403: replaces the form with the forbidden state and leaks nothing', () => {
      problem(expectPatch('r-1'), 403);
      detect();

      expect(el().querySelector('ui-forbidden-state')).not.toBeNull();
      expect(el().querySelector('widget-resume-form')).toBeNull();
      expect(text()).not.toContain('Staff Engineer');
      expect(text()).not.toContain('Changed');
    });

    it('404: replaces the form with the not-found state', () => {
      problem(expectPatch('r-1'), 404);
      detect();

      expect(el().querySelector('ui-not-found-state')).not.toBeNull();
      expect(el().querySelector('widget-resume-form')).toBeNull();
    });

    it('500: shows an error toast and keeps the form and the user’s edits', () => {
      problem(expectPatch('r-1'), 500, { detail: 'Boom' });
      detect();

      expect(toasts.toasts().map((t) => t.tone)).toEqual(['error']);
      expect(toasts.toasts()[0].message).toBe('Boom');
      expect(form().form.controls.title.value).toBe('Changed');
    });

    it('500 without a detail falls back to the problem title for the toast', () => {
      problem(expectPatch('r-1'), 500);
      detect();

      expect(toasts.toasts()[0].message).toBe('Error 500');
    });

    it('re-enables Save after a failed attempt so the user can retry', () => {
      problem(expectPatch('r-1'), 500);
      detect();

      expect(el().querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(false);
    });
  });

  describe('a save in flight when the route id changes', () => {
    async function startSaveOnR1(): Promise<TestRequest> {
      (await open('r-1')).flush(LOADED);
      detect();
      edit(form().form.controls.title, 'Changed');
      form().onSubmit();
      return expectPatch('r-1');
    }

    it('is cancelled, so its response cannot touch the new résumé', async () => {
      const save = await startSaveOnR1();

      await harness.navigateByUrl('/resumes/r-2', ResumeDetailPage);

      expect(save.cancelled).toBe(true);
      expectGet('r-2').flush({ ...LOADED, id: 'r-2', title: 'Second' });
      detect();
      expect(form().form.controls.title.value).toBe('Second');
      expect(toasts.toasts()).toHaveLength(0);
    });

    it('does not leave Save disabled on the new résumé', async () => {
      await startSaveOnR1();

      await harness.navigateByUrl('/resumes/r-2', ResumeDetailPage);
      expectGet('r-2').flush({ ...LOADED, id: 'r-2' });
      detect();
      edit(form().form.controls.title, 'Edited second');

      expect(el().querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(false);
      form().onSubmit();
      const req = expectPatch('r-2');
      expect(req.request.body).toEqual({ title: 'Edited second' });
      req.flush({ ...LOADED, id: 'r-2', title: 'Edited second' });
    });
  });

  it('percent-encodes a hostile id in the request URL', async () => {
    await harness.navigateByUrl('/resumes/..%2Fusers%2Fme', ResumeDetailPage);

    const req = httpMock.expectOne((r) => r.method === 'GET');
    expect(req.request.url).toBe(`${base}/..%2Fusers%2Fme`);
    problem(req, 404);
  });

  describe('keeping the user’s place while saving', () => {
    it('keeps edits typed while the save is in flight and offers them for the next save', async () => {
      (await open('r-1')).flush(LOADED);
      detect();
      edit(form().form.controls.title, 'Sent title');
      form().onSubmit();
      const req = expectPatch('r-1');

      edit(form().form.controls.currency, 'EUR');
      req.flush({ ...LOADED, title: 'Sent title' });
      detect();

      expect(form().form.controls.currency.value).toBe('EUR');
      expect(form().canSubmit).toBe(true);
      form().onSubmit();
      expect(expectPatch('r-1').request.body).toEqual({ currency: 'EUR' });
    });
  });

  describe('load failures (AC3)', () => {
    it('404: shows the not-found state with a back CTA and no form', async () => {
      problem(await open('r-404'), 404);
      detect();

      expect(el().querySelector('ui-not-found-state')).not.toBeNull();
      expect(el().querySelector('widget-resume-form')).toBeNull();
      expect(el().querySelector('[data-testid="resume-skeleton"]')).toBeNull();
    });

    it('shows not-found for a 404 that carries no body (the real route-constraint response)', async () => {
      (await open('not-a-guid')).flush(null, { status: 404, statusText: 'Not Found' });
      detect();

      expect(el().querySelector('ui-not-found-state')).not.toBeNull();
    });

    it('treats a non-GUID id exactly like not-found', async () => {
      problem(await open('not-a-guid'), 404);
      detect();

      expect(el().querySelector('ui-not-found-state')).not.toBeNull();
    });

    it('the not-found CTA goes back to the résumé list', async () => {
      problem(await open('r-404'), 404);
      detect();

      el().querySelector<HTMLButtonElement>('ui-not-found-state button')!.click();
      await harness.fixture.whenStable();

      expect(router.url).toBe('/resumes');
    });

    it('403: shows the forbidden state', async () => {
      problem(await open('r-403'), 403);
      detect();

      expect(el().querySelector('ui-forbidden-state')).not.toBeNull();
    });

    it('the forbidden CTA goes back to the résumé list', async () => {
      problem(await open('r-403'), 403);
      detect();

      el().querySelector<HTMLButtonElement>('ui-forbidden-state button')!.click();
      await harness.fixture.whenStable();

      expect(router.url).toBe('/resumes');
    });

    it('other errors: shows an error state whose Retry reloads and recovers', async () => {
      problem(await open('r-1'), 500);
      detect();
      expect(el().querySelector('ui-error-state')).not.toBeNull();

      el().querySelector<HTMLButtonElement>('ui-error-state button')!.click();
      detect();
      expect(el().querySelector('[data-testid="resume-skeleton"]')).not.toBeNull();
      expectGet('r-1').flush(LOADED);
      detect();

      expect(el().querySelector('ui-error-state')).toBeNull();
      expect(form().form.controls.title.value).toBe('Staff Engineer');
    });

    it('moves focus to the loading region when Retry removes its own button', async () => {
      problem(await open('r-1'), 500);
      detect();
      const retry = el().querySelector<HTMLButtonElement>('ui-error-state button')!;
      retry.focus();

      retry.click();
      detect();
      detect();

      expect(document.activeElement).toBe(el().querySelector('[data-testid="resume-skeleton"]'));
      expectGet('r-1').flush(LOADED);
    });

    it('a failed load does not bleed into the next id', async () => {
      problem(await open('r-404'), 404);
      detect();

      await harness.navigateByUrl('/resumes/r-1', ResumeDetailPage);
      expectGet('r-1').flush(LOADED);
      detect();

      expect(el().querySelector('ui-not-found-state')).toBeNull();
      expect(el().querySelector('widget-resume-form')).not.toBeNull();
    });
  });

  describe('teardown', () => {
    it('cancels an in-flight load when the page is destroyed', async () => {
      const req = await open('r-1');

      harness.fixture.destroy();

      expect(req.cancelled).toBe(true);
    });

    it('cancels an in-flight save when the page is destroyed', async () => {
      (await open('r-1')).flush(LOADED);
      detect();
      edit(form().form.controls.title, 'Changed');
      form().onSubmit();
      const req = expectPatch('r-1');

      harness.fixture.destroy();

      expect(req.cancelled).toBe(true);
    });
  });

  it('mounts a toast host for save feedback', async () => {
    (await open('r-1')).flush(LOADED);
    detect();

    expect(el().querySelector('ui-toast-host')).not.toBeNull();
  });
});
