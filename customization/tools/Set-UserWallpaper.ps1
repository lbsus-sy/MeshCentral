[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ImagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Run in the intended interactive user's session, not as LocalSystem.
# This helper changes only the current user's desktop wallpaper.
$resolved = (Resolve-Path -LiteralPath $ImagePath).Path
$extension = [IO.Path]::GetExtension($resolved).ToLowerInvariant()
if ($extension -notin @('.bmp', '.jpg', '.jpeg', '.png')) {
    throw 'Use a local .bmp, .jpg, .jpeg, or .png image file.'
}

if (-not $PSCmdlet.ShouldProcess([Environment]::UserName, "Set desktop wallpaper to '$resolved'")) {
    return
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class WallpaperNative {
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SystemParametersInfo(int action, int param, string value, int winIni);
}
'@

$desktopKey = 'HKCU:\Control Panel\Desktop'
Set-ItemProperty -LiteralPath $desktopKey -Name Wallpaper -Value $resolved
Set-ItemProperty -LiteralPath $desktopKey -Name WallpaperStyle -Value '10' # Fill
Set-ItemProperty -LiteralPath $desktopKey -Name TileWallpaper -Value '0'
$SPI_SETDESKWALLPAPER = 0x0014
$SPIF_UPDATEINIFILE = 0x01
$SPIF_SENDCHANGE = 0x02
if (-not [WallpaperNative]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $resolved, ($SPIF_UPDATEINIFILE -bor $SPIF_SENDCHANGE))) {
    throw "Windows rejected the wallpaper change (Win32 error $([Runtime.InteropServices.Marshal]::GetLastWin32Error()))."
}
Write-Host "Wallpaper changed for interactive user '$([Environment]::UserName)'."
