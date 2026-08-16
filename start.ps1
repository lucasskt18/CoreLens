$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

$env:DOTNET_ROOT = "C:\Program Files\dotnet"
$env:PATH = "C:\Program Files\dotnet;" + $env:PATH

Write-Host "CoreLens - subindo o ambiente local" -ForegroundColor Cyan

function Wait-Docker {
    for ($i = 1; $i -le 30; $i++) {
        docker info *> $null
        if ($LASTEXITCODE -eq 0) {
            return
        }
        Write-Host "Aguardando o Docker Desktop ($i/30)..."
        Start-Sleep -Seconds 2
    }
    throw "Abra o Docker Desktop e espere ficar Running. Depois rode este script de novo."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker nao esta no PATH. Abra o Docker Desktop e um Prompt novo."
}

Wait-Docker
Write-Host "Subindo TimescaleDB..."
docker compose up -d

Write-Host "Esperando o banco ficar pronto..."
for ($i = 1; $i -le 40; $i++) {
    docker exec corelens-timescaledb pg_isready -U corelens -d corelens *> $null
    if ($LASTEXITCODE -eq 0) {
        break
    }
    if ($i -eq 40) {
        throw "TimescaleDB nao ficou pronto. Veja o container no Docker Desktop."
    }
    Start-Sleep -Seconds 2
}

$apiProject = Join-Path $root "src\CoreLens.Api\CoreLens.Api.csproj"
$agentProject = Join-Path $root "src\CoreLens.Agent\CoreLens.Agent.csproj"
$frontendDir = Join-Path $root "frontend"

Write-Host "Abrindo API, Agent e dashboard em janelas novas..."
Start-Process -FilePath $dotnet -ArgumentList @("run", "--project", $apiProject) -WorkingDirectory $root
Start-Sleep -Seconds 3
Start-Process -FilePath $dotnet -ArgumentList @("run", "--project", $agentProject) -WorkingDirectory $root
Start-Process -FilePath "npm" -ArgumentList @("start") -WorkingDirectory $frontendDir

Write-Host ""
Write-Host "Pronto. Deixe as janelas abertas."
Write-Host "API        http://localhost:5080"
Write-Host "Dashboard  http://localhost:4200"
