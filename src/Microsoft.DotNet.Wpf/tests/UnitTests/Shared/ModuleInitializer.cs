// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

internal static class ModuleInitializer
{
    /// <summary>
    /// Module initializer used as a workaround for https://github.com/dotnet/runtime/issues/111825.
    /// </summary>
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        // When a native dll fails to resolve, try loading it from the directory of the assembly loading the dll.
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += static (assembly, name) =>
        {
            return NativeLibrary.TryLoad(name, assembly, DllImportSearchPath.AssemblyDirectory, out nint handle) ? handle : 0;
        };
    }
}
