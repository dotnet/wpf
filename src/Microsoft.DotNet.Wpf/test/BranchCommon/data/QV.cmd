@echo off
REM This script is responsible for an xcopy deployment of QualityVault.
REM Testers will normally run RunTests.cmd, which calls into QV.cmd with
REM additional run parameters.
REM "Call %~dp0\QV.cmd Run /DiscoveryInfoPath=%~dp0\DiscoveryInfo.xml /RunDirectory="%APPDATA%\QualityVault\Run" %*"
REM ~dp0 grabs the directory path of the 0th argument, which is the cmd file itself.

xcopy %~dp0\Infra "%ProgramFiles%\QualityVault\" /Q /Y /E
"%ProgramFiles%\QualityVault\QualityVaultFrontend.exe" %*
 rd /S /Q "%ProgramFiles%\QualityVault"
