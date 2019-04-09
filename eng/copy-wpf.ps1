# Copy wpf binaries from local build to desired location. The location can either be the version number (for copying to shared installation)
# or the location of the project file if copying binaries to the output of a locally built application.
Param(
[string]$destination,
[string]$arch="x86",
[switch]$release,
[switch]$shared,
[switch]$help
)

function CopyByBinaryToPublishLocation($publishLocation)
{
    $locallyBuiltBinaryLocationBase = Join-Path $RepoRoot "artifacts\bin"
    CopyNativeBinariesToPublishLocation $publishLocation $locallyBuiltBinaryLocationBase 
    CopyManagedBinariesToPublishLocation $publishLocation  $locallyBuiltBinaryLocationBase
    CopyThemeBinariesToPublishLocation $publishLocation  $locallyBuiltBinaryLocationBase
    CopyUIAutomationBinariesToPublishLocation $publishLocation  $locallyBuiltBinaryLocationBase
}

function CopyNativeBinariesToPublishLocation($publishLocation, $localBinLocation)
{
    $NativeBinaries = "D3DCompiler", "PenImc", "PresentationNative", "wpfgfx"
    $ArchFolder = if ($arch -eq "x86") { "Win32" } else { "x64" }
    foreach($binary in $NativeBinaries)
    {
        $BinLocation = Join-Path (Join-Path (Join-Path $localBinLocation $binary) $ArchFolder) $Config
        if ($binary -eq "D3DCompiler")
        {
            $binary = $binary + "_47"
        }
        $NetCore3NativeBinaryName = $binary + "_cor3.dll"
        CopyBinaryToLocation $NetCore3NativeBinaryName $BinLocation $publishLocation
    }
}

function CopyManagedBinariesToPublishLocation($publishLocation, $localBinLocation)
{
    # x86 managed binaries don't have a distinct folder
    $ManagedBinaries = "DirectWriteForwarder", "PresentationCore", "PresentationCore-CommonResources", "PresentationUI", "PresentationFramework-SystemCore", "PresentationFramework-SystemData", "PresentationFramework-SystemXml", "PresenationFramework-SystemXmlLinq", "ReachFramework", "System.Printing", "System.Windows.Controls.Ribbon", "System.Windows.Input.Manipulations", "WindowsBase", "WindowsFormsIntegration"
    CopyNetCoreApp3Binaries $publishLocation $localBinLocation $ManagedBinaries
}

function CopyThemeBinariesToPublishLocation($publishLocation, $localBinLocation)
{
    $ThemeBinaries = "PresentationFramework.Aero", "PresentationFramework.Aero2", "PresentationFramework.AeroLight", "PresentationFramework.Classic", "PresentationFramework.Luna", "PresentationFramework.Royale"
    CopyNetCoreApp3Binaries $publishLocation $localBinLocation $ThemeBinaries
}

function CopyUIAutomationBinariesToPublishLocation($publishLocation, $localBinLocation)
{
    $UIAutomationBinaries = "UIAutomationClient", "UIAutomationClientSideProviders", "UIAutomationProvider", "UIAutomationTypes"
    CopyNetCoreApp3Binaries $publishLocation $localBinLocation $UIAutomationBinaries
}

function CopyNetCoreApp3Binaries($publishLocation, $localBinLocation, $binaries)
{
    $ArchFolder = if ($arch -eq "x86") { "" } else { "x64" }
    foreach($binary in $binaries)
    {
        $FullBinaryName = $binary + ".dll"
        $BinLocation = Join-Path (Join-Path (Join-Path (Join-Path $localBinLocation $binary) $ArchFolder) $Config) "netcoreapp3.0"
        CopyBinaryToLocation $FullBinaryName $BinLocation $publishLocation
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
        Write-Host "Binary Unavailable: " $binary
    }
}

function Print-Usage()
{
    Write-Host "Usage: copy-wpf.ps1 -destination <value> [-arch <value] [-release] [-shared]"
    Write-Host "    This script helps developers deploy wpf assemblies to the proper location for easy testing. See "
    Write-Host "    developer-guide.md for more information on how to use this script."
    Write-Host ""
    Write-Host "Common parameters:"
    Write-Host "  -destination <value>    Location of .csproj or .vbproj of application to test against. If copying"
    Write-Host "                          over a shared installation, the value should be the version to copy over."
    Write-Host "  -arch <value>           Architecture of binaries to copy. Can be either x64 or x86. Default is x86."
    Write-Host "  -release                Copy release binaries. Default is to copy Debug binaries"
    Write-Host "  -shared                 Copy binaries over the shared installation of Microsoft.WindowsDesktop.App."
    Write-Host "  -help                   Print help and exit"
    Write-Host ""
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Config = if ($release) { "Release" } else { "Debug" }

if ($help -or [string]::IsNullOrEmpty($destination)) 
{
    Print-Usage
}
elseif($shared)
{
    Write-Host "Copying binaries to shared installation"
    $sharedLocation = Join-Path (Join-Path $env:ProgramFiles "dotnet\shared\Microsoft.WindowsDesktop.App") $destination
    if(![System.IO.File]::Exists($sharedLocation))
    {
        Write-Host "Location unavailable: " $sharedLocation -ForegroundColor Red
        return
    }
    CopyByBinaryToPublishLocation $sharedLocation
}
else
{
    $runtimeIdentifer = if ($arch -eq "x86") { "win-x86" } else { "win-x64" }
    $publishLocation = Join-Path (Join-Path (Join-Path $destination "bin\Debug\netcoreapp3.0") $runtimeIdentifer) "publish"
    if(![System.IO.File]::Exists($publishLocation))
    {
        Write-Host "Location unavailable: " $publishLocation -ForegroundColor Red
        return
    }
    Write-Host "Copying binaries to app published directory"
    CopyByBinaryToPublishLocation $publishLocation  
}

