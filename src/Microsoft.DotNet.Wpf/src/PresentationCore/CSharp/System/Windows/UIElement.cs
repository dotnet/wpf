// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.KnownBoxes;
using MS.Internal.Media;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using MS.Win32;

namespace System.Windows
{
    /// <summary>
    /// Visibility - Enum which describes 3 possible visibility options.
    /// </summary>
    /// <seealso cref="UIElement" />
    public enum Visibility : byte
    {
        /// <summary>
        /// Normally visible.
        /// </summary>
        Visible = 0,

        /// <summary>
        /// Occupies space in the layout, but is not visible (completely transparent).
        /// </summary>
        Hidden,

        /// <summary>
        /// Not visible and does not occupy any space in layout, as if it doesn't exist.
        /// </summary>
        Collapsed
    }

    // PreSharp uses message numbers that the C# compiler doesn't know about.
    // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691

    /// <summary>
    /// UIElement is the base class for frameworks building on the Windows Presentation Core.
    /// </summary>
    /// <remarks>
    /// UIElement adds to the base visual class "LIFE" - Layout, Input, Focus, and Eventing.
    /// UIElement can be considered roughly equivalent to an HWND in Win32, or an Element in Trident.
    /// UIElements can render (because they derive from Visual), visually size and position their children,
    /// respond to user input (including control of where input is getting sent to),
    /// and raise events that traverse the physical tree.<para/>
    ///
    /// UIElement is the most functional type in the Windows Presentation Core.
    /// </remarks>

    [UidProperty("Uid")]
    public partial class UIElement : Visual, IInputElement, IAnimatable
    {
        static UIElement()
        {
            UIElement.RegisterEvents(typeof(UIElement));

            RenderOptions.EdgeModeProperty.OverrideMetadata(
                typeof(UIElement),
                new UIPropertyMetadata(new PropertyChangedCallback(EdgeMode_Changed)));

            RenderOptions.BitmapScalingModeProperty.OverrideMetadata(
                typeof(UIElement),
                new UIPropertyMetadata(new PropertyChangedCallback(BitmapScalingMode_Changed)));

            RenderOptions.ClearTypeHintProperty.OverrideMetadata(
                typeof(UIElement),
                new UIPropertyMetadata(new PropertyChangedCallback(ClearTypeHint_Changed)));

            TextOptionsInternal.TextHintingModeProperty.OverrideMetadata(
                typeof(UIElement),
                new UIPropertyMetadata(new PropertyChangedCallback(TextHintingMode_Changed)));

            EventManager.RegisterClassHandler(typeof(UIElement), ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(OnManipulationStartingThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), ManipulationStartedEvent, new EventHandler<ManipulationStartedEventArgs>(OnManipulationStartedThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(OnManipulationDeltaThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(OnManipulationInertiaStartingThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), ManipulationBoundaryFeedbackEvent, new EventHandler<ManipulationBoundaryFeedbackEventArgs>(OnManipulationBoundaryFeedbackThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(OnManipulationCompletedThunk));
        }

        /// <summary>
        /// Constructor. This form of constructor will encounter a slight perf hit since it needs to initialize Dispatcher for the instance.
        /// </summary>
        public UIElement()
        {
            Initialize();
        }

        private void Initialize()
        {
            BeginPropertyInitialization();
            NeverMeasured = true;
            NeverArranged = true;

            SnapsToDevicePixelsCache = (bool) SnapsToDevicePixelsProperty.GetDefaultValue(DependencyObjectType);
            ClipToBoundsCache        = (bool) ClipToBoundsProperty.GetDefaultValue(DependencyObjectType);
            VisibilityCache          = (Visibility) VisibilityProperty.GetDefaultValue(DependencyObjectType);

            SetFlags(true, VisualFlags.IsUIElement);

            // Note: IsVisibleCache is false by default.

            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Verbose))
            {
                PerfService.GetPerfElementID(this);
            }
        }

        #region AllowDrop

        /// <summary>
        ///     The DependencyProperty for the AllowDrop property.
        /// </summary>
        public static readonly DependencyProperty AllowDropProperty =
                    DependencyProperty.Register(
                                "AllowDrop",
                                typeof(bool),
                                typeof(UIElement),
                                new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     A dependency property that allows the drop object as DragDrop target.
        /// </summary>
        public bool AllowDrop
        {
            get { return (bool) GetValue(AllowDropProperty); }
            set { SetValue(AllowDropProperty, BooleanBoxes.Box(value)); }
        }

        #endregion AllowDrop

        /// <summary>
        /// Get the StylusPlugInCollection associated with the UIElement
        /// </summary>
        protected StylusPlugInCollection StylusPlugIns
        {
            get
            {
                StylusPlugInCollection stylusCollection = StylusPlugInsField.GetValue(this);
                if (stylusCollection == null)
                {
                    stylusCollection = new StylusPlugInCollection(this);
                    StylusPlugInsField.SetValue(this, stylusCollection);
                }
                return stylusCollection;
            }
        }

        /// <summary>
        /// Returns the size the element computed during the Measure pass.
        /// This is only valid if IsMeasureValid is true.
        /// </summary>
        public Size DesiredSize
        {
            get
            {
                if(this.Visibility == Visibility.Collapsed)
                    return new Size(0,0);
                else
                    return _desiredSize;
            }
        }

        internal Size PreviousConstraint
        {
            get
            {
                return _previousAvailableSize;
            }
        }

        // This is needed to prevent dirty elements from drawing and crashing while doing so.
        private bool IsRenderable()
        {
            //elements that were created but never invalidated/measured are clean
            //from layout perspective, but we still don't want to render them
            //because they don't have state build up enough for that.
            if(NeverMeasured || NeverArranged)
                return false;

            //if element is collapsed, no rendering is needed
            //it is not only perf optimization, but also protection from
            //UIElement to break itself since RenderSize is reported as (0,0)
            //when UIElement is Collapsed
            if(ReadFlag(CoreFlags.IsCollapsed))
                return false;

            return IsMeasureValid && IsArrangeValid;
        }

        internal void InvalidateMeasureInternal()
        {
            MeasureDirty = true;
        }

        internal void InvalidateArrangeInternal()
        {
            ArrangeDirty = true;
        }

        /// <summary>
        /// Determines if the DesiredSize is valid.
        /// </summary>
        /// <remarks>
        /// A developer can force arrangement to be invalidated by calling InvalidateMeasure.
        /// IsArrangeValid and IsMeasureValid are related,
        /// in that arrangement cannot be valid without measurement first being valid.
        /// </remarks>
        public bool IsMeasureValid
        {
            get { return !MeasureDirty; }
        }

        /// <summary>
        /// Determines if the RenderSize and position of child elements is valid.
        /// </summary>
        /// <remarks>
        /// A developer can force arrangement to be invalidated by calling InvalidateArrange.
        /// IsArrangeValid and IsMeasureValid are related, in that arrangement cannot be valid without measurement first
        /// being valid.
        /// </remarks>
        public bool IsArrangeValid
        {
            get { return !ArrangeDirty; }
        }


        /// <summary>
        /// Invalidates the measurement state for the element.
        /// This has the effect of also invalidating the arrange state for the element.
        /// The element will be queued for an update layout that will occur asynchronously.
        /// </summary>
        public void InvalidateMeasure()
        {
            if(     !MeasureDirty
                &&  !MeasureInProgress )
            {
                Debug.Assert(MeasureRequest == null, "can't be clean and still have MeasureRequest");

//                 VerifyAccess();

                if(!NeverMeasured) //only measured once elements are allowed in *update* queue
                {
                    ContextLayoutManager ContextLayoutManager = ContextLayoutManager.From(Dispatcher);
                    if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose))
                    {
                        // Knowing when the layout queue goes from clean to dirty is interesting.
                        if (ContextLayoutManager.MeasureQueue.IsEmpty)
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutInvalidated, EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, PerfService.GetPerfElementID(this));
                        }
                    }

                    ContextLayoutManager.MeasureQueue.Add(this);
                }
                MeasureDirty = true;
            }
        }

        /// <summary>
        /// Invalidates the arrange state for the element.
        /// The element will be queued for an update layout that will occur asynchronously.
        /// MeasureCore will not be called unless InvalidateMeasure is also called - or that something
        /// else caused the measure state to be invalidated.
        /// </summary>
        public void InvalidateArrange()
        {
            if(   !ArrangeDirty
               && !ArrangeInProgress)
            {
                Debug.Assert(ArrangeRequest == null, "can't be clean and still have MeasureRequest");

//                 VerifyAccess();

                if(!NeverArranged)
                {
                    ContextLayoutManager ContextLayoutManager = ContextLayoutManager.From(Dispatcher);
                    ContextLayoutManager.ArrangeQueue.Add(this);
                }


                ArrangeDirty = true;
            }
        }

        /// <summary>
        /// Invalidates the rendering of the element.
        /// Causes <see cref="System.Windows.UIElement.OnRender"/> to be called at a later time.
        /// </summary>
        public void InvalidateVisual()
        {
            InvalidateArrange();
            RenderingInvalidated = true;
        }


        /// <summary>
        /// Notification that is called by Measure of a child when
        /// it ends up with different desired size for the child.
        /// </summary>
        /// <remarks>
        /// Default implementation simply calls invalidateMeasure(), assuming that layout of a
        /// parent should be updated after child changed its size.<para/>
        /// Finer point: this method can only be called in the scenario when the system calls Measure on a child,
        /// not when parent calls it since if parent calls it, it means parent has dirty layout and is recalculating already.
        /// </remarks>
        protected virtual void OnChildDesiredSizeChanged(UIElement child)
        {
            if(IsMeasureValid)
            {
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// This event fires every time Layout updates the layout of the trees associated with current Dispatcher.
        /// Layout update can happen as a result of some propety change, window resize or explicit user request.
        /// </summary>
        public event EventHandler LayoutUpdated
        {
            add
            {
                LayoutEventList.ListItem item = getLayoutUpdatedHandler(value);

                if(item == null)
                {
                    //set a weak ref in LM
                    item = ContextLayoutManager.From(Dispatcher).LayoutEvents.Add(value);
                    addLayoutUpdatedHandler(value, item);
                }
            }
            remove
            {
                LayoutEventList.ListItem item = getLayoutUpdatedHandler(value);

                if(item != null)
                {
                    removeLayoutUpdatedHandler(value);
                    //remove a weak ref from LM
                    ContextLayoutManager.From(Dispatcher).LayoutEvents.Remove(item);
                }
            }
        }


        private void addLayoutUpdatedHandler(EventHandler handler, LayoutEventList.ListItem item)
        {
            object cachedLayoutUpdatedItems = LayoutUpdatedListItemsField.GetValue(this);

            if(cachedLayoutUpdatedItems == null)
            {
               LayoutUpdatedListItemsField.SetValue(this, item);
               LayoutUpdatedHandlersField.SetValue(this, handler);
            }
            else
            {
                EventHandler cachedLayoutUpdatedHandler = LayoutUpdatedHandlersField.GetValue(this);
                if(cachedLayoutUpdatedHandler != null)
                {
                    //second unique handler is coming in.
                    //allocate a datastructure
                    Hashtable list = new Hashtable(2);

                    //add previously cached handler
                    list.Add(cachedLayoutUpdatedHandler, cachedLayoutUpdatedItems);

                    //add new handler
                    list.Add(handler, item);

                    LayoutUpdatedHandlersField.ClearValue(this);
                    LayoutUpdatedListItemsField.SetValue(this,list);
                }
                else //already have a list
                {
                    Hashtable list = (Hashtable)cachedLayoutUpdatedItems;
                    list.Add(handler, item);
                }
            }
        }

        private LayoutEventList.ListItem getLayoutUpdatedHandler(EventHandler d)
        {
            object cachedLayoutUpdatedItems = LayoutUpdatedListItemsField.GetValue(this);

            if(cachedLayoutUpdatedItems == null)
            {
               return null;
            }
            else
            {
                EventHandler cachedLayoutUpdatedHandler = LayoutUpdatedHandlersField.GetValue(this);
                if(cachedLayoutUpdatedHandler != null)
                {
                    if(cachedLayoutUpdatedHandler == d) return (LayoutEventList.ListItem)cachedLayoutUpdatedItems;
                }
                else //already have a list
                {
                    Hashtable list = (Hashtable)cachedLayoutUpdatedItems;
                    LayoutEventList.ListItem item = (LayoutEventList.ListItem)(list[d]);
                    return item;
                }
                return null;
            }
        }

        private void removeLayoutUpdatedHandler(EventHandler d)
        {
            object cachedLayoutUpdatedItems = LayoutUpdatedListItemsField.GetValue(this);
            EventHandler cachedLayoutUpdatedHandler = LayoutUpdatedHandlersField.GetValue(this);

            if(cachedLayoutUpdatedHandler != null) //single handler
            {
                if(cachedLayoutUpdatedHandler == d)
                {
                    LayoutUpdatedListItemsField.ClearValue(this);
                    LayoutUpdatedHandlersField.ClearValue(this);
                }
            }
            else //there is an ArrayList allocated
            {
                Hashtable list = (Hashtable)cachedLayoutUpdatedItems;
                list.Remove(d);
            }
        }

        /// <summary>
        /// Recursively propagates IsLayoutSuspended flag down to the whole v's sub tree.
        /// </summary>
        internal static void PropagateSuspendLayout(Visual v)
        {
            if(v.CheckFlagsAnd(VisualFlags.IsLayoutIslandRoot)) return;

            //the subtree is already suspended - happens when already suspended tree is further disassembled
            //no need to walk down in this case
            if(v.CheckFlagsAnd(VisualFlags.IsLayoutSuspended)) return;

            //  Assert that a UIElement has not being
            //  removed from the visual tree while updating layout.
            if (    Invariant.Strict
                &&  v.CheckFlagsAnd(VisualFlags.IsUIElement)    )
            {
                UIElement e = (UIElement)v;
                Invariant.Assert(!e.MeasureInProgress && !e.ArrangeInProgress);
            }

            v.SetFlags(true, VisualFlags.IsLayoutSuspended);
            v.TreeLevel = 0;

            int count = v.InternalVisualChildrenCount;

            for (int i = 0; i < count; i++)
            {
                Visual cv = v.InternalGetVisualChild(i);
                if (cv != null)
                {
                    PropagateSuspendLayout(cv);
                }
            }
        }

        /// <summary>
        /// Recursively resets IsLayoutSuspended flag on all visuals of the whole v's sub tree.
        /// For UIElements also re-inserts the UIElement into Measure and / or Arrange update queues
        /// if necessary.
        /// </summary>
        internal static void PropagateResumeLayout(Visual parent, Visual v)
        {
            if(v.CheckFlagsAnd(VisualFlags.IsLayoutIslandRoot)) return;

            //the subtree is already active - happens when new elements are added to the active tree
            //elements are created layout-active so they don't need to be specifically unsuspended
            //no need to walk down in this case
            //if(!v.CheckFlagsAnd(VisualFlags.IsLayoutSuspended)) return;

            //that can be true only on top of recursion, if suspended v is being connected to suspended parent.
            bool parentIsSuspended = parent == null ? false : parent.CheckFlagsAnd(VisualFlags.IsLayoutSuspended);
            uint parentTreeLevel   = parent == null ? 0     : parent.TreeLevel;

            if(parentIsSuspended) return;

            v.SetFlags(false, VisualFlags.IsLayoutSuspended);
            v.TreeLevel = parentTreeLevel + 1;

            if (v.CheckFlagsAnd(VisualFlags.IsUIElement))
            {
                //  re-insert UIElement into the update queues
                UIElement e = (UIElement)v;

                Invariant.Assert(!e.MeasureInProgress && !e.ArrangeInProgress);

                bool requireMeasureUpdate = e.MeasureDirty && !e.NeverMeasured && (e.MeasureRequest == null);
                bool requireArrangeUpdate = e.ArrangeDirty && !e.NeverArranged && (e.ArrangeRequest == null);
                ContextLayoutManager contextLayoutManager = (requireMeasureUpdate || requireArrangeUpdate)
                    ? ContextLayoutManager.From(e.Dispatcher)
                    : null;

                if (requireMeasureUpdate)
                {
                    contextLayoutManager.MeasureQueue.Add(e);
                }

                if (requireArrangeUpdate)
                {
                    contextLayoutManager.ArrangeQueue.Add(e);
                }
            }

            int count = v.InternalVisualChildrenCount;

            for (int i = 0; i < count; i++)
            {
                Visual cv = v.InternalGetVisualChild(i);
                if (cv != null)
                {
                    PropagateResumeLayout(v, cv);
                }
            }
        }

        /// <summary>
        /// Updates DesiredSize of the UIElement. Must be called by parents from theor MeasureCore, to form recursive update.
        /// This is first pass of layout update.
        /// </summary>
        /// <remarks>
        /// Measure is called by parents on their children. Internally, Measure calls MeasureCore override on the same object,
        /// giving it opportunity to compute its DesiredSize.<para/>
        /// This method will return immediately if child is not Dirty, previously measured
        /// and availableSize is the same as cached. <para/>
        /// This method also resets the IsMeasureinvalid bit on the child.<para/>
        /// In case when "unbounded measure to content" is needed, parent can use availableSize
        /// as double.PositiveInfinity. Any returned size is OK in this case.
        /// </remarks>
        /// <param name="availableSize">Available size that parent can give to the child. May be infinity (when parent wants to
        /// measure to content). This is soft constraint. Child can return bigger size to indicate that it wants bigger space and hope
        /// that parent can throw in scrolling...</param>
        public void Measure(Size availableSize)
        {
            bool etwTracingEnabled = false;
            long perfElementID = 0;
            ContextLayoutManager ContextLayoutManager = ContextLayoutManager.From(Dispatcher);
            if (ContextLayoutManager.AutomationEvents.Count != 0)
                UIElementHelper.InvalidateAutomationAncestors(this);

            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose))
            {
                perfElementID = PerfService.GetPerfElementID(this);

                etwTracingEnabled = true;
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureElementBegin, EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, perfElementID, availableSize.Width, availableSize.Height);
            }
            try
            {
                //             VerifyAccess();

                // Disable reentrancy during the measure pass.  This is because much work is done
                // during measure - such as inflating templates, formatting PTS stuff, creating
                // fonts, etc.  Generally speaking, we cannot survive reentrancy in these code
                // paths.
                using (Dispatcher.DisableProcessing())
                {
                    //enforce that Measure can not receive NaN size .
                    if (DoubleUtil.IsNaN(availableSize.Width) || DoubleUtil.IsNaN(availableSize.Height))
                        throw new InvalidOperationException(SR.Get(SRID.UIElement_Layout_NaNMeasure));

                    bool neverMeasured = NeverMeasured;

                    if (neverMeasured)
                    {
                        switchVisibilityIfNeeded(this.Visibility);
                        //to make sure effects are set correctly - otherwise it's not used
                        //simply because it is never pulled by anybody
                        pushVisualEffects();
                    }

                    bool isCloseToPreviousMeasure = DoubleUtil.AreClose(availableSize, _previousAvailableSize);


                    //if Collapsed, we should not Measure, keep dirty bit but remove request
                    if (this.Visibility == Visibility.Collapsed
                        || ((Visual)this).CheckFlagsAnd(VisualFlags.IsLayoutSuspended))
                    {
                        //reset measure request.
                        if (MeasureRequest != null)
                            ContextLayoutManager.From(Dispatcher).MeasureQueue.Remove(this);

                        //  remember though that parent tried to measure at this size
                        //  in case when later this element is called to measure incrementally
                        //  it has up-to-date information stored in _previousAvailableSize
                        if (!isCloseToPreviousMeasure)
                        {
                            //this will ensure that element will be actually re-measured at the new available size
                            //later when it becomes visible.
                            InvalidateMeasureInternal();

                            _previousAvailableSize = availableSize;
                        }

                        return;
                    }


                    //your basic bypass. No reason to calc the same thing.
                    if (IsMeasureValid                       //element is clean
                        && !neverMeasured                       //previously measured
                        && isCloseToPreviousMeasure) //and contraint matches
                    {
                        return;
                    }

                    NeverMeasured = false;
                    Size prevSize = _desiredSize;

                    //we always want to be arranged, ensure arrange request
                    //doing it before OnMeasure prevents unneeded requests from children in the queue
                    InvalidateArrange();
                    //_measureInProgress prevents OnChildDesiredSizeChange to cause the elements be put
                    //into the queue.

                    MeasureInProgress = true;

                    Size desiredSize = new Size(0, 0);

                    ContextLayoutManager layoutManager = ContextLayoutManager.From(Dispatcher);

                    bool gotException = true;

                    try
                    {
                        layoutManager.EnterMeasure();
                        desiredSize = MeasureCore(availableSize);

                        gotException = false;
                    }
                    finally
                    {
                        // reset measure in progress
                        MeasureInProgress = false;

                        _previousAvailableSize = availableSize;

                        layoutManager.ExitMeasure();

                        if (gotException)
                        {
                            // we don't want to reset last exception element on layoutManager if it's been already set.
                            if (layoutManager.GetLastExceptionElement() == null)
                            {
                                layoutManager.SetLastExceptionElement(this);
                            }
                        }
                    }

                    //enforce that MeasureCore can not return PositiveInfinity size even if given Infinte availabel size.
                    //Note: NegativeInfinity can not be returned by definition of Size structure.
                    if (double.IsPositiveInfinity(desiredSize.Width) || double.IsPositiveInfinity(desiredSize.Height))
                        throw new InvalidOperationException(SR.Get(SRID.UIElement_Layout_PositiveInfinityReturned, this.GetType().FullName));

                    //enforce that MeasureCore can not return NaN size .
                    if (DoubleUtil.IsNaN(desiredSize.Width) || DoubleUtil.IsNaN(desiredSize.Height))
                        throw new InvalidOperationException(SR.Get(SRID.UIElement_Layout_NaNReturned, this.GetType().FullName));

                    //reset measure dirtiness

                    MeasureDirty = false;
                    //reset measure request.
                    if (MeasureRequest != null)
                        ContextLayoutManager.From(Dispatcher).MeasureQueue.Remove(this);

                    //cache desired size
                    _desiredSize = desiredSize;

                    //notify parent if our desired size changed (watefall effect)
                    if (!MeasureDuringArrange
                       && !DoubleUtil.AreClose(prevSize, desiredSize))
                    {
                        UIElement p;
                        IContentHost ich;
                        GetUIParentOrICH(out p, out ich); //only one will be returned
                        if (p != null && !p.MeasureInProgress) //this is what differs this code from signalDesiredSizeChange()
                            p.OnChildDesiredSizeChanged(this);
                        else if (ich != null)
                            ich.OnChildDesiredSizeChanged(this);
                    }
                }
            }
            finally
            {
                if (etwTracingEnabled == true)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureElementEnd, EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, perfElementID, _desiredSize.Width, _desiredSize.Height);
                }
            }
        }

         //only one will be returned, whichever found first
        internal void GetUIParentOrICH(out UIElement uiParent, out IContentHost ich)
        {
            ich = null;
            uiParent = null;

            for(Visual v = VisualTreeHelper.GetParent(this) as Visual; v != null; v = VisualTreeHelper.GetParent(v) as Visual)
            {
                ich = v as IContentHost;
                if (ich != null) break;

                if(v.CheckFlagsAnd(VisualFlags.IsUIElement))
                {
                    uiParent = (UIElement)v;
                    break;
                }
            }
        }

         //walks visual tree up to find UIElement parent within Element Layout Island, so stops the walk if the island's root is found
        internal UIElement GetUIParentWithinLayoutIsland()
        {
            UIElement uiParent = null;

            for(Visual v = VisualTreeHelper.GetParent(this) as Visual; v != null; v = VisualTreeHelper.GetParent(v) as Visual)
            {
                if (v.CheckFlagsAnd(VisualFlags.IsLayoutIslandRoot))
                {
                    break;
                }

                if(v.CheckFlagsAnd(VisualFlags.IsUIElement))
                {
                    uiParent = (UIElement)v;
                    break;
                }
            }
            return uiParent;
        }


        /// <summary>
        /// Parents or system call this method to arrange the internals of children on a second pass of layout update.
        /// </summary>
        /// <remarks>
        /// This method internally calls ArrangeCore override, giving the derived class opportunity
        /// to arrange its children and/or content using final computed size.
        /// In their ArrangeCore overrides, derived class is supposed to create its visual structure and
        /// prepare itself for rendering. Arrange is called by parents
        /// from their implementation of ArrangeCore or by system when needed.
        /// This method sets Bounds=finalSize before calling ArrangeCore.
        /// </remarks>
        /// <param name="finalRect">This is the final size and location that parent or system wants this UIElement to assume.</param>
        public void Arrange(Rect finalRect)
        {
            bool etwTracingEnabled = false;
            long perfElementID = 0;

            ContextLayoutManager ContextLayoutManager = ContextLayoutManager.From(Dispatcher);
            if (ContextLayoutManager.AutomationEvents.Count != 0)
                UIElementHelper.InvalidateAutomationAncestors(this);

            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose))
            {
                perfElementID = PerfService.GetPerfElementID(this);

                etwTracingEnabled = true;
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeElementBegin, EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, perfElementID, finalRect.Top, finalRect.Left, finalRect.Width, finalRect.Height);
            }

            try
            {
                //             VerifyAccess();

                // Disable reentrancy during the arrange pass.  This is because much work is done
                // during arrange - such as formatting PTS stuff, creating
                // fonts, etc.  Generally speaking, we cannot survive reentrancy in these code
                // paths.
                using (Dispatcher.DisableProcessing())
                {
                    //enforce that Arrange can not come with Infinity size or NaN
                    if (double.IsPositiveInfinity(finalRect.Width)
                        || double.IsPositiveInfinity(finalRect.Height)
                        || DoubleUtil.IsNaN(finalRect.Width)
                        || DoubleUtil.IsNaN(finalRect.Height)
                      )
                    {
                        DependencyObject parent = GetUIParent() as UIElement;
                        throw new InvalidOperationException(
                            SR.Get(
                                SRID.UIElement_Layout_InfinityArrange,
                                    (parent == null ? "" : parent.GetType().FullName),
                                    this.GetType().FullName));
                    }


                    //if Collapsed, we should not Arrange, keep dirty bit but remove request
                    if (this.Visibility == Visibility.Collapsed
                        || ((Visual)this).CheckFlagsAnd(VisualFlags.IsLayoutSuspended))
                    {
                        //reset arrange request.
                        if (ArrangeRequest != null)
                            ContextLayoutManager.From(Dispatcher).ArrangeQueue.Remove(this);

                        //  remember though that parent tried to arrange at this rect
                        //  in case when later this element is called to arrange incrementally
                        //  it has up-to-date information stored in _finalRect
                        _finalRect = finalRect;

                        return;
                    }

                    //in case parent did not call Measure on a child, we call it now.
                    //parent can skip calling Measure on a child if it does not care about child's size
                    //passing finalSize practically means "set size" because that's what Measure(sz)/Arrange(same_sz) means
                    //Note that in case of IsLayoutSuspended (temporarily out of the tree) the MeasureDirty can be true
                    //while it does not indicate that we should re-measure - we just came of Measure that did nothing
                    //because of suspension
                    if (MeasureDirty
                       || NeverMeasured)
                    {
                        try
                        {
                            MeasureDuringArrange = true;
                            //If never measured - that means "set size", arrange-only scenario
                            //Otherwise - the parent previosuly measured the element at constriant
                            //and the fact that we are arranging the measure-dirty element now means
                            //we are not in the UpdateLayout loop but rather in manual sequence of Measure/Arrange
                            //(like in HwndSource when new RootVisual is attached) so there are no loops and there could be
                            //measure-dirty elements left after previosu single Measure pass) - so need to use cached constraint
                            if (NeverMeasured)
                                Measure(finalRect.Size);
                            else
                            {
                                Measure(PreviousConstraint);
                            }
                        }
                        finally
                        {
                            MeasureDuringArrange = false;
                        }
                    }

                    //bypass - if clean and rect is the same, no need to re-arrange
                    if (!IsArrangeValid
                        || NeverArranged
                        || !DoubleUtil.AreClose(finalRect, _finalRect))
                    {
                        bool firstArrange = NeverArranged;
                        NeverArranged = false;
                        ArrangeInProgress = true;

                        ContextLayoutManager layoutManager = ContextLayoutManager.From(Dispatcher);

                        Size oldSize = RenderSize;
                        bool sizeChanged = false;
                        bool gotException = true;

                        // If using layout rounding, round final size before calling ArrangeCore.
                        if (CheckFlagsAnd(VisualFlags.UseLayoutRounding))
                        {
                            DpiScale dpi = GetDpi();
                            finalRect = RoundLayoutRect(finalRect, dpi.DpiScaleX, dpi.DpiScaleY);
                        }

                        try
                        {
                            layoutManager.EnterArrange();

                            //This has to update RenderSize
                            ArrangeCore(finalRect);

                            //to make sure Clip is tranferred to Visual
                            ensureClip(finalRect.Size);

                            //  see if we need to call OnRenderSizeChanged on this element
                            sizeChanged = markForSizeChangedIfNeeded(oldSize, RenderSize);

                            gotException = false;
                        }
                        finally
                        {
                            ArrangeInProgress = false;
                            layoutManager.ExitArrange();

                            if (gotException)
                            {
                                // we don't want to reset last exception element on layoutManager if it's been already set.
                                if (layoutManager.GetLastExceptionElement() == null)
                                {
                                    layoutManager.SetLastExceptionElement(this);
                                }
                            }

                        }

                        _finalRect = finalRect;

                        ArrangeDirty = false;

                        //reset request.
                        if (ArrangeRequest != null)
                            ContextLayoutManager.From(Dispatcher).ArrangeQueue.Remove(this);

                        if ((sizeChanged || RenderingInvalidated || firstArrange) && IsRenderable())
                        {
                            DrawingContext dc = RenderOpen();
                            try
                            {
                                bool etwGeneralEnabled = EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose);
                                if (etwGeneralEnabled == true)
                                {
                                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientOnRenderBegin, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, perfElementID);
                                }

                                try
                                {
                                    OnRender(dc);
                                }
                                finally
                                {
                                    if (etwGeneralEnabled == true)
                                    {
                                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientOnRenderEnd, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, perfElementID);
                                    }
                                }
                            }
                            finally
                            {
                                dc.Close();
                                RenderingInvalidated = false;
                            }

                            updatePixelSnappingGuidelines();
                        }

                        if (firstArrange)
                        {
                            EndPropertyInitialization();
                        }
                    }
                }
            }
            finally
            {
                if (etwTracingEnabled == true)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeElementEnd, EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, perfElementID, finalRect.Top, finalRect.Left, finalRect.Width, finalRect.Height);
                }
            }
        }

        /// <summary>
        /// OnRender is called by the base class when the rendering instructions of the UIElement are required.
        /// Note: the drawing instructions sent to DrawingContext are not rendered immediately on the screen
        /// but rather stored and later passed to the rendering engine at proper time.
        /// Derived classes override this method to draw the content of the UIElement.
        /// </summary>
        protected virtual void OnRender(DrawingContext drawingContext)
        {
        }

        private void updatePixelSnappingGuidelines()
        {
            if((!SnapsToDevicePixels) || (_drawingContent == null))
            {
                this.VisualXSnappingGuidelines = this.VisualYSnappingGuidelines = null;
            }
            else
            {
                DoubleCollection xLines = this.VisualXSnappingGuidelines;

                if(xLines == null)
                {
                    xLines = new DoubleCollection();
                    xLines.Add(0d);
                    xLines.Add(this.RenderSize.Width);
                    this.VisualXSnappingGuidelines = xLines;
                }
                else
                {
                // xLines[0] = 0d;  - this already should be so
                // check to avoid potential dirtiness in renderer
                    int lastGuideline = xLines.Count - 1;
                    if(!DoubleUtil.AreClose(xLines[lastGuideline], this.RenderSize.Width))
                        xLines[lastGuideline] = this.RenderSize.Width;
                }

                DoubleCollection yLines = this.VisualYSnappingGuidelines;
                if(yLines == null)
                {
                    yLines = new DoubleCollection();
                    yLines.Add(0d);
                    yLines.Add(this.RenderSize.Height);
                    this.VisualYSnappingGuidelines = yLines;
                }
                else
                {
                // yLines[0] = 0d;  - this already should be so
                // check to avoid potential dirtiness in renderer
                    int lastGuideline = yLines.Count - 1;
                    if(!DoubleUtil.AreClose(yLines[lastGuideline], this.RenderSize.Height))
                        yLines[lastGuideline] = this.RenderSize.Height;
                }
            }
        }

        private bool markForSizeChangedIfNeeded(Size oldSize, Size newSize)
        {
            //already marked for SizeChanged, simply update the newSize
            bool widthChanged = !DoubleUtil.AreClose(oldSize.Width, newSize.Width);
            bool heightChanged = !DoubleUtil.AreClose(oldSize.Height, newSize.Height);

            SizeChangedInfo info = sizeChangedInfo;

            if(info != null)
            {
                info.Update(widthChanged, heightChanged);
                return true;
            }
            else if(widthChanged || heightChanged)
            {
                info = new SizeChangedInfo(this, oldSize, widthChanged, heightChanged);
                sizeChangedInfo = info;
                ContextLayoutManager.From(Dispatcher).AddToSizeChangedChain(info);

                //
                // This notifies Visual layer that hittest boundary potentially changed
                //

                PropagateFlags(
                    this,
                    VisualFlags.IsSubtreeDirtyForPrecompute,
                    VisualProxyFlags.IsSubtreeDirtyForRender);

                return true;
            }

            //this result is used to determine if we need to call OnRender after Arrange
            //OnRender is called for 2 reasons - someone called InvalidateVisual - then OnRender is called
            //on next Arrange, or the size changed.
            return false;
        }

        /// <summary>
        /// Rounds a size to integer values for layout purposes, compensating for high DPI screen coordinates.
        /// </summary>
        /// <param name="size">Input size.</param>
        /// <param name="dpiScaleX">DPI along x-dimension.</param>
        /// <param name="dpiScaleY">DPI along y-dimension.</param>
        /// <returns>Value of size that will be rounded under screen DPI.</returns>
        /// <remarks>This is a layout helper method. It takes DPI into account and also does not return
        /// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper associated with
        /// UseLayoutRounding  property and should not be used as a general rounding utility.</remarks>
        internal static Size RoundLayoutSize(Size size, double dpiScaleX, double dpiScaleY)
        {
            return new Size(RoundLayoutValue(size.Width, dpiScaleX), RoundLayoutValue(size.Height, dpiScaleY));
        }

        /// <summary>
        /// Calculates the value to be used for layout rounding at high DPI.
        /// </summary>
        /// <param name="value">Input value to be rounded.</param>
        /// <param name="dpiScale">Ratio of screen's DPI to layout DPI</param>
        /// <returns>Adjusted value that will produce layout rounding on screen at high dpi.</returns>
        /// <remarks>This is a layout helper method. It takes DPI into account and also does not return
        /// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper associated with
        /// UseLayoutRounding  property and should not be used as a general rounding utility.</remarks>
        internal static double RoundLayoutValue(double value, double dpiScale)
        {
            double newValue;

            // If DPI == 1, don't use DPI-aware rounding.
            if (!DoubleUtil.AreClose(dpiScale, 1.0))
            {
                newValue = Math.Round(value * dpiScale) / dpiScale;
                // If rounding produces a value unacceptable to layout (NaN, Infinity or MaxValue), use the original value.
                if (DoubleUtil.IsNaN(newValue) ||
                    Double.IsInfinity(newValue) ||
                    DoubleUtil.AreClose(newValue, Double.MaxValue))
                {
                    newValue = value;
                }
            }
            else
            {
                newValue = Math.Round(value);
            }

            return newValue;
        }

        /// <summary>
        /// If layout rounding is in use, rounds the size and offset of a rect.
        /// </summary>
        /// <param name="rect">Rect to be rounded.</param>
        /// <param name="dpiScaleX">DPI along x-dimension.</param>
        /// <param name="dpiScaleY">DPI along y-dimension.</param>
        /// <returns>Rounded rect.</returns>
        /// <remarks>This is a layout helper method. It takes DPI into account and also does not return
        /// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper associated with
        /// UseLayoutRounding  property and should not be used as a general rounding utility.</remarks>
        internal static Rect RoundLayoutRect(Rect rect, double dpiScaleX, double dpiScaleY)
        {
            return new Rect(RoundLayoutValue(rect.X, dpiScaleX),
                            RoundLayoutValue(rect.Y, dpiScaleY),
                            RoundLayoutValue(rect.Width, dpiScaleX),
                            RoundLayoutValue(rect.Height, dpiScaleY)
                            );
        }

        /// <summary>
        /// Ensures that _dpiScaleX and _dpiScaleY are set.
        /// </summary>
        /// <remarks>
        /// Should be called before reading _dpiScaleX and _dpiScaleY
        /// </remarks>
        internal static DpiScale EnsureDpiScale()
        {
            if (_setDpi)
            {
                _setDpi = false;
                int dpiX, dpiY;
                HandleRef desktopWnd = new HandleRef(null, IntPtr.Zero);

                // Win32Exception will get the Win32 error code so we don't have to
#pragma warning disable 6523
                IntPtr dc = UnsafeNativeMethods.GetDC(desktopWnd);

                // Detecting error case from unmanaged call, required by PREsharp to throw a Win32Exception
#pragma warning disable 6503
                if (dc == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
#pragma warning restore 6503
#pragma warning restore 6523

                try
                {
                    dpiX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), NativeMethods.LOGPIXELSX);
                    dpiY = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), NativeMethods.LOGPIXELSY);
                    _dpiScaleX = (double)dpiX / DpiUtil.DefaultPixelsPerInch;
                    _dpiScaleY = (double)dpiY / DpiUtil.DefaultPixelsPerInch;
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseDC(desktopWnd, new HandleRef(null, dc));
                }
            }
            return new DpiScale(_dpiScaleX, _dpiScaleY);
        }

        /// <summary>
        /// This is invoked after layout update before rendering if the element's RenderSize
        /// has changed as a result of layout update.
        /// </summary>
        /// <param name="info">Packaged parameters (<seealso cref="SizeChangedInfo"/>, includes
        /// old and new sizes and which dimension actually changes. </param>
        protected internal virtual void OnRenderSizeChanged(SizeChangedInfo info)
        {}

        /// <summary>
        /// Measurement override. Implement your size-to-content logic here.
        /// </summary>
        /// <remarks>
        /// MeasureCore is designed to be the main customizability point for size control of layout.
        /// Element authors should override this method, call Measure on each child element,
        /// and compute their desired size based upon the measurement of the children.
        /// The return value should be the desired size.<para/>
        /// Note: It is required that a parent element calls Measure on each child or they won't be sized/arranged.
        /// Typical override follows a pattern roughly like this (pseudo-code):
        /// <example>
        ///     <code lang="C#">
        /// <![CDATA[
        ///
        /// protected override Size MeasureCore(Size availableSize)
        /// {
        ///     foreach (UIElement child in ...)
        ///     {
        ///         child.Measure(availableSize);
        ///         availableSize.Deflate(child.DesiredSize);
        ///         _cache.StoreInfoAboutChild(child);
        ///     }
        ///
        ///     Size desired = CalculateBasedOnCache(_cache);
        ///     return desired;
        /// }
        /// ]]>
        ///     </code>
        /// </example>
        /// The key aspects of this snippet are:
        ///     <list type="bullet">
        /// <item>You must call Measure on each child element</item>
        /// <item>It is common to cache measurement information between the MeasureCore and ArrangeCore method calls</item>
        /// <item>Calling base.MeasureCore is not required.</item>
        /// <item>Calls to Measure on children are passing either the same availableSize as the parent, or a subset of the area depending
        /// on the type of layout the parent will perform (for example, it would be valid to remove the area
        /// for some border or padding).</item>
        ///     </list>
        /// </remarks>
        /// <param name="availableSize">Available size that parent can give to the child. May be infinity (when parent wants to
        /// measure to content). This is soft constraint. Child can return bigger size to indicate that it wants bigger space and hope
        /// that parent can throw in scrolling...</param>
        /// <returns>Desired Size of the control, given available size passed as parameter.</returns>
        protected virtual Size MeasureCore(Size availableSize)
        {
            //can not return availableSize here - this is too "greedy" and can cause the Infinity to be
            //returned. So the next "reasonable" choice is (0,0).
            return new Size(0,0);
        }

        /// <summary>
        /// ArrangeCore allows for the customization of the final sizing and positioning of children.
        /// </summary>
        /// <remarks>
        /// Element authors should override this method, call Arrange on each visible child element,
        /// to size and position each child element by passing a rectangle reserved for the child within parent space.
        /// Note: It is required that a parent element calls Arrange on each child or they won't be rendered.
        /// Typical override follows a pattern roughly like this (pseudo-code):
        /// <example>
        ///     <code lang="C#">
        /// <![CDATA[
        ///
        /// protected override Size ArrangeCore(Rect finalRect)
        /// {
        ///     //Call base, it will set offset and _size to the finalRect:
        ///     base.ArrangeCore(finalRect);
        ///
        ///     foreach (UIElement child in ...)
        ///     {
        ///         child.Arrange(new Rect(childX, childY, childWidth, childHeight);
        ///     }
        /// }
        /// ]]>
        ///     </code>
        /// </example>
        /// </remarks>
        /// <param name="finalRect">The final area within the parent that element should use to arrange itself and its children.</param>
        protected virtual void ArrangeCore(Rect finalRect)
        {
            // Set the element size.
            RenderSize = finalRect.Size;

            //Set transform to reflect the offset of finalRect - parents that have multiple children
            //pass offset in the finalRect to communicate the location of this child withing the parent.
            Transform renderTransform = RenderTransform;
            if(renderTransform == Transform.Identity)
                renderTransform = null;

            Vector oldOffset = VisualOffset;
            if (!DoubleUtil.AreClose(oldOffset.X, finalRect.X) ||
                !DoubleUtil.AreClose(oldOffset.Y, finalRect.Y))
            {
                VisualOffset = new Vector(finalRect.X, finalRect.Y);
            }

            if (renderTransform != null)
            {
                //render transform + layout offset, create a collection
                TransformGroup t = new TransformGroup();

                Point origin = RenderTransformOrigin;
                bool hasOrigin = (origin.X != 0d || origin.Y != 0d);
                if (hasOrigin)
                    t.Children.Add(new TranslateTransform(-(finalRect.Width * origin.X), -(finalRect.Height * origin.Y)));

                t.Children.Add(renderTransform);

                if (hasOrigin)
                    t.Children.Add(new TranslateTransform(finalRect.Width * origin.X,
                                                          finalRect.Height * origin.Y));

                VisualTransform = t;
            }
            else
            {
                VisualTransform = null;
            }
        }

        /// <summary>
        /// This is a public read-only property that returns size of the UIElement.
        /// This size is typcally used to find out where ink of the element will go.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Size RenderSize
        {
            get
            {
                if (this.Visibility == Visibility.Collapsed)
                    return new Size();
                else
                    return _size;
            }
            set
            {
                _size = value;
                InvalidateHitTestBounds();
            }
        }

        /// <summary>
        /// This method returns the hit-test bounds of a UIElement to the underlying Visual layer.
        /// </summary>
        internal override Rect GetHitTestBounds()
        {
            Rect hitBounds = new Rect(_size);

            if (_drawingContent != null)
            {
                MediaContext mediaContext = MediaContext.From(Dispatcher);
                BoundsDrawingContextWalker ctx = mediaContext.AcquireBoundsDrawingContextWalker();

                Rect resultRect = _drawingContent.GetContentBounds(ctx);
                mediaContext.ReleaseBoundsDrawingContextWalker(ctx);

                hitBounds.Union(resultRect);
            }

            return hitBounds;
        }

        /// <summary>
        /// The RenderTransform dependency property.
        /// </summary>
        /// <seealso cref="UIElement.RenderTransform" />
        [CommonDependencyProperty]
        public static readonly DependencyProperty RenderTransformProperty =
                    DependencyProperty.Register(
                                "RenderTransform",
                                typeof(Transform),
                                typeof(UIElement),
                                new PropertyMetadata(
                                            Transform.Identity,
                                            new PropertyChangedCallback(RenderTransform_Changed)));


        /// <summary>
        /// The RenderTransform property defines the transform that will be applied to UIElement during rendering of its content.
        /// This transform does not affect layout of the panel into which the UIElement is nested - the layout does not take this
        /// transform into account to determine the location and RenderSize of the UIElement.
        /// </summary>
        public Transform RenderTransform
        {
            get { return (Transform) GetValue(RenderTransformProperty); }
            set { SetValue(RenderTransformProperty, value); }
        }

        private static void RenderTransform_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;

            //if never measured, then nothing to do, it should be measured at some point
            if(!uie.NeverMeasured && !uie.NeverArranged)
            {
                // If the change is simply a subproperty change, there is no
                //  need to Arrange. (which combines RenderTransform with all the
                //  other transforms.)
                if (!e.IsASubPropertyChange)
                {
                    uie.InvalidateArrange();
                    uie.AreTransformsClean = false;
                }
            }
        }

        /// <summary>
        /// The RenderTransformOrigin dependency property.
        /// </summary>
        /// <seealso cref="UIElement.RenderTransformOrigin" />
        public static readonly DependencyProperty RenderTransformOriginProperty =
                    DependencyProperty.Register(
                                "RenderTransformOrigin",
                                typeof(Point),
                                typeof(UIElement),
                                new PropertyMetadata(
                                            new Point(0d,0d),
                                            new PropertyChangedCallback(RenderTransformOrigin_Changed)),
                                new ValidateValueCallback(IsRenderTransformOriginValid));


        private static bool IsRenderTransformOriginValid(object value)
        {
            Point v = (Point)value;
            return (    (!DoubleUtil.IsNaN(v.X) && !Double.IsPositiveInfinity(v.X) && !Double.IsNegativeInfinity(v.X))
                     && (!DoubleUtil.IsNaN(v.Y) && !Double.IsPositiveInfinity(v.Y) && !Double.IsNegativeInfinity(v.Y)));
        }


        /// <summary>
        /// The RenderTransformOrigin property defines the center of the RenderTransform relative to
        /// bounds of the element. It is a Point and both X and Y components are between 0 and 1.0 - the
        /// (0,0) is the default value and specified top-left corner of the element, (0.5, 0.5) is the center of element
        /// and (1,1) is the bottom-right corner of element.
        /// </summary>
        public Point RenderTransformOrigin
        {
            get { return (Point)GetValue(RenderTransformOriginProperty); }
            set { SetValue(RenderTransformOriginProperty, value); }
        }

        private static void RenderTransformOrigin_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;

            //if never measured, then nothing to do, it should be measured at some point
            if(!uie.NeverMeasured && !uie.NeverArranged)
            {
                uie.InvalidateArrange();
                uie.AreTransformsClean = false;
            }
        }

        /// <summary>
        /// OnVisualParentChanged is called when the parent of the Visual is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            // Synchronize ForceInherit properties
            if (_parent != null)
            {
                DependencyObject parent = _parent;

                if (!InputElement.IsUIElement(parent) && !InputElement.IsUIElement3D(parent))
                {
                    Visual parentAsVisual = parent as Visual;

                    if (parentAsVisual != null)
                    {
                        // We are being plugged into a non-UIElement visual. This
                        // means that our parent doesn't play by the same rules we
                        // do, so we need to track changes to our ancestors in
                        // order to bridge the gap.
                        parentAsVisual.VisualAncestorChanged += new AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);

                        // Try to find a UIElement ancestor to use for coersion.
                        parent = InputElement.GetContainingUIElement(parentAsVisual);
                    }
                    else
                    {
                        Visual3D parentAsVisual3D = parent as Visual3D;

                        if (parentAsVisual3D != null)
                        {
                            // We are being plugged into a non-UIElement visual. This
                            // means that our parent doesn't play by the same rules we
                            // do, so we need to track changes to our ancestors in
                            // order to bridge the gap.
                            parentAsVisual3D.VisualAncestorChanged += new AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);

                            // Try to find a UIElement ancestor to use for coersion.
                            parent = InputElement.GetContainingUIElement(parentAsVisual3D);
                        }
                    }
                }

                if (parent != null)
                {
                    SynchronizeForceInheritProperties(this, null, null, parent);
                }
                else
                {
                    // We don't have a UIElement parent or ancestor, so no
                    // coersions are necessary at this time.
                }
            }
            else
            {
                DependencyObject parent = oldParent;

                if (!InputElement.IsUIElement(parent) && !InputElement.IsUIElement3D(parent))
                {
                    // We are being unplugged from a non-UIElement visual. This
                    // means that our parent didn't play by the same rules we
                    // do, so we started track changes to our ancestors in
                    // order to bridge the gap.  Now we can stop.
                    if (oldParent is Visual)
                    {
                        ((Visual) oldParent).VisualAncestorChanged -= new AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);
                    }
                    else if (oldParent is Visual3D)
                    {
                        ((Visual3D) oldParent).VisualAncestorChanged -= new AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);
                    }

                    // Try to find a UIElement ancestor to use for coersion.
                    parent = InputElement.GetContainingUIElement(oldParent);
                }

                if (parent != null)
                {
                    SynchronizeForceInheritProperties(this, null, null, parent);
                }
                else
                {
                    // We don't have a UIElement parent or ancestor, so no
                    // coersions are necessary at this time.
                }
            }

            // Synchronize ReverseInheritProperty Flags
            //
            // NOTE: do this AFTER synchronizing force-inherited flags, since
            // they often effect focusability and such.
            this.SynchronizeReverseInheritPropertyFlags(oldParent, true);
        }

        private void OnVisualAncestorChanged_ForceInherit(object sender, AncestorChangedEventArgs e)
        {
            // NOTE:
            //
            // We are forced to listen to AncestorChanged events because
            // a UIElement may have raw Visuals between it and its nearest
            // UIElement parent.  We only care about changes that happen
            // to the visual tree BETWEEN this UIElement and its nearest
            // UIElement parent.  This is because we can rely on our
            // nearest UIElement parent to notify us when its force-inherit
            // properties change.

            DependencyObject parent = null;
            if(e.OldParent == null)
            {
                // We were plugged into something.

                // Find our nearest UIElement parent.
                parent = InputElement.GetContainingUIElement(_parent);

                // See if this parent is a child of the ancestor who's parent changed.
                // If so, we don't care about changes that happen above us.
                if(parent != null && VisualTreeHelper.IsAncestorOf(e.Ancestor, parent))
                {
                    parent = null;
                }
            }
            else
            {
                // we were unplugged from something.

                // Find our nearest UIElement parent.
                parent = InputElement.GetContainingUIElement(_parent);

                if(parent != null)
                {
                    // If we found a UIElement parent in our subtree, the
                    // break in the visual tree must have been above it,
                    // so we don't need to respond.
                    parent = null;
                }
                else
                {
                    // There was no UIElement parent in our subtree, so we
                    // may be detaching from some UIElement parent above
                    // the break point in the tree.
                    parent = InputElement.GetContainingUIElement(e.OldParent);
                }
            }

            if(parent != null)
            {
                SynchronizeForceInheritProperties(this, null, null, parent);
            }
        }

        internal void OnVisualAncestorChanged(object sender, AncestorChangedEventArgs e)
        {
            UIElement uie = sender as UIElement;
            if(null != uie)
                PresentationSource.OnVisualAncestorChanged(uie, e);
        }

        /// <summary>
        /// Helper, gives the UIParent under control of which
        /// the OnMeasure or OnArrange are currently called.
        /// This may be implemented as a tree walk up until
        /// LayoutElement is found.
        /// </summary>
        internal DependencyObject GetUIParent()
        {
            return UIElementHelper.GetUIParent(this, false);
        }

        internal DependencyObject GetUIParent(bool continuePastVisualTree)
        {
            return UIElementHelper.GetUIParent(this, continuePastVisualTree);
        }

        internal DependencyObject GetUIParentNo3DTraversal()
        {
            DependencyObject parent = null;

            // Try to find a UIElement parent in the visual ancestry.
            DependencyObject myParent = InternalVisualParent;
            parent = InputElement.GetContainingUIElement(myParent, true);

            return parent;
        }

        /// <summary>
        ///     Called to get the UI parent of this element when there is
        ///     no visual parent.
        /// </summary>
        /// <returns>
        ///     Returns a non-null value when some framework implementation
        ///     of this method has a non-visual parent connection,
        /// </returns>
        protected virtual internal DependencyObject GetUIParentCore()
        {
            return null;
        }

        /// <summary>
        /// Call this method to ensure that the whoel subtree of elements that includes this UIElement
        /// is properly updated.
        /// </summary>
        /// <remarks>
        /// This ensures that UIElements with IsMeasureInvalid or IsArrangeInvalid will
        /// get call to their MeasureCore and ArrangeCore, and all computed sizes will be validated.
        /// This method does nothing if layout is clean but it does work if layout is not clean so avoid calling
        /// it after each change in the element tree. It makes sense to either never call it (system will do this
        /// in a deferred manner) or only call it if you absolutely need updated sizes and positions after you do all changes.
        /// </remarks>
        public void UpdateLayout()
        {
//             VerifyAccess();
            ContextLayoutManager.From(Dispatcher).UpdateLayout();
        }

        internal static void BuildRouteHelper(DependencyObject e, EventRoute route, RoutedEventArgs args)
        {
            if (route == null)
            {
                throw new ArgumentNullException("route");
            }

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Source == null)
            {
                throw new ArgumentException(SR.Get(SRID.SourceNotSet));
            }

            if (args.RoutedEvent != route.RoutedEvent)
            {
                throw new ArgumentException(SR.Get(SRID.Mismatched_RoutedEvent));
            }

            // Route via visual tree
            if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct)
            {
                UIElement uiElement = e as UIElement;
                ContentElement contentElement = null;
                UIElement3D uiElement3D = null;

                if (uiElement == null)
                {
                    contentElement = e as ContentElement;

                    if (contentElement == null)
                    {
                        uiElement3D = e as UIElement3D;
                    }
                }

                // Add this element to route
                if (uiElement != null)
                {
                    uiElement.AddToEventRoute(route, args);
                }
                else if (contentElement != null)
                {
                    contentElement.AddToEventRoute(route, args);
                }
                else if (uiElement3D != null)
                {
                    uiElement3D.AddToEventRoute(route, args);
                }
            }
            else
            {
                int cElements = 0;

                while (e != null)
                {
                    UIElement uiElement = e as UIElement;
                    ContentElement contentElement = null;
                    UIElement3D uiElement3D = null;

                    if (uiElement == null)
                    {
                        contentElement = e as ContentElement;

                        if (contentElement == null)
                        {
                            uiElement3D = e as UIElement3D;
                        }
                    }

                    // Protect against infinite loops by limiting the number of elements
                    // that we will process.
                    if (cElements++ > MAX_ELEMENTS_IN_ROUTE)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.TreeLoop));
                    }

                    // Allow the element to adjust source
                    object newSource = null;
                    if (uiElement != null)
                    {
                        newSource = uiElement.AdjustEventSource(args);
                    }
                    else if (contentElement != null)
                    {
                        newSource = contentElement.AdjustEventSource(args);
                    }
                    else if (uiElement3D != null)
                    {
                        newSource = uiElement3D.AdjustEventSource(args);
                    }

                    // Add changed source information to the route
                    if (newSource != null)
                    {
                        route.AddSource(newSource);
                    }

                    // Invoke BuildRouteCore
                    bool continuePastVisualTree = false;
                                        if (uiElement != null)
                    {
                        //Add a Synchronized input pre-opportunity handler just before the class and instance handlers
                        uiElement.AddSynchronizedInputPreOpportunityHandler(route, args);

                        continuePastVisualTree = uiElement.BuildRouteCore(route, args);

                        // Add this element to route
                        uiElement.AddToEventRoute(route, args);

                        //Add a Synchronized input post-opportunity handler just after class and instance handlers
                        uiElement.AddSynchronizedInputPostOpportunityHandler(route, args);

                        // Get element's visual parent
                        e = uiElement.GetUIParent(continuePastVisualTree);
                    }
                    else if (contentElement != null)
                    {
                        //Add a Synchronized input pre-opportunity handler just before the class and instance handlers
                        contentElement.AddSynchronizedInputPreOpportunityHandler(route, args);

                        continuePastVisualTree = contentElement.BuildRouteCore(route, args);

                        // Add this element to route
                        contentElement.AddToEventRoute(route, args);

                        //Add a Synchronized input post-opportunity handler just after the class and instance handlers
                        contentElement.AddSynchronizedInputPostOpportunityHandler(route, args);

                        // Get element's visual parent
                        e = (DependencyObject)contentElement.GetUIParent(continuePastVisualTree);
                    }
                    else if (uiElement3D != null)
                    {
                        //Add a Synchronized input pre-opportunity handler just before the class and instance handlers
                        uiElement3D.AddSynchronizedInputPreOpportunityHandler(route, args);

                        continuePastVisualTree = uiElement3D.BuildRouteCore(route, args);

                        // Add this element to route
                        uiElement3D.AddToEventRoute(route, args);

                        //Add a Synchronized input post-opportunity handler just after the class and instance handlers
                        uiElement3D.AddSynchronizedInputPostOpportunityHandler(route, args);

                        // Get element's visual parent
                        e = uiElement3D.GetUIParent(continuePastVisualTree);
                    }

                    // If the BuildRouteCore implementation changed the
                    // args.Source to the route parent, respect it in
                    // the actual route.
                    if (e == args.Source)
                    {
                        route.AddSource(e);
                    }
                }
            }
        }

        // If this element is currently listening to synchronized input, add a pre-opportunity handler to keep track of event routed through this element.
        internal void AddSynchronizedInputPreOpportunityHandler(EventRoute route, RoutedEventArgs args)
        {
            if (InputManager.IsSynchronizedInput)
            {
                if (SynchronizedInputHelper.IsListening(this, args))
                {
                    RoutedEventHandler eventHandler = new RoutedEventHandler(this.SynchronizedInputPreOpportunityHandler);
                    SynchronizedInputHelper.AddHandlerToRoute(this, route, eventHandler, false);
                }
                else
                {
                    AddSynchronizedInputPreOpportunityHandlerCore(route, args);
                }
            }
        }

        // Helper to add pre-opportunity handler for templated parent of this element in case parent is listening
        // for synchronized input.
        internal virtual void AddSynchronizedInputPreOpportunityHandlerCore(EventRoute route, RoutedEventArgs args)
        {

        }

        // If this element is currently listening to synchronized input, add a handler to post process the synchronized input otherwise
        // add a synchronized input pre-opportunity handler from parent if parent is listening.
        internal void AddSynchronizedInputPostOpportunityHandler(EventRoute route, RoutedEventArgs args)
        {
            if (InputManager.IsSynchronizedInput)
            {
                if (SynchronizedInputHelper.IsListening(this, args))
                {
                    RoutedEventHandler eventHandler = new RoutedEventHandler(this.SynchronizedInputPostOpportunityHandler);
                    SynchronizedInputHelper.AddHandlerToRoute(this, route, eventHandler, true);
                }
                else
                {
                    // Add a preview handler from the parent.
                    SynchronizedInputHelper.AddParentPreOpportunityHandler(this, route, args);
                }
            }
        }

        // This event handler to be called before all the class & instance handlers for this element.
        internal void SynchronizedInputPreOpportunityHandler(object sender, RoutedEventArgs args)
        {
            SynchronizedInputHelper.PreOpportunityHandler(sender, args);
        }

        // This event handler to be called after class & instance handlers for this element.
        internal void SynchronizedInputPostOpportunityHandler(object sender, RoutedEventArgs args)
        {
            if (args.Handled && (InputManager.SynchronizedInputState == SynchronizedInputStates.HadOpportunity))
            {
                SynchronizedInputHelper.PostOpportunityHandler(sender, args);
            }
        }

        // Called by automation peer, when called this element will be the listening element for synchronized input.
        internal bool StartListeningSynchronizedInput(SynchronizedInputType inputType)
        {
            if(InputManager.IsSynchronizedInput)
            {
                return false;
            }
            else
            {
                InputManager.StartListeningSynchronizedInput(this, inputType);
                return true;
            }
        }

        // When called, input processing will return to normal mode.
        internal void CancelSynchronizedInput()
        {
            InputManager.CancelSynchronizedInput();
        }

        /// <summary>
        ///     Adds a handler for the given attached event
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static void AddHandler(DependencyObject d, RoutedEvent routedEvent, Delegate handler)
        {
            if (d == null)
            {
                throw new ArgumentNullException("d");
            }

            Debug.Assert(routedEvent != null, "RoutedEvent must not be null");

            UIElement uiElement = d as UIElement;
            if (uiElement != null)
            {
                uiElement.AddHandler(routedEvent, handler);
            }
            else
            {
                ContentElement contentElement = d as ContentElement;
                if (contentElement != null)
                {
                    contentElement.AddHandler(routedEvent, handler);
                }
                else
                {
                    UIElement3D uiElement3D = d as UIElement3D;
                    if (uiElement3D != null)
                    {
                        uiElement3D.AddHandler(routedEvent, handler);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.Invalid_IInputElement, d.GetType()));
                    }
                }
            }
        }

        /// <summary>
        ///     Removes a handler for the given attached event
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static void RemoveHandler(DependencyObject d, RoutedEvent routedEvent, Delegate handler)
        {
            if (d == null)
            {
                throw new ArgumentNullException("d");
            }

            Debug.Assert(routedEvent != null, "RoutedEvent must not be null");

            UIElement uiElement = d as UIElement;
            if (uiElement != null)
            {
                uiElement.RemoveHandler(routedEvent, handler);
            }
            else
            {
                ContentElement contentElement = d as ContentElement;
                if (contentElement != null)
                {
                    contentElement.RemoveHandler(routedEvent, handler);
                }
                else
                {
                    UIElement3D uiElement3D = d as UIElement3D;
                    if (uiElement3D != null)
                    {
                        uiElement3D.RemoveHandler(routedEvent, handler);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.Invalid_IInputElement, d.GetType()));
                    }
                }
            }
        }

        #region LoadedAndUnloadedEvents

        ///<summary>
        ///     Initiate the processing for [Un]Loaded event broadcast starting at this node
        /// </summary>
        internal virtual void OnPresentationSourceChanged(bool attached)
        {
            // Reset the FocusedElementProperty in order to get LostFocus event
            if (!attached && FocusManager.GetFocusedElement(this)!=null)
                FocusManager.SetFocusedElement(this, null);
        }

        #endregion LoadedAndUnloadedEvents

        /// <summary>
        ///     Translates a point relative to this element to coordinates that
        ///     are relative to the specified element.
        /// </summary>
        /// <remarks>
        ///     Passing null indicates that coordinates should be relative to
        ///     the root element in the tree that this element belongs to.
        /// </remarks>
        public Point TranslatePoint(Point point, UIElement relativeTo)
        {
            return InputElement.TranslatePoint(point, this, relativeTo);
        }

        /// <summary>
        ///     Returns the input element within this element that is
        ///     at the specified coordinates relative to this element.
        /// </summary>
        public IInputElement InputHitTest(Point point)
        {
            IInputElement enabledHit;
            IInputElement rawHit;
            InputHitTest(point, out enabledHit, out rawHit);

            return enabledHit;
        }

        /// <summary>
        ///     Returns the input element within this element that is
        ///     at the specified coordinates relative to this element.
        /// </summary>
        /// <param name="pt">
        ///     This is the coordinate, relative to this element, at which
        ///     to look for elements within this one.
        /// </param>
        /// <param name="enabledHit">
        ///     This element is the deepest enabled input element that is at the
        ///     specified coordinates.
        /// </param>
        /// <param name="rawHit">
        ///     This element is the deepest input element (not necessarily enabled)
        ///     that is at the specified coordinates.
        /// </param>
        internal void InputHitTest(Point pt, out IInputElement enabledHit, out IInputElement rawHit)
        {
            HitTestResult rawHitResult;
            InputHitTest(pt, out enabledHit, out rawHit, out rawHitResult);
        }

        /// <summary>
        ///     Returns the input element within this element that is
        ///     at the specified coordinates relative to this element.
        /// </summary>
        /// <param name="pt">
        ///     This is the coordinate, relative to this element, at which
        ///     to look for elements within this one.
        /// </param>
        /// <param name="enabledHit">
        ///     This element is the deepest enabled input element that is at the
        ///     specified coordinates.
        /// </param>
        /// <param name="rawHit">
        ///     This element is the deepest input element (not necessarily enabled)
        ///     that is at the specified coordinates.
        /// </param>
        /// <param name="rawHitResult">
        ///     Visual hit test result for the rawHit element
        /// </param>
        internal void InputHitTest(Point pt, out IInputElement enabledHit, out IInputElement rawHit, out HitTestResult rawHitResult)
        {
            PointHitTestParameters hitTestParameters = new PointHitTestParameters(pt);

            // We store the result of the hit testing here.  Note that the
            // HitTestResultCallback is an instance method on this class
            // so that it can store the element we hit.
            InputHitTestResult result = new InputHitTestResult();
            VisualTreeHelper.HitTest(this,
                                     new HitTestFilterCallback(InputHitTestFilterCallback),
                                     new HitTestResultCallback(result.InputHitTestResultCallback),
                                     hitTestParameters);

            DependencyObject candidate = result.Result;
            rawHit = candidate as IInputElement;
            rawHitResult = result.HitTestResult;
            enabledHit = null;
            while (candidate != null)
            {
                IContentHost contentHost = candidate as IContentHost;
                if (contentHost != null)
                {
                    // Do not call IContentHost.InputHitTest if the containing UIElement
                    // is not enabled.
                    DependencyObject containingElement = InputElement.GetContainingUIElement(candidate);

                    if ((bool)containingElement.GetValue(IsEnabledProperty))
                    {
                        pt = InputElement.TranslatePoint(pt, this, candidate);
                        enabledHit = rawHit = contentHost.InputHitTest(pt);
                        rawHitResult = null;
                        if (enabledHit != null)
                        {
                            break;
                        }
                    }
                }

                UIElement element = candidate as UIElement;
                if (element != null)
                {
                    if (rawHit == null)
                    {
                        // Earlier we hit a non-IInputElement. This is the first one
                        // we've found, so use that as rawHit.
                        rawHit = element;
                        rawHitResult = null;
                    }
                    if (element.IsEnabled)
                    {
                        enabledHit = element;
                        break;
                    }
                }

                UIElement3D element3D = candidate as UIElement3D;
                if (element3D != null)
                {
                    if (rawHit == null)
                    {
                        // Earlier we hit a non-IInputElement. This is the first one
                        // we've found, so use that as rawHit.
                        rawHit = element3D;
                        rawHitResult = null;
                    }
                    if (element3D.IsEnabled)
                    {
                        enabledHit = element3D;
                        break;
                    }
                }

                if (candidate == this)
                {
                    // We are at the element where the hit-test was initiated.
                    // If we haven't found the hit element by now, we missed
                    // everything.
                    break;
                }


                candidate = VisualTreeHelper.GetParentInternal(candidate);
            }
        }

        private HitTestFilterBehavior InputHitTestFilterCallback(DependencyObject currentNode)
        {
            HitTestFilterBehavior behavior = HitTestFilterBehavior.Continue;

            if(UIElementHelper.IsUIElementOrUIElement3D(currentNode))
            {
                if(!UIElementHelper.IsVisible(currentNode))
                {
                    // The element we are currently processing is not visible,
                    // so we do not allow hit testing to continue down this
                    // subtree.
                    behavior = HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                }
                if(!UIElementHelper.IsHitTestVisible(currentNode))
                {
                    // The element we are currently processing is not visible for hit testing,
                    // so we do not allow hit testing to continue down this
                    // subtree.
                    behavior = HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                }
            }
            else
            {
                // This is just a raw Visual, so it cannot receive input.
                // We allow the hit testing to continue through this visual.
                //
                // When we report the final input, we will return the containing
                // UIElement.
                behavior = HitTestFilterBehavior.Continue;
            }

            return behavior;
        }

        private class InputHitTestResult
        {
            public HitTestResultBehavior InputHitTestResultCallback(HitTestResult result)
            {
                _result = result;
                return HitTestResultBehavior.Stop;
            }


            public DependencyObject Result
            {
                get
                {
                    return _result != null ? _result.VisualHit : null;
                }
            }

            public HitTestResult HitTestResult
            {
                get
                {
                    return _result;
                }
            }

            private HitTestResult _result;
        }

        //
        // Left/Right Mouse Button Cracking Routines:
        //

        private static RoutedEvent CrackMouseButtonEvent(MouseButtonEventArgs e)
        {
            RoutedEvent newEvent = null;

            switch(e.ChangedButton)
            {
                case MouseButton.Left:
                    if(e.RoutedEvent == Mouse.PreviewMouseDownEvent)
                        newEvent = UIElement.PreviewMouseLeftButtonDownEvent;
                    else if(e.RoutedEvent == Mouse.MouseDownEvent)
                        newEvent = UIElement.MouseLeftButtonDownEvent;
                    else if(e.RoutedEvent == Mouse.PreviewMouseUpEvent)
                        newEvent = UIElement.PreviewMouseLeftButtonUpEvent;
                    else
                        newEvent = UIElement.MouseLeftButtonUpEvent;
                    break;
                case MouseButton.Right:
                    if(e.RoutedEvent == Mouse.PreviewMouseDownEvent)
                        newEvent = UIElement.PreviewMouseRightButtonDownEvent;
                    else if(e.RoutedEvent == Mouse.MouseDownEvent)
                        newEvent = UIElement.MouseRightButtonDownEvent;
                    else if(e.RoutedEvent == Mouse.PreviewMouseUpEvent)
                        newEvent = UIElement.PreviewMouseRightButtonUpEvent;
                    else
                        newEvent = UIElement.MouseRightButtonUpEvent;
                    break;
                default:
                    // No wrappers exposed for the other buttons.
                    break;
            }
            return ( newEvent );
        }

        private static void CrackMouseButtonEventAndReRaiseEvent(DependencyObject sender, MouseButtonEventArgs e)
        {
            RoutedEvent newEvent = CrackMouseButtonEvent(e);

            if (newEvent != null)
            {
                ReRaiseEventAs(sender, e, newEvent);
            }
        }

        /// <summary>
        ///     Re-raises an event with as a different RoutedEvent.
        /// </summary>
        /// <remarks>
        ///     Only used internally.  Added to support cracking generic MouseButtonDown/Up events
        ///     into MouseLeft/RightButtonDown/Up events.
        /// </remarks>
        /// <param name="args">
        ///     RoutedEventsArgs to re-raise with a new RoutedEvent
        /// </param>
        /// <param name="newEvent">
        ///     The new RoutedEvent to be associated with the RoutedEventArgs
        /// </param>
        private static void ReRaiseEventAs(DependencyObject sender, RoutedEventArgs args, RoutedEvent newEvent)
        {
            // Preseve and change the RoutedEvent
            RoutedEvent preservedRoutedEvent = args.RoutedEvent;
            args.OverrideRoutedEvent( newEvent );

            // Preserve Source
            object preservedSource = args.Source;

            EventRoute route = EventRouteFactory.FetchObject(args.RoutedEvent);

            if( TraceRoutedEvent.IsEnabled )
            {
                TraceRoutedEvent.Trace(
                    TraceEventType.Start,
                    TraceRoutedEvent.ReRaiseEventAs,
                    args.RoutedEvent,
                    sender,
                    args,
                    args.Handled );
            }

            try
            {
                // Build the route and invoke the handlers
                UIElement.BuildRouteHelper(sender, route, args);

                route.ReInvokeHandlers(sender, args);

                // Restore Source
                args.OverrideSource(preservedSource);

                // Restore RoutedEvent
                args.OverrideRoutedEvent(preservedRoutedEvent);

            }

            finally
            {
                if( TraceRoutedEvent.IsEnabled )
                {
                    TraceRoutedEvent.Trace(
                        TraceEventType.Stop,
                        TraceRoutedEvent.ReRaiseEventAs,
                        args.RoutedEvent,
                        sender,
                        args,
                        args.Handled );
                }
            }

            // Recycle the route object
            EventRouteFactory.RecycleObject(route);
        }

        /// <summary>
        ///     Implementation of RaiseEvent.
        ///     Called by both the trusted and non-trusted flavors of RaiseEvent.
        /// </summary>
        internal static void RaiseEventImpl(DependencyObject sender, RoutedEventArgs args)
        {
            EventRoute route = EventRouteFactory.FetchObject(args.RoutedEvent);

            if( TraceRoutedEvent.IsEnabled )
            {
                TraceRoutedEvent.Trace(
                    TraceEventType.Start,
                    TraceRoutedEvent.RaiseEvent,
                    args.RoutedEvent,
                    sender,
                    args,
                    args.Handled );
            }

            try
            {
                // Set Source
                args.Source = sender;

                UIElement.BuildRouteHelper(sender, route, args);

                route.InvokeHandlers(sender, args);

                // Reset Source to OriginalSource
                args.Source = args.OriginalSource;
            }

            finally
            {
                if( TraceRoutedEvent.IsEnabled )
                {
                    TraceRoutedEvent.Trace(
                        TraceEventType.Stop,
                        TraceRoutedEvent.RaiseEvent,
                        args.RoutedEvent,
                        sender,
                        args,
                        args.Handled );
                }
            }

            EventRouteFactory.RecycleObject(route);
        }

        /// <summary>
        ///     A property indicating if the mouse is over this element or not.
        /// </summary>
        public bool IsMouseDirectlyOver
        {
            get
            {
                // We do not return the cached value of reverse-inherited seed properties.
                //
                // The cached value is only used internally to detect a "change".
                //
                // More Info:
                // The act of invalidating the seed property of a reverse-inherited property
                // on the first side of the path causes the invalidation of the
                // reverse-inherited properties on both sides.  The input system has not yet
                // invalidated the seed property on the second side, so its cached value can
                // be incorrect.
                //
                return IsMouseDirectlyOver_ComputeValue();
            }
        }

        private bool IsMouseDirectlyOver_ComputeValue()
        {
            return (Mouse.DirectlyOver == this);
        }

#region new
        /// <summary>
        ///     Asynchronously re-evaluate the reverse-inherited properties.
        /// </summary>
        [FriendAccessAllowed]
        internal void SynchronizeReverseInheritPropertyFlags(DependencyObject oldParent, bool isCoreParent)
        {
            if(IsKeyboardFocusWithin)
            {
                Keyboard.PrimaryDevice.ReevaluateFocusAsync(this, oldParent, isCoreParent);
            }

            // Reevelauate the stylus properties first to guarentee that our property change
            // notifications fire before mouse properties.
            if(IsStylusOver)
            {
                StylusLogic.CurrentStylusLogicReevaluateStylusOver(this, oldParent, isCoreParent);
            }

            if(IsStylusCaptureWithin)
            {
                StylusLogic.CurrentStylusLogicReevaluateCapture(this, oldParent, isCoreParent);
            }

            if(IsMouseOver)
            {
                Mouse.PrimaryDevice.ReevaluateMouseOver(this, oldParent, isCoreParent);
            }

            if(IsMouseCaptureWithin)
            {
                Mouse.PrimaryDevice.ReevaluateCapture(this, oldParent, isCoreParent);
            }

            if (AreAnyTouchesOver)
            {
                TouchDevice.ReevaluateDirectlyOver(this, oldParent, isCoreParent);
            }

            if (AreAnyTouchesCapturedWithin)
            {
                TouchDevice.ReevaluateCapturedWithin(this, oldParent, isCoreParent);
            }
        }

        /// <summary>
        ///     Controls like popup want to control updating parent properties. This method
        ///     provides an opportunity for those controls to participate and block it.
        /// </summary>
        internal virtual bool BlockReverseInheritance()
        {
            return false;
        }

        /// <summary>
        ///     A property indicating if the mouse is over this element or not.
        /// </summary>
        public bool IsMouseOver
        {
            get
            {
                return ReadFlag(CoreFlags.IsMouseOverCache);
            }
        }

        /// <summary>
        ///     A property indicating if the stylus is over this element or not.
        /// </summary>
        public bool IsStylusOver
        {
            get
            {
                return ReadFlag(CoreFlags.IsStylusOverCache);
            }
        }

        /// <summary>
        ///     Indicates if Keyboard Focus is anywhere
        ///     within in the subtree starting at the
        ///     current instance
        /// </summary>
        public bool IsKeyboardFocusWithin
        {
            get
            {
                return ReadFlag(CoreFlags.IsKeyboardFocusWithinCache);
            }
        }

#endregion new

        /// <summary>
        ///     A property indicating if the mouse is captured to this element or not.
        /// </summary>
        public bool IsMouseCaptured
        {
            get { return (bool) GetValue(IsMouseCapturedProperty); }
        }

        /// <summary>
        ///     Captures the mouse to this element.
        /// </summary>
        public bool CaptureMouse()
        {
            return Mouse.Capture(this);
        }

        /// <summary>
        ///     Releases the mouse capture.
        /// </summary>
        public void ReleaseMouseCapture()
        {
            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }
        }

        /// <summary>
        ///     Indicates if mouse capture is anywhere within the subtree
        ///     starting at the current instance
        /// </summary>
        public bool IsMouseCaptureWithin
        {
            get
            {
                return ReadFlag(CoreFlags.IsMouseCaptureWithinCache);
            }
        }

        /// <summary>
        ///     A property indicating if the stylus is over this element or not.
        /// </summary>
        public bool IsStylusDirectlyOver
        {
            get
            {
                // We do not return the cached value of reverse-inherited seed properties.
                //
                // The cached value is only used internally to detect a "change".
                //
                // More Info:
                // The act of invalidating the seed property of a reverse-inherited property
                // on the first side of the path causes the invalidation of the
                // reverse-inherited properties on both sides.  The input system has not yet
                // invalidated the seed property on the second side, so its cached value can
                // be incorrect.
                //
                return IsStylusDirectlyOver_ComputeValue();
            }
        }

        private bool IsStylusDirectlyOver_ComputeValue()
        {
            return (Stylus.DirectlyOver == this);
        }

        /// <summary>
        ///     A property indicating if the stylus is captured to this element or not.
        /// </summary>
        public bool IsStylusCaptured
        {
            get { return (bool) GetValue(IsStylusCapturedProperty); }
        }

        /// <summary>
        ///     Captures the stylus to this element.
        /// </summary>
        public bool CaptureStylus()
        {
            return Stylus.Capture(this);
        }

        /// <summary>
        ///     Releases the stylus capture.
        /// </summary>
        public void ReleaseStylusCapture()
        {
            Stylus.Capture(null);
        }

        /// <summary>
        ///     Indicates if stylus capture is anywhere within the subtree
        ///     starting at the current instance
        /// </summary>
        public bool IsStylusCaptureWithin
        {
            get
            {
                return ReadFlag(CoreFlags.IsStylusCaptureWithinCache);
            }
        }

        /// <summary>
        ///     A property indicating if the keyboard is focused on this
        ///     element or not.
        /// </summary>
        public bool IsKeyboardFocused
        {
            get
            {
                // We do not return the cached value of reverse-inherited seed properties.
                //
                // The cached value is only used internally to detect a "change".
                //
                // More Info:
                // The act of invalidating the seed property of a reverse-inherited property
                // on the first side of the path causes the invalidation of the
                // reverse-inherited properties on both sides.  The input system has not yet
                // invalidated the seed property on the second side, so its cached value can
                // be incorrect.
                //
                return IsKeyboardFocused_ComputeValue();
            }
        }

        private bool IsKeyboardFocused_ComputeValue()
        {
            return (Keyboard.FocusedElement == this);
        }

        /// <summary>
        ///     Set a logical focus on the element. If the current keyboard focus is within the same scope move the keyboard on this element.
        /// </summary>
        public bool Focus()
        {
            if (Keyboard.Focus(this) == this)
            {
                
                // In order to show the touch keyboard we need to prompt the WinRT InputPane API.
                // We only do this when the keyboard focus has changed as the keyboard focus dictates
                // our current input targets for the touch and physical keyboards.
                TipTsfHelper.Show(this);

                // Successfully setting the keyboard focus updated the logical focus as well
                return true;
            }

            if (Focusable && IsEnabled)
            {
                // If we cannot set keyboard focus then set the logical focus only
                // Find element's FocusScope and set its FocusedElement if not already set
                // If FocusedElement is already set we don't want to steal focus for that scope
                DependencyObject focusScope = FocusManager.GetFocusScope(this);
                if (FocusManager.GetFocusedElement(focusScope) == null)
                {
                    FocusManager.SetFocusedElement(focusScope, (IInputElement)this);
                }
            }

            // Return false because current KeyboardFocus is not set on the element - only the logical focus is set
            return false;
        }

        /// <summary>
        ///     Request to move the focus from this element to another element
        /// </summary>
        /// <param name="request">Determine how to move the focus</param>
        /// <returns> Returns true if focus is moved successfully. Returns false if there is no next element</returns>
        public virtual bool MoveFocus(TraversalRequest request)
        {
            return false;
        }

        /// <summary>
        ///     Request to predict the element that should receive focus relative to this element for a
        /// given direction, without actually moving focus to it.
        /// </summary>
        /// <param name="direction">The direction for which focus should be predicted</param>
        /// <returns>
        ///     Returns the next element that focus should move to for a given FocusNavigationDirection.
        /// Returns null if focus cannot be moved relative to this element.
        /// </returns>
        public virtual DependencyObject PredictFocus(FocusNavigationDirection direction)
        {
            return null;
        }

        /// <summary>
        ///     The access key for this element was invoked. Base implementation sets focus to the element.
        /// </summary>
        /// <param name="e">The arguments to the access key event</param>
        protected virtual void OnAccessKey(AccessKeyEventArgs e)
        {
            this.Focus();
        }


        /// <summary>
        ///     A property indicating if the inptu method is enabled.
        /// </summary>
        public bool IsInputMethodEnabled
        {
            get { return (bool) GetValue(InputMethod.IsInputMethodEnabledProperty); }
        }

        /// <summary>
        ///     The Opacity property.
        /// </summary>
        public static readonly DependencyProperty OpacityProperty =
                    DependencyProperty.Register(
                                "Opacity",
                                typeof(double),
                                typeof(UIElement),
                                new UIPropertyMetadata(
                                            1.0d,
                                            new PropertyChangedCallback(Opacity_Changed)));


        private static void Opacity_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.pushOpacity();
        }

        /// <summary>
        /// Opacity applied to the rendered content of the UIElement.  When set, this opacity value
        /// is applied uniformly to the entire UIElement.
        /// </summary>
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public double Opacity
        {
            get { return (double) GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        private void pushOpacity()
        {
            if(this.Visibility == Visibility.Visible)
            {
                base.VisualOpacity = Opacity;
            }
        }

        /// <summary>
        ///     The OpacityMask property.
        /// </summary>
        public static readonly DependencyProperty OpacityMaskProperty
            = DependencyProperty.Register("OpacityMask", typeof(Brush), typeof(UIElement),
                                          new UIPropertyMetadata(new PropertyChangedCallback(OpacityMask_Changed)));

        private static void OpacityMask_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.pushOpacityMask();
        }

        /// <summary>
        /// OpacityMask applied to the rendered content of the UIElement.  When set, the alpha channel
        /// of the Brush's rendered content is applied to the rendered content of the UIElement.
        /// The other channels of the Brush's rendered content (e.g., Red, Green, or Blue) are ignored.
        /// </summary>
        public Brush OpacityMask
        {
            get { return (Brush) GetValue(OpacityMaskProperty); }
            set { SetValue(OpacityMaskProperty, value); }
        }

        private void pushOpacityMask()
        {
            base.VisualOpacityMask = OpacityMask;
        }

        /// <summary>
        ///     The BitmapEffect property.
        /// </summary>
        public static readonly DependencyProperty BitmapEffectProperty =
                DependencyProperty.Register(
                        "BitmapEffect",
                        typeof(BitmapEffect),
                        typeof(UIElement),
                        new UIPropertyMetadata(new PropertyChangedCallback(OnBitmapEffectChanged)));

        private static void OnBitmapEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;
            uie.pushBitmapEffect();
        }

        /// <summary>
        /// BitmapEffect applied to the rendered content of the UIElement.
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        public BitmapEffect BitmapEffect
        {
            get { return (BitmapEffect) GetValue(BitmapEffectProperty); }
            set { SetValue(BitmapEffectProperty, value); }
        }

        private void pushBitmapEffect()
        {
#pragma warning disable 0618
            base.VisualBitmapEffect = BitmapEffect;
#pragma warning restore 0618
        }


        /// <summary>
        ///     The Effect property.
        /// </summary>
        public static readonly DependencyProperty EffectProperty =
                DependencyProperty.Register(
                        "Effect",
                        typeof(Effect),
                        typeof(UIElement),
                        new UIPropertyMetadata(new PropertyChangedCallback(OnEffectChanged)));

        private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;
            uie.pushEffect();
        }

        /// <summary>
        /// Effect applied to the rendered content of the UIElement.
        /// </summary>
        public Effect Effect
        {
            get { return (Effect) GetValue(EffectProperty); }
            set { SetValue(EffectProperty, value); }
        }

        private void pushEffect()
        {
            base.VisualEffect = Effect;
        }

        /// <summary>
        ///     The BitmapEffectInput property.
        /// </summary>
        public static readonly DependencyProperty BitmapEffectInputProperty =
                DependencyProperty.Register(
                        "BitmapEffectInput",
                        typeof(BitmapEffectInput),
                        typeof(UIElement),
                        new UIPropertyMetadata(new PropertyChangedCallback(OnBitmapEffectInputChanged)));

        private static void OnBitmapEffectInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement) d).pushBitmapEffectInput((BitmapEffectInput) e.NewValue);
        }

        /// <summary>
        /// BitmapEffectInput accessor.
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        public BitmapEffectInput BitmapEffectInput
        {
            get { return (BitmapEffectInput) GetValue(BitmapEffectInputProperty); }
            set { SetValue(BitmapEffectInputProperty, value); }
        }


        private void pushBitmapEffectInput(BitmapEffectInput newValue)
        {
#pragma warning disable 0618
            base.VisualBitmapEffectInput = newValue;
#pragma warning restore 0618
        }

        private static void EdgeMode_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.pushEdgeMode();
        }

        private void pushEdgeMode()
        {
            base.VisualEdgeMode = RenderOptions.GetEdgeMode(this);
        }

        private static void BitmapScalingMode_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.pushBitmapScalingMode();
        }

        private void pushBitmapScalingMode()
        {
            base.VisualBitmapScalingMode = RenderOptions.GetBitmapScalingMode(this);
        }

        private static void ClearTypeHint_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.pushClearTypeHint();
        }

        private void pushClearTypeHint()
        {
            base.VisualClearTypeHint = RenderOptions.GetClearTypeHint(this);
        }

        private static void TextHintingMode_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.pushTextHintingMode();
        }

        private void pushTextHintingMode()
        {
            base.VisualTextHintingMode = TextOptionsInternal.GetTextHintingMode(this);
        }

        /// <summary>
        ///     The CacheMode property.
        /// </summary>
        public static readonly DependencyProperty CacheModeProperty =
                DependencyProperty.Register(
                        "CacheMode",
                        typeof(CacheMode),
                        typeof(UIElement),
                        new UIPropertyMetadata(new PropertyChangedCallback(OnCacheModeChanged)));

        private static void OnCacheModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;
            uie.pushCacheMode();
        }

        /// <summary>
        /// The CacheMode specifies parameters used to create a persistent cache of the UIElement and its subtree
        /// in order to increase rendering performance for content that is expensive to realize.
        /// </summary>
        public CacheMode CacheMode
        {
            get { return (CacheMode) GetValue(CacheModeProperty); }
            set { SetValue(CacheModeProperty, value); }
        }

        private void pushCacheMode()
        {
            base.VisualCacheMode = CacheMode;
        }

        /// <summary>
        /// pushVisualEffects - helper to propagate cacheMode, Opacity, OpacityMask, BitmapEffect, BitmapScalingMode and EdgeMode
        /// </summary>
        private void pushVisualEffects()
        {
            pushCacheMode();
            pushOpacity();
            pushOpacityMask();
            pushBitmapEffect();
            pushEdgeMode();
            pushBitmapScalingMode();
            pushClearTypeHint();
            pushTextHintingMode();
        }

        #region Uid
        /// <summary>
        /// Uid can be specified in xaml at any point using the xaml language attribute x:Uid.
        /// This is a long lasting (persisted in source) unique id for an element.
        /// </summary>
        static public readonly DependencyProperty UidProperty =
                    DependencyProperty.Register(
                                "Uid",
                                typeof(string),
                                typeof(UIElement),
                                new UIPropertyMetadata(String.Empty));

        /// <summary>
        /// Uid can be specified in xaml at any point using the xaml language attribute x:Uid.
        /// This is a long lasting (persisted in source) unique id for an element.
        /// </summary>
        public string Uid
        {
            get { return (string)GetValue(UidProperty); }
            set { SetValue(UidProperty, value); }
        }
        #endregion Uid

        /// <summary>
        ///     The Visibility property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty VisibilityProperty =
                DependencyProperty.Register(
                        "Visibility",
                        typeof(Visibility),
                        typeof(UIElement),
                        new PropertyMetadata(
                                VisibilityBoxes.VisibleBox,
                                new PropertyChangedCallback(OnVisibilityChanged)),
                        new ValidateValueCallback(ValidateVisibility));

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;

            Visibility newVisibility = (Visibility) e.NewValue;
            uie.VisibilityCache = newVisibility;
            uie.switchVisibilityIfNeeded(newVisibility);

            // The IsVisible property depends on this property.
            uie.UpdateIsVisibleCache();
        }

        private static bool ValidateVisibility(object o)
        {
            Visibility value = (Visibility) o;
            return (value == Visibility.Visible) || (value == Visibility.Hidden) || (value == Visibility.Collapsed);
        }

        /// <summary>
        ///     Visibility accessor
        /// </summary>
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public Visibility Visibility
        {
            get { return VisibilityCache; }
            set { SetValue(VisibilityProperty, VisibilityBoxes.Box(value)); }
        }

        private void switchVisibilityIfNeeded(Visibility visibility)
        {
            switch(visibility)
            {
                case Visibility.Visible:
                    ensureVisible();
                    break;

                case Visibility.Hidden:
                    ensureInvisible(false);
                    break;

                case Visibility.Collapsed:
                    ensureInvisible(true);
                    break;
            }
        }

        private void ensureVisible()
        {
            if(ReadFlag(CoreFlags.IsOpacitySuppressed))
            {
                //restore Opacity
                base.VisualOpacity = Opacity;

                if(ReadFlag(CoreFlags.IsCollapsed))
                {
                    WriteFlag(CoreFlags.IsCollapsed, false);

                    //invalidate parent if needed
                    signalDesiredSizeChange();

                    //we are suppressing rendering (see IsRenderable) of collapsed children (to avoid
                    //confusion when they see RenderSize=(0,0) reported for them)
                    //so now we should invalidate to re-render if some rendering props
                    //changed while UIElement was Collapsed (Arrange will cause re-rendering)
                    InvalidateVisual();
                }

                WriteFlag(CoreFlags.IsOpacitySuppressed, false);
            }
        }

        private void ensureInvisible(bool collapsed)
        {
            if(!ReadFlag(CoreFlags.IsOpacitySuppressed))
            {
                base.VisualOpacity = 0;
                WriteFlag(CoreFlags.IsOpacitySuppressed, true);
            }

            if(!ReadFlag(CoreFlags.IsCollapsed) && collapsed) //Hidden or Visible->Collapsed
            {
                WriteFlag(CoreFlags.IsCollapsed, true);

                //invalidate parent
                signalDesiredSizeChange();
            }
            else if(ReadFlag(CoreFlags.IsCollapsed) && !collapsed) //Collapsed -> Hidden
            {
                WriteFlag(CoreFlags.IsCollapsed, false);

                //invalidate parent
                signalDesiredSizeChange();
            }
        }

        private void signalDesiredSizeChange()
        {
            UIElement p;
            IContentHost ich;

            GetUIParentOrICH(out p, out ich); //only one will be returned

            if(p != null)
                p.OnChildDesiredSizeChanged(this);
            else if(ich != null)
                ich.OnChildDesiredSizeChanged(this);
        }

        private void ensureClip(Size layoutSlotSize)
        {
            Geometry clipGeometry = GetLayoutClip(layoutSlotSize);

            if(Clip != null)
            {
                if(clipGeometry == null)
                    clipGeometry = Clip;
                else
                {
                    CombinedGeometry cg = new CombinedGeometry(
                        GeometryCombineMode.Intersect,
                        clipGeometry,
                        Clip);

                    clipGeometry = cg;
                }
            }

            ChangeVisualClip(clipGeometry, true /* dontSetWhenClose */);
        }

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (_drawingContent != null)
            {
                if (_drawingContent.HitTestPoint(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            if ((_drawingContent != null) && GetHitTestBounds().IntersectsWith(hitTestParameters.Bounds))
            {
                IntersectionDetail intersectionDetail;

                intersectionDetail = _drawingContent.HitTestGeometry(hitTestParameters.InternalHitGeometry);
                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);

                if (intersectionDetail != IntersectionDetail.Empty)
                {
                    return new GeometryHitTestResult(this, intersectionDetail);
                }
            }

            return null;
        }

        /// <summary>
        /// Opens the DrawingVisual for rendering. The returned DrawingContext can be used to
        /// render into the DrawingVisual.
        /// </summary>
        [FriendAccessAllowed]
        internal DrawingContext RenderOpen()
        {
            return new VisualDrawingContext(this);
        }

        /// <summary>
        /// Called from the DrawingContext when the DrawingContext is closed.
        /// </summary>
        internal override void RenderClose(IDrawingContent newContent)
        {
            IDrawingContent oldContent = _drawingContent;

            //this element does not render - return
            if(oldContent == null && newContent == null)
                return;

            //
            // First cleanup the old content and the state associate with this node
            // related to it's content.
            //

            _drawingContent = null;

            if (oldContent != null)
            {
                //
                // Remove the notification handlers.
                //

                oldContent.PropagateChangedHandler(ContentsChangedHandler, false /* remove */);


                //
                // Disconnect the old content from this visual.
                //

                DisconnectAttachedResource(
                    VisualProxyFlags.IsContentConnected,
                    ((DUCE.IResource)oldContent));
            }


            //
            // Prepare the new content.
            //

            if (newContent != null)
            {
                // Propagate notification handlers.
                newContent.PropagateChangedHandler(ContentsChangedHandler, true /* adding */);
            }

            _drawingContent = newContent;


            //
            // Mark the visual dirty on all channels and propagate
            // the flags up the parent chain.
            //

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);

            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);
        }

        /// <summary>
        /// Overriding this function to release DUCE resources during Dispose and during removal of a subtree.
        /// </summary>
        internal override void FreeContent(DUCE.Channel channel)
        {
            Debug.Assert(_proxy.IsOnChannel(channel));

            if (_drawingContent != null)
            {
                if (CheckFlagsAnd(channel, VisualProxyFlags.IsContentConnected))
                {
                    DUCE.CompositionNode.SetContent(
                        _proxy.GetHandle(channel),
                        DUCE.ResourceHandle.Null,
                        channel);

                    ((DUCE.IResource)_drawingContent).ReleaseOnChannel(channel);

                    SetFlags(channel, false, VisualProxyFlags.IsContentConnected);
                }
            }

            // Call the base method too
            base.FreeContent(channel);
        }

        /// <summary>
        /// Returns the bounding box of the content.
        /// </summary>
        internal override Rect GetContentBounds()
        {
            if (_drawingContent != null)
            {
                Rect resultRect = Rect.Empty;
                MediaContext mediaContext = MediaContext.From(Dispatcher);
                BoundsDrawingContextWalker ctx = mediaContext.AcquireBoundsDrawingContextWalker();

                resultRect = _drawingContent.GetContentBounds(ctx);
                mediaContext.ReleaseBoundsDrawingContextWalker(ctx);

                return resultRect;
            }
            else
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// WalkContent - method which walks the content (if present) and calls out to the
        /// supplied DrawingContextWalker.
        /// </summary>
        /// <param name="walker">
        ///   DrawingContextWalker - the target of the calls which occur during
        ///   the content walk.
        /// </param>
        internal void WalkContent(DrawingContextWalker walker)
        {
            VerifyAPIReadOnly();

            if (_drawingContent != null)
            {
                _drawingContent.WalkContent(walker);
            }
        }

        /// <summary>
        /// RenderContent is implemented by derived classes to hook up their
        /// content. The implementer of this function can assert that the visual
        /// resource is valid on a channel when the function is executed.
        /// </summary>
        internal override void RenderContent(RenderContext ctx, bool isOnChannel)
        {
            DUCE.Channel channel = ctx.Channel;

            Debug.Assert(!CheckFlagsAnd(channel, VisualProxyFlags.IsContentConnected));
            Debug.Assert(_proxy.IsOnChannel(channel));


            //
            // Create the content on the channel.
            //

            if (_drawingContent != null)
            {
                DUCE.IResource drawingContent = (DUCE.IResource)_drawingContent;

                drawingContent.AddRefOnChannel(channel);

                // Hookup it up to the composition node.

                DUCE.CompositionNode.SetContent(
                    _proxy.GetHandle(channel),
                    drawingContent.GetHandle(channel),
                    channel);

                SetFlags(
                    channel,
                    true,
                    VisualProxyFlags.IsContentConnected);
            }
            else if (isOnChannel) /* _drawingContent == null */
            {
                DUCE.CompositionNode.SetContent(
                    _proxy.GetHandle(channel),
                    DUCE.ResourceHandle.Null,
                    channel);
            }
        }

        /// <summary>
        /// GetDrawing - Returns the drawing content of this Visual.
        /// </summary>
        /// <remarks>
        /// Changes to this DrawingGroup will not be propagated to the Visual's content.
        /// This method is called by both the Drawing property, and VisualTreeHelper.GetDrawing()
        /// </remarks>
        internal override DrawingGroup GetDrawing()
        {
            // Determine if Visual.Drawing should return mutable content

            VerifyAPIReadOnly();

            DrawingGroup drawingGroupContent = null;

            // Convert our content to a DrawingGroup, if content exists
            if (_drawingContent != null)
            {
                drawingGroupContent = DrawingServices.DrawingGroupFromRenderData((RenderData) _drawingContent);
            }

            return drawingGroupContent;
        }


        /// <summary>
        /// This method supplies an additional (to the <seealso cref="Clip"/> property) clip geometry
        /// that is used to intersect Clip in case if <seealso cref="ClipToBounds"/> property is set to "true".
        /// Typcally, this is a size of layout space given to the UIElement.
        /// </summary>
        /// <returns>Geometry to use as additional clip if ClipToBounds=true</returns>
        protected virtual Geometry GetLayoutClip(Size layoutSlotSize)
        {
            if(ClipToBounds)
            {
                RectangleGeometry rect = new RectangleGeometry(new Rect(RenderSize));
                rect.Freeze();
                return rect;
            }
            else
                return null;
        }

        /// <summary>
        /// ClipToBounds Property
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ClipToBoundsProperty =
                    DependencyProperty.Register(
                                "ClipToBounds",
                                typeof(bool),
                                typeof(UIElement),
                                new PropertyMetadata(
                                        BooleanBoxes.FalseBox, // default value
                                        new PropertyChangedCallback(ClipToBounds_Changed)));

        private static void ClipToBounds_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.ClipToBoundsCache = (bool) e.NewValue;

            //if never measured, then nothing to do, it should be measured at some point
            if(!uie.NeverMeasured || !uie.NeverArranged)
            {
                uie.InvalidateArrange();
            }
        }

        /// <summary>
        /// ClipToBounds Property
        /// </summary>
        /// <remarks>
        /// This property enables the content of this UIElement to be clipped by automatic Layout
        /// in order to "fit" into small space even if the content is larger.
        /// For example, if a text string is longer then available space, and Layout can not give it the
        /// "full" space to render, setting this property to "true" will ensure that the part of text string that
        /// does not fit will be automatically clipped.
        /// </remarks>
        public bool ClipToBounds
        {
            get { return ClipToBoundsCache; }
            set { SetValue(ClipToBoundsProperty, BooleanBoxes.Box(value)); }
        }


        /// <summary>
        /// Clip Property
        /// </summary>
        public static readonly DependencyProperty ClipProperty =
                    DependencyProperty.Register(
                                "Clip",
                                typeof(Geometry),
                                typeof(UIElement),
                                new PropertyMetadata(
                                            (Geometry) null,
                                            new PropertyChangedCallback(Clip_Changed)));

        private static void Clip_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;

            // if never measured, then nothing to do, it should be measured at some point
            if(!uie.NeverMeasured || !uie.NeverArranged)
            {
                uie.InvalidateArrange();
            }
        }

        /// <summary>
        /// Clip Property
        /// </summary>
        public Geometry Clip
        {
            get { return (Geometry) GetValue(ClipProperty); }
            set { SetValue(ClipProperty, value); }
        }

        /// <summary>
        /// Align Property
        /// </summary>
        public static readonly DependencyProperty SnapsToDevicePixelsProperty =
                DependencyProperty.Register(
                        "SnapsToDevicePixels",
                        typeof(bool),
                        typeof(UIElement),
                        new PropertyMetadata(
                                BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(SnapsToDevicePixels_Changed)));

        private static void SnapsToDevicePixels_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;
            uie.SnapsToDevicePixelsCache = (bool) e.NewValue;

            // if never measured, then nothing to do, it should be measured at some point
            if(!uie.NeverMeasured || !uie.NeverArranged)
            {
                uie.InvalidateArrange();
            }
        }

        /// <summary>
        /// SnapsToDevicePixels Property
        /// </summary>
        public bool SnapsToDevicePixels
        {
            get { return SnapsToDevicePixelsCache; }
            set { SetValue(SnapsToDevicePixelsProperty, value); }
        }

        /// <summary>
        ///     Indicates if the element has effectively focus.
        ///     Used by AccessKeyManager to determine starting
        ///     element in case of duplicate access keys. This
        ///     helps when the focus is delegated to some
        ///     other element in the tree.
        /// </summary>
        protected internal virtual bool HasEffectiveKeyboardFocus
        {
            get
            {
                return IsKeyboardFocused;
            }
        }

        // Internal accessor for AccessKeyManager class
        internal void InvokeAccessKey(AccessKeyEventArgs e)
        {
            OnAccessKey(e);
        }

        /// <summary>
        ///     GotFocus event
        /// </summary>
        public static readonly RoutedEvent GotFocusEvent = FocusManager.GotFocusEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     An event announcing that IsFocused changed to true.
        /// </summary>
        public event RoutedEventHandler GotFocus
        {
            add { AddHandler(GotFocusEvent, value); }
            remove { RemoveHandler(GotFocusEvent, value); }
        }

        /// <summary>
        ///     LostFocus event
        /// </summary>
        public static readonly RoutedEvent LostFocusEvent = FocusManager.LostFocusEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     An event announcing that IsFocused changed to false.
        /// </summary>
        public event RoutedEventHandler LostFocus
        {
            add { AddHandler(LostFocusEvent, value); }
            remove { RemoveHandler(LostFocusEvent, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsFocused property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsFocusedPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsFocused",
                                typeof(bool),
                                typeof(UIElement),
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox, // default value
                                            new PropertyChangedCallback(IsFocused_Changed)));

        /// <summary>
        ///     The DependencyProperty for IsFocused.
        ///     Flags:              None
        ///     Read-Only:          true
        /// </summary>
        public static readonly DependencyProperty IsFocusedProperty
            = IsFocusedPropertyKey.DependencyProperty;

        private static void IsFocused_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uiElement = ((UIElement)d);

            if ((bool) e.NewValue)
            {
                uiElement.OnGotFocus(new RoutedEventArgs(GotFocusEvent, uiElement));
            }
            else
            {
                uiElement.OnLostFocus(new RoutedEventArgs(LostFocusEvent, uiElement));
            }
        }

        /// <summary>
        ///     This method is invoked when the IsFocused property changes to true
        /// </summary>
        /// <param name="e">RoutedEventArgs</param>
        protected virtual void OnGotFocus(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     This method is invoked when the IsFocused property changes to false
        /// </summary>
        /// <param name="e">RoutedEventArgs</param>
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Gettor for IsFocused Property
        /// </summary>
        public bool IsFocused
        {
            get { return (bool) GetValue(IsFocusedProperty); }
        }

        //*********************************************************************
        #region IsEnabled Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the IsEnabled property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty IsEnabledProperty =
                    DependencyProperty.Register(
                                "IsEnabled",
                                typeof(bool),
                                typeof(UIElement),
                                new UIPropertyMetadata(
                                            BooleanBoxes.TrueBox, // default value
                                            new PropertyChangedCallback(OnIsEnabledChanged),
                                            new CoerceValueCallback(CoerceIsEnabled)));


        /// <summary>
        ///     A property indicating if this element is enabled or not.
        /// </summary>
        public bool IsEnabled
        {
            get { return (bool) GetValue(IsEnabledProperty);}
            set { SetValue(IsEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     IsEnabledChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsEnabledChanged
        {
            add {EventHandlersStoreAdd(IsEnabledChangedKey, value);}
            remove {EventHandlersStoreRemove(IsEnabledChangedKey, value);}
        }
        internal static readonly EventPrivateKey IsEnabledChangedKey = new EventPrivateKey(); // Used by ContentElement

        /// <summary>
        ///     Fetches the value that IsEnabled should be coerced to.
        /// </summary>
        /// <remarks>
        ///     This method is virtual is so that controls derived from UIElement
        ///     can combine additional requirements into the coersion logic.
        ///     <P/>
        ///     It is important for anyone overriding this property to also
        ///     call CoerceValue when any of their dependencies change.
        /// </remarks>
        protected virtual bool IsEnabledCore
        {
            get
            {
                // As of 1/25/2006, the following controls override this method:
                // ButtonBase.IsEnabledCore: CanExecute
                // MenuItem.IsEnabledCore:   CanExecute
                // ScrollBar.IsEnabledCore:  _canScroll
                return true;
            }
        }

        private static object CoerceIsEnabled(DependencyObject d, object value)
        {
            UIElement uie = (UIElement) d;

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.
            if((bool) value)
            {
                // Our parent can constrain us.  We can be plugged into either
                // a "visual" or "content" tree.  If we are plugged into a
                // "content" tree, the visual tree is just considered a
                // visual representation, and is normally composed of raw
                // visuals, not UIElements, so we prefer the content tree.
                //
                // The content tree uses the "logical" links.  But not all
                // "logical" links lead to a content tree.
                //
                DependencyObject parent = uie.GetUIParentCore() as ContentElement;
                if(parent == null)
                {
                    parent = InputElement.GetContainingUIElement(uie._parent);
                }

                if(parent == null || (bool)parent.GetValue(IsEnabledProperty))
                {
                    return BooleanBoxes.Box(uie.IsEnabledCore);
                }
                else
                {
                    return BooleanBoxes.FalseBox;
                }
            }
            else
            {
                return BooleanBoxes.FalseBox;
            }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(IsEnabledChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            uie.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();

            //Notify Automation in case it is interested.
            AutomationPeer peer = uie.GetAutomationPeer();
            if(peer != null)
                peer.InvalidatePeer();

        }


        //*********************************************************************
        #endregion IsEnabled Property
        //*********************************************************************

        //*********************************************************************
        #region IsHitTestVisible Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the IsHitTestVisible property.
        /// </summary>
        public static readonly DependencyProperty IsHitTestVisibleProperty =
                    DependencyProperty.Register(
                                "IsHitTestVisible",
                                typeof(bool),
                                typeof(UIElement),
                                new UIPropertyMetadata(
                                            BooleanBoxes.TrueBox, // default value
                                            new PropertyChangedCallback(OnIsHitTestVisibleChanged),
                                            new CoerceValueCallback(CoerceIsHitTestVisible)));

        /// <summary>
        ///     A property indicating if this element is hit test visible or not.
        /// </summary>
        public bool IsHitTestVisible
        {
            get { return (bool) GetValue(IsHitTestVisibleProperty); }
            set { SetValue(IsHitTestVisibleProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     IsHitTestVisibleChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsHitTestVisibleChanged
        {
            add {EventHandlersStoreAdd(IsHitTestVisibleChangedKey, value);}
            remove {EventHandlersStoreRemove(IsHitTestVisibleChangedKey, value);}
        }
        internal static readonly EventPrivateKey IsHitTestVisibleChangedKey = new EventPrivateKey(); // Used by ContentElement

        private static object CoerceIsHitTestVisible(DependencyObject d, object value)
        {
            UIElement uie = (UIElement) d;

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.
            if((bool) value)
            {
                // Our parent can constrain us.  We can be plugged into either
                // a "visual" or "content" tree.  If we are plugged into a
                // "content" tree, the visual tree is just considered a
                // visual representation, and is normally composed of raw
                // visuals, not UIElements, so we prefer the content tree.
                //
                // The content tree uses the "logical" links.  But not all
                // "logical" links lead to a content tree.
                //
                // However, ContentElements don't understand IsHitTestVisible,
                // so we ignore them.
                //
                DependencyObject parent = InputElement.GetContainingUIElement(uie._parent);

                if (parent == null || UIElementHelper.IsHitTestVisible(parent))
                {
                    return BooleanBoxes.TrueBox;
                }
                else
                {
                    return BooleanBoxes.FalseBox;
                }
            }
            else
            {
                return BooleanBoxes.FalseBox;
            }
        }

        private static void OnIsHitTestVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement)d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(IsHitTestVisibleChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            uie.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();
        }


        //*********************************************************************
        #endregion IsHitTestVisible Property
        //*********************************************************************

        //*********************************************************************
        #region IsVisible Property
        //*********************************************************************

        // The IsVisible property is a read-only reflection of the Visibility
        // property.
        private static PropertyMetadata _isVisibleMetadata = new ReadOnlyPropertyMetadata(BooleanBoxes.FalseBox,
                                                                                          new GetReadOnlyValueCallback(GetIsVisible),
                                                                                          new PropertyChangedCallback(OnIsVisibleChanged));

        internal static readonly DependencyPropertyKey IsVisiblePropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsVisible",
                                typeof(bool),
                                typeof(UIElement),
                                _isVisibleMetadata);

        /// <summary>
        ///     The DependencyProperty for the IsVisible property.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty = IsVisiblePropertyKey.DependencyProperty;

        /// <summary>
        ///     A property indicating if this element is Visible or not.
        /// </summary>
        public bool IsVisible
        {
            get { return ReadFlag(CoreFlags.IsVisibleCache); }
        }
        private static object GetIsVisible(DependencyObject d, out BaseValueSourceInternal source)
        {
            source = BaseValueSourceInternal.Local;
            return ((UIElement)d).IsVisible ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox;
        }

        /// <summary>
        ///     IsVisibleChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsVisibleChanged
        {
            add {EventHandlersStoreAdd(IsVisibleChangedKey, value);}
            remove {EventHandlersStoreRemove(IsVisibleChangedKey, value);}
        }
        internal static readonly EventPrivateKey IsVisibleChangedKey = new EventPrivateKey(); // Used by ContentElement

        internal void UpdateIsVisibleCache() // Called from PresentationSource
        {
            // IsVisible is a read-only property.  It derives its "base" value
            // from the Visibility property.
            bool isVisible = (Visibility == Visibility.Visible);

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.
            if(isVisible)
            {
                bool constraintAllowsVisible = false;

                // Our parent can constrain us.  We can be plugged into either
                // a "visual" or "content" tree.  If we are plugged into a
                // "content" tree, the visual tree is just considered a
                // visual representation, and is normally composed of raw
                // visuals, not UIElements, so we prefer the content tree.
                //
                // The content tree uses the "logical" links.  But not all
                // "logical" links lead to a content tree.
                //
                // However, ContentElements don't understand IsVisible,
                // so we ignore them.
                //
                DependencyObject parent = InputElement.GetContainingUIElement(_parent);

                if(parent != null)
                {
                    constraintAllowsVisible = UIElementHelper.IsVisible(parent);
                }
                else
                {
                    // We cannot be visible if we have no visual parent, unless:
                    // 1) We are the root, connected to a PresentationHost.
                    PresentationSource presentationSource = PresentationSource.CriticalFromVisual(this);
                    if(presentationSource != null)
                    {
                        constraintAllowsVisible = true;
                    }
                    else
                    {
                        // What if We are the root of a VisualBrush?  How can we tell?
                    }

                }

                if(!constraintAllowsVisible)
                {
                    isVisible = false;
                }
            }

            if(isVisible != IsVisible)
            {
                // Our IsVisible force-inherited property has changed.  Update our
                // cache and raise a change notification.

                WriteFlag(CoreFlags.IsVisibleCache, isVisible);
                NotifyPropertyChange(new DependencyPropertyChangedEventArgs(IsVisibleProperty, _isVisibleMetadata, BooleanBoxes.Box(!isVisible), BooleanBoxes.Box(isVisible)));
            }
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(IsVisibleChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            uie.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();
        }

        //*********************************************************************
        #endregion IsVisible Property
        //*********************************************************************

        //*********************************************************************
        #region Focusable Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the Focusable property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FocusableProperty =
                DependencyProperty.Register(
                        "Focusable",
                        typeof(bool),
                        typeof(UIElement),
                        new UIPropertyMetadata(
                                BooleanBoxes.FalseBox, // default value
                                new PropertyChangedCallback(OnFocusableChanged)));

        /// <summary>
        ///     Gettor and Settor for Focusable Property
        /// </summary>
        public bool Focusable
        {
            get { return (bool) GetValue(FocusableProperty); }
            set { SetValue(FocusableProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     FocusableChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler FocusableChanged
        {
            add {EventHandlersStoreAdd(FocusableChangedKey, value);}
            remove {EventHandlersStoreRemove(FocusableChangedKey, value);}
        }
        internal static readonly EventPrivateKey FocusableChangedKey = new EventPrivateKey(); // Used by ContentElement

        private static void OnFocusableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = (UIElement) d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(FocusableChangedKey, e);
        }

        //*********************************************************************
        #endregion Focusable Property
        //*********************************************************************

        /// <summary>
        /// Called by the Automation infrastructure when AutomationPeer
        /// is requested for this element. The element can return null or
        /// the instance of AutomationPeer-derived clas, if it supports UI Automation
        /// </summary>
        protected virtual AutomationPeer OnCreateAutomationPeer()
        {
            if (!AccessibilitySwitches.ItemsControlDoesNotSupportAutomation)
            {
                AutomationNotSupportedByDefaultField.SetValue(this, true);
            }
            return null;
        }

        // work around (ItemsControl.GroupStyle doesn't show items in groups in the UIAutomation tree)
        internal virtual AutomationPeer OnCreateAutomationPeerInternal() { return null; }

        /// <summary>
        /// Called by the Automation infrastructure or Control author
        /// to make sure the AutomationPeer is created. The element may
        /// create AP or return null, depending on OnCreateAutomationPeer override.
        /// </summary>
        internal AutomationPeer CreateAutomationPeer()
        {
            VerifyAccess(); //this will ensure the AP is created in the right context

            AutomationPeer ap = null;

            if(HasAutomationPeer)
            {
                ap = AutomationPeerField.GetValue(this);
            }
            else
            {
                if (!AccessibilitySwitches.ItemsControlDoesNotSupportAutomation)
                {
                    // work around (ItemsControl.GroupStyle doesn't show items in groups in the UIAutomation tree)
                    AutomationNotSupportedByDefaultField.ClearValue(this);
                    ap = OnCreateAutomationPeer();

                    // if this element returns an explicit peer (even null), use
                    // it.  But if it returns null by reaching the default method
                    // above, give it a second chance to create a peer.
                    // [This whole dance, including the UncommonField, would be
                    // unnecessary once ItemsControl implements its own override
                    // of OnCreateAutomationPeer.]
                    if (ap == null && !AutomationNotSupportedByDefaultField.GetValue(this))
                    {
                        ap = OnCreateAutomationPeerInternal();
                    }
                }
                else
                {
                    ap = OnCreateAutomationPeer();
                }

                if(ap != null)
                {
                    AutomationPeerField.SetValue(this, ap);
                    HasAutomationPeer = true;
                }
            }
            return ap;
        }

        /// <summary>
        /// Returns AutomationPeer if one exists.
        /// The AutomationPeer may not exist if not yet created by Automation infrastructure
        /// or if this element is not supposed to have one.
        /// </summary>
        internal AutomationPeer GetAutomationPeer()
        {
            VerifyAccess();

            if(HasAutomationPeer)
                return AutomationPeerField.GetValue(this);

            return null;
        }

        /// <summary>
        /// Called by the Automation infrastructure only in the case when the UIElement does not have a specific
        /// peer (does not override OnCreateAutomationPeer) but we still want some generic peer to be created and cached.
        /// For example, this is needed when HwndTarget contains a Panel and 2 Buttons underneath - the Panel
        /// normally does not have a peer so only one of the Buttons is visible in the tree.
        /// </summary>
        internal AutomationPeer CreateGenericRootAutomationPeer()
        {
            VerifyAccess(); //this will ensure the AP is created in the right context

            AutomationPeer ap = null;

            // If some peer was already created, specific or generic - use it.
            if(HasAutomationPeer)
            {
                ap = AutomationPeerField.GetValue(this);
            }
            else
            {
                ap = new GenericRootAutomationPeer(this);

                AutomationPeerField.SetValue(this, ap);
                HasAutomationPeer = true;
            }

            return ap;
        }


        /// <summary>
        /// This is used by the parser and journaling to uniquely identify a given element
        /// in a deterministic fashion, i.e., each time the same XAML/BAML is parsed/read,
        /// the items will be given the same PersistId.
        /// </summary>
        /// To keep PersistId from being serialized the set has been removed from the property and a separate
        /// set method has been created.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Obsolete("PersistId is an obsolete property and may be removed in a future release.  The value of this property is not defined.")]
        public int PersistId
        {
            get { return _persistId; }
        }

        /// <summary>
        /// This is used by the parser and journaling to uniquely identify a given element
        /// in a deterministic fashion, i.e., each time the same XAML/BAML is parsed/read,
        /// the items will be given the same PersistId.
        /// </summary>
        /// <param name="value"></param>
        /// To keep PersistId from being serialized the set has been removed from the property and a separate
        /// set method has been created.
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal void SetPersistId(int value)
        {
            _persistId = value;
        }

        // Helper method to retrieve and fire Clr Event handlers for DependencyPropertyChanged event
        private void RaiseDependencyPropertyChanged(EventPrivateKey key, DependencyPropertyChangedEventArgs args)
        {
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                Delegate handler = store.Get(key);
                if (handler != null)
                {
                    ((DependencyPropertyChangedEventHandler)handler)(this, args);
                }
            }
        }

        internal Rect PreviousArrangeRect
        {
            //  called from PresentationFramework!System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot()
            [FriendAccessAllowed]
            get
            {
                return _finalRect;
            }
        }

        // Cache for the Visibility property.  Storage is in Visual._nodeProperties.
        private Visibility VisibilityCache
        {
            get
            {
                if (CheckFlagsAnd(VisualFlags.VisibilityCache_Visible))
                {
                    return Visibility.Visible;
                }
                else if (CheckFlagsAnd(VisualFlags.VisibilityCache_TakesSpace))
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            set
            {
                Debug.Assert(value == Visibility.Visible || value == Visibility.Hidden || value == Visibility.Collapsed);

                switch (value)
                {
                    case Visibility.Visible:
                        SetFlags(true,  VisualFlags.VisibilityCache_Visible);
                        SetFlags(false, VisualFlags.VisibilityCache_TakesSpace);
                        break;

                    case Visibility.Hidden:
                        SetFlags(false, VisualFlags.VisibilityCache_Visible);
                        SetFlags(true,  VisualFlags.VisibilityCache_TakesSpace);
                        break;

                    case Visibility.Collapsed:
                        SetFlags(false, VisualFlags.VisibilityCache_Visible);
                        SetFlags(false, VisualFlags.VisibilityCache_TakesSpace);
                        break;
                }
            }
        }

        #region ForceInherit property support

        // Also called by FrameworkContentElement
        internal static void SynchronizeForceInheritProperties(
            UIElement        uiElement,
            ContentElement   contentElement,
            UIElement3D      uiElement3D,
            DependencyObject parent)
        {
            if(uiElement != null || uiElement3D != null)
            {
                bool parentValue = (bool) parent.GetValue(IsEnabledProperty);
                if(!parentValue)
                {
                    // For Read/Write force-inherited properties, use the standard coersion pattern.
                    //
                    // The IsEnabled property must be coerced false if the parent is false.
                    if (uiElement != null)
                    {
                        uiElement.CoerceValue(IsEnabledProperty);
                    }
                    else
                    {
                        uiElement3D.CoerceValue(IsEnabledProperty);
                    }
                }

                parentValue = (bool) parent.GetValue(IsHitTestVisibleProperty);
                if(!parentValue)
                {
                    // For Read/Write force-inherited properties, use the standard coersion pattern.
                    //
                    // The IsHitTestVisible property must be coerced false if the parent is false.
                    if (uiElement != null)
                    {
                        uiElement.CoerceValue(IsHitTestVisibleProperty);
                    }
                    else
                    {
                        uiElement3D.CoerceValue(IsHitTestVisibleProperty);
                    }
                }

                parentValue = (bool) parent.GetValue(IsVisibleProperty);
                if(parentValue)
                {
                    // For Read-Only force-inherited properties, use a private update method.
                    //
                    // The IsVisible property can only be true if the parent is true.
                    if (uiElement != null)
                    {
                        uiElement.UpdateIsVisibleCache();
                    }
                    else
                    {
                        uiElement3D.UpdateIsVisibleCache();
                    }
                }
            }
            else if(contentElement != null)
            {
                bool parentValue = (bool) parent.GetValue(IsEnabledProperty);
                if(!parentValue)
                {
                    // The IsEnabled property must be coerced false if the parent is false.
                    contentElement.CoerceValue(IsEnabledProperty);
                }
            }
        }

        // This is called from the force-inherit property changed events.
        internal static void InvalidateForceInheritPropertyOnChildren(Visual v, DependencyProperty property)
        {
            int cChildren = v.InternalVisual2DOr3DChildrenCount;
            for (int iChild = 0; iChild < cChildren; iChild++)
            {
                DependencyObject child = v.InternalGet2DOr3DVisualChild(iChild);

                Visual vChild = child as Visual;
                if (vChild != null)
                {
                    UIElement element = vChild as UIElement;

                    if (element != null)
                    {
                        if(property == IsVisibleProperty)
                        {
                            // For Read-Only force-inherited properties, use
                            // a private update method.
                            element.UpdateIsVisibleCache();
                        }
                        else
                        {
                            // For Read/Write force-inherited properties, use
                            // the standard coersion pattern.
                            element.CoerceValue(property);
                        }
                    }
                    else
                    {
                        // We have to "walk through" non-UIElement visuals.
                        vChild.InvalidateForceInheritPropertyOnChildren(property);
                    }
                }
                else
                {
                    Visual3D v3DChild = child as Visual3D;

                    if (v3DChild != null)
                    {
                        UIElement3D element3D = v3DChild as UIElement3D;

                        if(element3D != null)
                        {
                            if(property == IsVisibleProperty)
                            {
                                // For Read-Only force-inherited properties, use
                                // a private update method.
                                element3D.UpdateIsVisibleCache();
                            }
                            else
                            {
                                // For Read/Write force-inherited properties, use
                                // the standard coersion pattern.
                                element3D.CoerceValue(property);
                            }
                        }
                        else
                        {
                            // We have to "walk through" non-UIElement visuals.
                            v3DChild.InvalidateForceInheritPropertyOnChildren(property);
                        }
                    }
                }
            }
        }

        #endregion

        #region Manipulation

        /// <summary>
        ///     The dependency property for the IsManipulationEnabled property.
        /// </summary>
        public static readonly DependencyProperty IsManipulationEnabledProperty =
            DependencyProperty.Register(
                "IsManipulationEnabled",
                typeof(bool),
                typeof(UIElement),
                new PropertyMetadata(BooleanBoxes.FalseBox, new PropertyChangedCallback(OnIsManipulationEnabledChanged)));

        /// <summary>
        ///     Whether manipulations are enabled on this element or not.
        /// </summary>
        /// <remarks>
        ///     Handle the ManipulationStarting event to customize the behavior of manipulations.
        ///     Setting to false will immediately complete any current manipulation or inertia
        ///     on this element and raise a ManipulationCompleted event.
        /// </remarks>
        [CustomCategory(SRID.Touch_Category)]
        public bool IsManipulationEnabled
        {
            get
            {
                return (bool)GetValue(IsManipulationEnabledProperty);
            }
            set
            {
                SetValue(IsManipulationEnabledProperty, value);
            }
        }

        private static void OnIsManipulationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((UIElement)d).CoerceStylusProperties();
            }
            else
            {
                Manipulation.TryCompleteManipulation((UIElement)d);
            }
        }

        private void CoerceStylusProperties()
        {
            // When manipulation is enabled, disable Stylus.IsFlicksEnabled property.
            // This property, when active, produces visible effects that don't apply
            // and are distracting during a manipulation.
            //
            // Not using coercion to avoid adding the cost of doing the callback
            // on all elements, even when they don't need it.
            if (IsDefaultValue(this, Stylus.IsFlicksEnabledProperty))
            {
                SetCurrentValueInternal(Stylus.IsFlicksEnabledProperty, BooleanBoxes.FalseBox);
            }
        }

        private static bool IsDefaultValue(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            bool hasModifiers, isExpression, isAnimated, isCoerced, isCurrent;
            BaseValueSourceInternal source = dependencyObject.GetValueSource(dependencyProperty, null, out hasModifiers, out isExpression, out isAnimated, out isCoerced, out isCurrent);
            return (source == BaseValueSourceInternal.Default) && !isExpression && !isAnimated && !isCoerced;
        }

        /// <summary>
        ///     Indicates that a manipulation is about to start and allows for configuring its behavior.
        /// </summary>
        public static readonly RoutedEvent ManipulationStartingEvent = Manipulation.ManipulationStartingEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     Indicates that a manipulation is about to start and allows for configuring its behavior.
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<ManipulationStartingEventArgs> ManipulationStarting
        {
            add { AddHandler(ManipulationStartingEvent, value, false); }
            remove { RemoveHandler(ManipulationStartingEvent, value); }
        }

        private static void OnManipulationStartingThunk(object sender, ManipulationStartingEventArgs e)
        {
            ((UIElement)sender).OnManipulationStarting(e);
        }

        /// <summary>
        ///     Indicates that a manipulation has started.
        /// </summary>
        protected virtual void OnManipulationStarting(ManipulationStartingEventArgs e) { }

        /// <summary>
        ///     Indicates that a manipulation has started.
        /// </summary>
        public static readonly RoutedEvent ManipulationStartedEvent = Manipulation.ManipulationStartedEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     Indicates that a manipulation has started.
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<ManipulationStartedEventArgs> ManipulationStarted
        {
            add { AddHandler(ManipulationStartedEvent, value, false); }
            remove { RemoveHandler(ManipulationStartedEvent, value); }
        }

        private static void OnManipulationStartedThunk(object sender, ManipulationStartedEventArgs e)
        {
            ((UIElement)sender).OnManipulationStarted(e);
        }

        /// <summary>
        ///     Indicates that a manipulation has started.
        /// </summary>
        protected virtual void OnManipulationStarted(ManipulationStartedEventArgs e) { }

        /// <summary>
        ///     Provides data regarding changes to a currently occurring manipulation.
        /// </summary>
        public static readonly RoutedEvent ManipulationDeltaEvent = Manipulation.ManipulationDeltaEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     Provides data regarding changes to a currently occurring manipulation.
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<ManipulationDeltaEventArgs> ManipulationDelta
        {
            add { AddHandler(ManipulationDeltaEvent, value, false); }
            remove { RemoveHandler(ManipulationDeltaEvent, value); }
        }

        private static void OnManipulationDeltaThunk(object sender, ManipulationDeltaEventArgs e)
        {
            ((UIElement)sender).OnManipulationDelta(e);
        }

        /// <summary>
        ///     Provides data regarding changes to a currently occurring manipulation.
        /// </summary>
        protected virtual void OnManipulationDelta(ManipulationDeltaEventArgs e) { }

        /// <summary>
        ///     Allows a handler to customize the parameters of an inertia processor.
        /// </summary>
        public static readonly RoutedEvent ManipulationInertiaStartingEvent = Manipulation.ManipulationInertiaStartingEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     Allows a handler to customize the parameters of an inertia processor.
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<ManipulationInertiaStartingEventArgs> ManipulationInertiaStarting
        {
            add { AddHandler(ManipulationInertiaStartingEvent, value, false); }
            remove { RemoveHandler(ManipulationInertiaStartingEvent, value); }
        }

        private static void OnManipulationInertiaStartingThunk(object sender, ManipulationInertiaStartingEventArgs e)
        {
            ((UIElement)sender).OnManipulationInertiaStarting(e);
        }

        /// <summary>
        ///     Allows a handler to customize the parameters of an inertia processor.
        /// </summary>
        protected virtual void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e) { }

        /// <summary>
        ///     Allows a handler to provide feedback when a manipulation has encountered a boundary.
        /// </summary>
        public static readonly RoutedEvent ManipulationBoundaryFeedbackEvent = Manipulation.ManipulationBoundaryFeedbackEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     Allows a handler to provide feedback when a manipulation has encountered a boundary.
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<ManipulationBoundaryFeedbackEventArgs> ManipulationBoundaryFeedback
        {
            add { AddHandler(ManipulationBoundaryFeedbackEvent, value, false); }
            remove { RemoveHandler(ManipulationBoundaryFeedbackEvent, value); }
        }

        private static void OnManipulationBoundaryFeedbackThunk(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            ((UIElement)sender).OnManipulationBoundaryFeedback(e);
        }

        /// <summary>
        ///     Allows a handler to provide feedback when a manipulation has encountered a boundary.
        /// </summary>
        protected virtual void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e) { }

        /// <summary>
        ///     Indicates that a manipulation has completed.
        /// </summary>
        public static readonly RoutedEvent ManipulationCompletedEvent = Manipulation.ManipulationCompletedEvent.AddOwner(typeof(UIElement));

        /// <summary>
        ///     Indicates that a manipulation has completed.
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<ManipulationCompletedEventArgs> ManipulationCompleted
        {
            add { AddHandler(ManipulationCompletedEvent, value, false); }
            remove { RemoveHandler(ManipulationCompletedEvent, value); }
        }

        private static void OnManipulationCompletedThunk(object sender, ManipulationCompletedEventArgs e)
        {
            ((UIElement)sender).OnManipulationCompleted(e);
        }

        /// <summary>
        ///     Indicates that a manipulation has completed.
        /// </summary>
        protected virtual void OnManipulationCompleted(ManipulationCompletedEventArgs e) { }

        #endregion

        #region Touch

        /// <summary>
        ///     A property indicating if any touch devices are over this element or not.
        /// </summary>
        public bool AreAnyTouchesOver
        {
            get { return ReadFlag(CoreFlags.TouchesOverCache); }
        }

        /// <summary>
        ///     A property indicating if any touch devices are directly over this element or not.
        /// </summary>
        public bool AreAnyTouchesDirectlyOver
        {
            get { return (bool)GetValue(AreAnyTouchesDirectlyOverProperty); }
        }

        /// <summary>
        ///     A property indicating if any touch devices are captured to elements in this subtree.
        /// </summary>
        public bool AreAnyTouchesCapturedWithin
        {
            get { return ReadFlag(CoreFlags.TouchesCapturedWithinCache); }
        }

        /// <summary>
        ///     A property indicating if any touch devices are captured to this element.
        /// </summary>
        public bool AreAnyTouchesCaptured
        {
            get { return (bool)GetValue(AreAnyTouchesCapturedProperty); }
        }

        /// <summary>
        ///     Captures the specified device to this element.
        /// </summary>
        /// <param name="touchDevice">The touch device to capture.</param>
        /// <returns>True if capture was taken.</returns>
        public bool CaptureTouch(TouchDevice touchDevice)
        {
            if (touchDevice == null)
            {
                throw new ArgumentNullException("touchDevice");
            }

            return touchDevice.Capture(this);
        }

        /// <summary>
        ///     Releases capture from the specified touch device.
        /// </summary>
        /// <param name="touchDevice">The device that is captured to this element.</param>
        /// <returns>true if capture was released, false otherwise.</returns>
        public bool ReleaseTouchCapture(TouchDevice touchDevice)
        {
            if (touchDevice == null)
            {
                throw new ArgumentNullException("touchDevice");
            }

            if (touchDevice.Captured == this)
            {
                touchDevice.Capture(null);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///     Releases capture on any touch devices captured to this element.
        /// </summary>
        public void ReleaseAllTouchCaptures()
        {
            TouchDevice.ReleaseAllCaptures(this);
        }

        /// <summary>
        ///     The touch devices captured to this element.
        /// </summary>
        public IEnumerable<TouchDevice> TouchesCaptured
        {
            get
            {
                return TouchDevice.GetCapturedTouches(this, /* includeWithin = */ false);
            }
        }

        /// <summary>
        ///     The touch devices captured to this element and any elements in the subtree.
        /// </summary>
        public IEnumerable<TouchDevice> TouchesCapturedWithin
        {
            get
            {
                return TouchDevice.GetCapturedTouches(this, /* includeWithin = */ true);
            }
        }

        /// <summary>
        ///     The touch devices which are over this element and any elements in the subtree.
        ///     This is particularly relevant to elements which dont take capture (like Label).
        /// </summary>
        public IEnumerable<TouchDevice> TouchesOver
        {
            get
            {
                return TouchDevice.GetTouchesOver(this, /* includeWithin = */ true);
            }
        }

        /// <summary>
        ///     The touch devices which are directly over this element.
        ///     This is particularly relevant to elements which dont take capture (like Label).
        /// </summary>
        public IEnumerable<TouchDevice> TouchesDirectlyOver
        {
            get
            {
                return TouchDevice.GetTouchesOver(this, /* includeWithin = */ false);
            }
        }

        #endregion

        ///// LAYOUT DATA /////

        private Rect _finalRect;
        private Size _desiredSize;
        private Size _previousAvailableSize;
        private IDrawingContent _drawingContent;

        //right after creation all elements are Clean so go Invalidate at least one

        internal ContextLayoutManager.LayoutQueue.Request MeasureRequest;
        internal ContextLayoutManager.LayoutQueue.Request ArrangeRequest;

        // See PersistId property
        private int _persistId = 0;

        internal static List<double> DpiScaleXValues = new List<double>(3);
        internal static List<double> DpiScaleYValues = new List<double>(3);
        internal static object DpiLock = new object();
        private static double _dpiScaleX = 1.0;
        private static double _dpiScaleY = 1.0;
        private static bool _setDpi = true;

        ///// ATTACHED STORAGE /////

        // Perf analysis showed we were not using these fields enough to warrant
        // bloating each instance with the field, so storage is created on-demand
        // in the local store.
        internal static readonly UncommonField<EventHandlersStore> EventHandlersStoreField = new UncommonField<EventHandlersStore>();
        internal static readonly UncommonField<InputBindingCollection> InputBindingCollectionField = new UncommonField<InputBindingCollection>();
        internal static readonly UncommonField<CommandBindingCollection> CommandBindingCollectionField = new UncommonField<CommandBindingCollection>();
        private  static readonly UncommonField<object> LayoutUpdatedListItemsField = new UncommonField<object>();
        private  static readonly UncommonField<EventHandler> LayoutUpdatedHandlersField = new UncommonField<EventHandler>();
        private  static readonly UncommonField<StylusPlugInCollection> StylusPlugInsField = new UncommonField<StylusPlugInCollection>();
        private  static readonly UncommonField<AutomationPeer> AutomationPeerField = new UncommonField<AutomationPeer>();
        // This field serves to link this UIElement with another UIElement better suited to provide PositionInSet and SizeOfSet Automation properties.
        private  static readonly UncommonField<WeakReference<UIElement>> _positionAndSizeOfSetController = new UncommonField<WeakReference<UIElement>>();
        private  static readonly UncommonField<bool> AutomationNotSupportedByDefaultField = new UncommonField<bool>();

        internal SizeChangedInfo sizeChangedInfo;

        /// <summary>
        /// Internal property to expose uncommon field _positionAndSizeOfSetController.
        /// </summary>
        internal UIElement PositionAndSizeOfSetController
        {
            get
            {
                UIElement element = null;
                WeakReference<UIElement> wRef = _positionAndSizeOfSetController.GetValue(this);

                wRef?.TryGetTarget(out element);

                return element;
            }
            set
            {
                if (value != null)
                {
                    _positionAndSizeOfSetController.SetValue(this, new WeakReference<UIElement>(value));
                }
                else
                {
                    _positionAndSizeOfSetController.ClearValue(this);
                }
            }
        }

        internal bool HasAutomationPeer
        {
            get { return ReadFlag(CoreFlags.HasAutomationPeer); }
            set { WriteFlag(CoreFlags.HasAutomationPeer, value); }
        }
        private bool RenderingInvalidated
        {
            get { return ReadFlag(CoreFlags.RenderingInvalidated); }
            set { WriteFlag(CoreFlags.RenderingInvalidated, value); }
        }

        internal bool SnapsToDevicePixelsCache
        {
            get { return ReadFlag(CoreFlags.SnapsToDevicePixelsCache); }
            set { WriteFlag(CoreFlags.SnapsToDevicePixelsCache, value); }
        }

        internal bool ClipToBoundsCache
        {
            get { return ReadFlag(CoreFlags.ClipToBoundsCache); }
            set { WriteFlag(CoreFlags.ClipToBoundsCache, value); }
        }

        internal bool MeasureDirty
        {
            get { return ReadFlag(CoreFlags.MeasureDirty); }
            set { WriteFlag(CoreFlags.MeasureDirty, value); }
        }

        internal bool ArrangeDirty
        {
            get { return ReadFlag(CoreFlags.ArrangeDirty); }
            set { WriteFlag(CoreFlags.ArrangeDirty, value); }
        }

        internal bool MeasureInProgress
        {
            get { return ReadFlag(CoreFlags.MeasureInProgress); }
            set { WriteFlag(CoreFlags.MeasureInProgress, value); }
        }

        internal bool ArrangeInProgress
        {
            get { return ReadFlag(CoreFlags.ArrangeInProgress); }
            set { WriteFlag(CoreFlags.ArrangeInProgress, value); }
        }

        internal bool NeverMeasured
        {
            get { return ReadFlag(CoreFlags.NeverMeasured); }
            set { WriteFlag(CoreFlags.NeverMeasured, value); }
        }

        internal bool NeverArranged
        {
            get { return ReadFlag(CoreFlags.NeverArranged); }
            set { WriteFlag(CoreFlags.NeverArranged, value); }
        }

        internal bool MeasureDuringArrange
        {
            get { return ReadFlag(CoreFlags.MeasureDuringArrange); }
            set { WriteFlag(CoreFlags.MeasureDuringArrange, value); }
        }

        internal bool AreTransformsClean
        {
            get { return ReadFlag(CoreFlags.AreTransformsClean); }
            set { WriteFlag(CoreFlags.AreTransformsClean, value); }
        }

        internal static readonly FocusWithinProperty FocusWithinProperty = new FocusWithinProperty();
        internal static readonly MouseOverProperty MouseOverProperty = new MouseOverProperty();
        internal static readonly MouseCaptureWithinProperty MouseCaptureWithinProperty = new MouseCaptureWithinProperty();
        internal static readonly StylusOverProperty StylusOverProperty = new StylusOverProperty();
        internal static readonly StylusCaptureWithinProperty StylusCaptureWithinProperty = new StylusCaptureWithinProperty();
        internal static readonly TouchesOverProperty TouchesOverProperty = new TouchesOverProperty();
        internal static readonly TouchesCapturedWithinProperty TouchesCapturedWithinProperty = new TouchesCapturedWithinProperty();
        private Size               _size;
        internal const int MAX_ELEMENTS_IN_ROUTE = 4096;
    }

    [Flags]
    internal enum CoreFlags : uint
    {
        None                            = 0x00000000,
        SnapsToDevicePixelsCache        = 0x00000001,
        ClipToBoundsCache               = 0x00000002,
        MeasureDirty                    = 0x00000004,
        ArrangeDirty                    = 0x00000008,
        MeasureInProgress               = 0x00000010,
        ArrangeInProgress               = 0x00000020,
        NeverMeasured                   = 0x00000040,
        NeverArranged                   = 0x00000080,
        MeasureDuringArrange            = 0x00000100,
        IsCollapsed                     = 0x00000200,
        IsKeyboardFocusWithinCache      = 0x00000400,
        IsKeyboardFocusWithinChanged    = 0x00000800,
        IsMouseOverCache                = 0x00001000,
        IsMouseOverChanged              = 0x00002000,
        IsMouseCaptureWithinCache       = 0x00004000,
        IsMouseCaptureWithinChanged     = 0x00008000,
        IsStylusOverCache               = 0x00010000,
        IsStylusOverChanged             = 0x00020000,
        IsStylusCaptureWithinCache      = 0x00040000,
        IsStylusCaptureWithinChanged    = 0x00080000,
        HasAutomationPeer               = 0x00100000,
        RenderingInvalidated            = 0x00200000,
        IsVisibleCache                  = 0x00400000,
        AreTransformsClean              = 0x00800000,
        IsOpacitySuppressed             = 0x01000000,
        ExistsEventHandlersStore        = 0x02000000,
        TouchesOverCache                = 0x04000000,
        TouchesOverChanged              = 0x08000000,
        TouchesCapturedWithinCache      = 0x10000000,
        TouchesCapturedWithinChanged    = 0x20000000,
        TouchLeaveCache                 = 0x40000000,
        TouchEnterCache                 = 0x80000000,
    }
}



