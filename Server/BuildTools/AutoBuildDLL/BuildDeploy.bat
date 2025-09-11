@echo off
echo ==============================================
echo AI SMART RECALL - BUILD AND DEPLOY DLL
echo ==============================================
echo.
echo Dang build solution va copy DLL sang Unity...
echo.

cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "AutoBuildDeploy.ps1"

echo.
pause
