using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
        MS.Internal.NativeWPFDLLLoader.LoadDwrite();
    }
    #pragma warning restore CA2255 
}
