import { Routes } from '@angular/router';
import { HomePage } from './pages/homePage/homePage';
import { LandingPage } from './pages/landingPage/landingPage';

export const routes: Routes = [
  { path: '', component: LandingPage, pathMatch: 'full' },
  { path: 'home', component: HomePage },
  { path: '**', redirectTo: '' },
];
