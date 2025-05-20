// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: UIAutomation/Visual bridge
//
//

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Threading;

namespace MS.Internal.Automation
{
    // Automation Proxy for WCP elements - exposes WCP tree
    // and properties to Automation.
    //
    // Currently builds tree based on Visual tree; many later incorporate
    // parts of logical tree.
    //
    // Currently exposes just BoundingRectangle, ClassName, IsEnabled
    // and IsKeyboardFocused properties.
    internal class ElementProxy: IRawElementProviderFragmentRoot, IRawElementProviderAdviseEvents
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // private ctor - the Wrap() pseudo-ctor is used instead.
        private ElementProxy(AutomationPeer peer)
        {
            if ((AutomationInteropReferenceType == ReferenceType.Weak) && 
                (peer is UIElementAutomationPeer || peer is ContentElementAutomationPeer || peer is UIElement3DAutomationPeer))
            {
                _peer = new WeakReference(peer);
            }
            else
            {
                _peer = peer;
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderFragmentRoot
        //
        //------------	------------------------------------------

        #region Interface IRawElementProviderFragmentRoot
        // Implementation of the interface that exposes this
        // to UIAutomation.
        //
        // Interface IRawElementProviderFragmentRoot 'derives' from
        // IRawElementProviderSimple and IRawElementProviderFragment,
        // methods from those appear first below.
        //
        // Most of the interface methods on this interface need
        // to get onto the Visual's context before they can access it,
        // so use Dispatcher.Invoke to call a private method (in the
        // private methods section of this class) that does the real work.

        // IRawElementProviderSimple methods...

        public object GetPatternProvider(int pattern)
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return ElementUtil.Invoke(peer, static (state, pattern) => state.InContextGetPatternProvider(pattern), this, pattern);
        }

        public object GetPropertyValue(int property)
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return ElementUtil.Invoke(peer, static (state, property) => state.InContextGetPropertyValue(property), this, property);
        }

        public ProviderOptions ProviderOptions
        {
            get 
            {
                AutomationPeer peer = Peer;
                if (peer == null)
                {
                    return ProviderOptions.ServerSideProvider;
                }
                return ElementUtil.Invoke(peer, static (state) => state.InContextGetProviderOptions(), this);
            }
        }  

        public IRawElementProviderSimple HostRawElementProvider
        {
            get
            {
                IRawElementProviderSimple host  = null;
                HostedWindowWrapper hwndWrapper = null;
                AutomationPeer peer = Peer;
                if (peer == null)
                {
                    return null;
                }
                hwndWrapper = ElementUtil.Invoke(peer, static (state) => state.InContextGetHostRawElementProvider(), this);

                if (hwndWrapper != null)
                    host = GetHostHelper(hwndWrapper);
                
                return host;
            }
        }

        private static IRawElementProviderSimple GetHostHelper(HostedWindowWrapper hwndWrapper)
        {
            return AutomationInteropProvider.HostProviderFromHandle(hwndWrapper.Handle);
        }

        // IRawElementProviderFragment methods...

        public IRawElementProviderFragment Navigate(NavigateDirection direction)
        {
            AutomationPeer peer = Peer;

            return peer is not null ? ElementUtil.Invoke(peer, static (state, direction) => state.InContextNavigate(direction), this, direction) : null;
        }

        public int[] GetRuntimeId()
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return ElementUtil.Invoke(peer, static (state) => state.InContextGetRuntimeId(), this);
        }

        public Rect BoundingRectangle
        {
            get
            {
                AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

                return ElementUtil.Invoke(peer, static (state) => state.InContextBoundingRectangle(), this);
            }
        }
        
        public IRawElementProviderSimple[] GetEmbeddedFragmentRoots()
        {
            return null;
        }
        
        public void SetFocus()
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            ElementUtil.Invoke(peer, state => ((ElementProxy)state).InContextSetFocus(), this);
        }

        public IRawElementProviderFragmentRoot FragmentRoot
        {
            get
            {
                AutomationPeer peer = Peer;

                return peer is not null ? ElementUtil.Invoke(peer, static (state) => state.InContextFragmentRoot(), this) : null;
            }
        }

        // IRawElementProviderFragmentRoot methods..
        public IRawElementProviderFragment ElementProviderFromPoint(double x, double y)
        {
            AutomationPeer peer = Peer;

            return peer is not null ? ElementUtil.Invoke(peer, static (state, point) => state.InContextElementProviderFromPoint(point), this, new Point(x, y)) : null;
        }

        public IRawElementProviderFragment GetFocus()
        {
            AutomationPeer peer = Peer;

            return peer is not null ? ElementUtil.Invoke(peer, static (state) => state.InContextGetFocus(), this) : null;
        }

        // Event support: EventMap is a static class and access is synchronized, so no need to access it in UI thread context.
        // Directly add or remove the requested EventId from the EventMap from here.
        // Note: We update the  EventMap even if peer is not available that's because we still have to keep track of number of
        // subscribers  for the given event.

        public void AdviseEventAdded(int eventID, int[] properties)
        {
            EventMap.AddEvent(eventID);
        }

        public void AdviseEventRemoved(int eventID, int[] properties)
        {
            EventMap.RemoveEvent(eventID);
        }

        #endregion Interface IRawElementProviderFragmentRoot

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // Wrap the found peer before shipping it over process boundary - static version for use outside ElementProxy.
        internal static ElementProxy StaticWrap(AutomationPeer peer, AutomationPeer referencePeer)
        {
            ElementProxy result = null;

            if (peer != null)
            {
                //referencePeer is well-connected, since UIA is asking us using it.
                //However we are trying to return the peer that is possibly just created on some 
                //element in the middle of un-walked tree and we need to make sure it is fully
                //"connected" or initialized before we return it to UIA.
                //This method ensures it has the right _parent and other hookup.
                peer = peer.ValidateConnected(referencePeer);
                if (peer != null)
                {
                    // Use the already created Wrapper proxy for peer if it is still alive
                    if(peer.ElementProxyWeakReference != null)
                    {
                        result = peer.ElementProxyWeakReference.Target as ElementProxy;
                    }
                    if(result == null)
                    {
                        result = new ElementProxy(peer);
                        peer.ElementProxyWeakReference = new WeakReference(result);
                    }

                    // If the peer corresponds to DataItem notify peer to add to storage to 
                    // keep reference of peers going to the UIA Client
                    if(result != null)
                    {
                        if(peer.IsDataItemAutomationPeer())
                        {
                            peer.AddToParentProxyWeakRefCache();
                        }
                    }
                }
            }

            return result;
        }


        // Needed to enable access from AutomationPeer.PeerFromProvider
        internal AutomationPeer Peer
        {
            get
            {
                if (_peer is WeakReference)
                {
                    AutomationPeer peer = (AutomationPeer)((WeakReference)_peer).Target;
                    return peer;
                }
                else
                {
                    return (AutomationPeer)_peer;
                }
            }
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // The signature of most of the folling methods is "object func( object arg )",
        // since that's what the Conmtext.Invoke delegate requires.
        // Return the element at specified coords.
        private IRawElementProviderFragment InContextElementProviderFromPoint(Point arg)
        {
            Point point = arg;
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            AutomationPeer peerFromPoint = peer.GetPeerFromPoint(point);
            return StaticWrap(peerFromPoint, peer);
        }

        // Return proxy representing currently focused element (if any)
        private IRawElementProviderFragment InContextGetFocus()
        {
            // Note: - what if a custom element - eg anchor in a text box - has focus?
            // won't have a UIElement there, can we even find the host?
            // If it implements Automation, can hand over to it, but if it doesn't,
            // would like nearest item, drill in using visual tree?
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            AutomationPeer focusedPeer = AutomationPeer.AutomationPeerFromInputElement(Keyboard.FocusedElement);
            return StaticWrap(focusedPeer, peer);
        }

        //  redirect to AutomationPeer
        private int InContextGetPatternProvider(int arg)
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return (int)peer.GetWrappedPattern(arg);
        }

        // Return proxy representing element in specified direction (parent/next/firstchild/etc.)
        private IRawElementProviderFragment InContextNavigate(NavigateDirection navigateDirection)
        {
            AutomationPeer dest;
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }

            switch (navigateDirection)
            {
                case NavigateDirection.Parent:
                    dest = peer.GetParent(); 
                    break;
                    
                case NavigateDirection.FirstChild:
                    if (!peer.IsInteropPeer)
                    {
                        dest = peer.GetFirstChild(); 
                    }
                    else
                    {
                        return peer.GetInteropChild();
                    }
                    break;
                    
                case NavigateDirection.LastChild:
                    if (!peer.IsInteropPeer)
                    {
                        dest = peer.GetLastChild(); 
                    }
                    else
                    {
                        return peer.GetInteropChild();
                    }
                    break;
                    
                case NavigateDirection.NextSibling:
                    dest = peer.GetNextSibling(); 
                    break;
                    
                case NavigateDirection.PreviousSibling:
                    dest = peer.GetPreviousSibling(); 
                    break;
                    
                default: 
                    dest = null; 
                    break;
            }

            return StaticWrap(dest, peer);
        }

    
        // Return value for specified property; or null if not supported
        private ProviderOptions InContextGetProviderOptions()
        {
            ProviderOptions options = ProviderOptions.ServerSideProvider;
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return options;
            }
            if (peer.IsHwndHost) 
                options |= ProviderOptions.OverrideProvider;
            
            return options;
        }

        // Return value for specified property; or null if not supported
        private int InContextGetPropertyValue(int property)
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return (int)peer.GetPropertyValue(property);
        }

        /// Returns whether this is the Root of the WCP tree or not
        private HostedWindowWrapper InContextGetHostRawElementProvider()
        {
            AutomationPeer peer = Peer;
 
            return peer?.GetHostRawElementProvider();
        }

        // Return unique ID for this element...
        private int[] InContextGetRuntimeId()
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return peer.GetRuntimeId();
        }

        // Return bounding rectangle (screen coords) for this element...
        private Rect InContextBoundingRectangle()
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            return peer.GetBoundingRectangle();
        }

        // Set focus to this element...
        private object InContextSetFocus()
        {
            AutomationPeer peer = Peer ?? throw new ElementNotAvailableException();

            peer.SetFocus();
            return null;
        }

        // Return proxy representing the root of this WCP tree...
        private IRawElementProviderFragmentRoot InContextFragmentRoot()
        {
            AutomationPeer peer = Peer;
            AutomationPeer root = peer;
            if (root == null)
            {
                return null;
            }
            while(true)
            {
                AutomationPeer parent = root.GetParent();
                if(parent == null) break;
                root = parent;
            }

            return StaticWrap(root, peer);
        }

        #region disable switch for ElementProxy weak reference fix

        internal enum ReferenceType
        {
            Strong,
            Weak
        }

        // Returns RefrenceType.Strong if key AutomationWeakReferenceDisallow  under  
        // "HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\Windows  Presentation Foundation\Features"
        // is set to 1 else returns ReferenceType.Weak. The registry key will be read only once.
        internal static ReferenceType AutomationInteropReferenceType
        {
            get
            {
                if (_shouldCheckInTheRegistry)
                {
                    if (RegistryKeys.ReadLocalMachineBool(RegistryKeys.WPF_Features, RegistryKeys.value_AutomationWeakReferenceDisallow))
                    {
                        _automationInteropReferenceType = ReferenceType.Strong;
                    }
                    _shouldCheckInTheRegistry = false;
                }
                return _automationInteropReferenceType;
            }
        }
        
        private static ReferenceType _automationInteropReferenceType = ReferenceType.Weak;
        private static bool _shouldCheckInTheRegistry = true;

        #endregion 

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private readonly object _peer;

        #endregion Private Fields
    }
}
