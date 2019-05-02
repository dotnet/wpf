# This file prepares the helix machine for our tests runs

$dotnetLocation = Join-Path (Split-Path -Parent $script:MyInvocation.MyCommand.Path) "dotnet"

Write-Output "Adding dotnet location to path: $dotnetLocation"

# Prepend the path to ensure that dotnet.exe is called from there.
[System.Environment]::SetEnvironmentVariable("PATH", "$dotnetLocation;$env:path", [System.EnvironmentVariableTarget]::User)

# Don't look outside the TestHost to resolve best match SDKs.
[System.Environment]::SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0", [System.EnvironmentVariableTarget]::User)

# Disable first run since we do not need all ASP.NET packages restored.
[System.Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1", [System.EnvironmentVariableTarget]::User)

# Ensure that the dotnet installed at our dotnetLocation is used
[System.Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "$dotnetLocation", [System.EnvironmentVariableTarget]::User)
