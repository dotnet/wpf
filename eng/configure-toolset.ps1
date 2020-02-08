#
# This file should be kept in sync across https://www.github.com/dotnet/wpf and dotnet-wpf-int repos. 
#

# It is possible that $properties has not yet been defined here if this script is not being called with
# build.ps1 as an ancestor.  In that case, we should not do anything related to the properties variable.
if ((Get-Variable -Name 'properties' -Scope Script -ErrorAction Ignore))
{
    # This repo uses C++/CLI /clr:pure (or /clr:netcore) switches during compilation, which are 
    # deprecated. Ensure that this warning is always suppressed during build.
    if (($properties -eq $null) -or (-not ($properties -icontains '/nowarn:D9035'))) {
        $properties = @('/nowarn:D9035') + $properties
    }

    # This repo treats Solution/Project platform 'Any CPU' as ~= 'x86', and 
    # defaults to 'Any CPU'/x86. 
    if (($properties -eq $null) -or (-not ($properties -ilike '/p:Platform=*'))) {
        if ((Get-Variable -Name 'platform' -Scope Script -ErrorAction Ignore) -and -not $platform) {
            $platform='x86'
        }
    }

    # Make sure that Nuget restore doesn't hit the cache when running CI builds
    # See https://github.com/NuGet/Home/issues/3116
    if ((Get-Variable -Name 'ci' -Scope Script -ErrorAction Ignore) -and $ci) {
        if (($properties -eq $null) -or (-not ($properties -icontains '/p:RestoreNoCache=true'))) {
            $properties = @('/p:RestoreNoCache=true') + $properties
        }
    }
}

# Always generate binary logs
$binaryLog = $true
$DoNotAbortNativeToolsInstallationOnFailure = $true
$DoNotDisplayNativeToolsInstallationWarnings =$true

if ((Get-Variable -Name 'platform' -Scope Script -ErrorAction Ignore)) {
    $platformSuffix = if ($platform) { "$platform" } else { '' }
    $LogDir = (Join-Path $LogDir $platformSuffix)
}
