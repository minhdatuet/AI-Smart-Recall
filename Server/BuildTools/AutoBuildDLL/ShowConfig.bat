@echo off
echo ==============================================
echo AI SMART RECALL - THONG TIN CAU HINH
echo ==============================================
echo.

cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "AutoBuildDeploy.ps1" -ShowConfig

echo.
pause
