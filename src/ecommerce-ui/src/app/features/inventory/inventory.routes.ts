import { Routes } from '@angular/router';
import { InventoryManagementComponent } from './pages/inventory-management/inventory-management';
import { authGuard } from '../../core/guards/auth.guard';
import { adminGuard } from '../../core/guards/admin.guard';

export const INVENTORY_ROUTES: Routes = [
  { path: '', component: InventoryManagementComponent, canActivate: [authGuard, adminGuard] },
];
