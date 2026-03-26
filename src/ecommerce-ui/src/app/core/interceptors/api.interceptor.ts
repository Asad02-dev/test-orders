import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take } from 'rxjs';
import { environment } from '../../../environments/environment';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const oidcService = inject(OidcSecurityService);

  let apiReq = req;
  if (req.url.startsWith('/api/')) {
    apiReq = req.clone({
      url: `${environment.apiUrl}${req.url}`,
    });
  }

  return oidcService.getAccessToken().pipe(
    take(1),
    switchMap((token) => {
      const headers: Record<string, string> = {
        'X-Correlation-Id': crypto.randomUUID(),
      };

      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const authenticatedReq = apiReq.clone({
        setHeaders: headers,
      });

      return next(authenticatedReq);
    }),
  );
};
