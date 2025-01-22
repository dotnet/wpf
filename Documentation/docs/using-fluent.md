
# Developer Guide : Using Fluent theme in WPF in .NET 9

> [!NOTE]
> Fluent theme is still in experimental mode and there can be some breaking changes ( like UI behavior, the way in which Fluent is loaded, how the keys are referenced, ThemeMode APIs etc ) in the upcoming .NET releases.

In .NET 9, as part of the ongoing modernization in WPF, we introduced the Fluent ( Windows 11 ) theme. However there are still a lot of gaps that need to be filled. In this document, we describe the behaviors and general ways of using the new Fluent theme. Key-enhancements include : 

- Enable Fluent theme at two levels : Application and Window
- Support for Light and Dark themes
- Accent color support
- Default backdrop for windows when using Fluent theme

## Using Fluent theme in your WPF applications

Enabling Fluent theme in WPF application is supported at two levels : **Application** and **Window**.

There are two ways in which you can use \ enable Fluent theme in your WPF Applications - including the Fluent theme resource dictionaries or you can use the experimental **ThemeMode** APIs. 

> [!NOTE]
> Both the ways have the same effect i.e. the APIs only includes the ResourceDictionary as convenience.

### Setting Fluent theme by including Fluent theme ResourceDictionary

Fluent theme can be enabled for the whole application by including the Fluent theme ResourceDictionary in App.xaml as follows : 
```xml
<Application 
    x:Class="YourSampleApplication.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="clr-namespace:YourSampleApplication">
    <Application.Resources>
        <ResourceDictionary Source="pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml" />
    </Application.Resources>
</Application>
```

Similary Fluent theme can be enabled for a particular WPF window by including the ResourceDictionary in Window's XAML file as follows:
```xml
<Window
    x:Class="YourSampleApplication.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:local="clr-namespace:YourSampleApplication"
    mc:Ignorable="d"
    Title="MainWindow" Height="450" Width="800">
    <Window.Resources>
        <ResourceDictionary Source="pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml" />
    </Window.Resources>
    <Grid>

    </Grid>
</Window>
```

By default, when `Fluent.xaml` is included it reacts to the system theme changes. If you want your application to be in Light or Dark mode only include the following dictionaries respectively:
```xml
    <!-- For Fluent Light mode -->
    <ResourceDictionary Source="pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Light.xaml" />

    <!-- For Fluent Dark mode -->
    <ResourceDictionary Source="pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Dark.xaml" />
```

### Setting Fluent theme using the experimental ThemeMode APIs

In .NET 9, we also provided ***experimental APIs*** : `Application.ThemeMode` and `Window.ThemeMode` which can be set from XAML or code-behind to enable the Fluent theme in WPF app.

For setting ThemeMode on Application, you can do the following in App.xaml:
```xml
<Application 
    x:Class="YourSampleApplication.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="clr-namespace:YourSampleApplication"
    ThemeMode="Dark">
    <Application.Resources>
    
    </Application.Resources>
</Application>
```

Similary, we can set the ThemeMode on any Window as follows:
```xml
<Window
    x:Class="YourSampleApplication.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:local="clr-namespace:YourSampleApplication"
    mc:Ignorable="d"
    Title="MainWindow" Height="450" Width="800" ThemeMode="Dark">
    
</Window>
```

Since, the API is experimental, for using `ThemeMode` property in code-behind, we need to suppress **WPF0001** warning first. This can be done in the application's project file as follows:
```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);WPF0001</NoWarn>
</PropertyGroup>
```

or can be disabled using the `#pragma warning disable WPF0001` directive.

Once, the warning is suppressed, we can use the property from code-behind like this:

```cs
    // Sets Fluent theme on Application level
    Application.Current.ThemeMode = ThemeMode.Light;

    // Sets Fluent theme on one Window
    window.ThemeMode = ThemeMode.System;
```
There are 4 static properties defined in ThemeMode struct : `Light`, `Dark`, `System`, `None` and these are the only allowed values for `ThemeMode` right now.


### Expected behavior of the Fluent theme

Irrespective of how you have enabled the Fluent theme, the ThemeMode properties stay in sync with the ResourceDictionary's included in the `Application.Resources` and `Window.Resources`. The points below mention `ThemeMode` but they apply to both the scenarios.

1. When the `ThemeMode` is set to Light or Dark or System, the Fluent Themes are applied to the respective Application or Window.
2. The `ThemeMode` when set to System respects the current operating system's theme settings. This involves detecting whether the user is utilizing a light or dark theme as their App Mode.
3. When the `ThemeMode` is set to None, the Fluent Themes are not applied and the default `Aero2` theme is used.
4. Accent color changes will be adhered to whenever the Fluent Theme is applied irrespective of `ThemeMode`.
5. When the `ThemeMode` is set to a Window, it will take precedence over the Application's `ThemeMode`. In case Window `ThemeMode` is set to None, the window will adhere to Application's `ThemeMode`, even if Application uses Fluent Theme.
6. The default value of `ThemeMode` is None.
7. When **contrast themes** are enabled, HighContrast version of the Fluent theme is applied on the application. In this mode, there is no distinction between light and dark modes and all the window's will appear the same. When we switch back to normal themes, the previously set `ThemeMode` are applied on the windows.

> [!NOTE]
> Even if you include the ResourceDictionary, you will get the same behavior.

> [!NOTE]
> ThemeMode APIs are experimental and subject to change in the later .NET versions.

## Using Accent color brushes in WPF applications 

### When using Fluent theme

In Fluent theme color ResourceDictionary's ( Light.xaml, Dark.xaml and HC.xaml ) we have included the following accent color brushes which can be referred using DynamicResource extension and used for writing new styles or customizing existing control styles.

```xml
    <!-- Accent Color Brushes defined in Fluent color resource dictionaries  -->
    <SolidColorBrush x:Key="AccentTextFillColorPrimaryBrush" Color="{StaticResource SystemAccentColorLight3}" />
    <SolidColorBrush x:Key="AccentTextFillColorSecondaryBrush" Color="{StaticResource SystemAccentColorLight3}" />
    <SolidColorBrush x:Key="AccentTextFillColorTertiaryBrush" Color="{StaticResource SystemAccentColorLight2}" />
    <SolidColorBrush x:Key="AccentTextFillColorDisabledBrush" Color="{StaticResource AccentTextFillColorDisabled}" />
    
    <SolidColorBrush x:Key="TextOnAccentFillColorSelectedTextBrush" Color="{StaticResource TextOnAccentFillColorSelectedText}" />
    <SolidColorBrush x:Key="TextOnAccentFillColorPrimaryBrush" Color="{StaticResource TextOnAccentFillColorPrimary}" />
    <SolidColorBrush x:Key="TextOnAccentFillColorSecondaryBrush" Color="{StaticResource TextOnAccentFillColorSecondary}" />
    <SolidColorBrush x:Key="TextOnAccentFillColorDisabledBrush" Color="{StaticResource TextOnAccentFillColorDisabled}" />
    
    <SolidColorBrush x:Key="AccentFillColorSelectedTextBackgroundBrush" Color="{StaticResource SystemAccentColor}" />
    <SolidColorBrush x:Key="AccentFillColorDefaultBrush" Color="{StaticResource SystemAccentColorLight2}" />
    <SolidColorBrush x:Key="AccentFillColorSecondaryBrush" Opacity="0.9" Color="{StaticResource SystemAccentColorLight2}" />
    <SolidColorBrush x:Key="AccentFillColorTertiaryBrush" Opacity="0.8" Color="{StaticResource SystemAccentColorLight2}" />
    <SolidColorBrush x:Key="AccentFillColorDisabledBrush" Color="{StaticResource AccentFillColorDisabled}" />

    <SolidColorBrush x:Key="SystemFillColorAttentionBrush" Color="{StaticResource SystemAccentColor}" />    
 
```

In High Contrast mode, these brushes are updated to use the system highlight colors.

### Without using Fluent theme

Since, we have introduced AccentColor's similar to how other **SystemColors** were present in WPF, you can use accent colors and accent color brushes by directly referring the resource keys defined in **SystemColors** class like this :

```xml
    <StackPanel Orientation="Horizontal" Height="50">
        <StackPanel.Resources>
            <Style TargetType="Border">
                <Setter Property="Height" Value="50" />
                <Setter Property="Width" Value="30" />
            </Style>
        </StackPanel.Resources>
        <Border CornerRadius="2 0 0 2" Background="{DynamicResource {x:Static SystemColors.AccentColorDark3BrushKey}}" />
        <Border Background="{DynamicResource {x:Static SystemColors.AccentColorDark2BrushKey}}" />
        <Border Background="{DynamicResource {x:Static SystemColors.AccentColorDark1BrushKey}}" />
        <Border Background="{DynamicResource {x:Static SystemColors.AccentColorBrushKey}}" />
        <Border Background="{DynamicResource {x:Static SystemColors.AccentColorLight1BrushKey}}" />
        <Border Background="{DynamicResource {x:Static SystemColors.AccentColorLight2BrushKey}}" />
        <Border CornerRadius="0 2 2 0" Background="{DynamicResource {x:Static SystemColors.AccentColorLight3BrushKey}}" />
    </StackPanel>
```

> [!NOTE]
> When using AccentColor APIs directly, use Light1, Light2 and Light3 variations for Dark mode and Dark1, Dark2 and Dark3 for light mode in your application.

## Backdrop support in WPF

Unlike WinUI, WPF does not have `Acrylic` and `Mica` material, controllers or brushes. For now, to enable backdrop in WPF, we took advantage of the Desktop Window Manager ( DWM ) for specifying the system-drawn backdrop material on a window. We do this by calling the [DwmSetWindowAttribute function](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmsetwindowattribute) for the window.

We haven't provided any API yet, for switching between the system defined backdrop types ( see [DWM_SYSTEMBACKDROP_TYPE](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type) ), as we were not sure of the path we are going to take for this feature.

Rather, when Fluent theme is enabled we set Mica ( DWMSBT_MAINWINDOW ) backdrop on the Window. However, since we need to modify `PresentationSource.CompositionTarget.Background` and call the `DwmExtendFrameIntoClientArea` method, we have provided an  app-context switch to disable the applicaiton of backdrop on windows : `Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop`.

Developers can disable backdrop in their applications by including the switch as following in their project files : 
```xml
<ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop" Value="True" />
</ItemGroup>
```

or including it in your runtime config file as follows : 

```json
{
  "runtimeOptions": {
    "tfm": "net9.0",
    "frameworks": [
        // specifications...   
    ],
    "configProperties": {
      "Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop": true,
    }
  }
}
```

## Going ahead

There are a lot of issues in the styles and the current infrastructure of how Fluent theme is loaded, etc. and we will keep working iteratively to resolve these issues in .NET 10. There are still a lot of features that are still missing that we will work on in the next iteration.

Meanwhile, I would like to ask the community to go ahead and try out the new theme in their applications, and provide us with feedbacks via GitHub issues, discussions, feature suggestions and PRs to make it better.

> [!NOTE]
> While making PRs, make sure to run the `ThemeGenerator.Fluent.ps1` script to auto-generate the combined Fluent theme resource files.
