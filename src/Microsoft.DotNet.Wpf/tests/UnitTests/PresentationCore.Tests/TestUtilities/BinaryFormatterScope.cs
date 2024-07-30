// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace System;

/// <summary>
///  Scope for enabling / disabling the <see cref="BinaryFormatter"/>. Use in a <see langword="using"/> statement.
/// </summary>
public readonly ref struct BinaryFormatterScope
{
    private readonly AppContextSwitchScope _switchScope;

    public BinaryFormatterScope(bool enable)
    {
        // Prevent multiple BinaryFormatterScopes from running simultaneously. Using Monitor to allow recursion on
        // the same thread.
        Monitor.Enter(typeof(BinaryFormatterScope));
        _switchScope = new AppContextSwitchScope(AppContextSwitchNames.EnableUnsafeBinaryFormatterSerialization, enable);
    }

    public void Dispose()
    {
        try
        {
            _switchScope.Dispose();
        }
        finally
        {
            Monitor.Exit(typeof(BinaryFormatterScope));
        }
    }

    static BinaryFormatterScope()
    {
        // Need to explicitly set the switch to whatever the default is as its default value is in transition.

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter formatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        try
        {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(null!, null!);
#pragma warning restore SYSLIB0011
        }
        catch (NotSupportedException)
        {
            AppContext.SetSwitch(AppContextSwitchNames.EnableUnsafeBinaryFormatterSerialization, false);
            return;
        }
        catch (ArgumentNullException)
        {
            AppContext.SetSwitch(AppContextSwitchNames.EnableUnsafeBinaryFormatterSerialization, true);
            return;
        }

        throw new InvalidOperationException();
    }
}