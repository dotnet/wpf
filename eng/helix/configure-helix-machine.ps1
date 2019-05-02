# This file prepares the helix machine for our tests runs

$dotnetLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "dotnet"

Write-Output "Adding dotnet location to path: $dotnetLocation"

# Prepend the path to ensure that dotnet.exe is called from there.
$env:path="$dotnetLocation;$env:path"

# Don't look outside the TestHost to resolve best match SDKs.
$env:DOTNET_MULTILEVEL_LOOKUP = 0

# Disable first run since we do not need all ASP.NET packages restored.
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Ensure that the dotnet installed at our dotnetLocation is used
$env:DOTNET_ROOT=$dotnetLocation