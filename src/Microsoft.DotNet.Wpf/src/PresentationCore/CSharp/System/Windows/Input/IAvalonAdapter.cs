// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Interface implemented by AvalonAdapter in WinFormsIntegration to provide KeyboardNavigation 
//              with information to determine how to process a Keytab coming from ElementHost.
//

using MS.Internal.PresentationCore;
using System.Runtime.CompilerServices;

namespace System.Windows.Input
{
    /// <summary>
    ///     Interface for AvalonAdapter, provides an extended OnNoMoreTabStops from IKeyboardInputSite.
    /// </summary>
    internal interface IAvalonAdapter
    {
        bool OnNoMoreTabStops(TraversalRequest request, ref bool ShouldCycle);
    }

    /// <summary>
    ///     Implementation of IAvalonAdapter, we need this to prevent the linker from optimizing IAvalonAdapter away.
    /// </summary>
    internal class AvalonAdapterImpl : IAvalonAdapter
    {
        bool IAvalonAdapter.OnNoMoreTabStops(TraversalRequest request, ref bool ShouldCycle)
        {
            return false;
        }
    }
}



