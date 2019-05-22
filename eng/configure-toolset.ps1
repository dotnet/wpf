#
# This file should be kept in sync across https://www.github.com/dotnet/wpf and dotnet-wpf-int repos. 
#

## Install pre-commit WPF Git hook to protect WPF's generated files
function InstallCustomWPFGitHooks {
  $WPFPreCommitGitHookSource = Join-Path $RepoRoot "eng\wpfarcadesdk\tools\pre-commit.githook"
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
  else
  {
     Write-Host "Detected existing WPF Git pre-commit hook."
  }
}

# This repo uses C++/CLI /clr:pure (or /clr:netcore) switches during compilation, which are 
# deprecated. Ensure that this warning is always suppressed during build.
if (($properties -eq $null) -or (-not ($properties -icontains '/nowarn:D9035'))) {
    $properties = @('/nowarn:D9035') + $properties
}

# This repo treats Solution/Project platform 'Any CPU' as ~= 'x86', and 
# defaults to 'Any CPU'/x86. 
if (($properties -eq $null) -or (-not ($properties -ilike '/p:Platform=*'))) {
    if (-not $platform) {
        $platform='x86'
    }
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

# Always install custom WPF Git hooks when not in the CI build
if (-not ($ci)) {
    InstallCustomWPFGitHooks 
}

