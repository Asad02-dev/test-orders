export const environment = {
  production: true,
  // Empty string means requests use the same origin (relative path)
  apiUrl: '',
  keycloak: {
    authority: 'https://auth.example.com/realms/ecommerce',
    clientId: 'ecommerce-spa',
    redirectUri: 'https://shop.example.com',
    postLogoutRedirectUri: 'https://shop.example.com',
    scope: 'openid profile email',
  },
};
