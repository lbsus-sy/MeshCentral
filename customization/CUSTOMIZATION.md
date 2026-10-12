# Customization and deployment

## What is customized first

The server UI is primarily rendered by Handlebars templates under `views/`; the main views are very large and include client-side behavior. Avoid wholesale template replacement for the first pass. Keep brand assets and additive CSS separate, then add references through a narrowly scoped, version-aware patch after reviewing the template entry points. Remote desktop, terminal, file transfer, WebSocket, authentication, and authorization behavior should remain unchanged during visual-only work.

## Local development

From the repository root:

```sh
cp deploy/.env.example deploy/.env
# Edit deploy/.env and set MESH_HOSTNAME to a DNS name you control
docker compose -f deploy/compose.yaml --env-file deploy/.env config
docker compose -f deploy/compose.yaml --env-file deploy/.env up -d --build
docker compose -f deploy/compose.yaml --env-file deploy/.env logs -f meshcentral
```

The Compose build context points to the repository root, so the existing `docker/Dockerfile` and its build-time scripts are reused.

## Deployment rules

1. Pin production deployments to a reviewed Git tag or immutable image digest; avoid `latest`.
2. Keep `deploy/.env` out of Git. It may contain secrets in future configurations.
3. Persist all four volumes declared in Compose. The data volume is particularly important because it contains configuration and certificates.
4. Back up before image updates, and test restore procedures periodically.
5. Test HTTPS and HTTP redirect behavior, WebSocket upgrades, agent reconnects, remote desktop and file transfer after deployment.
6. If using a reverse proxy, expose only the ports needed by that topology. Configure the application to trust only the real proxy addresses, never arbitrary client-supplied forwarding headers.
7. Only enable iframe embedding with an explicit trusted origin policy.

## Branding

Put original logo/favicon/source artwork and additive style assets in `branding/`. Do not overwrite upstream protocol or authorization logic to achieve visual changes. Before linking assets from a template, inspect the active template variant and the version's supported custom-web directory behavior. Avoid loading third-party scripts from public CDNs in the authenticated admin UI unless explicitly required and reviewed.

## Update and recovery

- Stop MeshCentral before volume backup to get a consistent filesystem snapshot.
- Keep backup archives outside the live volume paths.
- Use the previous image tag for rollback, then restore data only if the new version changed persisted state incompatibly.
- Run the health check, login, agent reconnect, and remote session smoke tests before declaring an update complete.
