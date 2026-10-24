
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { AuthService } from '../../services/auth.service';

type RegistrationStage = 'details' | 'otp';

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './registration.html',
  styleUrl: './registration.css',
})
export class Registration implements OnDestroy {
  stage = signal<RegistrationStage>('details');

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  otp = '';

  showPassword = false;
  showConfirmPassword = false;
  loading = signal(false);
  verified = signal(false);

  errorMessage = '';
  successMessage = '';

  otpSecondsRemaining = signal(300);
  resendSecondsRemaining = signal(0);

  private countdown?: Subscription;

  constructor(private auth: AuthService) {}

  requestOtp(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (
      !this.fullName.trim() ||
      !this.email.trim() ||
      !this.password ||
      !this.confirmPassword
    ) {
      this.errorMessage = 'Please complete all fields.';
      return;
    }

    if (this.fullName.trim().length < 2) {
      this.errorMessage = 'Enter your full name.';
      return;
    }

    // Validate the email address.
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

    this.loading.set(true);

    this.auth
      .requestOtp(
        this.fullName.trim(),
        this.email.trim(),
        this.password
      )
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.stage.set('otp');
          this.otp = '';
          this.otpSecondsRemaining.set(300);
          this.resendSecondsRemaining.set(60);

          this.successMessage =
            'If this email can be registered, a verification code will arrive shortly.';

          this.startCountdown();
        },

        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          this.errorMessage =
            error.error?.message ??
            'Could not send the OTP. Please try again.';
        },

        complete: () => {
          this.loading.set(false);
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

    this.loading.set(true);

    this.auth.verifyOtp(this.email.trim(), this.otp).subscribe({
      next: () => {
        this.loading.set(false);
        this.stopCountdown();
        this.verified.set(true);

        this.successMessage =
          'Email verified. You can now continue to the login page.';
      },

      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage =
          error.error?.message ?? 'OTP verification failed.';
      },

      complete: () => {
        this.loading.set(false);
      },
    });
  }

  resendOtp(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.loading.set(true);

    this.auth.resendOtp(this.email.trim()).subscribe({
      next: () => {
        this.loading.set(false);
        this.otp = '';
        this.otpSecondsRemaining.set(300);
        this.resendSecondsRemaining.set(60);

        this.successMessage =
          'If a registration is pending, check your email for a new code.';

        this.startCountdown();
      },

      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage =
          error.error?.message ?? 'Could not resend the OTP.';
      },

      complete: () => {
        this.loading.set(false);
      },
    });
  }

  backToDetails(): void {
    this.stage.set('details');
    this.otp = '';
    this.errorMessage = '';
    this.successMessage = '';
  }

  get formattedTime(): string {
    const remaining = this.otpSecondsRemaining();
    const minutes = Math.floor(remaining / 60);
    const seconds = remaining % 60;

    return `${minutes}:${String(seconds).padStart(2, '0')}`;
  }

  private startCountdown(): void {
    this.stopCountdown();

    this.countdown = interval(1000).subscribe(() => {
      if (this.otpSecondsRemaining() > 0) {
        this.otpSecondsRemaining.update((remaining) => remaining - 1);
      }

      if (this.resendSecondsRemaining() > 0) {
        this.resendSecondsRemaining.update((remaining) => remaining - 1);
      }

      if (
        this.otpSecondsRemaining() === 0 &&
        this.resendSecondsRemaining() === 0
      ) {
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
