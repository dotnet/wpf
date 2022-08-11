using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class ModuleInitializer
{
    /// <summary>
    /// DirectWriteForwarder has a module constructor that implements
    /// the setting of the default DPI awareness for the process.
    /// We need to load DirectWriteForwarder the instant PresentationCore
    /// loads in order to ensure that this is set before any DPI sensitive
    /// operations are carried out.  To do this, we simply call LoadDwrite
    /// as the module constructor for DirectWriteForwarder would do this anyway.
    /// </summary>
#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
    {
        IsProcessDpiAware();

        MS.Internal.NativeWPFDLLLoader.LoadDwrite();
    }
#pragma warning restore CA2255

    private static void IsProcessDpiAware()
    {
        bool disableDpiAware = false;

        // By default, Application is DPIAware.
        Assembly assemblyApp = Assembly.GetEntryAssembly();

        // Check if the Application has explicitly set DisableDpiAwareness attribute.
        if (assemblyApp != null && Attribute.IsDefined(assemblyApp, typeof(System.Windows.Media.DisableDpiAwarenessAttribute)))
        {
            disableDpiAware = true;
        }

        if (!disableDpiAware)
        {
            // DpiAware composition is enabled for this application.
            SetProcessDPIAware_Internal();
        }

        // Only when DisableDpiAwareness attribute is set in Application assembly,
        // It will ignore the SetProcessDPIAware API call.
    }

    [DllImport("user32.dll", EntryPoint = "SetProcessDPIAware")]
    private static extern void SetProcessDPIAware_Internal();
}
