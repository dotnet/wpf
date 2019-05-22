// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines MultiBindingExpression object, uses a collection of BindingExpressions together.
//
// See spec at Data Binding.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;         // FocusChangedEvent
using System.Windows.Markup;
using MS.Internal.Controls; // Validation
using MS.Internal.KnownBoxes;
using MS.Internal.Data;
using MS.Utility;
using MS.Internal;                  // Invariant.Assert

namespace System.Windows.Data
{
/// <summary>
///  Describes a collection of BindingExpressions attached to a single property.
///     The inner BindingExpressions contribute their values to the MultiBindingExpression,
///     which combines/converts them into a resultant final value.
///     In the reverse direction, the target value is tranlated to
///     a set of values that are fed back into the inner BindingExpressions.
/// </summary>
public sealed class MultiBindingExpression: BindingExpressionBase, IDataBindEngineClient
{
    //------------------------------------------------------
    //
    //  Constructors
    //
    //------------------------------------------------------

    /// <summary> Constructor </summary>
    private MultiBindingExpression(MultiBinding binding, BindingExpressionBase owner)
        : base(binding, owner)
    {
        int count = binding.Bindings.Count;

        // reduce repeated allocations
        _tempValues = new object[count];
        _tempTypes = new Type[count];
    }

    //------------------------------------------------------
    //
    //  Interfaces
    //
    //------------------------------------------------------

    void IDataBindEngineClient.TransferValue()
    {
        TransferValue();
    }

    void IDataBindEngineClient.UpdateValue()
    {
        UpdateValue();
    }

    bool IDataBindEngineClient.AttachToContext(bool lastChance)
    {
        AttachToContext(lastChance);
        return !TransferIsDeferred;
    }

    void IDataBindEngineClient.VerifySourceReference(bool lastChance)
    {
    }

    void IDataBindEngineClient.OnTargetUpdated()
    {
        OnTargetUpdated();
    }

    DependencyObject IDataBindEngineClient.TargetElement
    {
        get { return !UsingMentor ? TargetElement : Helper.FindMentor(TargetElement); }
    }

    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    /// <summary> Binding from which this expression was created </summary>
    public MultiBinding ParentMultiBinding { get { return (MultiBinding)ParentBindingBase; } }

    /// <summary> List of inner BindingExpression </summary>
    public ReadOnlyCollection<BindingExpressionBase>   BindingExpressions
    {
        get { return new ReadOnlyCollection<BindingExpressionBase>(MutableBindingExpressions); }
    }

    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    /// <summary> Send the current value back to the source(s) </summary>
    /// <remarks> Does nothing when binding's Mode is not TwoWay or OneWayToSource </remarks>
    public override void UpdateSource()
    {
        // ultimately, what would be better would be to have a status flag that
        // indicates that this MultiBindingExpression has been Detached, as opposed to a
        // MultiBindingExpression that doesn't have anything in its BindingExpressions collection
        // in the first place.  Added to which, there should be distinct error
        // messages for both of these error conditions.
        if (MutableBindingExpressions.Count == 0)
            throw new InvalidOperationException(SR.Get(SRID.BindingExpressionIsDetached));

        NeedsUpdate = true;     // force update
        Update();               // update synchronously
    }

    /// <summary> Force a data transfer from sources to target </summary>
    /// <remarks> Will transfer data even if binding's Mode is OneWay </remarks>
    public override void UpdateTarget()
    {
        // ultimately, what would be better would be to have a status flag that
        // indicates that this MultiBindingExpression has been Detached, as opposed to a
        // MultiBindingExpression that doesn't have anything in its BindingExpressions collection
        // in the first place.  Added to which, there should be distinct error
        // messages for both of these error conditions.
        if (MutableBindingExpressions.Count == 0)
            throw new InvalidOperationException(SR.Get(SRID.BindingExpressionIsDetached));

        UpdateTarget(true);
    }

#region Expression overrides

#endregion  Expression overrides

    //------------------------------------------------------
    //
    //  Internal Properties
    //
    //------------------------------------------------------

    internal override bool IsParentBindingUpdateTriggerDefault
    {
        get { return (ParentMultiBinding.UpdateSourceTrigger == UpdateSourceTrigger.Default); }
    }

    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    // Create a new BindingExpression from the given Binding description
    internal static MultiBindingExpression CreateBindingExpression(DependencyObject d, DependencyProperty dp, MultiBinding binding, BindingExpressionBase owner)
    {
        FrameworkPropertyMetadata fwMetaData = dp.GetMetadata(d.DependencyObjectType) as FrameworkPropertyMetadata;

        if ((fwMetaData != null && !fwMetaData.IsDataBindingAllowed) || dp.ReadOnly)
            throw new ArgumentException(SR.Get(SRID.PropertyNotBindable, dp.Name), "dp");

        // create the BindingExpression
        MultiBindingExpression bindExpr = new MultiBindingExpression(binding, owner);

        bindExpr.ResolvePropertyDefaultSettings(binding.Mode, binding.UpdateSourceTrigger, fwMetaData);

        return bindExpr;
    }

    // Attach to things that may require tree context (parent, root, etc.)
    void AttachToContext(bool lastChance)
    {
        DependencyObject target = TargetElement;
        if (target == null)
            return;

        Debug.Assert(ParentMultiBinding.Converter != null || !String.IsNullOrEmpty(EffectiveStringFormat),
                "MultiBindingExpression should not exist if its bind does not have a valid converter.");

        bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.AttachToContext);

        _converter = ParentMultiBinding.Converter;
        if (_converter == null && String.IsNullOrEmpty(EffectiveStringFormat))
        {
            TraceData.Trace(TraceEventType.Error, TraceData.MultiBindingHasNoConverter, ParentMultiBinding);
        }

        if (isExtendedTraceEnabled)
        {
            TraceData.Trace(TraceEventType.Warning,
                                TraceData.AttachToContext(
                                    TraceData.Identify(this),
                                    lastChance ? " (last chance)" : String.Empty));
        }

        TransferIsDeferred = true;
        bool attached = true;       // true if all child bindings have attached
        int count = MutableBindingExpressions.Count;
        for (int i = 0; i < count; ++i)
        {
            if (MutableBindingExpressions[i].StatusInternal == BindingStatusInternal.Unattached)
                attached = false;
        }

        // if the child bindings aren't ready yet, try again later.  Leave
        // TransferIsDeferred set, to indicate we're not ready yet.
        if (!attached && !lastChance)
        {
            if (isExtendedTraceEnabled)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.ChildNotAttached(
                                        TraceData.Identify(this)));
            }

            return;
        }

        // listen to the Language property, if needed
        if (UsesLanguage)
        {
            WeakDependencySource[] commonSources = new WeakDependencySource[] { new WeakDependencySource(TargetElement, FrameworkElement.LanguageProperty) };
            WeakDependencySource[] newSources = CombineSources(-1, MutableBindingExpressions, MutableBindingExpressions.Count, null, commonSources);
            ChangeSources(newSources);
        }

        // initial transfer
        bool initialTransferIsUpdate = IsOneWayToSource;
        object currentValue;
        if (ShouldUpdateWithCurrentValue(target, out currentValue))
        {
            initialTransferIsUpdate = true;
            ChangeValue(currentValue, /*notify*/false);
            NeedsUpdate = true;
        }

        SetStatus(BindingStatusInternal.Active);

        if (!initialTransferIsUpdate)
        {
            UpdateTarget(false);
        }
        else
        {
            UpdateValue();
        }
    }


    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    /// <summary>
    ///     The ValidationError that caused this
    ///     BindingExpression to be invalid.
    /// </summary>
    public override ValidationError ValidationError
    {
        get
        {
            ValidationError validationError = base.ValidationError;

            if (validationError == null)
            {
                for ( int i = 0; i < MutableBindingExpressions.Count; i++ )
                {
                    validationError = MutableBindingExpressions[i].ValidationError;
                    if (validationError != null)
                        break;
                }
            }

            return validationError;
        }
    }

    /// <summary>
    ///     HasError returns true if any of the ValidationRules
    ///     of any of its inner bindings failed its validation rule
    ///     or the Multi-/PriorityBinding itself has a failing validation rule.
    /// </summary>
    public override bool HasError
    {
        get
        {
            bool hasError = base.HasError;

            if (!hasError)
            {
                for ( int i = 0; i < MutableBindingExpressions.Count; i++ )
                {
                    if (MutableBindingExpressions[i].HasError)
                        return true;
                }
            }

            return hasError;
        }
    }

    /// <summary>
    ///     HasValidationError returns true if any of the ValidationRules
    ///     of any of its inner bindings failed its validation rule
    ///     or the Multi-/PriorityBinding itself has a failing validation rule.
    /// </summary>
    public override bool HasValidationError
    {
        get
        {
            bool hasError = base.HasValidationError;

            if (!hasError)
            {
                for ( int i = 0; i < MutableBindingExpressions.Count; i++ )
                {
                    if (MutableBindingExpressions[i].HasValidationError)
                        return true;
                }
            }

            return hasError;
        }
    }

    //------------------------------------------------------
    //
    //  Protected Internal Methods
    //
    //------------------------------------------------------

    /// <summary>
    ///     Attach a BindingExpression to the given target (element, property)
    /// </summary>
    /// <param name="d">DependencyObject being set</param>
    /// <param name="dp">Property being set</param>
    internal override bool AttachOverride(DependencyObject d, DependencyProperty dp)
    {
        if (!base.AttachOverride(d, dp))
            return false;

        DependencyObject target = TargetElement;
        if (target == null)
            return false;

        // listen for lost focus
        if (IsUpdateOnLostFocus)
        {
            LostFocusEventManager.AddHandler(target, OnLostFocus);
        }

        TransferIsDeferred = true;          // Defer data transfer until after we activate all the BindingExpressions
        int count = ParentMultiBinding.Bindings.Count;
        for (int i = 0; i < count; ++i)
        {
            // ISSUE: It may be possible to have _attachedBindingExpressions be non-zero
            // at the end of Detach if the conditions for the increment on Attach
            // and the decrement on Detach are not precisely the same.
            AttachBindingExpression(i, false); // create new binding and have it added to end
        }

        // attach to things that need tree context.  Do it synchronously
        // if possible, otherwise post a task.  This gives the parser et al.
        // a chance to assemble the tree before we start walking it.
        AttachToContext(false /* lastChance */);
        if (TransferIsDeferred)
        {
            Engine.AddTask(this, TaskOps.AttachToContext);

            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.AttachToContext))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.DeferAttachToContext(
                                        TraceData.Identify(this)));
            }
        }

        return true;
    }

    /// <summary> sever all connections </summary>
    internal override void DetachOverride()
    {
        DependencyObject target = TargetElement;
        if (target != null && IsUpdateOnLostFocus)
        {
            LostFocusEventManager.RemoveHandler(target, OnLostFocus);
        }

        // Theoretically, we only need to detach number of AttentiveBindingExpressions,
        // but we'll traverse the whole list anyway and do aggressive clean-up.
        int count = MutableBindingExpressions.Count;

        for (int i = count - 1; i >= 0; i--)
        {
            BindingExpressionBase b = MutableBindingExpressions[i];

            if (b != null)
            {
                b.Detach();
                MutableBindingExpressions.RemoveAt(i);
            }
        }

        ChangeSources(null);

        base.DetachOverride();
    }

    /// <summary>
    /// Invalidate the given child expression.
    /// </summary>
    internal override void InvalidateChild(BindingExpressionBase bindingExpression)
    {
        int index = MutableBindingExpressions.IndexOf(bindingExpression);

        // do a sanity check that we care about this BindingExpression
        if (0 <= index && IsDynamic)
        {
            NeedsDataTransfer = true;
            Transfer();                 // this will Invalidate target property.
        }
    }

    /// <summary>
    /// Change the dependency sources for the given child expression.
    /// </summary>
    internal override void ChangeSourcesForChild(BindingExpressionBase bindingExpression, WeakDependencySource[] newSources)
    {
        int index = MutableBindingExpressions.IndexOf(bindingExpression);

        if (index >= 0)
        {
            WeakDependencySource[] commonSources = null;
            if (UsesLanguage)
            {
                commonSources = new WeakDependencySource[] { new WeakDependencySource(TargetElement, FrameworkElement.LanguageProperty) };
            }

            WeakDependencySource[] combinedSources = CombineSources(index, MutableBindingExpressions, MutableBindingExpressions.Count, newSources, commonSources);
            ChangeSources(combinedSources);
        }
    }

    /// <summary>
    /// Replace the given child expression with a new one.
    /// </summary>
    internal override void ReplaceChild(BindingExpressionBase bindingExpression)
    {
        int index = MutableBindingExpressions.IndexOf(bindingExpression);
        DependencyObject target = TargetElement;

        if (index >= 0 && target != null)
        {
            // detach and clean up the old binding
            bindingExpression.Detach();

            // replace BindingExpression
            AttachBindingExpression(index, true);
        }
    }

    // register the leaf bindings with the binding group
    internal override void UpdateBindingGroup(BindingGroup bg)
    {
        for (int i=0, n=MutableBindingExpressions.Count-1; i<n; ++i)
        {
            MutableBindingExpressions[i].UpdateBindingGroup(bg);
        }
    }

    /// <summary>
    /// Get the converted proposed value
    /// <summary>
    internal override object ConvertProposedValue(object value)
    {
        object result;
        bool success = ConvertProposedValueImpl(value, out result);

        // if the conversion failed, signal a validation error
        if (!success)
        {
            result = DependencyProperty.UnsetValue;
            ValidationError validationError = new ValidationError(ConversionValidationRule.Instance, this, SR.Get(SRID.Validation_ConversionFailed, value), null);
            UpdateValidationError(validationError);
        }

        return result;
    }

    private bool ConvertProposedValueImpl(object value, out object result)
    {
        DependencyObject target = TargetElement;
        if (target == null)
        {
            result = DependencyProperty.UnsetValue;
            return false;
        }

        result = GetValuesForChildBindings(value);

        if (IsDetached)
        {
            return false;   // user code detached the binding.  give up.
        }

        if (result == DependencyProperty.UnsetValue)
        {
            SetStatus(BindingStatusInternal.UpdateSourceError);

            return false;
        }

        object[] values = (object[])result;
        if (values == null)
        {
            if (TraceData.IsEnabled)
            {
                TraceData.Trace(TraceEventType.Error,
                    TraceData.BadMultiConverterForUpdate(
                        Converter.GetType().Name,
                        AvTrace.ToStringHelper(value),
                        AvTrace.TypeName(value)),
                    this);
            }

            result = DependencyProperty.UnsetValue;
            return false;
        }

        if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Update))
        {
            for (int i=0; i<values.Length; ++i)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.UserConvertBackMulti(
                                        TraceData.Identify(this),
                                        i,
                                        TraceData.Identify(values[i])));
            }
        }

        // if lengths are mismatched, show warning
        int count = MutableBindingExpressions.Count;
        if (values.Length != count && TraceData.IsEnabled)
        {
            TraceData.Trace(TraceEventType.Information, TraceData.MultiValueConverterMismatch,
                    Converter.GetType().Name, count, values.Length,
                    TraceData.DescribeTarget(target, TargetProperty));
        }

        // use the smaller count
        if (values.Length < count)
            count = values.Length;

        // using the result of ConvertBack as the raw value, run each child binding
        // through the first two steps of the update/validate process
        bool success = true;
        for (int i = 0; i < count; ++i)
        {
            value = values[i];

            if (value != Binding.DoNothing && value != DependencyProperty.UnsetValue)
            {
                BindingExpressionBase bindExpr = MutableBindingExpressions[i];

                bindExpr.SetValue(target, TargetProperty, value);   // could pass (null, null, values[i])

                value = bindExpr.GetRawProposedValue();
                if (!bindExpr.Validate(value, ValidationStep.RawProposedValue))
                    value = DependencyProperty.UnsetValue;

                value = bindExpr.ConvertProposedValue(value);
            }
            else if (value == DependencyProperty.UnsetValue && TraceData.IsEnabled)
            {
                TraceData.Trace(TraceEventType.Information,
                    TraceData.UnsetValueInMultiBindingExpressionUpdate(
                        Converter.GetType().Name,
                        AvTrace.ToStringHelper(value),
                        i,
                        _tempTypes[i]
                    ),
                    this);
            }

            if (value == DependencyProperty.UnsetValue)
            {
                success = false;
            }

            values[i] = value;
        }

        Array.Clear(_tempTypes, 0, _tempTypes.Length);
        result = values;
        return success;
    }

    object GetValuesForChildBindings(object rawValue)
    {
        if (Converter == null)
        {
            if (TraceData.IsEnabled)
            {
                TraceData.Trace(TraceEventType.Error, TraceData.MultiValueConverterMissingForUpdate, this);
            }

            return DependencyProperty.UnsetValue;
        }

        CultureInfo culture = GetCulture();
        int count = MutableBindingExpressions.Count;

        for (int i = 0; i < count; ++i)
        {
            BindingExpressionBase bindExpr = MutableBindingExpressions[i];
            BindingExpression be = bindExpr as BindingExpression;

            if (be != null && be.UseDefaultValueConverter)
                _tempTypes[i] = be.ConverterSourceType;
            else
                _tempTypes[i] = TargetProperty.PropertyType;
        }

        // MultiValueConverters are always user-defined, so don't catch exceptions (bug 992237)
        return Converter.ConvertBack(rawValue, _tempTypes, ParentMultiBinding.ConverterParameter, culture);
    }

    /// <summary>
    /// Get the converted proposed value and inform the binding group
    /// <summary>
    internal override bool ObtainConvertedProposedValue(BindingGroup bindingGroup)
    {
        bool result = true;
        if (NeedsUpdate)
        {
            object value = bindingGroup.GetValue(this);
            if (value != DependencyProperty.UnsetValue)
            {
                object[] values;
                value = ConvertProposedValue(value);

                if (value == DependencyProperty.UnsetValue)
                {
                    result = false;
                }
                else if ((values = value as object[]) != null)
                {
                    for (int i=0; i<values.Length; ++i)
                    {
                        if (values[i] == DependencyProperty.UnsetValue)
                        {
                            result = false;
                        }
                    }
                }
            }
            StoreValueInBindingGroup(value, bindingGroup);
        }
        else
        {
            bindingGroup.UseSourceValue(this);
        }

        return result;
    }

    /// <summary>
    /// Update the source value
    /// <summary>
    internal override object UpdateSource(object convertedValue)
    {
        if (convertedValue == DependencyProperty.UnsetValue)
        {
            SetStatus(BindingStatusInternal.UpdateSourceError);
            return convertedValue;
        }

        object[] values = convertedValue as object[];
        int count = MutableBindingExpressions.Count;
        if (values.Length < count)
            count = values.Length;

        BeginSourceUpdate();
        bool updateActuallyHappened = false;
        for (int i = 0; i < count; ++i)
        {
            object value = values[i];

            if (value != Binding.DoNothing)
            {
                BindingExpressionBase bindExpr = MutableBindingExpressions[i];

                bindExpr.UpdateSource(value);

                if (bindExpr.StatusInternal == BindingStatusInternal.UpdateSourceError)
                {
                    SetStatus(BindingStatusInternal.UpdateSourceError);
                }

                updateActuallyHappened = true;
            }
        }

        if (!updateActuallyHappened)
        {
            IsInUpdate = false;     // inhibit the "$10 bug" re-fetch if nothing actually updated
        }

        EndSourceUpdate();

        OnSourceUpdated();

        return convertedValue;
    }

    /// <summary>
    /// Update the source value and inform the binding group
    /// <summary>
    internal override bool UpdateSource(BindingGroup bindingGroup)
    {
        bool result = true;
        if (NeedsUpdate)
        {
            object value = bindingGroup.GetValue(this);
            UpdateSource(value);
            if (value == DependencyProperty.UnsetValue)
            {
                result = false;
            }
        }
        return result;
    }

    /// <summary>
    /// Store the value in the binding group
    /// </summary>
    internal override void StoreValueInBindingGroup(object value, BindingGroup bindingGroup)
    {
        bindingGroup.SetValue(this, value);

        object[] values = value as object[];
        if (values != null)
        {
            int count = MutableBindingExpressions.Count;
            if (values.Length < count)
                count = values.Length;

            for (int i=0; i<count; ++i)
            {
                MutableBindingExpressions[i].StoreValueInBindingGroup(values[i], bindingGroup);
            }
        }
        else
        {
            for (int i=MutableBindingExpressions.Count-1; i>=0; --i)
            {
                MutableBindingExpressions[i].StoreValueInBindingGroup(DependencyProperty.UnsetValue, bindingGroup);
            }
        }
    }

    /// <summary>
    /// Run validation rules for the given step
    /// <summary>
    internal override bool Validate(object value, ValidationStep validationStep)
    {
        if (value == Binding.DoNothing)
            return true;

        if (value == DependencyProperty.UnsetValue)
        {
            SetStatus(BindingStatusInternal.UpdateSourceError);
            return false;
        }

        // run rules attached to this multibinding
        bool result = base.Validate(value, validationStep);

        // run rules attached to the child bindings
        switch (validationStep)
        {
            case ValidationStep.RawProposedValue:
                // the child bindings don't get raw values until the Convert step
                break;

            default:
                object[] values = value as object[];
                int count = MutableBindingExpressions.Count;
                if (values.Length < count)
                    count = values.Length;

                for (int i=0; i<count; ++i)
                {
                    value = values[i];
                    if (value == DependencyProperty.UnsetValue)
                    {
                        // an unset value means the binding failed validation at an earlier step,
                        // typically at Raw step, evaluated during the MultiBinding's ConvertValue

                        //result = false;
                        // COMPAT: This should mean the MultiBinding as a whole fails validation, but
                        // in 3.5 this didn't happen.  Instead the process continued, writing back
                        // values to child bindings that succeeded, and simply not writing back
                        // to child bindings that didn't.
                    }
                    else if (value != Binding.DoNothing)
                    {
                        if (!MutableBindingExpressions[i].Validate(value, validationStep))
                        {
                            values[i] = DependencyProperty.UnsetValue;  // prevent writing an invalid value

                            //result = false;
                            // COMPAT: as above, preserve v3.5 behavior by not failing when a
                            // child binding fails to validate
                        }
                    }
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// Run validation rules for the given step, and inform the binding group
    /// <summary>
    internal override bool CheckValidationRules(BindingGroup bindingGroup, ValidationStep validationStep)
    {
        if (!NeedsValidation)
            return true;

        object value;
        switch (validationStep)
        {
            case ValidationStep.RawProposedValue:
            case ValidationStep.ConvertedProposedValue:
            case ValidationStep.UpdatedValue:
            case ValidationStep.CommittedValue:
                value = bindingGroup.GetValue(this);
                break;
            default:
                throw new InvalidOperationException(SR.Get(SRID.ValidationRule_UnknownStep, validationStep, bindingGroup));
        }

        bool result = Validate(value, validationStep);

        if (result && validationStep == ValidationStep.CommittedValue)
        {
            NeedsValidation = false;
        }

        return result;
    }

    /// <summary>
    /// Get the proposed value(s) that would be written to the source(s), applying
    /// conversion and checking UI-side validation rules.
    /// </summary>
    internal override bool ValidateAndConvertProposedValue(out Collection<ProposedValue> values)
    {
        Debug.Assert(NeedsValidation, "check NeedsValidation before calling this");
        values = null;

        // validate raw proposed value
        object rawValue = GetRawProposedValue();
        bool isValid = Validate(rawValue, ValidationStep.RawProposedValue);
        if (!isValid)
        {
            return false;
        }

        // apply conversion
        object conversionResult = GetValuesForChildBindings(rawValue);
        if (IsDetached || conversionResult == DependencyProperty.UnsetValue || conversionResult == null)
        {
            return false;
        }

        int count = MutableBindingExpressions.Count;
        object[] convertedValues = (object[])conversionResult;
        if (convertedValues.Length < count)
            count = convertedValues.Length;

        values = new Collection<ProposedValue>();
        bool result = true;

        // validate child bindings
        for (int i = 0; i < count; ++i)
        {
            object value = convertedValues[i];
            if (value == Binding.DoNothing)
            {
            }
            else if (value == DependencyProperty.UnsetValue)
            {
                // conversion failure
                result = false;
            }
            else
            {
                // send converted value to child binding
                BindingExpressionBase bindExpr = MutableBindingExpressions[i];
                bindExpr.Value = value;

                // validate child binding
                if (bindExpr.NeedsValidation)
                {
                    Collection<ProposedValue> childValues;
                    bool childResult = bindExpr.ValidateAndConvertProposedValue(out childValues);

                    // append child's values to our values
                    if (childValues != null)
                    {
                        for (int k=0, n=childValues.Count; k<n; ++k)
                        {
                            values.Add(childValues[k]);
                        }
                    }

                    // merge child's result
                    result = result && childResult;
                }
            }
        }

        return result;
    }


    // Return the object from which the given value was obtained, if possible
    internal override object GetSourceItem(object newValue)
    {
        if (newValue == null)
            return null;        // this avoids false positive results

        // It's impossible to find the source item in the general case - the value
        // may have been produced by the multi-converter, combining inputs from
        // several different sources.   But we can do it in the special case where
        // one of the child bindings actually produced the final value, and the
        // converter merely selected it (or did other extraneous work).
        int count = MutableBindingExpressions.Count;
        for (int i = 0; i < count; ++i)
        {
            object value = MutableBindingExpressions[i].GetValue(null, null); // could pass (null, null)
            if (ItemsControl.EqualsEx(value, newValue))
                return MutableBindingExpressions[i].GetSourceItem(newValue);
        }

        return null;
    }


    //------------------------------------------------------
    //
    //  Private Properties
    //
    //------------------------------------------------------

    /// <summary>
    /// expose a mutable version of the list of all BindingExpressions;
    /// derived internal classes need to be able to populate this list
    /// </summary>
    private Collection<BindingExpressionBase> MutableBindingExpressions
    {
        get { return _list; }
    }

    IMultiValueConverter Converter
    {
        get { return _converter; }
        set { _converter = value; }
    }

    //------------------------------------------------------
    //
    //  Private Methods
    //
    //------------------------------------------------------

    // Create a BindingExpression for position i
    BindingExpressionBase AttachBindingExpression(int i, bool replaceExisting)
    {
        DependencyObject target = TargetElement;
        if (target == null)
            return null;

        BindingBase binding = ParentMultiBinding.Bindings[i];

        // Check if replacement bindings have the correct UpdateSourceTrigger
        MultiBinding.CheckTrigger(binding);

        BindingExpressionBase bindExpr = binding.CreateBindingExpression(target, TargetProperty, this);
        if (replaceExisting) // replace exisiting or add as new binding?
            MutableBindingExpressions[i] = bindExpr;
        else
            MutableBindingExpressions.Add(bindExpr);

        bindExpr.Attach(target, TargetProperty);
        return bindExpr;
    }

    internal override void HandlePropertyInvalidation(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        DependencyProperty dp = args.Property;

        if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
        {
            TraceData.Trace(TraceEventType.Warning,
                                TraceData.GotPropertyChanged(
                                    TraceData.Identify(this),
                                    TraceData.Identify(d),
                                    dp.Name));
        }

        bool isConnected = true;
        TransferIsDeferred = true;

        if (UsesLanguage && d == TargetElement && dp == FrameworkElement.LanguageProperty)
        {
            InvalidateCulture();
            NeedsDataTransfer = true;   // force a transfer - it will honor the new culture
        }

        // if the binding has been detached (by the reference to TargetElement), quit now
        if (IsDetached)
            return;

        int n = MutableBindingExpressions.Count;
        for (int i = 0; i < n; ++i)
        {
            BindingExpressionBase bindExpr = MutableBindingExpressions[i];
            if (bindExpr != null)
            {
                DependencySource[] sources = bindExpr.GetSources();

                if (sources != null)
                {
                    for (int j = 0; j < sources.Length; ++j)
                    {
                        DependencySource source = sources[j];

                        if (source.DependencyObject == d && source.DependencyProperty == dp)
                        {
                            bindExpr.OnPropertyInvalidation(d, args);
                            break;
                        }
                    }
                }

                if (bindExpr.IsDisconnected)
                {
                    isConnected = false;
                }
            }
        }

        TransferIsDeferred = false;

        if (isConnected)
        {
            Transfer();                 // Transfer if inner BindingExpressions have called Invalidate(binding)
        }
        else
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Handle events from the centralized event table
    /// </summary>
    internal override bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
            return false;   // this method is no longer used (but must remain, for compat)
    }

    internal override void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
        {
            TraceData.Trace(TraceEventType.Warning,
                                TraceData.GotEvent(
                                    TraceData.Identify(this),
                                    "LostFocus",
                                    TraceData.Identify(sender)));
        }

        Update();
    }

#region Value

    /// <summary> Force a data transfer from source(s) to target </summary>
    /// <param name="includeInnerBindings">
    ///     use true to propagate UpdateTarget call to all inner BindingExpressions;
    ///     use false to avoid forcing data re-transfer from one-time inner BindingExpressions
    /// </param>
    void UpdateTarget(bool includeInnerBindings)
    {
        TransferIsDeferred = true;

        if (includeInnerBindings)
        {
            foreach (BindingExpressionBase b in MutableBindingExpressions)
            {
                b.UpdateTarget();
            }
        }

        TransferIsDeferred = false;
        NeedsDataTransfer = true;   // force data transfer
        Transfer();

        NeedsUpdate = false;
    }

    // transfer a value from the source to the target
    void Transfer()
    {
        // required state for transfer
        if (    NeedsDataTransfer       // Transfer is needed
            &&  StatusInternal != BindingStatusInternal.Unattached  // All bindings are attached
            &&  !TransferIsDeferred)    // Not aggregating transfers
        {
            TransferValue();
        }
    }

    // transfer a value from the source to the target
    void TransferValue()
    {
        IsInTransfer = true;
        NeedsDataTransfer = false;

        DependencyObject target = TargetElement;
        if (target == null)
            goto Done;

        bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Transfer);

        object value = DependencyProperty.UnsetValue;
        object preFormattedValue = _tempValues;
        CultureInfo culture = GetCulture();

        // gather values from inner BindingExpressions
        int count = MutableBindingExpressions.Count;
        for (int i = 0; i < count; ++i)
        {
            _tempValues[i] = MutableBindingExpressions[i].GetValue(target, TargetProperty); // could pass (null, null)

            if (isExtendedTraceEnabled)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.GetRawValueMulti(
                                        TraceData.Identify(this),
                                        i,
                                        TraceData.Identify(_tempValues[i])));
            }
        }

        // apply the converter
        if (Converter != null)
        {
            // MultiValueConverters are always user-defined, so don't catch exceptions (bug 992237)
            preFormattedValue = Converter.Convert(_tempValues, TargetProperty.PropertyType, ParentMultiBinding.ConverterParameter, culture);

            if (IsDetached)
            {
                // user code detached the binding.  Give up.
                return;
            }

            if (isExtendedTraceEnabled)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.UserConverter(
                                        TraceData.Identify(this),
                                        TraceData.Identify(preFormattedValue)));
            }
        }
        else if (EffectiveStringFormat != null)
        {
            // preFormattedValue = _tempValues;
            // But check for child binding conversion errors
            for (int i=0; i<_tempValues.Length; ++i)
            {
                if (_tempValues[i] == DependencyProperty.UnsetValue)
                {
                    preFormattedValue = DependencyProperty.UnsetValue;
                    break;
                }
            }
        }
        else    // no converter (perhaps user specified it in error)
        {
            if (TraceData.IsEnabled)
            {
                TraceData.Trace(TraceEventType.Error, TraceData.MultiValueConverterMissingForTransfer, this);
            }

            goto Done;
        }

        // apply string formatting
        if (EffectiveStringFormat == null || preFormattedValue == Binding.DoNothing || preFormattedValue == DependencyProperty.UnsetValue)
        {
            value = preFormattedValue;
        }
        else
        {
            try
            {
                // we call String.Format either with multiple values (obtained from
                // the child bindings) or a single value (as produced by the converter).
                // The if-test is needed to avoid wrapping _tempValues inside another object[].
                if (preFormattedValue == _tempValues)
                {
                    value = String.Format(culture, EffectiveStringFormat, _tempValues);
                }
                else
                {
                    value = String.Format(culture, EffectiveStringFormat, preFormattedValue);
                }

                if (isExtendedTraceEnabled)
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.FormattedValue(
                                            TraceData.Identify(this),
                                            TraceData.Identify(value)));
                }
            }
            catch (FormatException)
            {
                // formatting didn't work
                value = DependencyProperty.UnsetValue;

                if (isExtendedTraceEnabled)
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.FormattingFailed(
                                            TraceData.Identify(this),
                                            EffectiveStringFormat));
                }
            }
        }

        Array.Clear(_tempValues, 0, _tempValues.Length);

        // the special value DoNothing means no error, but no data transfer
        if (value == Binding.DoNothing)
            goto Done;

        // ultimately, TargetNullValue should get assigned implicitly,
        // even if the user doesn't declare it.  We can't do this yet because
        // of back-compat.  I wrote it both ways, and #if'd out the breaking
        // change.
    #if TargetNullValueBC   //BreakingChange
        if (IsNullValue(value))
    #else
        if (EffectiveTargetNullValue != DependencyProperty.UnsetValue &&
            IsNullValue(value))
    #endif
        {
            value = EffectiveTargetNullValue;

            if (isExtendedTraceEnabled)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.NullConverter(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)));
            }
        }

        // if the value isn't acceptable to the target property, don't use it
        if (value != DependencyProperty.UnsetValue && !TargetProperty.IsValidValue(value))
        {
            if (TraceData.IsEnabled)
            {
                TraceData.Trace(TraceLevel, TraceData.BadValueAtTransfer, value, this);
            }

            if (isExtendedTraceEnabled)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.BadValueAtTransferExtended(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)));
            }

            value = DependencyProperty.UnsetValue;
        }

        // if we can't obtain a value, try the fallback value.
        if (value == DependencyProperty.UnsetValue)
        {
            value = UseFallbackValue();

            if (isExtendedTraceEnabled)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.UseFallback(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)));
            }
        }

        if (isExtendedTraceEnabled)
        {
            TraceData.Trace(TraceEventType.Warning,
                                TraceData.TransferValue(
                                    TraceData.Identify(this),
                                    TraceData.Identify(value)));
        }

        // if this is a re-transfer after a source update and the value
        // hasn't changed, don't do any more work.
        bool realTransfer = !(IsInUpdate && ItemsControl.EqualsEx(value, Value));

        if (realTransfer)
        {
            // update the cached value
            ChangeValue(value, true);

            // push the new value through the property engine
            Invalidate(false);

            Validation.ClearInvalid(this);
        }

        // after updating all the state (value, validation), mark the binding clean
        Clean();

        if (realTransfer)
        {
            OnTargetUpdated();
        }

    Done:
        IsInTransfer = false;
    }

    void OnTargetUpdated()
    {
        if (NotifyOnTargetUpdated)
        {
            DependencyObject target = TargetElement;
            if (target != null)
            {
                // while attaching a normal (not style-defined) BindingExpression,
                // we must defer raising the event until after the
                // property has been invalidated, so that the event handler
                // gets the right value if it asks (bug 1036862)
                if (IsAttaching && this == target.ReadLocalValue(TargetProperty))
                {
                    Engine.AddTask(this, TaskOps.RaiseTargetUpdatedEvent);
                }
                else
                {
                    BindingExpression.OnTargetUpdated(target, TargetProperty);
                }
            }
        }
    }

    void OnSourceUpdated()
    {
        if (NotifyOnSourceUpdated)
        {
            DependencyObject target = TargetElement;
            if (target != null)
            {
                BindingExpression.OnSourceUpdated(target, TargetProperty);
            }
        }
    }

    internal override bool ShouldReactToDirtyOverride()
    {
        // react only if all the child bindings should react
        foreach (BindingExpressionBase beb in MutableBindingExpressions)
        {
            if (!beb.ShouldReactToDirtyOverride())
            {
                return false;
            }
        }
        return true;
    }

    // transfer a value from the target to the source
    internal override bool UpdateOverride()
    {
        // various reasons not to update:
        if (   !NeedsUpdate                     // nothing to do
            || !IsReflective                    // no update desired
            || IsInTransfer                     // in a transfer
            || StatusInternal == BindingStatusInternal.Unattached // not ready yet
            )
            return true;

        return UpdateValue();
    }

#endregion Value

    //------------------------------------------------------
    //
    //  Private Fields
    //
    //------------------------------------------------------

    Collection<BindingExpressionBase>  _list = new Collection<BindingExpressionBase>();
    IMultiValueConverter    _converter;
    object[]                _tempValues;
    Type[]                  _tempTypes;
}
}
