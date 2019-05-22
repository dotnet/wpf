// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Expression to evaluate a ResourceReference.
//
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Markup;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    ///     Expression to evaluate a ResourceReference
    /// </summary>
    [TypeConverter(typeof(ResourceReferenceExpressionConverter))]
    internal class ResourceReferenceExpression : Expression
    {
        /// <summary>
        ///     Constructor for ResourceReferenceExpression
        /// </summary>
        /// <param name="resourceKey">
        ///     Name of the resource being referenced
        /// </param>
        public ResourceReferenceExpression(object resourceKey)
        {
            _resourceKey = resourceKey;
        }

        /// <summary>
        ///     List of sources of the ResourceReferenceExpression
        /// </summary>
        /// <returns>Sources list</returns>
        internal override DependencySource[] GetSources()
        {
            return null;
        }

        /// <summary>
        ///     Called to evaluate the ResourceReferenceExpression value
        /// </summary>
        /// <param name="d">DependencyObject being queried</param>
        /// <param name="dp">Property being queried</param>
        /// <returns>Computed value. Unset if unavailable.</returns>
        internal override object GetValue(DependencyObject d, DependencyProperty dp)
        {
            if (d == null)
            {
                throw new ArgumentNullException("d");
            }
            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            // If the cached value is valid then return it
            if (ReadInternalState(InternalState.HasCachedResourceValue) == true)
                return _cachedResourceValue;

            object source;
            return GetRawValue(d, out source, dp);
        }


        // Clone a copy of this expression (this is used by Freezable.Copy)
        internal override Expression Copy( DependencyObject targetObject, DependencyProperty targetDP )
        {
            return new ResourceReferenceExpression( ResourceKey );
        }


        /// <summary>
        ///     Called to evaluate the ResourceReferenceExpression value
        /// </summary>
        /// <param name="d">DependencyObject being queried</param>
        /// <param name="source">Source object that the resource is found on</param>
        /// <param name="dp">DependencyProperty</param>
        /// <returns>Computed value. Unset if unavailable.</returns>
        /// <remarks>
        /// This routine has been separated from the above GetValue call because it is
        /// invoked by the ResourceReferenceExpressionConverter during serialization.
        /// </remarks>
        internal object GetRawValue(DependencyObject d, out object source, DependencyProperty dp)
        {
            // Find the mentor node to invoke FindResource on. For example
            // <Button>
            //   <Button.Background>
            //     <SolidColorBrush Color="{DynamicResource MyColor}" />
            //   </Button.Background>
            // </Button
            // Button is the mentor for the ResourceReference on SolidColorBrush
            if (ReadInternalState(InternalState.IsMentorCacheValid) == false)
            {
                // Find the mentor by walking up the InheritanceContext
                // links and update the cache
                _mentorCache = Helper.FindMentor(d);
                WriteInternalState(InternalState.IsMentorCacheValid, true);

                // If the mentor is different from the targetObject as will be the case
                // in the example described above, make sure you listen for ResourcesChanged
                // event on the mentor. That way you will be notified of ResourceDictionary
                // changes as well as logical tree changes
                if (_mentorCache != null && _mentorCache != _targetObject)
                {
                    Debug.Assert(_targetObject == d, "TargetObject that this expression is attached to must be the same as the one on which its value is being queried");

                    FrameworkElement mentorFE;
                    FrameworkContentElement mentorFCE;
                    Helper.DowncastToFEorFCE(_mentorCache, out mentorFE, out mentorFCE, true);

                    if (mentorFE != null)
                    {
                        mentorFE.ResourcesChanged += new EventHandler(InvalidateExpressionValue);
                    }
                    else
                    {
                        mentorFCE.ResourcesChanged += new EventHandler(InvalidateExpressionValue);
                    }
                }
            }

            object resource;
            if (_mentorCache != null)
            {
                FrameworkElement fe;
                FrameworkContentElement fce;
                Helper.DowncastToFEorFCE(_mentorCache, out fe, out fce, true /*throwIfNeither*/);

                // If there is a mentor do a FindResource call starting at that node
                resource = FrameworkElement.FindResourceInternal(fe,
                                                                 fce,
                                                                 dp,
                                                                 _resourceKey,
                                                                 null,  // unlinkedParent
                                                                 true,  // allowDeferredResourceReference
                                                                 false, // mustReturnDeferredResourceReference
                                                                 null,  // boundaryElement
                                                                 false, // disableThrowOnResourceFailure
                                                                 out source);
            }
            else
            {
                // If there is no mentor then simply search the App and the Themes for the right resource
                resource = FrameworkElement.FindResourceFromAppOrSystem(_resourceKey,
                                                                        out source,
                                                                        false, // disableThrowOnResourceFailure
                                                                        true,  // allowDeferredResourceReference
                                                                        false  /* mustReturnDeferredResourceReference*/);
            }

            if (resource == null)
            {
                // Assuming that null means the value doesn't exist in the resources section
                resource = DependencyProperty.UnsetValue;
            }

            // Update the cached values with this resource instance
            _cachedResourceValue = resource;
            WriteInternalState(InternalState.HasCachedResourceValue, true);

            object effectiveResource = resource;
            DeferredResourceReference deferredResourceReference = resource as DeferredResourceReference;
            if (deferredResourceReference != null)
            {
                if (deferredResourceReference.IsInflated)
                {
                    // use the inflated value in the Freezable test below
                    effectiveResource = deferredResourceReference.Value as Freezable;
                }
                else
                {
                    // listen for inflation, so we can do the Freezable test then
                    if (!ReadInternalState(InternalState.IsListeningForInflated))
                    {
                        deferredResourceReference.AddInflatedListener(this);
                        WriteInternalState(InternalState.IsListeningForInflated, true);
                    }
                }
            }

            ListenForFreezableChanges(effectiveResource);

            // Return the resource
            return resource;
        }

        /// <summary>
        ///     Allows ResourceReferenceExpression to store set values
        /// </summary>
        /// <param name="d">DependencyObject being set</param>
        /// <param name="dp">Property being set</param>
        /// <param name="value">Value being set</param>
        /// <returns>true if ResourceReferenceExpression handled storing of the value</returns>
        internal override bool SetValue(DependencyObject d, DependencyProperty dp, object value)
        {
            return false;
        }

        /// <summary>
        ///     Notification that the ResourceReferenceExpression has been set as a property's value
        /// </summary>
        /// <param name="d">DependencyObject being set</param>
        /// <param name="dp">Property being set</param>
        internal override void OnAttach(DependencyObject d, DependencyProperty dp)
        {
            _targetObject = d;
            _targetProperty = dp;

            FrameworkObject fo = new FrameworkObject(_targetObject);

            fo.HasResourceReference = true;

            if (!fo.IsValid)
            {
                // Listen for the InheritanceContextChanged event on the target node,
                // so that if this context hierarchy changes we can re-evaluate this expression.
                _targetObject.InheritanceContextChanged += new EventHandler(InvalidateExpressionValue);
            }
        }

        /// <summary>
        ///     Notification that the ResourceReferenceExpression has been removed as a property's value
        /// </summary>
        /// <param name="d">DependencyObject being cleared</param>
        /// <param name="dp">Property being cleared</param>
        internal override void OnDetach(DependencyObject d, DependencyProperty dp)
        {
            // Invalidate all the caches
            InvalidateMentorCache();

            if (!(_targetObject is FrameworkElement) && !(_targetObject is FrameworkContentElement))
            {
                // Stop listening for the InheritanceContextChanged event on the target node
                _targetObject.InheritanceContextChanged -= new EventHandler(InvalidateExpressionValue);
            }

            _targetObject = null;
            _targetProperty = null;
            // RemoveChangedHandler will have already been called via InvalidateMentorCache().
            _weakContainerRRE = null;
        }

        /// <summary>
        ///     Key used to lookup the resource
        /// </summary>
        public object ResourceKey
        {
            get { return _resourceKey; }
        }

        /// <summary>
        /// This method is called when the cached value of the resource has
        /// been invalidated.  E.g. after a new Resources property is set somewhere
        /// in the ancestory.
        /// </summary>
        private void InvalidateCacheValue()
        {
            object resource = _cachedResourceValue;

            // If the old value was a DeferredResourceReference, it should be
            // removed from its Dictionary's list to avoid a leak (bug 1624666).
            DeferredResourceReference deferredResourceReference = _cachedResourceValue as DeferredResourceReference;
            if (deferredResourceReference != null)
            {
                if (deferredResourceReference.IsInflated)
                {
                    // use the inflated value for the Freezable test below
                    resource = deferredResourceReference.Value;
                }
                else
                {
                    // stop listening for the Inflated event
                    if (ReadInternalState(InternalState.IsListeningForInflated))
                    {
                        deferredResourceReference.RemoveInflatedListener(this);
                        WriteInternalState(InternalState.IsListeningForInflated, false);
                    }
                }

                deferredResourceReference.RemoveFromDictionary();
            }

            StopListeningForFreezableChanges(resource);

            _cachedResourceValue = null;
            WriteInternalState(InternalState.HasCachedResourceValue, false);
        }

        /// <summary>
        ///     This method is called to invalidate all the cached values held in
        ///     this expression. This is called under the following 3 scenarios
        ///     1. InheritanceContext changes
        ///     2. Logical tree changes
        ///     3. ResourceDictionary changes
        ///     This call is more pervasive than the InvalidateCacheValue method
        /// </summary>
        private void InvalidateMentorCache()
        {
            if (ReadInternalState(InternalState.IsMentorCacheValid) == true)
            {
                if (_mentorCache != null)
                {
                    if (_mentorCache != _targetObject)
                    {
                        FrameworkElement mentorFE;
                        FrameworkContentElement mentorFCE;
                        Helper.DowncastToFEorFCE(_mentorCache, out mentorFE, out mentorFCE, true);

                        // Your mentor is about to change, make sure you detach handlers for
                        // the events that you were listening on the old mentor
                        if (mentorFE != null)
                        {
                            mentorFE.ResourcesChanged -= new EventHandler(InvalidateExpressionValue);
                        }
                        else
                        {
                            mentorFCE.ResourcesChanged -= new EventHandler(InvalidateExpressionValue);
                        }
                    }

                    // Drop the mentor cache
                    _mentorCache = null;
                }

                // Mark the cache invalid
                WriteInternalState(InternalState.IsMentorCacheValid, false);
            }

            // Invalidate the cached value of the expression
            InvalidateCacheValue();
        }

        /// <summary>
        ///     This event handler is called to invalidate the cached value held in
        ///     this expression. This is called under the following 3 scenarios
        ///     1. InheritanceContext changes
        ///     2. Logical tree changes
        ///     3. ResourceDictionary changes
        /// </summary>
        internal void InvalidateExpressionValue(object sender, EventArgs e)
        {
            // VS has a scenario where a TreeWalk invalidates all reference expressions on a DependencyObject.
            // If there is a dependency between RRE's, 
            // invalidating one RRE could cause _targetObject to be null on the other RRE. Hence this check. 
            if (_targetObject == null)
            {
                return;
            }

            ResourcesChangedEventArgs args = e as ResourcesChangedEventArgs;
            if (args != null)
            {
                ResourcesChangeInfo info = args.Info;
                if (!info.IsTreeChange)
                {
                    // This will happen when
                    // 1. Theme changes
                    // 2. Entire ResourceDictionary in the ancestry changes
                    // 3. Single entry in a ResourceDictionary in the ancestry is changed
                    // In all of the above cases it is sufficient to re-evaluate the cache
                    // value alone. The mentor relation ships stay the same.
                    InvalidateCacheValue();
                }
                else
                {
                    // This is the case of a logical tree change and hence we need to
                    // re-evaluate both the mentor and the cached value.
                    InvalidateMentorCache();
                }
            }
            else
            {
                // There is no information provided by the EventArgs. Hence we
                // pessimistically invalidate both the mentor and the cached value.
                // This code path will execute when the InheritanceContext changes.
                InvalidateMentorCache();
            }

            InvalidateTargetProperty(sender, e);
        }

        private void InvalidateTargetProperty(object sender, EventArgs e)
        {
            _targetObject.InvalidateProperty(_targetProperty);
        }

        private void InvalidateTargetSubProperty(object sender, EventArgs e)
        {
            _targetObject.NotifySubPropertyChange(_targetProperty);
        }

        private void ListenForFreezableChanges(object resource)
        {
            if (!ReadInternalState(InternalState.IsListeningForFreezableChanges))
            {
                // If this value is an unfrozen Freezable object, we need
                //  to listen to its changed event in order to properly update
                //  the cache.
                Freezable resourceAsFreezable = resource as Freezable;
                if( resourceAsFreezable != null && !resourceAsFreezable.IsFrozen )
                {
                    if (_weakContainerRRE == null)
                    {
                        _weakContainerRRE = new ResourceReferenceExpressionWeakContainer(this);
                    }
                    
                    // Hook up the event to the weak container to prevent memory leaks (Bug436021)
                    _weakContainerRRE.AddChangedHandler(resourceAsFreezable);
                    WriteInternalState(InternalState.IsListeningForFreezableChanges, true);
                }
            }
        }

        private void StopListeningForFreezableChanges(object resource)
        {
            if (ReadInternalState(InternalState.IsListeningForFreezableChanges))
            {
                // If the old value was an unfrozen Freezable object, we need
                //  to stop listening to its changed event.  If the old value wasn't
                //  frozen (hence we attached an listener) but has been frozen
                //  since then, the change handler we had attached was already
                //  discarded during the freeze so we don't care here.
                Freezable resourceAsFreezable = resource as Freezable;
                if (resourceAsFreezable != null && _weakContainerRRE != null)
                {
                    if (!resourceAsFreezable.IsFrozen)
                    {
                        _weakContainerRRE.RemoveChangedHandler();
                    }
                    else
                    {
                        // Resource is frozen so we can discard the weak reference.
                        _weakContainerRRE = null;
                    }
                }

                // It is possible that a freezable was unfrozen during the call to ListForFreezableChanges 
                // but was frozen before the call to StopListeningForFreezableChanges
                WriteInternalState(InternalState.IsListeningForFreezableChanges, false);
            }
        }

        // when a deferred resource reference is inflated, the value may need extra
        // work
        internal void OnDeferredResourceInflated(DeferredResourceReference deferredResourceReference)
        {
            if (ReadInternalState(InternalState.IsListeningForInflated))
            {
                // once the value is inflated, stop listening for the event
                deferredResourceReference.RemoveInflatedListener(this);
                WriteInternalState(InternalState.IsListeningForInflated, false);
            }

            ListenForFreezableChanges(deferredResourceReference.Value);
        }

        // Extracts the required flag and returns
        // bool to indicate if it is set or unset
        private bool ReadInternalState(InternalState reqFlag)
        {
            return (_state & reqFlag) != 0;
        }

        // Sets or Unsets the required flag based on
        // the bool argument
        private void WriteInternalState(InternalState reqFlag, bool set)
        {
            if (set)
            {
                _state |= reqFlag;
            }
            else
            {
                _state &= (~reqFlag);
            }
        }

        private object _resourceKey; // Name of the resource being referenced by this expression

        // Cached value and a dirty bit.  See GetValue.
        private object _cachedResourceValue;

        // Used to find the value for this expression when it is set on a non-FE/FCE.
        // The mentor is the FE/FCE that the FindResource method is invoked on.
        private DependencyObject _mentorCache;

        // Used by the change listener to fire invalidation.
        private DependencyObject _targetObject;
        private DependencyProperty _targetProperty;

        // Bit Fields used to store boolean flags
        private InternalState _state = InternalState.Default;  // this is a byte (see def'n)

        private ResourceReferenceExpressionWeakContainer _weakContainerRRE = null;

        /// <summary>
        /// This enum represents the internal state of the RRE.
        /// Additional bools should be coalesced into this enum.
        /// </summary>
        [Flags]
        private enum InternalState : byte
        {
            Default                       = 0x00,
            HasCachedResourceValue        = 0x01,
            IsMentorCacheValid            = 0x02,
            DisableThrowOnResourceFailure = 0x04,
            IsListeningForFreezableChanges= 0x08,
            IsListeningForInflated        = 0x10,
        }

        #region ResourceReferenceExpressionWeakContainer

        /// <summary>
        /// ResourceReferenceExpressionWeakContainer handles the Freezable.Changed event 
        /// without holding a strong reference to ResourceReferenceExpression. 
        /// </summary>
        private class ResourceReferenceExpressionWeakContainer : WeakReference
        {
            public ResourceReferenceExpressionWeakContainer(ResourceReferenceExpression target)
                : base(target) {}

            private void InvalidateTargetSubProperty(object sender, EventArgs args)
            {
                ResourceReferenceExpression expression = (ResourceReferenceExpression)Target;
                if (expression != null)
                {
                    expression.InvalidateTargetSubProperty(sender, args);
                }
                else
                {
                    RemoveChangedHandler();
                }
            }

            public void AddChangedHandler(Freezable resource)
            {
                // If _resource already exists, unhook the event handler.
                if (_resource != null)
                {
                    RemoveChangedHandler();
                }

                _resource = resource;
            
                Debug.Assert(!_resource.IsFrozen);
                _resource.Changed += new EventHandler(this.InvalidateTargetSubProperty);
            }

            public void RemoveChangedHandler()
            {
                if (!_resource.IsFrozen)
                {
                    _resource.Changed -= new EventHandler(this.InvalidateTargetSubProperty);
                    _resource = null;
                }
            }

            private Freezable _resource;
        }
        #endregion 
    }

    /// <summary>
    ///     These EventArgs are used to pass additional
    ///     information during a ResourcesChanged event
    /// </summary>
    internal class ResourcesChangedEventArgs : EventArgs
    {
        internal ResourcesChangedEventArgs(ResourcesChangeInfo info)
        {
            _info = info;
        }

        internal ResourcesChangeInfo Info
        {
            get { return _info; }
        }

        private ResourcesChangeInfo _info;
    }
}

