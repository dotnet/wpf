@echo off
REM This script is responsible for an xcopy deployment of QualityVault, with Debugging support.

@setlocal enabledelayedexpansion
set devenvCmd=echo VS not found

for /f "tokens=2 delims==" %%I in ('set vs') do (
    if exist "%%I..\IDE\devenv.exe" set devenvCmd="%%I..\IDE\devenv.exe" /debugexe
)

for /d %%I in ("C:\Program Files (x86)\Microsoft Visual Studio\*") do (
    if exist "%%I\Enterprise\Common7\IDE\devenv.exe" (
        set devenvCmd="%%I\Enterprise\Common7\IDE\devenv.exe" /debugexe
    )
    if exist "%%I\Preview\Common7\IDE\devenv.exe" (
        set devenvCmd="%%I\Preview\Common7\IDE\devenv.exe" /debugexe
    )
)

xcopy %~dp0\Infra "%ProgramFiles%\QualityVault\" /Q /Y /E
!devenvCmd! "%ProgramFiles%\QualityVault\QualityVaultFrontend.exe" %*
rd /S /Q "%ProgramFiles%\QualityVault"