# Load Test Results

Load test script: [`scripts/load-test.js`](scripts/load-test.js) (k6). Covers the two endpoints
named in Phase 12's requirements: `GET /api/v1/search` and `GET /api/v1/listings/{id}`.

## How this run was produced

This is a real k6 run against a real running instance of the API, not a simulated/estimated
result. Steps taken:

1. `docker compose up -d postgres redis` (Postgres 16 + Redis 7, the same images/config the app
   uses everywhere else).
2. `dotnet run --project src/Ogasela.Api` with `ASPNETCORE_ENVIRONMENT=Development` (EF
   migrations run automatically on startup, per `Program.cs`; Development wiring uses the Mock
   verification provider, so no AWS credentials are needed for this).
3. Seeded data via the real HTTP API (not direct DB writes): registered and biometrically
   verified one seller, then created and published 3 Free-plan listings in the seeded
   "Electronics" category.
4. Ran `k6 run scripts/load-test.js` against `http://localhost:5080` with `LISTING_ID` set to one
   of the published listings and `SEARCH_QUERY=iPhone`.
5. Tore the environment back down (`docker compose down`) once the run completed.

## Load profile

- 25 constant virtual users hitting `GET /api/v1/search?query=iPhone` for 30s
- 25 constant virtual users hitting `GET /api/v1/listings/{id}` for 30s
- Both scenarios run concurrently (50 VUs total), each with a 200ms think-time between requests

This is a "moderate concurrent load" smoke-level run on a single developer machine (Apple
Silicon MacBook Air) against a locally-run API with local Postgres/Redis, not a production or
cloud-equivalent host, and with a small (3-listing) dataset. Absolute throughput numbers here
will not transfer directly to a production deployment; the point of this run is to confirm the
p95 latency budget holds and to give a baseline shape (see "Interpreting these numbers" below).

## Results

| Endpoint | Requests | p90 | p95 | max | Failure rate |
|---|---|---|---|---|---|
| `GET /api/v1/search` | 3,558 | 15.62ms | **20.09ms** | 150.38ms | 0.00% |
| `GET /api/v1/listings/{id}` | 3,574 | 19.13ms | **25.39ms** | 126.04ms | 0.00% |

- Combined throughput: 7,132 requests in 30.1s (~237 req/s across both endpoints)
- Both `http_req_duration{endpoint:search} p(95)<800ms` and `http_req_duration{endpoint:listing_detail} p(95)<800ms` thresholds **passed**
- `http_req_failed rate<0.01` threshold **passed** (0 failed requests out of 7,132)

**p95 latency for both endpoints stayed under 800ms** - in this run, by roughly 30x margin.

## Interpreting these numbers

- The large margin below the 800ms budget is expected: this dataset has 3 listings and Postgres
  is on the same machine as the API (near-zero network latency), so most of each request's time
  is query planning/execution and JSON serialization rather than anything the 800ms budget is
  actually meant to guard against (network hops, a much larger/production-sized dataset, a
  loaded shared database, connection pool contention under real concurrent traffic).
- The search endpoint (`PostgresListingSearchRepository`, Phase 9) uses a `tsvector` GIN-indexed
  full-text query plus a boost-weight sort - both scale with index size, not linearly with
  result-set size, so latency growth under a realistic multi-thousand-listing catalog should
  stay modest, but this run doesn't prove that at scale.
- Re-run this script against a staging environment with production-representative data volume
  and network topology before treating the 800ms budget as validated for production.

## Reproducing this run

```bash
docker compose up -d postgres redis
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Ogasela.Api &

# Seed a seller + verify + publish at least one listing (see README.md's "Local setup" section
# for the request sequence), then:

BASE_URL=http://localhost:5080 \
LISTING_ID=<a real published listing id> \
SEARCH_QUERY=iPhone \
k6 run scripts/load-test.js
```
