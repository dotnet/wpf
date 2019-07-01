@echo off
Call %~dp0\DQV.cmd Run /DiscoveryInfoPath=%~dp0\DiscoveryInfo.xml /RunDirectory="%APPDATA%\QualityVault\Run" %*