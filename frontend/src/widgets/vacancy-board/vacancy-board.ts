import { ChangeDetectionStrategy, Component, ElementRef, inject, input } from '@angular/core';
import type { VacancyListItem } from '@entities/vacancy';
import { VacancyCardComponent } from './vacancy-card';

/**
 * Presentational grid of vacancy cards: one column on small screens, two from
 * `md`, three from `xl`, never a table (UX-DR16).
 *
 * It owns no fetching or pagination — the page hands it the accumulated items
 * in server order.
 */
@Component({
  selector: 'widget-vacancy-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VacancyCardComponent],
  template: `
    <ul class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
      @for (vacancy of items(); track vacancy.id || $index) {
        <li class="min-w-0">
          <widget-vacancy-card [vacancy]="vacancy" />
        </li>
      }
    </ul>
  `,
})
export class VacancyBoardComponent {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** The vacancies to render, already ordered by the server and accumulated by the page. */
  readonly items = input<VacancyListItem[]>([]);

  /**
   * Moves keyboard focus to the card at {@link index}: its first action link,
   * or the card itself when it has none. The page calls this after a load-more
   * so focus is not dropped when the button that was just activated disappears.
   * @param index Zero-based position in {@link items}.
   */
  focusCard(index: number): void {
    const card = this.host.nativeElement
      .querySelectorAll<HTMLElement>(':scope > ul > li article')
      .item(index);
    (card?.querySelector<HTMLElement>('a') ?? card)?.focus();
  }
}
