import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { UserService } from '@entities/user';

/**
 * Minimal authenticated landing stub. The full app shell (Story 1.5) and the
 * full dashboard (Story 1.6) are out of scope here — this only confirms the
 * post-registration landing and shows the hydrated profile. [AC7]
 */
@Component({
  selector: 'page-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="mx-auto max-w-3xl px-4 py-12">
      <p class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted">Dashboard</p>
      <h1 class="mb-4 text-xl font-semibold text-text-primary">
        @if (profile(); as user) {
          Welcome, <span class="font-serif italic text-brand-accent">{{ user.loginName }}</span>
        } @else {
          Welcome
        }
      </h1>
      @if (profile(); as user) {
        <p class="text-md text-text-secondary">
          You're signed in as {{ user.email }}.
        </p>
      } @else {
        <p class="text-md text-text-secondary">Your dashboard is being prepared.</p>
      }
    </main>
  `,
})
export class DashboardPage {
  private readonly userService = inject(UserService);
  /** The hydrated profile signal from the user entity service. */
  readonly profile = this.userService.profile;
}
