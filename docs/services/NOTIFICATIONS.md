# Notifications Service

## Responsibility
Consumes notification commands and persists a log entry for each notification sent (simulated email/system notification).

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5106 |
| Database | `ecommerce_notifications` (PostgreSQL) |

## Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/notifications/status` | — | Service health/status |
| GET | `/api/notifications` | ✅ | List recent notification logs |
| GET | `/api/notifications/orders/{orderId}` | ✅ | Notification history for order |
| GET | `/health` | — | Health check |

## Notification Types
| Type | Trigger |
|------|---------|
| `OrderConfirmation` | `SendOrderConfirmationNotificationCommand` |
| `OrderCancelled` | `SendOrderCancelledNotificationCommand` |

Each log entry includes: recipient email, subject, body, timestamp, and sent flag.

## Integration Events Consumed
| Event | Action |
|-------|--------|
| `SendOrderConfirmationNotificationCommand` | Log confirmation notification |
| `SendOrderCancelledNotificationCommand` | Log cancellation notification |

## Current Status (Phase 2/3)
- [x] Notification log persistence (PostgreSQL)
- [x] Confirmation and cancellation consumers
- [x] Query endpoints for notification history
- [x] Correlation ID middleware

## Future Work
- [ ] Real email delivery (SendGrid / SMTP)
- [ ] Notification templates
- [ ] Additional notification types (shipped, refunded)
- [ ] EF Core migrations
