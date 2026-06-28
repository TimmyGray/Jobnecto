import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { httpInterceptor } from '@shared/api';
import { routes } from './app.routes';

/**
 * Root application providers: router skeleton + HttpClient wired with the
 * Jobnecto HTTP interceptor (withCredentials + RFC 7807 normalization). [AC3]
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([httpInterceptor])),
  ],
};
