@echo off
REM 快速发布 Contracts 包到本地 NuGet 源

powershell -ExecutionPolicy Bypass -File "%~dp0publish-contracts.ps1" -Version 1.0.0 -Source local

pause
