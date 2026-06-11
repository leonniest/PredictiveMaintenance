# Predictive Maintenance Showcase

Vue 3 + .NET 10 showcase for predictive maintenance over seeded PLC/PCB-controlled assets. The app models admins, resellers, machines, controllers, parts, historical telemetry summaries, technician records, replacement history, predictions, alerts, support dispatch, and notification delivery.

## Stack

- .NET 10 ASP.NET Core API
- EF Core 10 with MSSQL
- Vue 3 + Vite + TypeScript
- RabbitMQ for optional alert publishing
- Mailpit for local SMTP inbox
- Server-side DeepSeek-compatible assistant integration
- Docker Compose for local showcase infrastructure and app containers

## Run With Docker

```powershell
docker compose up --build
```

Open:

- Dashboard: http://localhost:5173
- API Swagger: http://localhost:5088/swagger
- Mail inbox: http://localhost:8025
- RabbitMQ management: http://localhost:15672

RabbitMQ login is `guest` / `guest`.

## Demo Logins

- Admin: `admin@showcase.local` / `Admin123!`
- Reseller: `reseller@atlas-access.test` / `Reseller123!`
- Reseller: `reseller@vector-gate.test` / `Reseller123!`
- Reseller: `reseller@orbit-entry.test` / `Reseller123!`

Admin sees all resellers. Reseller users only see the machines assigned to their reseller organization.

## Dashboard Features

- Separate pages for overview, reseller scope, machine detail, analytics, alert center, and support settings.
- Machine detail shows machine-level alerts, first-alert date, controller details, part health, movement cycles, rotations or activations, heat, and due dates.
- Analytics includes technician record trend and part lifetime by part type.
- Support settings configure the support department, technician dispatch mailbox, escalation phone, and SLA hours per reseller.
- `Mail support` and alert dispatch actions send SMTP email to the configured support and technician recipients.
- Reseller users get a bottom-right assistant with persistent SQL-backed chat sessions, FAQ prompts, and server-side LLM routing.

## Swagger Authorization

Most API endpoints require a JWT. In Swagger:

1. Run `POST /api/auth/login` with one of the demo logins.
2. Copy the returned `token`.
3. Click `Authorize`.
4. Paste only the token value.

Requests without that token correctly return `401 Unauthorized`.

## Notification Toggle

The API sends alert notifications through SMTP by default:

```json
"Notifications": {
  "Mode": "Smtp"
}
```

Change `Notifications__Mode` to `RabbitMQ` in `docker-compose.yml` to publish alert payloads to the `predictive-maintenance.alerts` queue instead.

## Assistant LLM

The assistant runs through the API so the key is never exposed to Vue. Create a local `.env` file from `.env.example` and set `DEEPSEEK_API_KEY`, or set the variable in your shell before starting Docker:

```powershell
Copy-Item .env.example .env
# edit .env and set DEEPSEEK_API_KEY
docker compose up -d --build
```

The compose file maps `DeepSeek__BaseUrl` to `https://api.deepseek.com` and `DeepSeek__Model` to `deepseek-v4-flash`. If no key is configured, the assistant still works with a local maintenance-playbook fallback for the showcase.

## Local Development Without App Containers

Start only dependencies:

```powershell
docker compose up mssql rabbitmq mailpit
```

Run API:

```powershell
dotnet run --project backend/src/PredictiveMaintenance.Api/PredictiveMaintenance.Api.csproj --urls http://localhost:5088
```

Run frontend:

```powershell
cd frontend
npm install
npm run dev
```

The API creates the `PredictiveMaintenance` MSSQL database and seeds deterministic showcase data on startup.
