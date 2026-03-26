import { Routes } from '@angular/router';
import { ProductListComponent } from './pages/product-list/product-list';
import { ProductDetailComponent } from './pages/product-detail/product-detail';
import { ProductManagementComponent } from './pages/product-management/product-management';
import { adminGuard } from '../../core/guards/admin.guard';

export const CATALOG_ROUTES: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'manage', component: ProductManagementComponent, canActivate: [adminGuard] },
  { path: ':id', component: ProductDetailComponent },
];
