# Wispbyte deployment (experimental)

This is a best-effort path for testing the Lakers-branded MeshCentral fork on Wispbyte's Node.js app hosting. It is **not a guarantee that full MeshCentral agent connectivity will work** on the free tier.

## Important limitations

Wispbyte advertises free app hosting with 512 MB RAM and 1 GB disk. MeshCentral is a persistent remote-management server, not just a static website: it needs durable storage for its database, certificates, files and configuration, and managed devices need reliable WebSocket/agent connectivity. Wispbyte's general Node.js hosting documentation does not guarantee arbitrary inbound TCP ports or persistent-volume semantics for every free app server.

Before adding any managed devices, confirm in the Wispbyte panel/support that your instance provides:
- A stable public HTTPS URL and a supported externally reachable WebSocket endpoint.
- Persistent storage across restarts/redeploys for `meshcentral-data`, `meshcentral-files`, and backups.
- The public endpoint/port behavior required by MeshCentral agents; do not assume the platform's HTTP port also exposes every MeshCentral protocol.
- Enough memory and disk for the MeshCentral package and your intended device count.

If any of those are missing, use Wispbyte only for a landing page and host MeshCentral on a VPS/VM with persistent disk and a supported public endpoint. Cloudflare Pages is for static pages; it does not run the long-lived MeshCentral server.

## Setup outline

1. Create a Wispbyte **Node.js** app server and check the assigned public URL and runtime port in the panel.
2. Deploy this repository only if the panel permits GitHub integration or file upload for a Node.js app. Install the repository's production dependencies using the panel's startup/package controls.
3. Keep `meshcentral-data`, `meshcentral-files`, and backups in storage that survives restarts. Back them up outside the host before upgrading. The launcher writes config under `meshcentral-data`; it does not create persistent storage for you.
4. Set the startup command to `node customization/wispbyte/start.js`. The launcher reads `PORT` (then `SERVER_PORT`/`APP_PORT`) and tries public-host variables: `MESH_HOSTNAME`, `PUBLIC_HOSTNAME`, `EXTERNAL_HOSTNAME`, `APP_HOSTNAME`, `SERVER_DOMAIN`, `PUBLIC_URL`, `APP_URL`, `RENDER_EXTERNAL_URL`, `RAILWAY_PUBLIC_DOMAIN`, and a `FLY_APP_NAME`-derived host. If Wispbyte does not expose its public hostname to the Node.js process, set `MESH_HOSTNAME` to the assigned public hostname (domain only, no scheme or port). A public hostname is necessary for MeshCentral's agent/server identity; the code cannot infer a domain the provider does not expose.
5. The launcher enables TLS offload for the host's HTTPS proxy and listens on the assigned internal port. If the provider documents a stable proxy IP, set `MESH_TRUSTED_PROXY` to that IP so MeshCentral can limit trust of forwarded headers. If it is unset, the launcher assumes the provider isolates the origin listener behind its HTTPS proxy; do **not** use that default if clients can reach the origin directly. The launcher disables public account creation, applies Lakers branding and preserves the rest of an existing `meshcentral-data/config.json`. Back up the data directory before first start or upgrade.
6. Test browser login, WebSocket remote desktop, file transfer, agent enrollment/reconnect after restart, and data persistence with a disposable test device before using real devices.
7. Immediately create the first administrator account, enable MFA, disable public account creation, and keep the server patched.

## What the launcher does and does not do

The launcher copies the Lakers CSS into MeshCentral's supported `meshcentral-web/public/styles/custom.css` override directory, then starts the repository's MeshCentral server with the generated/preserved config. It does not provide a platform-level persistent volume or expose missing TCP ports. MeshCentral's Intel AMT/MPS service and any feature requiring a separate raw TCP port may remain unavailable unless Wispbyte explicitly supports those ports. If browser WebSockets or normal agent connections fail, do not try to work around the platform's network controls; move the server to a VPS/VM with the required ports.

## Current recommendation

Use the Docker Compose deployment in `customization/deploy/` on a Linux VM/VPS for a dependable full-featured server. Wispbyte's free app tier is resource-constrained and may not expose the networking/persistence MeshCentral requires. Never put administrator credentials or reusable enrollment secrets in public repository files.
