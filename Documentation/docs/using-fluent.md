
# Developer Guide : Using Fluent theme in WPF in .NET 9

In .NET 9, as part of the ongoing modernization in WPF, we introduced the Fluent ( Windows 11 ) theme. However there are still a lot of gaps that need to be filled. In this document, we describe the behaviors and general ways of using the new Fluent theme. Key-enhancements include : 

- Enable Fluent theme at two levels : Application and Window
- Support for Light and Dark themes
- Accent color support
- Default backdrop for windows when using Fluent theme

## Using Fluent theme in your WPF applications

Enabling Fluent theme in WPF application is supported at two levels : **Application** and **Window**.

There are two ways in which you can use \ enable Fluent theme in your WPF Applications - including the Fluent theme resource dictionaries or you can use the experimental **ThemeMode** APIs.

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

In .NET 9, we also provided experimental APIs : `Application.ThemeMode` and `Window.ThemeMode` which can be set from XAML or code-behind to enable the Fluent theme in WPF app.

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



