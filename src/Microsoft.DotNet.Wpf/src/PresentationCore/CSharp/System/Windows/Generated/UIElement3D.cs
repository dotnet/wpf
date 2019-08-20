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
    partial class UIElement3D 
    {
        static private readonly Type _typeofThis = typeof(UIElement3D);



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
        ///     Allows UIElement3D to augment the
        ///     <see cref="EventRoute"/>
        /// </summary>
        /// <remarks>
        ///     Sub-classes of UIElement3D can override
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
        ///     <see cref="UIElement3D.BuildRoute"/> and
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

            // Get class listeners for this UIElement3D
            RoutedEventHandlerInfoList classListeners =
                GlobalEventManager.GetDTypedClassListeners(this.DependencyObjectType, e.RoutedEvent);

            // Add all class listeners for this UIElement3D
            while (classListeners != null)
            {
                for(int i = 0; i < classListeners.Handlers.Length; i++)
                {
                    route.Add(this, classListeners.Handlers[i].Handler, classListeners.Handlers[i].InvokeHandledEventsToo);
                }

                classListeners = classListeners.Next;
            }

            // Get instance listeners for this UIElement3D
            FrugalObjectList<RoutedEventHandlerInfo> instanceListeners = null;
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                instanceListeners = store[e.RoutedEvent];

                // Add all instance listeners for this UIElement3D
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
        protected internal virtual void OnPreviewMouseDown(MouseButtonEventArgs e) {}

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
        protected internal virtual void OnMouseDown(MouseButtonEventArgs e) {}

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
        protected internal virtual void OnPreviewMouseUp(MouseButtonEventArgs e) {}

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
        protected internal virtual void OnMouseUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.PreviewMouseLeftButtonDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseLeftButtonDownEvent = UIElement.PreviewMouseLeftButtonDownEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.MouseLeftButtonDownEvent.
        /// </summary>
        public static readonly RoutedEvent MouseLeftButtonDownEvent = UIElement.MouseLeftButtonDownEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnMouseLeftButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.PreviewMouseLeftButtonUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseLeftButtonUpEvent = UIElement.PreviewMouseLeftButtonUpEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.MouseLeftButtonUpEvent.
        /// </summary>
        public static readonly RoutedEvent MouseLeftButtonUpEvent = UIElement.MouseLeftButtonUpEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnMouseLeftButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.PreviewMouseRightButtonDownEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseRightButtonDownEvent = UIElement.PreviewMouseRightButtonDownEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.MouseRightButtonDownEvent.
        /// </summary>
        public static readonly RoutedEvent MouseRightButtonDownEvent = UIElement.MouseRightButtonDownEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnMouseRightButtonDown(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.PreviewMouseRightButtonUpEvent.
        /// </summary>
        public static readonly RoutedEvent PreviewMouseRightButtonUpEvent = UIElement.PreviewMouseRightButtonUpEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e) {}

        /// <summary>
        ///     Alias to the UIElement.MouseRightButtonUpEvent.
        /// </summary>
        public static readonly RoutedEvent MouseRightButtonUpEvent = UIElement.MouseRightButtonUpEvent.AddOwner(_typeofThis);

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
        protected internal virtual void OnMouseRightButtonUp(MouseButtonEventArgs e) {}

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
        protected internal virtual void OnPreviewMouseMove(MouseEventArgs e) {}

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
        protected internal virtual void OnMouseMove(MouseEventArgs e) {}

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
        protected internal virtual void OnPreviewMouseWheel(MouseWheelEventArgs e) {}

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
        protected internal virtual void OnMouseWheel(MouseWheelEventArgs e) {}

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
        protected internal virtual void OnMouseEnter(MouseEventArgs e) {}

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
        protected internal virtual void OnMouseLeave(MouseEventArgs e) {}

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
        protected internal virtual void OnGotMouseCapture(MouseEventArgs e) {}

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
        protected internal virtual void OnLostMouseCapture(MouseEventArgs e) {}

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
        protected internal virtual void OnQueryCursor(QueryCursorEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusDown(StylusDownEventArgs e) {}

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
        protected internal virtual void OnStylusDown(StylusDownEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusUp(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusUp(StylusEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusMove(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusMove(StylusEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusInAirMove(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusInAirMove(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusEnter(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusLeave(StylusEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusInRange(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusInRange(StylusEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusOutOfRange(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusOutOfRange(StylusEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusSystemGesture(StylusSystemGestureEventArgs e) {}

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
        protected internal virtual void OnStylusSystemGesture(StylusSystemGestureEventArgs e) {}

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
        protected internal virtual void OnGotStylusCapture(StylusEventArgs e) {}

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
        protected internal virtual void OnLostStylusCapture(StylusEventArgs e) {}

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
        protected internal virtual void OnStylusButtonDown(StylusButtonEventArgs e) {}

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
        protected internal virtual void OnStylusButtonUp(StylusButtonEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusButtonDown(StylusButtonEventArgs e) {}

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
        protected internal virtual void OnPreviewStylusButtonUp(StylusButtonEventArgs e) {}

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
        protected internal virtual void OnPreviewKeyDown(KeyEventArgs e) {}

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
        protected internal virtual void OnKeyDown(KeyEventArgs e) {}

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
        protected internal virtual void OnPreviewKeyUp(KeyEventArgs e) {}

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
        protected internal virtual void OnKeyUp(KeyEventArgs e) {}

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
        protected internal virtual void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

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
        protected internal virtual void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

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
        protected internal virtual void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

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
        protected internal virtual void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {}

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
        protected internal virtual void OnPreviewTextInput(TextCompositionEventArgs e) {}

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
        protected internal virtual void OnTextInput(TextCompositionEventArgs e) {}

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
        protected internal virtual void OnPreviewQueryContinueDrag(QueryContinueDragEventArgs e) {}

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
        protected internal virtual void OnQueryContinueDrag(QueryContinueDragEventArgs e) {}

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
        protected internal virtual void OnPreviewGiveFeedback(GiveFeedbackEventArgs e) {}

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
        protected internal virtual void OnGiveFeedback(GiveFeedbackEventArgs e) {}

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
        protected internal virtual void OnPreviewDragEnter(DragEventArgs e) {}

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
        protected internal virtual void OnDragEnter(DragEventArgs e) {}

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
        protected internal virtual void OnPreviewDragOver(DragEventArgs e) {}

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
        protected internal virtual void OnDragOver(DragEventArgs e) {}

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
        protected internal virtual void OnPreviewDragLeave(DragEventArgs e) {}

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
        protected internal virtual void OnDragLeave(DragEventArgs e) {}

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
        protected internal virtual void OnPreviewDrop(DragEventArgs e) {}

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
        protected internal virtual void OnDrop(DragEventArgs e) {}

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
        protected internal virtual void OnPreviewTouchDown(TouchEventArgs e) {}

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
        protected internal virtual void OnTouchDown(TouchEventArgs e) {}

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
        protected internal virtual void OnPreviewTouchMove(TouchEventArgs e) {}

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
        protected internal virtual void OnTouchMove(TouchEventArgs e) {}

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
        protected internal virtual void OnPreviewTouchUp(TouchEventArgs e) {}

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
        protected internal virtual void OnTouchUp(TouchEventArgs e) {}

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
        protected internal virtual void OnGotTouchCapture(TouchEventArgs e) {}

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
        protected internal virtual void OnLostTouchCapture(TouchEventArgs e) {}

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
        protected internal virtual void OnTouchEnter(TouchEventArgs e) {}

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
        protected internal virtual void OnTouchLeave(TouchEventArgs e) {}

        /// <summary>
        ///     The dependency property for the IsMouseDirectlyOver property.
        /// </summary>
        public static readonly DependencyProperty IsMouseDirectlyOverProperty = UIElement.IsMouseDirectlyOverProperty.AddOwner(_typeofThis);

        private static void IsMouseDirectlyOver_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement3D) d).RaiseIsMouseDirectlyOverChanged(e);
        }

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
        ///     The dependency property for the IsMouseOver property.
        /// </summary>
        public static readonly DependencyProperty IsMouseOverProperty = UIElement.IsMouseOverProperty.AddOwner(_typeofThis);

        /// <summary>
        ///     The dependency property for the IsStylusOver property.
        /// </summary>
        public static readonly DependencyProperty IsStylusOverProperty = UIElement.IsStylusOverProperty.AddOwner(_typeofThis);

        /// <summary>
        ///     The dependency property for the IsKeyboardFocusWithin property.
        /// </summary>
        public static readonly DependencyProperty IsKeyboardFocusWithinProperty = UIElement.IsKeyboardFocusWithinProperty.AddOwner(_typeofThis);

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
        ///     The dependency property for the IsMouseCaptured property.
        /// </summary>
        public static readonly DependencyProperty IsMouseCapturedProperty = UIElement.IsMouseCapturedProperty.AddOwner(_typeofThis);

        private static void IsMouseCaptured_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement3D) d).RaiseIsMouseCapturedChanged(e);
        }

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
        ///     The dependency property for the IsMouseCaptureWithin property.
        /// </summary>
        public static readonly DependencyProperty IsMouseCaptureWithinProperty = UIElement.IsMouseCaptureWithinProperty.AddOwner(_typeofThis);

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
        ///     The dependency property for the IsStylusDirectlyOver property.
        /// </summary>
        public static readonly DependencyProperty IsStylusDirectlyOverProperty = UIElement.IsStylusDirectlyOverProperty.AddOwner(_typeofThis);

        private static void IsStylusDirectlyOver_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement3D) d).RaiseIsStylusDirectlyOverChanged(e);
        }

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
        ///     The dependency property for the IsStylusCaptured property.
        /// </summary>
        public static readonly DependencyProperty IsStylusCapturedProperty = UIElement.IsStylusCapturedProperty.AddOwner(_typeofThis);

        private static void IsStylusCaptured_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement3D) d).RaiseIsStylusCapturedChanged(e);
        }

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
        ///     The dependency property for the IsStylusCaptureWithin property.
        /// </summary>
        public static readonly DependencyProperty IsStylusCaptureWithinProperty = UIElement.IsStylusCaptureWithinProperty.AddOwner(_typeofThis);

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
        ///     The dependency property for the IsKeyboardFocused property.
        /// </summary>
        public static readonly DependencyProperty IsKeyboardFocusedProperty = UIElement.IsKeyboardFocusedProperty.AddOwner(_typeofThis);

        private static void IsKeyboardFocused_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UIElement3D) d).RaiseIsKeyboardFocusedChanged(e);
        }

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
        ///     The dependency property for the AreAnyTouchesDirectlyOver property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesDirectlyOverProperty = UIElement.AreAnyTouchesDirectlyOverProperty.AddOwner(_typeofThis);

        /// <summary>
        ///     The dependency property for the AreAnyTouchesOver property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesOverProperty = UIElement.AreAnyTouchesOverProperty.AddOwner(_typeofThis);

        /// <summary>
        ///     The dependency property for the AreAnyTouchesCaptured property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesCapturedProperty = UIElement.AreAnyTouchesCapturedProperty.AddOwner(_typeofThis);

        /// <summary>
        ///     The dependency property for the AreAnyTouchesCapturedWithin property.
        /// </summary>
        public static readonly DependencyProperty AreAnyTouchesCapturedWithinProperty = UIElement.AreAnyTouchesCapturedWithinProperty.AddOwner(_typeofThis);

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
