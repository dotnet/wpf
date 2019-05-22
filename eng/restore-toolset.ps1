#
# This file should be kept in sync across https://www.github.com/dotnet/wpf and dotnet-wpf-int repos. 
#

function InitializeWpfCustomToolset() {
  if (Test-Path variable:global:_WpfToolsetBuildProj) {
    return $global:_WpfToolsetBuildProj
  }
  $nugetCache = GetNuGetPackageCachePath
  $msbuild_sdks = $GlobalJson.'msbuild-sdks'
  if ('Microsoft.DotNet.Arcade.Wpf.Sdk'  -in $msbuild_sdks.PSobject.Properties.Name) {
      $wpfToolsetVersion = $GlobalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Wpf.Sdk'
      $wpfToolsetLocationFile = Join-Path $ToolsetDir "$wpfToolsetVersion.txt"
      if (Test-Path $wpfToolsetLocationFile) {
        $path = Get-Content $wpfToolsetLocationFile -TotalCount 1
        if (Test-Path $path) {
          return $global:_WpfToolsetBuildProj = $path
        }
      }
      if (-not $restore) {
        Write-Host "Wpf Toolset version $toolsetVersion has not been restored." -ForegroundColor Red
        ExitWithExitCode 1
      }
      $proj = Join-Path $ToolsetDir "wpfRestore.proj"
      $bl = if ($binaryLog) { "/bl:" + (Join-Path $LogDir "WpfToolsetRestore.binlog") } else { "" }
      '<Project Sdk="Microsoft.DotNet.Arcade.Wpf.Sdk"/>' | Set-Content $proj
      MSBuild $proj $bl /t:__WriteToolsetLocation /clp:ErrorsOnly`;NoSummary /p:__ToolsetLocationOutputFile=$wpfToolsetLocationFile
      $path = Get-Content $wpfToolsetLocationFile -TotalCount 1
      if (!(Test-Path $path)) {
        throw "Invalid toolset path: $path"
      }
      return $global:_WpfToolsetBuildProj = $path
  }
}
InitializeWpfCustomToolset
. $PsScriptRoot\common\init-tools-native.ps1 -InstallDirectory $PSScriptRoot\..\.tools\native -GlobalJsonFile $PSScriptRoot\..\global.json