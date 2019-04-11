#
# This file should be kept in sync across https://www.github.com/dotnet/wpf and dotnet-wpf-int repos. 
#

# This repo uses C++/CLI /clr:pure (or /clr:netcore) switches during compilation, which are 
# deprecated. Ensure that this warning is always suppressed during build.
if (($properties -eq $null) -or (-not ($properties -icontains '/nowarn:D9035'))) {
    $properties = @('/nowarn:D9035') + $properties
}

# Make sure that Nuget restore doesn't hit the cache when running CI builds
# See https://github.com/NuGet/Home/issues/3116
if ($ci) {
    if (($properties -eq $null) -or (-not ($properties -icontains '/p:RestoreNoCache=true'))) {
        $properties = @('/p:RestoreNoCache=true') + $properties
    }
}
# Always generate binary logs
$binaryLog = $true
$DoNotAbortNativeToolsInstallationOnFailure = $true
