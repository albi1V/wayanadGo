import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Registration } from './registration';

describe('Registration', () => {
  let component: Registration;
  let fixture: ComponentFixture<Registration>;
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Registration],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Registration);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpTesting.verify());

  function fillRegistrationDetails(): void {
    component.fullName = 'Wayanad Tester';
    component.email = 'wayanad-tester@example.test';
    component.password = 'correct-horse-battery';
    component.confirmPassword = component.password;
  }

  function submitDetailsForm(): void {
    const form = fixture.nativeElement.querySelector(
      'form.registration-form',
    ) as HTMLFormElement;
    form.dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
  }

  it('renders the OTP step after a successful request and clears loading', async () => {
    fillRegistrationDetails();
    submitDetailsForm();

    const request = httpTesting.expectOne(
      'http://localhost:5127/api/auth/request-otp',
    );
    expect(request.request.method).toBe('POST');

    request.flush({
      message: 'If this email can be registered, a verification code has been sent.',
    });
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(component.stage()).toBe('otp');
    expect(component.loading()).toBe(false);
    expect(page.querySelector('#otp')).not.toBeNull();
    expect(page.querySelector('#fullName')).toBeNull();
    expect(page.textContent).toContain('STEP 2 OF 2');
    expect(page.textContent).not.toContain('Please wait...');
  });

  it('shows the API error and clears loading when the OTP request fails', async () => {
    fillRegistrationDetails();
    submitDetailsForm();

    const request = httpTesting.expectOne(
      'http://localhost:5127/api/auth/request-otp',
    );
    request.flush(
      { message: 'Email delivery is temporarily unavailable.' },
      { status: 503, statusText: 'Service Unavailable' },
    );
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(component.stage()).toBe('details');
    expect(component.loading()).toBe(false);
    expect(page.querySelector('#fullName')).not.toBeNull();
    expect(page.textContent).toContain(
      'Email delivery is temporarily unavailable.',
    );
    expect(page.textContent).not.toContain('Please wait...');
  });
});
