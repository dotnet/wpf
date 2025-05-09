@echo off

::
:: GenerateElements.cmd runs the MilCodeGen system.
::
:: To run under Rascal (installed at %ProgramFiles%\Microsoft Rascal, or you must set %RascalPath%):
::   DebugMCG.cmd GenerateElements.cmd
::
:: GenerateElements.cmd may be invoked through the build process (e.g., bcz)
:: or directly from a razzle window in which case it relies on the paths provided
:: in SetClrPath.cmd
::
:: See also ParseMcgOpt.cmd for supported options.
::

setlocal enabledelayedexpansion

if "%_gf_test%"=="1" goto skiptoolpath
if "%_gf_test%"=="2" goto starttest

set DebuggerHook=

call %~dp0\ParseMcgOpt.cmd %McgOpt%

:starttest

:: Run from parent directory (relative to GenerateElements.cmd)
pushd %~dp0\..

call %~dp0\SetClrPath.cmd
set OutputDir=%RepoRoot%\src\Microsoft.DotNet.Wpf

:: Build resource model
"%clrpath%\xsd.exe" /classes /namespace:MS.Internal.MilCodeGen.ResourceModel /out:ResourceModel\Generated xml\Elements.xsd
if %ERRORLEVEL% NEQ 0 (
    echo mcg : error : Updating the resource model with XSD.exe failed.
    exit /b 1
)

:: Execute the MilCodeGen project
call %~dp0\InvokeCSP.cmd %Options% -rsp:main\Elements.rsp -enableCsPrime -r:"System.Xml.dll" -- %_SdFlag% -xmlFile:xml\Elements.xml -dataType:MS.Internal.MilCodeGen.ResourceModel.CG -o:"%OutputDir%"

popd
endlocal
goto :EOF
