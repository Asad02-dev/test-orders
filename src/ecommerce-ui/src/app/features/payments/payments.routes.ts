import { Routes } from '@angular/router';
import { PaymentListComponent } from './pages/payment-list/payment-list';
import { authGuard } from '../../core/guards/auth.guard';
import { adminGuard } from '../../core/guards/admin.guard';

export const PAYMENT_ROUTES: Routes = [
  { path: '', component: PaymentListComponent, canActivate: [authGuard, adminGuard] },
];
