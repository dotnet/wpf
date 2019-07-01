@echo off
Call %~dp0\DQV.cmd Run /DiscoveryInfoPath=%~dp0\DiscoveryInfoDrts.xml "/RunDirectory=%APPDATA%\QualityVault\Run" %*

if EXIST "%APPDATA%\QualityVault\Run\Report\DrtReport.xml" (
  echo To view DRT Report, run "DrtReport"
  echo start /B /WAIT "%ProgramFiles%\Internet Explorer\iexplore.exe" "%APPDATA%\QualityVault\Run\Report\DrtReport.xml" > DrtReport.cmd
  findstr /C:"Variations PassRate=\"100.00%%\"" "%APPDATA%\QualityVault\Run\Report\DrtReport.xml" > NUL
  if errorlevel 1 exit /b 1 else exit /b 0
)