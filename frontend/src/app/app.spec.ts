import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { App } from './app';
import { routes } from './app.routes';
import { LandingPage } from './pages/landingPage/landingPage';

describe('App navigation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes)],
    }).compileComponents();
  });

  it('opens the landing page at the root route', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.landing-page')).toBeTruthy();
  });

  it('navigates from landing to home and back', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.swipe-label') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.home-page')).toBeTruthy();

    (fixture.nativeElement.querySelector('.home-brand') as HTMLAnchorElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.landing-page')).toBeTruthy();
  });

  it('opens home after an upward swipe on the landing page', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();

    const landingPage = fixture.debugElement.query(By.directive(LandingPage))
      .componentInstance as LandingPage;
    landingPage.onTouchStart({ changedTouches: [{ screenY: 200 }] } as unknown as TouchEvent);
    landingPage.onTouchEnd({ changedTouches: [{ screenY: 100 }] } as unknown as TouchEvent);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.home-page')).toBeTruthy();
  });
});
