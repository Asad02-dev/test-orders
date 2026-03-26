export const environment = {
  production: false,
  apiUrl: 'http://localhost:5100',
  keycloak: {
    authority: 'http://localhost:8080/realms/ecommerce',
    clientId: 'ecommerce-spa',
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200',
    scope: 'openid profile email',
  },
};
