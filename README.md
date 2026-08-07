# GymCRM

GymCRM is a CRM/back-office system for gyms — it handles member accounts and authentication, and trainer scheduling (availability, working hours, time off, and training session bookings).

The backend is a **modular monolith**: a single ASP.NET Core host (`GymCRM.Api`) that hosts multiple self-contained modules (Identity, Scheduling) in-process, each with its own database, repositories, services, and controllers. A React SPA (`gymcrm.web`) consumes the API.

## What it does

- **Identity module** — member accounts, registration/login, JWT access + refresh tokens (cookie-based), account lockout after repeated failed logins.
- **Scheduling module** — trainer availability and working hours, time-off requests, training session booking with conflict/overlap validation, and public holiday seeding.

A few additional service projects (`GymCRM.BillingAPI`, `GymCRM.MessagingAPI`, `GymCRM.NotificationsAPI`, `GymCRM.WorkoutAPI`) exist in the solution as scaffolding for future modules; they are not wired into `GymCRM.Api` or `docker-compose.yaml` yet.

## Tech stack

**Backend**
- .NET 10 / ASP.NET Core (C# 14)
- Entity Framework Core + Npgsql (PostgreSQL)
- JWT Bearer authentication (cookie-delivered access token) + refresh tokens
- AutoMapper for entity/DTO mapping
- Serilog (console + rolling file sinks) for logging
- Swagger/Swashbuckle for API docs, with API versioning (`Asp.Versioning`)
- ASP.NET Core rate limiting and health checks
- xUnit + FluentAssertions for tests (unit + integration, integration tests run against a real Postgres)

**Frontend**
- React 19 + TypeScript (Create React App / `react-scripts`)
- React Router, React Bootstrap, Axios, `jwt-decode`

**Infrastructure**
- PostgreSQL 17 (separate `identity_db` and `scheduling_db` databases, same server)
- Docker / Docker Compose for local dev, testing, and image builds
- GitLab CI (build → test against Postgres → Docker image build)

## Methodologies

- **Modular monolith**: each module (`GymCRM.IdentityAPI`, `GymCRM.SchedulingAPI`) is a composition root exposing `Add<Module>Module(...)`, its own `DbContext`, migrations, and AutoMapper profiles. Cross-cutting host concerns (CORS, auth, versioning, Swagger, rate limiting) live once in `GymCRM.Api` (see `ProgramConfigurations.cs`).
- **Repository / Unit-of-Work pattern** on top of EF Core for data access.
- **DTO + AutoMapper** boundary between entities and API contracts.
- **Database-per-module**: Identity and Scheduling each own a separate Postgres database and migration history, even though both run inside the same process.
- **API versioning** via URL segment or `X-Api-Version` header, with generated Swagger docs per version.
- Automated tests split into **unit tests** (business/validation logic) and **integration tests** (repositories/services against a real database).

## Project structure

```
GymCRM.Api/                Host — wires up modules, auth, CORS, Swagger, rate limiting
GymCRM.IdentityAPI/        Identity module (accounts, auth, members)
GymCRM.SchedulingAPI/      Scheduling module (availability, time off, sessions)
GymCRM.Shared/             Shared utilities (e.g. JSON converters for Date/TimeOnly)
GymCRM.IdentityAPI.Tests/  Identity unit + integration tests
GymCRM.SchedulingAPI.Tests/Scheduling unit + integration tests
gymcrm.web/                React frontend
```

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)
- [Node.js](https://nodejs.org/) (for `gymcrm.web`)
- Docker + Docker Compose
- PostgreSQL 17 (provided via Docker Compose — no separate install needed)

### 1. Configure environment

Copy the example env file and fill in real values (at minimum a Postgres password and a base64-encoded JWT secret):

```bash
cp .env.example .env.development
```

### 2. Run everything with Docker Compose (recommended)

```bash
./dev.sh
```

This builds and starts Postgres, `GymCRM.Api`, and the React app (`docker-compose.dev.yml`, loading `.env.development`). Other entry points:

```bash
./prod.sh          # production compose (docker-compose.yaml), uses .env.production
./test.sh          # test compose (docker-compose.test.yml), uses .env.test
./dev-nginx.sh      # dev stack behind local nginx (docker-compose.dev-nginx.yml)
```

Useful helpers: `./status.sh` (service status), `./logs.sh` (tail logs), `./stop.sh` (stop the stack).

### 3. Run the API locally instead (e.g. from an IDE)

1. Start just the database:
   ```bash
   docker compose -f docker-compose.dev.yml --env-file .env.development up -d postgres
   ```
2. Load `.env.development` into .NET User Secrets for `GymCRM.Api`:
   ```bash
   ./setup-user-secrets.sh
   ```
3. Run `GymCRM.Api` with `ASPNETCORE_ENVIRONMENT=Development` (e.g. `dotnet run --project GymCRM.Api`, or press F5/Debug in your IDE). EF Core migrations for both modules are applied automatically on startup.
4. API is served at `http://localhost:55080` (Swagger UI at `/swagger` in Development).

### 4. Run the frontend locally

```bash
cd gymcrm.web
npm install
npm start
```

The app runs on `http://localhost:3000` and expects the API at the URL in `REACT_APP_API_URL` (see `.env.development`).

## Testing

```bash
dotnet test GymCRM.IdentityAPI.Tests
dotnet test GymCRM.SchedulingAPI.Tests
```

Integration tests require a reachable Postgres instance (connection string via `ConnectionStrings__DefaultConnection` or `appsettings.Test.json`) — see `.gitlab-ci.yml` for the CI configuration used against a containerized Postgres.
