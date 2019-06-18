// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

using System.Security;
using System.Security.Permissions;

using MS.Internal.PresentationCore;

namespace MS.Internal.Automation
{
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal class InteropAutomationProvider: IRawElementProviderFragmentRoot
    {
        #region Constructors

        internal InteropAutomationProvider(HostedWindowWrapper wrapper, AutomationPeer parent)
        {
            if (wrapper == null)
            {
                throw new ArgumentNullException("wrapper");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            _wrapper = wrapper;
            _parent = parent;
        }
        
        #endregion Constructors

        #region IRawElementProviderSimple
        
        ///
        ProviderOptions IRawElementProviderSimple.ProviderOptions
        {
            get { return ProviderOptions.ServerSideProvider | ProviderOptions.OverrideProvider; }
        }

        ///
        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            return null;
        }

        ///
        object IRawElementProviderSimple.GetPropertyValue(int propertyId)
        {
            return null;
        }

        /// <SecurityNote>
        ///     Critical    - Calls critical HostedWindowWrapper.Handle.
        ///     TreatAsSafe - The reason is described in the following comment by BrendanM
        ///         HostProviderFromHandle is a public method the APTCA assembly UIAutomationProvider.dll; 
        ///         ...\windows\AccessibleTech\longhorn\Automation\UIAutomationProvider\System\Windows\Automation\Provider\AutomationInteropProvider.cs
        ///         This calls through to an internal P/Invoke layer...
        ///         ...\windows\AccessibleTech\longhorn\Automation\UIAutomationProvider\MS\Internal\Automation\UiaCoreProviderApi.cs
        ///         Which P/Invokes to unmanaged UIAutomationCore.dll's UiaHostProviderFromHwnd API,
        ///         ...\windows\AccessibleTech\longhorn\Automation\UnmanagedCore\UIAutomationCoreAPI.cpp
        ///         Which checks the HWND with IsWindow, and returns  a new MiniHwndProxy instance:
        ///         ...\windows\AccessibleTech\longhorn\Automation\UnmanagedCore\MiniHwndProxy.cpp
        ///         
        ///         MiniHwndProxy does implement the IRawElementProviderSimple interface, but all methods 
        ///         return NULL or empty values; it does not expose any values or functionality through this. 
        ///         This object is designed to be an opaque cookie to contain the HWND so that only UIACore 
        ///         itself can access it. UIACore accesses the HWND by QI'ing for a private GUID, and then 
        ///         casting the returnd value to MiniHwndProxy, and calling a nonvirtual method to access a 
        ///         _hwnd field. While managed PT code maybe able to do a QI, the only way it could extract 
        ///         the _hwnd field would be by using unmanaged code.
        /// </SecurityNote>
        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider
        {
            get
            {
                return AutomationInteropProvider.HostProviderFromHandle(_wrapper.Handle);
            }
        }

        #endregion IRawElementProviderSimple

        #region IRawElementProviderFragment

        /// <SecurityNote>
        ///     TreatAsSafe - The reason this method can be treated as safe is because it yeilds information 
        ///         about the parent provider which can even otherwise be obtained by using public APIs such 
        ///         as UIElement.OnCreateAutomationPeer and AutomationProvider.ProviderFromPeer.
        /// </SecurityNote>
        IRawElementProviderFragment IRawElementProviderFragment.Navigate(NavigateDirection direction)
        {
            if (direction == NavigateDirection.Parent)
            {
                return (IRawElementProviderFragment)_parent.ProviderFromPeer(_parent);
            }

            return null;
        }

        ///
        int [] IRawElementProviderFragment.GetRuntimeId()
        {
            return null;
        }

        ///
        Rect IRawElementProviderFragment.BoundingRectangle
        {
            get { return Rect.Empty; }
        }

        ///
        IRawElementProviderSimple [] IRawElementProviderFragment.GetEmbeddedFragmentRoots()
        {
            return null;
        }

        ///        
        void IRawElementProviderFragment.SetFocus()
        {
            throw new NotSupportedException();
        }

        ///
        IRawElementProviderFragmentRoot IRawElementProviderFragment.FragmentRoot
        {
            get { return null; }
        }
        
        #endregion IRawElementProviderFragment

        #region IRawElementProviderFragmentRoot

        ///
        IRawElementProviderFragment IRawElementProviderFragmentRoot.ElementProviderFromPoint( double x, double y )
        {
            return null;
        }

        ///
        IRawElementProviderFragment IRawElementProviderFragmentRoot.GetFocus()
        {
            return null;
        }

        #endregion IRawElementProviderFragmentRoot

        #region Data

        private HostedWindowWrapper         _wrapper;
        private AutomationPeer              _parent;

        #endregion Data
    }
}

