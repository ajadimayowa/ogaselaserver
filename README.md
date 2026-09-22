# Ogasela API

A .NET 10 / ASP.NET Core backend for Ogasela, a Nigerian classifieds/marketplace platform:
listings, biometric seller verification, wallet payments, messaging, reviews & trust scores,
notifications, AI listing tools, Facebook/TikTok ad integrations, and an internal moderation/
admin back-office.

Clean-architecture-style layout:

```
src/
  Ogasela.Domain          entities, enums - no external dependencies
  Ogasela.Application     CQRS commands/queries (MediatR), interfaces, business rules
  Ogasela.Infrastructure  EF Core/Postgres, Redis, AWS, payment/ad/AI provider implementations
  Ogasela.Api             controllers, middleware, composition root (Program.cs)
  Ogasela.Shared          the Result<T>/Error type used everywhere instead of exceptions
tests/
  Ogasela.UnitTests         handler/domain logic tests against EF Core's InMemory provider
  Ogasela.IntegrationTests  full HTTP-level tests against real Postgres/Redis (Testcontainers)
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for `docker-compose` and for the integration test suite's
  Testcontainers-managed Postgres/Redis instances)
- The `dotnet-ef` tool, pinned in [`.config/dotnet-tools.json`](.config/dotnet-tools.json) -
  restore it with `dotnet tool restore`

## Local setup

1. Copy the environment template and fill in real values (see the
   [environment variables](#environment-variables) table below for what each one is and where to
   get it):

   ```bash
   cp .env.example .env
   ```

   `.env` is gitignored - it's never committed, and `docker-compose.yml` reads it to configure
   the `api` container. A value left blank is fine for anything marked "optional" below; the
   corresponding feature just no-ops or returns a clear error instead of crashing.

2. Bring everything up:

   ```bash
   docker compose up --build
   ```

   This starts Postgres, Redis, and the API (published in Release mode inside the container, see
   [`src/Ogasela.Api/Dockerfile`](src/Ogasela.Api/Dockerfile)) on `http://localhost:8080`.
   **Database migrations run automatically on startup** (`Program.cs` calls
   `dbContext.Database.MigrateAsync()` before the app starts serving traffic) - there's no
   separate migration step to run by hand for this path.

3. Interactive API docs ("Bira Docs" - classic Swagger UI, every route documented, with an
   auto-generated sample payload per request) are served at
   `http://localhost:8080/api/v1/bira-doc` in every environment, backed by the OpenAPI document
   at `/openapi/v1.json`.

### Running without Docker for the API itself

You can also run Postgres/Redis via `docker compose up -d postgres redis` and run the API
directly on the host:

```bash
docker compose up -d postgres redis
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Ogasela.Api
```

`appsettings.Development.json` already points at `localhost` for both connection strings and
ships a local-only JWT signing key, so this works with no `.env` file at all - useful for quick
iteration, but `Verification:Provider` is forced to `Mock` in this mode (no AWS credentials
needed) and every other third-party integration (payments, AI, ads, push) simply no-ops until
its own keys are supplied.

### Running migrations manually

Only needed if you're not relying on the automatic startup migration (e.g. generating a new
migration during development, or applying migrations to a database the app isn't currently
pointed at):

```bash
# Postgres must be reachable at the connection string in appsettings.json/appsettings.Development.json
dotnet ef database update --project src/Ogasela.Infrastructure --startup-project src/Ogasela.Api

# After changing an entity or an IEntityTypeConfiguration:
dotnet ef migrations add <Name> --project src/Ogasela.Infrastructure --startup-project src/Ogasela.Api -o Persistence/Migrations
```

## Running tests

```bash
dotnet test tests/Ogasela.UnitTests/Ogasela.UnitTests.csproj
dotnet test tests/Ogasela.IntegrationTests/Ogasela.IntegrationTests.csproj
```

Unit tests use EF Core's InMemory provider and hand-rolled fakes for every external dependency -
no Docker needed. Integration tests spin up real Postgres and Redis containers per test class via
[Testcontainers](https://testcontainers.com/) (so **Docker must be running**), exercise the app
through real HTTP calls (`WebApplicationFactory<Program>`), and swap only genuinely external
third parties (SMS, email, payment gateways, ad platform clients) for fakes/no-ops - everything
else (auth, EF Core, business rules) runs for real.

CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs both suites plus a dependency
vulnerability scan (`dotnet list package --vulnerable`) and a container image vulnerability scan
(Trivy) on every push/PR to `main`.

## Load testing

[`scripts/load-test.js`](scripts/load-test.js) is a [k6](https://k6.io/) script covering
`GET /api/v1/search` and `GET /api/v1/listings/{id}`. See
[`LOAD_TEST_RESULTS.md`](LOAD_TEST_RESULTS.md) for the latest results and exact reproduction
steps.

```bash
brew install k6   # or see https://grafana.com/docs/k6/latest/set-up/install-k6/

BASE_URL=http://localhost:8080 \
LISTING_ID=<a real published listing id> \
k6 run scripts/load-test.js
```

## Observability & hardening

- **Correlation IDs**: every response carries an `X-Correlation-Id` header (reused from the
  request if the caller already set one), and the same id is attached to every structured log
  line for that request - paste it from a bug report straight into your log search.
- **Health checks**: `GET /health` (everything, for diagnostics), `GET /health/live` (process is
  up - no dependency calls, safe for an orchestrator's liveness probe), `GET /health/ready`
  (Postgres + Redis + a lightweight S3 `HeadBucket` call - safe for a readiness probe/load
  balancer).
- **Rate limiting**: `/api/v1/auth/*` and the message-send endpoint are rate-limited per
  IP/user respectively (see [`RateLimitingExtensions`](src/Ogasela.Api/RateLimiting/RateLimitingExtensions.cs)
  for the exact policy and the `RateLimiting` config section to tune it) - on top of, not instead
  of, OtpService's own per-phone-number verify-attempt limit.
- **Logging**: structured (Serilog), and deliberately never logs OTP codes, JWTs, biometric image
  references, or raw third-party payment gateway response bodies (which can carry card/customer
  data) - see `PaystackGateway`/`FlutterwaveGateway` for the redacted logging pattern.

## Environment variables

None of these have real values in this repo - `.env.example` documents every one with an empty
default (or an obviously-fake placeholder), and `.env` (gitignored) is where real values go
locally. In a real deployment, set these as actual secrets (e.g. your platform's secret manager),
not as plain environment variables in a shared config file.

| Variable | Required? | Used for |
|---|---|---|
| `JWT_SECRET` | **Required** | Signs/validates every access & refresh token. Generate with `openssl rand -base64 64`. Rotating it invalidates every currently-issued token. |
| `ConnectionStrings__Default` | Required (has a Postgres-in-docker-compose default) | Postgres connection string. |
| `ConnectionStrings__Redis` | Required (has a Redis-in-docker-compose default) | Redis connection string - OTP storage, Data Protection key storage, SignalR/notifications, caching. |
| `TERMII_API_KEY` | Required for phone OTP delivery | [Termii](https://termii.com/) SMS API key, used to send OTP codes for phone verification. |
| `EMAIL_SMTP_KEY` | Required for transactional email | SMTP key for the configured relay (Brevo, by default) - welcome emails, listing lifecycle notifications. |
| `AWS_REGION` | Required if `Verification:Provider=Aws` | AWS region for S3 + Rekognition. |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` | Required if `Verification:Provider=Aws` | AWS credentials for S3 (category images, biometric image storage) and Rekognition (face match/liveness verification). |
| `AWS_S3_BUCKET_NAME` | Required if `Verification:Provider=Aws` | S3 bucket for category and biometric images; also what `/health/ready`'s S3 check probes. |
| `AWS_REKOGNITION_FACE_MATCH_THRESHOLD` | Optional (defaults to 90) | Minimum face-match confidence to auto-verify a seller. |
| `PAYSTACK_SECRET_KEY` | Required for wallet funding (default payment provider) | [Paystack](https://paystack.com/) secret key - transaction init/verify/refund and webhook signature verification. |
| `FLUTTERWAVE_SECRET_KEY` / `FLUTTERWAVE_SECRET_HASH` | Optional (alternate payment provider) | [Flutterwave](https://flutterwave.com/) secret key and webhook verification hash. Switch the active provider via `Payments:Provider`. |
| `FIREBASE_PROJECT_ID` / `FIREBASE_SERVICE_ACCOUNT_JSON` | Optional | Firebase Cloud Messaging - push notifications silently no-op until both are set. `FIREBASE_SERVICE_ACCOUNT_JSON` is the full service-account JSON as a single-line string. |
| `GEMINI_API_KEY` | Required for AI listing-copy generation (default AI provider) | Google Gemini API key. Switch providers via `Ai:Provider`. |
| `ANTHROPIC_API_KEY` | Optional (alternate AI provider) | Anthropic API key, used only when `Ai:Provider=Anthropic`. |
| `FACEBOOK_ADS_APP_ID` / `FACEBOOK_ADS_APP_SECRET` | Optional | Meta Marketing API app credentials for the Facebook ad-platform integration. `RedirectUri` (in `appsettings.json`, not a secret) must match what's registered with the app. |
| `TIKTOK_ADS_APP_ID` / `TIKTOK_ADS_APP_SECRET` | Optional | TikTok Business API app credentials for the TikTok ad-platform integration. |

A few operational knobs that aren't secrets - set directly in `appsettings.json`/
`appsettings.Development.json` rather than via `.env` - are also worth knowing about:
`Verification:Provider` (`Aws`/`Mock`), `Payments:Provider` (`Paystack`/`Flutterwave`),
`Ai:Provider` (`Gemini`/`Anthropic`), and `RateLimiting:AuthPermitsPerMinute` /
`RateLimiting:MessagingPermitsPerMinute`.
