@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0configure-helix-machine.ps1""" "
exit /b %ErrorLevel%

dotnet --info