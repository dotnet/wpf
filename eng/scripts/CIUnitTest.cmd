@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0..\scripts\Build.ps1""" -test -ci %*"