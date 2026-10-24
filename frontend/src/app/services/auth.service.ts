import { inject, Injectable, isDevMode, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  email: string;
  role: string;
  fullName: string | null;
}

export interface ApiMessage {
  message: string;
}

interface CurrentUserResponse {
  fullName: string | null;
  email: string;
  role: string;
}

interface PersistedSession {
  accessToken: string;
  expiresAtUtc: number;
}

const sessionStorageKey = 'wayanad-go.auth-session';

const apiBaseUrl = () =>
  (isDevMode() ? environment.development.apiUrl : environment.production.apiUrl).replace(/\/+$/, '');

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).getAccessToken();
  return token && request.url.startsWith(`${apiBaseUrl()}/`)
    ? next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))
    : next(request);
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${apiBaseUrl()}/auth`;
  private accessToken: string | null = null;
  private expiresAt = 0;
  private expiryTimer: ReturnType<typeof setTimeout> | undefined;
  private restoration?: Observable<boolean>;

  readonly isAuthenticated = signal(false);
  readonly userFullName = signal<string | null>(null);

  constructor(private readonly http: HttpClient) {}

  requestOtp(fullName: string, email: string, password: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/request-otp`, { fullName, email, password });
  }

  verifyOtp(email: string, otp: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/verify-otp`, { email, otp });
  }

  resendOtp(email: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/resend-otp`, { email });
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password });
  }

  setSession(response: AuthResponse): void {
    this.clearSession();

    const expiresAt = Date.parse(response.expiresAtUtc);
    if (!response.accessToken || !Number.isFinite(expiresAt) || expiresAt <= Date.now()) return;

    this.accessToken = response.accessToken;
    this.expiresAt = expiresAt;
    this.userFullName.set(response.fullName?.trim() || null);
    this.isAuthenticated.set(true);
    this.persistSession();
    this.scheduleExpiration();
    if (isDevMode()) {
      console.debug('Login response contains a non-empty fullName:', Boolean(response.fullName?.trim()));
    }
    if (!this.userFullName()) this.fetchAuthenticatedProfile().subscribe();
  }

  restoreSession(): Observable<boolean> {
    if (this.isAuthenticated()) return of(true);
    if (this.restoration) return this.restoration;

    const persisted = this.readPersistedSession();
    if (!persisted) return of(false);
    if (!persisted.accessToken || !Number.isFinite(persisted.expiresAtUtc) || persisted.expiresAtUtc <= Date.now()) {
      this.clearSession();
      return of(false);
    }

    // The stored name is deliberately not used. Ask the API to validate the
    // bearer token and return the name associated with its authenticated user.
    this.accessToken = persisted.accessToken;
    this.expiresAt = persisted.expiresAtUtc;

    const request = this.fetchAuthenticatedProfile().pipe(
      finalize(() => (this.restoration = undefined)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.restoration = request;
    return request;
  }

  private fetchAuthenticatedProfile(): Observable<boolean> {
    return this.http.get<CurrentUserResponse>(`${this.apiUrl}/me`).pipe(
      tap((profile) => {
        this.userFullName.set(profile.fullName?.trim() || null);
        this.isAuthenticated.set(true);
        this.scheduleExpiration();
      }),
      map(() => true),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401) this.clearSession();
        return of(false);
      }),
    );
  }

  getAccessToken(): string | null {
    if (this.accessToken && (!Number.isFinite(this.expiresAt) || this.expiresAt <= Date.now())) this.clearSession();
    return this.accessToken;
  }

  clearSession(): void {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.expiryTimer = undefined;
    this.accessToken = null;
    this.expiresAt = 0;
    this.userFullName.set(null);
    this.isAuthenticated.set(false);
    try {
      this.getSessionStorage()?.removeItem(sessionStorageKey);
    } catch {
      // Clearing the in-memory token and profile still takes effect.
    }
  }

  private persistSession(): void {
    if (!this.accessToken) return;

    try {
      this.getSessionStorage()?.setItem(sessionStorageKey, JSON.stringify({
        accessToken: this.accessToken,
        expiresAtUtc: this.expiresAt,
      } satisfies PersistedSession));
    } catch {
      // Storage may be disabled; the in-memory session still works until refresh.
    }
  }

  private readPersistedSession(): PersistedSession | null {
    try {
      const value = this.getSessionStorage()?.getItem(sessionStorageKey);
      if (!value) return null;
      const parsed: unknown = JSON.parse(value);
      if (
        typeof parsed === 'object' && parsed !== null &&
        'accessToken' in parsed && typeof parsed.accessToken === 'string' &&
        'expiresAtUtc' in parsed && typeof parsed.expiresAtUtc === 'number'
      ) {
        return parsed as PersistedSession;
      }
      this.clearSession();
    } catch {
      this.clearSession();
    }
    return null;
  }

  private getSessionStorage(): Storage | null {
    try {
      return typeof window === 'undefined' ? null : window.sessionStorage;
    } catch {
      return null;
    }
  }

  private scheduleExpiration(): void {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.expiryTimer = setTimeout(() => this.clearSession(), this.expiresAt - Date.now());
  }
}
