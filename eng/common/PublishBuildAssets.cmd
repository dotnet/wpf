@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0Build.ps1""" -msbuildEngine dotnet -restore /p:PublishBuildAssets=true /p:SdkProjects=PublishBuildAssets.proj %*"
exit /b %ErrorLevel%
