// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Security;
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
        private PointerStylusPlugInManager _manager;

        #endregion
    }
}
