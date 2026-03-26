import { provideAuth, LogLevel } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

export function provideOidcAuth() {
  return provideAuth({
    config: {
      authority: environment.keycloak.authority,
      redirectUrl: environment.keycloak.redirectUri,
      postLogoutRedirectUri: environment.keycloak.postLogoutRedirectUri,
      clientId: environment.keycloak.clientId,
      scope: environment.keycloak.scope,
      responseType: 'code',
      silentRenew: true,
      useRefreshToken: true,
      logLevel: environment.production ? LogLevel.Error : LogLevel.Debug,
      secureRoutes: [environment.apiUrl],
    },
  });
}
