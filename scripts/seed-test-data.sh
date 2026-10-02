#!/usr/bin/env bash
# Loads demo sellers (with store addresses), their reviews, active listings, and 60 days of demo analytics into the LOCAL docker-compose Postgres so the mobile
# app's home rails, search, and category pages have something to show. Re-runnable: existing
# rows are kept, and seeded listings are re-activated with a fresh 30-day expiry each run.
#
# Every seeded row's Id starts with 5eed, so it never collides with real data.
# The demo sellers (demo.seller1..4@ogasela.test) can sign in with password: Password123!
#
# Usage (from the Ogasela.API repo root, with `docker compose up` running):
#   scripts/seed-test-data.sh           # seed / refresh
#   scripts/seed-test-data.sh --reset   # remove all seeded rows
set -euo pipefail

cd "$(dirname "$0")/.."

psql_local() {
  docker compose exec -T postgres psql -U ogasela -d ogasela -v ON_ERROR_STOP=1 "$@"
}

case "${1:-}" in
  --reset)
    psql_local <<'SQL'
BEGIN;
DELETE FROM "AdCampaigns" WHERE "Id"::text LIKE '5eed%';
DELETE FROM "ListingDailyStats" WHERE "ListingId"::text LIKE '5eed%';
DELETE FROM "SellerDailyStats" WHERE "SellerId"::text LIKE '5eed%';
DELETE FROM "Reviews" WHERE "Id"::text LIKE '5eed%';
DELETE FROM "Listings" WHERE "Id"::text LIKE '5eed%';
DELETE FROM "SellerProfiles" WHERE "Id"::text LIKE '5eed%';
DELETE FROM "Users" WHERE "Id"::text LIKE '5eed%';
COMMIT;
SQL
    echo "Removed seeded test data."
    ;;
  "")
    psql_local < scripts/seed-test-data.sql > /dev/null
    psql_local -At -c "SELECT count(*) || ' seeded listings active' FROM \"Listings\" WHERE \"Id\"::text LIKE '5eed%' AND \"Status\" = 'Active';"
    ;;
  *)
    echo "Unknown option: $1 (expected --reset or nothing)" >&2
    exit 2
    ;;
esac
