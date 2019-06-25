// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Input.StylusPlugIns;
using System.Security;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusWisp
{
    /// <summary>
    /// Collection of StylusPlugIn objects
    /// </summary>
    /// <remarks>
    /// The collection order is based on the order that StylusPlugIn objects are
    /// added to the collection via the IList interfaces. The order of the StylusPlugIn
    /// objects in the collection is modifiable.
    /// Some of the methods are designed to be called from both the App thread and the Pen thread,
    /// but some of them are supposed to be called from one thread only. Please look at the 
    /// comments of each method for such an information.
    /// </remarks>
    internal class WispStylusPlugInCollection : StylusPlugInCollectionBase
    {
        #region Properties

        /// <summary>
        /// Get the current rect for the Element that the StylusPlugInCollection is attached to.
        /// May be empty rect if plug in is not in tree.
        /// </summary>
        internal override bool IsActiveForInput
        {
            get
            {
                return _penContexts != null;
            }
        }

        internal override object SyncRoot
        {
            get
            {
                return _penContexts != null ? _penContexts.SyncRoot : null;
            }
        }

        internal PenContexts PenContexts
        {
            get
            {
                return _penContexts;
            }
        }

        #endregion

        #region Private APIs

        internal override void UpdateState(UIElement element)
        {
            bool unhookPenContexts = true;

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy
            // which a call to Lock() can cause.
            using (element.Dispatcher.DisableProcessing())
            {
                // See if we should be enabled
                if (element.IsVisible && element.IsEnabled && element.IsHitTestVisible)
                {
                    PresentationSource presentationSource = PresentationSource.CriticalFromVisual(element as Visual);

                    if (presentationSource != null)
                    {
                        unhookPenContexts = false;

                        // Are we currently hooked up?  If not then hook up.
                        if (_penContexts == null)
                        {
                            InputManager inputManager = (InputManager)element.Dispatcher.InputManager;
                            PenContexts penContexts = StylusLogic.GetCurrentStylusLogicAs<WispLogic>().GetPenContextsFromHwnd(presentationSource);

                            // _penContexts must be non null or don't do anything.
                            if (penContexts != null)
                            {
                                _penContexts = penContexts;

                                lock (penContexts.SyncRoot)
                                {
                                    penContexts.AddStylusPlugInCollection(Wrapper);

                                    foreach (StylusPlugIn spi in Wrapper)
                                    {
                                        spi.InvalidateIsActiveForInput(); // Uses _penContexts being set to determine active state.
                                    }
                                    // Normally the Rect will be updated when we receive the LayoutUpdate. 
                                    // However there could be a race condition which the LayoutUpdate gets received 
                                    // before the properties like IsVisible being set.
                                    // So we should always force to call OnLayoutUpdated whenever the input is active.
                                    Wrapper.OnLayoutUpdated(this.Wrapper, EventArgs.Empty);
                                }
}
                        }
                    }
                }

                if (unhookPenContexts)
                {
                    Unhook();
                }
            }
        }

        internal override void Unhook()
        {
            // Are we currently unhooked?  If not then unhook.
            if (_penContexts != null)
            {
                lock (_penContexts.SyncRoot)
                {
                    _penContexts.RemoveStylusPlugInCollection(Wrapper);

                    // Can't recieve any more input now!
                    _penContexts = null;

                    // Notify after input is disabled to the PlugIns.
                    foreach (StylusPlugIn spi in Wrapper)
                    {
                        spi.InvalidateIsActiveForInput();
                    }
                }
            }
        }

        #endregion

        #region Fields

        // Note that this is only set when the Element is in a state to receive input (visible,enabled,in tree).
        private PenContexts _penContexts;

        #endregion
    }
}
