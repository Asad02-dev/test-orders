import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard';
import { authGuard } from '../../core/guards/auth.guard';

export const DASHBOARD_ROUTES: Routes = [
  { path: '', component: DashboardComponent, canActivate: [authGuard] },
];
