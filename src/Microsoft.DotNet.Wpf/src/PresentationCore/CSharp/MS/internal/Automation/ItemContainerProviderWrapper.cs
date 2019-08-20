// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// 
//
// Description: Item Container pattern provider wrapper for WPF
//
//

using System;
using System.Windows.Threading;

using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation
{
    // Automation/WPF Wrapper class: Implements that UIAutomation I...Provider
    // interface, and calls through to a WPF AutomationPeer which implements the corresponding
    // I...Provider inteface. Marshalls the call from the RPC thread onto the
    // target AutomationPeer's context.
    //
    // Class has two major parts to it:
    // * Implementation of the I...Provider, which uses Dispatcher.Invoke
    //   to call a private method (lives in second half of the class) via a delegate,
    //   if necessary, packages any params into an object param. Return type of Invoke
    //   must be cast from object to appropriate type.
    // * private methods - one for each interface entry point - which get called back
    //   on the right context. These call through to the peer that's actually
    //   implenting the I...Provider version of the interface. 
    internal class ItemContainerProviderWrapper : MarshalByRefObject, IItemContainerProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        private ItemContainerProviderWrapper(AutomationPeer peer, IItemContainerProvider iface)
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IItemContainerProvider
        //
        //------------------------------------------------------

        #region Interface IItemContainerProvider

        public IRawElementProviderSimple FindItemByProperty(IRawElementProviderSimple startAfter, int propertyId, object value)
        {
            object [] args = new object[]{startAfter, propertyId, value};
            return (IRawElementProviderSimple)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(FindItemByProperty), args);
        }

        #endregion Interface IItemContainerProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static object Wrap(AutomationPeer peer, object iface)
        {
            return new ItemContainerProviderWrapper(peer, (IItemContainerProvider)iface);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private object FindItemByProperty(object arg)
        {
            object[] args = (object[])arg;
            IRawElementProviderSimple startAfter = (IRawElementProviderSimple)args[0];
            int propertyId = (int)args[1];
            object value = (object)args[2];
            
            return _iface.FindItemByProperty(startAfter, propertyId, value);            
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private AutomationPeer _peer;
        private IItemContainerProvider _iface;

        #endregion Private Fields
    }
}
