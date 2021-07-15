// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;
using System.Collections.Specialized ;
using System.Windows.Input;
using System.Diagnostics;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    ///     The container for all state associated
    ///     with a RoutedEvent
    /// </summary>
    /// <remarks>
    ///     <see cref="RoutedEventArgs"/>
    ///     constitutes the <para/>
    ///     <see cref="RoutedEventArgs.RoutedEvent"/>, <para/>
    ///     <see cref="RoutedEventArgs.Handled"/>, <para/>
    ///     <see cref="RoutedEventArgs.Source"/> and <para/>
    ///     <see cref="RoutedEventArgs.OriginalSource"/> <para/>
    ///     <para/>
    ///
    ///     Different <see cref="RoutedEventArgs"/>
    ///     can be used with a single <see cref="RoutedEvent"/> <para/>
    ///     <para/>
    ///
    ///     The <see cref="RoutedEventArgs"/> is responsible
    ///     for packaging the <see cref="RoutedEvent"/>,
    ///     providing extra event state info, and invoking the
    ///     handler associated with the RoutedEvent
    /// </remarks>
    public class RoutedEventArgs : EventArgs
    {
        #region Construction

        /// <summary>
        ///     Constructor for <see cref="RoutedEventArgs"/>
        /// </summary>
        /// <remarks>
        ///     All members take default values <para/>
        ///     <para/>
        ///
        ///     <see cref="RoutedEventArgs.RoutedEvent"/>
        ///     defaults to null <para/>
        ///     <see cref="RoutedEventArgs.Handled"/> defaults to
        ///     false <para/>
        ///     <see cref="Source"/> defaults to null <para/>
        ///     <see cref="OriginalSource"/> also defaults to null
        ///     <para/>
        /// </remarks>
        public RoutedEventArgs()
        {
        }

        /// <summary>
        ///     Constructor for <see cref="RoutedEventArgs"/>
        /// </summary>
        /// <param name="routedEvent">The new value that the RoutedEvent Property is being set to </param>
        public RoutedEventArgs(RoutedEvent routedEvent) : this( routedEvent, null)
        {
        }

        /// <summary>
        ///     Constructor for <see cref="RoutedEventArgs"/>
        /// </summary>
        /// <param name="source">The new value that the SourceProperty is being set to </param>
        /// <param name="routedEvent">The new value that the RoutedEvent Property is being set to </param>
        public RoutedEventArgs(RoutedEvent routedEvent, object source)
        {
            _routedEvent = routedEvent;
            _source = _originalSource = source;
        }

        #endregion Construction

        #region External API
        /// <summary>
        ///     Returns the <see cref="RoutedEvent"/> associated
        ///     with this <see cref="RoutedEventArgs"/>
        /// </summary>
        /// <remarks>
        ///     The <see cref="RoutedEvent"/> cannot be null
        ///     at any time
        /// </remarks>
        public RoutedEvent RoutedEvent
        {
            get {return _routedEvent;}
            set
            {
                if (UserInitiated && InvokingHandler)
                    throw new InvalidOperationException(SR.Get(SRID.RoutedEventCannotChangeWhileRouting));

                _routedEvent = value;
            }
        }

        /// <summary>
        ///     Changes the RoutedEvent assocatied with these RoutedEventArgs
        /// </summary>
        /// <remarks>
        ///     Only used internally.  Added to support cracking generic MouseButtonDown/Up events
        ///     into MouseLeft/RightButtonDown/Up events.
        /// </remarks>
        /// <param name="newRoutedEvent">
        ///     The new RoutedEvent to associate with these RoutedEventArgs
        /// </param>
        internal void OverrideRoutedEvent( RoutedEvent newRoutedEvent )
        {
            _routedEvent = newRoutedEvent;
        }

        /// <summary>
        ///     Returns a boolean flag indicating if or not this
        ///     RoutedEvent has been handled this far in the route
        /// </summary>
        /// <remarks>
        ///     Initially starts with a false value before routing
        ///     has begun
        /// </remarks>
        public bool Handled
        {
            get
            {
                return _flags[ HandledIndex ] ;
            }

            set
            {
                if (_routedEvent == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RoutedEventArgsMustHaveRoutedEvent));
                }


                if( TraceRoutedEvent.IsEnabled )
                {
                    TraceRoutedEvent.TraceActivityItem(
                                            TraceRoutedEvent.HandleEvent,
                                            value,
                                            RoutedEvent.OwnerType.Name,
                                            RoutedEvent.Name,
                                            this );
                }


                // Note: We need to allow the caller to change the handled value
                // from true to false.
                //
                // We are concerned about scenarios where a child element
                // listens to a high-level event (such as TextInput) while a
                // parent element listens tp a low-level event such as KeyDown.
                // In these scenarios, we want the parent to not respond to the
                // KeyDown event, in deference to the child.
                //
                // Internally we implement this by asking the parent to only
                // respond to KeyDown events if they have focus.  This works
                // around the problem and is an example of an unofficial
                // protocol coordinating the two elements.
                //
                // But we imagine that there will be some cases we miss or
                // that third parties introduce.  For these cases, we expect
                // that the application author may need to mark the KeyDown
                // as handled in the child, and then reset the event to
                // being unhandled after the parent, so that default processing
                // and promotion still occur.
                //
                // For more information see the following task:
                // 20284: Input promotion breaks down when lower level input is intercepted

                _flags[ HandledIndex ] = value;
}
        }

        /// <summary>
        ///     Returns Source object that raised the RoutedEvent
        /// </summary>
        public object Source
        {
            get {return _source;}
            set
            {
                if (InvokingHandler && UserInitiated)
                    throw new InvalidOperationException(SR.Get(SRID.RoutedEventCannotChangeWhileRouting));

                if (_routedEvent == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RoutedEventArgsMustHaveRoutedEvent));
                }


                object source = value ;
                if (_source == null && _originalSource == null)
                {
                    // Gets here when it is the first time that the source is set.
                    // This implies that this is also the original source of the event
                    _source = _originalSource = source;
                    OnSetSource(source);
                }
                else if (_source != source)
                {
                    // This is the actiaon taken at all other times when the
                    // source is being set to a different value from what it was
                    _source = source;
                    OnSetSource(source);
                }
            }
        }

        /// <summary>
        ///     Changes the Source assocatied with these RoutedEventArgs
        /// </summary>
        /// <remarks>
        ///     Only used internally.  Added to support cracking generic MouseButtonDown/Up events
        ///     into MouseLeft/RightButtonDown/Up events.
        /// </remarks>
        /// <param name="source">
        ///     The new object to associate as the source of these RoutedEventArgs
        /// </param>
        internal void OverrideSource( object source )
        {
            _source = source;
        }

        /// <summary>
        ///     Returns OriginalSource object that raised the RoutedEvent
        /// </summary>
        /// <remarks>
        ///     Always returns the OriginalSource object that raised the
        ///     RoutedEvent unlike <see cref="RoutedEventArgs.Source"/>
        ///     that may vary under specific scenarios <para/>
        ///     This property acquires its value once before the event
        ///     handlers are invoked and never changes then on
        /// </remarks>
        public object OriginalSource
        {
            get {return _originalSource;}
        }

        /// <summary>
        ///     Invoked when the source of the event is set
        /// </summary>
        /// <remarks>
        ///     Changing the source of an event can often
        ///     require updating the data within the event.
        ///     For this reason, the OnSource=  method is
        ///     protected virtual and is meant to be
        ///     overridden by sub-classes of
        ///     <see cref="RoutedEventArgs"/> <para/>
        ///     Also see <see cref="RoutedEventArgs.Source"/>
        /// </remarks>
        /// <param name="source">
        ///     The new value that the SourceProperty is being set to
        /// </param>
        protected virtual void OnSetSource(object source)
        {
        }

        /// <summary>
        ///     Invokes the generic handler with the
        ///     appropriate arguments
        /// </summary>
        /// <remarks>
        ///     Is meant to be overridden by sub-classes of
        ///     <see cref="RoutedEventArgs"/> to provide
        ///     more efficient invocation of their delegate
        /// </remarks>
        /// <param name="genericHandler">
        ///     Generic Handler to be invoked
        /// </param>
        /// <param name="genericTarget">
        ///     Target on whom the Handler will be invoked
        /// </param>
        protected virtual void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler == null)
            {
                throw new ArgumentNullException("genericHandler");
            }

            if (genericTarget == null)
            {
                throw new ArgumentNullException("genericTarget");
            }

            if (_routedEvent == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.RoutedEventArgsMustHaveRoutedEvent));
            }

            InvokingHandler = true;
            try
            {
                if (genericHandler is RoutedEventHandler)
                {
                    ((RoutedEventHandler)genericHandler)(genericTarget, this);
                }
                else
                {
                    // Restricted Action - reflection permission required
                    genericHandler.DynamicInvoke(new object[] {genericTarget, this});
                }
            }
            finally
            {
                InvokingHandler = false;
            }
}

        #endregion External API

        #region Operations

        // Calls the InvokeEventHandler protected
        // virtual method
        //
        // This method is needed because
        // delegates are invoked from
        // RoutedEventHandler which is not a
        // sub-class of RoutedEventArgs
        // and hence cannot invoke protected
        // method RoutedEventArgs.FireEventHandler
        internal void InvokeHandler(Delegate handler, object target)
        {
            InvokingHandler = true;

            try
            {
                InvokeEventHandler(handler, target);
            }
            finally
            {
                InvokingHandler = false;
            }
        }

        internal bool UserInitiated
        {
            [FriendAccessAllowed] // Also used by Framework.
            get
            {
                if (_flags [UserInitiatedIndex])
                {
                    return true;
                }
                return false;
            }
        }

        internal void MarkAsUserInitiated()
        {
            _flags [ UserInitiatedIndex ] = true;
        }

        internal void ClearUserInitiated()
        {
            _flags [ UserInitiatedIndex ] = false ;
        }

        private bool InvokingHandler
        {
            get
            {
                return _flags[InvokingHandlerIndex];
            }
            set
            {
                _flags[InvokingHandlerIndex] = value;
            }
        }
        #endregion Operations



        #region Data

        private RoutedEvent _routedEvent;
        private object _source;
        private object _originalSource;

        private BitVector32          _flags;

        private const int HandledIndex                          = 1;
        private const int UserInitiatedIndex                    = 2;
        private const int InvokingHandlerIndex                  = 4;

        #endregion Data
    }

    /// <summary>
    ///     RoutedEventHandler Definition
    /// </summary>
    /// <ExternalAPI/>
    public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);
}

