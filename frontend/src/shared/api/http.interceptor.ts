import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { env } from '@shared/config';
import { normalizeProblemDetails, ProblemDetails } from './problem-details';

/** Absolute-URL detector (http://, https://, or protocol-relative //). */
const ABSOLUTE_URL = /^(https?:)?\/\//i;

/**
 * Functional HTTP interceptor for the Jobnecto client.
 *
 * Responsibilities (AC3):
 *  - Sets `withCredentials: true` on EVERY outgoing request so the HTTP-only
 *    auth cookie is sent/received (cookie transport is canonical — Decision 1.1).
 *  - Prefixes relative request URLs (those starting with `/`) with the configured
 *    API base URL from shared/config, so feature services call `/users` etc.
 *  - Normalizes any error response (RFC 7807 or otherwise) into a typed
 *    {@link ProblemDetails} and rethrows it, so callers never string-match raw
 *    bodies or surface raw status codes. [AC8, AC9]
 */
export const httpInterceptor: HttpInterceptorFn = (req, next) => {
  const url = ABSOLUTE_URL.test(req.url) ? req.url : `${env.apiBaseUrl}${req.url}`;

  const authReq = req.clone({
    url,
    withCredentials: true,
  });

  return next(authReq).pipe(
    catchError((error: unknown) => {
      const problem = toProblemDetails(error);
      return throwError(() => problem);
    }),
  );
};

/** Converts an HttpErrorResponse (or any thrown value) into a typed ProblemDetails. */
function toProblemDetails(error: unknown): ProblemDetails {
  if (error instanceof HttpErrorResponse) {
    return normalizeProblemDetails(error.error, error.status);
  }
  return normalizeProblemDetails(null, 0);
}
