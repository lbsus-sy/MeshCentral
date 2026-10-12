# Opt-in Windows helper tools

This directory contains local-only tools; they are not a remote command module integrated into the MeshCentral web UI.

## GUI EXE

`LakersDesktopTools.csproj` builds a no-console Windows GUI for three tasks:

- **Wallpaper:** choose a local image and confirm; it changes the desktop wallpaper for the currently signed-in user only. It does not change the lock screen or other users' profiles.
- **Diagnostics:** exports process metadata, service query output, and recent System/Application warning/error events to a JSON file selected by the user. It does not upload the report, terminate processes, or change system settings.
- **Crash dump analysis:** select a local `.dmp` file and a local Microsoft Debugging Tools for Windows `cdb.exe`; the tool runs `!analyze -v` and saves the output locally. Install the debugger from Microsoft's official distribution yourself. No dump is uploaded by this app. Dumps may contain credentials, personal data, memory contents, and sensitive file paths; review before sharing.

The GUI runs at normal user privilege (no UAC prompt required). Some process paths and event logs may be unavailable without elevated permissions. The app reports access errors rather than elevating itself.

## Build

On Windows with .NET 8 SDK:

```powershell
dotnet publish .\customization\tools\LakersDesktopTools.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\desktop-tools
```

GitHub Actions is configured to build a single-file, self-contained `LakersDesktopTools.exe` artifact; check the workflow result before assuming a successful build. The binary is unsigned until a trusted code-signing release process is configured.

## PowerShell helpers

The scripts `Collect-Diagnostics.ps1` and `Set-UserWallpaper.ps1` remain available for admins who prefer scripts. Run wallpaper changes in the intended interactive user's session, not as LocalSystem. Diagnostics may contain sensitive usernames, paths, hostnames, and event messages; review the report before sharing.

## Scope and safety

The tools are local and user-initiated. No remote BSOD/kernel crash trigger, arbitrary process-kill endpoint, credential collection, covert install, or upload-to-third-party behavior is included. For crash reproduction or kernel debugging, use a disposable isolated test VM with local confirmation.
