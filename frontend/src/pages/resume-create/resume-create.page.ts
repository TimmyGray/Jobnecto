import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { ResumeService } from '@entities/resume';
import type { CreateResumeCommand } from '@entities/resume';
import type { ProblemDetails } from '@shared/api';
import { PageHeaderComponent } from '@shared/ui';
import { ToastHostComponent, ToastService } from '@shared/ui';
import { ResumeFormComponent } from '@widgets/resume-form';

/**
 * `/resumes/new` — the résumé create form (Story 2.1 AC2, AC3).
 *
 * On a successful `201`, stays on this page (Decision Record: `/resumes`
 * still resolves to `ComingSoonStubPage` until Story 2.2 ships, so
 * auto-navigating there would dead-end a just-succeeded action) — shows a
 * success toast and resets {@link ResumeFormComponent} so another résumé can
 * be created immediately. On a `400`, forwards it to the form for inline
 * field errors; any other error shows an error toast.
 */
@Component({
  selector: 'page-resume-create',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, ResumeFormComponent, ToastHostComponent],
  templateUrl: './resume-create.page.html',
})
export class ResumeCreatePage {
  private readonly resumeService = inject(ResumeService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  /** True while the POST /resumes request is in flight (disables the form's submit button). */
  readonly submitting = signal(false);

  readonly resumeForm = viewChild.required(ResumeFormComponent);

  /** Handles the form's `(create)` event: POSTs, then toasts/resets or forwards server errors. */
  onCreate(command: CreateResumeCommand): void {
    this.submitting.set(true);
    this.resumeService
      .create(command)
      .pipe(
        finalize(() => this.submitting.set(false)),
        // If the user navigates away before the response arrives, the
        // subscription must not fire against a torn-down view — the toast
        // host is only mounted locally on this page (Decision Record), so a
        // toast queued after destroy would have nowhere to render anyway.
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toastService.show('Résumé created', 'success');
          this.resumeForm().resetForm();
        },
        error: (problem: ProblemDetails) => this.handleError(problem),
      });
  }

  private handleError(problem: ProblemDetails): void {
    if (problem.status === 400) {
      this.resumeForm().applyServerErrors(problem);
      return;
    }
    this.toastService.show(problem.detail ?? problem.title, 'error');
  }
}
