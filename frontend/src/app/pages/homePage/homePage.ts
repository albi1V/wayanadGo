import { Component, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './homePage.html',
  styleUrl: './homePage.css',
})
export class HomePage implements OnInit {
  readonly firstName = computed(() => {
    const fullName = this.auth.userFullName()?.trim();
    return fullName?.split(/\s+/)[0] || null;
  });

  constructor(
    readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.auth.restoreSession().subscribe();
  }

  logout(): void {
    this.auth.clearSession();
    void this.router.navigateByUrl('/home');
  }
}
