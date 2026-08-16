$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $root "CoreLens.sln"))) {
    $root = Split-Path -Parent $MyInvocation.MyCommand.Path
    $root = Split-Path -Parent $root
}

Set-Location (Join-Path $root "frontend")
npx ng build --configuration=production

$browser = Join-Path $root "frontend\dist\frontend\browser"
$legacy = Join-Path $root "frontend\dist\frontend"
$from = if (Test-Path (Join-Path $browser "index.html")) { $browser } else { $legacy }
$to = Join-Path $root "src\CoreLens.Api\wwwroot"

if (-not (Test-Path (Join-Path $from "index.html"))) {
    throw "Angular build did not produce index.html."
}

New-Item -ItemType Directory -Force -Path $to | Out-Null
Get-ChildItem $to -Force | Where-Object { $_.Name -ne ".gitkeep" } | Remove-Item -Recurse -Force
Copy-Item (Join-Path $from "*") $to -Recurse -Force
Write-Host "UI copied to $to"
