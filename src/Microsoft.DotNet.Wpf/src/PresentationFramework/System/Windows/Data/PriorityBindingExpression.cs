// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines PriorityBindingExpression object, which chooses a BindingExpression out
//              of a list of BindingExpressions in order of "priority" (and falls back
//              to the next BindingExpression as each BindingExpression fails)
//
// See spec at Data Binding.mht
//

using System;
using System.Collections;
using System.Collections.ObjectModel;   // Collection<T>
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;          // ValidationStep
using System.Windows.Threading;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Data;
using MS.Utility;

namespace System.Windows.Data
{
/// <summary>
///  Describes a collection of BindingExpressions attached to a single property.
///     These behave as "priority" BindingExpressions, meaning that the property
///     receives its value from the first BindingExpression in the collection that
///     can produce a legal value.
/// </summary>
public sealed class PriorityBindingExpression : BindingExpressionBase
{
    //------------------------------------------------------
    //
    //  Constructors
    //
    //------------------------------------------------------

    private PriorityBindingExpression(PriorityBinding binding, BindingExpressionBase owner)
        : base(binding, owner)
    {
    }

    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    /// <summary> Binding from which this expression was created </summary>
    public PriorityBinding ParentPriorityBinding { get { return (PriorityBinding)ParentBindingBase; } }

    /// <summary> List of inner BindingExpression </summary>
    public ReadOnlyCollection<BindingExpressionBase>   BindingExpressions
    {
        get { return new ReadOnlyCollection<BindingExpressionBase>(MutableBindingExpressions); }
    }

    /// <summary> Returns the active BindingExpression (or null) </summary>
    public BindingExpressionBase ActiveBindingExpression
    {
        get { return (_activeIndex < 0) ? null : MutableBindingExpressions[_activeIndex]; }
    }

    /// <summary>
    ///     HasValidationError returns true if any of the ValidationRules
    ///     of any of its inner bindings failed its validation rule
    ///     or the Multi-/PriorityBinding itself has a failing validation rule.
    /// </summary>
    public override bool HasValidationError
    {
        get { return (_activeIndex < 0) ? false : MutableBindingExpressions[_activeIndex].HasValidationError; }
    }

    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    /// <summary> Force a data transfer from source to target </summary>
    public override void UpdateTarget()
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            bindExpr.UpdateTarget();
        }
    }

    /// <summary> Send the current value back to the source </summary>
    /// <remarks> Does nothing when binding's Mode is not TwoWay or OneWayToSource </remarks>
    public override void UpdateSource()
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            bindExpr.UpdateSource();
        }
    }

#region Expression overrides

    /// <summary>
    ///     Allows Expression to store set values
    /// </summary>
    /// <param name="d">DependencyObject being set</param>
    /// <param name="dp">Property being set</param>
    /// <param name="value">Value being set</param>
    /// <returns>true if Expression handled storing of the value</returns>
    internal override bool SetValue(DependencyObject d, DependencyProperty dp, object value)
    {
        bool result;
        BindingExpressionBase bindExpr = ActiveBindingExpression;

        if (bindExpr != null)
        {
            result = bindExpr.SetValue(d, dp, value);
            if (result)
            {
                // the active binding's value becomes the value of the priority binding
                Value = bindExpr.Value;

                AdoptProperties(bindExpr);
                NotifyCommitManager();
            }
        }
        else
        {
            // If we couldn't find the active binding, just return true to keep the property
            // engine from removing the PriorityBinding.
            result = true;
        }

        return result;
    }

#endregion  Expression overrides

    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    // Create a new BindingExpression from the given Binding description
    internal static PriorityBindingExpression CreateBindingExpression(DependencyObject d, DependencyProperty dp, PriorityBinding binding, BindingExpressionBase owner)
    {
        FrameworkPropertyMetadata fwMetaData = dp.GetMetadata(d.DependencyObjectType) as FrameworkPropertyMetadata;

        if ((fwMetaData != null && !fwMetaData.IsDataBindingAllowed) || dp.ReadOnly)
            throw new ArgumentException(SR.Get(SRID.PropertyNotBindable, dp.Name), "dp");

        // create the BindingExpression
        PriorityBindingExpression bindExpr = new PriorityBindingExpression(binding, owner);

        return bindExpr;
    }

    //------------------------------------------------------
    //
    //  Protected Internal Properties
    //
    //------------------------------------------------------

    /// <summary>
    ///     Number of BindingExpressions that have been attached and are listening
    /// </summary>
    internal int AttentiveBindingExpressions
    {
        get { return (_activeIndex == NoActiveBindingExpressions) ? MutableBindingExpressions.Count : _activeIndex + 1; }
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

        SetStatus(BindingStatusInternal.Active);

        int count = ParentPriorityBinding.Bindings.Count;
        _activeIndex = NoActiveBindingExpressions;
        Debug.Assert(MutableBindingExpressions.Count == 0, "expect to encounter empty BindingExpression collection when attaching MultiBinding");
        for (int i = 0; i < count; ++i)
        {
            AttachBindingExpression(i, false);   // create new binding and have it added to end
        }

        return true;
    }

    /// <summary> sever all connections </summary>
    internal override void DetachOverride()
    {
        // Theoretically, we only need to detach number of AttentiveBindings,
        // but we'll traverse the whole list anyway and do aggressive clean-up.
        int count = MutableBindingExpressions.Count;
        for (int i = 0; i < count; ++i)
        {
            BindingExpressionBase b = MutableBindingExpressions[i];
            if (b != null)
                b.Detach();
        }

        ChangeSources(null);

        base.DetachOverride();
    }

    /// <summary>
    /// Invalidate the given child expression.
    /// </summary>
    internal override void InvalidateChild(BindingExpressionBase bindingExpression)
    {
        // Prevent re-entrancy, because ChooseActiveBindingExpression() may
        // activate/deactivate a BindingExpression that indirectly calls this again.
        if (_isInInvalidateBinding)
            return;
        _isInInvalidateBinding = true;

        int index = MutableBindingExpressions.IndexOf(bindingExpression);
        DependencyObject target = TargetElement;

        if (target != null && 0 <= index && index < AttentiveBindingExpressions)
        {
            // Optimization: only look for new ActiveBindingExpression when necessary:
            // 1. it is a higher priority BindingExpression (or there's no ActiveBindingExpression), or
            // 2. the existing ActiveBindingExpression is broken
            if (    index != _activeIndex
                ||  (bindingExpression.StatusInternal != BindingStatusInternal.Active && !bindingExpression.UsingFallbackValue))
            {
                ChooseActiveBindingExpression(target);
            }

            // update the value
            UsingFallbackValue = false;
            BindingExpressionBase bindExpr = ActiveBindingExpression;
            object newValue = (bindExpr != null) ? bindExpr.GetValue(target, TargetProperty) : UseFallbackValue();
            ChangeValue(newValue, true);

            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Transfer))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.PriorityTransfer(
                                        TraceData.Identify(this),
                                        TraceData.Identify(newValue),
                                        _activeIndex,
                                        TraceData.Identify(bindExpr)),
                                    this);
            }

            // don't invalidate during Attach.  The property engine does it
            // already, and it would interfere with the on-demand activation
            // of style-defined BindingExpressions.
            if (!IsAttaching)
            {
                // recompute expression
                target.InvalidateProperty(TargetProperty);
            }
        }

        _isInInvalidateBinding = false;
    }

    /// <summary>
    /// Change the dependency sources for the given child expression.
    /// </summary>
    internal override void ChangeSourcesForChild(BindingExpressionBase bindingExpression, WeakDependencySource[] newSources)
    {
        int index = MutableBindingExpressions.IndexOf(bindingExpression);

        if (index >= 0)
        {
            WeakDependencySource[] combinedSources = CombineSources(index, MutableBindingExpressions, AttentiveBindingExpressions, newSources);
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
            // clean up the old BindingExpression
            bindingExpression.Detach();

            // create a replacement BindingExpression and put it in the collection
            bindingExpression = AttachBindingExpression(index, true);
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

    internal override bool ShouldReactToDirtyOverride()
    {
        // react only if the active binding should react
        BindingExpressionBase active = ActiveBindingExpression;
        return (active != null) && active.ShouldReactToDirtyOverride();
    }

    /// <summary>
    /// Get the raw proposed value
    /// <summary>
    internal override object GetRawProposedValue()
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.GetRawProposedValue();
        }
        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Get the converted proposed value
    /// <summary>
    internal override object ConvertProposedValue(object rawValue)
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.ConvertProposedValue(rawValue);
        }
        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Get the converted proposed value and inform the binding group
    /// <summary>
    internal override bool ObtainConvertedProposedValue(BindingGroup bindingGroup)
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.ObtainConvertedProposedValue(bindingGroup);
        }

        return true;
    }

    /// <summary>
    /// Update the source value
    /// <summary>
    internal override object UpdateSource(object convertedValue)
    {
        object result;
        BindingExpressionBase bindExpr = ActiveBindingExpression;

        if (bindExpr != null)
        {
            result = bindExpr.UpdateSource(convertedValue);

            if (bindExpr.StatusInternal == BindingStatusInternal.UpdateSourceError)
            {
                SetStatus(BindingStatusInternal.UpdateSourceError);
            }
        }
        else
        {
            result = DependencyProperty.UnsetValue;
        }

        return result;
    }

    /// <summary>
    /// Update the source value and inform the binding group
    /// <summary>
    internal override bool UpdateSource(BindingGroup bindingGroup)
    {
        bool result = true;
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            result = bindExpr.UpdateSource(bindingGroup);

            if (bindExpr.StatusInternal == BindingStatusInternal.UpdateSourceError)
            {
                SetStatus(BindingStatusInternal.UpdateSourceError);
            }
        }
        return result;
    }

    /// <summary>
    /// Store the value in the binding group
    /// </summary>
    internal override void StoreValueInBindingGroup(object value, BindingGroup bindingGroup)
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            bindExpr.StoreValueInBindingGroup(value, bindingGroup);
        }
    }

    /// <summary>
    /// Run validation rules for the given step
    /// <summary>
    internal override bool Validate(object value, ValidationStep validationStep)
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.Validate(value, validationStep);
        }
        return true;
    }

    /// <summary>
    /// Run validation rules for the given step, and inform the binding group
    /// <summary>
    internal override bool CheckValidationRules(BindingGroup bindingGroup, ValidationStep validationStep)
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.CheckValidationRules(bindingGroup, validationStep);
        }
        return true;
    }

    /// <summary>
    /// Get the proposed value(s) that would be written to the source(s), applying
    /// conversion and checking UI-side validation rules.
    /// </summary>
    internal override bool ValidateAndConvertProposedValue(out Collection<ProposedValue> values)
    {
        Debug.Assert(NeedsValidation, "check NeedsValidation before calling this");

        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.ValidateAndConvertProposedValue(out values);
        }

        values = null;
        return true;
    }

    // Return the object from which the given value was obtained, if possible
    internal override object GetSourceItem(object newValue)
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            return bindExpr.GetSourceItem(newValue);
        }
        return true;
    }

    internal override void UpdateCommitState()
    {
        BindingExpressionBase bindExpr = ActiveBindingExpression;
        if (bindExpr != null)
        {
            AdoptProperties(bindExpr);
        }
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

        BindingBase binding = ParentPriorityBinding.Bindings[i];

        BindingExpressionBase bindExpr = binding.CreateBindingExpression(target, TargetProperty, this);
        if (replaceExisting) // replace exisiting or add as new binding?
            MutableBindingExpressions[i] = bindExpr;
        else
            MutableBindingExpressions.Add(bindExpr);

        bindExpr.Attach(target, TargetProperty);
        return bindExpr;
    }

    // Re-evaluate the choice of active BindingExpression
    void ChooseActiveBindingExpression(DependencyObject target)
    {
        int i, count = MutableBindingExpressions.Count;
        for (i = 0; i < count; ++i)
        {
            BindingExpressionBase bindExpr = MutableBindingExpressions[i];

            // Try to activate the BindingExpression if it isn't already activate
            if (bindExpr.StatusInternal == BindingStatusInternal.Inactive)
                bindExpr.Activate();

            if (bindExpr.StatusInternal == BindingStatusInternal.Active || bindExpr.UsingFallbackValue)
                break;
        }

        int newActiveIndex = (i < count) ? i : NoActiveBindingExpressions;

        // if active changes, adjust state
        if (newActiveIndex != _activeIndex)
        {
            int oldActiveIndex = _activeIndex;
            _activeIndex = newActiveIndex;

            // adopt the properties of the active binding
            AdoptProperties(ActiveBindingExpression);

            // tell the property engine the new list of sources
            WeakDependencySource[] newSources = CombineSources(-1, MutableBindingExpressions, AttentiveBindingExpressions, null);
            ChangeSources(newSources);

            // deactivate BindingExpressions that don't need to be attentive
            // BUG: only those with (StatusInternal == BindingStatusInternal.Active) will be deactivated
            if (newActiveIndex != NoActiveBindingExpressions)
                for (i = oldActiveIndex; i > newActiveIndex; --i)
                    MutableBindingExpressions[i].Deactivate();
        }
    }

    private void ChangeValue()
    {
    }

    internal override void HandlePropertyInvalidation(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        DependencyProperty dp = args.Property;

        if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
        {
            TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                TraceData.GotPropertyChanged(
                                    TraceData.Identify(this),
                                    TraceData.Identify(d),
                                    dp.Name),
                                this);
        }

        for (int i=0; i<AttentiveBindingExpressions; ++i)
        {
            BindingExpressionBase bindExpr = MutableBindingExpressions[i];
            DependencySource[] sources = bindExpr.GetSources();
            if (sources != null)
            {
                for (int j=0; j<sources.Length; ++j)
                {
                    DependencySource source = sources[j];
                    if (source.DependencyObject == d && source.DependencyProperty == dp)
                    {
                        bindExpr.OnPropertyInvalidation(d, args);
                        break;
                    }
                }
            }
        }
    }


    //------------------------------------------------------
    //
    //  Private Fields
    //
    //------------------------------------------------------

    const int   NoActiveBindingExpressions        = -1;  // no BindingExpressions work, all are attentive
    const int   UnknownActiveBindingExpression    = -2;  // need to determine active BindingExpression

    Collection<BindingExpressionBase>  _list = new Collection<BindingExpressionBase>();
    int         _activeIndex            = UnknownActiveBindingExpression;
    bool        _isInInvalidateBinding  = false;
}
}
