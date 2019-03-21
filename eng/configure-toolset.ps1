#
# This file should be kept in sync across https://www.github.com/dotnet/wpf and dotnet-wpf-int repos. 
#

# This repo uses C++/CLI /clr:pure (or /clr:netcore) switches during compilation, which are 
# deprecated. Ensure that this warning is always suppressed during build.
if (($properties -eq $null) -or (-not ($properties -icontains '/nowarn:D9035'))) {
    $properties = @('/nowarn:D9035') + $properties
}

# Always generate binary logs
$binaryLog = $true
$DoNotAbortNativeToolsInstallationOnFailure = $true
