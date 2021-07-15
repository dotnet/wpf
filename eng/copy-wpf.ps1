# Copy wpf binaries from local build to desired location. The location can either be the version number (for copying to shared installation)
# or the location of the project file if copying binaries to the output of a locally built application.
Param(
[string]$destination,
[string]$arch="x86",
[switch]$release,
[switch]$testhost,
[string]$version,
[switch]$help
)

function Print-Usage()
{
    Write-Host "Usage: copy-wpf.ps1 -destination <value> [-arch <value>] [-release] [-testhost] [-version]"
    Write-Host "    This script helps developers deploy wpf assemblies to the proper location for easy testing. See "
    Write-Host "    developer-guide.md for more information on how to use this script."
    Write-Host ""
    Write-Host "Common parameters:"
    Write-Host "  -destination <value>    Location of .csproj or .vbproj of application to test against. If using -testhost,"
    Write-Host "                          copies to the testhost location specified."
    Write-Host "  -arch <value>           Architecture of binaries to copy. Can be either x64 or x86. Default is x86."
    Write-Host "  -release                Copy release binaries. Default is to copy Debug binaries"
    Write-Host "  -testhost               Copy binaries over the testhost installation of dotnet"
    Write-Host "  -version                When -testhost is used, will copy binaries over specified version of the"
    Write-Host "                          Microsoft.WindowsDesktop.App shared runtime"
    Write-Host "  -help                   Print help and exit"
    Write-Host ""
}

Write-Host "*** Copy WPF files procedure ***"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Config = if ($release) { "Release" } else { "Debug" }

Write-Host "Target architecture - configuration: " $arch $Config

function CopyBinariesToLocation($location)
{
    $locallyBuiltBinaryLocationBase = Join-Path $RepoRoot "artifacts\packaging"

    Write-Host "Copy native binaries..."
    CopyNativeBinariesToLocation $location $locallyBuiltBinaryLocationBase

    Write-Host "Copy managed binaries..."
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
    # x86 - artifacts\packaging\Debug\Microsoft.DotNet.Wpf.GitHub\lib\net6.0
    # x64 - artifacts\packaging\Debug\x64\Microsoft.DotNet.Wpf.GitHub\lib\net6.0

    $PackageName = "Microsoft.DotNet.Wpf.GitHub"
    $BinaryLocationInPackage = "net6.0"
    CopyPackagedBinaries $location $localBinLocation $PackageName $BinaryLocationInPackage
}

function CopyPackagedBinaries($location, $localBinLocation, $packageName, $binaryLocationInPackage)
{
    $ArchFolder = if ($arch -eq "x86") { "" } else { "x64" }
    $BinLocation = [System.IO.Path]::Combine($localBinLocation, $Config, $ArchFolder, $packageName, "lib", $binaryLocationInPackage, "*")
    if (Test-Path $BinLocation)
    {
        Copy-Item -path $BinLocation -include "*.dll","*.pdb" -Destination $location
        Write-Host "All files are copied" -ForegroundColor Green
    }
    else
    {
        Write-Host "Source files location unavailable: " $BinLocation -ForegroundColor Yellow -NoNewline
        Write-Host "  Skip. No file has been copied."
        return
    }
}

function LocationIsSharedInstall($location, $arch)
{
    if ($arch -eq "x86")
    {
        return $location -eq "${env:ProgramFiles(x86)}\dotnet"
    }
    else
    {
        return $location -eq "$env:ProgramFiles\dotnet"
    }
}

if ($help -or [string]::IsNullOrEmpty($destination))
{
    Print-Usage
}
elseif($testhost)
{
    if ([string]::IsNullOrEmpty($version))
    {
        $location = Resolve-Path (Join-Path $destination "shared\Microsoft.WindowsDesktop.App\*")
        if ($location.Count -gt 1)
        {
            Write-Host "WARNING: Multiple versions of the Microsoft.WindowsDesktop.App runtime are located at $destination."
            Write-Host "         Choosing the last installed runtime. Use -version flag to specify a different version."
            $runtimeToChoose = $location.Count-1

            # If the last runtime is a backup, ignore it and choose the next one.
            if ($location[$runtimeToChoose].Path.Contains("Copy"))
            {
                $runtimeToChoose = $runtimeToChoose-1
            }
            $location = $location[$runtimeToChoose]
        }
    }
    else
    {
        $location = Resolve-Path (Join-Path $destination "shared\Microsoft.WindowsDesktop.App\$version")
    }

    Write-Host "Copying binaries to dotnet installation at $location"

    if(![System.IO.Directory]::Exists($location))
    {
        Write-Host "Location unavailable: " $location -ForegroundColor Red
        return
    }
    CopyBinariesToLocation $location

    if (LocationIsSharedInstall $destination $arch)
    {
        # There is nothing fundamentally different about a test host installation versus trying to copy
        # into program files. We just won't set the DOTNET_ROOT or DOTNET_MULTILEVEL_LOOKUP.
        Write-Host "Copying to Program Files, skipping setting environment variables."
    }
    else
    {
        # Set DOTNET_ROOT variables so the host can find it
        $dotnetVariableToSet = if ($arch -eq "x86") { "env:DOTNET_ROOT(x86)"} else { "env:DOTNET_ROOT"}
        Write-Host "** Setting $dotnetVariableToSet to $destination **"
        Set-Item -Path $dotnetVariableToSet -Value $destination

        Write-Host "** Setting env:DOTNET_MULTILEVEL_LOOKUP to 0 **"
        $env:DOTNET_MULTILEVEL_LOOKUP=0
    }
}
else
{
    $runtimeIdentifer = "win-$arch"
    $location = [System.IO.Path]::Combine($destination, "bin\Debug\net6.0", $runtimeIdentifer, "publish")
    if(![System.IO.Directory]::Exists($location))
    {
        Write-Host "App publishing directory unavailable: " $location -ForegroundColor Red
        return
    }

    Write-Host "App publishing directory: " $location
    Write-Host "Copying binaries to app publishing directory..."
    CopyBinariesToLocation $location
}
