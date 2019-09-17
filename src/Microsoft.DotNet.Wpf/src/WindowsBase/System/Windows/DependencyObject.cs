// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define NESTED_OPERATIONS_CHECK

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization; // For CultureInfo.InvariantCulture
using System.Reflection;

using System.Windows.Threading;

using MS.Utility;
using MS.Internal;
using MS.Internal.WindowsBase;
using System.Windows.Markup;

namespace System.Windows
{
    /// <summary>
    ///     DependencyObject is an object that participates in the property dependency system
    /// </summary>
    /// <remarks>
    ///     DependencyObject encompasses all property engine services. It's primary function
    ///     is providing facilities to compute a property's value based on other properties.<para/>
    ///
    ///     The Property Engine introduces a new type of property: attached properties. Attached
    ///     properties are identified via <see cref="DependencyProperty"/> and are read and
    ///     written using GetValue and SetValue.<para/>
    ///
    ///     Attached properties may be set and queried on any DependencyObject-derived type.<para/>
    ///
    ///     <see cref="Expression"/> is used to define relationships between properties. SetValue
    ///     is used to apply the Expression to the property on the instance.
    ///
    ///     DependencyObject services include the following:
    ///     <para/>
    ///     <list type="bullet">
    ///         <item>Dependency-based property value evaluation through Expressions</item>
    ///         <item>Property invalidation dependent traversal through Expressions</item>
    ///         <item>Attached property support</item>
    ///         <item>Invalidation notification services</item>
    ///     </list>
    /// </remarks>
    /// This attribute allows designers looking at metadata through TypeDescriptor to see dependency properties
    /// and attached properties.
    [System.ComponentModel.TypeDescriptionProvider(typeof(MS.Internal.ComponentModel.DependencyObjectProvider))]
    [System.Windows.Markup.NameScopeProperty("NameScope", typeof(System.Windows.NameScope))]
    public class DependencyObject : DispatcherObject
    {
        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public DependencyObject()
        {
            Initialize();
        }

        /// <summary>Returns the DType that represents the CLR type of this instance</summary>
        public DependencyObjectType DependencyObjectType
        {
            get
            {
                if (_dType == null)
                {
                    // Specialized type identification
                    _dType = DependencyObjectType.FromSystemTypeInternal(GetType());
                }

                // Do not call VerifyAccess because this method is trivial.
                return _dType;
            }
        }

        private void Initialize()
        {
            CanBeInheritanceContext = true;
            CanModifyEffectiveValues = true;
        }

        /// <summary>
        ///     Makes this object Read-Only state of this object; when in a Read-Only state, SetValue is not permitted,
        ///     though the effective value for a property may change.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal virtual void Seal()
        {
            Debug.Assert(!(this is Freezable), "A Freezable should not call DO's implementation of Seal()");

            // Currently DependencyObject.Seal() is semantically different than Freezable.Freeze().
            // Though freezing implies sealing the reverse isn't true.  The salient difference
            // here is that sealing a DependencyObject does not force all DPs on that object to
            // also be sealed.  Thus, when we Seal(), we promote all cached values to locally set
            // so that the user can continue to modify them.  Freezable types instead strip off
            // the promotion handler and freeze the cached default. Note that when / if we make
            // Seal() == Freeze this code should go away in favor of the behavior used for Freezables.
            PropertyMetadata.PromoteAllCachedDefaultValues(this);

            // Since Freeze doesn't call Seal the code below will also be duplicated in Freeze().

            // Since this object no longer changes it won't be able to notify dependents
            DependentListMapField.ClearValue(this);

            DO_Sealed = true;
        }

        /// <summary>
        ///     Indicates whether or not this object is in a Read-Only state; when in a Read-Only state, SetValue is not permitted,
        ///     though the effective value for a property may change.
        /// </summary>
        public bool IsSealed
        {
            get
            {
                return DO_Sealed;
            }
        }

        /// <summary>
        ///     We override Equals() to seal it to prevent custom Equals()
        ///     implementations.
        ///
        ///     There are only two scenarios where overriding Equals makes
        ///     sense:
        ///
        ///         1.  You are a value type (passed by copy).
        ///         2.  You are an immutable reference type (e.g., System.String).
        ///
        ///     Otherwise you are going to cause problems with keyed and
        ///     some types of sorted datastructures because your values
        ///     can mutate to be equals or not equals while they reside in
        ///     the store (bad news for System.Collections(.Generic)).
        ///
        ///     Furthermore, defining equality for two DOs is a very slippery
        ///     slope.  Are two brushes "equal" if they both paint red?  What
        ///     if one is only red this frame because it is animated?  What if
        ///     one is databound?  What if one is frozen?  ...and so on.
        ///
        ///     Since a DO can never be immutable (attached properties, etc.)
        ///     it makes sense to disallow overriding of Equals.
        /// </summary>
        public override sealed bool Equals(Object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        ///     CS0659: Required when overriding Equals().  Overriding
        ///     GetHashCode() is a bad idea for similar reasons.
        /// </summary>
        public override sealed int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Retrieve the value of a property
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <returns>The computed value</returns>
        public object GetValue(DependencyProperty dp)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            // Call Forwarded
            return GetValueEntry(
                    LookupEntry(dp.GlobalIndex),
                    dp,
                    null,
                    RequestFlags.FullyResolved).Value;
        }

        /// <summary>
        ///     This overload of GetValue returns UnsetValue if the property doesn't
        ///     have an entry in the _effectiveValues. This way we will avoid inheriting
        ///     the default value from the parent.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core.
        internal EffectiveValueEntry GetValueEntry(
            EntryIndex          entryIndex,
            DependencyProperty  dp,
            PropertyMetadata    metadata,
            RequestFlags        requests)
        {
            EffectiveValueEntry entry;

            if (dp.ReadOnly)
            {
                if (metadata == null)
                {
                    metadata = dp.GetMetadata(DependencyObjectType);
                }

                GetReadOnlyValueCallback getValueCallback = metadata.GetReadOnlyValueCallback;
                if (getValueCallback != null)
                {
                    BaseValueSourceInternal valueSource;
                    entry = new EffectiveValueEntry(dp);
                    entry.Value = getValueCallback(this, out valueSource);
                    entry.BaseValueSourceInternal = valueSource;
                    return entry;
                }
            }

            if (entryIndex.Found)
            {
                if ((requests & RequestFlags.RawEntry) != 0)
                {
                    entry = _effectiveValues[entryIndex.Index];
                }
                else
                {
                    entry = GetEffectiveValue(
                                entryIndex,
                                dp,
                                requests);
                }
            }
            else
            {
                entry = new EffectiveValueEntry(dp, BaseValueSourceInternal.Unknown);
            }

            if (entry.Value == DependencyProperty.UnsetValue)
            {
                if (dp.IsPotentiallyInherited)
                {
                    if (metadata == null)
                    {
                        metadata = dp.GetMetadata(DependencyObjectType);
                    }

                    if (metadata.IsInherited)
                    {
                        DependencyObject inheritanceParent = InheritanceParent;
                        if (inheritanceParent != null)
                        {
                            entryIndex = inheritanceParent.LookupEntry(dp.GlobalIndex);

                            if (entryIndex.Found)
                            {
                                entry = inheritanceParent.GetEffectiveValue(
                                                entryIndex,
                                                dp,
                                                requests & RequestFlags.DeferredReferences);
                                entry.BaseValueSourceInternal = BaseValueSourceInternal.Inherited;
                            }
                        }
                    }

                    if (entry.Value != DependencyProperty.UnsetValue)
                    {
                        return entry;
                    }
                }

                if ((requests & RequestFlags.SkipDefault) == 0)
                {
                    if (dp.IsPotentiallyUsingDefaultValueFactory)
                    {
                        if (metadata == null)
                        {
                            metadata = dp.GetMetadata(DependencyObjectType);
                        }

                        if (((requests & (RequestFlags.DeferredReferences | RequestFlags.RawEntry)) != 0) && metadata.UsingDefaultValueFactory)
                        {
                            entry.BaseValueSourceInternal = BaseValueSourceInternal.Default;

                            entry.Value = new DeferredMutableDefaultReference(metadata, this, dp);
                            return entry;
                        }
                    }
                    else if (!dp.IsDefaultValueChanged)
                    {
                        return EffectiveValueEntry.CreateDefaultValueEntry(dp, dp.DefaultMetadata.DefaultValue);
                    }

                    if (metadata == null)
                    {
                        metadata = dp.GetMetadata(DependencyObjectType);
                    }

                    return EffectiveValueEntry.CreateDefaultValueEntry(dp, metadata.GetDefaultValue(this, dp));
                }
            }
            return entry;
        }

        /// <summary>
        ///     This overload of GetValue assumes that entryIndex is valid.
        ///      It also does not do the check storage on the InheritanceParent.
        /// </summary>
        private EffectiveValueEntry GetEffectiveValue(
            EntryIndex          entryIndex,
            DependencyProperty  dp,
            RequestFlags        requests)
        {
            EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];
            EffectiveValueEntry effectiveEntry = entry.GetFlattenedEntry(requests);

            if (((requests & (RequestFlags.DeferredReferences | RequestFlags.RawEntry)) != 0) || !effectiveEntry.IsDeferredReference)
            {
                return effectiveEntry;
            }

            if (!entry.HasModifiers)
            {
                // For thread-safety, sealed DOs can't modify _effectiveValues.
                Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

                if (!entry.HasExpressionMarker)
                {
                    // The value for this property was meant to come from a dictionary
                    // and the creation of that value had been deferred until this
                    // time for better performance. Now is the time to actually instantiate
                    // this value by querying it from the dictionary. Once we have the
                    // value we can actually replace the deferred reference marker
                    // with the actual value.
                    DeferredReference reference = (DeferredReference)entry.Value;
                    object value = reference.GetValue(entry.BaseValueSourceInternal);

                    if (!dp.IsValidValue(value))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.InvalidPropertyValue, value, dp.Name));
                    }

                    // Make sure the entryIndex is in sync after
                    // the inflation of the deferred reference.
                    entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);

                    entry.Value = value;

                    _effectiveValues[entryIndex.Index] = entry;
                    return entry;
                }
            }
            else
            {
                // The value for this property was meant to come from a dictionary
                // and the creation of that value had been deferred until this
                // time for better performance. Now is the time to actually instantiate
                // this value by querying it from the dictionary. Once we have the
                // value we can actually replace the deferred reference marker
                // with the actual value.

                ModifiedValue modifiedValue = entry.ModifiedValue;
                DeferredReference reference = null;
                bool referenceFromExpression = false;

                if (entry.IsCoercedWithCurrentValue)
                {
                    if (!entry.IsAnimated)
                    {
                        reference = modifiedValue.CoercedValue as DeferredReference;
                    }
                }

                if (reference == null && entry.IsExpression)
                {
                    if (!entry.IsAnimated && !entry.IsCoerced)
                    {
                        reference = (DeferredReference) modifiedValue.ExpressionValue;
                        referenceFromExpression = true;
                    }
                }

                Debug.Assert(reference != null, "the only modified values that can have deferredreferences are (a) expression, (b) coerced control value");
                if (reference == null)
                {
                    return effectiveEntry;
                }

                object value = reference.GetValue(entry.BaseValueSourceInternal);

                if (!dp.IsValidValue(value))
                {
                    throw new InvalidOperationException(SR.Get(SRID.InvalidPropertyValue, value, dp.Name));
                }

                // Make sure the entryIndex is in sync after
                // the inflation of the deferred reference.
                entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);

                if (referenceFromExpression)
                {
                    entry.SetExpressionValue(value, modifiedValue.BaseValue);
                }
                else
                {
                    entry.SetCoercedValue(value, null, true /* skipBaseValueChecks */, entry.IsCoercedWithCurrentValue);
                }

                _effectiveValues[entryIndex.Index] = entry;

                effectiveEntry.Value = value;
            }
            return effectiveEntry;
        }

        /// <summary>
        ///     Sets the local value of a property
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <param name="value">New local value</param>
        public void SetValue(DependencyProperty dp, object value)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, value, metadata, false /* coerceWithDeferredReference */, false /* coerceWithCurrentValue */, OperationType.Unknown, false /* isInternal */);
        }

        /// <summary>
        ///     Sets the value of a property without changing its value source.
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <param name="value">New value</param>
        /// <remarks>
        ///     This method is intended for use by a component that wants to
        ///     programmatically set the value of one of its own properties, in a
        ///     way that does not disable an application's declared use of that property.
        ///     SetCurrentValue changes the effective value of the property, but
        ///     existing triggers, data-binding, styles, etc. will continue to
        ///     work.
        /// </remarks>
        public void SetCurrentValue(DependencyProperty dp, object value)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            this.VerifyAccess();

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, value, metadata, false /* coerceWithDeferredReference */, true /* coerceWithCurrentValue */, OperationType.Unknown, false /* isInternal */);
        }

        /// <summary>
        ///     Sets the local value of a property
        /// The purpose of this internal method is to reuse BooleanBoxes when setting boolean value
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <param name="value">New local value</param>
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void SetValue(DependencyProperty dp, bool value)
        {
            SetValue(dp, MS.Internal.KnownBoxes.BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     Sets the current value of a property
        /// The purpose of this internal method is to reuse BooleanBoxes when setting boolean value
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <param name="value">New local value</param>
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void SetCurrentValue(DependencyProperty dp, bool value)
        {
            SetCurrentValue(dp, MS.Internal.KnownBoxes.BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     Internal version of SetValue that bypasses type check in IsValidValue;
        ///     This is used in property setters
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <param name="value">New local value</param>
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void SetValueInternal(DependencyProperty dp, object value)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, value, metadata, false /* coerceWithDeferredReference */, false /* coerceWithCurrentValue */, OperationType.Unknown, true /* isInternal */);
        }

        /// <summary>
        ///     Internal version of SetCurrentValue that bypasses type check in IsValidValue;
        ///     This is used in property setters
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <param name="value">New local value</param>
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void SetCurrentValueInternal(DependencyProperty dp, object value)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, value, metadata, false /* coerceWithDeferredReference */, true /* coerceWithCurrentValue */, OperationType.Unknown, true /* isInternal */);
        }

        /// <summary>
        /// Sets the local value of a property.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void SetDeferredValue(DependencyProperty dp, DeferredReference deferredReference)
        {
            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, deferredReference, metadata, true /* coerceWithDeferredReference */, false /* coerceWithCurrentValue */, OperationType.Unknown, false /* isInternal */);
        }

        /// <summary>
        /// Sets the value of a property to a deferred reference, without changing the ValueSource.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void SetCurrentDeferredValue(DependencyProperty dp, DeferredReference deferredReference)
        {
            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, deferredReference, metadata, true /* coerceWithDeferredReference */, true /* coerceWithCurrentValue */, OperationType.Unknown, false /* isInternal */);
        }

        /// <summary>
        /// Sets the local value of a property with a mutable default value.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void SetMutableDefaultValue(DependencyProperty dp, object value)
        {
            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            // Do standard property set
            SetValueCommon(dp, value, metadata, false /* coerceWithDeferredReference */, false /* coerceWithCurrentValue */, OperationType.ChangeMutableDefaultValue, false /* isInternal */);
        }

        /// <summary>
        ///     Sets the local value of a property
        /// The purpose of this internal method is to reuse BooleanBoxes when setting boolean value
        /// </summary>
        /// <param name="dp">Dependency property key</param>
        /// <param name="value">New local value</param>
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void SetValue(DependencyPropertyKey dp, bool value)
        {
            SetValue(dp, MS.Internal.KnownBoxes.BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     Sets the local value of a property
        /// </summary>
        public void SetValue(DependencyPropertyKey key, object value)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            DependencyProperty dp;

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(key, out dp);

            // Do standard property set
            SetValueCommon(dp, value, metadata, false /* coerceWithDeferredReference */, false /* coerceWithCurrentValue */, OperationType.Unknown, false /* isInternal */);
        }

        /// <summary>
        ///     Called by SetValue or ClearValue to verify that the property
        /// can be changed.
        /// </summary>
        private PropertyMetadata SetupPropertyChange(DependencyProperty dp)
        {
            if ( dp != null )
            {
                if ( !dp.ReadOnly )
                {
                    // Get type-specific metadata for this property
                    return dp.GetMetadata(DependencyObjectType);
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.ReadOnlyChangeNotAllowed, dp.Name));
                }
            }
            else
            {
                throw new ArgumentNullException("dp");
            }
        }

        /// <summary>
        ///     Called by SetValue or ClearValue to verify that the property
        /// can be changed.
        /// </summary>
        private PropertyMetadata SetupPropertyChange(DependencyPropertyKey key, out DependencyProperty dp)
        {
            if ( key != null )
            {
                dp = key.DependencyProperty;
                Debug.Assert(dp != null);

                dp.VerifyReadOnlyKey(key);

                // Get type-specific metadata for this property
                return dp.GetMetadata(DependencyObjectType);
            }
            else
            {
                throw new ArgumentNullException("key");
            }
        }

        /// <summary>
        ///     The common code shared by all variants of SetValue
        /// </summary>
        // Takes metadata from caller because most of them have already retrieved it
        //  for their own purposes, avoiding the duplicate GetMetadata call.
        private void SetValueCommon(
            DependencyProperty  dp,
            object              value,
            PropertyMetadata    metadata,
            bool                coerceWithDeferredReference,
            bool                coerceWithCurrentValue,
            OperationType       operationType,
            bool                isInternal)
        {
            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.SetOnReadOnlyObjectNotAllowed, this));
            }

            Expression newExpr = null;
            DependencySource[] newSources = null;

            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

            // Treat Unset as a Clear
            if( value == DependencyProperty.UnsetValue )
            {
                Debug.Assert(!coerceWithCurrentValue, "Don't call SetCurrentValue with UnsetValue");
                // Parameters should have already been validated, so we call
                //  into the private method to avoid validating again.
                ClearValueCommon(entryIndex, dp, metadata);
                return;
            }

            // Validate the "value" against the DP.
            bool isDeferredReference = false;
            bool newValueHasExpressionMarker = (value == ExpressionInAlternativeStore);

            // First try to validate the value; only after this validation fails should we
            // do the more expensive checks (type checks) for the less common scenarios
            if (!newValueHasExpressionMarker)
            {
                bool isValidValue = isInternal ? dp.IsValidValueInternal(value) : dp.IsValidValue(value);

                // for properties of type "object", we have to always check for expression & deferredreference
                if (!isValidValue || dp.IsObjectType)
                {
                    // 2nd most common is expression
                    newExpr = value as Expression;
                    if (newExpr != null)
                    {
                        // For Expressions, perform additional validation
                        // Make sure Expression is "attachable"
                        if (!newExpr.Attachable)
                        {
                            throw new ArgumentException(SR.Get(SRID.SharingNonSharableExpression));
                        }

                        // Check dispatchers of all Sources
                        // CALLBACK
                        newSources = newExpr.GetSources();
                        ValidateSources(this, newSources, newExpr);
                    }
                    else
                    {
                        // and least common is DeferredReference
                        isDeferredReference = (value is DeferredReference);
                        if (!isDeferredReference)
                        {
                            if (!isValidValue)
                            {
                                // it's not a valid value & it's not an expression, so throw
                                throw new ArgumentException(SR.Get(SRID.InvalidPropertyValue, value, dp.Name));
                            }
                        }
                    }
                }
            }

            // Get old value
            EffectiveValueEntry oldEntry;
            if (operationType == OperationType.ChangeMutableDefaultValue)
            {
                oldEntry = new EffectiveValueEntry(dp, BaseValueSourceInternal.Default);
                oldEntry.Value = value;
            }
            else
            {
                oldEntry = GetValueEntry(entryIndex, dp, metadata, RequestFlags.RawEntry);
            }

            // if there's an expression in some other store, fetch it now
            Expression currentExpr =
                    (oldEntry.HasExpressionMarker)  ? _getExpressionCore(this, dp, metadata)
                  : (oldEntry.IsExpression)         ? (oldEntry.LocalValue as Expression)
                  :                                   null;

            // Allow expression to store value if new value is
            // not an Expression, if applicable

            bool handled = false;
            if ((currentExpr != null) && (newExpr == null))
            {
                // Resolve deferred references because we haven't modified
                // the expression code to work with DeferredReference yet.
                if (isDeferredReference)
                {
                    value = ((DeferredReference) value).GetValue(BaseValueSourceInternal.Local);
                }

                // CALLBACK
                handled = currentExpr.SetValue(this, dp, value);
                entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
            }

            // Create the new effective value entry
            EffectiveValueEntry newEntry;
            if (handled)
            {
                // If expression handled set, then done
                if (entryIndex.Found)
                {
                    newEntry = _effectiveValues[entryIndex.Index];
                }
                else
                {
                    // the expression.SetValue resulted in this value being removed from the table;
                    // use the default value.
                    newEntry = EffectiveValueEntry.CreateDefaultValueEntry(dp, metadata.GetDefaultValue(this, dp));
                }

                coerceWithCurrentValue = false; // expression already handled the control-value
            }
            else
            {
                // allow a control-value to coerce an expression value, when the
                // expression didn't handle the value
                if (coerceWithCurrentValue && currentExpr != null)
                {
                    currentExpr = null;
                }

                newEntry = new EffectiveValueEntry(dp, BaseValueSourceInternal.Local);

                // detach the old expression, if applicable
                if (currentExpr != null)
                {
                    // CALLBACK
                    DependencySource[] currentSources = currentExpr.GetSources();

                    UpdateSourceDependentLists(this, dp, currentSources, currentExpr, false);  // Remove

                    // CALLBACK
                    currentExpr.OnDetach(this, dp);
                    currentExpr.MarkDetached();
                    entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
                }

                // attach the new expression, if applicable
                if (newExpr == null)
                {
                    // simple local value set
                    newEntry.HasExpressionMarker = newValueHasExpressionMarker;
                    newEntry.Value = value;
                }
                else
                {
                    Debug.Assert(!coerceWithCurrentValue, "Expression values not supported in SetCurrentValue");

                    // First put the expression in the effectivevalueentry table for this object;
                    // this allows the expression to update the value accordingly in OnAttach
                    SetEffectiveValue(entryIndex, dp, dp.GlobalIndex, metadata, newExpr, BaseValueSourceInternal.Local);

                    // Before the expression is attached it has default value
                    object defaultValue = metadata.GetDefaultValue(this, dp);
                    entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
                    SetExpressionValue(entryIndex, defaultValue, newExpr);
                    UpdateSourceDependentLists(this, dp, newSources, newExpr, true);  // Add

                    newExpr.MarkAttached();

                    // CALLBACK
                    newExpr.OnAttach(this, dp);

                    // the attach may have added entries in the effective value table ...
                    // so, update the entryIndex accordingly.
                    entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);

                    newEntry = EvaluateExpression(
                            entryIndex,
                            dp,
                            newExpr,
                            metadata,
                            oldEntry,
                            _effectiveValues[entryIndex.Index]);

                    entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
                }
            }

            UpdateEffectiveValue(
                entryIndex,
                dp,
                metadata,
                oldEntry,
                ref newEntry,
                coerceWithDeferredReference,
                coerceWithCurrentValue,
                operationType);
        }

        //
        //  This is a helper routine to set this DO as the inheritance context of another,
        //  which has been set as a DP value here.
        //
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool ProvideSelfAsInheritanceContext( object value, DependencyProperty dp )
        {
            DependencyObject doValue = value as DependencyObject;
            if (doValue != null)
            {
                return ProvideSelfAsInheritanceContext(doValue, dp);
            }
            else
            {
                return false;
            }
        }

        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool ProvideSelfAsInheritanceContext( DependencyObject doValue, DependencyProperty dp )
        {
            // We have to call Freezable.AddInheritanceContext even if the request
            // for a new InheritanceContext is not allowed, because Freezable depends
            // on side-effects from setting the "Freezable context".  Freezable's
            // implementation does its own checks of the conditions omitted here.
            // Enhancement suggestion: Freezable should follow the same rules for
            // InheritanceContext as everyone else 


            if (doValue != null &&
                this.ShouldProvideInheritanceContext(doValue, dp) &&
                (doValue is Freezable ||
                    (this.CanBeInheritanceContext &&
                     !doValue.IsInheritanceContextSealed)
                ))
            {
                DependencyObject oldInheritanceContext = doValue.InheritanceContext;
                doValue.AddInheritanceContext(this, dp);

                // return true if the inheritance context actually changed to the new value
                return (this == doValue.InheritanceContext && this != oldInheritanceContext);
            }
            else
            {
                return false;
            }
        }

        //
        //  This is a helper routine to remove this DO as the inheritance context of another.
        //
        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool RemoveSelfAsInheritanceContext( object value, DependencyProperty dp )
        {
            DependencyObject doValue = value as DependencyObject;
            if (doValue != null)
            {
                return RemoveSelfAsInheritanceContext(doValue, dp);
            }
            else
            {
                return false;
            }
        }

        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal bool RemoveSelfAsInheritanceContext( DependencyObject doValue, DependencyProperty dp )
        {
            // We have to call Freezable.RemoveInheritanceContext even if the request
            // for a new InheritanceContext is not allowed, because Freezable depends
            // on side-effects from setting the "Freezable context".  Freezable's
            // implementation does its own checks of the conditions omitted here.
            // Enhancement suggestion: Freezable should follow the same rules for
            // InheritanceContext as everyone else 


            if (doValue != null &&
                this.ShouldProvideInheritanceContext(doValue, dp) &&
                (doValue is Freezable ||
                    (this.CanBeInheritanceContext &&
                     !doValue.IsInheritanceContextSealed)
                ))
            {
                DependencyObject oldInheritanceContext = doValue.InheritanceContext;
                doValue.RemoveInheritanceContext(this, dp);

                // return true if the inheritance context actually changed to the new value
                return (this == oldInheritanceContext && doValue.InheritanceContext != oldInheritanceContext);
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        ///     Clears the local value of a property
        /// </summary>
        /// <param name="dp">Dependency property</param>
        public void ClearValue(DependencyProperty dp)
        {
            // Do not allow foreign threads to clear properties.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(dp);

            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

            ClearValueCommon(entryIndex, dp, metadata);
        }

        /// <summary>
        ///     Clears the local value of a property
        /// </summary>
        public void ClearValue(DependencyPropertyKey key)
        {
            // Do not allow foreign threads to clear properties.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            DependencyProperty dp;

            // Cache the metadata object this method needed to get anyway.
            PropertyMetadata metadata = SetupPropertyChange(key, out dp);

            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

            ClearValueCommon(entryIndex, dp, metadata);
        }

        /// <summary>
        ///     The common code shared by all variants of ClearValue
        /// </summary>
        private void ClearValueCommon(EntryIndex entryIndex, DependencyProperty dp, PropertyMetadata metadata)
        {
            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.ClearOnReadOnlyObjectNotAllowed, this));
            }

            // Get old value
            EffectiveValueEntry oldEntry = GetValueEntry(
                                        entryIndex,
                                        dp,
                                        metadata,
                                        RequestFlags.RawEntry);

            // Get current local value
            // (No need to go through read local callback, just checking
            // for presence of Expression)
            object current = oldEntry.LocalValue;

            // Get current expression
            Expression currentExpr = (oldEntry.IsExpression) ? (current as Expression) : null;

            // Inform value expression of detachment, if applicable
            if (currentExpr != null)
            {
                // CALLBACK
                DependencySource[] currentSources = currentExpr.GetSources();

                UpdateSourceDependentLists(this, dp, currentSources, currentExpr, false);  // Remove

                // CALLBACK
                currentExpr.OnDetach(this, dp);
                currentExpr.MarkDetached();
                entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
            }

            // valuesource == Local && value == UnsetValue indicates that we are clearing the local value
            EffectiveValueEntry newEntry = new EffectiveValueEntry(dp, BaseValueSourceInternal.Local);

            // Property is now invalid
            UpdateEffectiveValue(
                    entryIndex,
                    dp,
                    metadata,
                    oldEntry,
                    ref newEntry,
                    false /* coerceWithDeferredReference */,
                    false /* coerceWithCurrentValue */,
                    OperationType.Unknown);
        }

        /// <summary>
        ///     This method is called by DependencyObjectPropertyDescriptor to determine
        ///     if a value is set for a given DP.
        /// </summary>
        internal bool ContainsValue(DependencyProperty dp)
        {
            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

            if (!entryIndex.Found)
            {
                return false;
            }

            EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];
            object value = entry.IsCoercedWithCurrentValue ? entry.ModifiedValue.CoercedValue : entry.LocalValue;
            return !object.ReferenceEquals(value, DependencyProperty.UnsetValue);
        }

        //
        // Changes the sources of an existing Expression
        //
        internal static void ChangeExpressionSources(Expression expr, DependencyObject d, DependencyProperty dp, DependencySource[] newSources)
        {
            if (!expr.ForwardsInvalidations)
            {
                // Get current local value (should be provided Expression)
                // (No need to go through read local callback, just checking
                // for presence of Expression)
                EntryIndex entryIndex = d.LookupEntry(dp.GlobalIndex);

                if (!entryIndex.Found || (d._effectiveValues[entryIndex.Index].LocalValue != expr))
                {
                    throw new ArgumentException(SR.Get(SRID.SourceChangeExpressionMismatch));
                }
            }

            // Get current sources
            // CALLBACK
            DependencySource[] currentSources = expr.GetSources();

            // Remove old
            if (currentSources != null)
            {
                UpdateSourceDependentLists(d, dp, currentSources, expr, false);  // Remove
            }

            // Add new
            if (newSources != null)
            {
                UpdateSourceDependentLists(d, dp, newSources, expr, true);  // Add
            }
        }

        /// <summary>
        ///     Coerce a property value
        /// </summary>
        /// <param name="dp">Dependency property</param>
        public void CoerceValue(DependencyProperty dp)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);
            PropertyMetadata metadata = dp.GetMetadata(DependencyObjectType);

            // if the property has a coerced-with-control value, apply the coercion
            // to that value.  This is done by simply calling SetCurrentValue.
            if (entryIndex.Found)
            {
                EffectiveValueEntry entry = GetValueEntry(entryIndex, dp, metadata, RequestFlags.RawEntry);
                if (entry.IsCoercedWithCurrentValue)
                {
                    SetCurrentValue(dp, entry.ModifiedValue.CoercedValue);
                    return;
                }
            }

            // IsCoerced == true && value == UnsetValue indicates that we need to re-coerce this value
            EffectiveValueEntry newEntry = new EffectiveValueEntry(dp, FullValueSource.IsCoerced);

            UpdateEffectiveValue(
                    entryIndex,
                    dp,
                    metadata,
                    new EffectiveValueEntry() /* oldEntry */,
                    ref newEntry,
                    false /* coerceWithDeferredReference */,
                    false /* coerceWithCurrentValue */,
                    OperationType.Unknown);
        }

        /// <summary>
        ///     This is to enable some performance-motivated shortcuts in property
        /// invalidation.  When this is called, it means the caller knows the
        /// value of the property is pointing to the same object instance as
        /// before, but the meaning has changed because something within that
        /// object has changed.
        /// </summary>
        /// <remarks>
        /// Clients who are unaware of this will still behave correctly, if not
        ///  particularly performant, by assuming that we have a new instance.
        /// Since invalidation operations are synchronous, we can set a bit
        ///  to maintain this knowledge through the invalidation operation.
        /// This would be problematic in cross-thread operations, but the only
        ///  time DependencyObject can be used across thread in today's design
        ///  is when it is a Freezable object that has been Frozen.  Frozen
        ///  means no more changes, which means no more invalidations.
        ///
        /// </remarks>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void InvalidateSubProperty(DependencyProperty dp)
        {
            // when a sub property changes, send a Changed notification with old and new value being the same, and with
            // IsASubPropertyChange set to true
            NotifyPropertyChange(new DependencyPropertyChangedEventArgs(dp, dp.GetMetadata(DependencyObjectType), GetValue(dp)));
        }

        /// <summary>
        ///     Notify the current DependencyObject that a "sub-property"
        /// change has occurred on the given DependencyProperty.
        /// </summary>
        /// <remarks>
        /// This does the same work as InvalidateSubProperty, and in addition
        /// it raise the Freezable.Changed event if the current DependencyObject
        /// is a Freezable.  This method should be called whenever an
        /// intermediate object is responsible for propagating the Freezable.Changed
        /// event (i.e. when the Freezable system doesn't propagate the event itself).
        /// </remarks>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void NotifySubPropertyChange(DependencyProperty dp)
        {
            InvalidateSubProperty(dp);

            // if the target is a Freezable, call FireChanged to kick off
            // notifications to the Freezable's parent chain.
            Freezable freezable = this as Freezable;
            if (freezable != null)
            {
                freezable.FireChanged();
            }
        }

        /// <summary>
        ///     Invalidates a property
        /// </summary>
        /// <param name="dp">Dependency property</param>
        public void InvalidateProperty(DependencyProperty dp)
        {
            InvalidateProperty(dp, preserveCurrentValue:false);
        }

        // Invalidation, optionally preserving the current value if the base
        // value doesn't change.
        //  The flag is set only by triggers, as a workaround for a missing API.
        // When we added SetCurrentValue, we should have also added ClearCurrentValue
        // to give controls a way to remove the current value. Lacking that, controls use
        // InvalidateProperty (VirtualizingStackPanel does this), relying on behavior
        // that should also have been different - an invalidation that doesn't change
        // the base value should preserve the current value.
        //  This matters for triggers. When any input to a
        // trigger condition changes, the trigger simply invalidates all the properties
        // mentioned in its setters.  These invalidations often discover no value change -
        // (example:  Trigger condition is "IsVisible && HasErrors";  if IsVisible is false,
        // changing input HasErrors won't change the condition value.   The dependent
        // properties are invalidated, but they don't actually change value.)
        //  To fix the bug, we are putting in the "preserve current value" behavior, but
        // only for invalidations that come from triggers.
        internal void InvalidateProperty(DependencyProperty dp, bool preserveCurrentValue)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            EffectiveValueEntry newEntry = new EffectiveValueEntry(dp, BaseValueSourceInternal.Unknown);
            newEntry.IsCoercedWithCurrentValue = preserveCurrentValue;

            UpdateEffectiveValue(
                    LookupEntry(dp.GlobalIndex),
                    dp,
                    dp.GetMetadata(DependencyObjectType),
                    new EffectiveValueEntry() /* oldEntry */,
                    ref newEntry,
                    false /* coerceWithDeferredReference */,
                    false /* coerceWithCurrentValue */,
                    OperationType.Unknown);
        }

        //
        //  This method
        //  1. Re-evaluates the effective value for the given property and fires the property changed notification
        //  2. When this method is invoked with the coersion flag set to false it means that we will simply
        //     coerce and will not try to re-evaluate the base value for the property
        //
        [FriendAccessAllowed] // Declared in Base also used in Framework
        internal UpdateResult UpdateEffectiveValue(
                EntryIndex          entryIndex,
                DependencyProperty  dp,
                PropertyMetadata    metadata,
                EffectiveValueEntry oldEntry,
            ref EffectiveValueEntry newEntry,
                bool                coerceWithDeferredReference,
                bool                coerceWithCurrentValue,
                OperationType       operationType)
        {
            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

#region EventTracing
#if VERBOSE_PROPERTY_EVENT
            bool isDynamicTracing = EventTrace.IsEnabled(EventTrace.Flags.performance, EventTrace.Level.verbose); // This was under "normal"
            if (isDynamicTracing)
            {
                ++InvalidationCount;
                if( InvalidationCount % 100 == 0 )
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYINVALIDATIONGUID,
                                                         MS.Utility.EventType.Info,
                                                         InvalidationCount );
                }

                string TypeAndName = String.Format(CultureInfo.InvariantCulture, "[{0}]{1}({2})",GetType().Name,dp.Name,base.GetHashCode()); // FxCop wanted the CultureInfo.InvariantCulture

                EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYINVALIDATIONGUID,
                                                     MS.Utility.EventType.StartEvent,
                                                     base.GetHashCode(), TypeAndName); // base.GetHashCode() to avoid calling a virtual, which FxCop doesn't like.
            }
#endif


#endregion EventTracing

#if NESTED_OPERATIONS_CHECK
            // Are we invalidating out of control?
            if( NestedOperations > NestedOperationMaximum )
            {
                // We're invalidating out of control, time to abort.
                throw new InvalidOperationException("Too many levels of nested DependencyProperty invalidations.  This usually indicates a circular reference in the application and the cycle needs to be broken.");
            }
            NestedOperations++; // Decrement in the finally block
#endif
            int targetIndex = dp.GlobalIndex;

            if (oldEntry.BaseValueSourceInternal == BaseValueSourceInternal.Unknown)
            {
                // Do a full get value of the old entry if it isn't supplied.
                // It isn't supplied in cases where we are *unsetting* a value
                // (e.g. ClearValue, style unapply, trigger unapply)
                oldEntry = GetValueEntry(
                                    entryIndex,
                                    dp,
                                    metadata,
                                    RequestFlags.RawEntry);
            }

            object oldValue = oldEntry.GetFlattenedEntry(RequestFlags.FullyResolved).Value;


            /*
            if( TraceDependencyProperty.IsEnabled )
            {
                TraceDependencyProperty.Trace(
                    TraceEventType.Verbose,
                    TraceDependencyProperty.UpdateEffectiveValueStart,
                    this,
                    dp,
                    dp.OwnerType,
                    oldValue,
                    oldEntry.BaseValueSourceInternal );
            }
            */

            // for control-value coercion, extract the desired control value, then
            // reset the new entry to ask for a re-evaluation with coercion
            object controlValue = null;
            if (coerceWithCurrentValue)
            {
                controlValue = newEntry.Value;
                newEntry = new EffectiveValueEntry(dp, FullValueSource.IsCoerced);
            }

            // check for early-out opportunities:
            //  1) the new entry is of lower priority than the current entry
            if ((newEntry.BaseValueSourceInternal != BaseValueSourceInternal.Unknown) &&
                (newEntry.BaseValueSourceInternal < oldEntry.BaseValueSourceInternal))
            {
                return 0;
            }

            bool isReEvaluate = false;
            bool isCoerceValue = false;
            bool isClearValue = false;

            if (newEntry.Value == DependencyProperty.UnsetValue)
            {
                FullValueSource fullValueSource = newEntry.FullValueSource;
                isCoerceValue = (fullValueSource == FullValueSource.IsCoerced);
                isReEvaluate = true;

                if (newEntry.BaseValueSourceInternal == BaseValueSourceInternal.Local)
                {
                    isClearValue = true;
                }
            }

            // if we're not in an animation update (caused by AnimationStorage.OnCurrentTimeInvalidated)
            // then always force a re-evaluation if (a) there was an animation in play or (b) there's
            // an expression evaluation to be made
            if (isReEvaluate ||
                (!newEntry.IsAnimated &&
                 (oldEntry.IsAnimated ||
                 (oldEntry.IsExpression && newEntry.IsExpression && (newEntry.ModifiedValue.BaseValue == oldEntry.ModifiedValue.BaseValue)))))
            {
                // we have to compute the new value
                if (!isCoerceValue)
                {
                    newEntry = EvaluateEffectiveValue(entryIndex, dp, metadata, oldEntry, newEntry, operationType);

                    // Make sure that the call out did not cause a change to entryIndex
                    entryIndex = CheckEntryIndex(entryIndex, targetIndex);

                    bool found = (newEntry.Value != DependencyProperty.UnsetValue);
                    if (!found && metadata.IsInherited)
                    {
                        DependencyObject inheritanceParent = InheritanceParent;
                        if (inheritanceParent != null)
                        {
                            // Fetch the IsDeferredValue flag from the InheritanceParent
                            EntryIndex parentEntryIndex = inheritanceParent.LookupEntry(dp.GlobalIndex);
                            if (parentEntryIndex.Found)
                            {
                                found = true;
                                newEntry = inheritanceParent._effectiveValues[parentEntryIndex.Index].GetFlattenedEntry(RequestFlags.FullyResolved);
                                newEntry.BaseValueSourceInternal = BaseValueSourceInternal.Inherited;
                            }
                        }
                    }

                    // interesting that I just had to add this ... suggests that we are now overinvalidating
                    if (!found)
                    {
                        newEntry = EffectiveValueEntry.CreateDefaultValueEntry(dp, metadata.GetDefaultValue(this, dp));
                    }
                }
                else
                {
                    if (!oldEntry.HasModifiers)
                    {
                        newEntry = oldEntry;
                    }
                    else
                    {
                        newEntry = new EffectiveValueEntry(dp, oldEntry.BaseValueSourceInternal);
                        ModifiedValue modifiedValue = oldEntry.ModifiedValue;
                        object baseValue = modifiedValue.BaseValue;
                        newEntry.Value = baseValue;
                        newEntry.HasExpressionMarker = oldEntry.HasExpressionMarker;

                        if (oldEntry.IsExpression)
                        {
                            newEntry.SetExpressionValue(modifiedValue.ExpressionValue, baseValue);
                        }

                        if (oldEntry.IsAnimated)
                        {
                            newEntry.SetAnimatedValue(modifiedValue.AnimatedValue, baseValue);
                        }
                    }
                }
            }

            // Coerce to current value
            if (coerceWithCurrentValue)
            {
                object baseValue = newEntry.GetFlattenedEntry(RequestFlags.CoercionBaseValue).Value;

                ProcessCoerceValue(
                    dp,
                    metadata,
                    ref entryIndex,
                    ref targetIndex,
                    ref newEntry,
                    ref oldEntry,
                    ref oldValue,
                    baseValue,
                    controlValue,
                    null /*coerceValueCallback */,
                    coerceWithDeferredReference,
                    coerceWithCurrentValue,
                    false /*skipBaseValueChecks*/);

                // Make sure that the call out did not cause a change to entryIndex
                entryIndex = CheckEntryIndex(entryIndex, targetIndex);
            }

            // Coerce Value
            if (metadata.CoerceValueCallback != null &&
                !(isClearValue && newEntry.FullValueSource == (FullValueSource)BaseValueSourceInternal.Default))
            {
                // CALLBACK
                object baseValue = newEntry.GetFlattenedEntry(RequestFlags.CoercionBaseValue).Value;

                ProcessCoerceValue(
                    dp,
                    metadata,
                    ref entryIndex,
                    ref targetIndex,
                    ref newEntry,
                    ref oldEntry,
                    ref oldValue,
                    baseValue,
                    null /* controlValue */,
                    metadata.CoerceValueCallback,
                    coerceWithDeferredReference,
                    false /* coerceWithCurrentValue */,
                    false /*skipBaseValueChecks*/);

                // Make sure that the call out did not cause a change to entryIndex
                entryIndex = CheckEntryIndex(entryIndex, targetIndex);
            }

            // The main difference between this callback and the metadata.CoerceValueCallback is that
            // designers want to be able to coerce during all value changes including a change to the
            // default value. Whereas metadata.CoerceValueCallback coerces all property values but the
            // default, because default values are meant to fit automatically fit into the coersion constraint.

            if (dp.DesignerCoerceValueCallback != null)
            {
                // During a DesignerCoerceValueCallback the value obtained is stored in the same
                // member as the metadata.CoerceValueCallback. In this case we do not store the
                // baseValue in the entry. Thus the baseValue checks will the violated. That is the
                // reason for skipping these checks in this one case.

                // Also before invoking the DesignerCoerceValueCallback the baseValue must
                // always be expanded if it is a DeferredReference

                ProcessCoerceValue(
                    dp,
                    metadata,
                    ref entryIndex,
                    ref targetIndex,
                    ref newEntry,
                    ref oldEntry,
                    ref oldValue,
                    newEntry.GetFlattenedEntry(RequestFlags.FullyResolved).Value,
                    null /*controlValue*/,
                    dp.DesignerCoerceValueCallback,
                    false /*coerceWithDeferredReference*/,
                    false /*coerceWithCurrentValue*/,
                    true /*skipBaseValueChecks*/);

                // Make sure that the call out did not cause a change to entryIndex
                entryIndex = CheckEntryIndex(entryIndex, targetIndex);
            }

            UpdateResult result = 0;

            if (newEntry.FullValueSource != (FullValueSource) BaseValueSourceInternal.Default)
            {
                Debug.Assert(newEntry.BaseValueSourceInternal != BaseValueSourceInternal.Unknown, "Value source should be known at this point");
                bool unsetValue = false;

                if (newEntry.BaseValueSourceInternal == BaseValueSourceInternal.Inherited)
                {
                    if (DependencyObject.IsTreeWalkOperation(operationType) &&
                        (newEntry.IsCoerced || newEntry.IsAnimated))
                    {
                        // an inherited value has been coerced or animated.  This
                        // should be treated as a new "set" of the property.
                        // The current tree walk should not continue into the subtree,
                        // but rather a new tree walk should start.

                        // this signals OnPropertyChanged to start a new tree walk
                        // and mark the current node as SelfInheritanceParent
                        operationType = OperationType.Unknown;

                        // this signals the caller not to continue the current
                        // tree walk into the subtree
                        result |= UpdateResult.InheritedValueOverridden;
                    }
                    else if (!IsSelfInheritanceParent)
                    {
                        // otherwise, just inherit the value from the InheritanceParent
                        unsetValue = true;
                    }
                }

                if (unsetValue)
                {
                    UnsetEffectiveValue(entryIndex, dp, metadata);
                }
                else
                {
                    SetEffectiveValue(entryIndex, dp, metadata, newEntry, oldEntry);
                }
            }
            else
            {
                UnsetEffectiveValue(entryIndex, dp, metadata);
            }

            // Change notifications are fired when the value actually changed or in
            // the case of the Freezable mutable factories when the value source changes.
            // Try AvaCop without the second condition to repro this problem.
            bool isAValueChange = !Equals(dp, oldValue, newEntry.GetFlattenedEntry(RequestFlags.FullyResolved).Value);

            if (isAValueChange)
            {
                result |= UpdateResult.ValueChanged;
            }

            if (isAValueChange ||
                (operationType == OperationType.ChangeMutableDefaultValue && oldEntry.BaseValueSourceInternal != newEntry.BaseValueSourceInternal) ||
                (metadata.IsInherited && oldEntry.BaseValueSourceInternal != newEntry.BaseValueSourceInternal && operationType != OperationType.AddChild && operationType != OperationType.RemoveChild && operationType != OperationType.Inherit))
            {
                result |= UpdateResult.NotificationSent;

                try
                {
                    // fire change notification
                    NotifyPropertyChange(
                            new DependencyPropertyChangedEventArgs(
                                    dp,
                                    metadata,
                                    isAValueChange,
                                    oldEntry,
                                    newEntry,
                                    operationType));
                }
                finally
                {
#if NESTED_OPERATIONS_CHECK
                    NestedOperations--;
#endif
                }
            }

#region EventTracing
#if VERBOSE_PROPERTY_EVENT
            if (isDynamicTracing)
            {
                if (EventTrace.IsEnabled(EventTrace.Flags.performance, EventTrace.Level.verbose))
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYINVALIDATIONGUID, MS.Utility.EventType.EndEvent);
                }
            }
#endif
#endregion EventTracing


            /*
            if( TraceDependencyProperty.IsEnabled )
            {
                TraceDependencyProperty.Trace(
                    TraceEventType.Verbose,
                    TraceDependencyProperty.UpdateEffectiveValueStop,
                    this, dp, dp.OwnerType,
                    newEntry.Value, newEntry.BaseValueSourceInternal );
            }
            */

            // There are two cases in which we need to adjust inheritance contexts:
            //
            //     1.  The value pointed to this DP has changed, in which case
            //         we need to move the context from the old value to the
            //         new value.
            //
            //     2.  The value has not changed, but the ValueSource for the
            //         property has.  (For example, we've gone from being a local
            //         value to the result of a binding expression that just
            //         happens to return the same DO instance.)  In which case
            //         we may need to add or remove contexts even though we
            //         did not raise change notifications.
            //
            // We don't want to provide an inheritance context if the entry is
            // animated, coerced, is an expression, is coming from a style or
            // template, etc.  To avoid this, we explicitly check that the
            // FullValueSource is Local.  By checking FullValueSource rather than
            // BaseValueSource we are implicitly filtering out any sources which
            // have modifiers.  (e.g., IsExpression, IsAnimated, etc.)

            bool oldEntryHadContext = oldEntry.FullValueSource == (FullValueSource) BaseValueSourceInternal.Local;
            bool newEntryNeedsContext = newEntry.FullValueSource == (FullValueSource) BaseValueSourceInternal.Local;

            // NOTE:  We use result rather than isAValueChange below so that we
            //        pick up mutable default promotion, etc.
            if (result != 0 || (oldEntryHadContext != newEntryNeedsContext))
            {
                if (oldEntryHadContext)
                {
                    // RemoveSelfAsInheritanceContext no-ops null, non-DO values, etc.
                    RemoveSelfAsInheritanceContext(oldEntry.LocalValue, dp);
                }

                // Become the context for the new value. This is happens after
                // invalidation so that FE has a chance to hookup the logical
                // tree first. This is done only if the current DependencyObject
                // wants to be in the InheritanceContext tree.
                if (newEntryNeedsContext)
                {
                    // ProvideSelfAsInheritanceContext no-ops null, non-DO values, etc.
                    ProvideSelfAsInheritanceContext(newEntry.LocalValue, dp);
                }

                // DANGER:  Callout might add/remove entries in the effective value table.
                //          Uncomment the following if you need to use entryIndex post
                //          context hookup.
                //
                // entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
            }

            return result;
        }

        private void ProcessCoerceValue(
            DependencyProperty dp,
            PropertyMetadata metadata,
            ref EntryIndex entryIndex,
            ref int targetIndex,
            ref EffectiveValueEntry newEntry,
            ref EffectiveValueEntry oldEntry,
            ref object oldValue,
            object baseValue,
            object controlValue,
            CoerceValueCallback coerceValueCallback,
            bool coerceWithDeferredReference,
            bool coerceWithCurrentValue,
            bool skipBaseValueChecks)
        {
            if (newEntry.IsDeferredReference)
            {
                Debug.Assert(!(newEntry.IsCoerced && !newEntry.IsCoercedWithCurrentValue) &&
                    !newEntry.IsAnimated, "Coerced or Animated value cannot be a deferred reference");

                // Allow values to stay deferred through coercion callbacks in
                // limited circumstances, when we know the listener is internal.
                // Since we never assign DeferredReference instances to
                // non-internal (non-friend assembly) classes, it's safe to skip
                // the dereference if the callback is to the DP owner (and not
                // a derived type).  This is consistent with passing raw
                // DeferredReference instances to ValidateValue callbacks, which
                // only ever go to the owner class.
                if (!coerceWithDeferredReference ||
                    dp.OwnerType != metadata.CoerceValueCallback.Method.DeclaringType) // Need 2nd check to rule out derived class callback overrides.
                {
                    // Resolve deferred references because we need the actual
                    // baseValue to evaluate the correct animated value. This is done
                    // by invoking GetValue for this property.
                    DeferredReference dr = (DeferredReference) baseValue;
                    baseValue = dr.GetValue(newEntry.BaseValueSourceInternal);

                    // Set the baseValue back into the entry
                    newEntry.SetCoersionBaseValue(baseValue);

                    entryIndex = CheckEntryIndex(entryIndex, targetIndex);
                }
            }

            object coercedValue = coerceWithCurrentValue ? controlValue : coerceValueCallback(this, baseValue);

            // Make sure that the call out did not cause a change to entryIndex
            entryIndex = CheckEntryIndex(entryIndex, targetIndex);

            // Even if we used the controlValue in the coerce callback, we still want to compare against the original baseValue
            // to determine if we need to set a coerced value.
            if (!Equals(dp, coercedValue, baseValue))
            {
                // returning DependencyProperty.UnsetValue from a Coercion callback means "don't do the set" ...
                // or "use previous value"
                if (coercedValue == DependencyProperty.UnsetValue)
                {
                    if (oldEntry.IsDeferredReference)
                    {
                        DeferredReference reference = (DeferredReference)oldValue;
                        oldValue = reference.GetValue(oldEntry.BaseValueSourceInternal);

                        entryIndex = CheckEntryIndex(entryIndex, targetIndex);
                    }

                    coercedValue = oldValue;
                }

                // Note that we do not support the value being coerced to a
                // DeferredReference
                if (!dp.IsValidValue(coercedValue))
                {
                    // well... unless it's the control's "current value"
                    if (!(coerceWithCurrentValue && coercedValue is DeferredReference))
                        throw new ArgumentException(SR.Get(SRID.InvalidPropertyValue, coercedValue, dp.Name));
                }

                // Set the coerced value here. All other values would
                // have been set during EvaluateEffectiveValue/GetValueCore.

                newEntry.SetCoercedValue(coercedValue, baseValue, skipBaseValueChecks, coerceWithCurrentValue);
            }
        }

        /// <summary>
        /// This is a helper method that is used to fire the property change notification through
        /// the callbacks and to all the dependents of this property such as bindings etc.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void NotifyPropertyChange(DependencyPropertyChangedEventArgs args)
        {
            // fire change notification
            OnPropertyChanged(args);

            if (args.IsAValueChange || args.IsASubPropertyChange)
            {
                // Invalidate all Dependents of this Source invalidation due
                // to Expression dependencies

                DependencyProperty dp = args.Property;
                object objectDependentsListMap = DependentListMapField.GetValue(this);
                if (objectDependentsListMap != null)
                {
                    FrugalMap dependentListMap = (FrugalMap)objectDependentsListMap;
                    object dependentList = dependentListMap[dp.GlobalIndex];
                    Debug.Assert(dependentList != null, "dependentList should either be unset or non-null");

                    if (dependentList != DependencyProperty.UnsetValue)
                    {
                        // The list can "go empty" if the items it references "went away"
                        if (((DependentList)dependentList).IsEmpty)
                            dependentListMap[dp.GlobalIndex] = DependencyProperty.UnsetValue;
                        else
                            ((DependentList)dependentList).InvalidateDependents(this, args);
                    }

                    // also notify "direct" dependents
                    dp = DirectDependencyProperty;
                    dependentList = dependentListMap[dp.GlobalIndex];
                    Debug.Assert(dependentList != null, "dependentList should either be unset or non-null");

                    if (dependentList != DependencyProperty.UnsetValue)
                    {
                        // The list can "go empty" if the items it references "went away"
                        if (((DependentList)dependentList).IsEmpty)
                            dependentListMap[dp.GlobalIndex] = DependencyProperty.UnsetValue;
                        else
                            ((DependentList)dependentList).InvalidateDependents(this, new DependencyPropertyChangedEventArgs(dp, (PropertyMetadata)null, null));
                    }
                }
            }
        }


        private EffectiveValueEntry EvaluateExpression(
            EntryIndex entryIndex,
            DependencyProperty dp,
            Expression expr,
            PropertyMetadata metadata,
            EffectiveValueEntry oldEntry,
            EffectiveValueEntry newEntry)
        {
            object value = expr.GetValue(this, dp);
            bool isDeferredReference = false;

            if (value != DependencyProperty.UnsetValue && value != Expression.NoValue)
            {
                isDeferredReference = (value is DeferredReference);
                if (!isDeferredReference && !dp.IsValidValue(value))
                {
#region EventTracing
#if VERBOSE_PROPERTY_EVENT
                    if (isDynamicTracing)
                    {
                        if (EventTrace.IsEnabled(EventTrace.Flags.performance, EventTrace.Level.verbose))
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYGUID,
                                                                MS.Utility.EventType.EndEvent,
                                                                EventTrace.PROPERTYVALIDATION, 0xFFF );
                        }
                    }
#endif
#endregion EventTracing
                    throw new InvalidOperationException(SR.Get(SRID.InvalidPropertyValue, value, dp.Name));
                }
            }
            else
            {
                if (value == Expression.NoValue)
                {
                    // The expression wants to "hide".  First set the
                    // expression value to NoValue to indicate "hiding".
                    newEntry.SetExpressionValue(Expression.NoValue, expr);

                    // Next, get the expression value some other way.
                    if (!dp.ReadOnly)
                    {
                        EvaluateBaseValueCore(dp, metadata, ref newEntry);
                        value = newEntry.GetFlattenedEntry(RequestFlags.FullyResolved).Value;
                    }
                    else
                    {
                        value = DependencyProperty.UnsetValue;
                    }
                }

                // if there is still no value, use the default
                if (value == DependencyProperty.UnsetValue)
                {
                    value = metadata.GetDefaultValue(this, dp);
                }
            }

            // Set the expr and its evaluated value into
            // the _effectiveValues cache
            newEntry.SetExpressionValue(value, expr);
            return newEntry;
        }

        private EffectiveValueEntry EvaluateEffectiveValue(
            EntryIndex entryIndex,
            DependencyProperty dp,
            PropertyMetadata metadata,
            EffectiveValueEntry oldEntry,
            EffectiveValueEntry newEntry, // this is only used to recognize if this is a clear local value
            OperationType operationType)
        {
#region EventTracing
#if VERBOSE_PROPERTY_EVENT
            bool isDynamicTracing = EventTrace.IsEnabled(EventTrace.Flags.performance, EventTrace.Level.verbose); // This was under "normal"
            if (isDynamicTracing)
            {
                ++ValidationCount;
                if( ValidationCount % 100 == 0 )
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYVALIDATIONGUID,
                                                         MS.Utility.EventType.Info,
                                                         ValidationCount );
                }

                string TypeAndName = String.Format(CultureInfo.InvariantCulture, "[{0}]{1}({2})",GetType().Name,dp.Name,base.GetHashCode());  // FxCop wanted the CultureInfo.InvariantCulture

                EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYVALIDATIONGUID,
                                                     MS.Utility.EventType.StartEvent,
                                                     base.GetHashCode(), TypeAndName ); // base.GetHashCode() to avoid calling a virtual, which FxCop doesn't like.
            }
#endif
#endregion EventTracing

#if NESTED_OPERATIONS_CHECK
            // Are we validating out of control?
            if( NestedOperations > NestedOperationMaximum )
            {
                // We're validating out of control, time to abort.
                throw new InvalidOperationException("Too many levels of nested DependencyProperty GetValue calls.  This usually indicates a circular reference in the application and the cycle needs to be broken.");
            }
            NestedOperations++; // Decrement in the finally block
#endif

            object value = DependencyProperty.UnsetValue;

            try
            {
                // Read local storage
                bool isSetValue = (newEntry.BaseValueSourceInternal == BaseValueSourceInternal.Local);
                bool isClearLocalValue = isSetValue && (newEntry.Value == DependencyProperty.UnsetValue);
                bool oldLocalIsExpression = false;
                bool preserveCurrentValue;

                // honor request for "preserve current value" behaviour - see InvalidateProperty.
                if (newEntry.BaseValueSourceInternal == BaseValueSourceInternal.Unknown &&
                    newEntry.IsCoercedWithCurrentValue)
                {
                    preserveCurrentValue = true;
                    newEntry.IsCoercedWithCurrentValue = false;     // clear flag only used for private communication
                }
                else
                {
                    preserveCurrentValue = false;
                }

                if (isClearLocalValue)
                {
                    newEntry.BaseValueSourceInternal = BaseValueSourceInternal.Unknown;
                }
                else
                {
                    // if we reached this on a re-evaluate of a setvalue, we need to make sure
                    // we don't lose track of the newly specified local value.
                    // for all other cases, the oldEntry will have the local value we should
                    // use.
                    value = isSetValue ? newEntry.LocalValue : oldEntry.LocalValue;

                    if (value == ExpressionInAlternativeStore)
                    {
                        value = DependencyProperty.UnsetValue;
                    }
                    else
                    {
                        oldLocalIsExpression = isSetValue ? newEntry.IsExpression : oldEntry.IsExpression;
                    }
                }

                // (If local storage not Unset and not an Expression, return)
                if (value != DependencyProperty.UnsetValue)
                {
                    newEntry = new EffectiveValueEntry(dp, BaseValueSourceInternal.Local);
                    newEntry.Value = value;

                    // Check if an Expression is set
                    if (oldLocalIsExpression)
                    {
                        // CALLBACK
                        newEntry = EvaluateExpression(
                            entryIndex,
                            dp,
                            (Expression) value,
                            metadata,
                            oldEntry,
                            newEntry);

                        entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);

                        value = newEntry.ModifiedValue.ExpressionValue;
                    }
                }

                // Subclasses are not allowed to resolve/modify the value for read-only properties.
                if( !dp.ReadOnly )
                {
                    // Give subclasses a chance to resolve/modify the value
                    EvaluateBaseValueCore(dp, metadata, ref newEntry);

                    // we need to have the default value in the entry before we do the animation check
                    if (newEntry.BaseValueSourceInternal == BaseValueSourceInternal.Unknown)
                    {
                        newEntry = EffectiveValueEntry.CreateDefaultValueEntry(dp, metadata.GetDefaultValue(this, dp));
                    }

                    value = newEntry.GetFlattenedEntry(RequestFlags.FullyResolved).Value;

                    // preserve a current value across invalidations that don't change
                    // the base value
                    if (preserveCurrentValue &&
                        oldEntry.IsCoercedWithCurrentValue &&
                        oldEntry.BaseValueSourceInternal == newEntry.BaseValueSourceInternal &&
                        Equals(dp, oldEntry.ModifiedValue.BaseValue, value))
                    {
                        object currentValue = oldEntry.ModifiedValue.CoercedValue;
                        newEntry.SetCoercedValue(currentValue, value, skipBaseValueChecks:true, coerceWithCurrentValue:true);
                        value = currentValue;
                    }

                    entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);

                    if (oldEntry.IsAnimated)
                    {
                        newEntry.ResetCoercedValue();
                        EvaluateAnimatedValueCore(dp, metadata, ref newEntry);
                        value = newEntry.GetFlattenedEntry(RequestFlags.FullyResolved).Value;
                    }
                }
            }
            finally
            {
#if NESTED_OPERATIONS_CHECK
                NestedOperations--;
#endif
            }

#region EventTracing
#if VERBOSE_PROPERTY_EVENT
            if (isDynamicTracing)
            {
                if (EventTrace.IsEnabled(EventTrace.Flags.performance, EventTrace.Level.verbose))
                {
                    int UsingDefault = 1;
                    if (value != DependencyProperty.UnsetValue)
                        UsingDefault = 0;
                    EventTrace.EventProvider.TraceEvent(EventTrace.PROPERTYVALIDATIONGUID,
                                                         MS.Utility.EventType.EndEvent,
                                                         UsingDefault);
                }
            }
#endif
#endregion EventTracing

            if (value == DependencyProperty.UnsetValue)
            {
                newEntry = EffectiveValueEntry.CreateDefaultValueEntry(dp, metadata.GetDefaultValue(this, dp));
            }

            return newEntry;
        }

        /// <summary>
        ///     Allows subclasses to participate in property base value computation
        /// </summary>
        internal virtual void EvaluateBaseValueCore(
                DependencyProperty  dp,
                PropertyMetadata    metadata,
            ref EffectiveValueEntry newEntry)
        {
        }

        /// <summary>
        ///     Allows subclasses to participate in property animated value computation
        /// </summary>
        internal virtual void EvaluateAnimatedValueCore(
                DependencyProperty  dp,
                PropertyMetadata    metadata,
            ref EffectiveValueEntry newEntry)
        {
        }

        /// <summary>
        ///     Notification that a specified property has been changed
        /// </summary>
        /// <param name="e">EventArgs that contains the property, metadata, old value, and new value for this change</param>
        protected virtual void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // Do not call VerifyAccess because this is a virtual, and is used as a call-out.

            if( e.Property == null )
            {
                throw new ArgumentException(SR.Get(SRID.ReferenceIsNull, "e.Property"), "e");
            }

            if (e.IsAValueChange || e.IsASubPropertyChange || e.OperationType == OperationType.ChangeMutableDefaultValue)
            {
                // Inform per-type/property invalidation listener, if exists
                PropertyMetadata metadata = e.Metadata;
                if ((metadata != null) && (metadata.PropertyChangedCallback != null))
                {
                    metadata.PropertyChangedCallback(this, e);
                }
            }
        }

        /// <summary>
        /// Override this method to control whether a DependencyProperty should be serialized.
        /// The base implementation returns true if the property is set (locally) on this object.
        /// </summary>
        protected internal virtual bool ShouldSerializeProperty( DependencyProperty dp )
        {
            return ContainsValue( dp );
        }

        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal BaseValueSourceInternal GetValueSource(DependencyProperty dp, PropertyMetadata metadata, out bool hasModifiers)
        {
            bool isExpression, isAnimated, isCoerced, isCurrent;
            return GetValueSource(dp, metadata, out hasModifiers, out isExpression, out isAnimated, out isCoerced, out isCurrent);
        }

        [FriendAccessAllowed] // Built into Base, also used by Core & Framework.
        internal BaseValueSourceInternal GetValueSource(DependencyProperty dp, PropertyMetadata metadata,
                out bool hasModifiers, out bool isExpression, out bool isAnimated, out bool isCoerced, out bool isCurrent)
        {
            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

            if (entryIndex.Found)
            {
                EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];
                hasModifiers = entry.HasModifiers;
                isExpression = entry.IsExpression;
                isAnimated = entry.IsAnimated;
                isCoerced = entry.IsCoerced;
                isCurrent = entry.IsCoercedWithCurrentValue;
                return entry.BaseValueSourceInternal;
            }
            else
            {
                isExpression = false;
                isAnimated = false;
                isCoerced = false;
                isCurrent = false;

                if (dp.ReadOnly)
                {
                    if (metadata == null)
                    {
                        metadata = dp.GetMetadata(DependencyObjectType);
                    }

                    GetReadOnlyValueCallback callback = metadata.GetReadOnlyValueCallback;
                    if (callback != null)
                    {
                        BaseValueSourceInternal source;
                        callback(this, out source);
                        hasModifiers = false;
                        return source;
                    }
                }

                if (dp.IsPotentiallyInherited)
                {
                    if (metadata == null)
                    {
                        metadata = dp.GetMetadata(DependencyObjectType);
                    }

                    if (metadata.IsInherited)
                    {
                        DependencyObject inheritanceParent = InheritanceParent;
                        if (inheritanceParent != null && inheritanceParent.LookupEntry(dp.GlobalIndex).Found)
                        {
                            hasModifiers = false;
                            return BaseValueSourceInternal.Inherited;
                        }
                    }
                }
            }

            hasModifiers = false;
            return BaseValueSourceInternal.Default;
        }

        /// <summary>
        ///     Retrieve the local value of a property (if set)
        /// </summary>
        /// <param name="dp">Dependency property</param>
        /// <returns>
        ///     The local value. DependencyProperty.UnsetValue if no local value was
        ///     set via <cref see="SetValue"/>.
        /// </returns>
        public object ReadLocalValue(DependencyProperty dp)
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

            // Call Forwarded
            return ReadLocalValueEntry(entryIndex, dp, false /* allowDeferredReferences */);
        }

        /// <summary>
        ///     Retrieve the local value of a property (if set)
        /// </summary>
        /// <returns>
        ///     The local value. DependencyProperty.UnsetValue if no local value was
        ///     set via <cref see="SetValue"/>.
        /// </returns>
        internal object ReadLocalValueEntry(EntryIndex entryIndex, DependencyProperty dp, bool allowDeferredReferences)
        {
            if (!entryIndex.Found)
            {
                return DependencyProperty.UnsetValue;
            }

            EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];
            object value = entry.IsCoercedWithCurrentValue ? entry.ModifiedValue.CoercedValue : entry.LocalValue;

            // convert a deferred reference into a real value
            if (!allowDeferredReferences && entry.IsDeferredReference)
            {
                // localValue may still not be a DeferredReference, e.g.
                // if it is an expression whose value is a DeferredReference.
                // So a little more work is needed before converting the value.
                DeferredReference dr = value as DeferredReference;
                if (dr != null)
                {
                    value = dr.GetValue(entry.BaseValueSourceInternal);
                }
            }

            // treat Expression marker as "unset"
            if (value == ExpressionInAlternativeStore)
            {
                value = DependencyProperty.UnsetValue;
            }

            return value;
        }

        /// <summary>
        ///     Create a local value enumerator for this instance
        /// </summary>
        /// <returns>Local value enumerator (stack based)</returns>
        public LocalValueEnumerator GetLocalValueEnumerator()
        {
            // Do not allow foreign threads access.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            this.VerifyAccess();

            uint effectiveValuesCount = EffectiveValuesCount;
            LocalValueEntry[] snapshot = new LocalValueEntry[effectiveValuesCount];
            int count = 0;

            // Iterate through the sorted effectiveValues
            for (uint i=0; i<effectiveValuesCount; i++)
            {
                DependencyProperty dp = DependencyProperty.RegisteredPropertyList.List[_effectiveValues[i].PropertyIndex];
                if (dp != null)
                {
                    object localValue = ReadLocalValueEntry(new EntryIndex(i), dp, false /* allowDeferredReferences */);
                    if (localValue != DependencyProperty.UnsetValue)
                    {
                        snapshot[count++] = new LocalValueEntry(dp, localValue);
                    }
                }
            }

            return new LocalValueEnumerator(snapshot, count);
        }

        /// <summary>
        ///     This is how we track if someone is enumerating the _effectiveValues
        ///     cache. This flag should be set to false before doing that.
        /// </summary>
        private bool CanModifyEffectiveValues
        {
            get { return (_packedData & 0x00080000) != 0; }

            set
            {
                Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

                if (value)
                {
                    _packedData |= 0x00080000;
                }
                else
                {
                    _packedData &= 0xFFF7FFFF;
                }
            }
        }

        [FriendAccessAllowed]   // defined in Base, used in Core and Framework
        internal bool IsInheritanceContextSealed
        {
            get { return (_packedData & 0x01000000) != 0; }
            set
            {
                if (value)
                {
                    _packedData |= 0x01000000;
                }
                else
                {
                    _packedData &= 0xFEFFFFFF;
                }
            }
        }

        private bool DO_Sealed
        {
            get { return (_packedData & 0x00400000) != 0; }
            set { if (value) { _packedData |= 0x00400000; } else { _packedData &= 0xFFBFFFFF; } }
        }

        // Freezable State stored here for size optimization:
        // Freezable is immutable
        internal bool Freezable_Frozen
        {
            // uses the same bit as Sealed ... even though they are not quite synonymous
            // Since Frozen implies Sealed, and calling Seal() is disallowed on Freezable,
            // this is ok.
            get { return DO_Sealed; }
            set { DO_Sealed = value; }
        }

        // Freezable State stored here for size optimization:
        // Freezable is being referenced in multiple places and hence cannot have a single InheritanceContext
        internal bool Freezable_HasMultipleInheritanceContexts
        {
            get { return (_packedData & 0x02000000) != 0; }
            set { if (value) { _packedData |= 0x02000000; } else { _packedData &= 0xFDFFFFFF; } }
        }

        // Freezable State stored here for size optimization:
        // Handlers stored in a dictionary
        internal bool Freezable_UsingHandlerList
        {
            get { return (_packedData & 0x04000000) != 0; }
            set { if (value) { _packedData |= 0x04000000; } else { _packedData &= 0xFBFFFFFF; } }
        }

        // Freezable State stored here for size optimization:
        // Context stored in a dictionary
        internal bool Freezable_UsingContextList
        {
            get { return (_packedData & 0x08000000) != 0; }
            set { if (value) { _packedData |= 0x08000000; } else { _packedData &= 0xF7FFFFFF; } }
        }

        // Freezable State stored here for size optimization:
        // Freezable has a single handler
        internal bool Freezable_UsingSingletonHandler
        {
            get { return (_packedData & 0x10000000) != 0; }
            set { if (value) { _packedData |= 0x10000000; } else { _packedData &= 0xEFFFFFFF; } }
        }

        // Freezable State stored here for size optimization:
        // Freezable has a single context
        internal bool Freezable_UsingSingletonContext
        {
            get { return (_packedData & 0x20000000) != 0; }
            set { if (value) { _packedData |= 0x20000000; } else { _packedData &= 0xDFFFFFFF; } }
        }


        // Animatable State stored here for size optimization:
        //
        internal bool Animatable_IsResourceInvalidationNecessary
        {
            [FriendAccessAllowed] // Built into Base, but used by Core.
            get { return (_packedData & 0x40000000) != 0; }
            [FriendAccessAllowed] // Built into Base, but used by Core.
            set { if (value) { _packedData |= 0x40000000; } else { _packedData &= 0xBFFFFFFF; } }
        }

        // IAnimatable State stored here for size optimization:
        // Returns true if this IAnimatable implemention has animations on its properties
        // but doesn't check the sub-properties for animations.
        internal bool IAnimatable_HasAnimatedProperties
        {
            [FriendAccessAllowed] // Built into Base, but used by Core.
            get { return (_packedData & 0x80000000) != 0; }
            [FriendAccessAllowed] // Built into Base, but used by Core.
            set { if (value) { _packedData |= 0x80000000; } else { _packedData &= 0x7FFFFFFF; } }
        }

        // internal DP used for direct dependencies (should never appear in an effective value table)
        //
        // A direct dependency can arise from WPF data binding in a situation like this:
        //      <Border Background="{Binding Path=Brush}"/>
        // when the Brush property on the source object is not a DP, but just a regular CLR property.
        // If a property on the brush changes, the border should be notified so that
        // it can repaint its background. The brush is notified of the change, and
        // propagtes the notification (as a SubPropertyChange) to all its customers that
        // use the brush via a DP, but this isn't enough for the current scenario.
        // To overcome this, the binding registers itself as a "direct" dependent of the brush
        // (using the following DP as the key).  The property engine will forward
        // notifications to direct dependents, the binding will hear about the change,
        // and will forward a sub-property change to the Border.
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        static internal readonly DependencyProperty DirectDependencyProperty =
            DependencyProperty.Register("__Direct", typeof(object), typeof(DependencyProperty));

        internal static void UpdateSourceDependentLists(DependencyObject d, DependencyProperty dp, DependencySource[] sources, Expression expr, bool add)
        {
            // Sources already validated to be on the same thread as Dependent (d)

            if (sources != null)
            {
                // don't hold a reference on the dependent if the expression is doing
                // the invalidations.  This helps avoid memory leaks
                if (expr.ForwardsInvalidations)
                {
                    d = null;
                    dp = null;
                }

                for (int i = 0; i < sources.Length; i++)
                {
                    DependencySource source = sources[i];

                    // A Sealed DependencyObject does not have a Dependents list
                    // so don't bother updating it (or attempt to add one).

                    Debug.Assert((!source.DependencyObject.IsSealed) ||
                            (DependentListMapField.GetValue(source.DependencyObject) == default(object)));

                    if (!source.DependencyObject.IsSealed)
                    {
                        // Retrieve the DependentListMap for this source
                        // The list of dependents to invalidate is stored using a special negative key

                        FrugalMap dependentListMap;
                        object value = DependentListMapField.GetValue(source.DependencyObject);
                        if (value != null)
                        {
                            dependentListMap = (FrugalMap)value;
                        }
                        else
                        {
                            dependentListMap = new FrugalMap();
                        }

                        // Get list of DependentList off of ID map of Source
                        object dependentListObj = dependentListMap[source.DependencyProperty.GlobalIndex];
                        Debug.Assert(dependentListObj != null, "dependentList should either be unset or non-null");

                        // Add/Remove new Dependent (this) to Source's list
                        if (add)
                        {
                            DependentList dependentList;
                            if (dependentListObj == DependencyProperty.UnsetValue)
                            {
                                dependentListMap[source.DependencyProperty.GlobalIndex] = dependentList = new DependentList();
                            }
                            else
                            {
                                dependentList = (DependentList)dependentListObj;
                            }

                            dependentList.Add(d, dp, expr);
                        }
                        else
                        {
                            if (dependentListObj != DependencyProperty.UnsetValue)
                            {
                                DependentList dependentList = (DependentList)dependentListObj;

                                dependentList.Remove(d, dp, expr);

                                if (dependentList.IsEmpty)
                                {
                                    // No more dependencies for this property; reclaim the space if we can.
                                    dependentListMap[source.DependencyProperty.GlobalIndex] = DependencyProperty.UnsetValue;
                                }
                            }
                        }

                        // Set the updated struct back into the source's _localStore.
                        DependentListMapField.SetValue(source.DependencyObject, dependentListMap);
                    }
                }
            }
        }

        internal static void ValidateSources(DependencyObject d, DependencySource[] newSources, Expression expr)
        {
            // Make sure all Sources are owned by the same thread.
            if (newSources != null)
            {
                Dispatcher dispatcher = d.Dispatcher;
                for (int i = 0; i < newSources.Length; i++)
                {
                    Dispatcher sourceDispatcher = newSources[i].DependencyObject.Dispatcher;
                    if (sourceDispatcher != dispatcher && !(expr.SupportsUnboundSources && sourceDispatcher == null))
                    {
                        throw new ArgumentException(SR.Get(SRID.SourcesMustBeInSameThread));
                    }
                }
            }
        }

        /// <summary>
        /// Register the two callbacks that are used to implement the "alternative
        /// Expression storage" feature, and return the two methods used to access
        /// the feature.
        /// </summary>
        /// <remarks>
        /// This method should only be called (once) from the Framework.  It should
        /// not be called directly by users.
        /// </remarks>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal static void RegisterForAlternativeExpressionStorage(
                            AlternativeExpressionStorageCallback getExpressionCore,
                            out AlternativeExpressionStorageCallback getExpression)
        {
            Debug.Assert(getExpressionCore != null, "getExpressionCore cannot be null");
            Debug.Assert(_getExpressionCore == null, "The 'alternative Expression storage' feature has already been registered");

            _getExpressionCore = getExpressionCore;

            getExpression = new AlternativeExpressionStorageCallback(GetExpression);
        }

        /// <summary>
        /// Used to determine whether a DependencyObject has a value with an expression, such as a resource reference.
        /// </summary>
        /// <returns>
        /// True if Dependency object has a value with an expression
        /// </returns>
        internal bool HasAnyExpression()
        {
            EffectiveValueEntry[] effectiveValues = EffectiveValues;
            uint numEffectiveValues = EffectiveValuesCount;
            bool result = false;

            for (uint i = 0; i < numEffectiveValues; i++)
            {
                DependencyProperty dp =
                    DependencyProperty.RegisteredPropertyList.List[effectiveValues[i].PropertyIndex];

                if (dp != null)
                {
                    EntryIndex entryIndex = new EntryIndex(i);
                    // The expression check only needs to be done when isChecking is true
                    // because if we return false here the Freeze() call will fail.
                    if (HasExpression(entryIndex, dp))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Return true iff the property has an expression applied to it.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal bool HasExpression(EntryIndex entryIndex, DependencyProperty dp)
        {
            if (!entryIndex.Found)
            {
                return false;
            }

            EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];

            object o = entry.LocalValue;

            bool result = (entry.HasExpressionMarker || o is Expression);
            return result;
        }

        /// <summary>
        /// Return the Expression (if any) currently in effect for the given property.
        /// </summary>
        private static Expression GetExpression(DependencyObject d, DependencyProperty dp, PropertyMetadata metadata)
        {
            EntryIndex entryIndex = d.LookupEntry(dp.GlobalIndex);

            if (!entryIndex.Found)
            {
                return null;
            }

            EffectiveValueEntry entry = d._effectiveValues[entryIndex.Index];

            if (entry.HasExpressionMarker)
            {
                if (_getExpressionCore != null)
                {
                    return _getExpressionCore(d, dp, metadata);
                }

                return null;
            }

            // no expression marker -- check local value itself
            if (entry.IsExpression)
            {
                return (Expression) entry.LocalValue;
            }

            return null;
        }

        #region InheritanceContext

        /// <summary>
        ///     InheritanceContext
        /// </summary>
        internal virtual DependencyObject InheritanceContext
        {
            [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
            get { return null; }
        }

        /// <summary>
        ///     You have a new InheritanceContext
        /// </summary>
        /// <remarks>
        ///     This method is equivalent to OnNewParent of
        ///     the yesteryears on an element. Note that the
        ///     implementation may choose to ignore this new
        ///     context, e.g. in the case of a Freezable that
        ///     is being shared.
        ///     <p/>
        ///     Do not call this method directly.  Instead call
        ///     ProvideSelfAsInheritanceContext, which checks various
        ///     preconditions and then calls AddInheritanceContext for you.
        /// </remarks>
        internal virtual void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
        }

        /// <summary>
        ///     You have lost an InheritanceContext
        /// </summary>
        /// <remarks>
        ///     <p/>
        ///     Do not call this method directly.  Instead call
        ///     RemoveSelfAsInheritanceContext, which checks various
        ///     preconditions and then calls RemoveInheritanceContext for you.
        /// </remarks>
        internal virtual void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
        }

        /// <summary>
        ///     You are about to provided as the InheritanceContext for the target.
        ///     You can choose to allow this or not.
        /// </summary>
        internal virtual bool ShouldProvideInheritanceContext(DependencyObject target, DependencyProperty property)
        {
            return true;
        }

        /// <summary>
        ///     The InheritanceContext for an ancestor
        ///     has changed
        /// </summary>
        /// <remarks>
        ///     This is the equivalent of OnAncestorChanged
        ///     for an element
        /// </remarks>
        [FriendAccessAllowed] // Built into Base, also used by Core.
        internal void OnInheritanceContextChanged(EventArgs args)
        {
            // Fire the event that BindingExpression and
            // ResourceReferenceExpression will be listening to.
            EventHandler handlers = InheritanceContextChangedHandlersField.GetValue(this);
            if (handlers != null)
            {
                handlers(this, args);
            }

            CanModifyEffectiveValues = false;
            try
            {
                // Notify all those DO that the current instance is a
                // context for (we will call these inheritanceChildren) about the
                // change in the context. This is like a recursive tree walk.
                // Iterate through the sorted effectiveValues
                uint effectiveValuesCount = EffectiveValuesCount;
                for (uint i=0; i<effectiveValuesCount; i++)
                {
                    DependencyProperty dp = DependencyProperty.RegisteredPropertyList.List[_effectiveValues[i].PropertyIndex];
                    if (dp != null)
                    {
                        object localValue = ReadLocalValueEntry(new EntryIndex(i), dp, true /* allowDeferredReferences */);
                        if (localValue != DependencyProperty.UnsetValue)
                        {
                            DependencyObject inheritanceChild = localValue as DependencyObject;
                            if (inheritanceChild!= null && inheritanceChild.InheritanceContext == this)
                            {
                                inheritanceChild.OnInheritanceContextChanged(args);
                            }
                        }
                    }
                }
            }
            finally
            {
                Debug.Assert(CanModifyEffectiveValues == false, "We do not expect re-entrancy here.");
                CanModifyEffectiveValues = true;
            }

            // Let sub-classes do their own thing
            OnInheritanceContextChangedCore(args);
        }

        /// <summary>
        ///     This is a means for subclasses to get notification
        ///     of InheritanceContext changes and then they can do
        ///     their own thing.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Core.
        internal virtual void OnInheritanceContextChangedCore(EventArgs args)
        {
        }

        /// <summary>
        ///     Event for InheritanceContextChanged. This is
        ///     the event that BindingExpression and
        ///     ResourceReferenceExpressions will be listening to.
        /// </summary>
        /// <remarks>
        ///     make this pay-for-play by storing handlers
        ///     in an uncommon field
        /// </remarks>
        internal event EventHandler InheritanceContextChanged
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            add
            {
                // Get existing event hanlders
                EventHandler handlers = InheritanceContextChangedHandlersField.GetValue(this);
                if (handlers != null)
                {
                    // combine to a multicast delegate
                    handlers = (EventHandler)Delegate.Combine(handlers, value);
                }
                else
                {
                    handlers = value;
                }
                // Set the delegate as an uncommon field
                InheritanceContextChangedHandlersField.SetValue(this, handlers);
            }

            [FriendAccessAllowed] // Built into Base, also used by Framework.
            remove
            {
                // Get existing event hanlders
                EventHandler handlers = InheritanceContextChangedHandlersField.GetValue(this);
                if (handlers != null)
                {
                    // Remove the given handler
                    handlers = (EventHandler)Delegate.Remove(handlers, value);
                    if (handlers == null)
                    {
                        // Clear the value for the uncommon field
                        // cause there are no more handlers
                        InheritanceContextChangedHandlersField.ClearValue(this);
                    }
                    else
                    {
                        // Set the remaining handlers as an uncommon field
                        InheritanceContextChangedHandlersField.SetValue(this, handlers);
                    }
                }
            }
        }

        /// <summary>
        ///     By default this is false since it doesn't have a context
        /// </summary>
        internal virtual bool HasMultipleInheritanceContexts
        {
            get { return false; }
        }

        /// <summary>
        ///     By default this is true since every DependencyObject can be an InheritanceContext
        /// </summary>
        internal bool CanBeInheritanceContext
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            get { return (_packedData & 0x00200000) != 0; }

            [FriendAccessAllowed] // Built into Base, also used by Framework.
            set
            {
                if (value)
                {
                    _packedData |= 0x00200000;
                }
                else
                {
                    _packedData &= 0xFFDFFFFF;
                }
            }
        }

        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal static bool IsTreeWalkOperation(OperationType operation)
        {
            return   operation == OperationType.AddChild ||
                     operation == OperationType.RemoveChild ||
                     operation == OperationType.Inherit;
        }

        /// <summary>
        /// Debug-only method that asserts that the current DO does not have any
        /// listeners on its InheritanceContextChanged event. This is used by
        /// Freezable (frozen Freezables can't have listeners).
        /// </summary>
        [Conditional ("DEBUG")]
        internal void Debug_AssertNoInheritanceContextListeners()
        {
            Debug.Assert(InheritanceContextChangedHandlersField.GetValue(this) == null,
                "This object should not have any listeners to its InheritanceContextChanged event");
        }

        // This uncommon field is used to store the handlers for the InheritanceContextChanged event
        private  static readonly UncommonField<EventHandler> InheritanceContextChangedHandlersField = new UncommonField<EventHandler>();

        #endregion InheritanceContext

        #region EffectiveValues

        // The rest of DependencyObject is its EffectiveValues cache

        // The cache of effective (aka "computed" aka "resolved") property
        // values for this DO.  If a DP does not have an entry in this array
        // it means one of two things:
        //  1) if it's an inheritable property, then its value may come from
        //     this DO's InheritanceParent
        //  2) if it's not an inheritable property (or this DO's InheritanceParent
        //     doesn't have an entry for this DP either), then the value for
        //     that DP on this DO is the default value.
        // Otherwise, the DP will have an entry in this array describing the
        // current value of the DP, where this value came from, and how it
        // has been modified
        internal EffectiveValueEntry[] EffectiveValues
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            get { return _effectiveValues; }
        }

        // The total number of entries in the above EffectiveValues cache
        internal uint EffectiveValuesCount
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            get { return _packedData & 0x000003FF; }
            private set { _packedData = (_packedData & 0xFFFFFC00) | (value & 0x000003FF); }
        }

        // The number of entries in the above EffectiveValues cache that
        // correspond to DPs that are inheritable on this DO; this count
        // helps us during "tree change" invalidations to know how big
        // of a "working change list" we have to construct.
        internal uint InheritableEffectiveValuesCount
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            get { return (_packedData >> 10) & 0x1FF; }
            set
            {
                Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");
                _packedData = ((value & 0x1FF) << 10) | (_packedData & 0xFFF803FF);
            }
        }

        // This flag indicates whether or not we are in "Property Initialization
        // Mode".  This is an opt-in mode: a DO starts out *not* in Property
        // Initialization Mode.  In this mode, the EffectiveValues cache grows
        // at a more liberal (2.0) rate.  Normally, outside of this mode, the
        // cache grows at a much stingier (1.2) rate.
        // Internal customers (currently only UIElement) access this mode
        // through the BeginPropertyInitialization/EndPropertyInitialization
        // methods below
        private bool IsInPropertyInitialization
        {
            get { return (_packedData & 0x00800000) != 0; }
            set
            {
                if (value)
                {
                    _packedData |= 0x00800000;
                }
                else
                {
                    _packedData &= 0xFF7FFFFF;
                }
            }
        }

        // A DependencyObject calls this method to indicate to the property
        // system that a bunch of property sets are about to happen; the
        // property system responds by elevating the growth rate of the
        // EffectiveValues cache, to speed up initialization by requiring
        // fewer reallocations
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void BeginPropertyInitialization()
        {
            IsInPropertyInitialization = true;
        }

        // A DependencyObject calls this method to indicate to the property
        // system that it is now done with the bunch of property sets that
        // accompanied the initialization of this element; the property
        // system responds by returning the growth rate of the
        // EffectiveValues cache to its normal rate, and then trimming
        // the cache to get rid of any excess bloat incurred by the
        // aggressive growth rate during initialization mode.
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void EndPropertyInitialization()
        {
            IsInPropertyInitialization = false;

            if (_effectiveValues != null)
            {
                uint effectiveValuesCount = EffectiveValuesCount;
                if (effectiveValuesCount != 0)
                {
                    uint endLength = effectiveValuesCount;
                    if (((float) endLength / (float) _effectiveValues.Length) < 0.8)
                    {
                        // For thread-safety, sealed DOs can't modify _effectiveValues.
                        Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

                        EffectiveValueEntry[] destEntries = new EffectiveValueEntry[endLength];
                        Array.Copy(_effectiveValues, 0, destEntries, 0, effectiveValuesCount);
                        _effectiveValues = destEntries;
                    }
                }
            }
        }


        internal DependencyObject InheritanceParent
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            get
            {
                if ((_packedData & 0x3E100000) == 0)
                {
                    return (DependencyObject) _contextStorage;
                }

                // return null if this DO has any of the following set:
                //    IsSelfInheritanceParent
                //    Freezable_HasMultipleInheritanceContexts
                //    Freezable_UsingHandlerList
                //    Freezable_UsingContextList
                //    Freezable_UsingSingletonHandler
                //    Freezable_UsingSingletonContext
                return null;
            }
        }

        private void SetInheritanceParent(DependencyObject newParent)
        {
            Debug.Assert((_packedData & 0x3E000000) == 0, "InheritanceParent should not be set in a Freezable, which manages its own inheritance context.");

            // For thread-safety, sealed DOs can't modify _contextStorage
            Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

            if (_contextStorage != null)
            {
                Debug.Assert(!IsSelfInheritanceParent, "If the IsSelfInheritanceParent is set then the InheritanceParent should have been removed.");

                _contextStorage = newParent;
            }
            else
            {
                if (newParent != null)
                {
                    // Merge all the inheritable properties on the inheritanceParent into the EffectiveValues
                    // store on the current node because someone had set an effective value for an
                    // inheritable property on this node.
                    if (IsSelfInheritanceParent)
                    {
                        MergeInheritableProperties(newParent);
                    }
                    else
                    {
                        _contextStorage = newParent;
                    }
                }
                else
                {
                    // Do nothing because before and after values are both null
                }
            }
        }



        internal bool IsSelfInheritanceParent
        {
            [FriendAccessAllowed] // Built into Base, also used by Framework.
            get { return (_packedData & 0x00100000) != 0; }
        }

        // Currently we only have support for turning this flag on. Once set this flag never goes false after that.
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void SetIsSelfInheritanceParent()
        {
            // Merge all the inheritable properties on the inheritanceParent into the EffectiveValues
            // store on the current node because someone tried to set an effective value for an
            // inheritable property on this node.
            DependencyObject inheritanceParent = InheritanceParent;
            if (inheritanceParent != null)
            {
                MergeInheritableProperties(inheritanceParent);

                // Get rid of the InheritanceParent since we won't need it anymore for
                // having cached all the inheritable properties on self
                SetInheritanceParent(null);
            }

            Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

            _packedData |= 0x00100000;
        }

        //
        //  This method
        //  1. Recalculates the InheritanceParent with respect to the given FrameworkParent
        //  2. Is called from [FE/FCE].OnAncestorChangedInternal
        //
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal void SynchronizeInheritanceParent(DependencyObject parent)
        {
            // If this flag is true it indicates that all the inheritable properties for this node
            // are cached on itself and hence we will not need the InheritanceParent pointer at all.
            if (!this.IsSelfInheritanceParent)
            {
                if (parent != null)
                {
                    if (!parent.IsSelfInheritanceParent)
                    {
                        SetInheritanceParent(parent.InheritanceParent);
                    }
                    else
                    {
                        SetInheritanceParent(parent);
                    }
                }
                else
                {
                    SetInheritanceParent(null);
                }
            }
        }

        //
        //  This method
        //  1. Merges the inheritable properties from the parent into the EffectiveValues store on self
        //
        private void MergeInheritableProperties(DependencyObject inheritanceParent)
        {
            Debug.Assert(inheritanceParent != null, "Must have inheritanceParent");
            Debug.Assert(inheritanceParent.IsSelfInheritanceParent, "An inheritanceParent should always be one that has all the inheritable properties cached on self");

            EffectiveValueEntry[] parentEffectiveValues = inheritanceParent.EffectiveValues;
            uint parentEffectiveValuesCount = inheritanceParent.EffectiveValuesCount;

            for (uint i=0; i<parentEffectiveValuesCount; i++)
            {
                EffectiveValueEntry entry = parentEffectiveValues[i];
                DependencyProperty dp = DependencyProperty.RegisteredPropertyList.List[entry.PropertyIndex];

                // There are UncommonFields also stored in the EffectiveValues cache. We need to exclude those.
                if (dp != null)
                {
                    PropertyMetadata metadata = dp.GetMetadata(DependencyObjectType);
                    if (metadata.IsInherited)
                    {
                        object value = inheritanceParent.GetValueEntry(
                                            new EntryIndex(i),
                                            dp,
                                            metadata,
                                            RequestFlags.SkipDefault | RequestFlags.DeferredReferences).Value;
                        if (value != DependencyProperty.UnsetValue)
                        {
                            EntryIndex entryIndex = LookupEntry(dp.GlobalIndex);

                            SetEffectiveValue(entryIndex, dp, dp.GlobalIndex, metadata, value, BaseValueSourceInternal.Inherited);
                        }
                    }
                }
            }
        }

        //
        //  This method
        //  1. Is used to check if the given entryIndex needs any change. It
        //  could happen that we have made a call out and thereby caused changes
        //  to the _effectiveValues store on the current element. In that case
        //  we would need to aquire new value for the index.
        //
        private EntryIndex CheckEntryIndex(EntryIndex entryIndex, int targetIndex)
        {
            uint effectiveValuesCount = EffectiveValuesCount;
            if (effectiveValuesCount > 0 && _effectiveValues.Length > entryIndex.Index)
            {
                EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];
                if (entry.PropertyIndex == targetIndex)
                {
                    return new EntryIndex(entryIndex.Index);
                }
            }

            return LookupEntry(targetIndex);
        }

        // look for an entry that matches the given dp
        // return value has Found set to true if an entry is found
        // return value has Index set to the index of the found entry (if Found is true)
        //            or  the location to insert an entry for this dp (if Found is false)
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal EntryIndex LookupEntry(int targetIndex)
        {
            int checkIndex;
            uint iLo = 0;
            uint iHi = EffectiveValuesCount;

            if (iHi <= 0)
            {
                return new EntryIndex(0, false /* Found */);
            }

            // Do a binary search to find the value
            while (iHi - iLo > 3)
            {
                uint iPv = (iHi + iLo) / 2;
                checkIndex = _effectiveValues[iPv].PropertyIndex;
                if (targetIndex == checkIndex)
                {
                    return new EntryIndex(iPv);
                }
                if (targetIndex <= checkIndex)
                {
                    iHi = iPv;
                }
                else
                {
                    iLo = iPv + 1;
                }
            }

            // Now we only have three values to search; switch to a linear search
            do
            {
                checkIndex = _effectiveValues[iLo].PropertyIndex;

                if (checkIndex == targetIndex)
                {
                    return new EntryIndex(iLo);
                }

                if (checkIndex > targetIndex)
                {
                    // we've gone past the targetIndex - return not found
                    break;
                }

                iLo++;
            }
            while (iLo < iHi);

            return new EntryIndex(iLo, false /* Found */);
        }

        // insert the given entry at the given index
        // this function assumes that entryIndex is at the right
        // location such that the resulting list remains sorted by EffectiveValueEntry.PropertyIndex
        private void InsertEntry(EffectiveValueEntry entry, uint entryIndex)
        {
            // For thread-safety, sealed DOs can't modify _effectiveValues.
            Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

#if DEBUG
            EntryIndex debugIndex = LookupEntry(entry.PropertyIndex);
            Debug.Assert(!debugIndex.Found && debugIndex.Index == entryIndex, "Inserting duplicate");
#endif

            if (CanModifyEffectiveValues == false)
            {
                throw new InvalidOperationException(SR.Get(SRID.LocalValueEnumerationInvalidated));
            }

            uint effectiveValuesCount = EffectiveValuesCount;
            if (effectiveValuesCount > 0)
            {
                if (_effectiveValues.Length == effectiveValuesCount)
                {
                    int newSize = (int) (effectiveValuesCount * (IsInPropertyInitialization ? 2.0 : 1.2));
                    if (newSize == effectiveValuesCount)
                    {
                        newSize++;
                    }

                    EffectiveValueEntry[] destEntries = new EffectiveValueEntry[newSize];
                    Array.Copy(_effectiveValues, 0, destEntries, 0, entryIndex);
                    destEntries[entryIndex] = entry;
                    Array.Copy(_effectiveValues, entryIndex, destEntries, entryIndex + 1, effectiveValuesCount - entryIndex);
                    _effectiveValues = destEntries;
                }
                else
                {
                    Array.Copy(_effectiveValues, entryIndex, _effectiveValues, entryIndex + 1, effectiveValuesCount - entryIndex);
                    _effectiveValues[entryIndex] = entry;
                }
            }
            else
            {
                if (_effectiveValues == null)
                {
                    _effectiveValues = new EffectiveValueEntry[EffectiveValuesInitialSize];
                }
                _effectiveValues[0] = entry;
            }
            EffectiveValuesCount = effectiveValuesCount + 1;
        }

        // remove the entry at the given index
        private void RemoveEntry(uint entryIndex, DependencyProperty dp)
        {
            // For thread-safety, sealed DOs can't modify _effectiveValues.
            Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

            if (CanModifyEffectiveValues == false)
            {
                throw new InvalidOperationException(SR.Get(SRID.LocalValueEnumerationInvalidated));
            }

            uint effectiveValuesCount = EffectiveValuesCount;
            Array.Copy(_effectiveValues, entryIndex + 1, _effectiveValues, entryIndex, (effectiveValuesCount - entryIndex) - 1);
            effectiveValuesCount--;
            EffectiveValuesCount = effectiveValuesCount;

            // clear last entry
            _effectiveValues[effectiveValuesCount].Clear();
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal virtual int EffectiveValuesInitialSize
        {
            get { return 2; }
        }

        internal void SetEffectiveValue(EntryIndex entryIndex, DependencyProperty dp, PropertyMetadata metadata, EffectiveValueEntry newEntry, EffectiveValueEntry oldEntry)
        {
            if (metadata != null &&
                metadata.IsInherited &&
                (newEntry.BaseValueSourceInternal != BaseValueSourceInternal.Inherited ||
                    newEntry.IsCoerced || newEntry.IsAnimated) &&
                !IsSelfInheritanceParent)
            {
                SetIsSelfInheritanceParent();
                entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
            }

            bool restoreMarker = false;

            if (oldEntry.HasExpressionMarker && !newEntry.HasExpressionMarker)
            {
                BaseValueSourceInternal valueSource = newEntry.BaseValueSourceInternal;
                restoreMarker = (valueSource == BaseValueSourceInternal.ThemeStyle ||
                                 valueSource == BaseValueSourceInternal.ThemeStyleTrigger ||
                                 valueSource == BaseValueSourceInternal.Style ||
                                 valueSource == BaseValueSourceInternal.TemplateTrigger ||
                                 valueSource == BaseValueSourceInternal.StyleTrigger ||
                                 valueSource == BaseValueSourceInternal.ParentTemplate ||
                                 valueSource == BaseValueSourceInternal.ParentTemplateTrigger);
            }

            if (restoreMarker)
            {
                newEntry.RestoreExpressionMarker();
            }
            else if (oldEntry.IsExpression && oldEntry.ModifiedValue.ExpressionValue == Expression.NoValue)
            {
                // we now have a value for an expression that is "hiding" - save it
                // as the expression value
                newEntry.SetExpressionValue(newEntry.Value, oldEntry.ModifiedValue.BaseValue);
            }

#if DEBUG
            object baseValue;
            if (!newEntry.HasModifiers)
            {
                baseValue = newEntry.Value;
            }
            else
            {
                if (newEntry.IsCoercedWithCurrentValue)
                {
                    baseValue = newEntry.ModifiedValue.CoercedValue;
                }
                else if (newEntry.IsExpression)
                {
                    baseValue = newEntry.ModifiedValue.ExpressionValue;
                }
                else
                {
                    baseValue = newEntry.ModifiedValue.BaseValue;
                }
            }

            Debug.Assert(newEntry.IsDeferredReference == (baseValue is DeferredReference));
#endif

            if (entryIndex.Found)
            {
                _effectiveValues[entryIndex.Index] = newEntry;
            }
            else
            {
                InsertEntry(newEntry, entryIndex.Index);
                if (metadata != null && metadata.IsInherited)
                {
                    InheritableEffectiveValuesCount++;
                }
            }

            Debug.Assert(dp == null || (dp.GlobalIndex == newEntry.PropertyIndex), "EffectiveValueEntry & DependencyProperty do not match");
        }

        //
        //  This method
        //  1. Create a new EffectiveValueEntry for the given DP and inserts it into the EffectiveValues list
        //
        [FriendAccessAllowed] // Built into Base, also used by Core and Framework.
        internal void SetEffectiveValue(EntryIndex entryIndex, DependencyProperty dp, int targetIndex, PropertyMetadata metadata, object value, BaseValueSourceInternal valueSource)
        {
            Debug.Assert(value != DependencyProperty.UnsetValue, "Value to be set cannot be UnsetValue");
            Debug.Assert(valueSource != BaseValueSourceInternal.Unknown, "ValueSource cannot be Unknown");

            // For thread-safety, sealed DOs can't modify _effectiveValues.
            Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

            if (metadata != null &&
                metadata.IsInherited &&
                valueSource != BaseValueSourceInternal.Inherited &&
                !IsSelfInheritanceParent)
            {
                SetIsSelfInheritanceParent();
                entryIndex = CheckEntryIndex(entryIndex, dp.GlobalIndex);
            }

            EffectiveValueEntry entry;
            if (entryIndex.Found)
            {
                entry = _effectiveValues[entryIndex.Index];
            }
            else
            {
                entry = new EffectiveValueEntry();
                entry.PropertyIndex = targetIndex;
                InsertEntry(entry, entryIndex.Index);
                if (metadata != null && metadata.IsInherited)
                {
                    InheritableEffectiveValuesCount++;
                }
            }

            bool hasExpressionMarker = (value == ExpressionInAlternativeStore);

            if (!hasExpressionMarker &&
                entry.HasExpressionMarker &&
                (valueSource == BaseValueSourceInternal.ThemeStyle ||
                 valueSource == BaseValueSourceInternal.ThemeStyleTrigger ||
                 valueSource == BaseValueSourceInternal.Style ||
                 valueSource == BaseValueSourceInternal.TemplateTrigger ||
                 valueSource == BaseValueSourceInternal.StyleTrigger ||
                 valueSource == BaseValueSourceInternal.ParentTemplate ||
                 valueSource == BaseValueSourceInternal.ParentTemplateTrigger))
            {
                entry.BaseValueSourceInternal = valueSource;
                entry.SetExpressionValue(value, ExpressionInAlternativeStore);
                entry.ResetAnimatedValue();
                entry.ResetCoercedValue();
            }
            else if (entry.IsExpression && entry.ModifiedValue.ExpressionValue == Expression.NoValue)
            {
                // we now have a value for an expression that is "hiding" - save it
                // as the expression value
                entry.SetExpressionValue(value, entry.ModifiedValue.BaseValue);
            }
            else
            {
                Debug.Assert(entry.BaseValueSourceInternal != BaseValueSourceInternal.Local || valueSource == BaseValueSourceInternal.Local,
                    "No one but another local value can stomp over an existing local value. The only way is to clear the entry");

                entry.BaseValueSourceInternal = valueSource;
                entry.ResetValue(value, hasExpressionMarker);
            }

            Debug.Assert(dp == null || (dp.GlobalIndex == entry.PropertyIndex), "EffectiveValueEntry & DependencyProperty do not match");
            _effectiveValues[entryIndex.Index] = entry;
        }


        //
        //  This method
        //  1. Removes the entry if there is one with valueSource >= the specified
        //
        internal void UnsetEffectiveValue(EntryIndex entryIndex, DependencyProperty dp, PropertyMetadata metadata)
        {
            if (entryIndex.Found)
            {
                RemoveEntry(entryIndex.Index, dp);
                if (metadata != null && metadata.IsInherited)
                {
                    InheritableEffectiveValuesCount--;
                }
            }
        }

        //
        //  This method
        //  1. Sets the expression on a ModifiedValue entry
        //
        private void SetExpressionValue(EntryIndex entryIndex, object value, object baseValue)
        {
            Debug.Assert(value != DependencyProperty.UnsetValue, "Value to be set cannot be UnsetValue");
            Debug.Assert(baseValue != DependencyProperty.UnsetValue, "BaseValue to be set cannot be UnsetValue");
            Debug.Assert(entryIndex.Found == true, "The baseValue for the expression should have been inserted prior to this and hence there should already been an entry for it.");

            // For thread-safety, sealed DOs can't modify _effectiveValues.
            Debug.Assert(!DO_Sealed, "A Sealed DO cannot be modified");

            EffectiveValueEntry entry = _effectiveValues[entryIndex.Index];

            entry.SetExpressionValue(value, baseValue);
            entry.ResetAnimatedValue();
            entry.ResetCoercedValue();
            _effectiveValues[entryIndex.Index] = entry;
        }

        /// <summary>
        ///     Helper method to compare two DP values
        /// </summary>
        private bool Equals(DependencyProperty dp, object value1, object value2)
        {
            if (dp.IsValueType || dp.IsStringType)
            {
                // Use Object.Equals for Strings and ValueTypes
                return Object.Equals(value1, value2);
            }
            else
            {
                // Use Object.ReferenceEquals for all other ReferenceTypes
                return Object.ReferenceEquals(value1, value2);
            }
        }

        #endregion EffectiveValues

        #region InstanceData

        // Specialized Type identification
        private DependencyObjectType _dType;

        // For Freezable:
        //    To save working set this object will initially reference a
        //    single delegate/context.  If a second object is added
        //    of the same type, we will convert to a list/list, which will
        //    be stored in _contextStorage.  If the user ever adds an object of the
        //    other type, we will create a HandlerContextStorage class, which _contextStorage
        //    will then point at.

        // For FrameworkContentElement/FrameworkElement:
        //    This is the parent whose effective values store would contain the
        //    value for the inheritable property on you. This change part of the
        //    performance optimization around inheritable properties whereby you
        //    wouldn't store the inheritable property on each and every node but
        //    will hold it only the node that the property was actually set.
        internal object _contextStorage;

        // The cache of effective values for this DependencyObject
        // This is an array sorted by DP.GlobalIndex.  This ordering is
        // maintained via an insertion sort algorithm.
        private EffectiveValueEntry[] _effectiveValues;

        // Stores:
        // Bits  0- 9 (0x000003FF): EffectiveValuesCount (0-1023)
        // Bits 10-18 (0x0007FC00): InheritableEffectiveValuesCount (0-511)
        //     Bit 19 (0x00080000): CanModifyEffectiveValues, says if you can change the _effectiveValues cache on the current element.
        //     Bit 20 (0x00100000): IsSelfInheritanceParent, says if all your inheritable property values are built into your effectiveValues store
        //     Bit 21 (0x00200000): CanBeInheritanceContext, says if you can be an InheritanceContext for someone
        //     Bit 22 (0x00400000): IsSealed:  whether or not this DO is in readonly mode
        //     Bit 23 (0x00800000): PropertyInitialization mode
        //     Bit 24 (0x01000000): IsInheritanceContextSealed, says if you can change InheritanceContext
        //     Bit 25 (0x02000000): Freezable_HasMultipleInheritanceContexts
        //     Bit 26 (0x04000000): Freezable_UsingHandlerList
        //     Bit 27 (0x08000000): Freezable_UsingContextList
        //     Bit 28 (0x10000000): Freezable_UsingSingletonHandler
        //     Bit 29 (0x20000000): Freezable_UsingSingletonContext
        //     Bit 30 (0x40000000): Animatable_IsResourceInvalidationNecessary
        //     Bit 31 (0x80000000): Animatable_HasAnimatedProperties

        private UInt32 _packedData = 0;

        #endregion InstanceData

        #region StaticData

        // special value in local store meaning that some alternative store (e.g.
        // the Framework's per-instance StyleData) is holding an Expression to
        // which we want to delegate SetValue.
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal static readonly object ExpressionInAlternativeStore = new NamedObject("ExpressionInAlternativeStore");

        // callbacks used for alternative expression storage
        private static AlternativeExpressionStorageCallback _getExpressionCore;

#if VERBOSE_PROPERTY_EVENT
        internal static int ValidationCount;
        internal static int InvalidationCount;
#endif

        // This field stores the list of dependents in a FrugalMap.
        // The field is of type object for two reasons:
        // 1) FrugalMap is a struct, and generics over value types have perf issues
        // 2) so that we can have the default value of "null" mean Unset.
        internal static readonly UncommonField<object> DependentListMapField = new UncommonField<object>();

        // Optimization, to avoid calling FromSystemType too often
        internal static DependencyObjectType DType = DependencyObjectType.FromSystemTypeInternal(typeof(DependencyObject));

        private const int NestedOperationMaximum = 153;

        #endregion StaticData
   }

    /// <summary> Callback used by the "alternative Expression storage" feature </summary>
    /// <remarks>
    /// This should only be used by the Framework.  It should not be used directly by users.
    /// </remarks>
    [FriendAccessAllowed] // Built into Base, also used by Framework.
    internal delegate Expression AlternativeExpressionStorageCallback(DependencyObject d, DependencyProperty dp, PropertyMetadata metadata);

    [FriendAccessAllowed] // Built into Base, also used by Framework.
    internal enum UpdateResult
    {
        ValueChanged = 0x01,
        NotificationSent = 0x02,
        InheritedValueOverridden = 0x04,
    }

    [FriendAccessAllowed] // Built into Base, also used by Framework.
    internal enum RequestFlags
    {
        FullyResolved = 0x00,
        AnimationBaseValue = 0x01,
        CoercionBaseValue = 0x02,
        DeferredReferences = 0x04,
        SkipDefault = 0x08,
        RawEntry = 0x10,
    }
}
