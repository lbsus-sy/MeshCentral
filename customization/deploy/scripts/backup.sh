#!/usr/bin/env bash
set -Eeuo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-deploy/compose.yaml}"
ENV_FILE="${ENV_FILE:-deploy/.env}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
ARCHIVE="${BACKUP_DIR%/}/meshcentral-volumes-${STAMP}.tar.gz"

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: docker CLI not found." >&2
  exit 1
fi
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Error: $ENV_FILE not found. Copy deploy/.env.example first." >&2
  exit 1
fi

mkdir -p "$BACKUP_DIR"
# A stopped service gives the most consistent file-based snapshot.
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" stop meshcentral
cleanup() {
  docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" start meshcentral >/dev/null 2>&1 || true
}
trap cleanup EXIT

volume_ids="$(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" config --volumes)"
if [[ -z "$volume_ids" ]]; then
  echo "Error: Compose did not report any volumes." >&2
  exit 1
fi

# Resolve actual project-scoped volume names through Compose and stream a root-owned archive.
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"; cleanup' EXIT
while IFS= read -r vol; do
  [[ -z "$vol" ]] && continue
  actual="$(docker volume ls --format '{{.Name}}' | grep -E "(^|_)${vol}$" | head -n1 || true)"
  if [[ -z "$actual" ]]; then
    echo "Warning: volume for logical name '$vol' not found; skipping." >&2
    continue
  fi
  mkdir -p "$tmp/$vol"
  docker run --rm -v "$actual:/source:ro" -v "$tmp/$vol:/dest" alpine:3.24 sh -ec 'cp -a /source/. /dest/'
done <<< "$volume_ids"

tar -czf "$ARCHIVE" -C "$tmp" .
chmod 600 "$ARCHIVE"
echo "Backup created: $ARCHIVE"
