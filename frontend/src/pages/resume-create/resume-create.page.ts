import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { ResumeService } from '@entities/resume';
import type { CreateResumeCommand } from '@entities/resume';
import type { ProblemDetails } from '@shared/api';
import { PageHeaderComponent } from '@shared/ui';
import { ToastService } from '@shared/ui';
import { ResumeFormComponent } from '@widgets/resume-form';

/**
 * `/resumes/new` — the résumé create form.
 *
 * On a successful `201` the user lands on `/resumes`, where the new résumé
 * shows up: `ResumeService.create()` has already invalidated the list cache, so
 * the list fetches a fresh first page. The success toast is raised once the
 * navigation settles; the toast host lives in the app shell, so it survives this
 * page being torn down. History is not replaced, because going Back to a blank
 * create form is harmless. If the navigation is refused or rejects, the user
 * stays here with the toast and a reset form, ready to create another one.
 *
 * On a `400`, forwards it to the form for inline field errors; any other error
 * shows an error toast.
 */
@Component({
  selector: 'page-resume-create',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, ResumeFormComponent],
  templateUrl: './resume-create.page.html',
})
export class ResumeCreatePage {
  private readonly resumeService = inject(ResumeService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /**
   * True from submit until the create has finished: on success that is when the
   * navigation to the list settles, not when the response arrives, so the submit
   * button cannot send a second POST while the list route loads.
   */
  readonly submitting = signal(false);

  readonly resumeForm = viewChild.required(ResumeFormComponent);

  /** Handles the form's `(create)` event: POSTs, then goes to the list or forwards server errors. */
  onCreate(command: CreateResumeCommand): void {
    if (this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.resumeService
      .create(command)
      .pipe(
        // If the user navigates away before the response arrives, the late
        // response must neither toast nor navigate them somewhere else.
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.afterCreated(),
        error: (problem: ProblemDetails) => {
          this.submitting.set(false);
          this.handleError(problem);
        },
      });
  }

  private afterCreated(): void {
    this.router.navigate(['/resumes']).then(
      (navigated) => this.afterCreateNavigation(navigated),
      () => this.afterCreateNavigation(false),
    );
  }

  private afterCreateNavigation(navigated: boolean): void {
    this.toastService.show('Résumé created', 'success');
    if (!navigated) {
      this.submitting.set(false);
      this.resumeForm().resetForm();
    }
  }

  private handleError(problem: ProblemDetails): void {
    if (problem.status === 400) {
      this.resumeForm().applyServerErrors(problem);
      return;
    }
    this.toastService.show(problem.detail ?? problem.title, 'error');
  }
}
