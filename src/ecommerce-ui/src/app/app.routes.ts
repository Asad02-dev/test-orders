import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { AUTH_ROUTES } from './features/auth/auth.routes';
import { DASHBOARD_ROUTES } from './features/dashboard/dashboard.routes';
import { CATALOG_ROUTES } from './features/catalog/catalog.routes';
import { CART_ROUTES } from './features/cart/cart.routes';
import { ORDER_ROUTES } from './features/orders/orders.routes';
import { INVENTORY_ROUTES } from './features/inventory/inventory.routes';
import { PAYMENT_ROUTES } from './features/payments/payments.routes';
import { NOTIFICATION_ROUTES } from './features/notifications/notifications.routes';

export const routes: Routes = [
  // Default route redirects to login
  { path: '', redirectTo: '/auth/login', pathMatch: 'full' },

  // Auth routes (public - no guard)
  { path: 'auth', children: AUTH_ROUTES },

  // Protected feature routes
  { path: 'dashboard', children: DASHBOARD_ROUTES, canActivate: [authGuard] },
  { path: 'catalog', children: CATALOG_ROUTES, canActivate: [authGuard] },
  { path: 'cart', children: CART_ROUTES },
  { path: 'orders', children: ORDER_ROUTES },
  { path: 'inventory', children: INVENTORY_ROUTES },
  { path: 'payments', children: PAYMENT_ROUTES },
  { path: 'notifications', children: NOTIFICATION_ROUTES },

  // Catch-all route redirects to login
  { path: '**', redirectTo: '/auth/login' },
];

