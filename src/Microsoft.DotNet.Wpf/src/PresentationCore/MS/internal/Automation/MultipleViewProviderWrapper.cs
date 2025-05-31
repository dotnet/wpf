// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Multiple View pattern provider wrapper for WCP
//
//

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation
{
    // Automation/WCP Wrapper class: Implements that UIAutomation I...Provider
    // interface, and calls through to a WCP AutomationPeer which implements the corresponding
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
    internal sealed class MultipleViewProviderWrapper : MarshalByRefObject, IMultipleViewProvider
    {
        private readonly AutomationPeer _peer;
        private readonly IMultipleViewProvider _iface;

        private MultipleViewProviderWrapper(AutomationPeer peer, IMultipleViewProvider iface)
        {
            Debug.Assert(peer is not null);
            Debug.Assert(iface is not null);

            _peer = peer;
            _iface = iface;
        }

        public string GetViewName(int viewID)
        {
            return ElementUtil.Invoke(_peer, static (state, viewID) => state.GetViewName(viewID), _iface, viewID);
        }

        public void SetCurrentView(int viewID)
        {
            ElementUtil.Invoke(_peer, static (state, viewID) => state.SetCurrentView(viewID), _iface, viewID);
        }

        public int CurrentView
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.CurrentView, _iface);
        }

        public int[] GetSupportedViews()
        {
            return ElementUtil.Invoke(_peer, static (state) => state.GetSupportedViews(), _iface);
        }

        internal static object Wrap(AutomationPeer peer, object iface)
        {
            return new MultipleViewProviderWrapper(peer, (IMultipleViewProvider)iface);
        }
    }
}
