# This file uses the canonical dotnet-install script to download the latest daily build of the SDK and install it 
# as the TestHost.  This is called by testhost.csproj during Restore.
# By default, this installs the .NET Core version specified in Global.json.  You can override this by providing "/p:UseLatestSdkForTestHost=true" during build.
Param(
[string]$testHostPath,
[string]$arch="x86",
[switch]$useLatest,
[switch]$help
)

function Print-Usage()
{
    Write-Host "Usage: install-testhost.ps1 -testHostPath <value> [-arch <value] [-uselatest]"
    Write-Host "    This script helps developers setup a local test host installation for testing wpf assemblies."
    Write-Host "    See developer-guide.md for more information on how to use this script."
    Write-Host ""
    Write-Host "Common parameters:"
    Write-Host "  -testHostPath <value>   Location where the test host will be installed"
    Write-Host "  -arch <value>           Architecture of binaries to copy. Can be either x64 or x86."
    Write-Host "                          Default is x86."
    Write-Host "  -uselatest              Use the latest available assemblies built out of the dotnet/core-sdk repo."
    Write-Host "                          Defaults to the version defined in global.json."
    Write-Host "  -help                   Print help and exit"
    Write-Host ""
}
function Create-Directory([string[]] $path) {
  if (!(Test-Path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

# Downloads the canonical dotnet-install script
function GetDotNetInstallScript([string] $dotnetRoot) {
  $installScript = "$dotnetRoot\dotnet-install.ps1"
  if (!(Test-Path $installScript)) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Create-Directory $dotnetRoot
    Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
  }

  return $installScript
}

# Uses the dotnet-install script to install the TestHost into the specified directory
function CreateTestHost([string] $dotnetRoot, [string] $dotnetArch, [string] $expectedVersion) {
  $installScript = GetDotNetInstallScript $dotnetRoot
  & $installScript -InstallDir $dotnetRoot -NoPath -Architecture $dotnetArch -Version $expectedVersion
  if ($lastExitCode -ne 0) {
    Write-Host "Failed to install dotnet cli (exit code '$lastExitCode')." -ForegroundColor Red
    exit $lastExitCode
  }
}

# Removes the TestHost (if it exists)
function CleanTestHost([string] $testHostPath)
{
    if(![string]::IsNullOrEmpty($testHostPath))
    {
        # Just in case an invalid path gets here, we don't want to randomly delete directories
        # Use a file indicator to at least have some surety we're deleting a TestHost
        $installIndicator = $testHostPath + "\\dotnet.exe"

        # If the TestHost already exists, remove it and re-install
        if(Test-Path $installIndicator)
        {
            Remove-Item $testHostPath -Force -Recurse
        }
     }
}

# Returns the .NET Core version string that is expected in the TestHost
function GetExpectedTestHostVersion($repoRoot, $latest)
{
    if($latest)
    {
        $versionContent = (Invoke-WebRequest https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version).Content
        return ($versionContent -split "`r`n|`r|`n")[1]
    }
    else
    {
        $GlobalJson = Get-Content -Raw -Path (Join-Path $repoRoot "global.json") | ConvertFrom-Json
        return $GlobalJson.tools.dotnet
    }
}

# Returns the current .NET Core version string of the TestHost
function GetCurrentTestHostVersion($testHostPath)
{
    $dotnet = Join-Path $testHostPath "\dotnet.exe"
    if(Test-Path $dotnet)
    {
        return & $dotnet "--version"
    }
}

if(![string]::IsNullOrEmpty($testHostPath))
{
    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

    $expectedVersion = GetExpectedTestHostVersion $RepoRoot $useLatest
    $currentVersion = GetCurrentTestHostVersion $testHostPath

    Write-Host "Installing TestHost at: " $testHostPath
    Write-Host "Expected TestHost Version: " $expectedVersion
    Write-Host "Current TestHost Version: " $currentVersion

    # If the TestHost is the wrong version, clean and re-install
    if($expectedVersion -ne $currentVersion)
    {
        Write-Host "Cleaning old TestHost and Installing: " $expectedVersion
        CleanTestHost $testHostPath
        GetDotNetInstallScript $testHostPath
        CreateTestHost $testHostPath $arch $expectedVersion
    }
    else
    {
        Write-Host "TestHost is up to date, skipping install"
    }
}
elseif($help)
{
  Print-Usage
  exit 0;
}
else
{
    Write-Host "Failed to create TestHost: path was not provided." -ForegroundColor Red
    Print-Usage
    exit 1;
}