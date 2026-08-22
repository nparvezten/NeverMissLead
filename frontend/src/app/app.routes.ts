import { Routes } from '@angular/router';
import { LandingDemoComponent } from './landing/landing-demo.component';
import { LoginComponent } from './dashboard/login/login.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { authGuard } from './dashboard/guards/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingDemoComponent, data: { vertical: 'tutoring' } },
  { path: 'demo/tutoring', component: LandingDemoComponent, data: { vertical: 'tutoring' } },
  { path: 'demo/dental', component: LandingDemoComponent, data: { vertical: 'dental' } },
  { path: 'demo/realty', component: LandingDemoComponent, data: { vertical: 'realty' } },
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
