import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  CreateUserCommand,
  CreateUserResult,
  GetCurrentUserResult,
  UserProfile,
} from './model';

/**
 * Signal-backed user entity service (Decision 1.2/1.3: Angular Signals +
 * injectable services, thin cache — no NgRx, no TanStack).
 *
 * Holds the hydrated authenticated profile in a signal and exposes the API
 * calls used by the sign-up flow:
 *  - {@link register} → `POST /api/v1/users`
 *  - {@link fetchCurrentUser} → `GET /api/v1/users/me` (hydration after 201)
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
   * Hydrates the current authenticated profile via `GET /api/v1/users/me`
   * (requires the cookie set during registration) and caches it in the signal.
   * @returns The current user profile.
   */
  fetchCurrentUser(): Observable<GetCurrentUserResult> {
    return this.http
      .get<GetCurrentUserResult>('/users/me')
      .pipe(tap((profile) => this.profileSignal.set(profile)));
  }

  /** Clears the cached profile (used on sign-out / invalidation). */
  invalidate(): void {
    this.profileSignal.set(null);
  }
}
