#!/usr/bin/env bash
# Seeds (or promotes) one SuperAdmin account - the only way to provision the first admin, since
# internal roles have no public signup path. Runs the API's own SuperAdminSeeder through
# `Ogasela.Api seed-superadmin`, so passwords are hashed and conflicts handled exactly as on
# startup (see src/Ogasela.Infrastructure/Accounts/SuperAdminSeeder.cs). Migrations run first.
#
# Usage (from the Ogasela.API repo root):
#   scripts/seed-superadmin.sh            # local `dotnet run` against appsettings.Development.json + the repo-root .env
#   scripts/seed-superadmin.sh --docker   # one-off api container via docker compose (uses .env)
#
# Prompts for anything not already set in SUPERADMIN_SEED_NAME / _EMAIL / _PHONE / _PASSWORD.
# The password is passed via environment variable, never as a command-line argument.
set -euo pipefail

cd "$(dirname "$0")/.."

mode="local"
case "${1:-}" in
  --docker) mode="docker" ;;
  "") ;;
  -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
  *) echo "Unknown option: $1 (expected --docker or nothing)" >&2; exit 2 ;;
esac

name="${SUPERADMIN_SEED_NAME:-}"
email="${SUPERADMIN_SEED_EMAIL:-}"
phone="${SUPERADMIN_SEED_PHONE:-}"
password="${SUPERADMIN_SEED_PASSWORD:-}"

[[ -n "$name" ]] || read -rp "Full name: " name
[[ -n "$email" ]] || read -rp "Email (used to sign in to the control portal): " email
[[ -n "$phone" ]] || read -rp "Phone (11 digits, e.g. 08031234567 - receives login codes): " phone

if [[ -z "$password" ]]; then
  read -rsp "Password (min 8 characters): " password; echo
  read -rsp "Confirm password: " confirm; echo
  if [[ "$password" != "$confirm" ]]; then
    echo "Passwords don't match." >&2
    exit 1
  fi
fi

if [[ -z "$name" ]]; then echo "Name is required." >&2; exit 1; fi
if [[ ! "$email" =~ ^[^[:space:]@]+@[^[:space:]@]+\.[^[:space:]@]+$ ]]; then echo "Invalid email: $email" >&2; exit 1; fi
if [[ ! "$phone" =~ ^0[789][01][0-9]{8}$ ]]; then echo "Phone must be an 11-digit Nigerian mobile number starting with 0 (e.g. 08031234567)." >&2; exit 1; fi
if (( ${#password} < 8 )); then echo "Password must be at least 8 characters." >&2; exit 1; fi

export SuperAdminSeed__Name="$name"
export SuperAdminSeed__Email="$email"
export SuperAdminSeed__Phone="$phone"
export SuperAdminSeed__Password="$password"

if [[ "$mode" == "docker" ]]; then
  docker compose run --rm \
    -e SuperAdminSeed__Name -e SuperAdminSeed__Email -e SuperAdminSeed__Phone -e SuperAdminSeed__Password \
    api seed-superadmin
else
  ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
    dotnet run --project src/Ogasela.Api -- seed-superadmin
fi
