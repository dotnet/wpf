@echo off
Call %~dp0\QV.cmd Run /DiscoveryInfoPath=%~dp0\DiscoveryInfo.xml /RunDirectory="%APPDATA%\QualityVault\Run" %*