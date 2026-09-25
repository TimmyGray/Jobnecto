import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { UserService } from '@entities/user';

/**
 * Route guard for every route except `/sign-up` and `/sign-in` (AR13, FR6).
 *
 * Fast path: a profile is already cached (warm in-app navigation) — allow
 * immediately, no network call. Cold path: no cached profile (first load or
 * a page reload, since {@link UserService.profile} is in-memory only) —
 * attempt a silent session restore (FR3) via {@link UserService.restoreSession},
 * allowing on success and redirecting to `/sign-in` with the attempted
 * destination preserved as `returnUrl` on failure (FR5).
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const userService = inject(UserService);
  const router = inject(Router);

  if (userService.isAuthenticated()) {
    return true;
  }

  return userService.restoreSession().pipe(
    map(() => true),
    catchError(() =>
      of(router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } })),
    ),
  );
};
