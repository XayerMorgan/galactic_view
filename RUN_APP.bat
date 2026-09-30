@echo off
if not exist "%~dp0CosmicZoomEngine_App\CosmicZoomEngine.exe" (
  echo No local player build found.
  echo Download a Windows release at https://github.com/XayerMorgan/galactic_view/releases/latest
  echo Or build from source using scripts\build-release.ps1.
  pause
  exit /b 1
)
cd /d "%~dp0CosmicZoomEngine_App"
start "" "CosmicZoomEngine.exe"
