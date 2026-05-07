# PowerShell 7+ — tear down the demo stack.
# Usage:
#   .\scripts\panel-demo-down.ps1            # stop containers, keep volume
#   .\scripts\panel-demo-down.ps1 -Purge     # also remove the postgres volume (resets seed)

[CmdletBinding()]
param(
    [switch] $Purge
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

if ($Purge) {
    Write-Host "docker compose down -v  (removing postgres volume)" -ForegroundColor Yellow
    docker compose down -v
} else {
    Write-Host "docker compose down" -ForegroundColor Yellow
    docker compose down
}
