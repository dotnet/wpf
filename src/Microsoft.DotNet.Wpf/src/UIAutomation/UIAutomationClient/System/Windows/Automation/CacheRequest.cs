// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class that describes the data to be pre-fected in Automation
//              element operations, and manange the current request.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Automation;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Specified type of reference to use when returning AutomationElements
    /// </summary>
    /// <remarks>
    /// AutomationElementMode.Full is the default, and specified that returned AutomationElements
    /// contain a full reference to the underlying UI.
    /// 
    /// AutomationElementMode.None specifies taht the returned AutomationElements have no
    /// reference to the underlying UI, and contain only cached information.
    /// 
    /// Certain operations on AutomationElements, such as GetCurrentProperty
    /// or SetFocus require a full reference; attempting to perform these on an
    /// AutomationElement that has AutomationElementMode.None will result in an
    /// InvalidOperationException being thrown.
    /// 
    /// Using AutomationElementMode.None can be more efficient when only properties are needed,
    /// as it avoids the overhead involved in setting up full references.
    /// </remarks>
#if (INTERNAL_COMPILE)
    internal enum AutomationElementMode
#else
    public enum AutomationElementMode
#endif
    {
        /// <summary>
        /// Specifies that returned AutomationElements have no reference to the
        /// underlying UI, and contain only cached information.
        /// </summary>
        None,

        /// <summary>
        /// Specifies that returned AutomationElements have a full reference to the
        /// underlying UI.
        /// </summary>
        Full
    }

    // Implementation notes:
    //
    // CacheRequest is the user-facing class that is used to build up
    // cache requests, using Add and the other properties. When activated,
    // the data is gathered into a UiaCacheRequest instance, which is
    // immutable - these are what the rest of UIA uses internally.
    //
    // The default cache request - which appears to be always at the bottom
    // of the stack and which cannot be moved - is not actually part of the
    // stack. Instead, current returns it whever the stack is empty.

    /// <summary>
    /// Class used to specify the properties and patterns that should be
    /// prefetched by UIAutomation when returning AutomationElements.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class CacheRequest
#else
    public sealed class CacheRequest
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Create a new CacheRequest with default values.
        /// </summary>
        /// <remarks>
        /// A Thread's current CacheRequest determins which properties,
        /// patterns and relative elements - if any - are pre-fetched by
        /// AutomationElement.
        /// 
        /// A default cache request works on the Control view of the tree,
        /// and does not prefetch any properties or patterns.
        /// 
        /// Use .Push or .Activate to make the CacheRequest the current active
        /// CacheRequest for the current thread.
        /// </remarks>
        public CacheRequest()
        {
            _instanceLock = new object();

            _viewCondition = Automation.ControlViewCondition;
            _scope = TreeScope.Element;
            _properties = new ArrayList();
            _patterns = new ArrayList();
            _automationElementMode = AutomationElementMode.Full;

            // We always want RuntimeID to be available...
            _properties.Add(AutomationElement.RuntimeIdProperty);
            _uiaCacheRequest = DefaultUiaCacheRequest;
        }

        // Private ctor used by Clone()
        private CacheRequest( Condition viewCondition,
                              TreeScope scope,
                              ArrayList properties,
                              ArrayList patterns,
                              AutomationElementMode automationElementMode,
                              UiaCoreApi.UiaCacheRequest uiaCacheRequest)
        {
            _instanceLock = new object();

            _viewCondition = viewCondition;
            _scope = scope;
            _properties = properties;
            _patterns = patterns;
            _automationElementMode = automationElementMode;
            _uiaCacheRequest = uiaCacheRequest;
        }
        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Push this CacheRequest onto this thread's stack of CacheRequests,
        /// making it the current CacheRequest for this thread.
        /// </summary>
        /// <remarks>
        /// Use Pop to remove this CacheRequest from the stack, making the previously
        /// active CacheRequest active again. CacheRequests can be pushed multiple times,
        /// and on multiple threads; but for each thread, must be popped off in the
        /// same order as they were pushed.
        /// 
        /// A CacheRequest cannot be modified while it is pushed on any thread; attempting
        /// to modify it will generate an InvalidOperationException.
        /// </remarks>
        public void Push()
        {
            // pushing multiple times is legal,
            // and pushing on different threads is also legal,
            // so no preconditions to check for here.

            AutomationProperty[] propertyArray = (AutomationProperty[])_properties.ToArray(typeof(AutomationProperty));
            AutomationPattern[] patternArray = (AutomationPattern[])_patterns.ToArray(typeof(AutomationPattern));

            // _threadStack is thread local storage (TLS) based, so can be
            // accessed outside of the lock.
            if (_threadStack == null)
            {
                _threadStack = new Stack();
            }

            _threadStack.Push(this);

            lock (_instanceLock)
            {
                _refCount++;

                // Generate a new UiaCacheRequest
                if (_uiaCacheRequest == null)
                {
                    _uiaCacheRequest = new UiaCoreApi.UiaCacheRequest(_viewCondition, _scope, propertyArray, patternArray, _automationElementMode);
                }
            }
        }

        /// <summary>
        /// Pop this CacheRequest from the current thread's stack of CacheRequests,
        /// restoring the previously active CacheRequest.
        /// </summary>
        /// <remarks>
        /// Only the currently active CacheRequest can be popped, attempting to pop
        /// a CacheRequest which is not the current one will result in an InvalidOperation
        /// Exception.
        ///
        /// The CacheRequest stack initially contains a default CacheRequest, which
        /// cannot be popped off the stack.
        /// </remarks>
        public void Pop()
        {
            // ensure that this is top of stack
            // (no lock needed here, since this is per-thread state)
            if (_threadStack == null || _threadStack.Count == 0 || _threadStack.Peek() != this)
            {
                throw new InvalidOperationException(SR.Get(SRID.CacheReqestCanOnlyPopTop));
            }

            _threadStack.Pop();

            lock (_instanceLock)
            {
                _refCount--;
            }
        }

        /// <summary>
        /// Make this the currenly active CacheRequest.
        /// </summary>
        /// <remarks>
        /// Returns an IDisposable which should be disposed
        /// when finished using this CacheRequest to deactivate it.
        /// This method is designed for use within a 'using' clause.
        /// </remarks>
        public IDisposable Activate()
        {
            Push();
            return new CacheRequestActivation(this);
        }

        /// <summary>
        /// Clone this CacheRequest
        /// </summary>
        /// <remarks>
        /// The returned CacheRequest contains the same request information, but is not
        /// pushed on the state of any thread.
        /// </remarks>
        public CacheRequest Clone()
        {
            // New copy contains only temp state, not any locking state (_refCount)
            return new CacheRequest(_viewCondition, _scope, (ArrayList)_properties.Clone(), (ArrayList)_patterns.Clone(), _automationElementMode, _uiaCacheRequest);
        }

        /// <summary>
        /// Add an AutomationProperty to this CacheRequest
        /// </summary>
        /// <param name="property">The identifier of the property to add to this CacheRequest</param>
        public void Add(AutomationProperty property)
        {
            Misc.ValidateArgumentNonNull(property, "property");
            lock (_instanceLock)
            {
                CheckAccess();
                if (!_properties.Contains(property))
                {
                    _properties.Add(property);
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Add an AutomationPattern to this CacheRequest
        /// </summary>
        /// <param name="pattern">The identifier of the pattern to add to this CacheRequest</param>
        public void Add(AutomationPattern pattern)
        {
            Misc.ValidateArgumentNonNull(pattern, "pattern");
            lock (_instanceLock)
            {
                CheckAccess();
                if (!_patterns.Contains(pattern))
                {
                    _patterns.Add(pattern);
                    Invalidate();
                }
            }
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Indicate which nodes should be pre-fetched
        /// </summary>
        /// <remarks>
        /// At least one or more of of TreeScope.Element, TreeScope.Children or
        /// TreeScope.Descendants must be specified.
        /// 
        /// TreeScope.Parent and TreeScope.Ancestors are not supported.
        /// </remarks>
        public TreeScope TreeScope
        {
            get
            {
                return _scope;
            }
            
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException(SR.Get(SRID.TreeScopeNeedAtLeastOne));
                }

                if ((value & ~(TreeScope.Element | TreeScope.Children | TreeScope.Descendants)) != 0)
                {
                    throw new ArgumentException(SR.Get(SRID.TreeScopeElementChildrenDescendantsOnly));
                }

                lock (_instanceLock)
                {
                    CheckAccess();
                    if (_scope != value)
                    {
                        _scope = value;
                        Invalidate();
                    }
                }
            }
        }


        /// <summary>
        /// Indicates the view to use when prefetching relative nodes
        /// </summary>
        /// <remarks>Defaults to Automation.ControlViewCondition</remarks>
        public Condition TreeFilter
        {
            get
            {
                return _viewCondition;
            }
            
            set
            {
                Misc.ValidateArgumentNonNull(value, "TreeFilter");
                lock (_instanceLock)
                {
                    CheckAccess();
                    if (_viewCondition != value)
                    {
                        _viewCondition = value;
                        Invalidate();
                    }
                }
            }
        }

        /// <summary>
        /// Specifies whether returned AutomationElements should contain
        /// full references to the underlying UI, or only cached information.
        /// </summary>
        public AutomationElementMode AutomationElementMode
        {
            get
            {
                return _automationElementMode;
            }

            set
            {
                lock (_instanceLock)
                {
                    CheckAccess();
                    if (_automationElementMode != value)
                    {
                        _automationElementMode = value;
                        Invalidate();
                    }
                }
            }
        }


        /// <summary>
        /// Return the most recent CacheRequest which has been activated
        /// by teh calling thread.
        /// </summary>
        public static CacheRequest Current
        {
            get
            {
                if ( _threadStack == null || _threadStack.Count == 0 )
                    return DefaultCacheRequest;

                return (CacheRequest)_threadStack.Peek();
            }
        }


        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        internal static UiaCoreApi.UiaCacheRequest DefaultUiaCacheRequest
        {
            get
            {
                if(_defaultUiaCacheRequest == null)
                {
                    _defaultUiaCacheRequest = new UiaCoreApi.UiaCacheRequest(Automation.ControlViewCondition, TreeScope.Element, new AutomationProperty[] { AutomationElement.RuntimeIdProperty }, new AutomationPattern[] { }, AutomationElementMode.Full);
                }
                return _defaultUiaCacheRequest;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal UiaCoreApi.UiaCacheRequest GetUiaCacheRequest()
        {
            if (_uiaCacheRequest == null)
            {
                AutomationProperty[] propertiesArray = (AutomationProperty[])_properties.ToArray(typeof(AutomationProperty));
                AutomationPattern[] patternsArray = (AutomationPattern[])_patterns.ToArray(typeof(AutomationPattern));
                lock (_instanceLock)
                {
                    _uiaCacheRequest = new UiaCoreApi.UiaCacheRequest(_viewCondition, _scope, propertiesArray, patternsArray, _automationElementMode);
                }
            }

            return _uiaCacheRequest;
        }

        static internal UiaCoreApi.UiaCacheRequest CurrentUiaCacheRequest
        {
            get
            {
                // No need to lock here, since this only uses thread state,
                // and the UiaCacheRequests are generated within a lock in Push.
                CacheRequest current = Current;
                return current._uiaCacheRequest;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Ensure that this CacheRequest isn't currently in use
        // Must be called within a lock(_instanceLock) to ensure
        // thread consistency
        void CheckAccess()
        {
            // Make sure this isn't being used by any thread's
            // CacheRequest stacks by using a refcount:
            // (Also check for defaultCacheRequest explicitly, since it
            // is never explicitly added to the stack)
            if (_refCount != 0 || this == DefaultCacheRequest)
            {
                throw new InvalidOperationException(SR.Get(SRID.CacheReqestCantModifyWhileActive));
            }
        }

        // Called when state changes - sets _uiaCacheRequest to null
        // to ensure that a clean one is generated next time Push is called
        void Invalidate()
        {
            _uiaCacheRequest = null;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        //--- Instance state ---

        // Current mutable state...
        Condition _viewCondition;
        TreeScope _scope;
        ArrayList _properties;
        ArrayList _patterns;
        AutomationElementMode _automationElementMode;

        // When we Push, the current state is bundled into this, which is
        // immutable. This is what the underlying requesting mechanism uses
        UiaCoreApi.UiaCacheRequest _uiaCacheRequest;

        // Used to track whether this instance is in use - inc'd on Push,
        // dec'd on Pop...
        int _refCount = 0;

        // Used to lock on this instance...
        object _instanceLock = null;

        //--- Per-Thread state ---

        // The stack of CacheRequests for this thread. Note that these need to be inited
        // on-demand, as static initialization does not work with [ThreadStatic] members -
        // it only applies to the first thread. Treat null as being the same as a stack with
        // just DefaultCacheRequest on it.
        [ThreadStatic]
        private static Stack _threadStack;

        //--- Global/Static state ---

        internal static readonly CacheRequest DefaultCacheRequest = new CacheRequest();

        internal static UiaCoreApi.UiaCacheRequest _defaultUiaCacheRequest;

        #endregion Private Fields
    }

    //------------------------------------------------------
    //
    //  Related utility classe
    //
    //------------------------------------------------------

    // Helper class returned by Access() - a using() block
    // will call Dispose() on this when it goes out of scope.
    internal class CacheRequestActivation : IDisposable
    {
        internal CacheRequestActivation(CacheRequest request)
        {
            _request = request;
        }

        public void Dispose()
        {
            Debug.Assert( _request != null );

            if( _request != null )
            {
                _request.Pop();
                _request = null;
            }
        }

        // No finalizer - usually Dispose is used with a finalizer,
        // but in this case, IDisposable is being used to manage scoping,
        // not ensure that resources are freed.

        private CacheRequest _request;
    }
}
