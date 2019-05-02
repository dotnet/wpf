@echo off
setlocal ENABLEDELAYEDEXPANSION
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0configure-helix-machine.ps1""" "

REM Run the tests
dotnet --info

REM We can use %HELIX_PYTHONPATH% %HELIX_SCRIPT_ROOT%\upload_result.py to upload any QV specific logs and/or screenshots that we are interested in.
REM For example: %HELIX_PYTHONPATH% %HELIX_SCRIPT_ROOT%\upload_result.py -result screenshot.jpg -result_name screenshot.jpg
REM Then, links to these artifacts can then be included in the xUnit logs.

REM Need to copy the xUnit log to a known location that helix can understand
REM copy Test\Drts\testResults.xml ..\testResults.xml