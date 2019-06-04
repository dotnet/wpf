[CmdLetBinding()]
Param(
    [string]$command,
    [switch]$ci
)

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
if (Test-Path "$testLocation\rundrts.cmd")
{
    Invoke-Expression "$testLocation\rundrts.cmd $command"
}

# We can use $env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT\upload_result.py to upload any QV specific logs and/or screenshots that we are interested in.
# For example: $env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT%\upload_result.py -result screenshot.jpg -result_name screenshot.jpg
# Then, links to these artifacts can then be included in the xUnit logs.

# Need to copy the xUnit log to a known location that helix can understand
if (Test-Path "$env:AppData\QualityVault\Run\Report\testResults.xml")
{
    $resultLocation = Get-Location
    Write-Output "Copying testResults.xml to $resultLocation"
    Copy-Item "$env:AppData\QualityVault\Run\Report\testResults.xml" $resultLocation
}

if (Test-Path "$env:AppData\QualityVault")
{
    # Cleanup what QualityVault left behind in AppData to save space on Helix machines
    Remove-Item "$env:AppData\QualityVault" -Recurse
}