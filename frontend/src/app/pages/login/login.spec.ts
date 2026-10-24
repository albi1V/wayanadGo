import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { AuthService } from '../../services/auth.service';
import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let httpTesting: HttpTestingController;
  let auth: AuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    await fixture.whenStable();
  });

  afterEach(() => {
    httpTesting.verify();
    auth.clearSession();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('sets the authenticated session and redirects after successful login', async () => {
    component.email = 'albin@example.test';
    component.password = 'correct-password';
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.onLogin();
    const request = httpTesting.expectOne('http://localhost:5127/api/auth/login');
    request.flush({
      accessToken: 'test-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      email: 'albin@example.test',
      role: 'User',
      fullName: 'Albin Varghese',
    });
    await fixture.whenStable();

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.userFullName()).toBe('Albin Varghese');
    expect(navigateSpy).toHaveBeenCalledWith(['/home']);
  });

  it('does not establish an authenticated session after a rejected login', async () => {
    component.email = 'albin@example.test';
    component.password = 'wrong-password';

    component.onLogin();
    const request = httpTesting.expectOne('http://localhost:5127/api/auth/login');
    request.flush(
      { message: 'Invalid email or password.' },
      { status: 401, statusText: 'Unauthorized' },
    );
    await fixture.whenStable();

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.userFullName()).toBeNull();
    expect(component.errorMessage).toBe('Invalid email or password.');
  });
});
