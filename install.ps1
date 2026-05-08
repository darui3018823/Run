#Requires -Version 5.1

# Re-launch with pwsh 7+ when running under Windows PowerShell 5.1
if ($PSVersionTable.PSVersion.Major -lt 7) {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    $pwsh    = if ($pwshCmd) { $pwshCmd.Source } else { $null }
    if (-not $pwsh) {
        foreach ($p in @(
            "$env:ProgramFiles\PowerShell\7\pwsh.exe",
            "$env:ProgramFiles\PowerShell\7-preview\pwsh.exe"
        )) {
            if (Test-Path $p) { $pwsh = $p; break }
        }
    }
    if ($pwsh) {
        & $pwsh -ExecutionPolicy Bypass -File $PSCommandPath @args
        exit $LASTEXITCODE
    }
    Write-Warning "pwsh 7+ not found — continuing with PS $($PSVersionTable.PSVersion)"
}

$ErrorActionPreference = 'Stop'
$root       = $PSScriptRoot
$installDir = "$env:LOCALAPPDATA\Programs\Run"
$publishDir = Join-Path $root "publish"

Write-Host "Building..."
dotnet publish $root -c Release -o $publishDir --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Stop existing instance gracefully before overwriting
$proc = Get-Process -Name Run -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Stopping existing instance..."
    $proc | Stop-Process -Force
    Start-Sleep -Milliseconds 800
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item "$publishDir\*" $installDir -Recurse -Force

$exe = Join-Path $installDir "Run.exe"
Write-Host "Installed -> $exe"
Start-Process $exe
Write-Host "Done. Configure startup from the tray icon."
