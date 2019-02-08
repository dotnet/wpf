@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0\common\Build.ps1""" %*"
exit /b %ErrorLevel%
