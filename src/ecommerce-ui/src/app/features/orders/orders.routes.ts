import { Routes } from '@angular/router';
import { OrderListComponent } from './pages/order-list/order-list';
import { OrderDetailComponent } from './pages/order-detail/order-detail';
import { authGuard } from '../../core/guards/auth.guard';

export const ORDER_ROUTES: Routes = [
  { path: '', component: OrderListComponent, canActivate: [authGuard] },
  { path: ':id', component: OrderDetailComponent, canActivate: [authGuard] },
];
