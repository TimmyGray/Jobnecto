import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { By } from '@angular/platform-browser';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { ResumeService } from '@entities/resume';
import { ToastService } from '@shared/ui';
import { ResumeFormComponent } from '@widgets/resume-form';
import { ResumeCreatePage } from './resume-create.page';

/** Stands in for the résumé list, where a successful create lands. */
@Component({ template: 'list' })
class ListStub {}

describe('ResumeCreatePage', () => {
  let harness: RouterTestingHarness;
  let page: ResumeCreatePage;
  let httpMock: HttpTestingController;
  let resumeService: ResumeService;
  let toastService: ToastService;
  let router: Router;

  const url = `${env.apiBaseUrl}/resumes`;
  const expectPost = (): TestRequest =>
    httpMock.expectOne((req) => req.method === 'POST' && req.url === url);
  const created = { status: 201, statusText: 'Created' };
  const form = (): ResumeFormComponent =>
    harness.routeDebugElement!.query(By.directive(ResumeFormComponent)).componentInstance;

  /** Lets a pending navigation (and the `.then`/`.catch` after it) settle. */
  async function settle(): Promise<void> {
    await harness.fixture.whenStable();
    await Promise.resolve();
    await Promise.resolve();
  }

  /** Fills in a title through the form, as a user would, and submits it. */
  function submitTitle(title: string): void {
    form().form.controls.title.setValue(title);
    form().form.controls.title.markAsDirty();
    harness.detectChanges();
    form().onSubmit();
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'resumes/new', component: ResumeCreatePage },
          { path: 'resumes', component: ListStub },
        ]),
      ],
    });
    harness = await RouterTestingHarness.create();
    page = await harness.navigateByUrl('/resumes/new', ResumeCreatePage);
    httpMock = TestBed.inject(HttpTestingController);
    resumeService = TestBed.inject(ResumeService);
    toastService = TestBed.inject(ToastService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('renders the résumé form (toasts are hosted once by the app shell)', () => {
    const el = harness.routeNativeElement as HTMLElement;
    expect(el.querySelector('widget-resume-form')).toBeTruthy();
    expect(el.querySelector('ui-toast-host')).toBeNull();
  });

  it('on 201: lands on /resumes, toasts once there, and leaves the list cache invalidated', async () => {
    resumeService.list(5).subscribe();
    httpMock.expectOne((req) => req.method === 'GET' && req.url === url).flush({
      items: [],
      totalCount: 0,
      lastSeenId: null,
      lastSeenUpdatedAt: null,
      pageSize: 5,
      hasNext: false,
    });
    expect(resumeService.page()).not.toBeNull();

    submitTitle('Staff Engineer');
    const req = expectPost();
    expect(req.request.body).toEqual({ title: 'Staff Engineer', locations: [], languages: [] });
    req.flush({ id: 'r-1', title: 'Staff Engineer' }, created);
    await settle();

    expect(router.url).toBe('/resumes');
    expect(toastService.toasts().map((t) => [t.message, t.tone])).toEqual([
      ['Résumé created', 'success'],
    ]);
    expect(resumeService.page()).toBeNull();
  });

  it('does not replace the history entry when navigating to the list', async () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    page.onCreate({ title: 'Staff Engineer' });
    expectPost().flush({ id: 'r-1' }, created);
    await settle();

    expect(navigate).toHaveBeenCalledWith(['/resumes']);
  });

  it('raises the toast only once navigation has landed', async () => {
    let finish!: (ok: boolean) => void;
    vi.spyOn(router, 'navigate').mockReturnValue(new Promise<boolean>((r) => (finish = r)));

    page.onCreate({ title: 'Staff Engineer' });
    expectPost().flush({ id: 'r-1' }, created);
    expect(toastService.toasts()).toHaveLength(0);

    finish(true);
    await settle();
    expect(toastService.toasts().map((t) => t.message)).toEqual(['Résumé created']);
  });

  it('keeps submitting() true after the 201 until navigation settles, so a second submit sends no second POST', async () => {
    let finish!: (ok: boolean) => void;
    vi.spyOn(router, 'navigate').mockReturnValue(new Promise<boolean>((r) => (finish = r)));

    submitTitle('Staff Engineer');
    expect(page.submitting()).toBe(true);
    expectPost().flush({ id: 'r-1' }, created);
    harness.detectChanges();

    expect(page.submitting()).toBe(true);
    const submit = (harness.routeNativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      'button[type="submit"]',
    );
    expect(submit?.disabled).toBe(true);
    form().onSubmit();
    page.onCreate({ title: 'Staff Engineer' });
    httpMock.expectNone((req) => req.method === 'POST');

    finish(true);
    await settle();
  });

  it.each([
    ['refused', () => Promise.resolve(false)],
    ['rejected', () => Promise.reject(new Error('ChunkLoadError'))],
  ])(
    'if navigation is %s: stays here, still toasts, resets the form and re-enables submit',
    async (_kind, outcome) => {
      vi.spyOn(router, 'navigate').mockImplementation(outcome);

      submitTitle('Staff Engineer');
      expectPost().flush({ id: 'r-1' }, created);
      await settle();
      harness.detectChanges();

      expect(router.url).toBe('/resumes/new');
      expect(toastService.toasts().map((t) => t.message)).toEqual(['Résumé created']);
      expect(form().form.controls.title.value).toBe('');
      expect(form().form.pristine).toBe(true);
      expect(page.submitting()).toBe(false);
    },
  );

  it('on 400: forwards the ProblemDetails to the form instead of toasting, and stays here', () => {
    const navigate = vi.spyOn(router, 'navigate');
    page.onCreate({ title: 'x'.repeat(501) });

    expectPost().flush(
      { title: 'Validation failed', status: 400, errors: { title: ['title must be at most 500 characters long.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(toastService.toasts()).toHaveLength(0);
    expect(page.resumeForm().errorFor('title')).toBe('title must be at most 500 characters long.');
    expect(page.submitting()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('on a non-400 error: shows an error toast and re-enables submit', () => {
    page.onCreate({ title: 'Staff Engineer' });

    expectPost().flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });

    expect(toastService.toasts()).toHaveLength(1);
    expect(toastService.toasts()[0].tone).toBe('error');
    expect(page.submitting()).toBe(false);
  });

  it('cancels the in-flight request on destroy, so a late response can never toast or navigate', () => {
    const navigate = vi.spyOn(router, 'navigate');
    page.onCreate({ title: 'Staff Engineer' });
    const req = expectPost();

    harness.fixture.destroy();

    // takeUntilDestroyed unsubscribes on destroy, which cancels the
    // underlying HTTP request; flushing a cancelled request throws.
    expect(() => req.flush({ id: 'r-1', title: 'Staff Engineer' }, created)).toThrow(/cancelled/i);
    expect(toastService.toasts()).toHaveLength(0);
    expect(navigate).not.toHaveBeenCalled();
  });
});
