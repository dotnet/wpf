@echo off

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0runtests.ps1"""  %*"