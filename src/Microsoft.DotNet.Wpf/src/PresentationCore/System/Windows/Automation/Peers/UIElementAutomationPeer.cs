﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MS.Internal.Automation;
using MS.Internal;

namespace System.Windows.Automation.Peers
{
    public class UIElementAutomationPeer: AutomationPeer
    {
        ///
        public UIElementAutomationPeer(UIElement owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            _owner = owner;
        }

        ///
        public UIElement Owner
        {
            get
            {
                return _owner;
            }
        }

        ///<summary>
        /// This static helper creates an AutomationPeer for the specified element and 
        /// caches it - that means the created peer is going to live long and shadow the
        /// element for its lifetime. The peer will be used by Automation to proxy the element, and
        /// to fire events to the Automation when something happens with the element.
        /// The created peer is returned from this method and also from subsequent calls to this method
        /// and <seealso cref="FromElement"/>. The type of the peer is determined by the 
        /// <seealso cref="UIElement.OnCreateAutomationPeer"/> virtual callback. If UIElement does not
        /// implement the callback, there will be no peer and this method will return 'null' (in other
        /// words, there is no such thing as a 'default peer').
        ///</summary>
        public static AutomationPeer CreatePeerForElement(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return element.CreateAutomationPeer();
        }

        ///
        public static AutomationPeer FromElement(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return element.GetAutomationPeer();
        }
            
         /// 
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = null;

            iterate(_owner,
                    (IteratorCallback)delegate(AutomationPeer peer)
                    {
                        if (children == null)
                            children = new List<AutomationPeer>();

                        children.Add(peer);
                        return (false);
                    });

            return children;
        }

        /// 
        internal static AutomationPeer GetRootAutomationPeer(Visual rootVisual, IntPtr hwnd)
        {
            AutomationPeer root = null;

            iterate(rootVisual,
                    (IteratorCallback)delegate(AutomationPeer peer)
                    {
                        root = peer;
                        return (true);
                    });

            root?.Hwnd = hwnd;

            return root;
        }

        private delegate bool IteratorCallback(AutomationPeer peer);

        //
        private static bool iterate(DependencyObject parent, IteratorCallback callback)
        {
            bool done = false;

            if(parent != null)
            {
                AutomationPeer peer = null;
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count && !done; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    
                    if(     child != null
                        &&  child is UIElement 
                        &&  (peer = CreatePeerForElement((UIElement)child)) != null  )
                    {
                        done = callback(peer);
                    }
                    else if ( child != null
                        &&    child is UIElement3D
                        &&    (peer = UIElement3DAutomationPeer.CreatePeerForElement(((UIElement3D)child))) != null )
                    {
                        done = callback(peer);
                    }
                    else
                    {
                        done = iterate(child, callback);
                    }
                }
            }
            
            return done;
        }

        /// 
        public override object GetPattern(PatternInterface patternInterface)
        {
            //Support synchronized input
            if (patternInterface == PatternInterface.SynchronizedInput)
            {
                // Adaptor object is used here to avoid loading UIA assemblies in non-UIA scenarios.
                if (_synchronizedInputPattern == null)
                    _synchronizedInputPattern = new SynchronizedInputAdaptor(_owner);  
                return _synchronizedInputPattern;     
            }
            return null;
        }


        //
        // P R O P E R T I E S 
        //

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        ///
        protected override string GetAutomationIdCore() 
        { 
            return (AutomationProperties.GetAutomationId(_owner));
        }

        ///
        protected override string GetNameCore()
        {
            return (AutomationProperties.GetName(_owner));
        }

        ///
        protected override string GetHelpTextCore()
        {
            return (AutomationProperties.GetHelpText(_owner));
        }

        ///
        protected override Rect GetBoundingRectangleCore()
        {
            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(_owner);

            // If there's no source, the element is not visible, return empty rect
            if(presentationSource == null)
                return Rect.Empty;

            HwndSource hwndSource = presentationSource as HwndSource;

            // If the source isn't an HwnSource, there's not much we can do, return empty rect
            if(hwndSource == null)
                return Rect.Empty;

            Rect rectElement    = new Rect(new Point(0, 0), _owner.RenderSize);
            Rect rectRoot       = PointUtil.ElementToRoot(rectElement, _owner, presentationSource);
            Rect rectClient     = PointUtil.RootToClient(rectRoot, presentationSource);
            Rect rectScreen     = PointUtil.ClientToScreen(rectClient, hwndSource);
            
            return rectScreen;
        }

        ///
        internal override Rect GetVisibleBoundingRectCore()
        {
            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(_owner);

            // If there's no source, the element is not visible, return empty rect
            if (presentationSource == null)
                return Rect.Empty;

            HwndSource hwndSource = presentationSource as HwndSource;

            // If the source isn't an HwnSource, there's not much we can do, return empty rect
            if (hwndSource == null)
                return Rect.Empty;

            Rect rectElement = CalculateVisibleBoundingRect(_owner);
            Rect rectRoot = PointUtil.ElementToRoot(rectElement, _owner, presentationSource);
            Rect rectClient = PointUtil.RootToClient(rectRoot, presentationSource);
            Rect rectScreen = PointUtil.ClientToScreen(rectClient, hwndSource);

            return rectScreen;
        }

        ///
        protected override bool IsOffscreenCore()
        {
            IsOffscreenBehavior behavior = AutomationProperties.GetIsOffscreenBehavior(_owner);

            switch (behavior)
            {
                case IsOffscreenBehavior.Onscreen :
                    return false;

                case IsOffscreenBehavior.Offscreen :
                    return true;

                case IsOffscreenBehavior.FromClip:
                    {
                        bool isOffscreen = !_owner.IsVisible;
                        
                        if (!isOffscreen)
                        {
                            Rect boundingRect = CalculateVisibleBoundingRect(_owner);
                            
                            isOffscreen = (DoubleUtil.AreClose(boundingRect, Rect.Empty) || 
                                           DoubleUtil.AreClose(boundingRect.Height, 0) || 
                                           DoubleUtil.AreClose(boundingRect.Width, 0));
                        }

                        return isOffscreen;
                    }
                
                default :
                    return !_owner.IsVisible;
            }
        }


        ///<summary>
        /// This eliminates the part of bounding rectangle if it is at all being overlapped/clipped by any of the visual ancestor up in the parent chain
        ///</summary>
        internal static Rect CalculateVisibleBoundingRect(UIElement owner)
        {
            Rect boundingRect = new Rect(owner.RenderSize);
            
            // Compute visible portion of the rectangle.

            DependencyObject parent = VisualTreeHelper.GetParent(owner);
            
            while (parent != null && 
                   !DoubleUtil.AreClose(boundingRect, Rect.Empty) && 
                   !DoubleUtil.AreClose(boundingRect.Height, 0) && 
                   !DoubleUtil.AreClose(boundingRect.Width, 0))
            {
                Visual visualParent = parent as Visual;
                if (visualParent != null)
                {
                    Geometry clipGeometry = VisualTreeHelper.GetClip(visualParent);
                    if (clipGeometry != null)
                    {
                        GeneralTransform transform = owner.TransformToAncestor(visualParent).Inverse;
                        // Safer version of transform to descendent (doing the inverse ourself and saves us changing the co-ordinate space of the owner's bounding rectangle), 
                        // we want the rect inside of our space. (Which is always rectangular and much nicer to work with)
                        if (transform != null)
                        {
                            Rect clipBounds = clipGeometry.Bounds;
                            clipBounds = transform.TransformBounds(clipBounds);
                            boundingRect.Intersect(clipBounds);
                        }
                        else
                        {
                            // No visibility if non-invertable transform exists.
                            boundingRect = Rect.Empty;
                        }
                    }
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return boundingRect;
        }

        ///
        protected override AutomationOrientation GetOrientationCore()
        {
            return (AutomationOrientation.None);
        }
        
        ///
        protected override string GetItemTypeCore()
        {
            return AutomationProperties.GetItemType(_owner);
        }

        ///
        protected override string GetClassNameCore()
        {
            return string.Empty;
        }

        ///
        protected override string GetItemStatusCore()
        {
            return AutomationProperties.GetItemStatus(_owner);
        }

        ///
        protected override bool IsRequiredForFormCore()
        {
            return AutomationProperties.GetIsRequiredForForm(_owner);
        }

        /// 
        protected override bool IsKeyboardFocusableCore()
        {
            return Keyboard.IsFocusable(_owner);
        }

        ///
        protected override bool HasKeyboardFocusCore()
        {
            return _owner.IsKeyboardFocused;
        }

        ///
        protected override bool IsEnabledCore()
        {
            return _owner.IsEnabled;
        }

        ///
        protected override bool IsDialogCore()
        {
            return AutomationProperties.GetIsDialog(_owner);
        }

        ///
        protected override bool IsPasswordCore()
        {
            return false;
        }

        ///
        protected override bool IsContentElementCore()
        {
            return true;
        }

        ///
        protected override bool IsControlElementCore()
        {
            // We only want this peer to show up in the Control view if it is visible
            // For compat we allow falling back to legacy behavior (returning true always)
            // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
            return IncludeInvisibleElementsInControlView || _owner.IsVisible;
        }

        ///
        protected override AutomationPeer GetLabeledByCore()
        {
            UIElement element = AutomationProperties.GetLabeledBy(_owner);
            if (element != null)
                return element.GetAutomationPeer();

            return null;
        }

        ///
        protected override string GetAcceleratorKeyCore()
        {
            return AutomationProperties.GetAcceleratorKey(_owner);
        }

        ///
        protected override string GetAccessKeyCore()
        {
            string result = AutomationProperties.GetAccessKey(_owner);
            if (string.IsNullOrEmpty(result))
                return AccessKeyManager.InternalGetAccessKeyCharacter(_owner);

            return string.Empty;
        }
        
        protected override AutomationLiveSetting GetLiveSettingCore()
        {
            return AutomationProperties.GetLiveSetting(_owner);
        }

        /// <summary>
        /// Provides a value for UIAutomation's PositionInSet property
        /// Reads <see cref="AutomationProperties.PositionInSetProperty"/> and returns the value if one was provided,
        /// otherwise it attempts to calculate one.
        /// </summary>
        protected override int GetPositionInSetCore()
        {
            int positionInSet = AutomationProperties.GetPositionInSet(_owner);

            if (positionInSet == AutomationProperties.AutomationPositionInSetDefault)
            {
                // If a value has been set for <see cref="UIElement.PositionAndSizeOfSetController"/>
                // forward the call to that element, otherwise return the default value.
                UIElement element = _owner.PositionAndSizeOfSetController;
                if (element != null)
                {
                    AutomationPeer peer = UIElementAutomationPeer.FromElement(element);
                    peer = peer.EventsSource ?? peer;
                    if (peer != null)
                    {
                        try
                        {
                            positionInSet = peer.GetPositionInSet();
                        }
                        catch (ElementNotAvailableException)
                        {
                            positionInSet = AutomationProperties.AutomationPositionInSetDefault;
                        }
                    }
                }
            }

            return positionInSet;
        }

        /// <summary>
        /// Provides a value for UIAutomation's SizeOfSet property
        /// Reads <see cref="AutomationProperties.SizeOfSetProperty"/> and returns the value if one was provided,
        /// otherwise it attempts to calculate one.
        /// </summary>
        protected override int GetSizeOfSetCore()
        {
            int sizeOfSet = AutomationProperties.GetSizeOfSet(_owner);

            if (sizeOfSet == AutomationProperties.AutomationSizeOfSetDefault)
            {
                // If a value has been set for <see cref="UIElement.PositionAndSizeOfSetController"/>
                // forward the call to that element, otherwise return the default value.
                UIElement element = _owner.PositionAndSizeOfSetController;
                if (element != null)
                {
                    AutomationPeer peer = UIElementAutomationPeer.FromElement(element);
                    peer = peer.EventsSource ?? peer;
                    if (peer != null)
                    {
                        try
                        {
                            sizeOfSet = peer.GetSizeOfSet();
                        }
                        catch (ElementNotAvailableException)
                        {
                            sizeOfSet = AutomationProperties.AutomationSizeOfSetDefault;
                        }
                    }
                }
            }

            return sizeOfSet;
        }

        /// <summary>
        /// Provides a value for UIAutomation's HeadingLevel property
        /// Reads <see cref="AutomationProperties.HeadingLevelProperty"/> and returns the value
        /// </summary>
        protected override AutomationHeadingLevel GetHeadingLevelCore()
        {
            return AutomationProperties.GetHeadingLevel(_owner);
        }

        //
        // M E T H O D S
        //

        ///
        protected override Point GetClickablePointCore()
        {
            Point pt = new Point(double.NaN, double.NaN);
            
            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(_owner);

            // If there's no source, the element is not visible, return (double.NaN, double.NaN) point
            if(presentationSource == null)
                return pt;

            HwndSource hwndSource = presentationSource as HwndSource;

            // If the source isn't an HwnSource, there's not much we can do, return (double.NaN, double.NaN) point
            if(hwndSource == null)
                return pt;

            Rect rectElement    = new Rect(new Point(0, 0), _owner.RenderSize);
            Rect rectRoot       = PointUtil.ElementToRoot(rectElement, _owner, presentationSource);
            Rect rectClient     = PointUtil.RootToClient(rectRoot, presentationSource);
            Rect rectScreen     = PointUtil.ClientToScreen(rectClient, hwndSource);
            
            pt = new Point(rectScreen.Left + rectScreen.Width * 0.5, rectScreen.Top + rectScreen.Height * 0.5);

            return pt;
        }

        ///
        protected override void SetFocusCore() 
        { 
            if (!_owner.Focus())
                throw new InvalidOperationException(SR.SetFocusFailed);
        }

        private UIElement _owner;
        private SynchronizedInputAdaptor _synchronizedInputPattern;
    }
}

