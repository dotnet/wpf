// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Window pattern provider wrapper for WCP
//
//

using System.Windows.Threading;
using System.Windows.Automation;
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
    internal class WindowProviderWrapper: MarshalByRefObject, IWindowProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private WindowProviderWrapper( AutomationPeer peer, IWindowProvider iface)
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IWindowProvider
        //
        //------------------------------------------------------
 
        #region Interface IWindowProvider

        public void SetVisualState(WindowVisualState state)
        {
            ElementUtil.Invoke(_peer, static (state, visualState) => state.SetVisualState(visualState), _iface, state);
        }

        public void Close()
        {
            ElementUtil.Invoke(_peer, static (state) => state.Close(), _iface);
        }

        public bool WaitForInputIdle(int milliseconds)
        {
            return ElementUtil.Invoke(_peer, static (state, milliseconds) => state.WaitForInputIdle(milliseconds), _iface, milliseconds);
        }

        public bool Maximizable
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.Maximizable, _iface);
        }

        public bool Minimizable
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.Minimizable, _iface);
        }

        public bool IsModal
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.IsModal, _iface);
        }

        public WindowVisualState VisualState
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.VisualState, _iface);
        }

        public WindowInteractionState InteractionState
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.InteractionState, _iface);
        }

        public bool IsTopmost
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.IsTopmost, _iface);
        }

        #endregion Interface IWindowProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface)
        {
            return new WindowProviderWrapper( peer, (IWindowProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private IWindowProvider _iface;

        #endregion Private Fields
    }
}
