[CmdLetBinding()]
Param(
    [string]$command,
    [switch]$ci
)

if ($ci)
{
    # When running in ci, the dotnet install is located at %HELIX_CORRELATION_PAYLOAD% along with all our test content.
    $dotnetLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "dotnet"
    # Only either the x86 or x64 versions of dotnet are installed on the helix machine, and they both go to the same location
    $x86dotnetLocation = $dotnetLocation

    # Run any extra machine setup required for Helix
    . "$PSScriptRoot\configure-helix-machine.ps1"
}
else
{
    # When running local, we run out of $(RepoRoot)artifacts\test\$(Configuration)\$(Platform)
    # The dotnet install is located at $(RepoRoot).dotnet
    $dotnetLocation =  Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "..\..\..\..\.dotnet" -Resolve
    # The x86 location installed by Arcade is in the x86 directory
    $x86dotnetLocation = "$dotnetLocation\x86"
}

# Set DOTNET_ROOT variables so the host can find it
Set-Item -Path "env:DOTNET_ROOT(x86)" -Value $x86dotnetLocation
Set-Item -Path "env:DOTNET_ROOT" -Value $dotnetLocation

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
    $resultLocation = $PSScriptRoot
    Write-Output "Copying test results to $resultLocation"
    Copy-Item "$env:AppData\QualityVault\Run\Report\testResults.xml" $resultLocation
}