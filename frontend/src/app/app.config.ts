import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { httpInterceptor } from '@shared/api';
import { routes } from './app.routes';
import { authRedirectInterceptor } from './auth-redirect.interceptor';

/**
 * Root application providers: router skeleton + HttpClient wired with the
 * Jobnecto HTTP interceptors (withCredentials + RFC 7807 normalization, then
 * the 401 session-lapse redirect). [AC3; Story 1.4 AC3]
 *
 * Order matters: `authRedirectInterceptor` is registered first so it observes
 * the already-normalized `ProblemDetails` that `httpInterceptor` throws.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authRedirectInterceptor, httpInterceptor])),
  ],
};
