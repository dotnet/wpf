// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows
{
    /// <summary>
    ///     Container for handler instance and other
    ///     invocation preferences for this handler
    ///     instance
    /// </summary>
    /// <remarks>
    ///     RoutedEventHandlerInfo constitutes the
    ///     handler instance and flag that indicates if 
    ///     or not this handler must be invoked for 
    ///     already handled events <para/>
    ///     <para/>
    ///
    ///     This class needs to be public because it is 
    ///     used by ContentElement in the Framework 
    ///     to store Instance EventHandlers
    /// </remarks>
    public struct RoutedEventHandlerInfo
    {
        #region Construction
        
        /// <summary>
        ///     Construtor for RoutedEventHandlerInfo
        /// </summary>
        /// <param name="handler">
        ///     Non-null handler
        /// </param>
        /// <param name="handledEventsToo">
        ///     Flag that indicates if or not the handler must 
        ///     be invoked for already handled events
        /// </param>
        internal RoutedEventHandlerInfo(Delegate handler, bool handledEventsToo)
        {
            _handler = handler;
            _handledEventsToo = handledEventsToo;
        }
        
        #endregion Construction      

        #region Operations

        /// <summary>
        ///     Returns associated handler instance
        /// </summary>
        public Delegate Handler
        {
            get {return _handler;}
        }

        /// <summary>
        ///     Returns HandledEventsToo Flag
        /// </summary>
        public bool InvokeHandledEventsToo
        {
            get {return _handledEventsToo;}
        }
        
        // Invokes handler instance as per specified 
        // invocation preferences
        internal void InvokeHandler(object target, RoutedEventArgs routedEventArgs)
        {
            if ((routedEventArgs.Handled == false) || (_handledEventsToo == true))
            {
                if (_handler is RoutedEventHandler)
                {
                    // Generic RoutedEventHandler is called directly here since
                    //  we don't need the InvokeEventHandler override to cast to
                    //  the proper type - we know what it is.
                    ((RoutedEventHandler)_handler)(target, routedEventArgs);
                }
                else
                {
                    // NOTE: Cannot call protected method InvokeEventHandler directly
                    routedEventArgs.InvokeHandler(_handler, target);
                }
            }
        }

        /// <summary>
        ///     Is the given object equivalent to the current one
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is RoutedEventHandlerInfo))
                return false;

            return Equals((RoutedEventHandlerInfo)obj);
        }

        /// <summary>
        ///     Is the given RoutedEventHandlerInfo equals the current
        /// </summary>
        public bool Equals(RoutedEventHandlerInfo handlerInfo)
        {
            return _handler == handlerInfo._handler && _handledEventsToo == handlerInfo._handledEventsToo;
        }

        /// <summary>
        ///     Serves as a hash function for a particular type, suitable for use in 
        ///     hashing algorithms and data structures like a hash table
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Equals operator overload
        /// </summary>
        public static bool operator== (RoutedEventHandlerInfo handlerInfo1, RoutedEventHandlerInfo handlerInfo2)
        {
            return handlerInfo1.Equals(handlerInfo2);
        }

        /// <summary>
        ///     NotEquals operator overload
        /// </summary>
        public static bool operator!= (RoutedEventHandlerInfo handlerInfo1, RoutedEventHandlerInfo handlerInfo2)
        {
            return !handlerInfo1.Equals(handlerInfo2);
        }

        /// <summary>
        ///     Cleanup all the references within the data
        /// </summary>
        /*
        Commented out to avoid "uncalled private code" fxcop violation
        internal void Clear()
        {
            _handler = null;
            _handledEventsToo = false;
        }
        */
        
        #endregion Operations

        #region Data

        private Delegate _handler;
        private bool _handledEventsToo;

        #endregion Data
    }
}

