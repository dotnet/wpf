// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Collection of StylusPlugIn objects
    /// </summary>
    /// <remarks>
    /// The collection order is based on the order that StylusPlugIn objects are
    /// added to the collection via the IList interfaces. The order of the StylusPlugIn
    /// objects in the collection is modifiable.
    /// </remarks>
    internal class PointerStylusPlugInCollection : StylusPlugInCollectionBase
    {
        #region Properties

        /// <summary>
        /// Gets if the collection is hooked to a PointerStylusPluginManager
        /// </summary>
        /// <SecurityNote>
        /// Critical - Accesses SecurityCritical data _manager.
        /// TreatAsSafe - Just returns if _manager is null.  No data goes in or out.  Knowing
        ///               the fact that you can recieve real time input is something that is safe
        ///               to know and we want to expose.
        /// </SecurityNote>
        internal override bool IsActiveForInput
        {
            get
            {
                return _manager != null;
            }
        }

        /// <summary>
        /// WM_POINTER stack needs no sync root object here.
        /// </summary>
        internal override object SyncRoot
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Private APIs

        /// <summary>
        /// Hooks/Unhooks the plugin collection as needed.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Presentation source access
        ///            Calls SecurityCritical routines PresentationSource.CriticalFromVisual
        /// TreatAsSafe: 
        ///          - no data handed out or accepted
        /// </SecurityNote>
        internal override void UpdateState(UIElement element)
        {
            bool unhook = true;

            // See if we should be enabled
            if (element.IsVisible && element.IsEnabled && element.IsHitTestVisible)
            {
                PresentationSource presentationSource = PresentationSource.CriticalFromVisual(element as Visual);

                if (presentationSource != null)
                {
                    unhook = false;

                    // Are we currently hooked up?  If not then hook up.
                    if (_manager == null)
                    {
                        _manager = StylusLogic.GetCurrentStylusLogicAs<PointerLogic>().PlugInManagers[presentationSource];

                        // _manager must be non null or don't do anything.
                        if (_manager != null)
                        {
                            _manager.AddStylusPlugInCollection(Wrapper);

                            foreach (StylusPlugIn spi in Wrapper)
                            {
                                spi.InvalidateIsActiveForInput();
}
                            // 
                            // Normally the Rect will be updated when we receive the LayoutUpdate. 
                            // However there could be a race condition which the LayoutUpdate gets received 
                            // before the properties like IsVisible being set.
                            // So we should always force to call OnLayoutUpdated whenever the input is active.
                            Wrapper.OnLayoutUpdated(this.Wrapper, EventArgs.Empty);
                        }
                    }
                }                
            }

            if (unhook)
            {
                Unhook();
            }
        }

        /// <summary>
        /// Unhooks the plugin collection from the manager
        /// </summary>
        /// <SecurityNote>
        ///     SafeCritical:  Accesses _manager
        ///                    Does not receive or expose any secure information
        /// </SecurityNote>
        internal override void Unhook()
        {
            // Are we currently unhooked?  If not then unhook.
            if (_manager != null)
            {
                _manager.RemoveStylusPlugInCollection(Wrapper);

                // Can't recieve any more input now!
                _manager = null;

                // Notify after input is disabled to the PlugIns.
                foreach (StylusPlugIn spi in Wrapper)
                {
                    spi.InvalidateIsActiveForInput();
                }
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The manager for stylus plugins
        /// </summary>
        /// <SecurityNote>
        ///     Critical to prevent accidental spread to transparent code
        /// </SecurityNote>
        private PointerStylusPlugInManager _manager;

        #endregion
    }
}
