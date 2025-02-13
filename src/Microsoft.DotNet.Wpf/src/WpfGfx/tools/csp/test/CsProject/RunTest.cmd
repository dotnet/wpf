@echo off

rem Test of csp.exe for a regular C# project.

rem csp.exe is easy to run. This script just:
rem
rem 1) Uses csp.exe from where it's built.
rem 2) Reports a useful message if csp.exe hasn't been built
rem 3) Makes sure we're in the right directory


rem Doesn't use a .rsp file or pass parameters to the project
rem (see test\CsProject for an example of that).



setlocal enabledelayedexpansion

pushd %~dp0

%CspExePath%\csp.exe -s:CsProjectTest.cs

popd

