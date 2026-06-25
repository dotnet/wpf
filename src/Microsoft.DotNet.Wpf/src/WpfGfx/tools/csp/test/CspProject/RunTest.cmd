@echo off

rem Test of csp.exe for a "C# prime" project.
rem
rem Also tests:
rem   * Parameters passed
rem   * Use of a .rsp file
rem   * "-main" parameter

setlocal

pushd %~dp0

%CspExePath%\csp.exe -enablecsprime -rsp:CspProject.rsp -- testparam1 testparam2

popd
