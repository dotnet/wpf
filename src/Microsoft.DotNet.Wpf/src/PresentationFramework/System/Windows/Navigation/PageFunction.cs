// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//             PageFunctions are the "atom" of Structured Navigation, 
//             this is the base element class from which the developer derives 
//             from to enable returning results to a caller.  
//
using System;
using System.Diagnostics;
using System.Windows.Controls;

using MS.Internal.AppModel;

namespace System.Windows.Navigation
{
    ///<summary>
    ///     Abstract base class for the generic PageFunction class.
    ///</summary>
    public abstract class PageFunctionBase : Page
    {
        #region Constructors

        ///<summary>
        ///    PageFunctionBase constructor
        ///</summary>
        protected PageFunctionBase()
        {
            // Make a new GUID for this ID, set the parent ID to 0-0-0-0-0
            PageFunctionId = Guid.NewGuid();
            ParentPageFunctionId = Guid.Empty;
        }
        #endregion Constructors

        #region Public Properties
        ///<summary>
        ///     When set to true, the pagefunction with this property set and all child page functions will get removed from the journal. 
        ///     This allows easy building of "transactioned" UI. If a series of steps have been completed, and it doesn't make sense to hit the back button and submit that transaction again, 
        ///     setting this property enables those series of steps to be removed from the journal. 
        ///</summary>
        ///<remarks>
        ///     Examples abound of "transactioned UI" buying a book from an ecommerce application, selling stock, creating a user account. 
        ///</remarks>
        public bool RemoveFromJournal
        {
            get
            {
                return _fRemoveFromJournal;
            }
            set
            {
                _fRemoveFromJournal = value;
            }
        }
        
        #endregion Public Properties

        #region Protected Methods
        ///<summary>
        ///     This method is called when a PageFunction is first navigated to. 
        ///     It is not called when a Pagefunction resumes. 
        ///</summary>
        ///<remarks>
        ///     Typically a developer will write some initialization code in their Start method, 
        ///     or they may decide to invoke a child PageFunction. 
        ///</remarks>
        protected virtual void Start()
        {
        }

        /*
        /// <summary>
        ///     To be used by derived classes to raise strongly types return events. Must be overriden.
        /// </summary>
        // protected abstract void RaiseTypedReturnEvent(Delegate d, object returnEventArgs);
        */

        #endregion Protected Methods        

        #region Internal Methods
        // An internal method which is used to invoke the protected Start() method by
        // other parts of the PageFunction code base.
        internal void CallStart()
        {
            Start();
        }

        ///<summary>
        ///     A Pagefunction calls this method to signal that it is completed and the ReturnEventArgs
        ///     will be supplied to the listener on Return ( and that listener will be unsuspended). 
        ///</summary>        
        internal void _OnReturnUnTyped(object o)
        {
            if (_finish != null)
            {
                _finish(this, o);
            }
        }

        internal void _AddEventHandler(Delegate d)
        {
            // This is where the parent-child relationship is established. If 
            // PageFunction A attaches one of its methods to PageFunction B's
            // Return event, then A must be B's parent.
            PageFunctionBase parent = d.Target as PageFunctionBase;
            if (parent != null)
            {
                ParentPageFunctionId = parent.PageFunctionId;
            }
            _returnHandler = Delegate.Combine(_returnHandler, d);
        }

        internal void _RemoveEventHandler(Delegate d)
        {
            _returnHandler = Delegate.Remove(_returnHandler, d);
        }

        internal void _DetachEvents()
        {
            _returnHandler = null;
        }

        //
        // Raise the event on the _returnHandler
        //
        internal void _OnFinish(object returnEventArgs)
        {
            RaiseTypedEventArgs args = new RaiseTypedEventArgs(_returnHandler, returnEventArgs);
            RaiseTypedEvent(this, args);
            //RaiseTypedReturnEvent(_returnHandler, returnEventArgs);
        }

        #endregion Internal Methods

        #region Internal Properties
        /// <summary>
        /// This is the GUID for this PageFunction. It will be used as its childrens'
        /// ParentPageFunctionId.
        /// </summary>
        internal Guid PageFunctionId
        {
            get { return _pageFunctionId; }
            set { _pageFunctionId = value; }
        }

        /// <summary>
        /// If this PageFunction is a child PageFunction, this will be the GUID of its
        /// parent PageFunction. If this is Guid.Empty, then the parent is NOT a
        /// PageFunction.
        /// </summary>
        internal Guid ParentPageFunctionId
        {
            get { return _parentPageFunctionId; }
            set { _parentPageFunctionId = value; }
        }

        internal Delegate _Return
        {
            get { return _returnHandler; }
        }

        /// <summary>
        /// Whether the PF is being resumed after a child PF returns OR due to journal navigation
        /// (Go Back/Fwd)
        /// </summary>
        internal bool _Resume
        {
            get { return _resume; }
            set { _resume = value; }
        }

        internal ReturnEventSaver _Saver
        {
            get { return _saverInfo; }
            set { _saverInfo = value; }
        }

        #endregion Internal Properties

        #region Internal Events
        //
        // The FinishEventHandler is used to communicate between the NavigationService and the ending Pagefunction
        //
        internal FinishEventHandler FinishHandler
        {
            get { return _finish; }
            set { _finish = value; }
        }
        internal event EventToRaiseTypedEvent RaiseTypedEvent;
        #endregion

        #region Private Fields
        private Guid _pageFunctionId;
        private Guid _parentPageFunctionId;
        
        private bool _fRemoveFromJournal = true; // new default, to make PFs behave more like functions

        private bool _resume;

        private ReturnEventSaver _saverInfo;                     // keeps track of Parent caller's Return Event Info
        private FinishEventHandler _finish;
        private Delegate _returnHandler;          // the delegate for the Return event        
        #endregion Private Fields
    }

    internal delegate void EventToRaiseTypedEvent(PageFunctionBase sender, RaiseTypedEventArgs args);
    internal class RaiseTypedEventArgs : System.EventArgs
    {
        internal RaiseTypedEventArgs(Delegate d, object o)
        {
            D = d;
            O = o;
        }
        internal Delegate D;
        internal Object O;
    }

    ///<summary>
    ///     A callback handler used to receive a ReturnEventArgs of type T
    ///</summary>
    public delegate void ReturnEventHandler<T>(object sender, ReturnEventArgs<T> e);

    ///<summary>
    ///     PageFunctions are the "atom" of Structured Navigation, 
    ///     this is the base element class from which the developer derives 
    ///     from to enable returning results to a caller.  
    ///</summary>
    /// <remarks>
    /// Right now Pagefunctions are non-cls compliant owing to their use of generics. It is expected in the LH timeframe that
    /// all CLS languages will support generics. 
    /// </remarks>    
    public class PageFunction<T> : PageFunctionBase
    {
        #region Constructors
        ///<summary>
        ///    Pagefunction constructor
        ///</summary>
        public PageFunction()
        {
            RaiseTypedEvent += new EventToRaiseTypedEvent(RaiseTypedReturnEvent);
        }
        #endregion Constructors

        #region Protected Methods
        ///<summary>
        ///     A Pagefunction calls this method to signal that it is completed and the ReturnEventArgs
        ///     will be supplied to the listener on Return ( and that listener will be unsuspended). 
        ///</summary>        
        protected virtual void OnReturn(ReturnEventArgs<T> e)
        {
            _OnReturnUnTyped(e);
        }

        ///<summary>
        /// Used to raise a strongly typed return event. Sealed since nobody should have the need to override as
        /// all derived types of this generic type will automatically get the strongly typed version from this
        /// generic version.
        ///</summary>
        internal void RaiseTypedReturnEvent(PageFunctionBase b, RaiseTypedEventArgs args)
        {
            Delegate d = args.D;
            object returnEventArgs = args.O;

            if (d != null)
            {
                ReturnEventArgs<T> ra = returnEventArgs as ReturnEventArgs<T>;

                Debug.Assert((returnEventArgs == null) || (ra != null));

                ReturnEventHandler<T> eh = d as ReturnEventHandler<T>;

                Debug.Assert(eh != null);

                eh(this, ra);
            }
        }
        #endregion Protected Methods

        #region Public Events

        ///<summary>
        ///     This is the event to which a caller will listen to get results returned. 
        ///</summary> 
        public event ReturnEventHandler<T> Return
        {
            // We need to provide a way to surface out
            // the listeners to the event
            // So we override add/remove and keep track of it here. 
            add
            {
                _AddEventHandler((Delegate)value);
            }
            remove
            {
                _RemoveEventHandler((Delegate)value);
            }
        }

        #endregion Public Events
    }
}
