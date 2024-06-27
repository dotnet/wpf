[CmdletBinding(PositionalBinding=$false)]
Param(
    [string][Alias('c')]$themeColor = "Light",
    [switch] $defaultMode
)

function Remove-OverridesDefaultStyle($xaml) {
    $xaml.ResourceDictionary.Style.ForEach({
        $_.Setter.ForEach({
            if ($_.Property -eq "OverridesDefaultStyle") {
                $_.ParentNode.RemoveChild($_) > $null
            }
        })
    })
}

function Convert-DynamicResourceToStaticResource($xaml) {
    foreach ($node in $xaml.ChildNodes) {
        if ($node.HasChildNodes) {
            Convert-DynamicResourceToStaticResource $node
        }

        if ($node.HasAttributes) {
            foreach ($attr in $node.Attributes) {
                $attr.Value = $attr.Value -replace "DynamicResource", "StaticResource"
            }
        }
    }
}

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

if ($defaultMode) {
    Remove-OverridesDefaultStyle $combinedXaml
    Convert-DynamicResourceToStaticResource $combinedXaml.ResourceDictionary
}

([xml]$combinedXaml).Save($outFilePath)