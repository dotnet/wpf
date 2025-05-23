$themeColors=@("Light", "Dark", "HC", "System");

$currentDir = Get-Location
$fluentThemeDir = Join-Path $currentDir "..\PresentationFramework.Fluent\"

$styleFilesDir = Join-Path $fluentThemeDir "Styles"
$resouceFilesDir = Join-Path $fluentThemeDir "Resources"

foreach($themeColor in $themeColors)
{
    $outFilePath = Join-Path $fluentThemeDir "Themes\Fluent.$themeColor.xaml"
    $themeColorFilePath = Join-Path $resouceFilesDir "Theme\$themeColor.xaml"
    if($themeColor -eq "System") {
        $outFilePath = Join-Path $fluentThemeDir "Themes\Fluent.xaml"
    }
   
    [xml]$combinedXaml = '<ResourceDictionary 
                            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" 
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
                            xmlns:sys="clr-namespace:System;assembly=mscorlib" 
                            xmlns:controls="clr-namespace:System.Windows.Controls;assembly=PresentationFramework" 
                            xmlns:fluentcontrols="clr-namespace:Fluent.Controls"
                            xmlns:system="clr-namespace:System;assembly=System.Runtime"
                            xmlns:ui="clr-namespace:System.Windows.Documents;assembly=PresentationUI"
                            xmlns:theme="clr-namespace:Microsoft.Windows.Themes"
                            xmlns:framework="clr-namespace:MS.Internal;assembly=PresentationFramework"
                            xmlns:base="clr-namespace:System.Windows;assembly=WindowsBase">
                        </ResourceDictionary>'
                        
    foreach ($file in Get-ChildItem $resouceFilesDir -Filter "*.xaml") {
        if($file.BaseName -eq "Fluent") {
            continue
        }
        [xml]$currentXaml = Get-Content $file.FullName -Encoding UTF8
        
        $combinedXaml.ResourceDictionary.InnerXml += $currentXaml.ResourceDictionary.InnerXml
    }
    
    if($themeColor -ne "System") {
        [xml]$themeColorXaml = Get-Content $themeColorFilePath -Encoding UTF8
        $combinedXaml.ResourceDictionary.InnerXml += $themeColorXaml.ResourceDictionary.InnerXml
    }
    
    foreach ($file in Get-ChildItem $styleFilesDir -Filter "*.xaml") {
        [xml]$currentXaml = Get-Content $file.FullName -Encoding UTF8
        $combinedXaml.ResourceDictionary.InnerXml += $currentXaml.ResourceDictionary.InnerXml
    }

    ([xml]$combinedXaml).Save($outFilePath)
}