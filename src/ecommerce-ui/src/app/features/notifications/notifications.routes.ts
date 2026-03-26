import { Routes } from '@angular/router';
import { NotificationListComponent } from './pages/notification-list/notification-list';
import { authGuard } from '../../core/guards/auth.guard';
import { adminGuard } from '../../core/guards/admin.guard';

export const NOTIFICATION_ROUTES: Routes = [
  { path: '', component: NotificationListComponent, canActivate: [authGuard, adminGuard] },
];
