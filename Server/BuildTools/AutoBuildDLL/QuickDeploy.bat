@echo off
echo ==============================================
echo AI SMART RECALL - QUICK DEPLOY (NO BUILD)
echo ==============================================
echo.
echo Chay MemoryPackSerializer va copy DLL (bo qua build)...
echo.

cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "AutoBuildDeploy.ps1" -SkipBuild

echo.
pause
