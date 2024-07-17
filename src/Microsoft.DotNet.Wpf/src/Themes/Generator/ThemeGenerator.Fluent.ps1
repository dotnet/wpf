[CmdletBinding(PositionalBinding=$false)]
Param(
    [string][Alias('c')]$themeColor = "Light"
)

$currentDir = Get-Location
$fluentThemeDir = Join-Path $currentDir "..\PresentationFramework.Fluent\"

$outFilePath = Join-Path $fluentThemeDir "Themes\Fluent.$themeColor.xaml"

$styleFilesDir = Join-Path $fluentThemeDir "Styles"
$resouceFilesDir = Join-Path $fluentThemeDir "Resources"
$themeColorFilePath = Join-Path $resouceFilesDir "Theme\$themeColor.xaml"

[xml]$combinedXaml = '<ResourceDictionary 
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" 
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
                        xmlns:sys="clr-namespace:System;assembly=mscorlib" 
                        xmlns:controls="clr-namespace:System.Windows.Controls;assembly=PresentationFramework" 
                        xmlns:fluentcontrols="clr-namespace:Fluent.Controls"
                        xmlns:system="clr-namespace:System;assembly=System.Runtime"
                        xmlns:ui="clr-namespace:System.Windows.Documents;assembly=PresentationUI"
                        xmlns:theme="clr-namespace:Microsoft.Windows.Themes"
                        xmlns:base="clr-namespace:System.Windows;assembly=WindowsBase">
                    </ResourceDictionary>'

foreach ($file in Get-ChildItem $resouceFilesDir -Filter "*.xaml") {
    if($file.BaseName -eq "Fluent") {
        continue
    }
    [xml]$currentXaml = Get-Content $file

    $combinedXaml.ResourceDictionary.InnerXml += $currentXaml.ResourceDictionary.InnerXml
}

[xml]$themeColorXaml = Get-Content $themeColorFilePath
$combinedXaml.ResourceDictionary.InnerXml += $themeColorXaml.ResourceDictionary.InnerXml

foreach ($file in Get-ChildItem $styleFilesDir -Filter "*.xaml") {
    [xml]$currentXaml = Get-Content $file

    $combinedXaml.ResourceDictionary.InnerXml += $currentXaml.ResourceDictionary.InnerXml
}

([xml]$combinedXaml).Save($outFilePath)