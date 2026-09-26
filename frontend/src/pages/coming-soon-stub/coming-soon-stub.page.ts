import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmptyStateComponent, PageHeaderComponent } from '@shared/ui';

/**
 * Bare-bones stand-in for a guarded route whose real feature is still
 * backlog (Epics 2-6). One component serves every such route, reading its
 * title from static route `data` so no nav destination is a dead end
 * (`prd-demo-mvp.md` §Success Criteria item 6). This is deliberately not
 * the canonical `ComingSoonPlaceholder` (UX-DR9) — that dashed/hatched,
 * mono-pill treatment is Story 7.1's job and will replace this wholesale.
 * [AC5]
 */
@Component({
  selector: 'page-coming-soon-stub',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, EmptyStateComponent],
  template: `
    <ui-page-header [eyebrow]="title">{{ title }}</ui-page-header>
    <ui-empty-state
      icon="🚧"
      headline="Coming soon"
      [guidance]="title + ' is on its way — check back soon.'"
    />
  `,
})
export class ComingSoonStubPage {
  protected readonly title =
    (inject(ActivatedRoute).snapshot.data['title'] as string | undefined) ?? 'Coming soon';
}
