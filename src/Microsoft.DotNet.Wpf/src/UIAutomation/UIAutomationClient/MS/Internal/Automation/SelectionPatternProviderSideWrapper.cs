// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provider-side wrapper for Selectioin Pattern

using System;
using System.Diagnostics;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace MS.Internal.Automation
{
    // Provider-side wrapper for Selection pattern
    // converts params between RawElementServerWrapper and IRawElementProvider 
    internal class SelectionPatternProviderSideWrapper: MarshalByRefObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private SelectionPatternProviderSideWrapper(ISelectionProvider target)
        {
            Debug.Assert(target != null);
            _target = target;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( object target )
        {
            return new SelectionPatternProviderSideWrapper((ISelectionProvider)target);
        }

        // public so that RPC can call it
        public IRawElementProviderSimple[] GetSelection()
        {
            return _target.GetSelection();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        // public so that RPC can call it
        public bool CanSelectMultiple
        {
            get
            {
                return _target.CanSelectMultiple;
            }
        }

        // public so that RPC can call it
        public bool IsSelectionRequired
        {
            get
            {
                return _target.IsSelectionRequired;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private ISelectionProvider _target;

        #endregion Private Fields
    }
}
