# Copy wpf binaries from local build to desired location. The location can either be the version number (for copying to shared installation)
# or the location of the project file if copying binaries to the output of a locally built application.
Param(
[string]$destination,
[string]$arch="x86",
[switch]$release,
[switch]$testhost,
[switch]$help
)

function Print-Usage()
{
    Write-Host "Usage: copy-wpf.ps1 -destination <value> [-arch <value] [-release] [-testhost]"
    Write-Host "    This script helps developers deploy wpf assemblies to the proper location for easy testing. See "
    Write-Host "    developer-guide.md for more information on how to use this script."
    Write-Host ""
    Write-Host "Common parameters:"
    Write-Host "  -destination <value>    Location of .csproj or .vbproj of application to test against. If copying"
    Write-Host "                          over a testhost installation, this should point to the location of dotnet.exe"
    Write-Host "  -arch <value>           Architecture of binaries to copy. Can be either x64 or x86. Default is x86."
    Write-Host "  -release                Copy release binaries. Default is to copy Debug binaries"
    Write-Host "  -testhost               Copy binaries over the test host installation of Microsoft.WindowsDesktop.App."
    Write-Host "  -help                   Print help and exit"
    Write-Host ""
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Config = if ($release) { "Release" } else { "Debug" }

if ($help -or [string]::IsNullOrEmpty($destination)) 
{
    Print-Usage
}
elseif($testhost)
{
    Write-Host "Copying binaries to test host installation"
    $location = Resolve-Path (Join-Path $destination "shared\Microsoft.WindowsDesktop.App\*")
    if(![System.IO.File]::Exists($location))
    {
        Write-Host "Location unavailable: " $destination -ForegroundColor Red
        return
    }
    CopyBinariesToLocation $location
}
else
{
    $runtimeIdentifer = if ($arch -eq "x86") { "win-x86" } else { "win-x64" }
    $location = [System.IO.Path]::Combine($destination, "bin\Debug\netcoreapp3.0", $runtimeIdentifer, "publish")
    if(![System.IO.File]::Exists($location))
    {
        Write-Host "Location unavailable: " $location -ForegroundColor Red
        return
    }
    Write-Host "Copying binaries to app published directory"
    CopyBinariesToLocation $location  
}

function CopyBinariesToLocation($location)
{
    $locallyBuiltBinaryLocationBase = Join-Path $RepoRoot "artifacts\bin"
    CopyNativeBinariesToLocation $location $locallyBuiltBinaryLocationBase 
    CopyManagedBinariesToLocation $location  $locallyBuiltBinaryLocationBase
    CopyThemeBinariesToLocation $location  $locallyBuiltBinaryLocationBase
    CopyUIAutomationBinariesToLocation $location  $locallyBuiltBinaryLocationBase
}
function CopyNativeBinariesToLocation($location, $localBinLocation)
{
    $NativeBinaries = "D3DCompiler", "PenImc", "PresentationNative", "wpfgfx"
    $ArchFolder = if ($arch -eq "x86") { "Win32" } else { "x64" }
    foreach($binary in $NativeBinaries)
    {
        $BinLocation = [System.IO.Path]::Combine($localBinLocation, $binary, $ArchFolder, $Config)
        if ($binary -eq "D3DCompiler")
        {
            $binary = $binary + "_47"
        }
        $NetCore3NativeBinaryName = $binary + "_cor3.dll"
        CopyBinaryToLocation $NetCore3NativeBinaryName $BinLocation $location
    }
}
function CopyManagedBinariesToLocation($location, $localBinLocation)
{
    # x86 managed binaries don't have a distinct folder
    $ManagedBinaries = "DirectWriteForwarder", "PresentationCore", "PresentationCore-CommonResources", "PresentationUI", "PresentationFramework-SystemCore", "PresentationFramework-SystemData", "PresentationFramework-SystemXml", "PresenationFramework-SystemXmlLinq", "ReachFramework", "System.Printing", "System.Windows.Controls.Ribbon", "System.Windows.Input.Manipulations", "WindowsBase", "WindowsFormsIntegration"
    CopyNetCoreApp3Binaries $location $localBinLocation $ManagedBinaries
}
function CopyThemeBinariesToLocation($location, $localBinLocation)
{
    $ThemeBinaries = "PresentationFramework.Aero", "PresentationFramework.Aero2", "PresentationFramework.AeroLight", "PresentationFramework.Classic", "PresentationFramework.Luna", "PresentationFramework.Royale"
    CopyNetCoreApp3Binaries $location $localBinLocation $ThemeBinaries
}
function CopyUIAutomationBinariesToLocation($location, $localBinLocation)
{
    $UIAutomationBinaries = "UIAutomationClient", "UIAutomationClientSideProviders", "UIAutomationProvider", "UIAutomationTypes"
    CopyNetCoreApp3Binaries $location $localBinLocation $UIAutomationBinaries
}

function CopyNetCoreApp3Binaries($location, $localBinLocation, $binaries)
{
    $ArchFolder = if ($arch -eq "x86") { "" } else { "x64" }
    foreach($binary in $binaries)
    {
        $FullBinaryName = $binary + ".dll"
        $BinLocation = [System.IO.Path]::Combine($localBinLocation, $binary, $ArchFolder, $Config, "netcoreapp3.0")
        CopyBinaryToLocation $FullBinaryName $BinLocation $location
    }
}

function CopyBinaryToLocation($binary, $binaryLocation, $destination)
{
    $FullBinaryPath = Join-Path $binaryLocation $binary
    if (Test-Path $FullBinaryPath)
    {
        Write-Host "Copying " $binary " to " $destination  
        Copy-Item $FullBinaryPath $destination -Force
    }
    else
    {
        Write-Host "Binary Unavailable: " $binary -ForegroundColor Yellow
    }
}

