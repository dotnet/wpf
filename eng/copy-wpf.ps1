# Copy wpf binaries from local build to desired location. The location can either be the version number (for copying to shared installation)
# or the location of the project file if copying binaries to the output of a locally built application.
Param(
[string]$destination,
[string]$arch="x86",
[switch]$release,
[switch]$local,
[switch]$help
)

function Print-Usage()
{
    Write-Host "Usage: copy-wpf.ps1 -destination <value> [-arch <value>] [-release] [-local]"
    Write-Host "    This script helps developers deploy wpf assemblies to the proper location for easy testing. See "
    Write-Host "    developer-guide.md for more information on how to use this script."
    Write-Host ""
    Write-Host "Common parameters:"
    Write-Host "  -destination <value>    Location of .csproj or .vbproj of application to test against. Ignored"
    Write-Host "                          if the -local parameter is used."
    Write-Host "  -arch <value>           Architecture of binaries to copy. Can be either x64 or x86. Default is x86."
    Write-Host "  -release                Copy release binaries. Default is to copy Debug binaries"
    Write-Host "  -local                  Copy binaries over the local dotnet installation in the .dotnet folder"
    Write-Host "  -help                   Print help and exit"
    Write-Host ""
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Config = if ($release) { "Release" } else { "Debug" }

function CopyBinariesToLocation($location)
{
    $locallyBuiltBinaryLocationBase = Join-Path $RepoRoot "artifacts\packaging"
    CopyNativeBinariesToLocation $location $locallyBuiltBinaryLocationBase 
    CopyManagedBinariesToLocation $location  $locallyBuiltBinaryLocationBase
}

function CopyNativeBinariesToLocation($location, $localBinLocation)
{
    # Layout of where the native binaries looks something like this:

    # x86 - artifacts\packaging\Debug\Microsoft.DotNet.Wpf.GitHub\lib\win-x86
    # x64 - artifacts\packaging\Debug\x64\Microsoft.DotNet.Wpf.GitHub\lib\win-x64
    
    $PackageName = "Microsoft.DotNet.Wpf.GitHub"
    $BinaryLocationInPackage =  "win-$arch"
    CopyPackagedBinaries $location $localBinLocation $PackageName $BinaryLocationInPackage
}
function CopyManagedBinariesToLocation($location, $localBinLocation)
{
    # Layout of where the managed binaries looks something like this: 
    # x86 - artifacts\packaging\Debug\Microsoft.DotNet.Wpf.GitHub\lib\netcoreapp3.0
    # x64 - artifacts\packaging\Debug\x64\Microsoft.DotNet.Wpf.GitHub\lib\netcoreapp3.0

    $PackageName = "Microsoft.DotNet.Wpf.GitHub"
    $BinaryLocationInPackage = "netcoreapp3.0"
    CopyPackagedBinaries $location $localBinLocation $PackageName $BinaryLocationInPackage
}

function CopyPackagedBinaries($location, $localBinLocation, $packageName, $binaryLocationInPackage)
{
    $ArchFolder = if ($arch -eq "x86") { "" } else { "x64" }
    $BinLocation = [System.IO.Path]::Combine($localBinLocation, $Config, $ArchFolder, $packageName, "lib", $binaryLocationInPackage, "*")
    Copy-Item -path $BinLocation -include "*.dll","*.pdb" -Destination $location
}

if ($help -or ([string]::IsNullOrEmpty($destination) -and !$local))
{
    Print-Usage
}
elseif($local)
{
    $destination = Join-Path $RepoRoot ".dotnet"
    Write-Host "Copying binaries to local installation"
    $location = Resolve-Path (Join-Path $destination "shared\Microsoft.WindowsDesktop.App\*")
    if(![System.IO.Directory]::Exists($location))
    {
        Write-Host "Location unavailable: " $location -ForegroundColor Red
        return
    }
    CopyBinariesToLocation $location
}
else
{
    $runtimeIdentifer = "win-$arch"
    $location = [System.IO.Path]::Combine($destination, "bin\Debug\netcoreapp3.0", $runtimeIdentifer, "publish")
    if(![System.IO.Directory]::Exists($location))
    {
        Write-Host "Location unavailable: " $location -ForegroundColor Red
        return
    }
    Write-Host "Copying binaries to app published directory"
    CopyBinariesToLocation $location  
}

