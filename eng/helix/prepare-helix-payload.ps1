[CmdLetBinding()]
Param(
    [string]$platform,
    [string]$configuration
)

$payloadDir = "$PSScriptRoot\HelixPayload\$configuration\$platform"

$nugetPackagesDir = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "packages"

# TODO: Once we have the nuget package from the dotnet-wpf-test, we can better understand what we 
# need to do here.
# Create the payload directory. Remove it if it already exists.
if(Test-Path $payloadDir)
{
    Remove-Item $payloadDir -Recurse
}
New-Item -ItemType Directory -Force -Path $payloadDir

function CopyFolderStructure($from, $to)
{
    Write-Output "Copying from: $from"
    Write-Output "          to: $to"
    
    if(Test-Path $to)
    {
        Remove-Item $to -Recurse
    }
    New-Item -ItemType Directory -Force -Path $to

    if (Test-Path $from)
    {
        Get-ChildItem $from | Copy-Item -Destination $to -Recurse 
    }
    else
    {
        Write-Output "Location doesn't exist: $from"
    }
}

# Copy files from nuget packages.
$testNugetLocation = Resolve-Path (Join-Path $nugetPackagesDir "runtime.win-$platform.Microsoft.DotNet.Wpf.Test.*\tools\win-$platform\Test")
$testPayloadLocation = Join-Path $payloadDir "Test"
CopyFolderStructure $testNugetLocation $testPayloadLocation

# Copy local DRT assemblies to test location
CopyFolderStructure $testNugetLocation $testPayloadLocation
$drtArtifactsLocation = [System.IO.Path]::Combine($env:BUILD_SOURCESDIRECTORY, "artifacts\test", $configuration, $platform, "Test\DRT")
$drtPayloadLocation = Join-Path $payloadDir "Test\DRT"
CopyFolderStructure $drtArtifactsLocation $drtPayloadLocation

# Copy scripts
Copy-Item "$PSScriptRoot\configure-helix-machine.ps1" $payloadDir
Copy-Item "$PSScriptRoot\runtests.ps1" $payloadDir