# Open global.json
$globaljsonpath = Join-Path $env:BUILD_SOURCESDIRECTORY 'global.json'
$jsondata = Get-Content -Raw -Path $globaljsonpath | ConvertFrom-Json

# Set DotNetCliVersion to global.json.tools.dotnet
$dotnetcliver = $jsondata.tools.dotnet
Write-Host "##vso[task.setvariable variable=DotNetCliVersion;]$dotnetcliver"