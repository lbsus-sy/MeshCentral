# Lakers Purple & Gold MeshCentral UI + Client Roadmap

## Current working scope
- UI theme: purple/gold CSS override.
- Deployment: persistent Node.js MeshCentral server, separate from optional Cloudflare Pages landing/download UI.
- Client packaging: Windows MSI/EXE should install the supported MeshAgent as a Windows service, run quietly without a console after explicit user/admin consent, be signed, support uninstall and log collection.
- Debug tools: opt-in diagnostics with visible authorization, audit events, bounded outputs and reversible actions.

## Safe first-run Windows installer design
1. The user runs a signed installer with an explicit install/consent step.
2. The installer displays the server FQDN and device-management purpose, then verifies the signed payload.
3. Normal post-install operation runs without a console window; installation failures write logs to `%ProgramData%/YourProduct/Logs`.
4. Service start, restart, uninstall and enrollment are idempotent and do not weaken Defender, UAC or other security controls.
5. Enrollment tokens are short-lived and never embedded as reusable secrets in public installers. Prefer a generated, scoped installer per tenant or MeshCentral's supported agent invitation workflow.
6. Provide a visible Programs & Features entry, signed uninstaller and documented service removal. No stealth, persistence evasion or hidden remote access.

## Custom feature phases
- Phase A: Lakers UI branding and deployment templates.
- Phase B: signed agent installer/build pipeline and safe auto-enrollment.
- Phase C: wallpaper management through a consented, permission-scoped module with preview, size/type limits, rollback and audit log.
- Phase D: read-only diagnostics — process inventory, CPU/memory, event logs, minidumps/crash dumps when authorized, and a WinDbg-inspired read-only dump summary.
- Phase E: destructive test actions remain disabled by default and require explicit local/admin confirmation in isolated test VMs. Do not add one-click arbitrary process termination or remote BSOD to a general-purpose production agent.

## Deployment boundary
MeshCentral's Node.js server and persistent database/data directories must remain on a supported VM/container host. Cloudflare Pages can host the public landing/download page. Cloudflare Tunnel can expose browser HTTPS/WebSockets only after validating the exact MeshCentral topology and agent compatibility. Intel AMT/MPS TCP 4433 and other raw TCP paths may need direct/routed access or a separate TCP-capable design.

See `customization/CUSTOMIZATION.md` for the current runbook and validation status.