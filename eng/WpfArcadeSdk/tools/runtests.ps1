[CmdLetBinding()]
Param(
    [string]$command,
    [switch]$ci
)

# Get the ID and security principal of the current user account
$myWindowsID=[System.Security.Principal.WindowsIdentity]::GetCurrent()
$myWindowsPrincipal=new-object System.Security.Principal.WindowsPrincipal($myWindowsID)
 
# Get the security principal for the Administrator role
$adminRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
 
# Only need to elevate if not running in ci or currently not an admin
$elevate = !($ci -or $myWindowsPrincipal.IsInRole($adminRole))
if ($elevate)
{
   # We are not running "as Administrator" - so relaunch as administrator
   
   # Create a new process object that starts PowerShell
   $newProcess = new-object System.Diagnostics.ProcessStartInfo "PowerShell";
   
   # Specify the current script path and name as a parameter
   $newProcess.Arguments = $myInvocation.MyCommand.Definition;
   
   # Indicate that the process should be elevated
   $newProcess.Verb = "runas";
   
   # Start the new process
   [System.Diagnostics.Process]::Start($newProcess);
   
   # Exit from the current, unelevated, process
   exit
}

# Run any configuration needed for the test pass
if (Test-Path "$PSScriptRoot\configure-machine.ps1")
{
    . "$PSScriptRoot\configure-machine.ps1" -ci:$ci
}

if (Test-Path "$env:AppData\QualityVault")
{
    # Cleanup any QualityVault stuff left behind before executing the tests
    Remove-Item "$env:AppData\QualityVault" -Recurse
}

# Run the tests
$testLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "Test"
if (Test-Path "$testLocation\QV.cmd")
{
    # We invoke QV directly instead of rundrts to prevent the "RunDrtReport" script being generated. 
    Invoke-Expression "$testLocation\QV.cmd Run /DiscoveryInfoPath=$testLocation\DiscoveryInfoDrts.xml /RunDirectory=$env:AppData\QualityVault\Run $command"
}

if ($ci -and (Test-Path "$env:AppData\QualityVault\Run\Report\DrtReport.xml"))
{
    Invoke-Expression "$env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT\upload_result.py -result $env:AppData\QualityVault\Run\Report\DrtReport.xml -result_name DrtReport.xml"
}
# We can use $env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT\upload_result.py to upload any QV specific logs and/or screenshots that we are interested in.
# For example: $env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT%\upload_result.py -result screenshot.jpg -result_name screenshot.jpg
# Then, links to these artifacts can then be included in the xUnit logs.

# Need to copy the xUnit log to a known location that helix can understand
if (Test-Path "$env:AppData\QualityVault\Run\Report\testResults.xml")
{
    $resultLocation = if($ci) { Get-Location } else { $PSScriptRoot }
    Write-Output "Copying testResults.xml to $resultLocation"
    Copy-Item "$env:AppData\QualityVault\Run\Report\testResults.xml" $resultLocation
}

if (Test-Path "$env:AppData\QualityVault")
{
    # Cleanup what QualityVault left behind in AppData to save space on Helix machines
    Remove-Item "$env:AppData\QualityVault" -Recurse
}
