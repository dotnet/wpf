// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Windows;
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    /// <summary>
    ///     Event args for KeyTipService.KeyTipAccessedEvent
    /// </summary>
    public class KeyTipAccessedEventArgs : RoutedEventArgs
    {
        public KeyTipAccessedEventArgs()
        {
        }

        /// <summary>
        ///     This property determines what will be the
        ///     next keytip scope after routing this event.
        /// </summary>
        public DependencyObject TargetKeyTipScope { get; set; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            KeyTipAccessedEventHandler handler = (KeyTipAccessedEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }

    /// <summary>
    ///     Event handler type for KeyTipService.KeyTipAccessedEvent.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void KeyTipAccessedEventHandler(object sender,
                                    KeyTipAccessedEventArgs e);
}