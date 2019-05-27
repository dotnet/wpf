[CmdLetBinding()]
Param(
    [string]$area
)

# Configure the machine before running tests
. "$PSScriptRoot\configure-helix-machine.ps1"

# Run the tests
$testLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "Test"
if (Test-Path "$testLocation\rundrts.cmd")
{
    Invoke-Expression "$testLocation\rundrts.cmd /Area=$area"
}

# We can use $env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT\upload_result.py to upload any QV specific logs and/or screenshots that we are interested in.
# For example: $env:HELIX_PYTHONPATH $env:HELIX_SCRIPT_ROOT%\upload_result.py -result screenshot.jpg -result_name screenshot.jpg
# Then, links to these artifacts can then be included in the xUnit logs.

# Need to copy the xUnit log to a known location that helix can understand
if (Test-Path "$env:AppData\QualityVault\Run\Report\testResults.xml")
{
    Copy-Item "$env:AppData\QualityVault\Run\Report\testResults.xml" ".\testResults.xml"
}