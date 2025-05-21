// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.Formatters.Binary;

namespace System;

public static class AppContextSwitchNames
{
    /// <summary>
    ///  The switch that controls whether or not the <see cref="BinaryFormatter"/> is enabled.
    /// </summary>
    public static string EnableUnsafeBinaryFormatterSerialization { get; }
        = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";

    /// <summary>
    ///  Switch that controls <see cref="AppContext"/> switch caching.
    /// </summary>
    public static string LocalAppContext_DisableCaching { get; }
        = "TestSwitch.LocalAppContext.DisableCaching";
}