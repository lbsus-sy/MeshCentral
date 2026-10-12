# Customization and deployment

This fork keeps UI assets and local deployment helpers separate from MeshCentral's remote-management core.

## What is customized first

The server UI is rendered primarily by Handlebars templates under `views/`; the largest templates contain both markup and client-side behavior. This first pass deliberately avoids rewriting those multi-megabyte files. MeshCentral supports a durable UI override at `meshcentral-web/public/styles/custom.css`, which is loaded after the default stylesheets. The CSS is kept in `customization/branding/web/public/styles/custom.css` and mounted read-only by Compose.

Authentication, authorization, WebSocket, remote desktop, terminal, and file-transfer logic remain upstream. Do not use CSS to hide security warnings or pretend to disable features; enforce access in server configuration and permissions.

## Local development

Run these commands from the repository root:

```sh
cp customization/deploy/.env.example customization/deploy/.env
# Edit customization/deploy/.env and set MESH_HOSTNAME to a DNS name you control.
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env config --quiet
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env up -d --build
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env logs -f meshcentral
```

The Compose build context points at this repository root and reuses `docker/Dockerfile`; it does not pull the upstream image as the application. The mounted branding directory is read-only so a container cannot modify the source-controlled CSS.

## Configuration behavior

This deployment uses `DYNAMIC_CONFIG=true` so the environment values for hostname, ports, account creation and TLS-offload can be applied on startup. Treat these environment variables as the source of truth for those managed settings. MeshCentral's own `config.json` and data remain in the persistent data volume. Review `customization/deploy/.env.example` before exposing the server.

Direct TLS is the default. For TLS termination at a reverse proxy, configure TLS offload and trusted-proxy settings deliberately to match the actual network topology. The trusted proxy environment setting accepts JSON syntax, such as `["127.0.0.1"]`; do not put a bare IP string there. Publish only the ports needed by your deployment.

## Persistent data and backups

Compose preserves the `meshcentral-data`, `meshcentral-files`, and `meshcentral-backups` volumes. The branding CSS is in Git and is mounted read-only from the repository.

Before an update, run:

```sh
bash customization/deploy/scripts/validate.sh
bash customization/deploy/scripts/backup.sh
```

Then check out a reviewed upstream commit/tag or your intended custom commit, rebuild, and smoke-test the deployment. Keep backups and a known-good Git ref until login, agent reconnect, WebSocket upgrades, remote desktop, and file transfer have been checked. Never delete the named volumes as part of an ordinary image update.

The backup helper stops MeshCentral during the archive to obtain a consistent filesystem snapshot. Store the resulting archive away from the live server for disaster recovery and periodically test restoration.

## Branding

- Edit `customization/branding/web/public/styles/custom.css` for theme changes.
- Put custom image overrides under `customization/branding/web/public/images/` while preserving expected paths and dimensions.
- MeshCentral supports title pictures and other branding values in the `domains[""]` section of `meshcentral-data/config.json`. Follow the official [Customization guide](https://docs.meshcentral.com/meshcentral/customization/) for image dimensions and supported settings.
- For deeper layout changes, copy only the required templates into the custom web directory and track them as version-sensitive overrides; avoid wholesale replacement until template changes are understood.

## Security and licensing notes

- Never commit `.env`, private keys, production certificates, database dumps, or real device data.
- Keep plugins disabled unless there is a reviewed operational need.
- Do not enable iframe embedding unless a concrete trusted origin policy is configured.
- Keep TLS and reverse-proxy trust configuration aligned; forwarded headers from untrusted sources must not be trusted.
- This repository is an Apache-2.0-licensed fork. Preserve upstream license and attribution notices when redistributing modifications.
