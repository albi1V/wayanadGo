import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  email = '';
  password = '';
  showPassword = false;
  errorMessage = '';
  successMessage = '';
  loading = false;

  constructor(private auth: AuthService, private router: Router) {}

  onLogin(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.email.trim() || !this.password) {
      this.errorMessage = 'Please enter your email and password.';
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email.trim())) {
      this.errorMessage = 'Please enter a valid email address.';
      return;
    }

    this.loading = true;
    this.auth.login(this.email.trim(), this.password).subscribe({
      next: (response) => {
        this.loading = false;
        this.auth.setSession(response);
        void this.router.navigate(['/home']);
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage = error.error?.message ?? 'Sign in failed. Please try again.';
      },
    });
  }
}
