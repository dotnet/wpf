// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define ENABLE_AUTOMATIONPEER_LOGGING   // uncomment to include logging of various activities

using System;
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

using MS.Internal;
using MS.Internal.Automation;
using MS.Internal.Media;
using MS.Internal.PresentationCore;
using MS.Win32;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Automation.Peers
{
    ///
    public enum PatternInterface
    {
        ///
        Invoke,
        ///
        Selection,
        ///
        Value,
        ///
        RangeValue,
        ///
        Scroll,
        ///
        ScrollItem,
        ///
        ExpandCollapse,
        ///
        Grid,
        ///
        GridItem,
        ///
        MultipleView,
        ///
        Window,
        ///
        SelectionItem,
        ///
        Dock,
        ///
        Table,
        ///
        TableItem,
        ///
        Toggle,
        ///
        Transform,
        ///
        Text,
        ///
        ItemContainer,
        ///
        VirtualizedItem,
        ///
        SynchronizedInput,
    }

    ///
    public enum AutomationOrientation
    {
        ///
        None = 0,
        ///
        Horizontal,
        ///
        Vertical,
    }

    ///
    public enum AutomationControlType
    {
        ///
        Button,
        ///
        Calendar,
        ///
        CheckBox,
        ///
        ComboBox,
        ///
        Edit,
        ///
        Hyperlink,
        ///
        Image,
        ///
        ListItem,
        ///
        List,
        ///
        Menu,
        ///
        MenuBar,
        ///
        MenuItem,
        ///
        ProgressBar,
        ///
        RadioButton,
        ///
        ScrollBar,
        ///
        Slider,
        ///
        Spinner,
        ///
        StatusBar,
        ///
        Tab,
        ///
        TabItem,
        ///
        Text,
        ///
        ToolBar,
        ///
        ToolTip,
        ///
        Tree,
        ///
        TreeItem,
        ///
        Custom,
        ///
        Group,
        ///
        Thumb,
        ///
        DataGrid,
        ///
        DataItem,
        ///
        Document,
        ///
        SplitButton,
        ///
        Window,
        ///
        Pane,
        ///
        Header,
        ///
        HeaderItem,
        ///
        Table,
        ///
        TitleBar,
        ///
        Separator,
    }

    ///
    public enum AutomationEvents
    {
        ///
        ToolTipOpened,
        ///
        ToolTipClosed,
        ///
        MenuOpened,
        ///
        MenuClosed,
        ///
        AutomationFocusChanged,
        ///
        InvokePatternOnInvoked,
        ///
        SelectionItemPatternOnElementAddedToSelection,
        ///
        SelectionItemPatternOnElementRemovedFromSelection,
        ///
        SelectionItemPatternOnElementSelected,
        ///
        SelectionPatternOnInvalidated,
        ///
        TextPatternOnTextSelectionChanged,
        ///
        TextPatternOnTextChanged,
        ///
        AsyncContentLoaded,
        ///
        PropertyChanged,
        ///
        StructureChanged,
        ///
        InputReachedTarget,
        ///
        InputReachedOtherElement,
        ///
        InputDiscarded,
        ///
        LiveRegionChanged,
    }


    ///<summary> This is a helper class to facilate the storage of Security critical data ( aka "Plutonium")
    /// What is "critical data" ? This is any data created that required an Assert for it's creation.
    ///</summary> As an example - the passage of hosted Hwnd between some AutomationPeer and UIA infrastructure.
    public sealed class HostedWindowWrapper
    {
        /// <summary>
        /// This is the only public constructor on this class.
        /// </summary>
        public HostedWindowWrapper(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        private HostedWindowWrapper()
        {
            _hwnd = IntPtr.Zero;
        }

        internal static HostedWindowWrapper CreateInternal(IntPtr hwnd)
        {
            HostedWindowWrapper wrapper = new HostedWindowWrapper();
            wrapper._hwnd = hwnd;
            return wrapper;
        }

        internal IntPtr Handle
        {
            get
            {
                return _hwnd;
            }
        }

        private IntPtr _hwnd;
    }


    ///
    public abstract class AutomationPeer: DispatcherObject
    {
        ///
        static AutomationPeer()
        {
            // Disable message processing to avoid re-entrancy (WM_GETOBJECT)
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                Initialize();
            }
        }

#if ENABLE_AUTOMATIONPEER_LOGGING
        protected AutomationPeer()
        {
            LogPeer(this);
        }
#endif

        //
        // VIRTUAL CALLBACKS
        //

        ///
        abstract protected List<AutomationPeer> GetChildrenCore();

        ///
        abstract public object GetPattern(PatternInterface patternInterface);


        //
        // PUBLIC METHODS
        //

        ///
        public void InvalidatePeer()
        {
            if(_invalidated) return;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, _updatePeer, this);
            _invalidated = true;
        }

        ///<summary>
        /// Used to check if Automation is indeed listening for the event.
        /// Typical usage is to check this before even creating the peer that will fire the event.
        /// Basically, this is a performance measure since if the Automation does not listen for the event,
        /// it does not make sense to create a peer to fire one.
        /// NOTE: the method is static and only answers if there is some listener in Automation,
        /// not specifically for some element. The Automation can hook up "broadcast listeners" so the
        /// per-element info is basically unavailable.
        ///</summary>
        static public bool ListenerExists(AutomationEvents eventId)
        {
            return (EventMap.HasRegisteredEvent(eventId));
        }

        ///<summary>
        /// Used by peer implementation to raise an event for Automation
        ///</summary>
        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void RaiseAutomationEvent(AutomationEvents eventId)
        {
            AutomationEvent eventObject = EventMap.GetRegisteredEvent(eventId);

            if (eventObject == null)
            {
                // nobody is listening to this event
                return;
            }

            IRawElementProviderSimple provider = ProviderFromPeer(this);
            if (provider != null)
            {
                AutomationInteropProvider.RaiseAutomationEvent(
                    eventObject,
                    provider,
                    new AutomationEventArgs(eventObject));
            }
        }

        /// <summary>
        /// This method is called by implementation of the peer to raise the automation propertychange notifications
        /// Typically, the peers that implement automation patterns liek IScrollProvider need to raise events specified by
        /// the particular pattern in case specific properties are changing.
        /// </summary>
        // Never inline, as we don't want to unnecessarily link the automation DLL via the ScrollPattern reference.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void RaisePropertyChangedEvent(AutomationProperty property, object oldValue, object newValue)
        {
            // Only send the event if there are listeners for this property change
            if (AutomationInteropProvider.ClientsAreListening)
            {
                RaisePropertyChangedInternal(ProviderFromPeer(this), property,oldValue,newValue);
            }
        }

        /// <summary>
        /// This method is called by implementation of the peer to raise the automation "async content loaded" notifications
        /// </summary>
        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void RaiseAsyncContentLoadedEvent(AsyncContentLoadedEventArgs args)
        {
            if(args == null)
                throw new ArgumentNullException("args");

            if (EventMap.HasRegisteredEvent(AutomationEvents.AsyncContentLoaded))
            {
                IRawElementProviderSimple provider = ProviderFromPeer(this);
                if(provider != null)
                {
                    AutomationInteropProvider.RaiseAutomationEvent(
                        AutomationElementIdentifiers.AsyncContentLoadedEvent,
                        provider,
                        args);
                }
            }
        }

        internal static void RaiseFocusChangedEventHelper(IInputElement newFocus)
        {
            // Callers have only checked if automation clients are present so filter for any interest in this particular event.
            if (EventMap.HasRegisteredEvent(AutomationEvents.AutomationFocusChanged))
            {
                AutomationPeer peer = AutomationPeerFromInputElement(newFocus);

                if (peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
                }
                else //non-automated element got focus, same as focus lost
                {
                    //No focused peer. Just don't report anything.
                }
            }
        }

        //  helper method. Makes attempt to find an automation peer corresponding to the given IInputElement...
        internal static AutomationPeer AutomationPeerFromInputElement(IInputElement focusedElement)
        {
            AutomationPeer peer = null;

            UIElement uie = focusedElement as UIElement;
            if (uie != null)
            {
                peer = UIElementAutomationPeer.CreatePeerForElement(uie);
            }
            else
            {
                ContentElement ce = focusedElement as ContentElement;
                if (ce != null)
                {
                    peer = ContentElementAutomationPeer.CreatePeerForElement(ce);
                }
                else
                {
                    UIElement3D uie3D = focusedElement as UIElement3D;
                    if (uie3D != null)
                    {
                        peer = UIElement3DAutomationPeer.CreatePeerForElement(uie3D);
                    }
                }
            }

            if (peer != null)
            {
                //  ValidateConnected ensures that EventsSource is initialized
                peer.ValidateConnected(peer);

                //  always use event source when available
                if (peer.EventsSource != null)
                {
                    peer = peer.EventsSource;
                }
            }

            return peer;
        }

        // We can only return peers to UIA that are properly connected to the UIA tree already
        // This means they should have _hwnd and _parent already set and _parent should point to the
        // peer which would have this peer returned from its GetChildrenCore. This method checks if the
        // peer is already connected, and if not then it walks the tree of peers from the top down, calling
        // GetChildren and trying to find itself in someone's children. Once this succeeds, the peer is connected
        // (because GetChildren will connect it). In this case this method will return "this".
        // However if the search does not find the peer, that means the peer
        // would never be exposed by specific context even though it is createable on the element (the decision to expose
        // children is on parent peers and parent peer may decide not to expose subpart of itself). In this case,
        // this method returns null.
        // ConnectedPeer parameter is some peer which is known to be connected (typically root, but if not, this method will
        // walk up from the given connectedPeer up to find a root)
        internal AutomationPeer ValidateConnected(AutomationPeer connectedPeer)
        {
            if(connectedPeer == null)
                throw new ArgumentNullException("connectedPeer");

            if(_parent != null && _hwnd != IntPtr.Zero) return this;

            if((connectedPeer._hwnd) != IntPtr.Zero)
            {
                while(connectedPeer._parent != null) connectedPeer = connectedPeer._parent;

                //now connectedPeer is the root
                if ((connectedPeer == this) || isDescendantOf(connectedPeer))
                    return this;
            }

            //last effort - find across all roots
            //only start fault in the tree from the root if we are not in the recursive sync update
            //Otherwise it will go through the peers that are currently on the stack
            ContextLayoutManager lm = ContextLayoutManager.From(this.Dispatcher);
            if(lm != null && lm.AutomationSyncUpdateCounter == 0)
            {
                AutomationPeer[] roots = lm.GetAutomationRoots();
                for(int i = 0; i < roots.Length; i++)
                {
                    AutomationPeer root = roots[i];

                    if (root != null)
                    {
                        if((root == this) || isDescendantOf(root))
                        return this;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This is responsible for adding parent info like the parent window handle
        /// and the parent itself to it's child. This is by definition is a securitycritical operation
        /// for two reasons
        /// 1. it's doing an action which is securitycritical
        /// 2. it can not be treated as safe as it doesn't know whether
        ///    the peer is actually this objects's parent or not and must be used by methods which has
        /// </summary>
        /// <param name="peer"></param>
        internal bool TrySetParentInfo(AutomationPeer peer)
        {
            Invariant.Assert((peer != null));

            if(peer._hwnd == IntPtr.Zero)
            {
                // parent is not yet part of Automation Tree itself
                return false;
            }

            _hwnd = peer._hwnd;
            _parent = peer;

            return true;
        }

        // To determine if the peer corresponds to DataItem
        virtual internal bool IsDataItemAutomationPeer()
        {
            return false;
        }

        // UpdatePeer is called asynchronously.  Between the time the call is
        // posted (InvalidatePeer) and the time the call is executed (UpdatePeer),
        // changes to the visual tree and/or automation tree may have eliminated
        // the need for UpdatePeer, or even made it a mistake.
        // Subclasses can override this method if they can detect this situation.
        virtual internal bool IgnoreUpdatePeer()
        {
            return false;
        }

        // This is mainly for enabling ITemsControl to keep the Cache of the Item's Proxy Weak Ref to
        // re-use the item peers being passed to clinet and still exist in memory
        virtual internal void AddToParentProxyWeakRefCache()
        {
            //do nothing
        }
        private bool isDescendantOf(AutomationPeer parent)
        {
            if(parent == null)
                throw new ArgumentNullException("parent");

            List<AutomationPeer> children  = parent.GetChildren();

            if(children == null)
                return false;

            int cnt = children.Count;
            for(int i = 0; i < cnt; ++i)
            {
                AutomationPeer child = children[i];

                //depth first
                if(child == this || this.isDescendantOf(child))
                    return true;
            }

            return false;
        }

        ///<summary>
        /// Outside of hosting scenarios AutomationPeers shoudl not override this method.
        /// It is needed for peers that implement their own host HWNDs
        /// for these HWNDs to appear in a proper place in the UIA tree.
        /// Without this interface being omplemented, the HWND is parented by UIA as a child
        /// of the HwndSource that hosts whole Avalon app. Instead, it is usually desirable
        /// to override this defautl behavior and tell UIA to parent hosted HWND as a child
        /// somewhere in Avlaon tree where it is actually hosted.
        /// <para/>
        /// Automation infrastructure provides necessary hookup, the AutomationPeer of the element that
        /// immediately hosts the HWND should implement this interface to be properly wired in.
        /// In addition to that, it should return this peer as IRawElementProviderSimple as a response to
        /// WM_GETOBJECT coming to the hosted HWND.
        /// <para/>
        /// To obtain the IRawElementProviderSimple interface, the peer should use
        /// System.Windows.Automation.AutomationInteropProvider.HostProviderFromHandle(hwnd).
        ///</summary>
        virtual protected HostedWindowWrapper GetHostRawElementProviderCore()
        {
            HostedWindowWrapper host = null;

            //in normal Avalon subtrees, only root peers should return wrapped HWND
            if(GetParent() == null)
            {
                // this way of creating HostedWindowWrapper does not require FullTrust
                host = HostedWindowWrapper.CreateInternal(Hwnd);
            }

            return host;
        }

        internal HostedWindowWrapper GetHostRawElementProvider()
        {
            return GetHostRawElementProviderCore();
        }

        ///<summary>
        /// Returns 'true' only if this is a peer that hosts HWND in Avalon (WindowsFormsHost or Popup for example).
        /// Such peers also have to override GetHostRawElementProviderCore method.
        ///</summary>
        virtual protected internal bool IsHwndHost { get { return false; }}

        //
        // P R O P E R T I E S
        //

        ///
        abstract protected Rect GetBoundingRectangleCore();

        ///
        abstract protected bool IsOffscreenCore();

        ///
        abstract protected AutomationOrientation GetOrientationCore();

        ///
        abstract protected string GetItemTypeCore();

        ///
        abstract protected string GetClassNameCore();

        ///
        abstract protected string GetItemStatusCore();

        ///
        abstract protected bool IsRequiredForFormCore();

        ///
        abstract protected bool IsKeyboardFocusableCore();

        ///
        abstract protected bool HasKeyboardFocusCore();

        ///
        abstract protected bool IsEnabledCore();

        ///
        abstract protected bool IsPasswordCore();

        ///
        abstract protected string GetAutomationIdCore();

        ///
        abstract protected string GetNameCore();

        ///
        abstract protected AutomationControlType GetAutomationControlTypeCore();

        ///
        virtual protected string GetLocalizedControlTypeCore()
        {
            ControlType controlType = GetControlType();
            return controlType.LocalizedControlType;
        }

        ///
        abstract protected bool IsContentElementCore();

        ///
        abstract protected bool IsControlElementCore();

        ///
        abstract protected AutomationPeer GetLabeledByCore();

        ///
        abstract protected string GetHelpTextCore();

        ///
        abstract protected string GetAcceleratorKeyCore();

        ///
        abstract protected string GetAccessKeyCore();

        ///
        abstract protected Point GetClickablePointCore();

        ///
        abstract protected void SetFocusCore();

        ///
        virtual protected AutomationLiveSetting GetLiveSettingCore()
        {
            return AutomationLiveSetting.Off;
        }

        /// <summary>
        /// Override this method to provide UIAutomation with a list of elements affected or controlled by this AutomationPeer.
        /// </summary>
        /// <returns>
        /// A list of AutomationPeers for the controlled elements.
        /// </returns>
        virtual protected List<AutomationPeer> GetControlledPeersCore()
        {
            return null;
        }

        /// <summary>
        /// Override this method to provide UIAutomation with an integer value describing the size of a group or set this element belongs to.
        /// </summary>
        virtual protected int GetSizeOfSetCore()
        {
            return AutomationProperties.AutomationSizeOfSetDefault;
        }

        /// <summary>
        /// Override this method to provide UIAutomation with a 1-based integer value describing the position this element occupies in a group or set.
        /// </summary>
        virtual protected int GetPositionInSetCore()
        {
            return AutomationProperties.AutomationPositionInSetDefault;
        }


        //
        // INTERNAL STUFF - NOT OVERRIDABLE
        //
        virtual internal Rect GetVisibleBoundingRectCore()
        {
            // Too late to add abstract methods, since this class has already shipped(using default definition)!
            return GetBoundingRectangle();
        }

        ///
        public Rect GetBoundingRectangle()
        {
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                _boundingRectangle = GetBoundingRectangleCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return _boundingRectangle;
        }

        ///
        public bool IsOffscreen()
        {
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                _isOffscreen = IsOffscreenCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return _isOffscreen;
        }

        ///
        public AutomationOrientation GetOrientation()
        {
            AutomationOrientation result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetOrientationCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetItemType()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetItemTypeCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetClassName()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetClassNameCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetItemStatus()
        {
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                _itemStatus = GetItemStatusCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return _itemStatus;
        }

        ///
        public bool IsRequiredForForm()
        {
            bool result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = IsRequiredForFormCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public bool IsKeyboardFocusable()
        {
            bool result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = IsKeyboardFocusableCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public bool HasKeyboardFocus()
        {
            bool result;
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = HasKeyboardFocusCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public bool IsEnabled()
        {
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                _isEnabled = IsEnabledCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return _isEnabled;
        }

        ///
        public bool IsPassword()
        {
            bool result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = IsPasswordCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetAutomationId()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetAutomationIdCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetName()
        {
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                _name = GetNameCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return _name;
        }

        ///
        public AutomationControlType GetAutomationControlType()
        {
            AutomationControlType result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetAutomationControlTypeCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetLocalizedControlType()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetLocalizedControlTypeCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public bool IsContentElement()
        {
            bool result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;

                // As per the UIA guidelines an entity has to be a control to be able to hold content.
                // See http://msdn.microsoft.com/en-us/library/system.windows.automation.automationelement.iscontentelementproperty.aspx

                result = IsControlElementPrivate() && IsContentElementCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public bool IsControlElement()
        {
            bool result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = IsControlElementPrivate();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        private bool IsControlElementPrivate()
        {
            return IsControlElementCore();
        }

        ///
        public AutomationPeer GetLabeledBy()
        {
            AutomationPeer result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetLabeledByCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetHelpText()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetHelpTextCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetAcceleratorKey()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetAcceleratorKeyCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public string GetAccessKey()
        {
            string result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetAccessKeyCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public Point GetClickablePoint()
        {
            Point result;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetClickablePointCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public void SetFocus()
        {
            if (_publicSetFocusInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicSetFocusInProgress = true;
                SetFocusCore();
            }
            finally
            {
                _publicSetFocusInProgress = false;
            }
        }

        ///
        public AutomationLiveSetting GetLiveSetting()
        {
            AutomationLiveSetting result = AutomationLiveSetting.Off;
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetLiveSettingCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        /// <summary>
        /// This method provides UIAuatomation with a list of elements affected or controlled by this AutomationPeer.
        /// </summary>
        /// <returns>
        /// A list of AutomationPeers for the controlled elements.
        /// </returns>
        public List<AutomationPeer> GetControlledPeers()
        {
            List<AutomationPeer> result = null;
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetControlledPeersCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        /// <summary>
        ///     Calls <see cref="GetControlledPeers"/> to get a list of AutomationPeers then transforms it into an array
        ///     of <see cref="IRawElementProviderSimple"/> to provide the ControlleFor property to UIA.
        /// </summary>
        /// <returns>
        ///     An array of <see cref="IRawElementProviderSimple"/> representing the AutomationPeers provided by <see cref="GetControlledPeers"/>
        /// </returns>
        private IRawElementProviderSimple[] GetControllerForProviderArray()
        {
            List<AutomationPeer> controlledPeers = GetControlledPeers();
            IRawElementProviderSimple[] result = null;

            if (controlledPeers != null)
            {
                result = new IRawElementProviderSimple[controlledPeers.Count];

                for (int i = 0; i < controlledPeers.Count; i++)
                {
                    result[i] = ProviderFromPeer(controlledPeers[i]);
                }
            }

            return result;
        }
        /// <summary>
        /// Attempt to get the value for the SizeOfSet property.
        /// </summary>
        /// <remarks>
        /// This public call cannot be attempted if another public call is in progress.
        /// </remarks>
        /// <returns>
        /// The value for the SizeOfSet property.
        /// </returns>
        public int GetSizeOfSet()
        {
            int result = AutomationProperties.AutomationSizeOfSetDefault;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetSizeOfSetCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        /// <summary>
        /// Attempt to get the value for the PositionInSet property.
        /// </summary>
        /// <remarks>
        /// This public call cannot be attempted if another public call is in progress.
        /// </remarks>
        /// <returns>
        /// The value for the PositionInSet property.
        /// </returns>
        public int GetPositionInSet()
        {
            int result = AutomationProperties.AutomationPositionInSetDefault;

            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                result = GetPositionInSetCore();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return result;
        }

        ///
        public AutomationPeer GetParent()
        {
            return _parent;
        }

        ///
        public List<AutomationPeer> GetChildren()
        {
            if (_publicCallInProgress)
                throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));

            try
            {
                _publicCallInProgress = true;
                EnsureChildren();
            }
            finally
            {
                _publicCallInProgress = false;
            }
            return _children;
        }

        ///
        public void ResetChildrenCache()
        {
            using (UpdateChildren())
            {
            }
        }

        ///
        internal int[] GetRuntimeId()
        {
            return new int [] { 7, SafeNativeMethods.GetCurrentProcessId(), this.GetHashCode() };
        }

        ///
        internal string GetFrameworkId() { return ("WPF"); }

        //
        internal AutomationPeer GetFirstChild()
        {
            AutomationPeer peer = null;

            EnsureChildren();

            if (_children != null && _children.Count > 0)
            {
                peer = _children[0];
                peer.ChooseIterationParent(this);
            }

            return peer;
        }

        //
        private void EnsureChildren()
        {
            //  if !_childrenValid or _ancestorsInvalid,  indicates that the automation tree under this peer is not up to date, so requires
            //  rebuilding before navigating. This usually is the case when the peer is re-parented because of UI changes but
            // UpdateSubtree is not called on it yet.
            if (!_childrenValid || _ancestorsInvalid)
            {
                _children = GetChildrenCore();
                if (_children != null)
                {
                    int count = _children.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        _children[i]._parent = this;
                        _children[i]._index = i;
                        _children[i]._hwnd = _hwnd;
                    }
                }
                _childrenValid = true;
            }
        }

        // Use to update the Children irrespective of the _childrenValid flag.
        internal void ForceEnsureChildren()
        {
            _childrenValid = false;
            EnsureChildren();
        }

        //
        internal AutomationPeer GetLastChild()
        {
            AutomationPeer peer = null;

            EnsureChildren();

            if (_children != null && _children.Count > 0)
            {
                peer = _children[_children.Count - 1];
                peer.ChooseIterationParent(this);
            }

            return peer;
        }

        //
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal virtual InteropAutomationProvider GetInteropChild()
        {
            return null;
        }

        //
        internal AutomationPeer GetNextSibling()
        {
            AutomationPeer sibling = null;
            AutomationPeer parent = IterationParent;

            if (parent != null)
            {
                parent.EnsureChildren();

                if (    parent._children != null
                    &&  _index >= 0
                    &&  _index + 1 < parent._children.Count
                    &&  parent._children[_index] == this    )
                {
                    sibling = parent._children[_index + 1];
                    sibling.IterationParent = parent;
                }
            }

            return sibling;
        }

        //
        internal AutomationPeer GetPreviousSibling()
        {
            AutomationPeer sibling = null;
            AutomationPeer parent = IterationParent;

            if (parent != null)
            {
                parent.EnsureChildren();

                if (    parent._children != null
                    &&  _index - 1 >= 0
                    &&  _index < parent._children.Count
                    &&  parent._children[_index] == this    )
                {
                    sibling = parent._children[_index - 1];
                    sibling.IterationParent = parent;
                }
            }

            return sibling;
        }

        // For ItemsControls, there are typically two different peers for each
        // (visible) item:  an "item" peer and a "wrapper" peer.  The former
        // corresponds to the item itself, the latter to the container.  (These
        // are different peers, even when the item is its own container.)  The item
        // peer appears as a child of the ItemsControl's peer, while the wrapper
        // peer is what you get by asking the container for its peer directly.
        // For example, a ListBoxAutomationPeer holds children of type
        // ListBoxItemAutomationPeer, while the peer for a ListBoxItem has type
        // ListBoxItemWrapperAutomationPeer.
        //
        // The item and wrapper peers for a particular item can (and do) share
        // children, i.e. the _children lists can contain the same elements.  The
        // _parent pointer in a (shared) child peer could point to either the
        // item peer or the wrapper peer, depending on which parent peer rebuilt
        // its _children collection (EnsureChildren) most recently.  This is
        // confusing, but usually the two lists are identical so the navigation
        // methods (GetFirstChild, GetNextSibling, etc.) produce the correct
        // result regardless of which parent is used.
        //
        // It matters for TabControl, however.  The item peer (TabItemAutomationPeer)
        // and wrapper peer (TabItemWrapperAutomationPeer) contain the same
        // children, except when the TabItem is selected (so that its content
        // is shown in the TabControls large display area).  In that case the item
        // peer has extra children, corresponding to the displayed content.
        //
        // When an app does something that causes the wrapper peer to call
        // EnsureChildren (e.g. changes TabControl.IsEnabled), the shared
        // children all point back to the wrapper peer, while the extra children
        // still point back to the item peer.  An automation client that searches
        // through, or iterates over, the children of the item peer will not see
        // the extra children.  It (effectively) calls itemPeer.GetFirstChild,
        // followed by repeated calls to childPeer.GetNextSibling;  those calls
        // are evaluated using the _parent's child collection, but _parent points
        // to the wrapper peer (which doesn't have the extra children).
        //
        // To patch this problem, the navigation methods should use the parent that
        // started the iteration, i.e. the one where GetFirstChild was called.  But
        // we can't change _parent (the "custody parent") - we tried that, and it
        // caused compatibility problems (crashes). Instead we add a new pointer, referring to the
        // "iteration parent". GetFirstChild sets it, and GetNextSibling propagates
        // it through the family as the iteration proceeds.  We only need to do
        // this if the iteration parent differs from the custody parent and has
        // a different set of children (a rare situation), so to make it pay-for-play
        // we implement the new pointer as an indirection through an existing field.
        // We chose _eventsSource - it's already encapsulated in a property, and is used
        // in situations that aren't perf-critical (creation of new peers, et al.).

        // Called by GetFirstChild, GetLastChild.
        // Choose which of the custody parent (_parent) and the caller
        // should serve as the iteration parent for the iteration that's starting.
        private void ChooseIterationParent(AutomationPeer caller)
        {
            Debug.Assert(caller._children != null, "iteration over a null family");
            AutomationPeer iterationParent;

            // easy (and frequent) case:  both candidates are the same
            // easy case: custody parent is null (preserve original behavior)
            if (_parent == caller || _parent == null)
            {
                iterationParent = _parent;
            }
            else
            {
                // Usually we choose the custody parent.   The only case that needs
                // something different is the TabItem case described earlier, which we
                // recognize by the fact that the families have different size.
                //
                // There's also a funky case when the custody parent's family is null,
                // even after EnsureChildren.  Like the "custodyParent == null" case above,
                // I'm not sure this can ever happen, but if it does we choose the
                // custody parent;  this preserves the original logic that terminates
                // the iteration at the next call to GetNextSibling.
                _parent.EnsureChildren();
                iterationParent = (_parent._children == null || _parent._children.Count == caller._children.Count)
                    ? _parent : caller;
            }

            IterationParent = iterationParent;
        }

        private AutomationPeer IterationParent
        {
            get
            {
                return !_hasIterationParent ? _parent : ((PeerRecord)_eventsSourceOrPeerRecord).IterationParent;
            }
            set
            {
                if (value == _parent)
                {
                    if (_hasIterationParent)
                    {
                        // remove the indirection
                        _eventsSourceOrPeerRecord = EventsSource;
                        _hasIterationParent = false;
                    }
                    else
                    {
                        // this is the 90% case - nothing to do
                    }
                }
                else
                {
                    if (!_hasIterationParent)
                    {
                        // add the indirection
                        PeerRecord record = new PeerRecord { EventsSource = EventsSource, IterationParent = value };
                        _eventsSourceOrPeerRecord = record;
                        _hasIterationParent = true;
                    }
                    else
                    {
                        // revise the existing indirection
                        ((PeerRecord)_eventsSourceOrPeerRecord).IterationParent = value;
                    }
                }
            }
        }

        //
        internal ControlType GetControlType()
        {
            ControlType controlType = null;

            AutomationControlType type = GetAutomationControlTypeCore();

            switch (type)
            {
                case AutomationControlType.Button:        controlType = ControlType.Button;       break;
                case AutomationControlType.Calendar:      controlType = ControlType.Calendar;     break;
                case AutomationControlType.CheckBox:      controlType = ControlType.CheckBox;     break;
                case AutomationControlType.ComboBox:      controlType = ControlType.ComboBox;     break;
                case AutomationControlType.Edit:          controlType = ControlType.Edit;         break;
                case AutomationControlType.Hyperlink:     controlType = ControlType.Hyperlink;    break;
                case AutomationControlType.Image:         controlType = ControlType.Image;        break;
                case AutomationControlType.ListItem:      controlType = ControlType.ListItem;     break;
                case AutomationControlType.List:          controlType = ControlType.List;         break;
                case AutomationControlType.Menu:          controlType = ControlType.Menu;         break;
                case AutomationControlType.MenuBar:       controlType = ControlType.MenuBar;      break;
                case AutomationControlType.MenuItem:      controlType = ControlType.MenuItem;     break;
                case AutomationControlType.ProgressBar:   controlType = ControlType.ProgressBar;  break;
                case AutomationControlType.RadioButton:   controlType = ControlType.RadioButton;  break;
                case AutomationControlType.ScrollBar:     controlType = ControlType.ScrollBar;    break;
                case AutomationControlType.Slider:        controlType = ControlType.Slider;       break;
                case AutomationControlType.Spinner:       controlType = ControlType.Spinner;      break;
                case AutomationControlType.StatusBar:     controlType = ControlType.StatusBar;    break;
                case AutomationControlType.Tab:           controlType = ControlType.Tab;          break;
                case AutomationControlType.TabItem:       controlType = ControlType.TabItem;      break;
                case AutomationControlType.Text:          controlType = ControlType.Text;         break;
                case AutomationControlType.ToolBar:       controlType = ControlType.ToolBar;      break;
                case AutomationControlType.ToolTip:       controlType = ControlType.ToolTip;      break;
                case AutomationControlType.Tree:          controlType = ControlType.Tree;         break;
                case AutomationControlType.TreeItem:      controlType = ControlType.TreeItem;     break;
                case AutomationControlType.Custom:        controlType = ControlType.Custom;       break;
                case AutomationControlType.Group:         controlType = ControlType.Group;        break;
                case AutomationControlType.Thumb:         controlType = ControlType.Thumb;        break;
                case AutomationControlType.DataGrid:      controlType = ControlType.DataGrid;     break;
                case AutomationControlType.DataItem:      controlType = ControlType.DataItem;     break;
                case AutomationControlType.Document:      controlType = ControlType.Document;     break;
                case AutomationControlType.SplitButton:   controlType = ControlType.SplitButton;  break;
                case AutomationControlType.Window:        controlType = ControlType.Window;       break;
                case AutomationControlType.Pane:          controlType = ControlType.Pane;         break;
                case AutomationControlType.Header:        controlType = ControlType.Header;       break;
                case AutomationControlType.HeaderItem:    controlType = ControlType.HeaderItem;   break;
                case AutomationControlType.Table:         controlType = ControlType.Table;        break;
                case AutomationControlType.TitleBar:      controlType = ControlType.TitleBar;     break;
                case AutomationControlType.Separator:     controlType = ControlType.Separator;    break;
                default: break;
            }

            return controlType;
        }

        public AutomationPeer GetPeerFromPoint(Point point)
        {
            return GetPeerFromPointCore(point);
        }

        protected virtual AutomationPeer GetPeerFromPointCore(Point point)
        {
            AutomationPeer found = null;

            if(!IsOffscreen())
            {
                List<AutomationPeer> children = GetChildren();
                if(children != null)
                {
                    int count = children.Count;
                    for(int i = count-1; (i >= 0) && (found == null); --i)
                    {
                        found = children[i].GetPeerFromPoint(point);
                    }
                }

                if(found == null)
                {
                    Rect bounds = GetVisibleBoundingRect();
                    if (bounds.Contains(point))
                        found = this;
                }
            }

            return found;
        }

        /// <summary>
        /// inherited by item peers to return just visible part of bounding rectangle.
        /// </summary>
        /// <returns></returns>
        internal Rect GetVisibleBoundingRect()
        {
            return GetVisibleBoundingRectCore();
        }

        ///<Summary>
        /// Creates an element provider (proxy) from a peer. Some patterns require returning objects of type
        /// IRawElementProviderSimple - this is an Automation-specific wrapper interface that corresponds to a peer.
        /// To wrap an AutomationPeer into the wrapper that exposes this interface, use this method.
        ///</Summary>
        protected internal IRawElementProviderSimple ProviderFromPeer(AutomationPeer peer)
        {
            AutomationPeer referencePeer = this;

            //replace itself with EventsSource if we are aggregated and hidden from the UIA
            AutomationPeer eventsSource;
            if((peer == this) && ((eventsSource = EventsSource) != null))
            {
                referencePeer = peer = eventsSource;
            }

            return ElementProxy.StaticWrap(peer, referencePeer);
        }

        private IRawElementProviderSimple ProviderFromPeerNoDelegation(AutomationPeer peer)
        {
            AutomationPeer referencePeer = this;
            return ElementProxy.StaticWrap(peer, referencePeer);
        }

        ///<Summary>
        /// When one AutomationPeer is using the pattern of another AutomationPeer instead of exposing
        /// it in the children collection (example - ListBox exposes IScrollProvider from internal ScrollViewer
        /// but does not expose the ScrollViewerAutomationPeer as its child) - then before returning the pattern
        /// interface from GetPattern, the "main" AutomationPeer should call this method to set up itself as
        /// "source" for the events fired by the pattern on the subordinate AutomationPeer.
        /// Otherwise, the hidden subordinate AutomationPeer will fire pattern's events from its own identity which
        /// will confuse UIA since its identity is not exposed to UIA.
        ///</Summary>
        public AutomationPeer EventsSource
        {
            get
            {
                return !_hasIterationParent
                    ? (AutomationPeer)_eventsSourceOrPeerRecord
                    : ((PeerRecord)_eventsSourceOrPeerRecord).EventsSource;
            }
            set
            {
                if (!_hasIterationParent)
                {
                    _eventsSourceOrPeerRecord = value;
                }
                else
                {
                    ((PeerRecord)_eventsSourceOrPeerRecord).EventsSource = value;
                }
            }
        }


        ///<Summary>
        /// Returns AutomationPeer corresponding to the given provider.
        ///</Summary>
        protected AutomationPeer PeerFromProvider(IRawElementProviderSimple provider)
        {
            ElementProxy proxy = provider as ElementProxy;
            if (proxy != null)
            {
                return (proxy.Peer);
            }

            return null;
        }

        //called on a root peer of a tree when it's time to fire automation events
        //walks down the tree, updates caches and fires automation events
        internal void FireAutomationEvents()
        {
            UpdateSubtree();
        }

        // internal handling of structure chanegd events
        private void RaisePropertyChangedInternal(IRawElementProviderSimple provider,
                                                             AutomationProperty propertyId,
                                                             object oldValue,
                                                             object newValue)
        {
            // Callers have only checked if automation clients are present so filter for any interest in this particular event.
            if (  provider != null
               && EventMap.HasRegisteredEvent(AutomationEvents.PropertyChanged) )
            {
                AutomationPropertyChangedEventArgs e = new AutomationPropertyChangedEventArgs(propertyId, oldValue, newValue);
                AutomationInteropProvider.RaiseAutomationPropertyChangedEvent(provider, e);
            }
#if ENABLE_AUTOMATIONPEER_LOGGING
            LogPropertyChanged(this, propertyId);
#endif
        }

        // InvalidateLimit – lower bound for  raising ChildrenInvalidated StructureChange event
        internal void UpdateChildrenInternal(int invalidateLimit)
        {
            List<AutomationPeer> oldChildren = _children;
            List<AutomationPeer> addedChildren = null;
            HashSet<AutomationPeer> hs = null;

            _childrenValid = false;
            EnsureChildren();

            // Callers have only checked if automation clients are present so filter for any interest in this particular event.
            if (!EventMap.HasRegisteredEvent(AutomationEvents.StructureChanged))
                return;

            //store old children in a hashset
            if(oldChildren != null)
            {
                hs = new HashSet<AutomationPeer>();
                for(int count = oldChildren.Count, i = 0; i < count; i++)
                {
                    hs.Add(oldChildren[i]);
                }
            }

            //walk over new children, remove the ones that were in the old collection from hash table
            //and add new ones into addedChildren list
            int addedCount = 0;

            if(_children != null)
            {
                for(int count = _children.Count, i = 0; i < count; i++)
                {
                    AutomationPeer child = _children[i];
                    if(hs != null && hs.Contains(child))
                    {
                        hs.Remove(child); //same child, nothing to notify
                    }
                    else
                    {
                        if(addedChildren == null) addedChildren = new List<AutomationPeer>();

                        //stop accumulatin new children here because the notification
                        //is going to become "bulk anyways and exact set of chidlren is not
                        //needed, only count.
                        ++addedCount;
                        if(addedCount <= invalidateLimit)
                            addedChildren.Add(child);
                    }
                }
            }

            //now the hs only has "removed" children. If the count does not yet
            //calls for "bulk" notification, use per-child notification, otherwise use "bulk"
            int removedCount = (hs == null ? 0 : hs.Count);

            if(removedCount + addedCount > invalidateLimit) //bilk invalidation
            {
                StructureChangeType flags;

                // Set bulk event type depending on if these were adds, removes or a mix
                if (addedCount == 0)
                    flags = StructureChangeType.ChildrenBulkRemoved;
                else if ( removedCount == 0 )
                    flags = StructureChangeType.ChildrenBulkAdded;
                else
                    flags = StructureChangeType.ChildrenInvalidated;

                IRawElementProviderSimple provider = ProviderFromPeerNoDelegation(this);
                if(provider != null)
                {
                    int [] rid = this.GetRuntimeId(); //use runtimeID of parent for bulk notifications

                    AutomationInteropProvider.RaiseStructureChangedEvent(
                                    provider,
                                    new StructureChangedEventArgs(flags, rid));
                }
            }
            else
            {
                if (removedCount > 0)
                {
                    //for children removed, provider is the parent
                    IRawElementProviderSimple provider = ProviderFromPeerNoDelegation(this);
                    if (provider != null)
                    {
                        //hs contains removed children by now
                        foreach (AutomationPeer removedChild in hs)
                        {
                            int[] rid = removedChild.GetRuntimeId();

                            AutomationInteropProvider.RaiseStructureChangedEvent(
                                            provider,
                                            new StructureChangedEventArgs(StructureChangeType.ChildRemoved, rid));
                        }
                    }
                }
                if (addedCount > 0)
                {
                    //hs contains removed children by now
                    foreach (AutomationPeer addedChild in addedChildren)
                    {
                        //for children added, provider is the child itself
                        IRawElementProviderSimple provider = ProviderFromPeerNoDelegation(addedChild);
                        if (provider != null)
                        {
                            int[] rid = addedChild.GetRuntimeId();

                            AutomationInteropProvider.RaiseStructureChangedEvent(
                                            provider,
                                            new StructureChangedEventArgs(StructureChangeType.ChildAdded, rid));
                        }
                    }
                }
            }
        }

        // internal handling of structure changed events
        virtual internal IDisposable UpdateChildren()
        {
            UpdateChildrenInternal(AutomationInteropProvider.InvalidateLimit);
            return null;
        }

        ///<summary>
        /// This method causes recursive walk from this peer down and updates all the cahced information -
        /// set of children and some most frequently asked properties. It also fires notifications to UIA if those change.
        /// Notrmally, the system would call this method automationcally upon layout update, walking UIA tree and calling this method.
        /// However, in some rare cases
        /// developers should use this method from implementation of custom peers - example is when peer is actually
        /// delegating the functionality to some other peer which is not even exposed in UIA tree - like ListBoxItemAP
        /// delegates the work to ListBoxItemWrapperAP. Since "subordinate" peer is not being updated by the system since it is not
        /// in the UIA tree.
        ///</summary>
        ///<remarks>
        /// Is it possible that they turn around and reenter asking for new value while in event handler?
        //  If yes, we'll serve old value
        ///</remarks>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal void UpdateSubtree()
        {
            ContextLayoutManager lm = ContextLayoutManager.From(this.Dispatcher);
            if(lm != null)
            {
                lm.AutomationSyncUpdateCounter = lm.AutomationSyncUpdateCounter + 1;

                try
                {
                    IRawElementProviderSimple provider = null;




                    bool notifyPropertyChanged = EventMap.HasRegisteredEvent(AutomationEvents.PropertyChanged);
                    bool notifyStructureChanged = EventMap.HasRegisteredEvent(AutomationEvents.StructureChanged);

                    //  did anybody ask for property changed norification?
                    if (notifyPropertyChanged)
                    {
                        string itemStatus = GetItemStatusCore();
                        if (itemStatus != _itemStatus)
                        {
                            if(provider == null)
                                provider = ProviderFromPeerNoDelegation(this);
                            RaisePropertyChangedInternal(provider,
                                                         AutomationElementIdentifiers.ItemStatusProperty,
                                                         _itemStatus,
                                                         itemStatus);
                            _itemStatus = itemStatus;
                        }

                        string name = GetNameCore();
                        if (name != _name)
                        {
                            if(provider == null)
                                provider = ProviderFromPeerNoDelegation(this);
                            RaisePropertyChangedInternal(provider,
                                                         AutomationElementIdentifiers.NameProperty,
                                                         _name,
                                                         name);
                            _name = name;
                        }

                        bool isOffscreen = IsOffscreenCore();
                        if (isOffscreen != _isOffscreen)
                        {
                            if(provider == null)
                                provider = ProviderFromPeerNoDelegation(this);
                            RaisePropertyChangedInternal(provider,
                                                         AutomationElementIdentifiers.IsOffscreenProperty,
                                                         _isOffscreen,
                                                         isOffscreen);
                            _isOffscreen = isOffscreen;
                        }

                        bool isEnabled = IsEnabledCore();
                        if (isEnabled != _isEnabled)
                        {
                            if(provider == null)
                                provider = ProviderFromPeerNoDelegation(this);
                            RaisePropertyChangedInternal(provider,
                                                         AutomationElementIdentifiers.IsEnabledProperty,
                                                         _isEnabled,
                                                         isEnabled);
                            _isEnabled = isEnabled;
                        }
}

                    //  did anybody ask for structure changed norification?
                    //  if somebody asked for property changed then structure must be updated
                    if (this._childrenValid? (this.AncestorsInvalid || (ControlType.Custom == this.GetControlType())) : (notifyStructureChanged || notifyPropertyChanged))
                    {
                        using (UpdateChildren())
                        {
                            AncestorsInvalid = false;

                            for(AutomationPeer peer = GetFirstChild(); peer != null; peer = peer.GetNextSibling())
                            {
                                peer.UpdateSubtree();
                            }
                        }
                    }
                    AncestorsInvalid = false;
                    _invalidated = false;
                }
                finally
                {
                    lm.AutomationSyncUpdateCounter = lm.AutomationSyncUpdateCounter - 1;
                }
            }
        }

        /// <summary>
        /// propagate the new value for AncestorsInvalid through the parent chain,
        /// use EventSource (wrapper) peers whenever available as it’s the one connected to the tree.
        /// </summary>
        internal void InvalidateAncestorsRecursive()
        {
            if (!AncestorsInvalid)
            {
                AncestorsInvalid = true;
                if (EventsSource != null)
                {
                    EventsSource.InvalidateAncestorsRecursive();
                }

                if (_parent != null)
                    _parent.InvalidateAncestorsRecursive();
            }
        }

        private static object UpdatePeer(object arg)
        {
            AutomationPeer peer = (AutomationPeer)arg;
            if (!peer.IgnoreUpdatePeer())
            {
                peer.UpdateSubtree();
            }
            return null;
        }

        internal void AddToAutomationEventList()
        {
            if(!_addedToEventList)
            {
                ContextLayoutManager lm = ContextLayoutManager.From(this.Dispatcher);
                lm.AutomationEvents.Add(this); //this adds the root peer into the list of roots, for deferred event firing
                _addedToEventList = true;
            }
}

        internal IntPtr Hwnd
        {
            get { return _hwnd; }
            set { _hwnd = value; }
        }

        //
        internal object GetWrappedPattern(int patternId)
        {
            object result = null;

            PatternInfo info = (PatternInfo)s_patternInfo[patternId];

            if (info != null)
            {
                object iface = GetPattern(info.PatternInterface);
                if (iface != null)
                {
                    result = info.WrapObject(this, iface);
                }
            }

            return result;
        }

        //
        internal object GetPropertyValue(int propertyId)
        {
            object result = null;

            GetProperty getProperty = (GetProperty)s_propertyInfo[propertyId];

            if (getProperty != null)
            {
                result = getProperty(this);
            }

            return result;
        }

        //
        internal virtual bool AncestorsInvalid
        {
            get { return _ancestorsInvalid; }
            set { _ancestorsInvalid = value; }
        }

        //
        internal bool ChildrenValid
        {
            get { return _childrenValid; }
            set { _childrenValid = value; }
        }

        //
        internal bool IsInteropPeer
        {
            get { return _isInteropPeer; }
            set { _isInteropPeer = value; }
        }

        //
        internal int Index
        {
            get { return _index; }
        }

        //
        internal List<AutomationPeer> Children
        {
            get { return _children; }
        }

        // To Keep the WeakRefernce of ElementProxy wrapper for this peer so that it can be reused
        // rather than creating the new Wrapper object if there is need and it still exist.
        internal WeakReference ElementProxyWeakReference
        {
            get{ return _elementProxyWeakReference; }
            set
            {
                if(value.Target as ElementProxy != null)
                    _elementProxyWeakReference = value;
            }
        }

        // Determine whether invisible items should show up in UIAutomation Control View
        internal bool IncludeInvisibleElementsInControlView
        {
            get
            {
                //  As part of this breaking change, invisible items should no longer show in
                // UIAutomation's Control View, as usual with other accessibility breaking changes we control
                // whether the user gets the new behavior or not via the UseLegacyAccessibilityFeatures3 AppContext flag.
                return AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures;
            }
        }

#if ENABLE_AUTOMATIONPEER_LOGGING
                // For diagnosing bugs (especially perf), it can be helpful to know how
                // often certain activities occur in a given timeframe.  These logs record
                // two such activities:  raising of PropertyChanged events, and creation
                // of new AutomationPeers.  Each broken down by the type of peer.
                // To use this:
                // 1. Uncomment the definition of ENABLE_AUTOMATIONPEER_LOGGING (line 1)
                // 2. Add calls to ClearLog and SummarizeLog at the beginning and end
                //      of the timeframe of interest.  For example, at the beginning and
                //      end of ContextLayoutManager.fireAutomationEvents - to record
                //      activity during a single pass of rebuilding the automation tree.
                // 3. Set the thresholds as desired (can also be done at debug time).
                // 4. Rebuild PresentationCore (and any assemblies you changed in step 2).
                // 5. Install the instrumented assemblies and run your scenario
                // 6. Set breakpoints/tracepoints in SummarizeLog to see the results of
                //      interest.  Start by tracing the summary string.
                // 7. Add logs for other activities as desired, following the obvious pattern.

                static Dictionary<Type, int> _pcLog = new Dictionary<Type, int>();
                static int _pcThreshold = 20;

                static Dictionary<Type, int> _peerLog = new Dictionary<Type, int>();
                static int _peerThreshold = 20;

                static void LogPropertyChanged(AutomationPeer peer, AutomationProperty property)
                {
                    int oldCount, newCount;
                    Type peerType = peer.GetType();
                    if (_pcLog.TryGetValue(peerType, out oldCount))
                    {
                        newCount = oldCount + 1;
                    }
                    else
                    {
                        newCount = 1;
                    }
                    _pcLog[peerType] = newCount;
                }

                static void LogPeer(AutomationPeer peer)
                {
                    int oldCount, newCount;
                    Type peerType = peer.GetType();
                    if (_peerLog.TryGetValue(peerType, out oldCount))
                    {
                        newCount = oldCount + 1;
                    }
                    else
                    {
                        newCount = 1;
                    }
                    _peerLog[peerType] = newCount;
                }

                static internal void ClearLog()
                {
                    _pcLog.Clear();
                    _peerLog.Clear();
                }

                static internal void SummarizeLog()
                {
                    SummarizeLog(_pcLog, _pcThreshold, "events");
                    SummarizeLog(_peerLog, _peerThreshold, "peers");
                }

                static void SummarizeLog(Dictionary<Type,int> log, int threshold, string title)
                {
                    int size = log.Count;
                    if (size == 0)
                        return;

                    KeyValuePair<Type,int>[] pairs = new KeyValuePair<Type,int>[size];
                    foreach (KeyValuePair<Type,int> kvp in log)
                    {
                        pairs[--size] = kvp;
                    }

                    Array.Sort(pairs, new LogComparer());

                    int largeCount = 0;
                    int sum = 0;
                    size = pairs.Length;
                    for (int i=0; i<size; ++i)
                    {
                        KeyValuePair<Type,int> kvp = pairs[i];
                        if (kvp.Value > threshold)
                        {
                            string s1 = String.Format("{0} {1}", kvp.Value, kvp.Key.Name);
                            ++largeCount;
                        }
                        sum += kvp.Value;
                    }

                    if (largeCount > 0)
                    {
                        string s = String.Format("--- {0} {3}, {1}/{2} types ---",
                                        sum, largeCount, size, title);
                    }
                }

                class LogComparer : Comparer<KeyValuePair<Type,int>>
                {
                    public override int Compare(KeyValuePair<Type,int> x, KeyValuePair<Type,int> y)
                    {
                        return (y.Value - x.Value);
                    }
                }
#endif

        private static void Initialize()
        {
            //  initializeing patterns
            s_patternInfo = new Hashtable();
            s_patternInfo[InvokePatternIdentifiers.Pattern.Id]          = new PatternInfo(InvokePatternIdentifiers.Pattern.Id,          new WrapObject(InvokeProviderWrapper.Wrap),             PatternInterface.Invoke);
            s_patternInfo[SelectionPatternIdentifiers.Pattern.Id]       = new PatternInfo(SelectionPatternIdentifiers.Pattern.Id,       new WrapObject(SelectionProviderWrapper.Wrap),          PatternInterface.Selection);
            s_patternInfo[ValuePatternIdentifiers.Pattern.Id]           = new PatternInfo(ValuePatternIdentifiers.Pattern.Id,           new WrapObject(ValueProviderWrapper.Wrap),              PatternInterface.Value);
            s_patternInfo[RangeValuePatternIdentifiers.Pattern.Id]      = new PatternInfo(RangeValuePatternIdentifiers.Pattern.Id,      new WrapObject(RangeValueProviderWrapper.Wrap),         PatternInterface.RangeValue);
            s_patternInfo[ScrollPatternIdentifiers.Pattern.Id]          = new PatternInfo(ScrollPatternIdentifiers.Pattern.Id,          new WrapObject(ScrollProviderWrapper.Wrap),             PatternInterface.Scroll);
            s_patternInfo[ScrollItemPatternIdentifiers.Pattern.Id]      = new PatternInfo(ScrollItemPatternIdentifiers.Pattern.Id,      new WrapObject(ScrollItemProviderWrapper.Wrap),         PatternInterface.ScrollItem);
            s_patternInfo[ExpandCollapsePatternIdentifiers.Pattern.Id]  = new PatternInfo(ExpandCollapsePatternIdentifiers.Pattern.Id,  new WrapObject(ExpandCollapseProviderWrapper.Wrap),     PatternInterface.ExpandCollapse);
            s_patternInfo[GridPatternIdentifiers.Pattern.Id]            = new PatternInfo(GridPatternIdentifiers.Pattern.Id,            new WrapObject(GridProviderWrapper.Wrap),               PatternInterface.Grid);
            s_patternInfo[GridItemPatternIdentifiers.Pattern.Id]        = new PatternInfo(GridItemPatternIdentifiers.Pattern.Id,        new WrapObject(GridItemProviderWrapper.Wrap),           PatternInterface.GridItem);
            s_patternInfo[MultipleViewPatternIdentifiers.Pattern.Id]    = new PatternInfo(MultipleViewPatternIdentifiers.Pattern.Id,    new WrapObject(MultipleViewProviderWrapper.Wrap),       PatternInterface.MultipleView);
            s_patternInfo[WindowPatternIdentifiers.Pattern.Id]          = new PatternInfo(WindowPatternIdentifiers.Pattern.Id,          new WrapObject(WindowProviderWrapper.Wrap),             PatternInterface.Window);
            s_patternInfo[SelectionItemPatternIdentifiers.Pattern.Id]   = new PatternInfo(SelectionItemPatternIdentifiers.Pattern.Id,   new WrapObject(SelectionItemProviderWrapper.Wrap),      PatternInterface.SelectionItem);
            s_patternInfo[DockPatternIdentifiers.Pattern.Id]            = new PatternInfo(DockPatternIdentifiers.Pattern.Id,            new WrapObject(DockProviderWrapper.Wrap),               PatternInterface.Dock);
            s_patternInfo[TablePatternIdentifiers.Pattern.Id]           = new PatternInfo(TablePatternIdentifiers.Pattern.Id,           new WrapObject(TableProviderWrapper.Wrap),              PatternInterface.Table);
            s_patternInfo[TableItemPatternIdentifiers.Pattern.Id]       = new PatternInfo(TableItemPatternIdentifiers.Pattern.Id,       new WrapObject(TableItemProviderWrapper.Wrap),          PatternInterface.TableItem);
            s_patternInfo[TogglePatternIdentifiers.Pattern.Id]          = new PatternInfo(TogglePatternIdentifiers.Pattern.Id,          new WrapObject(ToggleProviderWrapper.Wrap),             PatternInterface.Toggle);
            s_patternInfo[TransformPatternIdentifiers.Pattern.Id]       = new PatternInfo(TransformPatternIdentifiers.Pattern.Id,       new WrapObject(TransformProviderWrapper.Wrap),          PatternInterface.Transform);
            s_patternInfo[TextPatternIdentifiers.Pattern.Id]            = new PatternInfo(TextPatternIdentifiers.Pattern.Id,            new WrapObject(TextProviderWrapper.Wrap),               PatternInterface.Text);

            // To avoid the worst situation on legacy systems which may not have new unmanaged core. with this change with old unmanaged core
            // this will these patterns will be null and won't be added and hence reponse will be as it is not present at all rather than any crash.
            if (VirtualizedItemPatternIdentifiers.Pattern != null)
                s_patternInfo[VirtualizedItemPatternIdentifiers.Pattern.Id] = new PatternInfo(VirtualizedItemPatternIdentifiers.Pattern.Id, new WrapObject(VirtualizedItemProviderWrapper.Wrap), PatternInterface.VirtualizedItem);
            if (ItemContainerPatternIdentifiers.Pattern != null)
                s_patternInfo[ItemContainerPatternIdentifiers.Pattern.Id] = new PatternInfo(ItemContainerPatternIdentifiers.Pattern.Id, new WrapObject(ItemContainerProviderWrapper.Wrap), PatternInterface.ItemContainer);
            if (SynchronizedInputPatternIdentifiers.Pattern != null)
            {
                s_patternInfo[SynchronizedInputPatternIdentifiers.Pattern.Id] = new PatternInfo(SynchronizedInputPatternIdentifiers.Pattern.Id, new WrapObject(SynchronizedInputProviderWrapper.Wrap), PatternInterface.SynchronizedInput);
            }

            //  initializeing properties
            s_propertyInfo = new Hashtable();
            s_propertyInfo[AutomationElementIdentifiers.IsControlElementProperty.Id] = new GetProperty(IsControlElement);
            s_propertyInfo[AutomationElementIdentifiers.ControlTypeProperty.Id] = new GetProperty(GetControlType);
            s_propertyInfo[AutomationElementIdentifiers.IsContentElementProperty.Id] = new GetProperty(IsContentElement);
            s_propertyInfo[AutomationElementIdentifiers.LabeledByProperty.Id] = new GetProperty(GetLabeledBy);
            s_propertyInfo[AutomationElementIdentifiers.NativeWindowHandleProperty.Id] = new GetProperty(GetNativeWindowHandle);
            s_propertyInfo[AutomationElementIdentifiers.AutomationIdProperty.Id] = new GetProperty(GetAutomationId);
            s_propertyInfo[AutomationElementIdentifiers.ItemTypeProperty.Id] = new GetProperty(GetItemType);
            s_propertyInfo[AutomationElementIdentifiers.IsPasswordProperty.Id] = new GetProperty(IsPassword);
            s_propertyInfo[AutomationElementIdentifiers.LocalizedControlTypeProperty.Id] = new GetProperty(GetLocalizedControlType);
            s_propertyInfo[AutomationElementIdentifiers.NameProperty.Id] = new GetProperty(GetName);
            s_propertyInfo[AutomationElementIdentifiers.AcceleratorKeyProperty.Id] = new GetProperty(GetAcceleratorKey);
            s_propertyInfo[AutomationElementIdentifiers.AccessKeyProperty.Id] = new GetProperty(GetAccessKey);
            s_propertyInfo[AutomationElementIdentifiers.HasKeyboardFocusProperty.Id] = new GetProperty(HasKeyboardFocus);
            s_propertyInfo[AutomationElementIdentifiers.IsKeyboardFocusableProperty.Id] = new GetProperty(IsKeyboardFocusable);
            s_propertyInfo[AutomationElementIdentifiers.IsEnabledProperty.Id] = new GetProperty(IsEnabled);
            s_propertyInfo[AutomationElementIdentifiers.BoundingRectangleProperty.Id] = new GetProperty(GetBoundingRectangle);
            s_propertyInfo[AutomationElementIdentifiers.ProcessIdProperty.Id] = new GetProperty(GetCurrentProcessId);
            s_propertyInfo[AutomationElementIdentifiers.RuntimeIdProperty.Id] = new GetProperty(GetRuntimeId);
            s_propertyInfo[AutomationElementIdentifiers.ClassNameProperty.Id] = new GetProperty(GetClassName);
            s_propertyInfo[AutomationElementIdentifiers.HelpTextProperty.Id] = new GetProperty(GetHelpText);
            s_propertyInfo[AutomationElementIdentifiers.ClickablePointProperty.Id] = new GetProperty(GetClickablePoint);
            s_propertyInfo[AutomationElementIdentifiers.CultureProperty.Id] = new GetProperty(GetCultureInfo);
            s_propertyInfo[AutomationElementIdentifiers.IsOffscreenProperty.Id] = new GetProperty(IsOffscreen);
            s_propertyInfo[AutomationElementIdentifiers.OrientationProperty.Id] = new GetProperty(GetOrientation);
            s_propertyInfo[AutomationElementIdentifiers.FrameworkIdProperty.Id] = new GetProperty(GetFrameworkId);
            s_propertyInfo[AutomationElementIdentifiers.IsRequiredForFormProperty.Id] = new GetProperty(IsRequiredForForm);
            s_propertyInfo[AutomationElementIdentifiers.ItemStatusProperty.Id] = new GetProperty(GetItemStatus);
            if (!AccessibilitySwitches.UseNetFx47CompatibleAccessibilityFeatures && AutomationElementIdentifiers.LiveSettingProperty != null)
            {
                s_propertyInfo[AutomationElementIdentifiers.LiveSettingProperty.Id] = new GetProperty(GetLiveSetting);
            }
            if (!AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures && AutomationElementIdentifiers.ControllerForProperty != null)
            {
                s_propertyInfo[AutomationElementIdentifiers.ControllerForProperty.Id] = new GetProperty(GetControllerFor);
            }
            if (!AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures && AutomationElementIdentifiers.SizeOfSetProperty != null)
            {
                s_propertyInfo[AutomationElementIdentifiers.SizeOfSetProperty.Id] = new GetProperty(GetSizeOfSet);
            }
            if (!AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures && AutomationElementIdentifiers.PositionInSetProperty != null)
            {
                s_propertyInfo[AutomationElementIdentifiers.PositionInSetProperty.Id] = new GetProperty(GetPositionInSet);
            }
        }

        private delegate object WrapObject(AutomationPeer peer, object iface);

        private class PatternInfo
        {
            internal PatternInfo(int id, WrapObject wrapObject, PatternInterface patternInterface)
            {
                Id = id;
                WrapObject = wrapObject;
                PatternInterface = patternInterface;
            }

            internal int Id;
            internal WrapObject WrapObject;
            internal PatternInterface PatternInterface;
        }

        private delegate object GetProperty(AutomationPeer peer);

        private static object IsControlElement(AutomationPeer peer)         {   return peer.IsControlElement(); }
        private static object GetControlType(AutomationPeer peer)           {   ControlType controlType = peer.GetControlType(); return controlType.Id;  }
        private static object IsContentElement(AutomationPeer peer)         {   return peer.IsContentElement(); }
        private static object GetLabeledBy(AutomationPeer peer)             {   AutomationPeer byPeer = peer.GetLabeledBy(); return ElementProxy.StaticWrap(byPeer, peer);  }
        private static object GetNativeWindowHandle(AutomationPeer peer)    {   return null /* not used? */;    }
        private static object GetAutomationId(AutomationPeer peer)          {   return peer.GetAutomationId();  }
        private static object GetItemType(AutomationPeer peer)              {   return peer.GetItemType();      }
        private static object IsPassword(AutomationPeer peer)               {   return peer.IsPassword();       }
        private static object GetLocalizedControlType(AutomationPeer peer)  {   return peer.GetLocalizedControlType();  }
        private static object GetName(AutomationPeer peer)                  {   return peer.GetName();          }
        private static object GetAcceleratorKey(AutomationPeer peer)        {   return peer.GetAcceleratorKey();    }
        private static object GetAccessKey(AutomationPeer peer)             {   return peer.GetAccessKey();     }
        private static object HasKeyboardFocus(AutomationPeer peer)         {   return peer.HasKeyboardFocus(); }
        private static object IsKeyboardFocusable(AutomationPeer peer)      {   return peer.IsKeyboardFocusable();  }
        private static object IsEnabled(AutomationPeer peer)                {   return peer.IsEnabled();        }
        private static object GetBoundingRectangle(AutomationPeer peer)     {   return peer.GetBoundingRectangle(); }
        private static object GetCurrentProcessId(AutomationPeer peer)      {   return SafeNativeMethods.GetCurrentProcessId(); }
        private static object GetRuntimeId(AutomationPeer peer)             {   return peer.GetRuntimeId();     }
        private static object GetClassName(AutomationPeer peer)             {   return peer.GetClassName();     }
        private static object GetHelpText(AutomationPeer peer)              {   return peer.GetHelpText();  }
        private static object GetClickablePoint(AutomationPeer peer)        {   Point pt = peer.GetClickablePoint(); return new double[] {pt.X, pt.Y};  }
        private static object GetCultureInfo(AutomationPeer peer)           {   return null;    }
        private static object IsOffscreen(AutomationPeer peer)              {   return peer.IsOffscreen();  }
        private static object GetOrientation(AutomationPeer peer)           {   return peer.GetOrientation();   }
        private static object GetFrameworkId(AutomationPeer peer)           {   return peer.GetFrameworkId();   }
        private static object IsRequiredForForm(AutomationPeer peer)        {   return peer.IsRequiredForForm();    }
        private static object GetItemStatus(AutomationPeer peer)            {   return peer.GetItemStatus();    }
        private static object GetLiveSetting(AutomationPeer peer)           {   return peer.GetLiveSetting(); }
        private static object GetControllerFor(AutomationPeer peer)         {   return peer.GetControllerForProviderArray(); }
        private static object GetSizeOfSet(AutomationPeer peer)             {   return peer.GetSizeOfSet(); }
        private static object GetPositionInSet(AutomationPeer peer)         {   return peer.GetPositionInSet(); }

        private static Hashtable s_patternInfo;
        private static Hashtable s_propertyInfo;

        private int _index = -1;
        private IntPtr _hwnd;
        private List<AutomationPeer> _children;
        private AutomationPeer _parent;

        private object _eventsSourceOrPeerRecord;

        private Rect _boundingRectangle;
        private string _itemStatus;
        private string _name;
        private bool _isOffscreen;
        private bool _isEnabled;
        private bool _invalidated;
        private bool _ancestorsInvalid;
        private bool _childrenValid;
        private bool _addedToEventList;
        private bool _publicCallInProgress;
        private bool _publicSetFocusInProgress;
        private bool _isInteropPeer;
        private bool _hasIterationParent;
        private WeakReference _elementProxyWeakReference = null;

        private static DispatcherOperationCallback _updatePeer = new DispatcherOperationCallback(UpdatePeer);

        private class PeerRecord
        {
            private AutomationPeer _eventsSource;
            public AutomationPeer EventsSource
            {
                 get { return _eventsSource; }
                 set { _eventsSource = value; }
            }

            private AutomationPeer _iterationParent;
            public AutomationPeer IterationParent
            {
                get { return _iterationParent; }
                set { _iterationParent = value; }
            }
        }
    }
}
