// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: MoveSizeWinEventHandler implementation.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using MS.Win32;
using MS.Internal;

namespace System.Windows.Documents
{
    internal class MoveSizeWinEventHandler : WinEventHandler
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // ctor that takes a range of events
        internal MoveSizeWinEventHandler() : base(NativeMethods.EVENT_SYSTEM_MOVESIZEEND,
                                                  NativeMethods.EVENT_SYSTEM_MOVESIZEEND)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Register text store that will receive move/sice event.
        internal void RegisterTextStore(TextStore textstore)
        {
            if (_arTextStore == null)
            {
               _arTextStore = new ArrayList(1);
            }

            _arTextStore.Add(textstore);
        }

        // Unregister text store.
        internal void UnregisterTextStore(TextStore textstore)
        {
            _arTextStore.Remove(textstore);
        }

        // The callback from WinEvent.
        internal override void WinEventProc(int eventId, IntPtr hwnd)
        {
            Invariant.Assert(eventId == NativeMethods.EVENT_SYSTEM_MOVESIZEEND);
             
            if (_arTextStore != null)
            {
                for (int i = 0; i < _arTextStore.Count; i++)
                {
                    bool notified = false;
                    TextStore textstore = (TextStore)_arTextStore[i];

                    IntPtr hwndTemp = textstore.CriticalSourceWnd;
                    while (hwndTemp != IntPtr.Zero)
                    {
                        if (hwnd == hwndTemp)
                        {
                            // Only when the parent window of the source of this TextStore is
                            // moved or resized, we notfiy to Cicero.
                            textstore.OnLayoutUpdated();
                            notified = true;
                            break;
                        }
                        hwndTemp = UnsafeNativeMethods.GetParent(new HandleRef(this, hwndTemp));
                    }
                    if (!notified)
                        textstore.MakeLayoutChangeOnGotFocus();
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Number of TextStores listening to this event.
        internal int TextStoreCount
        {
            get
            {
                return _arTextStore.Count;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // list of the registered TextStores.
        private ArrayList _arTextStore;

        #endregion Private Fields
    }
}

