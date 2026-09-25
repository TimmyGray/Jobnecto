import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails, SKIP_AUTH_REDIRECT } from '@shared/api';
import { UserService } from '@entities/user';

/**
 * App-layer interceptor (AR12): reacts to a mid-task session lapse.
 *
 * Registered *outside* {@link httpInterceptor} in `app.config.ts` so it
 * observes the already-normalized {@link ProblemDetails} that interceptor
 * throws. On any `401` not flagged with {@link SKIP_AUTH_REDIRECT}, it drops
 * the cached profile and routes to `/sign-in`, preserving the in-flight
 * destination as a `returnUrl` query param (FR5, AC3). Deliberately kept out
 * of `shared/api` — feature-sliced layering (AR11) puts `UserService` (an
 * entity) above `shared`, so this entity-aware side effect belongs at the
 * `app` layer, not inside the pure HTTP-normalization interceptor.
 */
export const authRedirectInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const userService = inject(UserService);

  return next(req).pipe(
    catchError((error: unknown) => {
      const status = error instanceof HttpErrorResponse ? error.status : (error as ProblemDetails)?.status;
      // Also skip when already on/heading to `/sign-in`: without this, a
      // second concurrent 401 (or any 401 while genuinely on that page)
      // would recompute `returnUrl` from `router.url` *after* the first
      // redirect already landed there, clobbering the real destination with
      // `returnUrl=/sign-in`.
      if (status === 401 && !req.context.get(SKIP_AUTH_REDIRECT) && !router.url.startsWith('/sign-in')) {
        userService.invalidate();
        void router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};
