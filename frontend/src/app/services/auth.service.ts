import { inject, Injectable, isDevMode } from '@angular/core';
import { HttpClient, HttpInterceptorFn } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  email: string;
  role: string;
}

export interface ApiMessage {
  message: string;
}

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
    this.accessToken = response.accessToken;
    this.expiresAt = Date.parse(response.expiresAtUtc);
  }

  getAccessToken(): string | null {
    if (this.accessToken && (!Number.isFinite(this.expiresAt) || this.expiresAt <= Date.now())) this.clearSession();
    return this.accessToken;
  }

  clearSession(): void {
    this.accessToken = null;
    this.expiresAt = 0;
  }
}
