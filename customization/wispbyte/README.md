# Wispbyte deployment (experimental)

This is a best-effort path for testing the Lakers-branded MeshCentral fork on Wispbyte's free Node.js app hosting. It is **not a guarantee that full MeshCentral agent connectivity will work** on the free tier.

## Important limitations

Wispbyte currently advertises a free app tier with 512 MB RAM and 1 GB disk. MeshCentral is a persistent remote-management server, not just a static website: it needs durable storage for its database, certificates, files and configuration, and managed devices need reliable WebSocket/agent connectivity. Wispbyte's general Node.js hosting documentation does not promise arbitrary inbound TCP ports or persistent-volume semantics for every free app server.

Before adding any managed devices, confirm in the Wispbyte panel/support that your instance provides:
- A stable public HTTPS URL and a supported externally reachable WebSocket endpoint.
- Persistent storage across restarts/redeploys for `meshcentral-data`, `meshcentral-files`, and backups.
- The public endpoint/port behavior required by MeshCentral agents; do not assume the platform's HTTP port also exposes every MeshCentral protocol.
- Enough memory and disk for the MeshCentral package and your intended device count.

If any of those are missing, use Wispbyte only for a landing page and host MeshCentral on a small VPS/VM with persistent disk and a supported public endpoint. Cloudflare Pages is for static pages; it does not run the long-lived MeshCentral server.

## Setup outline

1. Create a Wispbyte **Node.js** app server and check its assigned public URL/port in the panel.
2. Deploy this repository only if the panel permits GitHub integration or file upload for a Node.js app. Install the repository's production dependencies using the panel's startup/package controls.
3. Keep `meshcentral-data`, `meshcentral-files`, and backups in storage that survives restarts. Back them up outside the host before upgrading.
4. Set the app's startup command to the repository's supported MeshCentral startup command and configure the listening port and hostname to match Wispbyte's actual allocation. Do not blindly assume port 443 or port 3000.
5. Test browser login, WebSocket remote desktop, file transfer, agent enrollment/reconnect after restart, and data persistence with a disposable test device before using real devices.
6. Immediately create the first administrator account, enable MFA, disable public account creation, and keep the server patched.

## Current recommendation

Use the Docker Compose deployment in `customization/deploy/` on a Linux VM/VPS for a dependable full-featured server. Wispbyte's free app tier is resource-constrained and may not expose the networking/persistence MeshCentral requires. Never put administrator credentials or reusable enrollment secrets in public repository files.
