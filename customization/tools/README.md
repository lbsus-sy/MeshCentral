# Opt-in Windows helper tools

These are initial, local helper scripts—not yet a remote command module integrated into the MeshCentral UI.

## Read-only diagnostics

Run in PowerShell:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Collect-Diagnostics.ps1 -Hours 24
```

The script exports process metadata, service state and recent System/Application warnings and errors to a JSON file. It does not upload the report or terminate processes. Review the output for sensitive usernames, paths, hostnames and event-log content before sharing it. Run elevated only if you specifically need additional event-log visibility.

## Wallpaper helper

Copy the image to the target Windows machine and run the script **in the intended interactive user's session**:

```powershell
.\Set-UserWallpaper.ps1 -ImagePath 'C:\Path\To\wallpaper.jpg' -WhatIf
.\Set-UserWallpaper.ps1 -ImagePath 'C:\Path\To\wallpaper.jpg'
```

The first command previews the action. The second prompts for confirmation and changes the current user's wallpaper. It does not upload images, alter the lock screen, or change other users' profiles. A service running as LocalSystem cannot safely assume that HKCU refers to the logged-in desktop user, so do not invoke this helper from a service without a separately designed, consent-based user-session broker.

## Not included

No remote BSOD, kernel crash trigger, stealth install, arbitrary process-kill endpoint, credential collection, or upload-to-third-party behavior. Application crash reproduction and kernel debugging belong in a disposable isolated test VM with local confirmation.
