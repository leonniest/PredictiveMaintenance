# Predictive Maintenance Showcase

Run: `docker compose up -d --build` (set `DEEPSEEK_API_KEY` in `.env` first). Full docs: [dev.md](dev.md).

## URLs

| URL | What |
| --- | --- |
| http://localhost:5173 | Dashboard (Vue) |
| http://localhost:5088/swagger | API Swagger |
| http://localhost:8025 | Mailpit inbox |
| http://localhost:15672 | RabbitMQ management (`guest`/`guest`) |

Logins: `admin@showcase.local` / `Admin123!` — resellers `reseller@atlas-access.test`, `reseller@vector-gate.test`, `reseller@orbit-entry.test` / `Reseller123!`.

## API Endpoints (JWT required except login)

```
POST /api/auth/login
GET  /api/me
GET  /api/dashboard/summary
GET  /api/companies
GET  /api/companies/{companyId}
GET  /api/machines/{machineId}
GET  /api/alerts
POST /api/alerts/evaluate                      (admin)
POST /api/alerts/{alertId}/acknowledge
POST /api/alerts/{alertId}/notify
GET  /api/support/settings
PUT  /api/support/settings
POST /api/support/dispatch
GET  /api/assistant/faq
GET  /api/assistant/sessions
POST /api/assistant/sessions
GET  /api/assistant/sessions/{sessionId}
POST /api/assistant/sessions/{sessionId}/messages
GET  /api/analytics/part-lifetimes
GET  /api/analytics/failure-trends
```
