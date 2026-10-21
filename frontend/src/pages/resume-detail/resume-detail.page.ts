import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription, finalize } from 'rxjs';
import { ResumeService } from '@entities/resume';
import type { ResumeResult, UpdateResumeCommand } from '@entities/resume';
import { mapProblemToUxState } from '@shared/api';
import type { ProblemDetails } from '@shared/api';
import {
  ErrorStateComponent,
  ForbiddenStateComponent,
  NotFoundStateComponent,
  PageHeaderComponent,
  SkeletonComponent,
  ToastHostComponent,
  ToastService,
} from '@shared/ui';
import { ResumeFormComponent } from '@widgets/resume-form';

/** Where the page stands: loading, showing the form, or one of the recovery states. */
type DetailStatus = 'loading' | 'ready' | 'not-found' | 'forbidden' | 'error';

/**
 * `/resumes/:id` — view and edit one résumé.
 *
 * Reads the id from the route (and reloads when it changes, since the router
 * reuses this component between ids), shows the résumé in the shared form in
 * edit mode, and PATCHes only the fields the user changed. The server's answer
 * becomes the form's new baseline, so a second save diffs against what was
 * just saved.
 *
 * Ownership is entirely server-side: a read of someone else's résumé is a
 * `404` (existence is not disclosed) and a write is a `403`; both render a
 * recovery state and never any of the résumé's data.
 */
@Component({
  selector: 'page-resume-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    ResumeFormComponent,
    SkeletonComponent,
    NotFoundStateComponent,
    ForbiddenStateComponent,
    ErrorStateComponent,
    ToastHostComponent,
  ],
  templateUrl: './resume-detail.page.html',
})
export class ResumeDetailPage {
  private readonly resumeService = inject(ResumeService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  private readonly form = viewChild(ResumeFormComponent);
  private readonly loadingRegion = viewChild<ElementRef<HTMLElement>>('loadingRegion');
  private loadSubscription: Subscription | undefined;
  private saveSubscription: Subscription | undefined;
  private id = '';

  /** Which of the page's views is showing. */
  readonly status = signal<DetailStatus>('loading');
  /** The résumé as last loaded or saved; the form's baseline. */
  readonly resume = signal<ResumeResult | null>(null);
  /** True while a save request is in flight. */
  readonly saving = signal(false);

  /** The heading: the résumé's title once loaded, with a placeholder for a blank one. */
  protected readonly heading = computed(() => {
    const resume = this.resume();
    if (this.status() !== 'ready' || !resume) {
      return 'Résumé';
    }
    return resume.title?.trim() || 'Untitled résumé';
  });

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.id = params.get('id') ?? '';
      this.load();
    });
  }

  /**
   * Reloads the current résumé after a failed load. The Retry button is
   * removed by the state change, so focus moves to the loading region instead of
   * dropping to <body>.
   */
  retry(): void {
    this.load();
    afterNextRender(
      () => {
        const active = document.activeElement;
        if (!active || active === document.body) {
          this.loadingRegion()?.nativeElement.focus();
        }
      },
      { injector: this.injector },
    );
  }

  /** Returns to the résumé list (the recovery action of the not-found and forbidden states). */
  goBack(): void {
    void this.router.navigate(['/resumes']);
  }

  /**
   * Saves the changed fields. On success the response becomes the new baseline;
   * a `400` is mapped onto the form's fields; a `403`/`404` means the résumé is
   * no longer the user's to edit, so the form gives way to a recovery state;
   * anything else toasts and keeps the user's edits.
   * @param command Only the fields that changed.
   */
  onUpdate(command: UpdateResumeCommand): void {
    if (this.saving()) {
      return;
    }
    this.saving.set(true);
    this.saveSubscription = this.resumeService
      .update(this.id, command)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (saved) => {
          this.resume.set(saved);
          this.toastService.show('Résumé saved', 'success');
        },
        error: (problem: ProblemDetails) => this.handleSaveError(problem),
      });
  }

  /**
   * Loads the résumé for the current id. Cancels any load still in flight and
   * any save issued for the previous id, so a late response can never be applied
   * to the résumé now showing.
   */
  private load(): void {
    this.loadSubscription?.unsubscribe();
    this.saveSubscription?.unsubscribe();
    this.saving.set(false);
    this.resume.set(null);
    this.status.set('loading');
    this.loadSubscription = this.resumeService
      .get(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resume) => {
          this.resume.set(resume);
          this.status.set('ready');
        },
        error: (problem: ProblemDetails) => this.status.set(mapProblemToUxState(problem)),
      });
  }

  private handleSaveError(problem: ProblemDetails): void {
    if (problem.status === 400) {
      this.form()?.applyServerErrors(problem);
      return;
    }
    const state = mapProblemToUxState(problem);
    if (state === 'error') {
      this.toastService.show(problem.detail ?? problem.title, 'error');
      return;
    }
    this.resume.set(null);
    this.status.set(state);
  }
}
