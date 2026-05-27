#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PATCHES_DIR="$SCRIPT_DIR/../database/production-patches"

DB_USER="${POSTGRES_USER:-42jobs}"
DB_NAME="${POSTGRES_DB:-42jobs}"

echo "── Applying production patches ──"
echo ""

count=0
for f in "$PATCHES_DIR"/*.sql; do
  [ -f "$f" ] || continue
  name="$(basename "$f")"
  echo -n "  $name ... "
  docker exec -i 42jobs-db psql -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 < "$f" > /dev/null 2>&1
  echo "ok"
  count=$((count + 1))
done

echo ""
echo "── $count patch(es) applied ──"
