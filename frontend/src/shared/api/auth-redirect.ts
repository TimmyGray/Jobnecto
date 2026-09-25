import { HttpContextToken } from '@angular/common/http';

/**
 * Set on a request to suppress the HTTP interceptor's automatic 401 → `/sign-in`
 * redirect (see {@link httpInterceptor}). Used for calls that are expected to
 * fail with `401` as a normal, non-session-lapse outcome — sign-in itself
 * (wrong credentials) and the guard's own cold-load session-restore probe
 * (`token/refresh` / `users/me` for a genuinely anonymous visitor) — so those
 * callers can render their own error/redirect without racing the interceptor.
 */
export const SKIP_AUTH_REDIRECT = new HttpContextToken<boolean>(() => false);
