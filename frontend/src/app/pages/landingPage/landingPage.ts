
import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landingPage.html',
  styleUrl: './landingPage.css'
})
export class LandingPage {
  private touchStartY = 0;

  constructor(private readonly router: Router) {}

  onTouchStart(event: TouchEvent): void {
    this.touchStartY = event.changedTouches[0]?.screenY ?? 0;
  }

  onTouchEnd(event: TouchEvent): void {
    const endY = event.changedTouches[0]?.screenY ?? this.touchStartY;
    if (this.touchStartY - endY > 60) {
      void this.router.navigateByUrl('/home');
    }
  }
}
