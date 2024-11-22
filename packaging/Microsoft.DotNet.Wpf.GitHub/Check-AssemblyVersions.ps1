[CmdletBinding(PositionalBinding=$false)]
Param(
  [Parameter(Mandatory=$True, Position=1)]
  [string] $NuspecFile,
  [Parameter(Mandatory=$True, Position=2)]
  [string] $ExpectedAssemblyVersion,
  [Parameter(Mandatory=$True, Position=3)]
  [string] $IsServicingRelease,
  [Parameter(ValueFromRemainingArguments=$true)][String[]] $properties
)

$servicingRelease = $null;
[bool]::TryParse($IsServicingRelease, [ref]$servicingRelease) | Out-Null;

[xml] $xmlDoc = Get-Content -Path $NuspecFile -Force;

# 
# Verify that components that are exposed as references in the targeting packs don't have their versions revved.
# See https://github.com/dotnet/core/issues/7172#issuecomment-1034105137 for more details.
[xml] $xmlDoc = Get-Content -Path $NuspecFile -Force;

# Iterate over files that MUST NOT have their versions revved with every release
$nonRevAssemblies = $xmlDoc.package.files.file | `
    Where-Object { 
            ($_.target.StartsWith('lib\') -or $_.target.StartsWith('ref\')) `
                -and $_.target.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase) `
                -and !$_.target.EndsWith('resources.dll', [System.StringComparison]::OrdinalIgnoreCase)
        } | `
    Select-Object -Unique src | `
    Select-Object -ExpandProperty src;

$nonRevAssemblies | `
    sort-object | `
    foreach-object {
        $assembly = $_;
        [string] $version = ([Reflection.AssemblyName]::GetAssemblyName($assembly).Version).ToString()

        Write-Host "$assembly`: $version"
        if (![string]::Equals($version, $ExpectedAssemblyVersion)) {
            throw "$assembly is not versioned correctly. Expected: '$ExpectedAssemblyVersion', found: '$version'."
            exit -1;
        }
    }

# Iterate over files that MUST have their versions revved with every release
$revAssemblies = $xmlDoc.package.files.file | `
    Where-Object { 
            $_.target.StartsWith('sdk\analyzers\') `
                -and $_.target.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase) `
                -and !$_.target.EndsWith('resources.dll', [System.StringComparison]::OrdinalIgnoreCase)
        } | `
    Select-Object -Unique src | `
    Select-Object -ExpandProperty src;

$revAssemblies | `
    sort-object | `
    foreach-object {
        $assembly = $_;
        [string] $version = ([Reflection.AssemblyName]::GetAssemblyName($assembly).Version).ToString()

        Write-Host "$assembly`: $version"
        if ($servicingRelease -and [string]::Equals($version, $ExpectedAssemblyVersion)) {
            throw "$assembly is not versioned correctly. '$version' is not expected."
            exit -1;
        }
    }
