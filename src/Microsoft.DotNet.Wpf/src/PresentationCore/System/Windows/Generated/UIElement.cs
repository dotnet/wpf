// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Windows.Input;
using System.Windows.Media.Animation;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows
{
    partial class UIElement : IAnimatable
    {
        static private readonly Type _typeofThis = typeof(UIElement);

        #region IAnimatable

        /// <summary>
        /// Applies an AnimationClock to a DepencencyProperty which will
        /// replace the current animations on the property using the snapshot
        /// and replace HandoffBehavior.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty to animate.
        /// </param>
        /// <param name="clock">
        /// The AnimationClock that will animate the property. If this is null
        /// then all animations will be removed from the property.
        /// </param>
        public void ApplyAnimationClock(
            DependencyProperty dp,
            AnimationClock clock)
        {
            ApplyAnimationClock(dp, clock, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Applies an AnimationClock to a DependencyProperty. The effect of
        /// the new AnimationClock on any current animations will be determined by
        /// the value of the handoffBehavior parameter.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty to animate.
        /// </param>
        /// <param name="clock">
        /// The AnimationClock that will animate the property. If parameter is null
        /// then animations will be removed from the property if handoffBehavior is
        /// SnapshotAndReplace; otherwise the method call will have no result.
        /// </param>
        /// <param name="handoffBehavior">
        /// Determines how the new AnimationClock will transition from or
        /// affect any current animations on the property.
        /// </param>
        public void ApplyAnimationClock(
            DependencyProperty dp,
            AnimationClock clock,
            HandoffBehavior handoffBehavior)
        {
            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            if (!AnimationStorage.IsPropertyAnimatable(this, dp))
            {
        #pragma warning disable 56506 // Suppress presharp warning: Parameter 'dp' to this public method must be validated:  A null-dereference can occur here.
                throw new ArgumentException(SR.Get(SRID.Animation_DependencyPropertyIsNotAnimatable, dp.Name, this.GetType()), "dp");
        #pragma warning restore 56506
            }

            if (clock != null
                && !AnimationStorage.IsAnimationValid(dp, clock.Timeline))
            {
        #pragma warning disable 56506 // Suppress presharp warning: Parameter 'dp' to this public method must be validated:  A null-dereference can occur here.
                throw new ArgumentException(SR.Get(SRID.Animation_AnimationTimelineTypeMismatch, clock.Timeline.GetType(), dp.Name, dp.PropertyType), "clock");
        #pragma warning restore 56506
            }

            if (!HandoffBehaviorEnum.IsDefined(handoffBehavior))
            {
                throw new ArgumentException(SR.Get(SRID.Animation_UnrecognizedHandoffBehavior));
            }

            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.IAnimatable_CantAnimateSealedDO, dp, this.GetType()));
            }                    

            AnimationStorage.ApplyAnimationClock(this, dp, clock, handoffBehavior);
        }

        /// <summary>
        /// Starts an animation for a DependencyProperty. The animation will
        /// begin when the next frame is rendered.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty to animate.
        /// </param>
        /// <param name="animation">
        /// <para>The AnimationTimeline to used to animate the property.</para>
        /// <para>If the AnimationTimeline's BeginTime is null, any current animations
        /// will be removed and the current value of the property will be held.</para>
        /// <para>If this value is null, all animations will be removed from the property
        /// and the property value will revert back to its base value.</para>
        /// </param>
        public void BeginAnimation(DependencyProperty dp, AnimationTimeline animation)
        {
            BeginAnimation(dp, animation, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Starts an animation for a DependencyProperty. The animation will
        /// begin when the next frame is rendered.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty to animate.
        /// </param>
        /// <param name="animation">
        /// <para>The AnimationTimeline to used to animate the property.</para>
        /// <para>If the AnimationTimeline's BeginTime is null, any current animations
        /// will be removed and the current value of the property will be held.</para>
        /// <para>If this value is null, all animations will be removed from the property
        /// and the property value will revert back to its base value.</para>
        /// </param>
        /// <param name="handoffBehavior">
        /// Specifies how the new animation should interact with any current
        /// animations already affecting the property value.
        /// </param>
        public void BeginAnimation(DependencyProperty dp, AnimationTimeline animation, HandoffBehavior handoffBehavior)
        {
            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            if (!AnimationStorage.IsPropertyAnimatable(this, dp))
            {
        #pragma warning disable 56506 // Suppress presharp warning: Parameter 'dp' to this public method must be validated:  A null-dereference can occur here.
                throw new ArgumentException(SR.Get(SRID.Animation_DependencyPropertyIsNotAnimatable, dp.Name, this.GetType()), "dp");
        #pragma warning restore 56506
            }

            if (   animation != null
                && !AnimationStorage.IsAnimationValid(dp, animation))
            {
                throw new ArgumentException(SR.Get(SRID.Animation_AnimationTimelineTypeMismatch, animation.GetType(), dp.Name, dp.PropertyType), "animation");
            }

            if (!HandoffBehaviorEnum.IsDefined(handoffBehavior))
            {
                throw new ArgumentException(SR.Get(SRID.Animation_UnrecognizedHandoffBehavior));
            }

            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.IAnimatable_CantAnimateSealedDO, dp, this.GetType()));
            }                    

            AnimationStorage.BeginAnimation(this, dp, animation, handoffBehavior);
        }

        /// <summary>
        /// Returns true if any properties on this DependencyObject have a
        /// persistent animation or the object has one or more clocks associated
        /// with any of its properties.
        /// </summary>
        public bool HasAnimatedProperties
        {
            get
            {
                VerifyAccess();

                return IAnimatable_HasAnimatedProperties;
            }
        }

        /// <summary>
        ///   If the dependency property is animated this method will
        ///   give you the value as if it was not animated.
        /// </summary>
        /// <param name="dp">The DependencyProperty</param>
        /// <returns>
        ///   The value that would be returned if there were no
        ///   animations attached.  If there aren't any attached, then
        ///   the result will be the same as that returned from
        ///   GetValue.
        /// </returns>
        public object GetAnimationBaseValue(DependencyProperty dp)
        {
            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            return this.GetValueEntry(
                    LookupEntry(dp.GlobalIndex),
                    dp,
                    null,
                    RequestFlags.AnimationBaseValue).Value;
        }

        #endregion IAnimatable

        #region Animation

        /// <summary>
        ///     Allows subclasses to participate in property animated value computation
        /// </summary>
        /// <param name="dp"></param>
        /// <param name="metadata"></param>
        /// <param name="entry">EffectiveValueEntry computed by base</param>
        internal sealed override void EvaluateAnimatedValueCore(
                DependencyProperty  dp,
                PropertyMetadata    metadata,
            ref EffectiveValueEntry entry)
        {
            if (IAnimatable_HasAnimatedProperties)
            {
                AnimationStorage storage = AnimationStorage.GetStorage(this, dp);

                if (storage != null)
                {
                    storage.EvaluateAnimatedValue(metadata, ref entry);                      
                }
            }
        }

        #endregion Animation

        #region Commands
        /// <summary>
        /// Instance level InputBinding collection, initialized on first use.
        /// To have commands handled (QueryEnabled/Execute) on an element instance,
        /// the user of this method can add InputBinding with handlers thru this
        /// method.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public InputBindingCollection InputBindings
        {
            get
            {
                VerifyAccess();
                InputBindingCollection bindings = InputBindingCollectionField.GetValue(this);
                if (bindings == null)
                {
                    bindings = new InputBindingCollection(this);
                    InputBindingCollectionField.SetValue(this, bindings);
                }

                return bindings;
            }
        }

        // Used by CommandManager to avoid instantiating an empty collection
        internal InputBindingCollection InputBindingsInternal
        {
            get
            {
                VerifyAccess();
                return InputBindingCollectionField.GetValue(this);
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        // for serializer to serialize only when InputBindings is not empty
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeInputBindings()
        {
            InputBindingCollection bindingCollection = InputBindingCollectionField.GetValue(this);
            if (bindingCollection != null && bindingCollection.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Instance level CommandBinding collection, initialized on first use.
        /// To have commands handled (QueryEnabled/Execute) on an element instance,
        /// the user of this method can add CommandBinding with handlers thru this
        /// method.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public CommandBindingCollection CommandBindings
        {
            get
            {
                VerifyAccess();
                CommandBindingCollection bindings = CommandBindingCollectionField.GetValue(this);
                if (bindings == null)
                {
                    bindings = new CommandBindingCollection();
                    CommandBindingCollectionField.SetValue(this, bindings);
                }

                return bindings;
            }
        }

        // Used by CommandManager to avoid instantiating an empty collection
        internal CommandBindingCollection CommandBindingsInternal
        {
            get
            {
                VerifyAccess();
                return CommandBindingCollectionField.GetValue(this);
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        // for serializer to serialize only when CommandBindings is not empty
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeCommandBindings()
        {
            CommandBindingCollection bindingCollection = CommandBindingCollectionField.GetValue(this);
            if (bindingCollection != null && bindingCollection.Count > 0)
            {
                return true;
            }

            return false;
        }

        #endregion Commands

        #region Events

        /// <summary>
        ///     Allows UIElement to augment the
        ///     <see cref="EventRoute"/>
        /// </summary>
        /// <remarks>
        ///     Sub-classes of UIElement can override
        ///     this method to custom augment the route
        /// </remarks>
        /// <param name="route">
        ///     The <see cref="EventRoute"/> to be
        ///     augmented
        /// </param>
        /// <param name="args">
        ///     <see cref="RoutedEventArgs"/> for the
        ///     RoutedEvent to be raised post building
        ///     the route
        /// </param>
        /// <returns>
        ///     Whether or not the route should continue past the visual tree.
        ///     If this is true, and there are no more visual parents, the route
        ///     building code will call the GetUIParentCore method to find the
        ///     next non-visual parent.
        /// </returns>
        internal virtual bool BuildRouteCore(EventRoute route, RoutedEventArgs args)
        {
            return false;
        }

        /// <summary>
        ///     Builds the <see cref="EventRoute"/>
        /// </summary>
        /// <param name="route">
        ///     The <see cref="EventRoute"/> being
        ///     built
        /// </param>
        /// <param name="args">
        ///     <see cref="RoutedEventArgs"/> for the
        ///     RoutedEvent to be raised post building
        ///     the route
        /// </param>
        internal void BuildRoute(EventRoute route, RoutedEventArgs args)
        {
            UIElement.BuildRouteHelper(this, route, args);
        }

        /// <summary>
        ///     Raise the events specified by
        ///     <see cref="RoutedEventArgs.RoutedEvent"/>
        /// </summary>
        /// <remarks>
        ///     This method is a shorthand for
        ///     <see cref="UIElement.BuildRoute"/> and
        ///     <see cref="EventRoute.InvokeHandlers"/>
        /// </remarks>
        /// <param name="e">
        ///     <see cref="RoutedEventArgs"/> for the event to
        ///     be raised
        /// </param>
        public void RaiseEvent(RoutedEventArgs e)
        {
            // VerifyAccess();

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            e.ClearUserInitiated();

            UIElement.RaiseEventImpl(this, e);
        }

        /// <summary>
        ///     "Trusted" internal flavor of RaiseEvent.
        ///     Used to set the User-initated RaiseEvent.
        /// </summary>
        internal void RaiseEvent(RoutedEventArgs args, bool trusted)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (trusted)
            {
                RaiseTrustedEvent(args);
            }
            else
            {
                args.ClearUserInitiated();

                UIElement.RaiseEventImpl(this, args);
            }
        }

        internal void RaiseTrustedEvent(RoutedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            // Try/finally to ensure that UserInitiated bit is cleared.
            args.MarkAsUserInitiated();

            try
            {
                UIElement.RaiseEventImpl(this, args);
            }
            finally
            {
                // Clear the bit - just to guarantee it's not used again
                args.ClearUserInitiated();
            }
        }


        /// <summary>
        ///     Allows adjustment to the event source
        /// </summary>
        /// <remarks>
        ///     Subclasses must override this method
        ///     to be able to adjust the source during
        ///     route invocation <para/>
        ///
        ///     NOTE: Expected to return null when no
        ///     change is made to source
        /// </remarks>
        /// <param name="args">
        ///     Routed Event Args
        /// </param>
        /// <returns>
        ///     Returns new source
        /// </returns>
        internal virtual object AdjustEventSource(RoutedEventArgs args)
        {
            return null;
        }

        /// <summary>
        ///     See overloaded method for details
        /// </summary>
        /// <remarks>
        ///     handledEventsToo defaults to false <para/>
        ///     See overloaded method for details
        /// </remarks>
        /// <param name="routedEvent"/>
        /// <param name="handler"/>
        public void AddHandler(RoutedEvent routedEvent, Delegate handler)
        {
            // HandledEventToo defaults to false
            // Call forwarded
            AddHandler(routedEvent, handler, false);
        }

        /// <summary>
        ///     Adds a routed event handler for the particular
        ///     <see cref="RoutedEvent"/>
        /// </summary>
        /// <remarks>
        ///     The handler added thus is also known as
        ///     an instance handler <para/>
        ///     <para/>
        ///
        ///     NOTE: It is not an error to add a handler twice
        ///     (handler will simply be called twice) <para/>
        ///     <para/>
        ///
        ///     Input parameters <see cref="RoutedEvent"/>
        ///     and handler cannot be null <para/>
        ///     handledEventsToo input parameter when false means
        ///     that listener does not care about already handled events.
        ///     Hence the handler will not be invoked on the target if
        ///     the RoutedEvent has already been
        ///     <see cref="RoutedEventArgs.Handled"/> <para/>
        ///     handledEventsToo input parameter when true means
        ///     that the listener wants to hear about all events even if
        ///     they have already been handled. Hence the handler will
        ///     be invoked irrespective of the event being
        ///     <see cref="RoutedEventArgs.Handled"/>
        /// </remarks>
        /// <param name="routedEvent">
        ///     <see cref="RoutedEvent"/> for which the handler
        ///     is attached
        /// </param>
        /// <param name="handler">
        ///     The handler that will be invoked on this object
        ///     when the RoutedEvent is raised
        /// </param>
        /// <param name="handledEventsToo">
        ///     Flag indicating whether or not the listener wants to
        ///     hear about events that have already been handled
        /// </param>
        public void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            // VerifyAccess();

            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.Get(SRID.HandlerTypeIllegal));
            }

            EnsureEventHandlersStore();
            EventHandlersStore.AddRoutedEventHandler(routedEvent, handler, handledEventsToo);

            OnAddHandler (routedEvent, handler);
        }

        /// <summary>
        ///     Notifies subclass of a new routed event handler.  Note that this is
        ///     called once for each handler added, but OnRemoveHandler is only called
        ///     on the last removal.
        /// </summary>
        internal virtual void OnAddHandler(
            RoutedEvent routedEvent,
            Delegate handler)
        {
        }

        /// <summary>
        ///     Removes all instances of the specified routed
        ///     event handler for this object instance
        /// </summary>
        /// <remarks>
        ///     The handler removed thus is also known as
        ///     an instance handler <para/>
        ///     <para/>
        ///
        ///     NOTE: This method does nothing if there were
        ///     no handlers registered with the matching
        ///     criteria <para/>
        ///     <para/>
        ///
        ///     Input parameters <see cref="RoutedEvent"/>
        ///     and handler cannot be null <para/>
        ///     This method ignores the handledEventsToo criterion
        /// </remarks>
        /// <param name="routedEvent">
        ///     <see cref="RoutedEvent"/> for which the handler
        ///     is attached
        /// </param>
        /// <param name="handler">
        ///     The handler for this object instance to be removed
        /// </param>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            // VerifyAccess();

            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.Get(SRID.HandlerTypeIllegal));
            }

            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                store.RemoveRoutedEventHandler(routedEvent, handler);

                OnRemoveHandler (routedEvent, handler);

                if (store.Count == 0)
                {
                    // last event handler was removed -- throw away underlying EventHandlersStore
                    EventHandlersStoreField.ClearValue(this);
                    WriteFlag(CoreFlags.ExistsEventHandlersStore, false);
                }
}
        }

        /// <summary>
        ///     Notifies subclass of an event for which a handler has been removed.
        /// </summary>
        internal virtual void OnRemoveHandler(
            RoutedEvent routedEvent,
            Delegate handler)
        {
        }

        private void EventHandlersStoreAdd(EventPrivateKey key, Delegate handler)
        {
            EnsureEventHandlersStore();
            EventHandlersStore.Add(key, handler);
        }

        private void EventHandlersStoreRemove(EventPrivateKey key, Delegate handler)
        {
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                store.Remove(key, handler);
                if (store.Count == 0)
                {
                    // last event handler was removed -- throw away underlying EventHandlersStore
                    EventHandlersStoreField.ClearValue(this);
                    WriteFlag(CoreFlags.ExistsEventHandlersStore, false);
                }
            }
        }

        /// <summary>
        ///     Add the event handlers for this element to the route.
        /// </summary>
        public void AddToEventRoute(EventRoute route, RoutedEventArgs e)
        {
            if (route == null)
            {
                throw new ArgumentNullException("route");
            }
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            // Get class listeners for this UIElement
            RoutedEventHandlerInfoList classListeners =
                GlobalEventManager.GetDTypedClassListeners(this.DependencyObjectType, e.RoutedEvent);

            // Add all class listeners for this UIElement
            while (classListeners != null)
            {
                for(int i = 0; i < classListeners.Handlers.Length; i++)
                {
                    route.Add(this, classListeners.Handlers[i].Handler, classListeners.Handlers[i].InvokeHandledEventsToo);
                }

                classListeners = classListeners.Next;
            }

            // Get instance listeners for this UIElement
            FrugalObjectList<RoutedEventHandlerInfo> instanceListeners = null;
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                instanceListeners = store[e.RoutedEvent];

                // Add all instance listeners for this UIElement
                if (instanceListeners != null)
                {
                    for (int i = 0; i < instanceListeners.Count; i++)
                    {
                        route.Add(this, instanceListeners[i].Handler, instanceListeners[i].InvokeHandledEventsToo);
                    }
                }
            }

            // Allow Framework to add event handlers in styles
            AddToEventRouteCore(route, e);
        }

        /// <summary>
        ///     This virtual method is to be overridden in Framework
        ///     to be able to add handlers for styles
        /// </summary>
        internal virtual void AddToEventRouteCore(EventRoute route, RoutedEventArgs args)
        {
        }

        /// <summary>
        ///     Event Handlers Store
        /// </summary>
        /// <remarks>
        ///     The idea of exposing this property is to allow
        ///     elements in the Framework to generically use
        ///     EventHandlersStore for Clr events as well.
        /// </remarks>
        internal EventHandlersStore EventHandlersStore
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get
            {
                if(!ReadFlag(CoreFlags.ExistsEventHandlersStore))
                {
                    return null;
                }
                return EventHandlersStoreField.GetValue(this);
            }
        }

        /// <summary>
        ///     Ensures that EventHandlersStore will return
        ///     non-null when it is called.
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal void EnsureEventHandlersStore()
        {
            if (EventHandlersStore == null)
            {
                EventHandlersStoreField.SetValue(this, new EventHandlersStore());
                WriteFlag(CoreFlags.ExistsEventHandlersStore, true);
            }
        }

        #endregion Events

        internal virtual bool InvalidateAutomationAncestorsCore(Stack<DependencyObject> branchNodeStack, out bool continuePastVisualTree)
        {
            continuePastVisualTree = false;
            return true;
        }

        /// <summary>
        /// Used by UIElement, ContentElement, and UIElement3D to register common Events.
        /// </summary>
        internal static void RegisterEvents(Type type)
        {
            EventManager.RegisterClassHandler(type, Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(UIElement.OnPreviewMouseDownThunk), true);
            EventManager.RegisterClassHandler(type, Mouse.MouseDownEvent, new MouseButtonEventHandler(UIElement.OnMouseDownThunk), true);
            EventManager.RegisterClassHandler(type, Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(UIElement.OnPreviewMouseUpThunk), true);
            EventManager.RegisterClassHandler(type, Mouse.MouseUpEvent, new MouseButtonEventHandler(UIElement.OnMouseUpThunk), true);
            EventManager.RegisterClassHandler(type, UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(UIElement.OnPreviewMouseLeftButtonDownThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(UIElement.OnMouseLeftButtonDownThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(UIElement.OnPreviewMouseLeftButtonUpThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(UIElement.OnMouseLeftButtonUpThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(UIElement.OnPreviewMouseRightButtonDownThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(UIElement.OnMouseRightButtonDownThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.PreviewMouseRightButtonUpEvent, new MouseButtonEventHandler(UIElement.OnPreviewMouseRightButtonUpThunk), false);
            EventManager.RegisterClassHandler(type, UIElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(UIElement.OnMouseRightButtonUpThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.PreviewMouseMoveEvent, new MouseEventHandler(UIElement.OnPreviewMouseMoveThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.MouseMoveEvent, new MouseEventHandler(UIElement.OnMouseMoveThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(UIElement.OnPreviewMouseWheelThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.MouseWheelEvent, new MouseWheelEventHandler(UIElement.OnMouseWheelThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.MouseEnterEvent, new MouseEventHandler(UIElement.OnMouseEnterThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.MouseLeaveEvent, new MouseEventHandler(UIElement.OnMouseLeaveThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.GotMouseCaptureEvent, new MouseEventHandler(UIElement.OnGotMouseCaptureThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.LostMouseCaptureEvent, new MouseEventHandler(UIElement.OnLostMouseCaptureThunk), false);
            EventManager.RegisterClassHandler(type, Mouse.QueryCursorEvent, new QueryCursorEventHandler(UIElement.OnQueryCursorThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusDownEvent, new StylusDownEventHandler(UIElement.OnPreviewStylusDownThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusDownEvent, new StylusDownEventHandler(UIElement.OnStylusDownThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusUpEvent, new StylusEventHandler(UIElement.OnPreviewStylusUpThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusUpEvent, new StylusEventHandler(UIElement.OnStylusUpThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusMoveEvent, new StylusEventHandler(UIElement.OnPreviewStylusMoveThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusMoveEvent, new StylusEventHandler(UIElement.OnStylusMoveThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusInAirMoveEvent, new StylusEventHandler(UIElement.OnPreviewStylusInAirMoveThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusInAirMoveEvent, new StylusEventHandler(UIElement.OnStylusInAirMoveThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusEnterEvent, new StylusEventHandler(UIElement.OnStylusEnterThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusLeaveEvent, new StylusEventHandler(UIElement.OnStylusLeaveThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusInRangeEvent, new StylusEventHandler(UIElement.OnPreviewStylusInRangeThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusInRangeEvent, new StylusEventHandler(UIElement.OnStylusInRangeThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusOutOfRangeEvent, new StylusEventHandler(UIElement.OnPreviewStylusOutOfRangeThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusOutOfRangeEvent, new StylusEventHandler(UIElement.OnStylusOutOfRangeThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusSystemGestureEvent, new StylusSystemGestureEventHandler(UIElement.OnPreviewStylusSystemGestureThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusSystemGestureEvent, new StylusSystemGestureEventHandler(UIElement.OnStylusSystemGestureThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.GotStylusCaptureEvent, new StylusEventHandler(UIElement.OnGotStylusCaptureThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.LostStylusCaptureEvent, new StylusEventHandler(UIElement.OnLostStylusCaptureThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusButtonDownEvent, new StylusButtonEventHandler(UIElement.OnStylusButtonDownThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.StylusButtonUpEvent, new StylusButtonEventHandler(UIElement.OnStylusButtonUpThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusButtonDownEvent, new StylusButtonEventHandler(UIElement.OnPreviewStylusButtonDownThunk), false);
            EventManager.RegisterClassHandler(type, Stylus.PreviewStylusButtonUpEvent, new StylusButtonEventHandler(UIElement.OnPreviewStylusButtonUpThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.PreviewKeyDownEvent, new KeyEventHandler(UIElement.OnPreviewKeyDownThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.KeyDownEvent, new KeyEventHandler(UIElement.OnKeyDownThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.PreviewKeyUpEvent, new KeyEventHandler(UIElement.OnPreviewKeyUpThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.KeyUpEvent, new KeyEventHandler(UIElement.OnKeyUpThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.PreviewGotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(UIElement.OnPreviewGotKeyboardFocusThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(UIElement.OnGotKeyboardFocusThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.PreviewLostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(UIElement.OnPreviewLostKeyboardFocusThunk), false);
            EventManager.RegisterClassHandler(type, Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(UIElement.OnLostKeyboardFocusThunk), false);
            EventManager.RegisterClassHandler(type, TextCompositionManager.PreviewTextInputEvent, new TextCompositionEventHandler(UIElement.OnPreviewTextInputThunk), false);
            EventManager.RegisterClassHandler(type, TextCompositionManager.TextInputEvent, new TextCompositionEventHandler(UIElement.OnTextInputThunk), false);
            EventManager.RegisterClassHandler(type, CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(UIElement.OnPreviewExecutedThunk), false);
            EventManager.RegisterClassHandler(type, CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(UIElement.OnExecutedThunk), false);
            EventManager.RegisterClassHandler(type, CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(UIElement.OnPreviewCanExecuteThunk), false);
            EventManager.RegisterClassHandler(type, CommandManager.CanExecuteEvent, new CanExecuteRoutedEventHandler(UIElement.OnCanExecuteThunk), false);
            EventManager.RegisterClassHandler(type, CommandDevice.CommandDeviceEvent, new CommandDeviceEventHandler(UIElement.OnCommandDeviceThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.PreviewQueryContinueDragEvent, new QueryContinueDragEventHandler(UIElement.OnPreviewQueryContinueDragThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.QueryContinueDragEvent, new QueryContinueDragEventHandler(UIElement.OnQueryContinueDragThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.PreviewGiveFeedbackEvent, new GiveFeedbackEventHandler(UIElement.OnPreviewGiveFeedbackThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.GiveFeedbackEvent, new GiveFeedbackEventHandler(UIElement.OnGiveFeedbackThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.PreviewDragEnterEvent, new DragEventHandler(UIElement.OnPreviewDragEnterThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.DragEnterEvent, new DragEventHandler(UIElement.OnDragEnterThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.PreviewDragOverEvent, new DragEventHandler(UIElement.OnPreviewDragOverThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.DragOverEvent, new DragEventHandler(UIElement.OnDragOverThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.PreviewDragLeaveEvent, new DragEventHandler(UIElement.OnPreviewDragLeaveThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.DragLeaveEvent, new DragEventHandler(UIElement.OnDragLeaveThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.PreviewDropEvent, new DragEventHandler(UIElement.OnPreviewDropThunk), false);
            EventManager.RegisterClassHandler(type, DragDrop.DropEvent, new DragEventHandler(UIElement.OnDropThunk), false);
            EventManager.RegisterClassHandler(type, Touch.PreviewTouchDownEvent, new EventHandler<TouchEventArgs>(UIElement.OnPreviewTouchDownThunk), false);
            EventManager.RegisterClassHandler(type, Touch.TouchDownEvent, new EventHandler<TouchEventArgs>(UIElement.OnTouchDownThunk), false);
            EventManager.RegisterClassHandler(type, Touch.PreviewTouchMoveEvent, new EventHandler<TouchEventArgs>(UIElement.OnPreviewTouchMoveThunk), false);
            EventManager.RegisterClassHandler(type, Touch.TouchMoveEvent, new EventHandler<TouchEventArgs>(UIElement.OnTouchMoveThunk), false);
            EventManager.RegisterClassHandler(type, Touch.PreviewTouchUpEvent, new EventHandler<TouchEventArgs>(UIElement.OnPreviewTouchUpThunk), false);
            EventManager.RegisterClassHandler(type, Touch.TouchUpEvent, new EventHandler<TouchEventArgs>(UIElement.OnTouchUpThunk), false);
            EventManager.RegisterClassHandler(type, Touch.GotTouchCaptureEvent, new EventHandler<TouchEventArgs>(UIElement.OnGotTouchCaptureThunk), false);
            EventManager.RegisterClassHandler(type, Touch.LostTouchCaptureEvent, new EventHandler<TouchEventArgs>(UIElement.OnLostTouchCaptureThunk), false);
            EventManager.RegisterClassHandler(type, Touch.TouchEnterEvent, new EventHandler<TouchEventArgs>(UIElement.OnTouchEnterThunk), false);
            EventManager.RegisterClassHandler(type, Touch.TouchLeaveEvent, new EventHandler<TouchEventArgs>(UIElement.OnTouchLeaveThunk), false);
        }



        private static void OnPreviewMouseDownThunk(object sender, MouseButtonEventArgs e)
        {
            if(!e.Handled)
            {
                UIElement uie = sender as UIElement;

                if (uie != null)
                {
                    uie.OnPreviewMouseDown(e);
                }
                else
                {
                    ContentElement ce = sender as ContentElement;

                    if (ce != null)
                    {
                        ce.OnPreviewMouseDown(e);
                    }
                    else
                    {
                        ((UIElement3D)sender).OnPreviewMouseDown(e);
                    }
                }
            }

            // Always raise this "sub-event", but we pass along the handledness.
            UIElement.CrackMouseButtonEventAndReRaiseEvent((DependencyObject)sender, e);
        }

        private static void OnMouseDownThunk(object sender, MouseButtonEventArgs e)
        {
            if(!e.Handled)
            {
                CommandManager.TranslateInput((IInputElement)sender, e);
            }

            if(!e.Handled)
            {
                UIElement uie = sender as UIElement;

                if (uie != null)
                {
                    uie.OnMouseDown(e);
                }
                else
                {
                    ContentElement ce = sender as ContentElement;

                    if (ce != null)
                    {
                        ce.OnMouseDown(e);
                    }
                    else
                    {
                        ((UIElement3D)sender).OnMouseDown(e);
                    }
                }
            }

            // Always raise this "sub-event", but we pass along the handledness.
            UIElement.CrackMouseButtonEventAndReRaiseEvent((DependencyObject)sender, e);
        }

        private static void OnPreviewMouseUpThunk(object sender, MouseButtonEventArgs e)
        {
            if(!e.Handled)
            {
                UIElement uie = sender as UIElement;

                if (uie != null)
                {
                    uie.OnPreviewMouseUp(e);
                }
                else
                {
                    ContentElement ce = sender as ContentElement;

                    if (ce != null)
                    {
                        ce.OnPreviewMouseUp(e);
                    }
                    else
                    {
                        ((UIElement3D)sender).OnPreviewMouseUp(e);
                    }
                }
            }

            // Always raise this "sub-event", but we pass along the handledness.
            UIElement.CrackMouseButtonEventAndReRaiseEvent((DependencyObject)sender, e);
        }

        private static void OnMouseUpThunk(object sender, MouseButtonEventArgs e)
        {
            if(!e.Handled)
            {
                UIElement uie = sender as UIElement;

                if (uie != null)
                {
                    uie.OnMouseUp(e);
                }
                else
                {
                    ContentElement ce = sender as ContentElement;

                    if (ce != null)
                    {
                        ce.OnMouseUp(e);
                    }
                    else
                    {
                        ((UIElement3D)sender).OnMouseUp(e);
                    }
                }
            }

            // Always raise this "sub-event", but we pass along the handledness.
            UIElement.CrackMouseButtonEventAndReRaiseEvent((DependencyObject)sender, e);
        }

        private static void OnPreviewMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewMouseLeftButtonDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewMouseLeftButtonDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewMouseLeftButtonDown(e);
                }
            }
        }

        private static void OnMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseLeftButtonDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseLeftButtonDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseLeftButtonDown(e);
                }
            }
        }

        private static void OnPreviewMouseLeftButtonUpThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewMouseLeftButtonUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewMouseLeftButtonUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewMouseLeftButtonUp(e);
                }
            }
        }

        private static void OnMouseLeftButtonUpThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseLeftButtonUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseLeftButtonUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseLeftButtonUp(e);
                }
            }
        }

        private static void OnPreviewMouseRightButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewMouseRightButtonDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewMouseRightButtonDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewMouseRightButtonDown(e);
                }
            }
        }

        private static void OnMouseRightButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseRightButtonDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseRightButtonDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseRightButtonDown(e);
                }
            }
        }

        private static void OnPreviewMouseRightButtonUpThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewMouseRightButtonUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewMouseRightButtonUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewMouseRightButtonUp(e);
                }
            }
        }

        private static void OnMouseRightButtonUpThunk(object sender, MouseButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseRightButtonUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseRightButtonUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseRightButtonUp(e);
                }
            }
        }

        private static void OnPreviewMouseMoveThunk(object sender, MouseEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewMouseMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewMouseMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewMouseMove(e);
                }
            }
        }

        private static void OnMouseMoveThunk(object sender, MouseEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseMove(e);
                }
            }
        }

        private static void OnPreviewMouseWheelThunk(object sender, MouseWheelEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewMouseWheel(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewMouseWheel(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewMouseWheel(e);
                }
            }
        }

        private static void OnMouseWheelThunk(object sender, MouseWheelEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            CommandManager.TranslateInput((IInputElement)sender, e);

            if(!e.Handled)
            {
                UIElement uie = sender as UIElement;

                if (uie != null)
                {
                    uie.OnMouseWheel(e);
                }
                else
                {
                    ContentElement ce = sender as ContentElement;

                    if (ce != null)
                    {
                        ce.OnMouseWheel(e);
                    }
                    else
                    {
                        ((UIElement3D)sender).OnMouseWheel(e);
                    }
                }
            }
        }

        private static void OnMouseEnterThunk(object sender, MouseEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseEnter(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseEnter(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseEnter(e);
                }
            }
        }

        private static void OnMouseLeaveThunk(object sender, MouseEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnMouseLeave(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnMouseLeave(e);
                }
                else
                {
                    ((UIElement3D)sender).OnMouseLeave(e);
                }
            }
        }

        private static void OnGotMouseCaptureThunk(object sender, MouseEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnGotMouseCapture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnGotMouseCapture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnGotMouseCapture(e);
                }
            }
        }

        private static void OnLostMouseCaptureThunk(object sender, MouseEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnLostMouseCapture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnLostMouseCapture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnLostMouseCapture(e);
                }
            }
        }

        private static void OnQueryCursorThunk(object sender, QueryCursorEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnQueryCursor(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnQueryCursor(e);
                }
                else
                {
                    ((UIElement3D)sender).OnQueryCursor(e);
                }
            }
        }

        private static void OnPreviewStylusDownThunk(object sender, StylusDownEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusDown(e);
                }
            }
        }

        private static void OnStylusDownThunk(object sender, StylusDownEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusDown(e);
                }
            }
        }

        private static void OnPreviewStylusUpThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusUp(e);
                }
            }
        }

        private static void OnStylusUpThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusUp(e);
                }
            }
        }

        private static void OnPreviewStylusMoveThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusMove(e);
                }
            }
        }

        private static void OnStylusMoveThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusMove(e);
                }
            }
        }

        private static void OnPreviewStylusInAirMoveThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusInAirMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusInAirMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusInAirMove(e);
                }
            }
        }

        private static void OnStylusInAirMoveThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusInAirMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusInAirMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusInAirMove(e);
                }
            }
        }

        private static void OnStylusEnterThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusEnter(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusEnter(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusEnter(e);
                }
            }
        }

        private static void OnStylusLeaveThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusLeave(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusLeave(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusLeave(e);
                }
            }
        }

        private static void OnPreviewStylusInRangeThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusInRange(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusInRange(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusInRange(e);
                }
            }
        }

        private static void OnStylusInRangeThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusInRange(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusInRange(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusInRange(e);
                }
            }
        }

        private static void OnPreviewStylusOutOfRangeThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusOutOfRange(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusOutOfRange(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusOutOfRange(e);
                }
            }
        }

        private static void OnStylusOutOfRangeThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusOutOfRange(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusOutOfRange(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusOutOfRange(e);
                }
            }
        }

        private static void OnPreviewStylusSystemGestureThunk(object sender, StylusSystemGestureEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusSystemGesture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusSystemGesture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusSystemGesture(e);
                }
            }
        }

        private static void OnStylusSystemGestureThunk(object sender, StylusSystemGestureEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusSystemGesture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusSystemGesture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusSystemGesture(e);
                }
            }
        }

        private static void OnGotStylusCaptureThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnGotStylusCapture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnGotStylusCapture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnGotStylusCapture(e);
                }
            }
        }

        private static void OnLostStylusCaptureThunk(object sender, StylusEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnLostStylusCapture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnLostStylusCapture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnLostStylusCapture(e);
                }
            }
        }

        private static void OnStylusButtonDownThunk(object sender, StylusButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusButtonDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusButtonDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusButtonDown(e);
                }
            }
        }

        private static void OnStylusButtonUpThunk(object sender, StylusButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnStylusButtonUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnStylusButtonUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnStylusButtonUp(e);
                }
            }
        }

        private static void OnPreviewStylusButtonDownThunk(object sender, StylusButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusButtonDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusButtonDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusButtonDown(e);
                }
            }
        }

        private static void OnPreviewStylusButtonUpThunk(object sender, StylusButtonEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewStylusButtonUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewStylusButtonUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewStylusButtonUp(e);
                }
            }
        }

        private static void OnPreviewKeyDownThunk(object sender, KeyEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewKeyDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewKeyDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewKeyDown(e);
                }
            }
        }

        private static void OnKeyDownThunk(object sender, KeyEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            CommandManager.TranslateInput((IInputElement)sender, e);

            if(!e.Handled)
            {
                UIElement uie = sender as UIElement;

                if (uie != null)
                {
                    uie.OnKeyDown(e);
                }
                else
                {
                    ContentElement ce = sender as ContentElement;

                    if (ce != null)
                    {
                        ce.OnKeyDown(e);
                    }
                    else
                    {
                        ((UIElement3D)sender).OnKeyDown(e);
                    }
                }
            }
        }

        private static void OnPreviewKeyUpThunk(object sender, KeyEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewKeyUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewKeyUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewKeyUp(e);
                }
            }
        }

        private static void OnKeyUpThunk(object sender, KeyEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnKeyUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnKeyUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnKeyUp(e);
                }
            }
        }

        private static void OnPreviewGotKeyboardFocusThunk(object sender, KeyboardFocusChangedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewGotKeyboardFocus(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewGotKeyboardFocus(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewGotKeyboardFocus(e);
                }
            }
        }

        private static void OnGotKeyboardFocusThunk(object sender, KeyboardFocusChangedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnGotKeyboardFocus(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnGotKeyboardFocus(e);
                }
                else
                {
                    ((UIElement3D)sender).OnGotKeyboardFocus(e);
                }
            }
        }

        private static void OnPreviewLostKeyboardFocusThunk(object sender, KeyboardFocusChangedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewLostKeyboardFocus(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewLostKeyboardFocus(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewLostKeyboardFocus(e);
                }
            }
        }

        private static void OnLostKeyboardFocusThunk(object sender, KeyboardFocusChangedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnLostKeyboardFocus(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnLostKeyboardFocus(e);
                }
                else
                {
                    ((UIElement3D)sender).OnLostKeyboardFocus(e);
                }
            }
        }

        private static void OnPreviewTextInputThunk(object sender, TextCompositionEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewTextInput(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewTextInput(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewTextInput(e);
                }
            }
        }

        private static void OnTextInputThunk(object sender, TextCompositionEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnTextInput(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnTextInput(e);
                }
                else
                {
                    ((UIElement3D)sender).OnTextInput(e);
                }
            }
        }

        private static void OnPreviewExecutedThunk(object sender, ExecutedRoutedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            // Command Manager will determine if preview or regular event.
            CommandManager.OnExecuted(sender, e);
        }

        private static void OnExecutedThunk(object sender, ExecutedRoutedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            // Command Manager will determine if preview or regular event.
            CommandManager.OnExecuted(sender, e);
        }

        private static void OnPreviewCanExecuteThunk(object sender, CanExecuteRoutedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            // Command Manager will determine if preview or regular event.
            CommandManager.OnCanExecute(sender, e);
        }

        private static void OnCanExecuteThunk(object sender, CanExecuteRoutedEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            // Command Manager will determine if preview or regular event.
            CommandManager.OnCanExecute(sender, e);
        }

        private static void OnCommandDeviceThunk(object sender, CommandDeviceEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            // Command Manager will determine if preview or regular event.
            CommandManager.OnCommandDevice(sender, e);
        }

        private static void OnPreviewQueryContinueDragThunk(object sender, QueryContinueDragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewQueryContinueDrag(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewQueryContinueDrag(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewQueryContinueDrag(e);
                }
            }
        }

        private static void OnQueryContinueDragThunk(object sender, QueryContinueDragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnQueryContinueDrag(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnQueryContinueDrag(e);
                }
                else
                {
                    ((UIElement3D)sender).OnQueryContinueDrag(e);
                }
            }
        }

        private static void OnPreviewGiveFeedbackThunk(object sender, GiveFeedbackEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewGiveFeedback(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewGiveFeedback(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewGiveFeedback(e);
                }
            }
        }

        private static void OnGiveFeedbackThunk(object sender, GiveFeedbackEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnGiveFeedback(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnGiveFeedback(e);
                }
                else
                {
                    ((UIElement3D)sender).OnGiveFeedback(e);
                }
            }
        }

        private static void OnPreviewDragEnterThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewDragEnter(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewDragEnter(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewDragEnter(e);
                }
            }
        }

        private static void OnDragEnterThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnDragEnter(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnDragEnter(e);
                }
                else
                {
                    ((UIElement3D)sender).OnDragEnter(e);
                }
            }
        }

        private static void OnPreviewDragOverThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewDragOver(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewDragOver(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewDragOver(e);
                }
            }
        }

        private static void OnDragOverThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnDragOver(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnDragOver(e);
                }
                else
                {
                    ((UIElement3D)sender).OnDragOver(e);
                }
            }
        }

        private static void OnPreviewDragLeaveThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewDragLeave(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewDragLeave(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewDragLeave(e);
                }
            }
        }

        private static void OnDragLeaveThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnDragLeave(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnDragLeave(e);
                }
                else
                {
                    ((UIElement3D)sender).OnDragLeave(e);
                }
            }
        }

        private static void OnPreviewDropThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewDrop(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewDrop(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewDrop(e);
                }
            }
        }

        private static void OnDropThunk(object sender, DragEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnDrop(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnDrop(e);
                }
                else
                {
                    ((UIElement3D)sender).OnDrop(e);
                }
            }
        }

        private static void OnPreviewTouchDownThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewTouchDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewTouchDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewTouchDown(e);
                }
            }
        }

        private static void OnTouchDownThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnTouchDown(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnTouchDown(e);
                }
                else
                {
                    ((UIElement3D)sender).OnTouchDown(e);
                }
            }
        }

        private static void OnPreviewTouchMoveThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewTouchMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewTouchMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewTouchMove(e);
                }
            }
        }

        private static void OnTouchMoveThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnTouchMove(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnTouchMove(e);
                }
                else
                {
                    ((UIElement3D)sender).OnTouchMove(e);
                }
            }
        }

        private static void OnPreviewTouchUpThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnPreviewTouchUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnPreviewTouchUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnPreviewTouchUp(e);
                }
            }
        }

        private static void OnTouchUpThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnTouchUp(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnTouchUp(e);
                }
                else
                {
                    ((UIElement3D)sender).OnTouchUp(e);
                }
            }
        }

        private static void OnGotTouchCaptureThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnGotTouchCapture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnGotTouchCapture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnGotTouchCapture(e);
                }
            }
        }

        private static void OnLostTouchCaptureThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnLostTouchCapture(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnLostTouchCapture(e);
                }
                else
                {
                    ((UIElement3D)sender).OnLostTouchCapture(e);
                }
            }
        }

        private static void OnTouchEnterThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnTouchEnter(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnTouchEnter(e);
                }
                else
                {
                    ((UIElement3D)sender).OnTouchEnter(e);
                }
            }
        }

        private static void OnTouchLeaveThunk(object sender, TouchEventArgs e)
        {
            Invariant.Assert(!e.Handled, "Unexpected: Event has already been handled.");

            UIElement uie = sender as UIElement;

            if (uie != null)
            {
                uie.OnTouchLeave(e);
            }
            else
            {
                ContentElement ce = sender as ContentElement;

                if (ce != null)
                {
                    ce.OnTouchLeave(e);
                }
                else
                {
                    ((UIElement3D)sender).OnTouchLeave(e);
                }
            }
        }

        /// <summary>
        ///     Alias to the Mouse.PreviewMouseDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseDownEvent = Mouse.PreviewMouseDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse button was pressed
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseDown
        {
            add { AddHandler(Mouse.PreviewMouseDownEvent, value, false); }
            remove { RemoveHandler(Mouse.PreviewMouseDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse button was pressed
        /// </summary>
        protected virtual void OnPreviewMouseDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.MouseDownEvent.
        /// </summary>
        public static readonly RoutedEvent MouseDownEvent = Mouse.MouseDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse button was pressed
        /// </summary>
        public event MouseButtonEventHandler MouseDown
        {
            add { AddHandler(Mouse.MouseDownEvent, value, false); }
            remove { RemoveHandler(Mouse.MouseDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse button was pressed
        /// </summary>
        protected virtual void OnMouseDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.PreviewMouseUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseUpEvent = Mouse.PreviewMouseUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse button was released
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseUp
        {
            add { AddHandler(Mouse.PreviewMouseUpEvent, value, false); }
            remove { RemoveHandler(Mouse.PreviewMouseUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse button was released
        /// </summary>
        protected virtual void OnPreviewMouseUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.MouseUpEvent.
        /// </summary>
        public static readonly RoutedEvent MouseUpEvent = Mouse.MouseUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse button was released
        /// </summary>
        public event MouseButtonEventHandler MouseUp
        {
            add { AddHandler(Mouse.MouseUpEvent, value, false); }
            remove { RemoveHandler(Mouse.MouseUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse button was released
        /// </summary>
        protected virtual void OnMouseUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the left mouse button was pressed
        /// </summary>
        public static readonly RoutedEvent PreviewMouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent("PreviewMouseLeftButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the left mouse button was pressed
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseLeftButtonDown
        {
            add { AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, value, false); }
            remove { RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the left mouse button was pressed
        /// </summary>
        protected virtual void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the left mouse button was pressed
        /// </summary>
        public static readonly RoutedEvent MouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent("MouseLeftButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the left mouse button was pressed
        /// </summary>
        public event MouseButtonEventHandler MouseLeftButtonDown
        {
            add { AddHandler(UIElement.MouseLeftButtonDownEvent, value, false); }
            remove { RemoveHandler(UIElement.MouseLeftButtonDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the left mouse button was pressed
        /// </summary>
        protected virtual void OnMouseLeftButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the left mouse button was released
        /// </summary>
        public static readonly RoutedEvent PreviewMouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent("PreviewMouseLeftButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the left mouse button was released
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseLeftButtonUp
        {
            add { AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, value, false); }
            remove { RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the left mouse button was released
        /// </summary>
        protected virtual void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the left mouse button was released
        /// </summary>
        public static readonly RoutedEvent MouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent("MouseLeftButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the left mouse button was released
        /// </summary>
        public event MouseButtonEventHandler MouseLeftButtonUp
        {
            add { AddHandler(UIElement.MouseLeftButtonUpEvent, value, false); }
            remove { RemoveHandler(UIElement.MouseLeftButtonUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the left mouse button was released
        /// </summary>
        protected virtual void OnMouseLeftButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the right mouse button was pressed
        /// </summary>
        public static readonly RoutedEvent PreviewMouseRightButtonDownEvent = EventManager.RegisterRoutedEvent("PreviewMouseRightButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the right mouse button was pressed
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseRightButtonDown
        {
            add { AddHandler(UIElement.PreviewMouseRightButtonDownEvent, value, false); }
            remove { RemoveHandler(UIElement.PreviewMouseRightButtonDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the right mouse button was pressed
        /// </summary>
        protected virtual void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the right mouse button was pressed
        /// </summary>
        public static readonly RoutedEvent MouseRightButtonDownEvent = EventManager.RegisterRoutedEvent("MouseRightButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the right mouse button was pressed
        /// </summary>
        public event MouseButtonEventHandler MouseRightButtonDown
        {
            add { AddHandler(UIElement.MouseRightButtonDownEvent, value, false); }
            remove { RemoveHandler(UIElement.MouseRightButtonDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the right mouse button was pressed
        /// </summary>
        protected virtual void OnMouseRightButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the right mouse button was released
        /// </summary>
        public static readonly RoutedEvent PreviewMouseRightButtonUpEvent = EventManager.RegisterRoutedEvent("PreviewMouseRightButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the right mouse button was released
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseRightButtonUp
        {
            add { AddHandler(UIElement.PreviewMouseRightButtonUpEvent, value, false); }
            remove { RemoveHandler(UIElement.PreviewMouseRightButtonUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the right mouse button was released
        /// </summary>
        protected virtual void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Declaration of the routed event reporting the right mouse button was released
        /// </summary>
        public static readonly RoutedEvent MouseRightButtonUpEvent = EventManager.RegisterRoutedEvent("MouseRightButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), _typeofThis);

        /// <summary>
        ///     Event reporting the right mouse button was released
        /// </summary>
        public event MouseButtonEventHandler MouseRightButtonUp
        {
            add { AddHandler(UIElement.MouseRightButtonUpEvent, value, false); }
            remove { RemoveHandler(UIElement.MouseRightButtonUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the right mouse button was released
        /// </summary>
        protected virtual void OnMouseRightButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.PreviewMouseMoveEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseMoveEvent = Mouse.PreviewMouseMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a mouse move
        /// </summary>
        public event MouseEventHandler PreviewMouseMove
        {
            add { AddHandler(Mouse.PreviewMouseMoveEvent, value, false); }
            remove { RemoveHandler(Mouse.PreviewMouseMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a mouse move
        /// </summary>
        protected virtual void OnPreviewMouseMove(MouseEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.MouseMoveEvent.
        /// </summary>
        public static readonly RoutedEvent MouseMoveEvent = Mouse.MouseMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a mouse move
        /// </summary>
        public event MouseEventHandler MouseMove
        {
            add { AddHandler(Mouse.MouseMoveEvent, value, false); }
            remove { RemoveHandler(Mouse.MouseMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a mouse move
        /// </summary>
        protected virtual void OnMouseMove(MouseEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.PreviewMouseWheelEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseWheelEvent = Mouse.PreviewMouseWheelEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a mouse wheel rotation
        /// </summary>
        public event MouseWheelEventHandler PreviewMouseWheel
        {
            add { AddHandler(Mouse.PreviewMouseWheelEvent, value, false); }
            remove { RemoveHandler(Mouse.PreviewMouseWheelEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a mouse wheel rotation
        /// </summary>
        protected virtual void OnPreviewMouseWheel(MouseWheelEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.MouseWheelEvent.
        /// </summary>
        public static readonly RoutedEvent MouseWheelEvent = Mouse.MouseWheelEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a mouse wheel rotation
        /// </summary>
        public event MouseWheelEventHandler MouseWheel
        {
            add { AddHandler(Mouse.MouseWheelEvent, value, false); }
            remove { RemoveHandler(Mouse.MouseWheelEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a mouse wheel rotation
        /// </summary>
        protected virtual void OnMouseWheel(MouseWheelEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.MouseEnterEvent.
        /// </summary>
        public static readonly RoutedEvent MouseEnterEvent = Mouse.MouseEnterEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse entered this element
        /// </summary>
        public event MouseEventHandler MouseEnter
        {
            add { AddHandler(Mouse.MouseEnterEvent, value, false); }
            remove { RemoveHandler(Mouse.MouseEnterEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse entered this element
        /// </summary>
        protected virtual void OnMouseEnter(MouseEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.MouseLeaveEvent.
        /// </summary>
        public static readonly RoutedEvent MouseLeaveEvent = Mouse.MouseLeaveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse left this element
        /// </summary>
        public event MouseEventHandler MouseLeave
        {
            add { AddHandler(Mouse.MouseLeaveEvent, value, false); }
            remove { RemoveHandler(Mouse.MouseLeaveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse left this element
        /// </summary>
        protected virtual void OnMouseLeave(MouseEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.GotMouseCaptureEvent.
        /// </summary>
        public static readonly RoutedEvent GotMouseCaptureEvent = Mouse.GotMouseCaptureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that this element got the mouse capture
        /// </summary>
        public event MouseEventHandler GotMouseCapture
        {
            add { AddHandler(Mouse.GotMouseCaptureEvent, value, false); }
            remove { RemoveHandler(Mouse.GotMouseCaptureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that this element got the mouse capture
        /// </summary>
        protected virtual void OnGotMouseCapture(MouseEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.LostMouseCaptureEvent.
        /// </summary>
        public static readonly RoutedEvent LostMouseCaptureEvent = Mouse.LostMouseCaptureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that this element lost the mouse capture
        /// </summary>
        public event MouseEventHandler LostMouseCapture
        {
            add { AddHandler(Mouse.LostMouseCaptureEvent, value, false); }
            remove { RemoveHandler(Mouse.LostMouseCaptureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that this element lost the mouse capture
        /// </summary>
        protected virtual void OnLostMouseCapture(MouseEventArgs e) {}

        /// <summary>
        ///     Alias to the Mouse.QueryCursorEvent.
        /// </summary>
        public static readonly RoutedEvent QueryCursorEvent = Mouse.QueryCursorEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the cursor to display was requested
        /// </summary>
        public event QueryCursorEventHandler QueryCursor
        {
            add { AddHandler(Mouse.QueryCursorEvent, value, false); }
            remove { RemoveHandler(Mouse.QueryCursorEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the cursor to display was requested
        /// </summary>
        protected virtual void OnQueryCursor(QueryCursorEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusDownEvent = Stylus.PreviewStylusDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus-down
        /// </summary>
        public event StylusDownEventHandler PreviewStylusDown
        {
            add { AddHandler(Stylus.PreviewStylusDownEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus-down
        /// </summary>
        protected virtual void OnPreviewStylusDown(StylusDownEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusDownEvent.
        /// </summary>
        public static readonly RoutedEvent StylusDownEvent = Stylus.StylusDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus-down
        /// </summary>
        public event StylusDownEventHandler StylusDown
        {
            add { AddHandler(Stylus.StylusDownEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus-down
        /// </summary>
        protected virtual void OnStylusDown(StylusDownEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusUpEvent = Stylus.PreviewStylusUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus-up
        /// </summary>
        public event StylusEventHandler PreviewStylusUp
        {
            add { AddHandler(Stylus.PreviewStylusUpEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus-up
        /// </summary>
        protected virtual void OnPreviewStylusUp(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusUpEvent.
        /// </summary>
        public static readonly RoutedEvent StylusUpEvent = Stylus.StylusUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus-up
        /// </summary>
        public event StylusEventHandler StylusUp
        {
            add { AddHandler(Stylus.StylusUpEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus-up
        /// </summary>
        protected virtual void OnStylusUp(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusMoveEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusMoveEvent = Stylus.PreviewStylusMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus move
        /// </summary>
        public event StylusEventHandler PreviewStylusMove
        {
            add { AddHandler(Stylus.PreviewStylusMoveEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus move
        /// </summary>
        protected virtual void OnPreviewStylusMove(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusMoveEvent.
        /// </summary>
        public static readonly RoutedEvent StylusMoveEvent = Stylus.StylusMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus move
        /// </summary>
        public event StylusEventHandler StylusMove
        {
            add { AddHandler(Stylus.StylusMoveEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus move
        /// </summary>
        protected virtual void OnStylusMove(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusInAirMoveEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusInAirMoveEvent = Stylus.PreviewStylusInAirMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus-in-air-move
        /// </summary>
        public event StylusEventHandler PreviewStylusInAirMove
        {
            add { AddHandler(Stylus.PreviewStylusInAirMoveEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusInAirMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus-in-air-move
        /// </summary>
        protected virtual void OnPreviewStylusInAirMove(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusInAirMoveEvent.
        /// </summary>
        public static readonly RoutedEvent StylusInAirMoveEvent = Stylus.StylusInAirMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus-in-air-move
        /// </summary>
        public event StylusEventHandler StylusInAirMove
        {
            add { AddHandler(Stylus.StylusInAirMoveEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusInAirMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus-in-air-move
        /// </summary>
        protected virtual void OnStylusInAirMove(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusEnterEvent.
        /// </summary>
        public static readonly RoutedEvent StylusEnterEvent = Stylus.StylusEnterEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus entered this element
        /// </summary>
        public event StylusEventHandler StylusEnter
        {
            add { AddHandler(Stylus.StylusEnterEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusEnterEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus entered this element
        /// </summary>
        protected virtual void OnStylusEnter(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusLeaveEvent.
        /// </summary>
        public static readonly RoutedEvent StylusLeaveEvent = Stylus.StylusLeaveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus left this element
        /// </summary>
        public event StylusEventHandler StylusLeave
        {
            add { AddHandler(Stylus.StylusLeaveEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusLeaveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus left this element
        /// </summary>
        protected virtual void OnStylusLeave(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusInRangeEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusInRangeEvent = Stylus.PreviewStylusInRangeEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus is now in range of the digitizer
        /// </summary>
        public event StylusEventHandler PreviewStylusInRange
        {
            add { AddHandler(Stylus.PreviewStylusInRangeEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusInRangeEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus is now in range of the digitizer
        /// </summary>
        protected virtual void OnPreviewStylusInRange(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusInRangeEvent.
        /// </summary>
        public static readonly RoutedEvent StylusInRangeEvent = Stylus.StylusInRangeEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus is now in range of the digitizer
        /// </summary>
        public event StylusEventHandler StylusInRange
        {
            add { AddHandler(Stylus.StylusInRangeEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusInRangeEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus is now in range of the digitizer
        /// </summary>
        protected virtual void OnStylusInRange(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusOutOfRangeEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusOutOfRangeEvent = Stylus.PreviewStylusOutOfRangeEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus is now out of range of the digitizer
        /// </summary>
        public event StylusEventHandler PreviewStylusOutOfRange
        {
            add { AddHandler(Stylus.PreviewStylusOutOfRangeEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusOutOfRangeEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus is now out of range of the digitizer
        /// </summary>
        protected virtual void OnPreviewStylusOutOfRange(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusOutOfRangeEvent.
        /// </summary>
        public static readonly RoutedEvent StylusOutOfRangeEvent = Stylus.StylusOutOfRangeEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus is now out of range of the digitizer
        /// </summary>
        public event StylusEventHandler StylusOutOfRange
        {
            add { AddHandler(Stylus.StylusOutOfRangeEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusOutOfRangeEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus is now out of range of the digitizer
        /// </summary>
        protected virtual void OnStylusOutOfRange(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusSystemGestureEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusSystemGestureEvent = Stylus.PreviewStylusSystemGestureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus system gesture
        /// </summary>
        public event StylusSystemGestureEventHandler PreviewStylusSystemGesture
        {
            add { AddHandler(Stylus.PreviewStylusSystemGestureEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusSystemGestureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus system gesture
        /// </summary>
        protected virtual void OnPreviewStylusSystemGesture(StylusSystemGestureEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusSystemGestureEvent.
        /// </summary>
        public static readonly RoutedEvent StylusSystemGestureEvent = Stylus.StylusSystemGestureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a stylus system gesture
        /// </summary>
        public event StylusSystemGestureEventHandler StylusSystemGesture
        {
            add { AddHandler(Stylus.StylusSystemGestureEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusSystemGestureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a stylus system gesture
        /// </summary>
        protected virtual void OnStylusSystemGesture(StylusSystemGestureEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.GotStylusCaptureEvent.
        /// </summary>
        public static readonly RoutedEvent GotStylusCaptureEvent = Stylus.GotStylusCaptureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that this element got the stylus capture
        /// </summary>
        public event StylusEventHandler GotStylusCapture
        {
            add { AddHandler(Stylus.GotStylusCaptureEvent, value, false); }
            remove { RemoveHandler(Stylus.GotStylusCaptureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that this element got the stylus capture
        /// </summary>
        protected virtual void OnGotStylusCapture(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.LostStylusCaptureEvent.
        /// </summary>
        public static readonly RoutedEvent LostStylusCaptureEvent = Stylus.LostStylusCaptureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that this element lost the stylus capture
        /// </summary>
        public event StylusEventHandler LostStylusCapture
        {
            add { AddHandler(Stylus.LostStylusCaptureEvent, value, false); }
            remove { RemoveHandler(Stylus.LostStylusCaptureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that this element lost the stylus capture
        /// </summary>
        protected virtual void OnLostStylusCapture(StylusEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusButtonDownEvent.
        /// </summary>
        public static readonly RoutedEvent StylusButtonDownEvent = Stylus.StylusButtonDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus button is down
        /// </summary>
        public event StylusButtonEventHandler StylusButtonDown
        {
            add { AddHandler(Stylus.StylusButtonDownEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusButtonDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus button is down
        /// </summary>
        protected virtual void OnStylusButtonDown(StylusButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.StylusButtonUpEvent.
        /// </summary>
        public static readonly RoutedEvent StylusButtonUpEvent = Stylus.StylusButtonUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus button is up
        /// </summary>
        public event StylusButtonEventHandler StylusButtonUp
        {
            add { AddHandler(Stylus.StylusButtonUpEvent, value, false); }
            remove { RemoveHandler(Stylus.StylusButtonUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus button is up
        /// </summary>
        protected virtual void OnStylusButtonUp(StylusButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusButtonDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusButtonDownEvent = Stylus.PreviewStylusButtonDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus button is down
        /// </summary>
        public event StylusButtonEventHandler PreviewStylusButtonDown
        {
            add { AddHandler(Stylus.PreviewStylusButtonDownEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusButtonDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus button is down
        /// </summary>
        protected virtual void OnPreviewStylusButtonDown(StylusButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Stylus.PreviewStylusButtonUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewStylusButtonUpEvent = Stylus.PreviewStylusButtonUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the stylus button is up
        /// </summary>
        public event StylusButtonEventHandler PreviewStylusButtonUp
        {
            add { AddHandler(Stylus.PreviewStylusButtonUpEvent, value, false); }
            remove { RemoveHandler(Stylus.PreviewStylusButtonUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the stylus button is up
        /// </summary>
        protected virtual void OnPreviewStylusButtonUp(StylusButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.PreviewKeyDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewKeyDownEvent = Keyboard.PreviewKeyDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a key was pressed
        /// </summary>
        public event KeyEventHandler PreviewKeyDown
        {
            add { AddHandler(Keyboard.PreviewKeyDownEvent, value, false); }
            remove { RemoveHandler(Keyboard.PreviewKeyDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a key was pressed
        /// </summary>
        protected virtual void OnPreviewKeyDown(KeyEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.KeyDownEvent.
        /// </summary>
        public static readonly RoutedEvent KeyDownEvent = Keyboard.KeyDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a key was pressed
        /// </summary>
        public event KeyEventHandler KeyDown
        {
            add { AddHandler(Keyboard.KeyDownEvent, value, false); }
            remove { RemoveHandler(Keyboard.KeyDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a key was pressed
        /// </summary>
        protected virtual void OnKeyDown(KeyEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.PreviewKeyUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewKeyUpEvent = Keyboard.PreviewKeyUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a key was released
        /// </summary>
        public event KeyEventHandler PreviewKeyUp
        {
            add { AddHandler(Keyboard.PreviewKeyUpEvent, value, false); }
            remove { RemoveHandler(Keyboard.PreviewKeyUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a key was released
        /// </summary>
        protected virtual void OnPreviewKeyUp(KeyEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.KeyUpEvent.
        /// </summary>
        public static readonly RoutedEvent KeyUpEvent = Keyboard.KeyUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a key was released
        /// </summary>
        public event KeyEventHandler KeyUp
        {
            add { AddHandler(Keyboard.KeyUpEvent, value, false); }
            remove { RemoveHandler(Keyboard.KeyUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a key was released
        /// </summary>
        protected virtual void OnKeyUp(KeyEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.PreviewGotKeyboardFocusEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewGotKeyboardFocusEvent = Keyboard.PreviewGotKeyboardFocusEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that the keyboard is focused on this element
        /// </summary>
        public event KeyboardFocusChangedEventHandler PreviewGotKeyboardFocus
        {
            add { AddHandler(Keyboard.PreviewGotKeyboardFocusEvent, value, false); }
            remove { RemoveHandler(Keyboard.PreviewGotKeyboardFocusEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that the keyboard is focused on this element
        /// </summary>
        protected virtual void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.GotKeyboardFocusEvent.
        /// </summary>
        public static readonly RoutedEvent GotKeyboardFocusEvent = Keyboard.GotKeyboardFocusEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that the keyboard is focused on this element
        /// </summary>
        public event KeyboardFocusChangedEventHandler GotKeyboardFocus
        {
            add { AddHandler(Keyboard.GotKeyboardFocusEvent, value, false); }
            remove { RemoveHandler(Keyboard.GotKeyboardFocusEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that the keyboard is focused on this element
        /// </summary>
        protected virtual void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.PreviewLostKeyboardFocusEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewLostKeyboardFocusEvent = Keyboard.PreviewLostKeyboardFocusEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that the keyboard is no longer focusekeyboard is no longer focuseed
        /// </summary>
        public event KeyboardFocusChangedEventHandler PreviewLostKeyboardFocus
        {
            add { AddHandler(Keyboard.PreviewLostKeyboardFocusEvent, value, false); }
            remove { RemoveHandler(Keyboard.PreviewLostKeyboardFocusEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that the keyboard is no longer focusekeyboard is no longer focuseed
        /// </summary>
        protected virtual void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

        /// <summary>
        ///     Alias to the Keyboard.LostKeyboardFocusEvent.
        /// </summary>
        public static readonly RoutedEvent LostKeyboardFocusEvent = Keyboard.LostKeyboardFocusEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting that the keyboard is no longer focusekeyboard is no longer focuseed
        /// </summary>
        public event KeyboardFocusChangedEventHandler LostKeyboardFocus
        {
            add { AddHandler(Keyboard.LostKeyboardFocusEvent, value, false); }
            remove { RemoveHandler(Keyboard.LostKeyboardFocusEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting that the keyboard is no longer focusekeyboard is no longer focuseed
        /// </summary>
        protected virtual void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

        /// <summary>
        ///     Alias to the TextCompositionManager.PreviewTextInputEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewTextInputEvent = TextCompositionManager.PreviewTextInputEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting text composition
        /// </summary>
        public event TextCompositionEventHandler PreviewTextInput
        {
            add { AddHandler(TextCompositionManager.PreviewTextInputEvent, value, false); }
            remove { RemoveHandler(TextCompositionManager.PreviewTextInputEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting text composition
        /// </summary>
        protected virtual void OnPreviewTextInput(TextCompositionEventArgs e) {}

        /// <summary>
        ///     Alias to the TextCompositionManager.TextInputEvent.
        /// </summary>
        public static readonly RoutedEvent TextInputEvent = TextCompositionManager.TextInputEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting text composition
        /// </summary>
        public event TextCompositionEventHandler TextInput
        {
            add { AddHandler(TextCompositionManager.TextInputEvent, value, false); }
            remove { RemoveHandler(TextCompositionManager.TextInputEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting text composition
        /// </summary>
        protected virtual void OnTextInput(TextCompositionEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.PreviewQueryContinueDragEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewQueryContinueDragEvent = DragDrop.PreviewQueryContinueDragEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the preview query continue drag is going to happen
        /// </summary>
        public event QueryContinueDragEventHandler PreviewQueryContinueDrag
        {
            add { AddHandler(DragDrop.PreviewQueryContinueDragEvent, value, false); }
            remove { RemoveHandler(DragDrop.PreviewQueryContinueDragEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the preview query continue drag is going to happen
        /// </summary>
        protected virtual void OnPreviewQueryContinueDrag(QueryContinueDragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.QueryContinueDragEvent.
        /// </summary>
        public static readonly RoutedEvent QueryContinueDragEvent = DragDrop.QueryContinueDragEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the query continue drag is going to happen
        /// </summary>
        public event QueryContinueDragEventHandler QueryContinueDrag
        {
            add { AddHandler(DragDrop.QueryContinueDragEvent, value, false); }
            remove { RemoveHandler(DragDrop.QueryContinueDragEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the query continue drag is going to happen
        /// </summary>
        protected virtual void OnQueryContinueDrag(QueryContinueDragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.PreviewGiveFeedbackEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewGiveFeedbackEvent = DragDrop.PreviewGiveFeedbackEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the preview give feedback is going to happen
        /// </summary>
        public event GiveFeedbackEventHandler PreviewGiveFeedback
        {
            add { AddHandler(DragDrop.PreviewGiveFeedbackEvent, value, false); }
            remove { RemoveHandler(DragDrop.PreviewGiveFeedbackEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the preview give feedback is going to happen
        /// </summary>
        protected virtual void OnPreviewGiveFeedback(GiveFeedbackEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.GiveFeedbackEvent.
        /// </summary>
        public static readonly RoutedEvent GiveFeedbackEvent = DragDrop.GiveFeedbackEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the give feedback is going to happen
        /// </summary>
        public event GiveFeedbackEventHandler GiveFeedback
        {
            add { AddHandler(DragDrop.GiveFeedbackEvent, value, false); }
            remove { RemoveHandler(DragDrop.GiveFeedbackEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the give feedback is going to happen
        /// </summary>
        protected virtual void OnGiveFeedback(GiveFeedbackEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.PreviewDragEnterEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewDragEnterEvent = DragDrop.PreviewDragEnterEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the preview drag enter is going to happen
        /// </summary>
        public event DragEventHandler PreviewDragEnter
        {
            add { AddHandler(DragDrop.PreviewDragEnterEvent, value, false); }
            remove { RemoveHandler(DragDrop.PreviewDragEnterEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the preview drag enter is going to happen
        /// </summary>
        protected virtual void OnPreviewDragEnter(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.DragEnterEvent.
        /// </summary>
        public static readonly RoutedEvent DragEnterEvent = DragDrop.DragEnterEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the drag enter is going to happen
        /// </summary>
        public event DragEventHandler DragEnter
        {
            add { AddHandler(DragDrop.DragEnterEvent, value, false); }
            remove { RemoveHandler(DragDrop.DragEnterEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the drag enter is going to happen
        /// </summary>
        protected virtual void OnDragEnter(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.PreviewDragOverEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewDragOverEvent = DragDrop.PreviewDragOverEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the preview drag over is going to happen
        /// </summary>
        public event DragEventHandler PreviewDragOver
        {
            add { AddHandler(DragDrop.PreviewDragOverEvent, value, false); }
            remove { RemoveHandler(DragDrop.PreviewDragOverEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the preview drag over is going to happen
        /// </summary>
        protected virtual void OnPreviewDragOver(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.DragOverEvent.
        /// </summary>
        public static readonly RoutedEvent DragOverEvent = DragDrop.DragOverEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the drag over is going to happen
        /// </summary>
        public event DragEventHandler DragOver
        {
            add { AddHandler(DragDrop.DragOverEvent, value, false); }
            remove { RemoveHandler(DragDrop.DragOverEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the drag over is going to happen
        /// </summary>
        protected virtual void OnDragOver(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.PreviewDragLeaveEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewDragLeaveEvent = DragDrop.PreviewDragLeaveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the preview drag leave is going to happen
        /// </summary>
        public event DragEventHandler PreviewDragLeave
        {
            add { AddHandler(DragDrop.PreviewDragLeaveEvent, value, false); }
            remove { RemoveHandler(DragDrop.PreviewDragLeaveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the preview drag leave is going to happen
        /// </summary>
        protected virtual void OnPreviewDragLeave(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.DragLeaveEvent.
        /// </summary>
        public static readonly RoutedEvent DragLeaveEvent = DragDrop.DragLeaveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the drag leave is going to happen
        /// </summary>
        public event DragEventHandler DragLeave
        {
            add { AddHandler(DragDrop.DragLeaveEvent, value, false); }
            remove { RemoveHandler(DragDrop.DragLeaveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the drag leave is going to happen
        /// </summary>
        protected virtual void OnDragLeave(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.PreviewDropEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewDropEvent = DragDrop.PreviewDropEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the preview drop is going to happen
        /// </summary>
        public event DragEventHandler PreviewDrop
        {
            add { AddHandler(DragDrop.PreviewDropEvent, value, false); }
            remove { RemoveHandler(DragDrop.PreviewDropEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the preview drop is going to happen
        /// </summary>
        protected virtual void OnPreviewDrop(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the DragDrop.DropEvent.
        /// </summary>
        public static readonly RoutedEvent DropEvent = DragDrop.DropEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the drag enter is going to happen
        /// </summary>
        public event DragEventHandler Drop
        {
            add { AddHandler(DragDrop.DropEvent, value, false); }
            remove { RemoveHandler(DragDrop.DropEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the drag enter is going to happen
        /// </summary>
        protected virtual void OnDrop(DragEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.PreviewTouchDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewTouchDownEvent = Touch.PreviewTouchDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger touched the screen
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> PreviewTouchDown
        {
            add { AddHandler(Touch.PreviewTouchDownEvent, value, false); }
            remove { RemoveHandler(Touch.PreviewTouchDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger touched the screen
        /// </summary>
        protected virtual void OnPreviewTouchDown(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.TouchDownEvent.
        /// </summary>
        public static readonly RoutedEvent TouchDownEvent = Touch.TouchDownEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger touched the screen
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> TouchDown
        {
            add { AddHandler(Touch.TouchDownEvent, value, false); }
            remove { RemoveHandler(Touch.TouchDownEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger touched the screen
        /// </summary>
        protected virtual void OnTouchDown(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.PreviewTouchMoveEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewTouchMoveEvent = Touch.PreviewTouchMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger moved across the screen
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> PreviewTouchMove
        {
            add { AddHandler(Touch.PreviewTouchMoveEvent, value, false); }
            remove { RemoveHandler(Touch.PreviewTouchMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger moved across the screen
        /// </summary>
        protected virtual void OnPreviewTouchMove(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.TouchMoveEvent.
        /// </summary>
        public static readonly RoutedEvent TouchMoveEvent = Touch.TouchMoveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger moved across the screen
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> TouchMove
        {
            add { AddHandler(Touch.TouchMoveEvent, value, false); }
            remove { RemoveHandler(Touch.TouchMoveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger moved across the screen
        /// </summary>
        protected virtual void OnTouchMove(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.PreviewTouchUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewTouchUpEvent = Touch.PreviewTouchUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger lifted off the screen
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> PreviewTouchUp
        {
            add { AddHandler(Touch.PreviewTouchUpEvent, value, false); }
            remove { RemoveHandler(Touch.PreviewTouchUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger lifted off the screen
        /// </summary>
        protected virtual void OnPreviewTouchUp(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.TouchUpEvent.
        /// </summary>
        public static readonly RoutedEvent TouchUpEvent = Touch.TouchUpEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger lifted off the screen
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> TouchUp
        {
            add { AddHandler(Touch.TouchUpEvent, value, false); }
            remove { RemoveHandler(Touch.TouchUpEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger lifted off the screen
        /// </summary>
        protected virtual void OnTouchUp(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.GotTouchCaptureEvent.
        /// </summary>
        public static readonly RoutedEvent GotTouchCaptureEvent = Touch.GotTouchCaptureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger was captured to an element
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> GotTouchCapture
        {
            add { AddHandler(Touch.GotTouchCaptureEvent, value, false); }
            remove { RemoveHandler(Touch.GotTouchCaptureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger was captured to an element
        /// </summary>
        protected virtual void OnGotTouchCapture(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.LostTouchCaptureEvent.
        /// </summary>
        public static readonly RoutedEvent LostTouchCaptureEvent = Touch.LostTouchCaptureEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting a finger is no longer captured to an element
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> LostTouchCapture
        {
            add { AddHandler(Touch.LostTouchCaptureEvent, value, false); }
            remove { RemoveHandler(Touch.LostTouchCaptureEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting a finger is no longer captured to an element
        /// </summary>
        protected virtual void OnLostTouchCapture(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.TouchEnterEvent.
        /// </summary>
        public static readonly RoutedEvent TouchEnterEvent = Touch.TouchEnterEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse entered this element
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> TouchEnter
        {
            add { AddHandler(Touch.TouchEnterEvent, value, false); }
            remove { RemoveHandler(Touch.TouchEnterEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse entered this element
        /// </summary>
        protected virtual void OnTouchEnter(TouchEventArgs e) {}

        /// <summary>
        ///     Alias to the Touch.TouchLeaveEvent.
        /// </summary>
        public static readonly RoutedEvent TouchLeaveEvent = Touch.TouchLeaveEvent.AddOwner(_typeofThis);

        /// <summary>
        ///     Event reporting the mouse left this element
        /// </summary>
        [CustomCategory(SRID.Touch_Category)]
        public event EventHandler<TouchEventArgs> TouchLeave
        {
            add { AddHandler(Touch.TouchLeaveEvent, value, false); }
            remove { RemoveHandler(Touch.TouchLeaveEvent, value); }
        }

        /// <summary>
        ///     Virtual method reporting the mouse left this element
        /// </summary>
        protected virtual void OnTouchLeave(TouchEventArgs e) {}

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsMouseDirectlyOverPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsMouseDirectlyOver",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox, // default value
                                            new PropertyChangedCallback(IsMouseDirectlyOver_Changed)));

        /// <summary>
        ///     The dependency property for the IsMouseDirectlyOver property.
        /// </summary>
        public static readonly DependencyProperty IsMouseDirectlyOverProperty =
            IsMouseDirectlyOverPropertyKey.DependencyProperty;

        private static void IsMouseDirectlyOver_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement) d).RaiseIsMouseDirectlyOverChanged(e);
        }

        /// <summary>
        ///     IsMouseDirectlyOverChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsMouseDirectlyOverChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsMouseDirectlyOver property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsMouseDirectlyOverChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsMouseDirectlyOverChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsMouseDirectlyOverChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsMouseDirectlyOver property changed.
        /// </summary>
        protected virtual void OnIsMouseDirectlyOverChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private void RaiseIsMouseDirectlyOverChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsMouseDirectlyOverChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsMouseDirectlyOverChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsMouseOverPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsMouseOver",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the IsMouseOver property.
        /// </summary>
        public static readonly DependencyProperty IsMouseOverProperty =
            IsMouseOverPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsStylusOverPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsStylusOver",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the IsStylusOver property.
        /// </summary>
        public static readonly DependencyProperty IsStylusOverProperty =
            IsStylusOverPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsKeyboardFocusWithinPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsKeyboardFocusWithin",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the IsKeyboardFocusWithin property.
        /// </summary>
        public static readonly DependencyProperty IsKeyboardFocusWithinProperty =
            IsKeyboardFocusWithinPropertyKey.DependencyProperty;

        /// <summary>
        ///     IsKeyboardFocusWithinChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsKeyboardFocusWithinChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsKeyboardFocusWithin property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsKeyboardFocusWithinChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsKeyboardFocusWithinChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsKeyboardFocusWithinChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsKeyboardFocusWithin property changed.
        /// </summary>
        protected virtual void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        internal void RaiseIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsKeyboardFocusWithinChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsKeyboardFocusWithinChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsMouseCapturedPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsMouseCaptured",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox, // default value
                                            new PropertyChangedCallback(IsMouseCaptured_Changed)));

        /// <summary>
        ///     The dependency property for the IsMouseCaptured property.
        /// </summary>
        public static readonly DependencyProperty IsMouseCapturedProperty =
            IsMouseCapturedPropertyKey.DependencyProperty;

        private static void IsMouseCaptured_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement) d).RaiseIsMouseCapturedChanged(e);
        }

        /// <summary>
        ///     IsMouseCapturedChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsMouseCapturedChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsMouseCaptured property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsMouseCapturedChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsMouseCapturedChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsMouseCapturedChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsMouseCaptured property changed.
        /// </summary>
        protected virtual void OnIsMouseCapturedChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private void RaiseIsMouseCapturedChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsMouseCapturedChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsMouseCapturedChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsMouseCaptureWithinPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsMouseCaptureWithin",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the IsMouseCaptureWithin property.
        /// </summary>
        public static readonly DependencyProperty IsMouseCaptureWithinProperty =
            IsMouseCaptureWithinPropertyKey.DependencyProperty;

        /// <summary>
        ///     IsMouseCaptureWithinChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsMouseCaptureWithinChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsMouseCaptureWithin property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsMouseCaptureWithinChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsMouseCaptureWithinChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsMouseCaptureWithinChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsMouseCaptureWithin property changed.
        /// </summary>
        protected virtual void OnIsMouseCaptureWithinChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        internal void RaiseIsMouseCaptureWithinChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsMouseCaptureWithinChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsMouseCaptureWithinChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsStylusDirectlyOverPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsStylusDirectlyOver",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox, // default value
                                            new PropertyChangedCallback(IsStylusDirectlyOver_Changed)));

        /// <summary>
        ///     The dependency property for the IsStylusDirectlyOver property.
        /// </summary>
        public static readonly DependencyProperty IsStylusDirectlyOverProperty =
            IsStylusDirectlyOverPropertyKey.DependencyProperty;

        private static void IsStylusDirectlyOver_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement) d).RaiseIsStylusDirectlyOverChanged(e);
        }

        /// <summary>
        ///     IsStylusDirectlyOverChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsStylusDirectlyOverChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsStylusDirectlyOver property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsStylusDirectlyOverChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsStylusDirectlyOverChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsStylusDirectlyOverChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsStylusDirectlyOver property changed.
        /// </summary>
        protected virtual void OnIsStylusDirectlyOverChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private void RaiseIsStylusDirectlyOverChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsStylusDirectlyOverChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsStylusDirectlyOverChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsStylusCapturedPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsStylusCaptured",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox, // default value
                                            new PropertyChangedCallback(IsStylusCaptured_Changed)));

        /// <summary>
        ///     The dependency property for the IsStylusCaptured property.
        /// </summary>
        public static readonly DependencyProperty IsStylusCapturedProperty =
            IsStylusCapturedPropertyKey.DependencyProperty;

        private static void IsStylusCaptured_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement) d).RaiseIsStylusCapturedChanged(e);
        }

        /// <summary>
        ///     IsStylusCapturedChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsStylusCapturedChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsStylusCaptured property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsStylusCapturedChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsStylusCapturedChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsStylusCapturedChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsStylusCaptured property changed.
        /// </summary>
        protected virtual void OnIsStylusCapturedChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private void RaiseIsStylusCapturedChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsStylusCapturedChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsStylusCapturedChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsStylusCaptureWithinPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsStylusCaptureWithin",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the IsStylusCaptureWithin property.
        /// </summary>
        public static readonly DependencyProperty IsStylusCaptureWithinProperty =
            IsStylusCaptureWithinPropertyKey.DependencyProperty;

        /// <summary>
        ///     IsStylusCaptureWithinChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsStylusCaptureWithinChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsStylusCaptureWithin property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsStylusCaptureWithinChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsStylusCaptureWithinChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsStylusCaptureWithinChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsStylusCaptureWithin property changed.
        /// </summary>
        protected virtual void OnIsStylusCaptureWithinChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        internal void RaiseIsStylusCaptureWithinChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsStylusCaptureWithinChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsStylusCaptureWithinChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey IsKeyboardFocusedPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "IsKeyboardFocused",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox, // default value
                                            new PropertyChangedCallback(IsKeyboardFocused_Changed)));

        /// <summary>
        ///     The dependency property for the IsKeyboardFocused property.
        /// </summary>
        public static readonly DependencyProperty IsKeyboardFocusedProperty =
            IsKeyboardFocusedPropertyKey.DependencyProperty;

        private static void IsKeyboardFocused_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement) d).RaiseIsKeyboardFocusedChanged(e);
        }

        /// <summary>
        ///     IsKeyboardFocusedChanged private key
        /// </summary>
        internal static readonly EventPrivateKey IsKeyboardFocusedChangedKey = new EventPrivateKey();


        /// <summary>
        ///     An event reporting that the IsKeyboardFocused property changed.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsKeyboardFocusedChanged
        {
            add    { EventHandlersStoreAdd(UIElement.IsKeyboardFocusedChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsKeyboardFocusedChangedKey, value); }
        }

        /// <summary>
        ///     An event reporting that the IsKeyboardFocused property changed.
        /// </summary>
        protected virtual void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private void RaiseIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs args)
        {
            // Call the virtual method first.
            OnIsKeyboardFocusedChanged(args);

            // Raise the public event second.
            RaiseDependencyPropertyChanged(UIElement.IsKeyboardFocusedChangedKey, args);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey AreAnyTouchesDirectlyOverPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "AreAnyTouchesDirectlyOver",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the AreAnyTouchesDirectlyOver property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesDirectlyOverProperty =
            AreAnyTouchesDirectlyOverPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey AreAnyTouchesOverPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "AreAnyTouchesOver",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the AreAnyTouchesOver property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesOverProperty =
            AreAnyTouchesOverPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey AreAnyTouchesCapturedPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "AreAnyTouchesCaptured",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the AreAnyTouchesCaptured property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesCapturedProperty =
            AreAnyTouchesCapturedPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey AreAnyTouchesCapturedWithinPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                "AreAnyTouchesCapturedWithin",
                                typeof(bool),
                                _typeofThis,
                                new PropertyMetadata(
                                            BooleanBoxes.FalseBox));

        /// <summary>
        ///     The dependency property for the AreAnyTouchesCapturedWithin property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesCapturedWithinProperty =
            AreAnyTouchesCapturedWithinPropertyKey.DependencyProperty;

        internal bool ReadFlag(CoreFlags field)
        {
            return (_flags & field) != 0;
        }

        internal void WriteFlag(CoreFlags field,bool value)
        {
            if (value)
            {
                 _flags |= field;
            }
            else
            {
                 _flags &= (~field);
            }
        }

        private CoreFlags       _flags;
    }
}
