// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The Freezable class (plus the FreezableHelper class)
//              encompasses all of the Freezable pattern.


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;

using MS.Internal;                          // for Invariant
using MS.Internal.WindowsBase;              // FriendAccessAllowed
using MS.Utility;                           // FrugalList

namespace System.Windows
{
    /// <summary>
    /// The Freezable class encapsulates the Freezable pattern for DOs whose
    /// values can potentially be frozen.  See the Freezable documentation for
    /// more details.
    /// </summary>
    public abstract class Freezable : DependencyObject, ISealable
    {
#if DEBUG

        private static int _nextID = 1;

        private readonly int DebugID = _nextID++;

#endif

        #region Protected Constructors

        //------------------------------------------------------
        //
        //  Protected constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Construct a mutable Freezable.
        /// </summary>
        protected Freezable()
        {
            Debug.Assert(!Freezable_Frozen
                    && !Freezable_HasMultipleInheritanceContexts
                    && !(HasHandlers || HasContextInformation),
                    "Initial state is incorrect");
        }

        #endregion

        #region Public Methods

        //------------------------------------------------------
        //
        //  Public methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Makes a mutable deep base value clone of this Freezable.
        ///
        /// Caveat: Frozen default values will still be frozen afterwards
        /// </summary>
        /// <returns>A clone of the Freezable.</returns>
        public Freezable Clone()
        {
            ReadPreamble();

            Freezable clone = CreateInstance();

            clone.CloneCore(this);
            Debug_VerifyCloneCommon(/* original = */ this, /* clone = */ clone, /* isDeepClone = */ true);

            return clone;
        }

        /// <summary>
        /// Makes a mutable current value clone of this Freezable.
        ///
        /// Caveat: Frozen default values will still be frozen afterwards
        /// </summary>
        /// <returns>
        /// Returns a mutable deep copy of this Freezable that represents
        /// its current state.
        /// </returns>
        public Freezable CloneCurrentValue()
        {
            ReadPreamble();

            Freezable clone = CreateInstance();

            clone.CloneCurrentValueCore(this);

            // Freezable implementers who override CloneCurrentValueCore must ensure that
            // on creation the copy is not frozen.  Debug_VerifyCloneCommon checks for this,
            // among other things.
            Debug_VerifyCloneCommon(/* original = */ this, /* clone = */ clone, /* isDeepClone = */ true);

            return clone;
        }

        /// <summary>
        ///     Semantically equivalent to Freezable.Clone().Freeze() except that
        ///     GetAsFrozen avoids a copying any portions of the Freezable graph
        ///     which are already frozen.
        /// </summary>
        public Freezable GetAsFrozen()
        {
            ReadPreamble();

            if (IsFrozenInternal)
            {
                return this;
            }

            Freezable clone = CreateInstance();

            clone.GetAsFrozenCore(this);
            Debug_VerifyCloneCommon(/* original = */ this, /* clone = */ clone, /* isDeepClone = */ false);

            clone.Freeze();

            return clone;
        }


        /// <summary>
        ///     Semantically equivalent to Freezable.CloneCurrentValue().Freeze() except that
        ///     GetCurrentValueAsFrozen avoids a copying any portions of the Freezable graph
        ///     which are already frozen.
        /// </summary>
        public Freezable GetCurrentValueAsFrozen()
        {
            ReadPreamble();

            if (IsFrozenInternal)
            {
                return this;
            }

            Freezable clone = CreateInstance();

            clone.GetCurrentValueAsFrozenCore(this);
            Debug_VerifyCloneCommon(/* original = */ this, /* clone = */ clone, /* isDeepClone = */ false);

            clone.Freeze();

            return clone;
        }

        /// <summary>
        /// True if this Freezable can be frozen (by calling Freeze())
        /// </summary>
        public bool CanFreeze
        {
            get
            {
                return IsFrozenInternal || FreezeCore(/* isChecking = */ true);
            }
        }

        /// <summary>
        /// Does an in-place modification to make the object frozen. It is legal to
        /// call this on values that are already frozen.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This exception
        /// will be thrown if this Freezable can't be frozen. Use
        /// the CanFreeze property to detect this in advance.</exception>
        public void Freeze()
        {
            // Check up front that the operation will succeed before we begin.
            if (!CanFreeze)
            {
                throw new InvalidOperationException(SR.Get(SRID.Freezable_CantFreeze));
            }

            Freeze(/* isChecking = */ false);
        }

        #endregion

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Returns whether or not the Freezable is modifiable.  Attempts
        /// to set properties on an IsFrozen value result
        /// in exceptions being raised.
        /// </summary>
        public bool IsFrozen
        {
            get
            {
                ReadPreamble();

                return IsFrozenInternal;
            }
        }

        internal bool IsFrozenInternal
        {
            get
            {
                return Freezable_Frozen;
            }
        }

        #endregion
        #region Public Events

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        /// The Changed event is raised whenever something on this
        /// Freezable is modified.  Note that it is illegal to
        /// add or remove event handlers from a value with
        /// IsFrozen.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// An attempt was made to modify the Changed handler of
        /// a value with IsFrozen == true.
        /// </exception>
        public event EventHandler Changed
        {
            add
            {
                WritePreamble();

                if (value != null)
                {
                    ChangedInternal += value;
                }
}
            remove
            {
                WritePreamble();

                if (value != null)
                {
                    ChangedInternal -= value;
                }
            }
        }

        internal event EventHandler ChangedInternal
        {
            add
            {
                HandlerAdd(value);

                // Adding/Removing Changed handlers does not raise the Changed event.
                // Therefore we intentionally do not call WritePostscript().
            }

            remove
            {
                HandlerRemove(value);

                // Adding/Removing Changed handlers does not raise the Changed event.
                // Therefore we intentionally do not call WritePostscript().
            }
        }
        #endregion

        #region Protected Methods

        //------------------------------------------------------
        //
        //  Protected methods
        //
        //------------------------------------------------------

        /// <remarks>
        /// Override OnPropertyChanged so that we can fire the Freezable's Changed
        /// handler in response to a DP changing.
        /// </remarks>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // The property system will call us back when a SetValue is performed
            // on a Freezable.  The Freezable then walks it's contexts and causes
            // a subproperty invalidation on each context and fires any changed
            // handlers that have been registered.

            // When a default value is being promoted to a local value the sub property 
            // change that caused the promotion is being merged with the value promotion 
            // change. This fix was implemented for DevDivBug#108642. It is required to 
            // detect this case specially and propagate subproperty invalidations for it.

            if (!e.IsASubPropertyChange || e.OperationType == OperationType.ChangeMutableDefaultValue)
            {
                WritePostscript();
            }

            // OnPropertyChanged is called after the old inheritance context is
            // removed, but before the new one is added.
            Debug_DetectContextLeaks();
        }


        /// <summary>
        /// Create a default instance of a Freezable object. Actual allocation
        /// will occur in CreateInstanceCore.
        /// </summary>
        /// <returns>A new instance of the class</returns>
        protected Freezable CreateInstance()
        {
            Freezable newFreezable = CreateInstanceCore();

            Debug_VerifyInstance("CreateInstance", this, newFreezable);

            return newFreezable;
        }

        //
        /// <summary>
        /// Subclasses must implement this to create instances of themselves.
        /// See the Freezable documentation for examples.
        /// </summary>
        /// <returns>A new instance of the class</returns>
        protected abstract Freezable CreateInstanceCore();

        /// <summary>
        /// If you derive from Freezable you may need to override this method. Reasons
        /// to override include:
        ///    1) Your subclass has data that is not exposed via DPs
        ///    2) Your subclass has to perform extra work during construction. For
        ///       example, your subclass implements ISupportInitialize.
        ///
        /// The default implementation makes deep clones of all writable, locally set
        /// properties including expressions. The property's base value is copied -- not the
        /// current value. It skips read only DPs.
        ///
        /// If you do override this method, you MUST call the base implementation.
        ///
        /// This is called by Clone().
        /// </summary>
        /// <param name="sourceFreezable">The Freezable to clone information from</param>
        protected virtual void CloneCore(Freezable sourceFreezable)
        {
            CloneCoreCommon(sourceFreezable,
                /* useCurrentValue = */ false,
                /* cloneFrozenValues = */ true);
        }

        /// <summary>
        /// If you derive from Freezable you may need to override this method. Reasons
        /// to override include:
        ///    1) Your subclass has data that is not exposed via DPs
        ///    2) Your subclass has to perform extra work during construction. For
        ///       example, your subclass implements ISupportInitialize.
        ///
        /// The default implementation goes through all DPs making copies of their
        /// current values. It skips read only and default DPs
        ///
        /// If you do override this method, you MUST call the base implementation.
        ///
        /// This is called by CloneCurrentValue().
        /// </summary>
        /// <param name="sourceFreezable">The Freezable to copy info from</param>
        protected virtual void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            CloneCoreCommon(sourceFreezable,
                /* useCurrentValue = */ true,
                /* cloneFrozenValues = */ true);
        }

        /// <summary>
        /// If you derive from Freezable you may need to override this method. Reasons
        /// to override include:
        ///    1) Your subclass has data that is not exposed via DPs
        ///    2) Your subclass has to perform extra work during construction. For
        ///       example, your subclass implements ISupportInitialize.
        ///
        /// The default implementation makes clones of all writable, unfrozen, locally set
        /// properties including expressions. The property's base value is copied -- not the
        /// current value. It skips read only DPs and any values which are already frozen.
        ///
        /// If you do override this method, you MUST call the base implementation.
        ///
        /// You do not need to Freeze values as they are copied.  The result will be
        /// frozen by GetAsFrozen() before being returned.
        ///
        /// This is called by GetAsFrozen().
        /// </summary>
        /// <param name="sourceFreezable">The Freezable to clone information from</param>
        protected virtual void GetAsFrozenCore(Freezable sourceFreezable)
        {
            CloneCoreCommon(sourceFreezable,
                /* useCurrentValue = */ false,
                /* cloneFrozenValues = */ false);
        }

        /// <summary>
        /// If you derive from Freezable you may need to override this method. Reasons
        /// to override include:
        ///    1) Your subclass has data that is not exposed via DPs
        ///    2) Your subclass has to perform extra work during construction. For
        ///       example, your subclass implements ISupportInitialize.
        ///
        /// The default implementation goes through all DPs making copies of their
        /// current values. It skips read only DPs and any values which are already frozen.
        ///
        /// If you do override this method, you MUST call the base implementation.
        ///
        /// You do not need to Freeze values as they are copied.  The result will be
        /// frozen by GetCurrentValueAsFrozen() before being returned.
        ///
        /// This is called by GetCurrentValueAsFrozen().
        /// </summary>
        /// <param name="sourceFreezable">The Freezable to clone information from</param>
        protected virtual void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            CloneCoreCommon(sourceFreezable,
                /* useCurrentValue = */ true,
                /* cloneFrozenValues = */ false);
        }

        /// <summary>
        /// If you derive from Freezable you will need to override this if your subclass
        /// has data that is not exposed via DPs.
        ///
        /// The default implementation goes through all DPs and returns false
        /// if any DP has an expression or if any Freezable DP cannot freeze.
        ///
        /// If you do override this method, you MUST call the base implementation.
        ///
        /// This is called by Freeze().
        /// </summary>
        /// <param name="isChecking">If this is true, the method will just check
        /// to see that the object can be frozen, but won't actually freeze it.
        /// </param>
        /// <returns>True if the Freezable is or can be frozen.</returns>
        protected virtual bool FreezeCore(bool isChecking)
        {
            EffectiveValueEntry[] effectiveValues = EffectiveValues;
            uint numEffectiveValues = EffectiveValuesCount;

            // Loop through all DPs and call their FreezeValueCallback.
            for (uint i = 0; i < numEffectiveValues; i++)
            {
                DependencyProperty dp =
                    DependencyProperty.RegisteredPropertyList.List[effectiveValues[i].PropertyIndex];

                if (dp != null)
                {
                    EntryIndex entryIndex = new EntryIndex(i);
                    PropertyMetadata metadata = dp.GetMetadata(DependencyObjectType);
                    
                    FreezeValueCallback freezeValueCallback = metadata.FreezeValueCallback;
                    if(!freezeValueCallback(this, dp, entryIndex, metadata, isChecking))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        //
        // _eventStorage is used as a performance/memory speedup when firing change handlers.
        // It exists once per thread for thread safety, and is used to store the list of change
        // handlers that are gathered by GetChangeHandlersAndInvalidateSubProperties.  Reusing the
        // same EventStorage gives gains because it doesn't need to be reallocated each time
        // FireChanged occurs.
        //
        [ThreadStatic]
        static private EventStorage _eventStorage = null;

        /// <summary>
        /// Property to access and intialize the thread static _eventStorage variable.
        /// </summary>
        private EventStorage CachedEventStorage
        {
            get
            {
                // make sure _eventStorage is not null - with ThreadStatic it appears that the second
                // thread to access the variable will set this to null
                if (_eventStorage == null)
                {
                    _eventStorage = new EventStorage(INITIAL_EVENTSTORAGE_SIZE);
                }

                return _eventStorage;
            }
        }

        /// <summary>
        /// Gets an EventStorage object to be used to cache event handlers and sets it to be
        /// in use.
        /// </summary>
        /// <returns>
        /// An EventStorage object to be used to cache event handlers that is set
        /// to be in use.
        /// </returns>
        private EventStorage GetEventStorage()
        {
            EventStorage eventStorage = CachedEventStorage;

            // if we reach a case where EventStorage is being used - meaning FireChanged called
            // a handler that in turn called FireChanged which is probably a bad thing to have
            // happen - just allocate a new one that won't be cached.
            if (eventStorage.InUse)
            {
                // use the cached EventStorage's physical size as an estimate of how big we
                // need to be in order to avoid growing the newly created EventStorage
                int cachedPhysicalSize = eventStorage.PhysicalSize;
                eventStorage = new EventStorage(cachedPhysicalSize);
            }

            eventStorage.InUse = true;

            return eventStorage;
        }

        /// <summary>
        /// This method is called when a modification happens to the Freezable object.
        /// </summary>
        protected virtual void OnChanged()
        {
        }


        /// <summary>
        /// This method walks up the context graph recursively, gathering all change handlers that
        /// exist at or above the current node, placing them in calledHandlers.  While
        /// performing the walk it will also call OnChanged and InvalidateSubProperty on all
        /// DO/DP pairs encountered on the walk.
        /// </summary>
        private void GetChangeHandlersAndInvalidateSubProperties(ref EventStorage calledHandlers)
        {
            this.OnChanged();

            Freezable contextAsFreezable;

            if (Freezable_UsingSingletonContext)
            {
                DependencyObject context = SingletonContext;

                contextAsFreezable = context as Freezable;
                if (contextAsFreezable != null)
                {
                    contextAsFreezable.GetChangeHandlersAndInvalidateSubProperties(ref calledHandlers);
                }

                if (SingletonContextProperty != null)
                {
                    context.InvalidateSubProperty(SingletonContextProperty);
                }
            }
            else if (Freezable_UsingContextList)
            {
                FrugalObjectList<FreezableContextPair> contextList = ContextList;

                DependencyObject lastDO = null;

                int deadRefs = 0;
                for (int i = 0, count = contextList.Count; i < count; i++)
                {
                    FreezableContextPair currentContext = contextList[i];

                    DependencyObject currentDO = (DependencyObject)currentContext.Owner.Target;
                    if (currentDO != null)
                    {
                        // we only want to grab change handlers once per context reference - so skip
                        // until we find a new one
                        if (currentDO != lastDO)
                        {
                            contextAsFreezable = currentDO as Freezable;
                            if (contextAsFreezable != null)
                            {
                                contextAsFreezable.GetChangeHandlersAndInvalidateSubProperties(ref calledHandlers);
                            }

                            lastDO = currentDO;
                        }

                        if (currentContext.Property != null)
                        {
                            currentDO.InvalidateSubProperty(currentContext.Property);
                        }
                    }
                    else
                    {
                        ++deadRefs;
                    }
                }

                PruneContexts(contextList, deadRefs);
            }


            GetHandlers(ref calledHandlers);
        }


        /// <summary>
        /// Extenders of Freezable must call this method at the beginning of any
        /// public API which reads the state of the object.  (e.g., a proprety getter.)
        /// This ensures that the object is being accessed from a valid thread.
        /// </summary>
        protected void ReadPreamble()
        {
            VerifyAccess();
        }

        /// <summary>
        /// Extenders of Freezable must call this method prior to changing the state
        /// of the object (e.g. the beginning of a property setter.)  This ensures that
        /// the object is not frozen and is being accessed from a valid thread.
        /// </summary>
        protected void WritePreamble()
        {
            VerifyAccess();

            if (IsFrozenInternal)
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.Freezable_CantBeFrozen,GetType().FullName));
            }
        }

        /// <summary>
        /// Extenders of Freezable must call this method at the end of an API which
        /// changed the state of the object (e.g., at the end of a property setter) to
        /// raise the Changed event.  Multiple state changes within a method or
        /// property may be "batched" into a single call to WritePostscript().
        /// </summary>
        protected void WritePostscript()
        {
            FireChanged();
        }

        /// <summary>
        /// Extenders of Freezable call this to set in a new value for internal
        /// properties or other embedded values that themselves are DependencyObjects.
        /// This method insures that the appropriate context pointers are set up for
        ///  the old and the new Dependency objects.
        ///
        /// In this version the property is set to be null since
        /// it is not explicitly specified.
        ///
        /// </summary>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value to set into the property</param>
        protected void OnFreezablePropertyChanged(
            DependencyObject oldValue,
            DependencyObject newValue
            )
        {
            OnFreezablePropertyChanged(oldValue, newValue, null);
        }

        /// <summary>
        /// Extenders of Freezable call this to set in a new value for internal
        /// properties or other embedded values that themselves are DependencyObjects.
        /// This method insures that the appropriate context pointers are set up for
        /// the old and the new DependencyObject objects.
        /// </summary>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value to set into the property</param>
        /// <param name="property">The property that is being changed or null if none</param>
        protected void OnFreezablePropertyChanged(
            DependencyObject oldValue,
            DependencyObject newValue,
            DependencyProperty property
            )
        {
            //
            //    We should ensure dispatchers are consistent *before* modifying
            //    changed handlers, otherwise we will leave the freezable in an
            //    inconsistent state.
            //
            if (newValue != null)
            {
                EnsureConsistentDispatchers(this, newValue);
            }

            if (oldValue != null)
            {
                RemoveSelfAsInheritanceContext(oldValue, property);
            }

            if (newValue != null)
            {
                ProvideSelfAsInheritanceContext(newValue, property);
            }
        }

        /// <summary>
        /// Helper method that just invokes Freeze on provided
        /// Freezable if it's not null.  Otherwise it doesn't do anything.
        /// </summary>
        /// <param name="freezable">Freezable to freeze.</param>
        /// <param name="isChecking">If this is true, the method will just check
        /// to see that the object can be frozen, but won't actually freeze it.
        /// </param>
        /// <returns>True if the Freezable was or can be frozen.
        /// False if isChecking was true and the Freezable can't be frozen.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">This exception
        /// will be thrown if isChecking is passed in as false and this
        /// Freezable can't be frozen.</exception>
        //  Future Note: Consider removing if we move Freezables to DO's, and moving it into
        // SetFreezableContextCore directly.  What situations would remain for subclasses to need to call it?
        static protected internal bool Freeze(Freezable freezable, bool isChecking)
        {
            if (freezable != null)
            {
                return freezable.Freeze(isChecking);
            }

            // <mcalkins> I guess something that's null is always frozen.
            return true;
        }

        #endregion  // Protected Methods

        #region ISealable

        /// <summary>
        /// Can this freezable be sealed
        /// </summary>
        bool ISealable.CanSeal
        {
            get { return CanFreeze; }
        }

        /// <summary>
        /// Is this freezable sealed
        /// </summary>
        bool ISealable.IsSealed
        {
            get { return IsFrozen; }
        }

        /// <summary>
        /// Seal this freezable
        /// </summary>
        void ISealable.Seal()
        {
            Freeze();
        }

        #endregion ISealable

        #region Internal Methods

        /// <summary>
        /// Clears off the context storage and all Changed event handlers
        /// </summary>
        internal void ClearContextAndHandlers()
        {
            Freezable_UsingHandlerList = false;
            Freezable_UsingContextList = false;
            Freezable_UsingSingletonHandler = false;
            Freezable_UsingSingletonContext = false;
            _contextStorage = null;
            _property = null;
        }


        /// <summary>
        /// Raises changed notifications for this Freezable.  This includes
        /// calling the OnChanged virtual, invalidating sub properties, and
        /// raising the Changed event.
        /// </summary>
        internal void FireChanged()
        {
            // to avoid access costs, we start with calledHandlers at null and then
            // set it the first time we encounter change handlers that need to be stored.
            EventStorage calledHandlers = null;

            GetChangeHandlersAndInvalidateSubProperties(ref calledHandlers);

            // Fire all of the change handlers
            if (calledHandlers != null)
            {
                for (int i = 0, count = calledHandlers.Count; i < count; i++)
                {
                    // Note: there is a known issue here where if one of these handlers
                    // throws an exception, then we effectively will no longer be able to
                    // use the EventStorage cache since it will not be possible to set its InUse flag
                    // to false, and we will also keep any memory it was pointing to alive.
                    // Everything will continue to function normally, however, we will be allocating
                    // a new EventStorage each time rather than using the one stored in the cache.
                    // Catching the exception and clearing the flag (and nulling
                    // out the contents) will solve it, but due to Task #45099 on the exception
                    // strategy for the property engine, this has not yet been implemented.
                    //
                    // call the function and then set to null to avoid hanging on to any
                    // references.
                    calledHandlers[i](this, EventArgs.Empty);
                    calledHandlers[i] = null;
                }

                // we no longer need the EventStorage object - clear its contents and set
                // it to not be in use.
                calledHandlers.Clear();
                calledHandlers.InUse = false;
            }
        }

        /// <summary>
        /// Calling DependencyObject.Seal() on a Freezable will leave it in a weird
        /// state - it won't be free-threaded, but since Seal and Freeze use the
        /// same bit, the Freezable will think it is Frozen.  We therefore disallow
        /// calling Seal() on a Freezable.
        /// </summary>
        internal override void Seal()
        {
            Invariant.Assert(false);
        }

        #endregion  // Internal Methods

        #region Private methods

        internal bool Freeze(bool isChecking)
        {
            if (isChecking)
            {
                ReadPreamble();

                return FreezeCore(true);
            }
            else if (!IsFrozenInternal)
            {
                WritePreamble();

                // Check with derived classes to see how they feel about this.
                // If our caller didn't check CanFreeze this may throw
                // an exception.
                FreezeCore(false);

                // Any cached default values created using the FreezableDefaultValueFactory
                // must be removed and frozen. Leaving them alone is not an option since they will
                // attempt to promote themselves to locally-set if the user modifies them -
                // at that point this object will be sealed and the SetValue call will throw an
                // exception. For Freezables we're required to freeze all DPs, so for performance
                // we simply toss out the cache and return the frozen default prototype, which has
                // exactly the same state as the cached default (see PropertyMetadata.GetDefaultValue()).
                PropertyMetadata.RemoveAllCachedDefaultValues(this);

                // Since this object no longer changes it won't be able to notify dependents
                DependentListMapField.ClearValue(this);

                // The heart of Freeze.  IsFrozen will now return
                // true, we keep the handler status bits since we haven't changed our
                // handler storage yet.
                Freezable_Frozen = true;

                this.DetachFromDispatcher();

                // We do notify now, since we're "changing" to frozen.  But not
                // until after everything below us is frozen.
                FireChanged();

                // Clear off event handler/context flags when becoming frozen.  We don't need to call
                // OnInheritanceContextChanged because a Frozen freezable has no one listening to its
                // InheritanceContextChanged event. Listeners are added when either a BindingExpression
                // or ResourceReferenceExpression is set into a DP. Both derive from Expression, and
                // calling Freeze on any Freezable with a DP set to an Expression will throw an exception.
                Debug_AssertNoInheritanceContextListeners();
                ClearContextAndHandlers();

                WritePostscript();
            }

            return true;
        }

        // Makes a deep clone of a Freezable.  Helper method for
        // CloneCore(), CloneCurrentValueCore() and GetAsFrozenCore()
        //
        // If useCurrentValue is true it calls GetValue on each of the sourceFreezable's DPs; if false
        // it uses ReadLocalValue.
        private void CloneCoreCommon(Freezable sourceFreezable, bool useCurrentValue, bool cloneFrozenValues)
        {
            EffectiveValueEntry[] srcEffectiveValues = sourceFreezable.EffectiveValues;
            uint srcEffectiveValueCount = sourceFreezable.EffectiveValuesCount;

            // Iterate through the effective values array.  Note that default values aren't
            // stored here so the only defaults we'll come across are modified defaults,
            // which useCurrentValue = true uses and useCurrentValue = false ignores.
            for (uint i = 0; i < srcEffectiveValueCount; i++)
            {
                EffectiveValueEntry srcEntry = srcEffectiveValues[i];

                DependencyProperty dp = DependencyProperty.RegisteredPropertyList.List[srcEntry.PropertyIndex];

                // We need to skip ReadOnly properties otherwise SetValue will fail
                if ((dp != null) && !dp.ReadOnly)
                {
                    object sourceValue;

                    EntryIndex entryIndex = new EntryIndex(i);

                    if (useCurrentValue)
                    {
                        // Default values aren't in the EffectiveValues array
                        // so we won't see them as we iterate.  We do copy modified defaults.
                        Debug.Assert(srcEntry.BaseValueSourceInternal != BaseValueSourceInternal.Default || srcEntry.HasModifiers);

                        sourceValue = sourceFreezable.GetValueEntry(
                                            entryIndex,
                                            dp,
                                            null,
                                            RequestFlags.FullyResolved).Value;

                        // GetValue should not have returned UnsetValue
                        Debug.Assert(sourceValue != DependencyProperty.UnsetValue);
                    }
                    else // use base values
                    {
                        // If the local value has modifiers, ReadLocalValue will return the base
                        // value, which is what we want.  A modified default will return UnsetValue,
                        // which will be ignored at the call to SetValue
                        sourceValue = sourceFreezable.ReadLocalValueEntry(entryIndex, dp, true /* allowDeferredReferences */);

                        // For the useCurrentValue = false case we ignore any UnsetValues.
                        if (sourceValue == DependencyProperty.UnsetValue)
                        {
                            continue;
                        }

                        // If the DP is an expression ReadLocalValue will return the actual expression.
                        // In this case we need to copy it.
                        if (srcEntry.IsExpression)
                        {
                            sourceValue = ((Expression)sourceValue).Copy(this, dp);
                        }
                    }

                    //
                    // If the value of the current DP is a Freezable
                    // we need to recurse and call the appropriate Clone method in
                    // order to do a deep copy.
                    //

                    Debug.Assert(!(sourceValue is Expression && sourceValue is Freezable),
                        "This logic assumes Expressions and Freezables don't co-derive");

                    Freezable valueAsFreezable = sourceValue as Freezable;

                    if (valueAsFreezable != null)
                    {
                        Freezable valueAsFreezableClone;

                        //
                        // Choose between the four possible ways of
                        // cloning a Freezable
                        //
                        if (cloneFrozenValues) //CloneCore and CloneCurrentValueCore
                        {
                            valueAsFreezableClone = valueAsFreezable.CreateInstanceCore();

                            if (useCurrentValue)
                            {
                                // CloneCurrentValueCore implementation.  We clone even if the
                                // Freezable is frozen by recursing into CloneCurrentValueCore.
                                valueAsFreezableClone.CloneCurrentValueCore(valueAsFreezable);
                            }
                            else
                            {
                                // CloneCore implementation.  We clone even if the Freezable is
                                // frozen by recursing into CloneCore.
                                valueAsFreezableClone.CloneCore(valueAsFreezable);
                            }

                            sourceValue = valueAsFreezableClone;
                            Debug_VerifyCloneCommon(valueAsFreezable, valueAsFreezableClone, /*isDeepClone=*/ true);
                        }
                        else // skip cloning frozen values
                        {
                            if (!valueAsFreezable.IsFrozen)
                            {
                                valueAsFreezableClone = valueAsFreezable.CreateInstanceCore();

                                if (useCurrentValue)
                                {
                                    // GetCurrentValueAsFrozenCore implementation.  Only clone if the
                                    // Freezable is mutable by recursing into GetCurrentValueAsFrozenCore.
                                    valueAsFreezableClone.GetCurrentValueAsFrozenCore(valueAsFreezable);
                                }
                                else
                                {
                                    // GetAsFrozenCore implementation.  Only clone if the Freezable is
                                    // mutable by recursing into GetAsFrozenCore.
                                    valueAsFreezableClone.GetAsFrozenCore(valueAsFreezable);
                                }

                                sourceValue = valueAsFreezableClone;
                                Debug_VerifyCloneCommon(valueAsFreezable, valueAsFreezableClone, /*isDeepClone=*/ false);
                            }
                        }
                    }

                    SetValue(dp, sourceValue);
                }
            }
        }

        // Throws if owner/child are not context free and on different dispatchers.
        private static void EnsureConsistentDispatchers(DependencyObject owner, DependencyObject child)
        {
            Debug.Assert(owner != null && child != null,
                "Caller should guard against passing null owner/child.");

            // It is illegal to set a DependencyObject from one Dispatcher into a owner
            // being serviced by a different Dispatcher (i.e., they need to be on
            // the same thread or be context free (Dispatcher == null))
            if (owner.Dispatcher != null &&
                child.Dispatcher != null &&
                owner.Dispatcher != child.Dispatcher)
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.Freezable_AttemptToUseInnerValueWithDifferentThread));
            }
        }

        // These methods provide an abstraction for managing Freezable context
        // information - the context information being DO/DP pairs that the Freezable maps to.
        //
        // The methods will attempt to use as little memory as possible to store this information.
        // When there is only one context it will store the information directly, otherwise it will
        // place it within a list.  When using a list, these methods place the DO/DP pairs so
        // that DOs are grouped together.  This is done so that when walking the graph, it
        // is easier to track which DO's change handlers have already been gathered.
        //

        /// <summary>
        /// Removes the context information for a Freezable.
        /// <param name="context">The DependencyObject to remove that references this Freezable.</param>
        /// <param name="property">The property of the DependencyObject this object maps to or null if none.</param>
        /// </summary>
        private void RemoveContextInformation(DependencyObject context, DependencyProperty property)
        {
            Debug.Assert(context != null);

            bool failed = true;

            if (Freezable_UsingSingletonContext)
            {
                if (SingletonContext == context && SingletonContextProperty == property)
                {
                    RemoveSingletonContext();
                    failed = false;
                }
            }
            else if (Freezable_UsingContextList)
            {
                FrugalObjectList<FreezableContextPair> list = ContextList;

                int deadRefs = 0;
                int index = -1;
                int count = list.Count;

                for (int i = 0; i < count; i++)
                {
                    FreezableContextPair entry = list[i];

                    object owner = entry.Owner.Target;
                    if (owner != null)
                    {
                        if (failed && entry.Property == property && owner == context)
                        {
                            index = i;
                            failed = false;
                        }
                    }
                    else
                    {
                        ++deadRefs;
                    }
                }

                if (index != -1)
                {
                    Debug.Assert(!failed);

                    list.RemoveAt(index);
                }

                PruneContexts(list, deadRefs);
            }

            // Make sure we actually removed something - if not throw an exception
            if (failed)
            {
                throw new ArgumentException(SR.Get(SRID.Freezable_NotAContext), "context");
            }
        }

        /// <summary>
        /// Removes the single piece of contextual information that we have and updates all flags
        /// accordingly.
        /// </summary>
        private void RemoveSingletonContext()
        {
            Debug.Assert(Freezable_UsingSingletonContext);
            Debug.Assert(SingletonContext != null);

            if (HasHandlers)
            {
                _contextStorage = ((HandlerContextStorage)_contextStorage)._handlerStorage;
            }
            else
            {
                _contextStorage = null;
            }

            Freezable_UsingSingletonContext = false;
        }

        /// <summary>
        /// Removes the context list and updates all flags accordingly.
        /// </summary>
        private void RemoveContextList()
        {
            Debug.Assert(Freezable_UsingContextList);

            if (HasHandlers)
            {
                _contextStorage = ((HandlerContextStorage)_contextStorage)._handlerStorage;
            }
            else
            {
                _contextStorage = null;
            }

            Freezable_UsingContextList = false;
        }


        /// <summary>
        /// Helper function to add context information to a Freezable.
        /// </summary>
        /// <param name="context">The DependencyObject to add that references this Freezable.</param>
        /// <param name="property">The property of the DependencyObject this object maps to or null if none.</param>
        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            Debug.Assert(context != null);

            // Debug_VerifyContextIsValid(context, property);

            if (!IsFrozenInternal)
            {
                DependencyObject oldInheritanceContext = InheritanceContext;

                AddContextInformation(context, property);

                // Check if the context has changed
                // If we are frozen, or we already had multiple contexts, the context has not changed
                if (oldInheritanceContext != InheritanceContext)
                {
                    OnInheritanceContextChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Helper function to remove context information from a Freezable.
        /// </summary>
        /// <param name="context">The DependencyObject that references this Freezable.</param>
        /// <param name="property">The property of the DependencyObject this object maps to or null if none.</param>
        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            Debug.Assert(context != null);

            if (!IsFrozenInternal)
            {
                DependencyObject oldInheritanceContext = InheritanceContext;

                RemoveContextInformation(context, property);

                // Check if the context has changed
                // If we are frozen, or we already had multiple contexts, the context has not changed
                if (oldInheritanceContext != InheritanceContext)
                {
                    OnInheritanceContextChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Adds context information to a Freezable.
        /// <param name="context">The DependencyObject to add that references this Freezable.</param>
        /// <param name="property">The property of the DependencyObject this object maps to or null if none.</param>
        /// </summary>
        internal void AddContextInformation(DependencyObject context, DependencyProperty property)
        {
            Debug.Assert(context != null);

            if (Freezable_UsingSingletonContext)
            {
                ConvertToContextList();
            }

            if (Freezable_UsingContextList)
            {
                AddContextToList(context, property);
            }
            else
            {
                AddSingletonContext(context, property);
            }
        }

        /// <summary>
        /// Helper function to convert to using a list to store context information.
        /// The SingletonContext is inserted into the list.
        /// </summary>
        private void ConvertToContextList()
        {
            Debug.Assert(Freezable_UsingSingletonContext);

            // The list is initialized with capacity for 2 entries since we
            // know we have a 2nd context to insert, hence the conversion
            // from the singleton context state.
            FrugalObjectList<FreezableContextPair> list = new FrugalObjectList<FreezableContextPair>(2);

            // Note: This converts the SingletonContext from a strong reference to a WeakReference
            list.Add(new FreezableContextPair(SingletonContext, SingletonContextProperty));

            if (HasHandlers)
            {
                ((HandlerContextStorage)_contextStorage)._contextStorage = list;
            }
            else
            {
                _contextStorage = list;
            }

            Freezable_UsingContextList = true;
            Freezable_UsingSingletonContext = false;

            // clear the singleton context property
            _property = null;
        }

        /// <summary>
        /// Helper function to add a singleton context to the Freezable's storage
        /// <param name="context">The DependencyObject to add that references this Freezable.</param>
        /// <param name="property">The property of the DependencyObject this object maps to or null if none.</param>
        /// </summary>
        private void AddSingletonContext(DependencyObject context, DependencyProperty property)
        {
            Debug.Assert(!Freezable_UsingSingletonContext && !Freezable_UsingContextList);
            Debug.Assert(context != null);

            if (HasHandlers)
            {
                HandlerContextStorage hps = new HandlerContextStorage();

                hps._handlerStorage = _contextStorage;
                hps._contextStorage = context;

                _contextStorage = hps;
            }
            else
            {
                _contextStorage = context;
            }

            // set the singleton context property
            _property = property;

            Freezable_UsingSingletonContext = true;
        }

        /// <summary>
        /// Adds the context information to the context list.  It does this by inserting the
        /// new context information in a location so that all context information referring
        /// to the same DO are grouped together.
        /// </summary>
        /// <param name="context">The DependencyObject to add that references this Freezable.</param>
        /// <param name="property">The property of the DependencyObject this object maps to or null if none.</param>
        private void AddContextToList(DependencyObject context, DependencyProperty property)
        {
            Debug.Assert(context != null);

            FrugalObjectList<FreezableContextPair> list = ContextList;
            int count = list.Count;
            int insertIndx = count;        // insert at the end by default
            int deadRefs = 0;

            DependencyObject lastContext = null;
            bool multipleInheritanceContextsFound = HasMultipleInheritanceContexts;  // We can never leave this state once there

            // Note: This method should really never be called if 'context' is not supposed
            // to act as the InheritanceContext for 'this'.  The checks in the next line
            // are bypassed in central method DO.ProvideSelfAsInheritanceContext in the
            // Freezable case, because Freezable relies on side-effects (adding 'context'
            // to the list, even if it's not the InheritanceContext).  Instead, we do
            // the checks here.
            bool newInheritanceContext = context.CanBeInheritanceContext && !this.IsInheritanceContextSealed;  // becomes false if we find context on the list

            for (int i = 0; i < count; i++)
            {
                DependencyObject currentContext = (DependencyObject)list[i].Owner.Target;
                if (currentContext != null)
                {
                    if (currentContext == context)
                    {
                        // insert after the last matching context
                        insertIndx = i + 1;
                        newInheritanceContext = false;
                    }

                    if (newInheritanceContext && !multipleInheritanceContextsFound)
                    {
                        if (currentContext != lastContext && currentContext.CanBeInheritanceContext)  // Count remaining inheritance contexts
                        {
                            // We already found a previous inheritance context, so we have multiple ones
                            multipleInheritanceContextsFound = true;
                            Freezable_HasMultipleInheritanceContexts = true;
                        }
                        lastContext = currentContext;
                    }
                }
                else
                {
                    ++deadRefs;
                }
            }

            list.Insert(insertIndx, new FreezableContextPair(context, property));

            PruneContexts(list, deadRefs);
        }


        private void PruneContexts(FrugalObjectList<FreezableContextPair> oldList, int numDead)
        {
            int count = oldList.Count;

            if (count - numDead == 0)
            {
                RemoveContextList();
            }
            else if (numDead > 0)
            {
                FrugalObjectList<FreezableContextPair> newList =
                    new FrugalObjectList<FreezableContextPair>(count - numDead);

                for (int i = 0; i < count; i++)
                {
                    if (oldList[i].Owner.IsAlive)
                    {
                        newList.Add(oldList[i]);
                    }
                }

                ContextList = newList;
            }
        }

        /// <summary>
        /// Helper function to get all of the event handlers for the Freezable and
        /// place them in the calledHandlers list.
        /// <param name="calledHandlers"> Where to place the change handlers for the Freezable. </param>
        /// </summary>
        private void GetHandlers(ref EventStorage calledHandlers)
        {
            if (Freezable_UsingSingletonHandler)
            {
                if (calledHandlers == null)
                {
                    calledHandlers = GetEventStorage();
                }

                calledHandlers.Add(SingletonHandler);
            }
            else if (Freezable_UsingHandlerList)
            {
                if (calledHandlers == null)
                {
                    calledHandlers = GetEventStorage();
                }

                FrugalObjectList<EventHandler> handlers = HandlerList;

                for (int i = 0, count = handlers.Count; i < count; i++)
                {
                    calledHandlers.Add(handlers[i]);
                }
            }
        }

        /// <summary>
        /// Add the specified EventHandler
        /// </summary>
        /// <param name="handler">Handler to add</param>
        private void HandlerAdd(EventHandler handler)
        {
            Debug.Assert(handler != null);

            if (Freezable_UsingSingletonHandler)
            {
                ConvertToHandlerList();
            }

            if (Freezable_UsingHandlerList)
            {
                HandlerList.Add(handler);
            }
            else
            {
                AddSingletonHandler(handler);
            }
        }

        /// <summary>
        /// Remove the specified EventHandler
        /// </summary>
        /// <param name="handler">Handler to remove</param>
        private void HandlerRemove(EventHandler handler)
        {
            bool failed = true;

            Debug.Assert(handler != null);

            if (Freezable_UsingSingletonHandler)
            {
                if (SingletonHandler == handler)
                {
                    RemoveSingletonHandler();
                    failed = false;
                }
            }
            else if (Freezable_UsingHandlerList)
            {
                FrugalObjectList<EventHandler> handlers = HandlerList;
                int index = handlers.IndexOf(handler);

                if (index >= 0)
                {
                    handlers.RemoveAt(index);
                    failed = false;
                }

                if (handlers.Count == 0)
                {
                    RemoveHandlerList();
                }
            }

            if (failed)
            {
                throw new ArgumentException(SR.Get(SRID.Freezable_UnregisteredHandler), "handler");
            }
        }

        //
        //  Removes the singleton handler the Freezable is storing and resets
        //  any state indicating this.
        //
        private void RemoveSingletonHandler()
        {
            Debug.Assert(Freezable_UsingSingletonHandler);

            if (HasContextInformation)
            {
              _contextStorage = ((HandlerContextStorage)_contextStorage)._contextStorage;
            }
            else
            {
              _contextStorage = null;
            }

            Freezable_UsingSingletonHandler = false;
        }

        //
        //  Removes the handler list the Freezable is storing and resets
        //  any state indicating this.
        //
        private void RemoveHandlerList()
        {
            Debug.Assert(Freezable_UsingHandlerList && HandlerList.Count == 0);

            if (HasContextInformation)
            {
                _contextStorage = ((HandlerContextStorage)_contextStorage)._contextStorage;
            }
            else
            {
                _contextStorage = null;
            }

             Freezable_UsingHandlerList = false;
        }

        /// <summary>
        /// Helper function to convert to using a list to store context information.
        /// The SingletonContext is inserted into the list.
        /// </summary>
        private void ConvertToHandlerList()
        {
            Debug.Assert(Freezable_UsingSingletonHandler);

            EventHandler singletonHandler = SingletonHandler;

            // The list is initialized with capacity for 2 entries since we
            // know we have a 2nd handler to insert, hence the conversion
            // from the singleton handler state.
            FrugalObjectList<EventHandler> list = new FrugalObjectList<EventHandler>(2);

            list.Add(singletonHandler);

            if (HasContextInformation)
            {
                ((HandlerContextStorage)_contextStorage)._handlerStorage = list;
            }
            else
            {
                _contextStorage = list;
            }

            Freezable_UsingHandlerList = true;
            Freezable_UsingSingletonHandler = false;
        }

        //
        // helper function to add a singleton handler.  The passed in handler parameter
        // will be stored as the singleton handler.
        //
        private void AddSingletonHandler(EventHandler handler)
        {
            Debug.Assert(!Freezable_UsingHandlerList && !Freezable_UsingSingletonHandler);
            Debug.Assert(handler != null);

            if (HasContextInformation)
            {
                HandlerContextStorage hps = new HandlerContextStorage();

                hps._contextStorage = _contextStorage;
                hps._handlerStorage = handler;

                _contextStorage = hps;
            }
            else
            {
                _contextStorage = handler;
            }

            Freezable_UsingSingletonHandler = true;
        }

        #endregion

        #region Private properties

        //------------------------------------------------------
        //
        //  Private properties
        //
        //------------------------------------------------------

        //
        // The below properties help at getting the singleton/list for the context or change handlers.
        // In all cases, if the other object exists (i.e. we want context, and there are also stored handlers),
        // then _contextStorage is HandlerContextStorage, so we need to get the data we want from that class.
        //

        /// <summary>
        /// Returns the context list the Freezable has.  This function assumes
        /// that UsingContextList is true before being called.
        /// </summary>
        private FrugalObjectList<FreezableContextPair> ContextList
        {
            get
            {
                Debug.Assert(Freezable_UsingContextList && !Freezable_UsingSingletonContext,
                             "Must call UsingContextList before use");

                if (HasHandlers)
                {
                    HandlerContextStorage ptrStorage = (HandlerContextStorage)_contextStorage;

                    return (FrugalObjectList<FreezableContextPair>)ptrStorage._contextStorage;
                }
                else
                {
                    return (FrugalObjectList<FreezableContextPair>)_contextStorage;
                }
            }

            set
            {
                Debug.Assert(Freezable_UsingContextList && !Freezable_UsingSingletonContext,
                             "Must call UsingContextList before use");

                if (HasHandlers)
                {
                    ((HandlerContextStorage)_contextStorage)._contextStorage = value;
                }
                else
                {
                    _contextStorage = value;
                }
            }
        }

        /// <summary>
        /// Returns the handler list the Freezable has.  This function assumes
        /// the handlers for the Freezable are stored in a list.
        /// </summary>
        private FrugalObjectList<EventHandler> HandlerList
        {
            get
            {
                Debug.Assert(Freezable_UsingHandlerList && !Freezable_UsingSingletonHandler,
                                      "Must call UsingHandlerList before use");

                if (HasContextInformation)
                {
                    HandlerContextStorage ptrStorage = (HandlerContextStorage)_contextStorage;

                    return (FrugalObjectList<EventHandler>)ptrStorage._handlerStorage;
                }
                else
                {
                    return (FrugalObjectList<EventHandler>)_contextStorage;
                }
            }
        }

        /// <summary>
        /// Returns the singleton handler the Freezable has.  This function assumes
        /// that UsingSingletonHandler is true before being called.
        /// </summary>
        private EventHandler SingletonHandler
        {
            get
            {
                Debug.Assert(Freezable_UsingSingletonHandler && !Freezable_UsingHandlerList,
                                      "Must call UsingSingletonHandler before use");

                if (HasContextInformation)
                {
                    HandlerContextStorage ptrStorage = (HandlerContextStorage)_contextStorage;

                    return (EventHandler)ptrStorage._handlerStorage;
}
                else
                {
                    return (EventHandler)_contextStorage;
}
            }
        }

        /// <summary>
        /// Returns the singleton context the Freezable has.  This function assumes
        /// that UsingSingletonContext is true before being called.
        /// </summary>
        private DependencyObject SingletonContext
        {
            get
            {
                Debug.Assert(Freezable_UsingSingletonContext && !Freezable_UsingContextList,
                             "Must call UsingSingletonContext before use");

                if (HasHandlers)
                {
                    HandlerContextStorage ptrStorage = (HandlerContextStorage)_contextStorage;

                    return (DependencyObject)ptrStorage._contextStorage;
                }
                else
                {
                    return (DependencyObject)_contextStorage;
                }
            }
        }

        /// <summary>
        /// Returns/sets the singleton context property of the Freezable.  This
        /// function assumes that UsingSingletonContext is true before being called.
        /// </summary>
        private DependencyProperty SingletonContextProperty
        {
            get
            {
                Debug.Assert(Freezable_UsingSingletonContext && !Freezable_UsingContextList,
                             "Must call UsingSingletonContext before use");

                return (DependencyProperty)_property;
            }
        }

        /// <summary>
        /// Whether the Freezable has event handlers.
        /// </summary>
        private bool HasHandlers
        {
            get
            {
                return (Freezable_UsingHandlerList || Freezable_UsingSingletonHandler);
            }
        }

        /// <summary>
        /// Whether the Freezable has context information.
        /// </summary>
        private bool HasContextInformation
        {
            get
            {
                return (Freezable_UsingContextList || Freezable_UsingSingletonContext);
            }
        }

        #endregion

        #region InheritanceContext

        /// <summary>
        ///     InheritanceContext
        /// </summary>
        internal override DependencyObject InheritanceContext
        {
            [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
            get
            {
                if (!Freezable_HasMultipleInheritanceContexts)
                {
                    if (Freezable_UsingSingletonContext)  // We have exactly one Freezable context
                    {
                        DependencyObject singletonContext = SingletonContext;
                        if (singletonContext.CanBeInheritanceContext)
                        {
                            return singletonContext;
                        }
                    }
                    else if (Freezable_UsingContextList)
                    {
                        // We have multiple Freezable contexts, but at most one context is valid
                        FrugalObjectList<FreezableContextPair> list = ContextList;
                        int count = list.Count;

                        for (int i = 0; i < count; i++)
                        {
                            DependencyObject currentContext = (DependencyObject)list[i].Owner.Target;

                            if (currentContext != null && currentContext.CanBeInheritanceContext)
                            {
                                // This is the first and only valid inheritance context we should find
                                return currentContext;
                            }
                        }
                    }
                }

                return null;  // If we have gotten here, we have either multiple or no valid contexts
            }
        }

        /// <summary>
        ///     HasMultipleInheritanceContexts
        /// </summary>
        internal override bool HasMultipleInheritanceContexts
        {
            [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
            get { return Freezable_HasMultipleInheritanceContexts; }
        }

        #endregion InheritanceContext

        //
        // A simple class that is used when the Freezable needs to store both handlers and context info.
        // The _handlerStorage and _contextStorage fields can store either a list or a direct
        // reference to the object - Freezable's Freezable_* flags (actually added to DependencyObject.cs)
        // can be used to test for which one to use.
        //
        private class HandlerContextStorage
        {
            public object _handlerStorage;
            public object _contextStorage;
        }

        //
        // A simple struct that stores a weak ref to a dependency object and a corresponding property
        // of that object.
        //
        private struct FreezableContextPair
        {
            public FreezableContextPair(DependencyObject dependObject, DependencyProperty dependProperty)
            {
                Owner = new WeakReference(dependObject);
                Property = dependProperty;
            }

            public readonly WeakReference Owner;
            public readonly DependencyProperty Property;
        }

        //
        // A simple class that is used to cache the event handlers that are gathered during a call
        // to FireChanged.  Using this cache cuts down on the amount of managed allocations, which
        // improves the performance of Freezables.
        //
        private class EventStorage
        {
            public EventStorage(int initialSize)
            {
                // check just in case
                if (initialSize <= 0) initialSize = 1;

                _events = new EventHandler[initialSize];
                _logSize = 0;
                _physSize = initialSize;
                _inUse = false;
            }

            //
            //  Adds a new EventHandler to the storage.  In the case that more memory is needed, the cache
            //  size is doubled.
            //
            public void Add(EventHandler e)
            {
                if (_logSize == _physSize) {
                    _physSize *= 2;
                    EventHandler[] temp = new EventHandler[_physSize];

                    for (int i = 0; i < _logSize; i++) {
                        temp[i] = _events[i];
                    }

                    _events = temp;
                }

                _events[_logSize] = e;
                _logSize++;
            }

            //
            // Clears the list but does not free the memory so that future uses of the
            // class can reuse the space and not take an allocation performance hit.
            //
            public void Clear()
            {
                _logSize = 0;
            }

            public int Count
            {
                get
                {
                    return _logSize;
                }
            }

            public int PhysicalSize
            {
                get
                {
                    return _physSize;
                }
            }

            public EventHandler this[int idx]
            {
                get
                {
                    return _events[idx];
                }

                set
                {
                    _events[idx] = value;
                }
            }

            //
            //  So that it's possible to reuse EventStorage classes, and so that if one is being used, another
            //  person does not overwrite the contents (i.e. FireChanged causes someone else call their FireChanged),
            //  an InUse flag is set to indicate whether someone is currently using this class.
            //
            public bool InUse
            {
                get
                {
                    return _inUse;
                }
                set
                {
                    _inUse = value;
                }
            }

            EventHandler[] _events;         // list of events
            int _logSize;                   // the logical size of the list
            int _physSize;                  // the allocated buffer size
            bool _inUse;
        }

        //------------------------------------------------------
        //
        //  Debug fields
        //
        //------------------------------------------------------

        #region Debug 

        // Verify a clone.  If isDeepClone is true we make sure that the cloned object is not the same as the
        // original. GetAsFrozen and GetCurrentValueAsFrozen do not do deep clones since they will immediately
        // return any frozen originals rather than cloning them.
        private static void Debug_VerifyCloneCommon(Freezable original, object clone, bool isDeepClone)
        {
            if (Invariant.Strict)
            {
                Freezable cloneAsFreezable = (Freezable) clone;

                Debug_VerifyInstance("CloneCore", original, cloneAsFreezable);

                // Extra CloneCommon checks
                if (isDeepClone)
                {
                    Invariant.Assert(clone != original, "CloneCore should not return the same instance as the original.");
                }

                Invariant.Assert(!cloneAsFreezable.HasHandlers, "CloneCore should not have handlers attached on construction.");

                IList originalAsIList = original as IList;
                if (originalAsIList != null)
                {
                    // we've already checked that original and clone are the same type
                    IList cloneAsIList = clone as IList;

                    Invariant.Assert(originalAsIList.Count == cloneAsIList.Count, "CloneCore didn't clone all of the elements in the list.");

                    for (int i = 0; i < cloneAsIList.Count; i++)
                    {
                        Freezable originalItemAsFreezable = originalAsIList[i] as Freezable;
                        Freezable cloneItemAsFreezable = cloneAsIList[i] as Freezable;
                        if (isDeepClone && cloneItemAsFreezable != null && cloneItemAsFreezable != null)
                        {
                            Invariant.Assert(originalItemAsFreezable != cloneItemAsFreezable, "CloneCore didn't clone the elements in the list correctly.");
                        }
                    }
                }
            }
        }

        private static void Debug_VerifyInstance(String methodName, Freezable original, Freezable newInstance)
        {
            if (Invariant.Strict)
            {
                Invariant.Assert(newInstance != null, "{0} should not return null.", methodName);
                Invariant.Assert(newInstance.GetType() == original.GetType(),
                    String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0} should return instance of same type. (Expected= '{1}', Actual='{2}')",
                        methodName, original.GetType(), newInstance.GetType()));
                Invariant.Assert(!newInstance.IsFrozen, "{0} should return a mutable instance. Recieved a frozen instance.",
                    methodName);
            }
        }

        // Enumerates our FreezableContextPairs and (when we have full DP information)
        // verifies that the context is still valid.
        private void Debug_DetectContextLeaks()
        {
            if (Invariant.Strict)
            {
                if (Freezable_UsingSingletonContext)
                {
                    Debug_VerifyContextIsValid(SingletonContext, SingletonContextProperty);
                }
                else if (Freezable_UsingContextList)
                {
                    FrugalObjectList<FreezableContextPair> contextList = ContextList;

                    for(int i = 0, count = ContextList.Count; i < count; i++)
                    {
                        FreezableContextPair context = ContextList[i];                        
                        DependencyObject owner = (DependencyObject) context.Owner.Target;

                        if (!context.Owner.IsAlive)
                        {
                            // If the WeakReference is no longer alive the owner which
                            // was "using" this Freezable has been GC'ed.
                            //
                            // There is no way to verify that this object was pointing
                            // to us pre-collection, but in theory it was and we are
                            // just waiting for compaction.

                            continue;
                        }

                        Debug_VerifyContextIsValid(owner, context.Property);
                    }
                }
            }
        }

        // Verifies that the given owner/property pair constitutes a valid
        // inheritance context for this Freezable.  This is a no-op if the
        // property is null.
        private void Debug_VerifyContextIsValid(DependencyObject owner, DependencyProperty property)
        {
            if (Invariant.Strict)
            {
                Invariant.Assert(owner != null,
                    "We should not have null owners in the ContextList/SingletonContext.");

                if (property == null)
                {
                    // This context was not made through a DependencyProperty.  There is
                    // nothing we can verify.

                    return;
                }

                // If we have DP information for the context, we can verify that
                // the property on the owner is still referencing us.  Example:
                //
                //            (Pen.Brush)
                //
                //              .-----. 
                //             '       v
                //           Pen      Brush
                //             ^       .
                //              '-----' 
                //
                //              Context
                //
                // If the owner's DP value does not point to us than we've leaked
                // a context.

                DependencyObject ownerAsDO = (DependencyObject) owner;
                object effectiveValue = ownerAsDO.GetValue(property);

                // There is a notable exception to the rule above, which is that
                // ResourceDictionaries create a context between the resource and
                // the FE which owns the resource.
                //
                // In this case, the connection will be made via the pragmatic,
                // but somewhat arbitrarily chosen VisualBrush.Visual DP.
                //
                // See comments in ResourceDictionary.AddInheritanceContext.  Note
                // that the owner will be the FE which owns the ResourceDictionary,
                // not the ResourceDictionary itself.

                bool mayBeResourceDictionary =
                    property.Name == "Visual"
                    && property.OwnerType.FullName == "System.Windows.Media.VisualBrush"
                    && owner.GetType().FullName != "System.Windows.Media.VisualBrush";    // ResourceDictionaries may not be owned by a VisualBrush.

// Find a way to bring back context verification.
//                
//                Invariant.Assert(effectiveValue == this || mayBeResourceDictionary,
//                    String.Format(System.Globalization.CultureInfo.InvariantCulture,
//                        "Detected context leak: Property '{0}.{1}' on {2}.  Expected '{3}', Actual '{4}'",
//                        property.OwnerType.Name,
//                        property.Name,
//                        owner.GetType().FullName,
//                        this,
//                        effectiveValue));
            }
        }

        #endregion Debug
 
        //------------------------------------------------------
        //
        //  Private fields
        //
        //------------------------------------------------------

        #region Private Fields

        // For the common case of having only a single context, we use _property
        // to store the DependencyProperty that goes with the single context.
        private DependencyProperty _property;


        // initial size to make the EventStorage cache
        private const int INITIAL_EVENTSTORAGE_SIZE = 4;

        #endregion
    }
}
