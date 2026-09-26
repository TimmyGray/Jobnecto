import { ChangeDetectionStrategy, Component, ElementRef, signal, viewChild } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { A11yModule } from '@angular/cdk/a11y';
import { NAV_ITEMS } from './nav-items';

/**
 * Authenticated app shell (Story 1.5): fixed left sidebar on `lg/xl`,
 * top-bar + off-canvas drawer on `xs/sm`. Hosted as the parent route
 * component for every guarded route (`authGuard` sits on this route, not
 * each child) so the shell wraps any authenticated page automatically.
 * [AC1, AC3, AC4, UX-DR14, UX-DR16, UX-DR17]
 */
@Component({
  selector: 'widget-app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, A11yModule],
  template: `
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-50 focus:rounded-md focus:bg-inverse focus:px-4 focus:py-2 focus:text-text-inverse"
      >Skip to content</a
    >

    <div class="min-h-screen bg-canvas lg:flex">
      <nav
        aria-label="Primary"
        class="hidden lg:flex lg:w-60 lg:shrink-0 lg:flex-col lg:gap-1 lg:border-r lg:border-border-default lg:bg-surface lg:p-4"
      >
        @for (item of navItems; track item.path) {
          <a
            [routerLink]="item.path"
            routerLinkActive="text-text-primary font-semibold"
            class="rounded-md px-3 py-2 text-md text-text-secondary transition-colors duration-fast hover:bg-canvas"
            >{{ item.label }}</a
          >
        }
      </nav>

      <div class="flex items-center justify-between border-b border-border-default bg-surface px-4 py-3 lg:hidden">
        <span class="font-serif italic text-lg text-text-primary">Jobnecto</span>
        <button
          #drawerToggle
          type="button"
          data-testid="drawer-toggle"
          aria-label="Open navigation"
          [attr.aria-expanded]="drawerOpen()"
          class="rounded-md p-2 text-text-primary hover:bg-canvas"
          (click)="drawerOpen.set(true)"
        >
          ☰
        </button>
      </div>

      @if (drawerOpen()) {
        <div
          data-testid="drawer-backdrop"
          class="fixed inset-0 z-40 bg-inverse/40 lg:hidden"
          (click)="closeDrawer()"
        ></div>
        <nav
          aria-label="Primary"
          data-testid="drawer"
          cdkTrapFocus
          cdkTrapFocusAutoCapture
          (keydown.escape)="closeDrawer()"
          class="fixed inset-y-0 left-0 z-50 flex w-64 flex-col gap-1 bg-surface p-4 lg:hidden"
        >
          @for (item of navItems; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="text-text-primary font-semibold"
              class="rounded-md px-3 py-2 text-md text-text-secondary transition-colors duration-fast hover:bg-canvas"
              (click)="closeDrawer()"
              >{{ item.label }}</a
            >
          }
        </nav>
      }

      <main id="main-content" tabindex="-1" class="mx-auto max-w-[1040px] flex-1 px-4 py-8">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
})
export class AppShellComponent {
  protected readonly navItems = NAV_ITEMS;
  protected readonly drawerOpen = signal(false);
  private readonly drawerToggle = viewChild<ElementRef<HTMLButtonElement>>('drawerToggle');

  /** Closes the drawer and returns focus to the hamburger toggle that opened it. */
  protected closeDrawer(): void {
    this.drawerOpen.set(false);
    this.drawerToggle()?.nativeElement.focus();
  }
}
