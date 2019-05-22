// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: CurrentChanging event arguments
//

using System;
using System.Windows;
using MS.Internal.WindowsBase;

namespace System.ComponentModel
{
    /// <summary>
    /// Arguments for the CurrentChanging event.
    /// A collection that supports ICollectionView raises this event
    /// whenever the CurrentItem is changing, or when the contents
    /// of the collection has been reset.
    /// By default, the event is cancelable when CurrentChange is 
    /// caused by a move current operation and uncancelable when 
    /// caused by an irreversable collection change operation.
    /// </summary>
    public class CurrentChangingEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Construct a cancelable CurrentChangingEventArgs that is used
        /// to notify listeners when CurrentItem is about to change.
        /// </summary>
        public CurrentChangingEventArgs()
        {
            Initialize(true);
        }

        /// <summary>
        /// Construct a CurrentChangingEventArgs that is used to notify listeners when CurrentItem is about to change.
        /// </summary>
        /// <param name="isCancelable">if false, setting Cancel to true will cause an InvalidOperationException to be thrown.</param>
        public CurrentChangingEventArgs(bool isCancelable)
        {
            Initialize(isCancelable);
        }

        private void Initialize(bool isCancelable)
        {
            _isCancelable = isCancelable;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// If this event can be canceled.  When this is False, setting Cancel to True will cause an InvalidOperationException to be thrown.
        /// </summary>
        public bool IsCancelable
        {
            get { return _isCancelable; }
        }

        /// <summary>
        /// When set to true, this event will be canceled.
        /// </summary>
        /// <remarks>
        /// If IsCancelable is False, setting this value to True will cause an InvalidOperationException to be thrown.
        /// </remarks>
        public bool Cancel
        {
            get { return _cancel; }
            set
            {
                if (IsCancelable)
                {
                    _cancel = value;
                }
                else if (value)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CurrentChangingCannotBeCanceled));
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private bool _cancel = false;
        private bool _isCancelable;
    }

    /// <summary>
    ///     The delegate to use for handlers that receive the CurrentChanging event.
    /// </summary>
    public delegate void CurrentChangingEventHandler(object sender, CurrentChangingEventArgs e);
}


