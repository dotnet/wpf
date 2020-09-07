// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines MultiBinding object, uses a collection of bindings together.
//
// Specs:       UIBinding.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;  // Validation
using System.Windows.Markup;
using MS.Internal.Controls; // Validation
using MS.Internal.Data;
using MS.Utility;

namespace System.Windows.Data
{
/// <summary>
///  Describes a collection of bindings attached to a single property.
///     The inner bindings contribute their values to the MultiBinding,
///     which combines/converts them into a resultant final value.
///     In the reverse direction, the target value is tranlated to
///     a set of values that are fed back into the inner bindings.
/// </summary>
[ContentProperty("Bindings")]
public class MultiBinding : BindingBase, IAddChild
{
    //------------------------------------------------------
    //
    //  Constructors
    //
    //------------------------------------------------------

    /// <summary> Default constructor </summary>
    public MultiBinding()
    {
        _bindingCollection = new BindingCollection(this, new BindingCollectionChangedCallback(OnBindingCollectionChanged));
    }

#region IAddChild

    ///<summary>
    /// Called to Add the object as a Child.
    ///</summary>
    ///<param name="value">
    /// Object to add as a child - must have type BindingBase
    ///</param>
    void IAddChild.AddChild(Object value)
    {
        BindingBase binding = value as BindingBase;
        if (binding != null)
            Bindings.Add(binding);
        else
            throw new ArgumentException(SR.Get(SRID.ChildHasWrongType, this.GetType().Name, "BindingBase", value.GetType().FullName), "value");
    }

    ///<summary>
    /// Called when text appears under the tag in markup
    ///</summary>
    ///<param name="text">
    /// Text to Add to the Object
    ///</param>
    void IAddChild.AddText(string text)
    {
        XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
    }

#endregion IAddChild

    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    /// <summary> List of inner bindings </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Collection<BindingBase> Bindings
    {
        get { return _bindingCollection; }
    }

    /// <summary>
    /// This method is used by TypeDescriptor to determine if this property should
    /// be serialized.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeBindings()
    {
        return (Bindings != null && Bindings.Count > 0);
    }

    /// <summary> Binding type </summary>
    [DefaultValue(BindingMode.Default)]
    public BindingMode Mode
    {
        get
        {
            switch (GetFlagsWithinMask(BindingFlags.PropagationMask))
            {
                case BindingFlags.OneWay:           return BindingMode.OneWay;
                case BindingFlags.TwoWay:           return BindingMode.TwoWay;
                case BindingFlags.OneWayToSource:   return BindingMode.OneWayToSource;
                case BindingFlags.OneTime:          return BindingMode.OneTime;
                case BindingFlags.PropDefault:      return BindingMode.Default;
            }
            Debug.Assert(false, "Unexpected BindingMode value");
            return 0;
        }
        set
        {
            CheckSealed();
            ChangeFlagsWithinMask(BindingFlags.PropagationMask, FlagsFrom(value));
        }
    }

    /// <summary> Update type </summary>
    [DefaultValue(UpdateSourceTrigger.PropertyChanged)]
    public UpdateSourceTrigger UpdateSourceTrigger
    {
        get
        {
            switch (GetFlagsWithinMask(BindingFlags.UpdateMask))
            {
                case BindingFlags.UpdateOnPropertyChanged:    return UpdateSourceTrigger.PropertyChanged;
                case BindingFlags.UpdateOnLostFocus:    return UpdateSourceTrigger.LostFocus;
                case BindingFlags.UpdateExplicitly:     return UpdateSourceTrigger.Explicit;
                case BindingFlags.UpdateDefault:        return UpdateSourceTrigger.Default;
            }
            Debug.Assert(false, "Unexpected UpdateSourceTrigger value");
            return 0;
        }
        set
        {
            CheckSealed();
            ChangeFlagsWithinMask(BindingFlags.UpdateMask, FlagsFrom(value));
        }
    }


    /// <summary> Raise SourceUpdated event whenever a value flows from target to source </summary>
    [DefaultValue(false)]
    public bool NotifyOnSourceUpdated
    {
        get
        {
            return TestFlag(BindingFlags.NotifyOnSourceUpdated);
        }
        set
        {
            bool currentValue = TestFlag(BindingFlags.NotifyOnSourceUpdated);
            if (currentValue != value)
            {
                CheckSealed();
                ChangeFlag(BindingFlags.NotifyOnSourceUpdated, value);
            }
        }
    }


    /// <summary> Raise TargetUpdated event whenever a value flows from source to target </summary>
    [DefaultValue(false)]
    public bool NotifyOnTargetUpdated
    {
        get
        {
            return TestFlag(BindingFlags.NotifyOnTargetUpdated);
        }
        set
        {
            bool currentValue = TestFlag(BindingFlags.NotifyOnTargetUpdated);
            if (currentValue != value)
            {
                CheckSealed();
                ChangeFlag(BindingFlags.NotifyOnTargetUpdated, value);
            }
        }
    }

    /// <summary> Raise ValidationError event whenever there is a ValidationError on Update</summary>
    [DefaultValue(false)]
    public bool NotifyOnValidationError
    {
        get
        {
            return TestFlag(BindingFlags.NotifyOnValidationError);
        }
        set
        {
            bool currentValue = TestFlag(BindingFlags.NotifyOnValidationError);
            if (currentValue != value)
            {
                CheckSealed();
                ChangeFlag(BindingFlags.NotifyOnValidationError, value);
            }
        }
    }

    /// <summary> Converter to convert the source values to/from the target value</summary>
    [DefaultValue(null)]
    public IMultiValueConverter Converter
    {
        get { return (IMultiValueConverter)GetValue(Feature.Converter, null); }
        set { CheckSealed(); SetValue(Feature.Converter, value, null); }
    }

    /// <summary>
    /// The parameter to pass to converter.
    /// </summary>
    /// <value></value>
    [DefaultValue(null)]
    public object ConverterParameter
    {
        get { return GetValue(Feature.ConverterParameter, null); }
        set { CheckSealed(); SetValue(Feature.ConverterParameter, value, null); }
    }

    /// <summary> Culture in which to evaluate the converter </summary>
    [DefaultValue(null)]
    [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
    public CultureInfo ConverterCulture
    {
        get { return (CultureInfo)GetValue(Feature.Culture, null); }
        set { CheckSealed(); SetValue(Feature.Culture, value, null); }
    }

    /// <summary>
    ///     Collection&lt;ValidationRule&gt; is a collection of ValidationRule
    ///     instances on either a Binding or a MultiBinding.  Each of the rules
    ///     is checked for validity on update
    /// </summary>
    public Collection<ValidationRule> ValidationRules
    {
        get
        {
            if (!HasValue(Feature.ValidationRules))
                SetValue(Feature.ValidationRules, new ValidationRuleCollection());

            return (ValidationRuleCollection)GetValue(Feature.ValidationRules, null);
        }
    }

    /// <summary>
    /// This method is used by TypeDescriptor to determine if this property should
    /// be serialized.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeValidationRules()
    {
        return (HasValue(Feature.ValidationRules) && ValidationRules.Count > 0);
    }


    /// <summary>
    /// called whenever any exception is encountered when trying to update
    /// the value to the source. The application author can provide its own
    /// handler for handling exceptions here. If the delegate returns
    ///     null - don’t throw an error or provide a ValidationError.
    ///     Exception - returns the exception itself, we will fire the exception using Async exception model.
    ///     ValidationError - it will set itself as the BindingInError and add it to the element’s Validation errors.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter
    {
        get { return (UpdateSourceExceptionFilterCallback)GetValue(Feature.ExceptionFilterCallback, null); }
        set { SetValue(Feature.ExceptionFilterCallback, value, null); }
    }

    /// <summary> True if an exception during source updates should be considered a validation error.</summary>
    [DefaultValue(false)]
    public bool ValidatesOnExceptions
    {
        get
        {
            return TestFlag(BindingFlags.ValidatesOnExceptions);
        }
        set
        {
            bool currentValue = TestFlag(BindingFlags.ValidatesOnExceptions);
            if (currentValue != value)
            {
                CheckSealed();
                ChangeFlag(BindingFlags.ValidatesOnExceptions, value);
            }
        }
    }

    /// <summary> True if a data error in the source item should be considered a validation error.</summary>
    [DefaultValue(false)]
    public bool ValidatesOnDataErrors
    {
        get
        {
            return TestFlag(BindingFlags.ValidatesOnDataErrors);
        }
        set
        {
            bool currentValue = TestFlag(BindingFlags.ValidatesOnDataErrors);
            if (currentValue != value)
            {
                CheckSealed();
                ChangeFlag(BindingFlags.ValidatesOnDataErrors, value);
            }
        }
    }

    /// <summary> True if a data error from INotifyDataErrorInfo source item should be considered a validation error.</summary>
    [DefaultValue(true)]
    public bool ValidatesOnNotifyDataErrors
    {
        get
        {
            return TestFlag(BindingFlags.ValidatesOnNotifyDataErrors);
        }
        set
        {
            bool currentValue = TestFlag(BindingFlags.ValidatesOnNotifyDataErrors);
            if (currentValue != value)
            {
                CheckSealed();
                ChangeFlag(BindingFlags.ValidatesOnNotifyDataErrors, value);
            }
        }
    }

    //------------------------------------------------------
    //
    //  Protected Methods
    //
    //------------------------------------------------------

    /// <summary>
    /// Create an appropriate expression for this Binding, to be attached
    /// to the given DependencyProperty on the given DependencyObject.
    /// </summary>
    internal override BindingExpressionBase CreateBindingExpressionOverride(DependencyObject target, DependencyProperty dp, BindingExpressionBase owner)
    {
        if (Converter == null && String.IsNullOrEmpty(StringFormat))
            throw new InvalidOperationException(SR.Get(SRID.MultiBindingHasNoConverter));

        for (int i = 0; i < Bindings.Count; ++i)
        {
            CheckTrigger(Bindings[i]);
        }

        return MultiBindingExpression.CreateBindingExpression(target, dp, this, owner);
    }

    internal override ValidationRule LookupValidationRule(Type type)
    {
        return LookupValidationRule(type, ValidationRulesInternal);
    }

    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    internal object DoFilterException(object bindExpr, Exception exception)
    {
        UpdateSourceExceptionFilterCallback callback = (UpdateSourceExceptionFilterCallback)GetValue(Feature.ExceptionFilterCallback, null);
        if (callback != null)
            return callback(bindExpr, exception);

        return exception;
    }

    internal static void CheckTrigger(BindingBase bb)
    {
        Binding binding = bb as Binding;
        if (binding != null)
        {
            if (binding.UpdateSourceTrigger != UpdateSourceTrigger.PropertyChanged &&
                binding.UpdateSourceTrigger != UpdateSourceTrigger.Default)
                throw new InvalidOperationException(SR.Get(SRID.NoUpdateSourceTriggerForInnerBindingOfMultiBinding));
        }
    }

    internal override BindingBase CreateClone()
    {
        return new MultiBinding();
    }

    internal override void InitializeClone(BindingBase baseClone, BindingMode mode)
    {
        MultiBinding clone = (MultiBinding)baseClone;

        CopyValue(Feature.Converter, clone);
        CopyValue(Feature.ConverterParameter, clone);
        CopyValue(Feature.Culture, clone);
        CopyValue(Feature.ValidationRules, clone);
        CopyValue(Feature.ExceptionFilterCallback, clone);

        for (int i=0; i<_bindingCollection.Count; ++i)
        {
            clone._bindingCollection.Add(_bindingCollection[i].Clone(mode));
        }

        base.InitializeClone(baseClone, mode);
    }

    //------------------------------------------------------
    //
    //  Internal Properties
    //
    //------------------------------------------------------

    // same as the public ValidationRules property, but
    // doesn't try to create an instance if there isn't one there
    internal override Collection<ValidationRule> ValidationRulesInternal
    {
        get
        {
            return (ValidationRuleCollection)GetValue(Feature.ValidationRules, null);
        }
    }

    internal override CultureInfo ConverterCultureInternal
    {
        get { return ConverterCulture; }
    }

    internal override bool ValidatesOnNotifyDataErrorsInternal
    {
        get { return ValidatesOnNotifyDataErrors; }
    }

    //------------------------------------------------------
    //
    //  Private Methods
    //
    //------------------------------------------------------

    private void OnBindingCollectionChanged()
    {
        CheckSealed();
    }

    //------------------------------------------------------
    //
    //  Private Fields
    //
    //------------------------------------------------------

    BindingCollection       _bindingCollection;
}
}
