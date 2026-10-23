import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { environment } from '../../../environments/environment';

type RegistrationStage = 'details' | 'otp';

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './registration.html',
  styleUrl: './registration.css',
})
export class Registration implements OnDestroy {
  stage: RegistrationStage = 'details';

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  otp = '';

  showPassword = false;
  showConfirmPassword = false;
  loading = false;

  errorMessage = '';
  successMessage = '';

  otpSecondsRemaining = 300;
  private countdown?: Subscription;

  private readonly apiUrl = (
    environment.development.apiUrl
  ).replace(/\/+$/, '');

  constructor(private http: HttpClient) {}

  requestOtp(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.fullName.trim() || !this.email.trim() ||
        !this.password || !this.confirmPassword) {
      this.errorMessage = 'Please complete all fields.';
      return;
    }

    if (this.fullName.trim().length < 2) {
      this.errorMessage = 'Enter your full name.';
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email.trim())) {
      this.errorMessage = 'Enter a valid email address.';
      return;
    }

    if (this.password.length < 8) {
      this.errorMessage = 'Password must contain at least 8 characters.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.loading = true;

    // Planned API contract. The backend must validate all fields,
    // securely store pending registration data, and send the OTP.
    this.http.post(`${this.apiUrl}/auth/request-otp`, {
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      password: this.password,
    }).subscribe({
      next: () => {
        this.loading = false;
        this.stage = 'otp';
        this.otp = '';
        this.otpSecondsRemaining = 300;
        this.successMessage = 'OTP sent. Please check your email.';
        this.startCountdown();
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage =
          error.error?.message ??
          'Could not send the OTP. Please try again.';
      },
    });
  }

  verifyOtp(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!/^\d{6}$/.test(this.otp)) {
      this.errorMessage = 'Enter the six-digit OTP.';
      return;
    }

    this.loading = true;

    this.http.post(`${this.apiUrl}/auth/verify-otp`, {
      email: this.email.trim(),
      otp: this.otp,
    }).subscribe({
      next: () => {
        this.loading = false;
        this.stopCountdown();
        this.successMessage =
          'Email verified. We can now continue to the login page.';
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage =
          error.error?.message ?? 'OTP verification failed.';
      },
    });
  }

  resendOtp(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.loading = true;

    this.http.post(`${this.apiUrl}/auth/resend-otp`, {
      email: this.email.trim(),
    }).subscribe({
      next: () => {
        this.loading = false;
        this.otp = '';
        this.otpSecondsRemaining = 300;
        this.successMessage = 'A new OTP has been requested.';
        this.startCountdown();
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage =
          error.error?.message ?? 'Could not resend the OTP.';
      },
    });
  }

  backToDetails(): void {
    this.stopCountdown();
    this.stage = 'details';
    this.otp = '';
    this.errorMessage = '';
    this.successMessage = '';
  }

  get formattedTime(): string {
    const minutes = Math.floor(this.otpSecondsRemaining / 60);
    const seconds = this.otpSecondsRemaining % 60;
    return `${minutes}:${String(seconds).padStart(2, '0')}`;
  }

  private startCountdown(): void {
    this.stopCountdown();

    this.countdown = interval(1000).subscribe(() => {
      if (this.otpSecondsRemaining > 0) {
        this.otpSecondsRemaining--;
      } else {
        this.stopCountdown();
      }
    });
  }

  private stopCountdown(): void {
    this.countdown?.unsubscribe();
    this.countdown = undefined;
  }

  ngOnDestroy(): void {
    this.stopCountdown();
  }
}
