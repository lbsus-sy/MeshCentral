# Lakers MeshCentral customization

This repository is a customized fork of [MeshCentral](https://github.com/Ylianst/MeshCentral), the open-source web-based remote device management platform. Upstream license and attribution notices are preserved.

> **Security and consent warning:** MeshCentral is remote-management software. Depending on server permissions, an enrolled agent can enable remote desktop, terminal, file transfer, and other device-management operations. Deploy only on devices you own or are explicitly authorized to administer. Inform device owners/administrators, secure the server with HTTPS and strong authentication, keep backups, and maintain a tested uninstall procedure. Do not use this project for covert access, unauthorized surveillance, credential theft, or security-control evasion.

## Customization quick links

- [Feature overview and disclaimer](customization/README.md)
- [Lakers purple/gold theme](customization/branding/web/public/styles/custom.css)
- [Wispbyte Node.js launcher](customization/wispbyte/README.md)
- [Windows GUI agent installer](customization/client/README.md)
- [Wallpaper, diagnostics and crash dump tools](customization/tools/README.md)
- [Deployment guide](customization/CUSTOMIZATION.md)
- [Feature roadmap and limitations](customization/ROADMAP.md)

## Windows EXE builds

GitHub Actions builds two unsigned, self-contained Windows x64 GUI artifacts: `LakersMeshAgentInstaller.exe` and `LakersDesktopTools.exe`. Open the repository's Actions tab, select the latest successful **Windows GUI client packaging** workflow run, and download the artifacts. Artifacts expire after 14 days. They are not signed by this repository, so Windows may show an unknown-publisher warning.

The installer requests UAC elevation, shows a consent screen, and expects the Windows agent EXE downloaded from your own trusted MeshCentral server. It does not bundle a universal agent because generated agents are configured for their server. Verify that the device appears in the server UI after installation.

The desktop tools GUI changes wallpaper for the current user, collects local read-only diagnostics, and can invoke a locally installed Microsoft `cdb.exe` to analyze a user-selected crash dump. It does not upload reports or dumps.

## Wispbyte compatibility

The launcher reads the provider's assigned runtime port and tries common public-host environment variables. If the host does not expose a public hostname to the process, set `MESH_HOSTNAME` manually. A Node.js runtime alone does not guarantee support for persistent storage, WebSocket upgrades, TLS proxying, or all agent traffic; test these features on your actual allocation before enrolling production devices.

## Status

These are source changes and configured CI builds, not a claim of a completed live Wispbyte deployment. Remote wallpaper is not yet integrated as a first-party MeshCentral UI action; the included desktop EXE changes the current signed-in user's local wallpaper. Diagnostics and crash dump analysis are local and user-initiated. No signed release or end-to-end agent session has been certified yet.
