# This file prepares the helix machine for our tests runs
$dotnetLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "dotnet"

# Set DOTNET_ROOT variables so the host can find it
Set-Item -Path "env:DOTNET_ROOT(x86)" -Value $dotnetLocation
Set-Item -Path "env:DOTNET_ROOT" -Value $dotnetLocation