# Backend local setup

The API loads `WayanadGo.Api/.env` for local development. Copy `.env.example` to `.env` and fill in local values. The `.env` file is ignored by Git. Values supplied by the process environment take precedence over `.env`.

For production, supply `ConnectionStrings__DefaultConnection`, `JWT_SECRET`, and the `Email__*` settings through the hosting platform's secret or environment-variable manager. Set `Cors__AllowedOrigins__0` to the deployed Angular origin. Do not deploy the `.env` file. `JWT_SECRET` must contain at least 32 bytes. SMTP is configured for STARTTLS, typically on port 587.

Run the API from the repository root with:

```powershell
dotnet run --project .\backend\WayanadGo.Api --launch-profile https
```

The HTTPS profile listens on `https://localhost:7025` and redirects HTTP requests to HTTPS. Angular uses `https://localhost:7025/api`.

## Database migrations

Review pending migration SQL before applying it:

```powershell
dotnet ef migrations list --project .\backend\WayanadGo.Infrastructure --startup-project .\backend\WayanadGo.Api
dotnet ef migrations script --idempotent --project .\backend\WayanadGo.Infrastructure --startup-project .\backend\WayanadGo.Api -o .\migration-review.sql
```

After reviewing the script and confirming the target is the intended local database, apply with:

```powershell
dotnet ef database update --project .\backend\WayanadGo.Infrastructure --startup-project .\backend\WayanadGo.Api
```

The prepared migration keeps destination rows, fills missing slugs and timestamps, and creates the authentication tables and indexes. It changes null legacy descriptions/locations to empty strings before making them required. The migration refuses automatic rollback because that could delete account data. Do not run `database update` against production without a reviewed deployment plan and backup.
