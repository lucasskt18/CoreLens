$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Write-Host "CoreLens — starting local stack" -ForegroundColor Cyan

$docker = Get-Command docker -ErrorAction SilentlyContinue
if ($docker) {
    Write-Host "Starting TimescaleDB..."
    docker compose up -d
} else {
    Write-Host "Docker not found. Start TimescaleDB yourself (localhost:5432, user/pass/db corelens)." -ForegroundColor Yellow
}

$env:DOTNET_ROOT = "C:\Program Files\dotnet"

Start-Process -FilePath $dotnet -ArgumentList "run --project `"$root\src\CoreLens.Api\CoreLens.Api.csproj`"" -WorkingDirectory $root
Start-Sleep -Seconds 2
Start-Process -FilePath $dotnet -ArgumentList "run --project `"$root\src\CoreLens.Agent\CoreLens.Agent.csproj`"" -WorkingDirectory $root
Start-Process -FilePath "npm" -ArgumentList "start" -WorkingDirectory "$root\frontend"

Write-Host "API http://localhost:5080"
Write-Host "Dashboard http://localhost:4200"
