
import { Routes } from '@angular/router';

import { HomePage } from './pages/homePage/homePage';
import { LandingPage } from './pages/landingPage/landingPage';
import { Login } from './pages/login/login';
import { Registration } from './pages/registration/registration';

export const routes: Routes = [
  { path: '', component: LandingPage, pathMatch: 'full' },
  { path: 'home', component: HomePage },
  { path: 'login', component: Login },
  { path: 'register', component: Registration },
  { path: '**', redirectTo: '' }
];
