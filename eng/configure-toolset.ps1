# If we are running a test pass, ensure that we point at the TestHost and that
# tests are run against it.
if ($test)
{
    # Don't look outside the TestHost to resolve best match SDKs.
    $env:DOTNET_MULTILEVEL_LOOKUP = 0

    # Use dotnet as the build environment so we don't call MSBuild.
    $msbuildEngine = "dotnet"
    
    $platform = ""

    # Find the Platform build parameter (if it exists and is x64; x86 does not add to the path)
    if($properties)
    {
        foreach($prop in $properties)
        {
            if ($prop -match "/p:Platform=(.*x64)")
            {
                $platform = $Matches[1]
            }
        }
    }

    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $ArtifactsDir = Join-Path $RepoRoot "artifacts"

    # Expand the testhost directory
    $testHostDir = Join-Path $ArtifactsDir "\testhost\" 
    if($platform)
    {
        $testHostDir = Join-Path $testHostDir $platform
    }
    $testHostDir = Join-Path $testHostDir $configuration
    $testHostDir = Resolve-Path $testHostDir

    # Prepend testhost to path to ensure that dotnet.exe is called from there.
    $env:path="$testHostDir;$env:path"

    # CI should always be run against a TestHost matching the Global.json version
    # Otherwise, short circuiting the eng/common/tools.ps1 script can result in
    # variables not being passed along to subsequent phases in the build yaml.
    # See eng/common/tools.ps1:InitializeDotNetCli to see why we do this.
    if(!$ci)
    {
        # Set this in order to short circuit the eng/common/tools.ps1 script from
        # overriding our TestHost in the case where the TestHost version doesn't match
        # the dotnet version called out in Global.json
        $global:_DotNetInstallDir = $testHostDir

        # When short circuiting eng/common/tools.ps1, make sure the dotnet install env var
        # matches the TestHost.  Otherwise the build will try to pull from multiple locations and
        # will fail verification of the TestHost.
        $env:DOTNET_INSTALL_DIR = $testHostDir
    }

    Write-Host "Testing configured to use TestHost installed at: " $testHostDir
}