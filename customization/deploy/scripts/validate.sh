#!/usr/bin/env bash
set -Eeuo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-customization/deploy/compose.yaml}"
ENV_FILE="${ENV_FILE:-customization/deploy/.env}"

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: docker CLI not found." >&2
  exit 1
fi
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE. Copy customization/deploy/.env.example and configure it first." >&2
  exit 1
fi

docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" config --quiet
echo "Compose configuration is valid."
