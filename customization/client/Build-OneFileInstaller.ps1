param(
    [Parameter(Mandatory = $true)][string]$AgentPath,
    [string]$OutputPath = ".\dist\LakersMeshAgentInstaller.exe"
)
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$agent = (Resolve-Path $AgentPath).Path
$item = Get-Item $agent
if ($item.Length -lt 4096 -or $item.Length -gt 250MB) { throw "Agent file size is outside the expected range." }
$s = [System.IO.File]::OpenRead($agent)
try { if ($s.ReadByte() -ne 0x4D -or $s.ReadByte() -ne 0x5A) { throw "AgentPath is not a Windows PE executable." } } finally { $s.Dispose() }
$out = [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $OutputPath))
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $out) | Out-Null
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("lakers-build-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    $project = Join-Path $repoRoot "customization\client\LakersAgentInstaller.csproj"
    dotnet publish $project -c Release -r win-x64 --self-contained true "-p:MeshAgentPath=$agent" -o $temp
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
    $built = Join-Path $temp "LakersMeshAgentInstaller.exe"
    if (!(Test-Path $built)) { throw "Build output EXE was not created." }
    Copy-Item $built $out -Force
    Write-Host "Created one-file installer: $out"
    Write-Host "This EXE embeds the server-generated MeshCentral agent. Share only with authorized users/devices."
} finally { Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue }
