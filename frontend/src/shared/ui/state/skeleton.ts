import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Layout-preserving skeleton placeholder for first-load (NFR2, no layout shift).
 * [AC5; FE Guide §8.1]
 *
 * Render N lines of a given height/width. The shimmer animation is disabled
 * under `prefers-reduced-motion` via the global reduced-motion rule.
 */
@Component({
  selector: 'ui-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-2" aria-hidden="true">
      @for (line of lineArray(); track $index) {
        <div
          class="animate-pulse rounded-md bg-border-default"
          [style.height]="height()"
          [style.width]="$last ? lastWidth() : width()"
        ></div>
      }
    </div>
  `,
})
export class SkeletonComponent {
  /** Number of placeholder lines. */
  readonly lines = input(3);
  /** Height of each line (CSS length). */
  readonly height = input('16px');
  /** Width of full-width lines (CSS length). */
  readonly width = input('100%');
  /** Width of the final line (typically shorter, for a natural look). */
  readonly lastWidth = input('60%');

  /** Iterable used by the template `@for`. */
  protected readonly lineArray = computed(() =>
    Array.from({ length: Math.max(0, this.lines()) }),
  );
}
