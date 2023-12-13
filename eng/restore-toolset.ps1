#
# This file should be kept in sync across https://www.github.com/dotnet/wpf and dotnet-wpf-int repos. 
#

# One-time setup for Wpf's custom toolset
function InitializeWpfCustomToolset() {
  if (Test-Path variable:global:_WpfToolsetBuildProj) {
    return $global:_WpfToolsetBuildProj
  }
  $nugetCache = GetNuGetPackageCachePath

  # Get all sdks listed in repo's 'global.json' file
  $msbuild_sdks = $GlobalJson.'msbuild-sdks'

  # Determine if WpfArcadeSdk is present in this repo's 'global.json' file.
  #
  # The Arcade.Wpf.Sdk will only be present in 'global.json' if it is not available in the
  # local repo (under repo_root/eng/wpfarcadesdk).  The WpfArcadeSdk will be available
  # in the internal WPF repo's 'global.json' only (dotnet-wpf-int), as it needs to resolve
  # the location during build time from the NuGet cache.  The public WPF GitHub repo has 
  # a local copy of the WPF Arcade SDK and a 'global.json' entry for the sdk is not required.
  if ('Microsoft.DotNet.Arcade.Wpf.Sdk'  -in $msbuild_sdks.PSobject.Properties.Name) {

      # Get the version of the Wpf Arcade SDK for the toolset location file name
      $wpfToolsetVersion = $GlobalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Wpf.Sdk'
      $wpfToolsetLocationFile = Join-Path $ToolsetDir "$wpfToolsetVersion.txt"

      # If toolset file already exists, one-time setup has already run
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

      # Install WPF git hooks when WpfArcadeSdk is located in the NuGet cache (dotnet-wpf-int)
      if (!$ci)
      {
          $installGitHooksProject = Join-Path $ToolsetDir "wpfInstallWPFPreCommitGitHook.proj"
          '<Project Sdk="Microsoft.DotNet.Arcade.Wpf.Sdk"/>' | Set-Content $installGitHooksProject
          $installGitHooksBinLog = if ($binaryLog) { "/bl:" + (Join-Path $LogDir "InstallGitHooks.binlog") } else { "" }
          MSBuild $installGitHooksProject $installGitHooksBinlog /t:InstallWPFPreCommitGitHook /clp:ErrorsOnly`;NoSummary 
      }

      # Write toolset location (e.g., dotnet-wpf-int\artifacts\toolset\4.8.0-preview7.19322.1.txt)
      $proj = Join-Path $ToolsetDir "wpfRestore.proj"
      $bl = if ($binaryLog) { "/bl:" + (Join-Path $LogDir "WpfToolsetRestore.binlog") } else { "" }
      '<Project Sdk="Microsoft.DotNet.Arcade.Wpf.Sdk"/>' | Set-Content $proj
      MSBuild $proj $bl /t:__WriteToolsetLocation /clp:ErrorsOnly`;NoSummary /p:__ToolsetLocationOutputFile=$wpfToolsetLocationFile

      # Verify toolset file was successfully written
      $path = Get-Content $wpfToolsetLocationFile -TotalCount 1
      if (!(Test-Path $path)) {
        throw "Invalid toolset path: $path"
      }

      return $global:_WpfToolsetBuildProj = $path
  }
}

# Installs custom WPF git hook to prevent modification of generated files 
function InstallCustomWPFGitHooksFromLocalToolsPath {

  # Install the githook using the inline task if WpfArcadeSdk is located in 
  # engineering root.  This should only be the case for the public GitHub repo
  # (e.g., dotnet-wpf/eng/wpfarcadesdk.)
  $WPFArcadeSDKPath = Join-Path $EngRoot "wpfarcadesdk";

  if (Test-Path $WPFArcadeSDKPath) {

    # Install the githook using the script
    $WPFPreCommitGitHookSource = Join-Path $EngRoot "wpfarcadesdk\tools\pre-commit.githook"
    $WPFPreCommitGitHookDest = Join-Path $RepoRoot ".git\hooks\pre-commit"

    if (-not (Test-Path $WPFPreCommitGitHookSource)) {
        Write-Host "WPF PreCommit GitHook file is missing: $WPFPreCommitGitHookSource"
        ExitWithExitCode 1
    }

    Write-Host "Detecting WPF Git hooks..."

    if (-not (Test-Path $WPFPreCommitGitHookDest)) {
         Write-Host "Installing WPF Git pre-commit hook..."
         try {
            Copy-Item -Path $WPFPreCommitGitHookSource -Destination $WPFPreCommitGitHookDest 
         }
         catch {
          Write-Host "Error: WPF Git pre-commit hook installation failed!"
          Write-Host $_
          Write-Host $_.Exception
          Write-Host $_.ScriptStackTrace
          ExitWithExitCode 1
        }
    }
    else {
     Write-Host "Detected existing WPF Git pre-commit hook."
    }
  }
  else
  {
      Write-Host "InstallCustomWPFGitHooks: WpfArcadeSdk was not available in repo's engineering root.";
  }
}

InitializeWpfCustomToolset

if (!$ci)
{
    InstallCustomWPFGitHooksFromLocalToolsPath 
}

. $PsScriptRoot\common\init-tools-native.ps1 -InstallDirectory $PSScriptRoot\..\.tools\native -GlobalJsonFile $PSScriptRoot\..\global.json

