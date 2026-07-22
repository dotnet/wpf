// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: UIAutomation/Visual bridge
//
//

using System.Collections.Concurrent;
using System.Collections.Generic;
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
            // Weakly reference peers whose lifetime is owned elsewhere so UIA references don't pin
            // recycled/virtualized peers - and the controls they root - in memory. Data-item peers
            // are gated by an opt-out switch.
            if ((AutomationInteropReferenceType == ReferenceType.Weak) &&
                (peer is UIElementAutomationPeer || peer is ContentElementAutomationPeer || peer is UIElement3DAutomationPeer ||
                 (peer.IsDataItemAutomationPeer() && !CoreAppContextSwitches.UseStrongReferenceForItemAutomationPeers)))
            {
                _peer = new WeakReference(peer);

                // Let the sweep release this proxy's CCW once its peer is collected.
                if (!CoreAppContextSwitches.DisableUiaProviderDisconnect)
                {
                    ProxyDisconnector.Register(this, peer.Dispatcher);
                }
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

        public object GetPatternProvider ( int pattern )
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            return ElementUtil.Invoke(peer, new DispatcherOperationCallback( InContextGetPatternProvider ), pattern);
        }

        public object GetPropertyValue(int property)
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            return ElementUtil.Invoke(peer, new DispatcherOperationCallback(InContextGetPropertyValue), property);
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
                return (ProviderOptions)ElementUtil.Invoke(peer, state => ((ElementProxy)state).InContextGetProviderOptions(), this); 
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
                hwndWrapper = (HostedWindowWrapper)ElementUtil.Invoke(
                    peer, 
                    new DispatcherOperationCallback(InContextGetHostRawElementProvider), 
                    null);

                if(hwndWrapper != null)
                    host = GetHostHelper(hwndWrapper);
                
                return host;
            }
        }

        private IRawElementProviderSimple GetHostHelper(HostedWindowWrapper hwndWrapper)
        {
            return AutomationInteropProvider.HostProviderFromHandle(hwndWrapper.Handle);
        }

        // IRawElementProviderFragment methods...

        public IRawElementProviderFragment Navigate( NavigateDirection direction )
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            return (IRawElementProviderFragment)ElementUtil.Invoke(peer, new DispatcherOperationCallback(InContextNavigate), direction);
        }

        public int [ ] GetRuntimeId()
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                // Serve the cached id during disconnect so the call can identify this provider.
                if (_disconnecting && _cachedRuntimeId != null)
                {
                    return _cachedRuntimeId;
                }
                throw new ElementNotAvailableException();
            }
            int[] runtimeId = (int []) ElementUtil.Invoke( peer, state => ((ElementProxy)state).InContextGetRuntimeId(), this);
            if (_peer is WeakReference)
            {
                _cachedRuntimeId = runtimeId;
            }
            return runtimeId;
        }

        public Rect BoundingRectangle
        {
            get 
            {
                AutomationPeer peer = Peer;
                if (peer == null)
                {
                    throw new ElementNotAvailableException();
                }
                return (Rect)ElementUtil.Invoke(peer, state => ((ElementProxy)state).InContextBoundingRectangle(), this); 
            }
        }
        
        public IRawElementProviderSimple [] GetEmbeddedFragmentRoots()
        {
            return null;
        }
        
        public void SetFocus()
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            ElementUtil.Invoke(peer, state => ((ElementProxy)state).InContextSetFocus(), this);
        }

        public IRawElementProviderFragmentRoot FragmentRoot
        {
            get 
            {
                AutomationPeer peer = Peer;
                if (peer == null)
                {
                    return null;
                }
                return (IRawElementProviderFragmentRoot) ElementUtil.Invoke( peer, state => ((ElementProxy)state).InContextFragmentRoot(), this); 
            }
        }

        // IRawElementProviderFragmentRoot methods..
        public IRawElementProviderFragment ElementProviderFromPoint( double x, double y )
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            return (IRawElementProviderFragment) ElementUtil.Invoke( peer, new DispatcherOperationCallback( InContextElementProviderFromPoint ), new Point( x, y ) );
        }

        public IRawElementProviderFragment GetFocus()
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            return (IRawElementProviderFragment) ElementUtil.Invoke( peer, state => ((ElementProxy)state).InContextGetFocus(), this);
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

                            // Root the peer for the in-flight traversal: it is only weakly held by its proxy and
                            // could be collected between being returned to UIA and its first callback. KeepAlive
                            // self-guards on the switch / registry-key / data-item eligibility.
                            PeerKeepAlive.KeepAlive(peer);
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

                    // A data-item peer can be collected mid-walk when its row virtualizes out, surfacing ENA.
                    // Park a short-lived strong root (see PeerKeepAlive) that outlives the walk but is
                    // released once the peer stops being touched. KeepAlive self-guards on eligibility.
                    PeerKeepAlive.KeepAlive(peer);

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
        private object InContextElementProviderFromPoint( object arg )
        {
            Point point = (Point)arg;
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            AutomationPeer peerFromPoint = peer.GetPeerFromPoint(point);
            return StaticWrap(peerFromPoint, peer);
        }

        // Return proxy representing currently focused element (if any)
        private object InContextGetFocus()
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
        private object InContextGetPatternProvider(object arg)
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            return peer.GetWrappedPattern((int)arg);
        }

        // Return proxy representing element in specified direction (parent/next/firstchild/etc.)
        private object InContextNavigate( object arg )
        {
            NavigateDirection direction = (NavigateDirection) arg;
            AutomationPeer dest;
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }

            switch( direction )
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
        private object InContextGetProviderOptions()
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
        private object InContextGetPropertyValue ( object arg )
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            return peer.GetPropertyValue((int)arg);
        }

        /// Returns whether this is the Root of the WCP tree or not
        private object InContextGetHostRawElementProvider( object unused )
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                return null;
            }
            return peer.GetHostRawElementProvider();
        }

        // Return unique ID for this element...
        private object InContextGetRuntimeId()
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            return peer.GetRuntimeId();
        }

        // Return bounding rectangle (screen coords) for this element...
        private object InContextBoundingRectangle()
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            return peer.GetBoundingRectangle();
        }

        // Set focus to this element...
        private object InContextSetFocus()
        {
            AutomationPeer peer = Peer;
            if (peer == null)
            {
                throw new ElementNotAvailableException();
            }
            peer.SetFocus();
            return null;
        }

        // Return proxy representing the root of this WCP tree...
        private object InContextFragmentRoot()
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

        #region data-item peer keep-alive

        // Bounds the lifetime of weakly-referenced data-item peers so an in-flight UIA traversal cannot
        // observe one collected mid-walk, without reintroducing the unbounded leak. Touched peers are
        // strong-rooted and rotated out by a Background DispatcherTimer once untouched for a window.
        private static class PeerKeepAlive
        {
            // A peer stays rooted until untouched for one full window and at most two (9-18 s). 9 s is ~2x
            // the measured worst-case ~4.4 s gap between an element's surface and readback touches under
            // forced-Gen2 GC, so it survives blocking GC while still releasing idle peers within seconds.
            private static readonly TimeSpan Window = TimeSpan.FromSeconds(9);

            // Lock-free on the hot path: KeepAlive runs at the top of nearly every UIA interface call and only
            // stamps the peer with the current generation (a striped concurrent write). OnTick advances the
            // generation and drops peers untouched for two windows. Only the rare timer start/stop takes a lock.
            private static readonly ConcurrentDictionary<AutomationPeer, int> _tracked = new ConcurrentDictionary<AutomationPeer, int>();
            private static volatile int _generation;
            private static readonly object _timerLock = new object();
            private static volatile DispatcherTimer _timer;

            // Renew the keep-alive window for a weakly-held data-item peer. The full eligibility guard lives
            // here (not at the call sites) so the two callers cannot drift apart: the opt-out switch, the
            // legacy AutomationWeakReferenceDisallow registry key (surfaced as AutomationInteropReferenceType),
            // and data-item-ness. A null peer or non-weak reference type is a no-op.
            internal static void KeepAlive(AutomationPeer peer)
            {
                if (peer == null ||
                    CoreAppContextSwitches.UseStrongReferenceForItemAutomationPeers ||
                    AutomationInteropReferenceType != ReferenceType.Weak ||
                    !peer.IsDataItemAutomationPeer())
                {
                    return;
                }

                Dispatcher dispatcher = peer.Dispatcher;
                if (dispatcher == null)
                {
                    return;
                }

                // Skip the striped-lock write when this peer is already stamped for the current generation:
                // repeat touches within a window are the common case and need no update.
                if (!_tracked.TryGetValue(peer, out int g) || g != _generation)
                {
                    _tracked[peer] = _generation;
                }

                if (_timer == null)
                {
                    lock (_timerLock)
                    {
                        // The constructor associates the timer with the supplied dispatcher and starts it, so this
                        // is safe to call from the UIA worker thread that drives the proxy.
                        _timer ??= new DispatcherTimer(Window, DispatcherPriority.Background, OnTick, dispatcher);
                    }
                }
            }

            private static void OnTick(object sender, EventArgs e)
            {
                // Runs only on the timer's dispatcher thread, so it is the sole writer of _generation.
                int cutoff = ++_generation - 1;
                foreach (KeyValuePair<AutomationPeer, int> entry in _tracked)
                {
                    // Value-matched removal: a concurrent KeepAlive re-stamp changes the value, so a peer
                    // touched again mid-tick is not evicted while UIA is still using it.
                    if (entry.Value < cutoff)
                    {
                        _tracked.TryRemove(entry);
                    }
                }

                if (_tracked.IsEmpty)
                {
                    lock (_timerLock)
                    {
                        // Re-check under the lock so a peer added between the test and the stop keeps ticking.
                        if (_tracked.IsEmpty && _timer != null)
                        {
                            _timer.Stop();
                            _timer = null;

                            // A KeepAlive can add a peer and still see the not-yet-cleared timer; recreate it so
                            // that peer is not left rooted with no tick to release it.
                            if (!_tracked.IsEmpty)
                            {
                                _timer = new DispatcherTimer(Window, DispatcherPriority.Background, OnTick, Dispatcher.CurrentDispatcher);
                            }
                        }
                    }
                }
            }
        }

        #endregion data-item peer keep-alive

        #region dead-peer proxy disconnect

        // volatile: _cachedRuntimeId is written on the UIA worker thread but read on the dispatcher
        // thread during the re-entrant GetRuntimeId that UiaDisconnectProvider issues, so the read needs
        // an acquire barrier to avoid observing a stale null.
        private volatile bool _disconnecting;
        private volatile int[] _cachedRuntimeId;

        // True once the weakly-held peer has been collected. Must NOT touch Peer: doing so would hand out a
        // transient strong reference and defeat the very collection the sweep is waiting for. Safe because a
        // collected peer already throws ENA, so disconnect can't disturb a live walk.
        private bool IsPeerCollected
        {
            get { return _peer is WeakReference weak && weak.Target == null; }
        }

        private bool Disconnect()
        {
            _disconnecting = true;
            try
            {
                AutomationInteropProvider.DisconnectProvider(this);
                return true;
            }
            catch (Exception)
            {
                // Disconnect failed (e.g. transient input-sync re-entrancy); caller re-queues for retry.
                return false;
            }
            finally
            {
                _disconnecting = false;
            }
        }

        // Background sweep that disconnects weakly-held ElementProxy instances whose peer has been collected.
        private static class ProxyDisconnector
        {
            private static readonly TimeSpan Window = TimeSpan.FromSeconds(2);

            // Cap per-tick work so a large backlog drains over several ticks instead of one COM burst.
            private const int MaxDisconnectsPerTick = 5000;

            // Give up re-queuing a proxy after this many failed disconnects. Real transient failures clear
            // in 1-2 ticks, so this never trips for them; it caps the pathological case (a disconnect that
            // throws every time, e.g. runtime id never cached) so the timer can still self-stop.
            private const int MaxDisconnectAttempts = 8;

            private static readonly object _lock = new object();
            private static readonly List<WeakReference<ElementProxy>> _registry = new List<WeakReference<ElementProxy>>();

            // Attempt counts kept only for the rare re-queued failures, not as a field on every proxy.
            private static readonly Dictionary<ElementProxy, int> _failedAttempts = new Dictionary<ElementProxy, int>();
            private static DispatcherTimer _timer;

            internal static void Register(ElementProxy proxy, Dispatcher dispatcher)
            {
                if (dispatcher == null)
                {
                    return;
                }

                lock (_lock)
                {
                    _registry.Add(new WeakReference<ElementProxy>(proxy));
                    _timer ??= new DispatcherTimer(Window, DispatcherPriority.Background, OnTick, dispatcher);
                }
            }

            private static void OnTick(object sender, EventArgs e)
            {
                // Pick targets under the lock; disconnect outside it so Register never blocks.
                List<ElementProxy> toDisconnect = null;

                lock (_lock)
                {
                    int write = 0;
                    int disconnectCount = 0;

                    for (int read = 0; read < _registry.Count; read++)
                    {
                        WeakReference<ElementProxy> slot = _registry[read];
                        if (!slot.TryGetTarget(out ElementProxy proxy))
                        {
                            continue;
                        }

                        if (proxy.IsPeerCollected && disconnectCount < MaxDisconnectsPerTick)
                        {
                            (toDisconnect ??= new List<ElementProxy>()).Add(proxy);
                            disconnectCount++;
                            continue;
                        }

                        _registry[write++] = slot;
                    }

                    _registry.RemoveRange(write, _registry.Count - write);

                    if (_registry.Count == 0 && toDisconnect == null)
                    {
                        _timer.Stop();
                        _timer = null;
                    }
                }

                if (toDisconnect != null)
                {
                    List<ElementProxy> succeeded = null;
                    List<ElementProxy> failed = null;
                    foreach (ElementProxy proxy in toDisconnect)
                    {
                        if (proxy.Disconnect())
                        {
                            (succeeded ??= new List<ElementProxy>()).Add(proxy);
                        }
                        else
                        {
                            (failed ??= new List<ElementProxy>()).Add(proxy);
                        }
                    }

                    lock (_lock)
                    {
                        if (succeeded != null && _failedAttempts.Count != 0)
                        {
                            foreach (ElementProxy proxy in succeeded)
                            {
                                _failedAttempts.Remove(proxy);
                            }
                        }

                        if (failed != null)
                        {
                            // Re-queue a failed disconnect (its CCW is a GC root that would otherwise leak the
                            // proxy) until the attempt cap, then give up - leaking only the lightweight shell so
                            // the timer can still self-stop. The timer is alive because this tick had targets.
                            foreach (ElementProxy proxy in failed)
                            {
                                int attempts = _failedAttempts.TryGetValue(proxy, out int n) ? n + 1 : 1;
                                if (attempts < MaxDisconnectAttempts)
                                {
                                    _failedAttempts[proxy] = attempts;
                                    _registry.Add(new WeakReference<ElementProxy>(proxy));
                                }
                                else
                                {
                                    _failedAttempts.Remove(proxy);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion dead-peer proxy disconnect

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
