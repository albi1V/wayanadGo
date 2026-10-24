import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { authInterceptor, AuthService } from '../../services/auth.service';
import { HomePage } from './homePage';

describe('HomePage', () => {
  let fixture: ComponentFixture<HomePage>;
  let auth: AuthService;
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomePage);
    auth = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpTesting.verify();
    auth.clearSession();
  });

  it('keeps the guest hero and login action on initial load and after refresh', () => {
    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).toContain('FIND YOUR NEXT ADVENTURE');
    expect(page.textContent).toContain('Where to inWayanad today?');
    expect(page.querySelector('.welcome-message')).toBeNull();
    expect(page.querySelector('.profile-button')?.hasAttribute('hidden')).toBe(false);
    expect(page.querySelector('.logout-button')).toBeNull();

  });

  it('restores a refreshed session only after the API confirms the user profile', async () => {
    auth.clearSession();
    window.sessionStorage.setItem('wayanad-go.auth-session', JSON.stringify({
      accessToken: 'session-token',
      expiresAtUtc: Date.now() + 60_000,
    }));

    const restored = firstValueFrom(auth.restoreSession());
    const request = httpTesting.expectOne('http://localhost:5127/api/auth/me');
    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer session-token');
    expect(auth.isAuthenticated()).toBe(false);
    request.flush({
      fullName: 'Albin Varghese',
      email: 'albin@example.test',
      role: 'User',
    });
    await restored;
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    expect(auth.isAuthenticated()).toBe(true);
    expect(page.textContent).toContain('WELCOME BACK, ALBIN 👋');
  });

  it('clears a persisted session when the API rejects its token', async () => {
    auth.clearSession();
    window.sessionStorage.setItem('wayanad-go.auth-session', JSON.stringify({
      accessToken: 'invalid-token',
      expiresAtUtc: Date.now() + 60_000,
    }));

    const restored = firstValueFrom(auth.restoreSession());
    const request = httpTesting.expectOne('http://localhost:5127/api/auth/me');
    request.flush(null, { status: 401, statusText: 'Unauthorized' });
    await restored;
    fixture.detectChanges();

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.userFullName()).toBeNull();
    expect(window.sessionStorage.getItem('wayanad-go.auth-session')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.welcome-message')).toBeNull();
  });

  it('shows the authenticated first name and clears the session on logout', async () => {
    auth.setSession({
      accessToken: 'test-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      email: 'albin@example.test',
      role: 'User',
      fullName: 'Albin Varghese',
    });
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).toContain('WELCOME BACK, ALBIN 👋');
    expect(page.textContent).toContain('Where to inWayanad today?');
    expect(page.querySelector('.logout-button')).not.toBeNull();
    expect(page.querySelector('.profile-button')?.hasAttribute('hidden')).toBe(true);

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    page.querySelector<HTMLButtonElement>('.logout-button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(auth.getAccessToken()).toBeNull();
    expect(auth.userFullName()).toBeNull();
    expect(page.textContent).not.toContain('WELCOME BACK');
    expect(page.textContent).toContain('Where to inWayanad today?');
    expect(page.querySelector('.logout-button')).toBeNull();
    expect(page.querySelector('.profile-button')?.hasAttribute('hidden')).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith('/home');
  });

  it('loads the verified profile when the login response has no usable name', () => {
    auth.setSession({
      accessToken: 'test-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      email: 'albin@example.test',
      role: 'User',
      fullName: '   ',
    });
    const profileRequest = httpTesting.expectOne('http://localhost:5127/api/auth/me');
    profileRequest.flush({
      fullName: 'Albin Varghese',
      email: 'albin@example.test',
      role: 'User',
    });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('WELCOME BACK, ALBIN 👋');
  });

  it.each([null, '', '   '])('uses the authenticated fallback for name=%s', (fullName) => {
    auth.setSession({
      accessToken: 'test-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      email: 'user@example.test',
      role: 'User',
      fullName,
    });
    const profileRequest = httpTesting.expectOne('http://localhost:5127/api/auth/me');
    profileRequest.flush({ fullName, email: 'user@example.test', role: 'User' });
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).toContain('WELCOME BACK 👋');
    expect(page.querySelector('.logout-button')).not.toBeNull();
  });
});
