# This file prepares the helix machine for our tests runs
$dotnetLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "dotnet"

# Set DOTNET_ROOT variables so the host can find it
Set-Item -Path "env:DOTNET_ROOT(x86)" -Value $dotnetLocation
Set-Item -Path "env:DOTNET_ROOT" -Value $dotnetLocation

# Temporary workaround until https://github.com/dotnet/wpf/issues/816 is addressed.
# The test infrastructure is built against an older version of Microsoft.WindowsDesktop.App.
# Here we re-write the infrastructure runtimeconfig files so that they can load
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
