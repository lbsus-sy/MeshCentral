# Windows agent installer (GUI EXE)

This is a small, self-contained Windows x64 GUI-subsystem executable. Its application manifest requests administrator elevation, so Windows shows its normal UAC prompt. **After the user approves UAC, the program automatically runs the agent install command without opening its own window or showing follow-up popups.** It is not designed to bypass or suppress Windows security prompts.

## Use it

1. From your own MeshCentral server, download the Windows agent EXE generated for the intended device group/server.
2. Put that file (normally named `MeshAgent*.exe`) in the same folder as `LakersMeshAgentInstaller.exe`.
3. Run `LakersMeshAgentInstaller.exe` and approve the standard UAC prompt only if you trust the agent and intend to authorize persistent remote management.
4. After UAC approval, installation runs automatically in the background. There is no second confirmation window or console window.
5. Check `%ProgramData%\LakersMeshCentral\installer.log` for the result, then verify that the device appears in the intended MeshCentral server.

If you prefer not to place the files together, pass the full agent path as the first argument. For removal, run the elevated installer with `--uninstall` and optionally pass the installed agent executable path, for example:

```powershell
LakersMeshAgentInstaller.exe --uninstall "C:\Program Files\Mesh Agent\MeshAgent.exe"
```

Without an explicit path, uninstall mode uses a single `MeshAgent*.exe` found beside the wrapper. The wrapper refuses to guess if multiple candidates exist. A non-zero exit code and details are written to the local log; no app message boxes are shown. The MeshAgent command may take up to three minutes before the wrapper times out.

## Build

On Windows with the .NET 8 SDK:

```powershell
dotnet publish .\customization\client\LakersAgentInstaller.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\installer
```

The output is a self-contained x64 GUI application with no console window. GitHub Actions is configured to build it; check the workflow result before assuming a successful build. This repository does not sign the executable, so SmartScreen or reputation warnings may still appear.

## Important security notice

**MeshCentral is remote-management software.** A generated agent can enable remote desktop, terminal, file transfer, and other management functions according to server permissions. Install only on a device you own or are explicitly authorized to administer, and only when you trust the MeshCentral server that generated the agent. The UAC prompt is the user's authorization point; approving it permits a persistent Windows service to be installed. Inform the device owner/administrator. Never use this tool for covert access, unauthorized monitoring, credential collection, or security evasion.

The installer does not bundle a universal agent, silently download one, hide the installed service, disable security software, or suppress UAC. Agent executables are server-specific. File-size and PE-header checks are basic format checks, not proof of trust; only use an agent obtained directly from your own trusted server.

## Limits

- This wrapper runs the documented MeshAgent `-fullinstall` or `-fulluninstall` command.
- A zero exit code does not guarantee the device has connected; verify in the MeshCentral UI.
- The EXE remains unsigned until a trusted code-signing certificate and protected release process are configured.
- CI artifacts are available only after the relevant GitHub Actions workflow completes successfully and are retained for 14 days.
