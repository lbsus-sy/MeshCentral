#!/usr/bin/env bash
set -Eeuo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-customization/deploy/compose.yaml}"
ENV_FILE="${ENV_FILE:-customization/deploy/.env}"
BACKUP_DIR="${BACKUP_DIR:-./customization/backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
ARCHIVE="${BACKUP_DIR%/}/meshcentral-volumes-${STAMP}.tar.gz"
was_running=false
tmp=""

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: docker CLI not found." >&2
  exit 1
fi
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Error: $ENV_FILE not found. Copy customization/deploy/.env.example first." >&2
  exit 1
fi

container_id="$(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps -a -q meshcentral)"
if [[ -z "$container_id" ]]; then
  echo "Error: MeshCentral container not found. Start the stack once before backing it up." >&2
  exit 1
fi
if [[ "$(docker inspect -f '{{.State.Running}}' "$container_id")" == "true" ]]; then
  was_running=true
  docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" stop meshcentral
fi

cleanup() {
  [[ -n "$tmp" && -d "$tmp" ]] && rm -rf "$tmp"
  if [[ "$was_running" == true ]]; then
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" start meshcentral >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

mkdir -p "$BACKUP_DIR"
volume_ids="$(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" config --volumes)"
if [[ -z "$volume_ids" ]]; then
  echo "Error: Compose did not report any named volumes." >&2
  exit 1
fi

project_name="$(docker inspect -f '{{ index .Config.Labels "com.docker.compose.project" }}' "$container_id")"
tmp="$(mktemp -d)"
while IFS= read -r vol; do
  [[ -z "$vol" ]] && continue
  actual="$(docker volume ls --filter "label=com.docker.compose.project=$project_name" --filter "label=com.docker.compose.volume=$vol" --format '{{.Name}}' | head -n1 || true)"
  if [[ -z "$actual" ]]; then
    echo "Error: Compose volume '$vol' for project '$project_name' not found; aborting to avoid a partial backup." >&2
    exit 1
  fi
  mkdir -p "$tmp/$vol"
  docker run --rm -v "$actual:/source:ro" -v "$tmp/$vol:/dest" alpine:3.24 sh -ec 'cp -a /source/. /dest/'
done <<< "$volume_ids"

tar -czf "$ARCHIVE" -C "$tmp" .
chmod 600 "$ARCHIVE"
echo "Backup created: $ARCHIVE"
