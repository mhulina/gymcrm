# Deploying to SnapDeploy

SnapDeploy deploys one container per app, with no docker-compose/multi-container-stack support,
no persistent volumes, and no documented private networking between your own containers. Its
"database" feature is exclusively a managed add-on (`DATABASE_URL` auto-injected) - there's no
supported way to self-host Postgres as your own container there and have it reachable, so this
app deploys as **two** SnapDeploy containers (`api`, `webapp`) plus SnapDeploy's **managed
Postgres add-on** for the database. This is different from the self-hosted `docker-compose.prod.yml`
path (which does run a real Dockerized Postgres, exactly like the dev setup) - use that instead on
a normal VPS/server if you want a database you actually run yourself.

These are manual dashboard/CLI steps - there's no SnapDeploy account or API access available from
this environment, so none of this has been executed or verified end-to-end.

## 1. Deploy the API container

- New container, connect this GitHub repo.
- Dockerfile path: `GymCRM.Api/Dockerfile` (unchanged from dev - already a standalone,
  self-contained image, no changes needed for this).
- It listens on port 8080 inside the container (the base image's default) - point SnapDeploy's
  port detection at 8080 if it's not auto-detected.

## 2. Attach the managed Postgres add-on

- Add SnapDeploy's one-click managed Postgres add-on to the `api` container.
- The app needs **two** databases (`identity_db`, `scheduling_db`); the add-on provisions one
  database by default. Use SnapDeploy's browser-based SQL editor to create the second one:
  ```sql
  CREATE DATABASE scheduling_db;
  ```
  (the add-on's own database can be renamed to or used as `identity_db`, or create both fresh and
  ignore the default one - whichever SnapDeploy's UI makes easier.)
- SnapDeploy auto-injects a `DATABASE_URL`-style connection string. Translate it into these two
  env vars by hand on the `api` container (SnapDeploy's docs confirm you can set env vars
  alongside add-on-injected ones):
  ```
  ConnectionStrings__Identity=Host=<add-on host>;Port=<add-on port>;Database=identity_db;Username=<add-on user>;Password=<add-on password>
  ConnectionStrings__Scheduling=Host=<add-on host>;Port=<add-on port>;Database=scheduling_db;Username=<add-on user>;Password=<add-on password>
  ```

## 3. Set the remaining `api` env vars

```
ASPNETCORE_ENVIRONMENT=Production
Authentication__SecretForKey=<generate a random base64 secret - e.g. `openssl rand -base64 48`>
Authentication__Issuer=GymCRM.IdentityAPI
Authentication__Audience=GymCRM
Authentication__ExpiryMinutes=60
Security__MaxFailedLoginAttempts=10
Security__LockoutDurationMinutes=5
Security__AuthRateLimitPerMinute=100
Security__RegisterRateLimitPerMinute=100
```

Leave `Cors__AllowedOrigins` for step 5, once the `webapp` container's URL is known.

## 4. Deploy the webapp container

- New container, same GitHub repo.
- Dockerfile path: `gymcrm.web/Dockerfile.prod` (the static-only nginx image also used by the
  self-hosted stack - deliberately has no `/api/` reverse-proxy, since that would need
  container-to-container networking SnapDeploy doesn't document). It listens on port 80.
- Build args, pointing directly at the `api` container's own SnapDeploy URL (cross-origin - the
  same pattern already used today in dev against `localhost:55080`, just a different host):
  ```
  REACT_APP_API_URL=https://<your-api-container>.snapdeploy.app
  REACT_APP_IDENTITY_API_URL=https://<your-api-container>.snapdeploy.app/api/v1/
  REACT_APP_SCHEDULING_API_URL=https://<your-api-container>.snapdeploy.app/api/v1/
  REACT_APP_ACCOUNTS_ENDPOINT=https://<your-api-container>.snapdeploy.app/api/v1/Authentication/
  REACT_APP_MEMBERS_ENDPOINT=https://<your-api-container>.snapdeploy.app/api/v1/Members/
  ```

## 5. Close the loop on CORS

Back on the `api` container, set:
```
Cors__AllowedOrigins=https://<your-webapp-container>.snapdeploy.app
```
Without this, every browser request from the deployed frontend to the deployed API is blocked by
CORS (the two containers are on different origins here, unlike the self-hosted single-domain
nginx setup) - this is the one step that's easy to forget and the one most likely to look like a
mysterious "network error" in the browser if skipped.

## Verifying it worked

- `https://<your-api-container>.snapdeploy.app/health` should return `Healthy`.
- `https://<your-webapp-container>.snapdeploy.app` should load the app; register/login should
  round-trip successfully (proves the DB connection strings and CORS are both correct).
