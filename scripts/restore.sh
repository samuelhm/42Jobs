#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

if [ $# -ne 1 ]; then
    echo "Usage: $0 <backup_file.sql.gz>"
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: file not found: $BACKUP_FILE"
    exit 1
fi

if [ -f "$PROJECT_DIR/.env" ]; then
    export $(grep -v '^\s*#' "$PROJECT_DIR/.env" | grep -v '^\s*$' | xargs)
fi

DB_USER="${POSTGRES_USER:-42jobs}"
DB_NAME="${POSTGRES_DB:-42jobs}"
DB_CONTAINER="${DB_CONTAINER:-42jobs-db}"

echo "⚠  This will overwrite database '$DB_NAME' with '$BACKUP_FILE'"
read -r -p "Are you sure? [y/N] " confirm

if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo "Aborted."
    exit 0
fi

echo "→ Restoring $DB_NAME from $BACKUP_FILE..."
gunzip -c "$BACKUP_FILE" | docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME"

echo "✓ Restore complete."
