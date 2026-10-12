#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

bash customization/deploy/scripts/validate.sh
bash customization/deploy/scripts/backup.sh

# Update exactly the checked-out source revision; do not silently pull an unreviewed branch.
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env up -d --build
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env ps
echo "Deployment rebuilt from the current checkout. Verify login, agents, WebSockets, remote desktop and file transfer."
