<#
Upload builds to itch.io with butler (playbook 0.4).
One-time: install butler, run `butler login`, copy tools\itch.env.example to tools\itch.env and fill it in.
Usage (from the project folder):
  powershell -ExecutionPolicy Bypass -File tools\upload.ps1 html      # or: windows, all
#>
param(
    [ValidateSet('html', 'windows', 'all')]
    [string]$Channel = 'html'
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$envFile = Join-Path $PSScriptRoot 'itch.env'
if (-not (Test-Path $envFile)) { throw "Missing tools\itch.env - copy tools\itch.env.example and fill it in." }
$cfg = @{}
foreach ($line in Get-Content $envFile) {
    if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$') { $cfg[$Matches[1]] = $Matches[2].Trim('"') }
}
$user = $cfg['ITCH_USER']
$game = if ($cfg['ITCH_GAME']) { $cfg['ITCH_GAME'] } else { 'shift-shift' }
if (-not $user -or $user -eq 'your-itch-username') { throw "Set ITCH_USER in tools\itch.env." }
if (-not (Get-Command butler -ErrorAction SilentlyContinue)) { throw "butler not found on PATH (https://itch.io/docs/butler/)." }

$version = (git describe --tags --always --dirty 2>$null)
if (-not $version) { $version = Get-Date -Format 'yyyyMMdd-HHmm' }

function Push-Build([string]$Path, [string]$Name) {
    $target = $user + '/' + $game + ':' + $Name
    Write-Host "Pushing $Path -> $target ($version)"
    & butler push $Path $target --userversion $version
    if ($LASTEXITCODE -ne 0) { throw "butler push failed for channel $Name" }
}

function Push-Html {
    if (-not (Test-Path 'Builds\WebGL\index.html')) { throw "No Builds\WebGL\index.html - run Shift+Shift > Build WebGL first." }
    Push-Build 'Builds\WebGL' 'html'
}

function Push-Windows {
    if (-not (Test-Path 'Builds\Windows')) { throw "No Builds\Windows - run Shift+Shift > Build Windows first." }
    $zip = 'Builds\ShiftShift-Windows.zip'
    if (Test-Path $zip) { Remove-Item $zip }
    Compress-Archive -Path 'Builds\Windows\*' -DestinationPath $zip
    Push-Build $zip 'windows'
}

switch ($Channel) {
    'html'    { Push-Html }
    'windows' { Push-Windows }
    'all'     { Push-Html; Push-Windows }
}
