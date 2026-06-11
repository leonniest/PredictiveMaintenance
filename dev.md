# Predictive Maintenance Showcase — Developer Guide

A full-stack showcase that predicts maintenance needs for PLC/PCB-controlled access machines (rotating doors, sliding doors, gates, barriers) operated by reseller companies. An admin sees everything; reseller users see only their own fleet.

## Repository Layout

```
.
├── docker-compose.yml          # All infrastructure + app containers
├── .env                        # DEEPSEEK_API_KEY (gitignored, see .env.example)
├── README.md                   # URLs + endpoint list
├── backend/
│   ├── src/
│   │   ├── PredictiveMaintenance.Api/             # ASP.NET Core minimal API host
│   │   ├── PredictiveMaintenance.Application/     # Services, DTOs, interfaces
│   │   ├── PredictiveMaintenance.Domain/          # Entities + enums (no dependencies)
│   │   └── PredictiveMaintenance.Infrastructure/  # EF Core, SMTP, RabbitMQ, DeepSeek, seeding
│   └── tests/PredictiveMaintenance.Tests/         # xUnit tests (prediction service)
└── frontend/                   # Vue 3 + Vite + TypeScript SPA
```

## Stack

- **Backend:** .NET 10, ASP.NET Core minimal APIs, EF Core 10, MSSQL 2022
- **Frontend:** Vue 3 (script setup), Vite, TypeScript — no router/store library; a single `App.vue` switches pages
- **Infra:** Docker Compose with MSSQL, RabbitMQ, Mailpit (local SMTP inbox)
- **LLM:** DeepSeek chat completions, called server-side only

## Docker Services (docker-compose.yml)

| Service | Container | Ports (host) | Purpose |
| --- | --- | --- | --- |
| `mssql` | predictive-maintenance-mssql | 14333 → 1433 | Database (sa / `Your_strong_password123!`) |
| `rabbitmq` | predictive-maintenance-rabbitmq | 5672, 15672 | Optional alert publishing + management UI |
| `mailpit` | predictive-maintenance-mailpit | 1025 (SMTP), 8025 (UI) | Catches all outgoing mail |
| `api` | predictive-maintenance-api | 5088 → 8080 | ASP.NET Core API |
| `frontend` | predictive-maintenance-frontend | 5173 → 80 | Nginx serving the built SPA, proxies `/api` to the api container |

`DEEPSEEK_API_KEY` is read from the root `.env` file and injected into the api container as `DeepSeek__ApiKey`. Model and base URL are set via `DeepSeek__Model` (`deepseek-v4-flash`) and `DeepSeek__BaseUrl` (`https://api.deepseek.com`).

## Backend

### Architecture

Classic clean-architecture split:

- **Domain** — entities (`Company`, `Machine`, `ControllerUnit`, `MachinePart`, `TelemetryDailySummary`, `TechnicianRecord`, `MaintenanceAlert`, `ResellerSupportSettings`, `AppUser`, assistant chat session/message) and enums (`AlertSeverity`, `AlertStatus`, `ControllerKind`, `PartType`, `UserRole`, `WorkType`). One machine has exactly one controller; a controller has parts; parts have daily telemetry summaries, technician records and alerts.
- **Application** — service interfaces and implementations:
  - `AuthService` — credential validation against PBKDF2-hashed passwords.
  - `DashboardService` — summary, companies, machines, alerts, part-lifetime and failure-trend analytics. All scoped by `UserContext` (admin sees all, reseller only their `CompanyId`).
  - `PredictionService` — pure heuristic model: combines movement-cycle ratio, age ratio and temperature ratio against per-part-type baselines (learned from seeded technician replacement records) into a health score (0–100), severity, predicted due date and reason codes.
  - `AlertService` — `EvaluateAsync` runs the prediction for every part and creates/updates a `MaintenanceAlert` for anything at Warning/Critical severity. Also handles acknowledge + notify (notify sends through the configured notifier and stamps channel/time).
  - `SupportService` — per-reseller support settings (support mailbox, technician dispatch mailbox, escalation phone, SLA hours) and **technician dispatch**: sends a dispatch email listing the open alerts of the machine and, when dispatched from a specific alert, sets that alert's status to `Dispatched` (persisted, survives refresh).
  - `AssistantService` — SQL-persisted chat sessions per user. Builds a system prompt + last 16 messages and calls the DeepSeek client when a key is configured; otherwise answers with a local playbook fallback. API/network errors are returned as readable assistant messages instead of HTTP 500s.
- **Infrastructure** — `PredictiveMaintenanceDbContext` (EF Core, SQL Server), `SeedData`, `SmtpEmailSender`/`SmtpAlertNotifier` (MailKit → Mailpit), `RabbitMqAlertNotifier`, `ConfiguredAlertNotifier` (picks SMTP or RabbitMQ via `Notifications:Mode`), `DeepSeekClient`, `Pbkdf2PasswordHasher`.

### API host (Program.cs)

- JWT bearer auth; `POST /api/auth/login` issues an 8-hour token containing role + companyId claims. Everything under `/api` (except login) requires the token; `/api/alerts/evaluate` requires the Admin role.
- Swagger UI at `/swagger` with a Bearer security definition (paste the raw token).
- On startup `SeedDatabaseWithRetryAsync` retries up to 10× (waits for MSSQL), then `SeedData` runs `EnsureCreated` and inserts deterministic demo data — 3 resellers, 9 machines, parts, ~18 months of telemetry summaries and technician history — only when the database is empty.
- `AlertEvaluationHostedService` re-evaluates predictions 15 s after startup and every 5 minutes.

### Alert lifecycle

`Open` → (`Notified` when mailed to the reseller) → (`Dispatched` when a technician dispatch email was triggered from the alert) → `Acknowledged` / `Resolved`. Open, Notified and Dispatched all count as "open" in every dashboard count.

## Frontend

- `src/api.ts` — tiny fetch wrapper; JWT stored in `localStorage` (`pm_token`) and attached as a Bearer header. All calls go to relative `/api/...` (Vite dev server and the Nginx container both proxy to the API).
- `src/App.vue` — owns all state. After login it loads summary, companies, alerts, analytics and support settings in parallel, then **auto-refreshes silently every 20 s** (no refresh button; background refreshes never flash the loading state or clear status messages).
- **Single source of truth for counts:** every open-alert number on screen (overview metrics, reseller risk list, reseller cards, machine badges) is derived client-side from the one `/api/alerts` response, so the overview and the resellers page can never disagree.
- Pages (`src/views`): `OverviewPage` (metrics, reseller fleet risk, critical dispatch queue), `ResellersPage` (per-company cards with machine drill-down), `MachinesPage` (machine list + detail: controller info, machine alerts, part table with usage/heat/prediction), `AnalyticsPage` (replacement life by part type + monthly technician work-order table + per-model lifetime tables), `AlertsPage` (full alert center with notify / mail support / acknowledge), `SettingsPage` (support settings per reseller), `ArchitecturePage` (interactive SVG system diagram — click a block to see its contents and connections).
- Components: `AppSidebar`, `TopAlertSummary`, `LoginView`, `BarChart` (inline SVG), `EmptyState`, `AssistantWidget` (bottom-right chat for reseller users: persistent sessions, FAQ prompts).
- Dispatch buttons turn into a disabled green **Dispatched** state; because the status is persisted server-side it stays dispatched across auto-refreshes and reloads.

## Assistant / DeepSeek

- The key never reaches the browser; Vue talks only to `/api/assistant/...`.
- `DeepSeekClient` posts to `{BaseUrl}/chat/completions` with model `deepseek-v4-flash` (current models: `deepseek-v4-flash`, `deepseek-v4-pro`; `deepseek-chat`/`deepseek-reasoner` are deprecated July 2026).
- Configure via root `.env`: `DEEPSEEK_API_KEY=sk-...`, then `docker compose up -d --build api`. Without a key the assistant uses the local playbook fallback.
- API errors (bad key, insufficient balance, etc.) are reported inside the chat reply with the upstream message.

## Notifications

`Notifications:Mode` selects the alert notifier: `Smtp` (default — Mailpit at :8025 shows everything) or `RabbitMQ` (publishes alert JSON to queue `predictive-maintenance.alerts`). Technician dispatch and "Mail support" always use SMTP via the per-reseller support settings.

## Local Development (without app containers)

```powershell
docker compose up -d mssql rabbitmq mailpit
dotnet run --project backend/src/PredictiveMaintenance.Api/PredictiveMaintenance.Api.csproj --urls http://localhost:5088
cd frontend; npm install; npm run dev
```

The API connects to MSSQL on `localhost:14333` (see `appsettings.json`). To enable DeepSeek locally set the env var `DeepSeek__ApiKey` before `dotnet run`.

### Tests / checks

```powershell
dotnet test PredictiveMaintenance.sln
cd frontend; npx vue-tsc --noEmit
```

### Resetting the demo database

Data is seeded only into an empty database. To re-seed from scratch:

```powershell
docker compose down
docker volume rm predictivemaintenance_mssql-data
docker compose up -d --build
```
