# Optional Cloudflare Pages landing/download page

This directory is for a static landing page, documentation and signed-client download links only.

- It is not the MeshCentral server.
- Never put server private keys, MeshCentral database files, enrollment secrets or API tokens here.
- Do not proxy agent WebSockets or management operations through an arbitrary Pages function as a substitute for MeshCentral.
- Point the console link to the HTTPS origin hosting MeshCentral.

Suggested deployment: connect this directory (or a separate frontend repository) to Cloudflare Pages. Keep the MeshCentral Node.js service on a VM/container host and optionally protect browser access with Cloudflare Access or a correctly configured Cloudflare Tunnel.