# Gateway Service

## Responsibility
Thin YARP reverse proxy that routes all external API traffic to downstream services. Also handles CORS and forwards auth headers.

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5100 |

## Routes
| Gateway Path | Downstream Service |
|---|---|
| `/api/catalog/**` | Catalog.Api (5101) |
| `/api/cart/**` | Cart.Api (5102) |
| `/api/orders/**` | Orders.Api (5103) |
| `/api/inventory/**` | Inventory.Api (5104) |
| `/api/payments/**` | Payments.Api (5105) |
| `/api/notifications/**` | Notifications.Api (5106) |

Configure downstream addresses in `appsettings.json` under `ReverseProxy.Clusters`.

## Current Status
- [x] YARP routing for all 6 services
- [x] Keycloak JWT auth forwarding
- [x] CORS (any origin for local development)
- [x] Health check at `/health`

## Future Work
- [ ] Rate limiting per route
- [ ] Request aggregation (cart + catalog in one call)
- [ ] Auth enforcement per route (not just forwarding)
- [ ] Correlation ID injection to outgoing requests
