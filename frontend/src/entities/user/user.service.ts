import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, switchMap, tap } from 'rxjs';
import { SKIP_AUTH_REDIRECT } from '@shared/api/auth-redirect';
import {
  CreateUserCommand,
  CreateUserResult,
  GetCurrentUserResult,
  RefreshAccessTokenResult,
  SignInCommand,
  SignInResult,
  UserProfile,
} from './model';

/** Context shared by calls whose own 401 is not a session lapse (see {@link SKIP_AUTH_REDIRECT}). */
const SKIP_REDIRECT_CONTEXT = new HttpContext().set(SKIP_AUTH_REDIRECT, true);

/**
 * Signal-backed user entity service (Decision 1.2/1.3: Angular Signals +
 * injectable services, thin cache — no NgRx, no TanStack).
 *
 * Holds the hydrated authenticated profile in a signal and exposes the API
 * calls used by the sign-up flow:
 *  - {@link register} → `POST /api/v1/users`
 *  - {@link signIn} → `POST /api/v1/users/sessions`
 *  - {@link fetchCurrentUser} → `GET /api/v1/users/me` (hydration after 201/200)
 *
 * URLs are relative; the HTTP interceptor prefixes the API base and sets
 * `withCredentials` so the auth cookie is carried. [AC3, AC7]
 */
@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  /** Backing signal for the current authenticated profile (null when anonymous). */
  private readonly profileSignal = signal<UserProfile | null>(null);

  /** Read-only view of the current profile. */
  readonly profile = this.profileSignal.asReadonly();

  /** Whether a hydrated profile is present. */
  readonly isAuthenticated = computed(() => this.profileSignal() !== null);

  /**
   * Registers a new account. On `201` the server sets the HTTP-only auth cookie.
   * @param input The registration payload `{ loginName, email, password }`.
   * @returns The created user projection.
   */
  register(input: CreateUserCommand): Observable<CreateUserResult> {
    return this.http.post<CreateUserResult>('/users', input);
  }

  /**
   * Signs in a returning user with their credentials. On `200` the server sets
   * the HTTP-only auth cookie. Does not write the profile signal — callers
   * hydrate it separately via {@link fetchCurrentUser}.
   * @param input The sign-in payload `{ identifier, password }`.
   * @returns The sign-in result.
   */
  signIn(input: SignInCommand): Observable<SignInResult> {
    // A 401 here is a wrong-credential response (Story 1.3 Trap 3), not a
    // session lapse — the sign-in page owns rendering it, so it must not
    // race the app-layer 401 redirect.
    return this.http.post<SignInResult>('/users/sessions', input, { context: SKIP_REDIRECT_CONTEXT });
  }

  /**
   * Hydrates the current authenticated profile via `GET /api/v1/users/me`
   * and caches it in the signal.
   *
   * `skipAuthRedirect` defaults to `false` (a 401 here participates in the
   * app-layer redirect, the correct behavior for a page re-fetching the
   * profile mid-session). Pass `true` only right after register/sign-in,
   * where a failed hydration must never itself surface as an error or a
   * redirect (Story 1.3 Trap 2 / this story's AC1) — the caller decides what
   * that hydration failure means, not the interceptor.
   * @param skipAuthRedirect Whether to suppress the 401 session-lapse redirect for this call.
   * @returns The current user profile.
   */
  fetchCurrentUser(skipAuthRedirect = false): Observable<GetCurrentUserResult> {
    return this.http
      .get<GetCurrentUserResult>('/users/me', skipAuthRedirect ? { context: SKIP_REDIRECT_CONTEXT } : {})
      .pipe(tap((profile) => this.profileSignal.set(profile)));
  }

  /**
   * Silently renews the current session via `POST /api/v1/users/token/refresh`
   * without requiring the user to re-enter credentials (FR3). Requires an
   * already-valid cookie; `401` means the cookie is missing/expired.
   * @returns The renewed access-token contract (browser transport ignores its body).
   */
  refreshToken(): Observable<RefreshAccessTokenResult> {
    return this.http.post<RefreshAccessTokenResult>(
      '/users/token/refresh',
      null,
      { context: SKIP_REDIRECT_CONTEXT },
    );
  }

  /**
   * Cold-load session restore: renews the session (confirming a still-valid
   * cookie survived a page reload, since {@link profile} is in-memory only)
   * then hydrates the profile. Used by {@link authGuard}. A `401` from the
   * refresh call propagates as an error and `/users/me` is never called.
   * @returns The hydrated current user profile.
   */
  restoreSession(): Observable<GetCurrentUserResult> {
    return this.refreshToken().pipe(switchMap(() => this.fetchCurrentUser(true)));
  }

  /** Clears the cached profile (used on sign-out / invalidation). */
  invalidate(): void {
    this.profileSignal.set(null);
  }
}
