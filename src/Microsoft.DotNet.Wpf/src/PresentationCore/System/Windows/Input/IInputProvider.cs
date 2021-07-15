// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;

using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     An interface implemented by all input providers.
    /// </summary>
    internal interface IInputProvider
    {
        /// <summary>
        ///     Indicates if the provider is responsible for providing
        ///     input for the specified visual.
        /// </summary>
        bool ProvidesInputForRootVisual(Visual v);

        /// <summary>
        ///     Notifies the input provider that it is no longer 
        ///     the active input provider.  If the input provider
        ///     needs to report more input, it will need to reactivate.
        /// </summary>
        void NotifyDeactivate();
    }
}



