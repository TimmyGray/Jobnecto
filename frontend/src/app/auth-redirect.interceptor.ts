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
  // Captured before the request goes out, not inside `catchError`: by the
  // time an async response/error arrives, an unrelated navigation already in
  // flight (e.g. the user clicked a link right as a stale request from the
  // previous page finally 401s) could have already moved `router.url` on —
  // recomputing it at catch-time would attribute `returnUrl` to the wrong
  // route. This is the route that actually issued the failing request.
  const attemptedUrl = router.url;

  return next(req).pipe(
    catchError((error: unknown) => {
      const status = error instanceof HttpErrorResponse ? error.status : (error as ProblemDetails)?.status;
      // Also skip when already on/heading to `/sign-in`: without this, a
      // second concurrent 401 (or any 401 while genuinely on that page)
      // would clobber the real destination with `returnUrl=/sign-in`.
      if (status === 401 && !req.context.get(SKIP_AUTH_REDIRECT) && !attemptedUrl.startsWith('/sign-in')) {
        userService.invalidate();
        void router.navigate(['/sign-in'], { queryParams: { returnUrl: attemptedUrl } });
      }
      return throwError(() => error);
    }),
  );
};
