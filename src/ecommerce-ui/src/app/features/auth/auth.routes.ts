import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized';
import { CallbackComponent } from './pages/callback/callback';

export const AUTH_ROUTES: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'callback', component: CallbackComponent },
  { path: 'unauthorized', component: UnauthorizedComponent },
];
