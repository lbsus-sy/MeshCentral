# Beginner install (Ubuntu / Debian VPS)

## One command

1. Point a DNS A record (for example `mesh.example.com`) to the VPS public IP.
2. Allow inbound TCP ports 80 and 443 in the VPS firewall/security group.
3. SSH to the VPS and run:

```bash
curl -fsSL https://raw.githubusercontent.com/lbsus-sy/MeshCentral/custom/ui-deploy-foundation/customization/deploy/install.sh | sudo bash
```

4. Enter the hostname when prompted.
5. Open `https://your-hostname` and create the first administrator account.

The script installs Docker, downloads this branch, creates the environment file, builds the container and starts MeshCentral. It stops rather than overwriting an existing `/opt/lakers-meshcentral` directory.

## Important

- Supports Ubuntu/Debian VPS with public DNS and TCP 80/443 available.
- This branch is not merged to `master`; the script deploys `custom/ui-deploy-foundation`.
- The simple Compose setup does **not** automatically issue a publicly trusted TLS certificate. Configure trusted TLS/reverse proxy before exposing sensitive use to the internet; do not treat a browser certificate warning as safe for production.
- Create the first administrator, enable MFA, and keep public account registration disabled.
- Keep environment files, Docker volumes, server data and certificates private. Back up before updating.

## Commands

Run from `/opt/lakers-meshcentral`:

```bash
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env ps
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env logs -f meshcentral
bash customization/deploy/scripts/backup.sh
```

Do not delete named Docker volumes when stopping or updating.
