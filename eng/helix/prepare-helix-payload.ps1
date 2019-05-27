[CmdLetBinding()]
Param(
    [string]$platform,
    [string]$configuration
)

$payloadDir = "HelixPayload\$configuration\$platform"

$nugetPackagesDir = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "packages"

# Create the payload directory. Remove it if it already exists.
if(Test-Path $payloadDir)
{
    Remove-Item $payloadDir -Recurse
}
New-Item -ItemType Directory -Force -Path $payloadDir

function CopyFolderStructure($from, $to)
{
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
Copy-Item "eng\helix\configure-helix-machine.ps1" $payloadDir
Copy-Item "eng\helix\runtests.ps1" $payloadDir