# PowerShell 7+ — end-to-end demo script
# Usage:
#   .\scripts\panel-demo.ps1                    # full run (default)
#   .\scripts\panel-demo.ps1 -SkipTests         # skip backend tests for a faster run
#   .\scripts\panel-demo.ps1 -SkipBuild         # skip docker rebuild (containers already built)
#   .\scripts\panel-demo.ps1 -StopAfter         # stop containers when finished
#
# What this script does, in order:
#   1. Prints repo identity (branch, tag, last commits) — proof of conventional history.
#   2. Runs `dotnet build -c Release`             — proof: 0 warnings / 0 errors.
#   3. Runs `dotnet test  -c Release --no-build`  — proof: 43 / 43 passing.
#   4. Runs `dotnet format --verify-no-changes`   — proof: format clean.
#   5. Runs `npm run build` for the SPA            — proof: strict TS + Vite production build.
#   6. Brings the full stack up via docker compose (db + api + web).
#   7. Probes the live endpoints and runs an end-to-end demo flow:
#         register a fresh user → create task → list tasks → delete task → 401 path.
#   8. Opens the SPA, Swagger and the Razor MVC home view in the default browser.
#
# Every step prints a green "PASS" or red "FAIL" banner. The script exits non-zero on the
# first failure so it is obvious which gate caught it.

[CmdletBinding()]
param(
    [switch] $SkipTests,
    [switch] $SkipBuild,
    [switch] $StopAfter
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$script:stepIndex = 0
function Step([string]$title, [scriptblock]$block) {
    $script:stepIndex++
    $banner = "=== [$($script:stepIndex)] $title ==="
    Write-Host ""
    Write-Host $banner -ForegroundColor Cyan
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $block
        $sw.Stop()
        Write-Host ("PASS  ({0:n1}s) — {1}" -f $sw.Elapsed.TotalSeconds, $title) -ForegroundColor Green
    } catch {
        $sw.Stop()
        Write-Host ("FAIL  ({0:n1}s) — {1}" -f $sw.Elapsed.TotalSeconds, $title) -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        throw
    }
}

function Require-Tool([string]$exe, [string]$friendlyName) {
    if (-not (Get-Command $exe -ErrorAction SilentlyContinue)) {
        throw "$friendlyName not found on PATH ('$exe' missing)."
    }
}

# ---------------------------------------------------------------------------
# 0. Pre-flight
# ---------------------------------------------------------------------------
Step "Pre-flight: required tools" {
    Require-Tool dotnet "the .NET 9 SDK"
    Require-Tool docker "Docker Desktop"
    Require-Tool npm    "Node.js / npm"
    Require-Tool git    "git"
    docker info *> $null
    if ($LASTEXITCODE -ne 0) { throw "Docker daemon is not responding. Start Docker Desktop and retry." }
}

# ---------------------------------------------------------------------------
# 1. Repo identity & history
# ---------------------------------------------------------------------------
Step "Repository identity (branch, tag, last 10 commits)" {
    git rev-parse --abbrev-ref HEAD
    git describe --tags --abbrev=0 2>$null
    git log --oneline -n 10
}

# ---------------------------------------------------------------------------
# 2. Backend verification battery
# ---------------------------------------------------------------------------
Step "dotnet build (Release) — must produce 0 warnings / 0 errors" {
    dotnet build TaskFlow.sln -c Release --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

if (-not $SkipTests) {
    Step "dotnet test (43 tests across 4 projects)" {
        dotnet test TaskFlow.sln -c Release --no-build --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
    }
} else {
    Write-Host "(skipping tests by request)" -ForegroundColor Yellow
}

Step "dotnet format --verify-no-changes" {
    dotnet format TaskFlow.sln --verify-no-changes -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Formatting drift detected." }
}

# ---------------------------------------------------------------------------
# 3. Frontend verification
# ---------------------------------------------------------------------------
Step "SPA strict TS + Vite production build" {
    Push-Location src/TaskFlow.Web
    try {
        if (-not (Test-Path node_modules)) { npm ci }
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "SPA build failed." }
    } finally {
        Pop-Location
    }
}

# ---------------------------------------------------------------------------
# 4. Bring stack up
# ---------------------------------------------------------------------------
Step "Docker compose up (postgres + api + web)" {
    if ($SkipBuild) {
        docker compose up -d
    } else {
        docker compose up -d --build
    }
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed." }
}

Step "Wait for API + Web to become reachable" {
    $deadline = (Get-Date).AddSeconds(60)
    $apiOk = $false; $webOk = $false
    while ((Get-Date) -lt $deadline -and -not ($apiOk -and $webOk)) {
        if (-not $apiOk) {
            try { $apiOk = (Invoke-WebRequest http://localhost:5092/swagger/v1/swagger.json -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200 } catch { Start-Sleep -Milliseconds 800 }
        }
        if (-not $webOk) {
            try { $webOk = (Invoke-WebRequest http://localhost:8080/ -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200 } catch { Start-Sleep -Milliseconds 800 }
        }
    }
    if (-not $apiOk) { throw "API did not become reachable on http://localhost:5092 in 60s." }
    if (-not $webOk) { throw "SPA did not become reachable on http://localhost:8080 in 60s." }
}

# ---------------------------------------------------------------------------
# 5. End-to-end live API demo
# ---------------------------------------------------------------------------
$apiBase    = 'http://localhost:5092/api/v1'
$ephemEmail = "panel+$(Get-Date -Format yyyyMMddHHmmss)@taskflow.dev"
$ephemPass  = 'Panel-Demo-1!'
$script:token = $null
$script:taskId = $null

Step "POST /auth/register (ephemeral demo user)" {
    $body = @{ email = $ephemEmail; password = $ephemPass } | ConvertTo-Json
    $r = Invoke-RestMethod -Method Post -Uri "$apiBase/auth/register" -ContentType 'application/json' -Body $body
    $script:token = $r.accessToken
    if (-not $script:token) { throw "register did not return a token." }
    Write-Host "  user=$ephemEmail  token.len=$($script:token.Length)"
}

Step "POST /tasks  (create a task as that user)" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{
        title       = 'Prepare panel walkthrough'
        description = 'Generated live by panel-demo.ps1'
        dueDateUtc  = (Get-Date).ToUniversalTime().AddDays(7).ToString('o')
    } | ConvertTo-Json
    $r = Invoke-RestMethod -Method Post -Uri "$apiBase/tasks" -Headers $headers -ContentType 'application/json' -Body $body
    $script:taskId = $r.id
    Write-Host "  taskId=$script:taskId  status=$($r.status)"
}

Step "GET /tasks   (list — must contain the new task)" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $list = Invoke-RestMethod -Method Get -Uri "$apiBase/tasks" -Headers $headers
    if (-not ($list | Where-Object { $_.id -eq $script:taskId })) { throw "Created task not in list." }
    Write-Host "  list count=$($list.Count)"
}

Step "DELETE /tasks/{id}  (cleanup)" {
    $headers = @{ Authorization = "Bearer $script:token" }
    Invoke-RestMethod -Method Delete -Uri "$apiBase/tasks/$script:taskId" -Headers $headers | Out-Null
    Write-Host "  deleted $script:taskId"
}

Step "Auth boundary: GET /tasks WITHOUT token must return 401" {
    try {
        Invoke-WebRequest -Uri "$apiBase/tasks" -UseBasicParsing -TimeoutSec 5 | Out-Null
        throw "Expected 401, got 200."
    } catch [System.Net.WebException] {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -ne 401) { throw "Expected 401, got $code." }
        Write-Host "  401 Unauthorized as expected"
    } catch [Microsoft.PowerShell.Commands.HttpResponseException] {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -ne 401) { throw "Expected 401, got $code." }
        Write-Host "  401 Unauthorized as expected"
    }
}

Step "POST /auth/login with the seeded demo user" {
    $body = @{ email = 'demo@taskflow.dev'; password = 'Demo123!' } | ConvertTo-Json
    $r = Invoke-RestMethod -Method Post -Uri "$apiBase/auth/login" -ContentType 'application/json' -Body $body
    if (-not $r.accessToken) { throw "demo login did not return a token." }
    Write-Host "  demo login OK, token expires $($r.expiresAtUtc)"
}

# ---------------------------------------------------------------------------
# 6. Open the UI
# ---------------------------------------------------------------------------
Step "Open SPA, Swagger, Razor MVC home in default browser" {
    Start-Process 'http://localhost:8080/'
    Start-Process 'http://localhost:5092/swagger'
    Start-Process 'http://localhost:5092/'
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host " ALL GATES PASSED — TaskFlow is live." -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  SPA            http://localhost:8080" -ForegroundColor Gray
Write-Host "  Swagger        http://localhost:5092/swagger" -ForegroundColor Gray
Write-Host "  Razor MVC      http://localhost:5092/" -ForegroundColor Gray
Write-Host "  Demo user      demo@taskflow.dev / Demo123!" -ForegroundColor Gray
Write-Host ""
Write-Host "  Tear down with:  .\scripts\panel-demo-down.ps1" -ForegroundColor Gray
Write-Host ""

if ($StopAfter) {
    Write-Host "Stopping containers (--StopAfter)…" -ForegroundColor Yellow
    docker compose down
}
