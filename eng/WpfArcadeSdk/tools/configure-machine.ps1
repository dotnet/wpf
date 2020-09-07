[CmdLetBinding()]
Param(
    [switch]$ci
)
# This file prepares the machine for our tests runs

if ($ci)
{
    # When running in ci, the dotnet install is located at %HELIX_CORRELATION_PAYLOAD% along with all our test content.
    $dotnetLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "dotnet"
    # Only either the x86 or x64 versions of dotnet are installed on the helix machine, and they both go to the same location
    $x86dotnetLocation = $dotnetLocation
}
else
{
    # When running local, we can use the DOTNET_INSTALL_DIR environment variable set by Arcade
    $dotnetLocation = $env:DOTNET_INSTALL_DIR

    # The x86 location installed by Arcade is in the x86 directory
    $x86dotnetLocation = "$dotnetLocation\x86"
    
    # This is yet one more unfortunate workaround required until https://github.com/dotnet/wpf/issues/816 is addressed, or until
    # QualityVault and STI don't use WindowsDesktop any longer. Since the multi-framework support only installs the Microsoft.NETCore.App
    # runtime in the 'x86' dir, we need the ability to look outside that directory for an install of Microsoft.WindowsDesktop.App.
    $env:DOTNET_MULTILEVEL_LOOKUP=1
}

# Set DOTNET_ROOT variables so the host can find it
Set-Item -Path "env:DOTNET_ROOT(x86)" -Value $x86dotnetLocation
Set-Item -Path "env:DOTNET_ROOT" -Value $dotnetLocation

# Temporary workaround until https://github.com/dotnet/wpf/issues/816 is addressed.
# The test infrastructure is built against an older version of Microsoft.WindowsDesktop.App.
# Here we re-write the infrastructure runtimeconfig files so that they can load
if ($ci)
{
    $runtimes = dotnet --list-runtimes
    foreach ($rt in $runtimes)
    {
        if ($rt.StartsWith("Microsoft.WindowsDesktop.App"))
        {
            $version = $rt.Split(" ")[1]
        }
    }
    
    if ($null -ne $version)
    {
        # Rewrite the *.runtimeconfig.json files to match the version of the runtime on the machine
        $infraLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "Test\Infra"
        $stiConfigFile = Join-Path $infraLocation "Sti.runtimeconfig.json"
        $qvConfigFile = Join-Path $infraLocation "QualityVaultFrontEnd.runtimeconfig.json"
        $configFiles = $stiConfigFile, $qvConfigFile
        foreach ($config in $configFiles)
        {
            # Read current config file
            $jsondata = Get-Content -Raw -Path $config | ConvertFrom-Json
            # Update version
            $jsondata.runtimeOptions.framework.version = $version
            # Write data back
            $jsondata | ConvertTo-Json -depth 100 | Set-Content $config
        } 
    }
    else
    {
        Write-Error "No WindowsDesktop runtime found on machine!"
        
        Write-Output "Runtimes installed:"
        Write-Output $runtimes
    }
    
}
