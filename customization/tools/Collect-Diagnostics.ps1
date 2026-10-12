[CmdletBinding()]
param(
    [ValidateRange(1, 168)]
    [int]$Hours = 24,
    [string]$OutputDirectory = (Join-Path $env:USERPROFILE 'Desktop\MeshCentral-Diagnostics')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Read-only collector: it does not stop processes, change settings, or upload data.
$cutoff = (Get-Date).AddHours(-$Hours)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outFile = Join-Path $OutputDirectory "diagnostics-$stamp.json"

$processes = Get-Process | ForEach-Object {
    $proc = $_
    $path = $null
    try { $path = $proc.Path } catch { }
    [pscustomobject]@{
        Name = $proc.ProcessName
        Id = $proc.Id
        CPUSeconds = if ($null -ne $proc.CPU) { [math]::Round($proc.CPU, 2) } else { $null }
        WorkingSetMB = [math]::Round($proc.WorkingSet64 / 1MB, 1)
        Path = $path
    }
} | Sort-Object Name, Id

$services = Get-CimInstance Win32_Service | Select-Object Name, DisplayName, State, StartMode, ProcessId

$eventLogs = @{}
foreach ($logName in @('System', 'Application')) {
    try {
        $eventLogs[$logName] = @(
            Get-WinEvent -FilterHashtable @{ LogName = $logName; StartTime = $cutoff; Level = @(1, 2, 3) } -ErrorAction Stop |
            Select-Object -First 500 TimeCreated, Id, LevelDisplayName, ProviderName, Message
        )
    } catch {
        $eventLogs[$logName] = @([pscustomobject]@{ Warning = "Unable to read $logName log: $($_.Exception.Message)" })
    }
}

$report = [pscustomobject]@{
    SchemaVersion = 1
    CollectedAt = (Get-Date).ToString('o')
    ComputerName = $env:COMPUTERNAME
    UserName = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    WindowHours = $Hours
    Processes = @($processes)
    Services = @($services)
    Events = $eventLogs
    Notes = @(
        'Read-only collection; no process was terminated and no settings were changed.',
        'Review the report for usernames, file paths, hostnames or other sensitive data before sharing.',
        'This script does not upload the report anywhere.'
    )
}
$report | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $outFile -Encoding UTF8
Write-Host "Diagnostics saved to: $outFile"
Write-Host 'Review the JSON for sensitive information before sharing it.'
