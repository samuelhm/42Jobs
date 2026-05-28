#!/usr/bin/env bash
set -euo pipefail

# ─── Get latest tag ───────────────────────────────────────
latest=$(git tag --sort=-v:refname --list 'v*' | head -1)

if [ -z "$latest" ]; then
    new="v0.1.0-beta"
else
    if [[ "$latest" =~ ^v([0-9]+)\.([0-9]+)\.([0-9]+)-(.+)$ ]]; then
        major="${BASH_REMATCH[1]}"
        minor="${BASH_REMATCH[2]}"
        patch="${BASH_REMATCH[3]}"
        suffix="${BASH_REMATCH[4]}"
        new="v${major}.${minor}.$((patch + 1))-${suffix}"
    else
        echo "Error: el tag '$latest' no sigue el patrón vX.Y.Z-suffix" >&2
        exit 1
    fi
fi

# ─── Safety check ─────────────────────────────────────────
if git rev-parse "$new" >/dev/null 2>&1; then
    echo "Error: el tag $new ya existe" >&2
    exit 1
fi

# ─── Create tag + push ────────────────────────────────────
echo "Tag anterior : $latest"
echo "Nuevo tag    : $new"
echo ""

git tag "$new"
git push origin "$new"

echo "Release $new creada — el deploy se ha disparado en GitHub Actions"
