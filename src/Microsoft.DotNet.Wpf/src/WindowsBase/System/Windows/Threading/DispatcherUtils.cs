// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Windows.Threading;

internal static class DispatcherUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DispatcherSynchronizationContext GetOrCreateContext(Dispatcher dispatcher, DispatcherPriority priority)
    {
        // Not used since NETFX 4.5
        if (BaseCompatibilityPreferences.GetReuseDispatcherSynchronizationContextInstance())
        {
            return dispatcher._defaultDispatcherSynchronizationContext;
        }

        // This is our general path without any special app context switches
        if (BaseCompatibilityPreferences.GetFlowDispatcherSynchronizationContextPriority())
        {
            return new DispatcherSynchronizationContext(dispatcher, priority);
        }

        // Compatibility path
        return new DispatcherSynchronizationContext(dispatcher, DispatcherPriority.Normal);
    }
}
