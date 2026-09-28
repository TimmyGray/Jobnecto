import { describe, expect, it, beforeEach } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { ResumeService } from '@entities/resume';
import { ToastService } from '@shared/ui';
import { ResumeCreatePage } from './resume-create.page';

describe('ResumeCreatePage', () => {
  let fixture: ComponentFixture<ResumeCreatePage>;
  let page: ResumeCreatePage;
  let httpMock: HttpTestingController;
  let resumeService: ResumeService;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResumeCreatePage],
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResumeCreatePage);
    page = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    resumeService = TestBed.inject(ResumeService);
    toastService = TestBed.inject(ToastService);
    fixture.detectChanges();
  });

  it('renders the résumé form and a toast host', () => {
    expect(fixture.nativeElement.querySelector('widget-resume-form')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('ui-toast-host')).toBeTruthy();
  });

  it('on 201: shows a success toast, resets the form, and leaves the list cache invalidated', () => {
    resumeService.list(5).subscribe();
    httpMock.expectOne((req) => req.url === `${env.apiBaseUrl}/resumes`).flush({
      items: [],
      totalCount: 0,
      lastSeenId: null,
      lastSeenUpdatedAt: null,
      pageSize: 5,
      hasNext: false,
    });
    expect(resumeService.page()).not.toBeNull();

    page.onCreate({ title: 'Staff Engineer' });

    const req = httpMock.expectOne(`${env.apiBaseUrl}/resumes`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'r-1', title: 'Staff Engineer' }, { status: 201, statusText: 'Created' });

    expect(toastService.toasts()).toHaveLength(1);
    expect(toastService.toasts()[0].tone).toBe('success');
    expect(resumeService.page()).toBeNull();
    httpMock.verify();
  });

  it('on 400: forwards the ProblemDetails to the form instead of toasting', () => {
    page.onCreate({ title: 'x'.repeat(501) });

    const req = httpMock.expectOne(`${env.apiBaseUrl}/resumes`);
    req.flush(
      { title: 'Validation failed', status: 400, errors: { title: ['title must be at most 500 characters long.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(toastService.toasts()).toHaveLength(0);
    expect(page.resumeForm().errorFor('title')).toBe('title must be at most 500 characters long.');
    httpMock.verify();
  });

  it('on a non-400 error: shows an error toast', () => {
    page.onCreate({ title: 'Staff Engineer' });

    const req = httpMock.expectOne(`${env.apiBaseUrl}/resumes`);
    req.flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });

    expect(toastService.toasts()).toHaveLength(1);
    expect(toastService.toasts()[0].tone).toBe('error');
    httpMock.verify();
  });

  it('cancels the in-flight request on destroy, so a late response can never toast or touch a torn-down view', () => {
    page.onCreate({ title: 'Staff Engineer' });
    const req = httpMock.expectOne(`${env.apiBaseUrl}/resumes`);

    fixture.destroy();

    // takeUntilDestroyed unsubscribes on destroy, which cancels the
    // underlying HTTP request — flushing a cancelled request throws, which
    // is itself proof the subscription (and the toast/resetForm it would
    // have triggered) never fires against a destroyed view.
    expect(() =>
      req.flush({ id: 'r-1', title: 'Staff Engineer' }, { status: 201, statusText: 'Created' }),
    ).toThrow(/cancelled/i);
    expect(toastService.toasts()).toHaveLength(0);
  });

  it('sets submitting() while the request is in flight and clears it after', () => {
    expect(page.submitting()).toBe(false);

    page.onCreate({ title: 'Staff Engineer' });
    expect(page.submitting()).toBe(true);

    const req = httpMock.expectOne(`${env.apiBaseUrl}/resumes`);
    req.flush({ id: 'r-1' }, { status: 201, statusText: 'Created' });

    expect(page.submitting()).toBe(false);
    httpMock.verify();
  });
});
