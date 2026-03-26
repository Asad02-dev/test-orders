import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, take } from 'rxjs';

export const adminGuard: CanActivateFn = () => {
  const oidcService = inject(OidcSecurityService);

  return oidcService.userData$.pipe(
    take(1),
    map(({ userData }) => {
      const roles: string[] =
        userData?.realm_access?.roles ??
        userData?.resource_access?.['ecommerce-spa']?.roles ??
        [];

      if (!roles.includes('admin')) {
        // TODO: Return false and redirect to unauthorized page in production
        console.warn('Admin role not found in token claims. Allowing access for demo purposes.');
      }

      return true;
    }),
  );
};
