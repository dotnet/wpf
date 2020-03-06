// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Windows;
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    /// <summary>
    ///     Event args for DismissPopup event.
    /// </summary>
    public class RibbonDismissPopupEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// This is an instance constructor for the RibbonDismissPopupEventArgs class.  It
        /// is constructed with a reference to the event being raised.
        /// </summary>
        /// <returns>Nothing.</returns>
        public RibbonDismissPopupEventArgs()
            : this(RibbonDismissPopupMode.Always)
        {
        }

        public RibbonDismissPopupEventArgs(RibbonDismissPopupMode dismissMode)
            : base()
        {
            RoutedEvent = RibbonControlService.DismissPopupEvent;
            DismissMode = dismissMode;
        }

        public RibbonDismissPopupMode DismissMode
        {
            get;
            private set;
        }

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe RibbonDismissPopupEventArgs delegate for the DismissPopupEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        /// <returns>Nothing.</returns>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            RibbonDismissPopupEventHandler handler = (RibbonDismissPopupEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }
}