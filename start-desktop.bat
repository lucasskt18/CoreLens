@echo off
cd /d "%~dp0"
set DOTNET_ROOT=C:\Program Files\dotnet
set PATH=C:\Program Files\dotnet;%PATH%
dotnet run --project "%~dp0src\CoreLens.Desktop\CoreLens.Desktop.csproj"
if errorlevel 1 pause
