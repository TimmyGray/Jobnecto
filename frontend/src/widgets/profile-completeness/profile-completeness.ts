import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { UserProfile } from '@entities/user';
import { ErrorStateComponent, SkeletonComponent } from '@shared/ui';

/** One row of the onboarding checklist. */
export interface CompletenessItem {
  /** Human-readable action, phrased as a next step rather than a deficiency. */
  label: string;
  /** Whether the underlying profile field is filled. */
  done: boolean;
}

/** The six profile fields the checklist tracks, in display order. */
const TRACKED_FIELDS: readonly { label: string; field: keyof UserProfile }[] = [
  { label: 'Set login name', field: 'loginName' },
  // Presence only — there is no email-verification flow, so "verify" would overclaim.
  { label: 'Add email', field: 'email' },
  { label: 'Add phone', field: 'phone' },
  { label: 'Set location', field: 'location' },
  { label: 'Write about', field: 'about' },
  { label: 'Upload avatar', field: 'avatar' },
];

/**
 * Derives the onboarding checklist and its completion percentage from a profile.
 *
 * Exported so the dashboard can feed the same percentage to its count tile
 * without recomputing it from a second, divergent definition.
 * @param profile The hydrated profile, or null when it is not loaded yet.
 * @returns The six checklist items and the whole-number completion percentage.
 */
export function profileCompleteness(profile: UserProfile | null): {
  items: CompletenessItem[];
  percent: number;
} {
  const items = TRACKED_FIELDS.map(({ label, field }) => ({
    label,
    done: isFilled(profile?.[field]),
  }));
  const filled = items.filter((item) => item.done).length;
  return { items, percent: Math.round((filled / TRACKED_FIELDS.length) * 100) };
}

/** A field counts as filled only when it holds non-blank text. */
function isFilled(value: unknown): boolean {
  return typeof value === 'string' && value.trim().length > 0;
}

/**
 * Onboarding checklist showing how complete the user's profile is.
 *
 * Framed as orientation, not pressure: remaining items read as next steps,
 * carry no danger styling, and link straight to the profile page. Done-ness
 * is carried by shape and screen-reader text as well as colour.
 */
@Component({
  selector: 'widget-profile-completeness',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ErrorStateComponent, SkeletonComponent],
  template: `
    <section
      class="h-full rounded-lg border border-border-default bg-surface p-6"
      aria-labelledby="completeness-heading"
    >
      <p class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted">
        Onboarding checklist
      </p>
      <h2 id="completeness-heading" class="text-lg font-semibold text-text-primary">
        Finish your profile
      </h2>

      @if (failed()) {
        <ui-error-state
          headline="Couldn't load your profile"
          detail="The rest of your dashboard is still up to date."
          (retry)="retry.emit()"
        />
      } @else if (profile() === null) {
        <div class="mt-6">
          <ui-skeleton [lines]="7" />
        </div>
      } @else {
        <div class="mt-4 flex items-center gap-3">
          <div
            class="h-1 flex-1 overflow-hidden rounded-pill bg-canvas"
            role="progressbar"
            aria-label="Profile completeness"
            [attr.aria-valuenow]="percent()"
            aria-valuemin="0"
            aria-valuemax="100"
          >
            <div
              class="h-full rounded-pill bg-action-primary transition-[width] duration-standard"
              [style.width.%]="percent()"
            ></div>
          </div>
          <span class="text-sm font-medium text-text-secondary">{{ percent() }}%</span>
        </div>

        <ul class="mt-6 flex flex-col gap-4">
          @for (item of items(); track item.label) {
            <li data-testid="checklist-item" class="flex items-center gap-3 text-md">
              <span
                aria-hidden="true"
                [class.bg-action-primary]="item.done"
                [class.border-border-strong]="!item.done"
                class="h-4 w-4 shrink-0 rounded-pill border-2 border-transparent"
              ></span>
              @if (item.done) {
                <span class="text-text-muted line-through">{{ item.label }}</span>
                <span class="sr-only">done</span>
              } @else {
                <a
                  routerLink="/profile"
                  class="rounded-sm text-text-primary underline-offset-4 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
                  >{{ item.label }}</a
                >
                <span class="sr-only">not done</span>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class ProfileCompletenessComponent {
  /** The hydrated profile, or null while it is still unknown. */
  readonly profile = input<UserProfile | null>(null);
  /** Whether the profile load failed; renders this panel's own error state. */
  readonly failed = input(false);

  /** Emitted when the user asks to retry the failed profile load. */
  readonly retry = output<void>();

  /** The checklist rows derived from {@link profile}. */
  protected readonly items = computed(() => profileCompleteness(this.profile()).items);

  /** Whole-number completion percentage derived from {@link profile}. */
  protected readonly percent = computed(() => profileCompleteness(this.profile()).percent);
}
