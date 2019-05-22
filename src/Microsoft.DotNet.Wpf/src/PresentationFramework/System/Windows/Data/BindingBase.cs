// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines BindingBase object, common base class for Binding,
//              PriorityBinding, MultiBinding.
//
// See spec at Data Binding.mht
//

using System;
using System.Collections.ObjectModel;   // Collection<T>
using System.ComponentModel;    // [DefaultValue]
using System.Diagnostics;       // Debug.Assert
using System.Globalization;     // CultureInfo
using System.Windows.Markup;    // MarkupExtension
using System.Windows.Controls;  // ValidationRule
using MS.Internal;              // Helper


namespace System.Windows.Data
{
    /// <summary> This enum describes how the data flows through a given Binding
    /// </summary>
    public enum BindingMode
    {
        /// <summary> Data flows from source to target and vice-versa </summary>
        TwoWay,
        /// <summary> Data flows from source to target, source changes cause data flow </summary>
        OneWay,
        /// <summary> Data flows from source to target once, source changes are ignored </summary>
        OneTime,
        /// <summary> Data flows from target to source, target changes cause data flow </summary>
        OneWayToSource,
        /// <summary> Data flow is obtained from target property default </summary>
        Default
    }

    /// <summary> This enum describes when updates (target-to-source data flow)
    /// happen in a given Binding.
    /// </summary>
    public enum UpdateSourceTrigger
    {
        /// <summary> Obtain trigger from target property default </summary>
        Default,
        /// <summary> Update whenever the target property changes </summary>
        PropertyChanged,
        /// <summary> Update only when target element loses focus, or when Binding deactivates </summary>
        LostFocus,
        /// <summary> Update only by explicit call to BindingExpression.UpdateSource() </summary>
        Explicit
    }

    /// <summary>
    /// Base class for Binding, PriorityBinding, and MultiBinding.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    [Localizability(LocalizationCategory.None, Modifiability = Modifiability.Unmodifiable, Readability = Readability.Unreadable)] // Not localizable by-default
    public abstract class BindingBase: MarkupExtension
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BindingBase()
        {
            Debug.Assert((int)Feature.LastFeatureId <= 32, "UncommonValueTable supports only 32 Ids");
        }

        /// <summary> Initialize a new instance of BindingBase. </summary>
        /// <remarks>
        /// This constructor can only be called by one of the built-in
        /// derived classes.
        /// </remarks>
        internal BindingBase()
        {
        }

        #endregion Constructors

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary> Value to use when source cannot provide a value </summary>
        /// <remarks>
        ///     Initialized to DependencyProperty.UnsetValue; if FallbackValue is not set, BindingExpression
        ///     will return target property's default when Binding cannot get a real value.
        /// </remarks>
        public object FallbackValue
        {
            get { return GetValue(Feature.FallbackValue, DependencyProperty.UnsetValue); }
            set { CheckSealed(); SetValue(Feature.FallbackValue, value); }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeFallbackValue()
        {
            return HasValue(Feature.FallbackValue);
        }

        /// <summary> Format string used to convert the data to type String.
        /// </summary>
        /// <remarks>
        ///     This property is used when the target of the binding has type
        ///     String and no Converter is declared.  It is ignored in all other
        ///     cases.
        /// </remarks>
        [System.ComponentModel.DefaultValue(null)]
        public string StringFormat
        {
            get { return (string)GetValue(Feature.StringFormat, null); }
            set { CheckSealed(); SetValue(Feature.StringFormat, value, null); }
        }

        /// <summary> Value used to represent "null" in the target property.
        /// </summary>
        public object TargetNullValue
        {
            get { return GetValue(Feature.TargetNullValue, DependencyProperty.UnsetValue); }
            set { CheckSealed(); SetValue(Feature.TargetNullValue, value); }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeTargetNullValue()
        {
            return HasValue(Feature.TargetNullValue);
        }

        /// <summary> Name of the <see cref="BindingGroup"/> this binding should join.
        /// </summary>
        [DefaultValue("")]
        public string BindingGroupName
        {
            get { return (string)GetValue(Feature.BindingGroupName, String.Empty); }
            set { CheckSealed(); SetValue(Feature.BindingGroupName, value, String.Empty); }
        }

        /// <summary>
        /// The time (in milliseconds) to wait after the most recent property
        /// change before performing source update.
        /// </summary>
        /// <remarks>
        /// This property affects only TwoWay bindings with UpdateSourceTrigger=PropertyChanged.
        /// </remarks>
        [DefaultValue(0)]
        public int Delay
        {
            get { return (int)GetValue(Feature.Delay, 0); }
            set { CheckSealed(); SetValue(Feature.Delay, value, 0); }
        }

        #endregion Public Properties

        #region Public Methods

        //------------------------------------------------------
        //
        //  MarkupExtension overrides
        //
        //------------------------------------------------------

        /// <summary>
        /// Return the value to set on the property for the target for this
        /// binding.
        /// </summary>
        public sealed override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Binding a property value only works on DependencyObject and DependencyProperties.
            // For all other cases, just return this Binding object as the value.

            if (serviceProvider == null)
            {
                return this;
            }

            // Bindings are not allowed On CLR props except for Setter,Trigger,Condition (bugs 1183373,1572537)

            DependencyObject targetDependencyObject;
            DependencyProperty targetDependencyProperty;
            Helper.CheckCanReceiveMarkupExtension(this, serviceProvider, out targetDependencyObject, out targetDependencyProperty);

            if (targetDependencyObject == null || targetDependencyProperty == null)
            {
                return this;
            }

            // delegate real work to subclass
            return CreateBindingExpression(targetDependencyObject, targetDependencyProperty);
        }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #endregion Public Methods

        #region Protected Enums

        //------------------------------------------------------
        //
        //  Protected Enums
        //
        //------------------------------------------------------

        /// <summary> Flags indicating special properties of a Binding. </summary>
        [Flags]
        internal enum BindingFlags : uint
        {
            /// <summary> Data flows from source to target (only) </summary>
            OneWay = BindingExpressionBase.BindingFlags.OneWay,
            /// <summary> Data flows in both directions - source to target and vice-versa </summary>
            TwoWay = BindingExpressionBase.BindingFlags.TwoWay,
            /// <summary> Data flows from target to source (only) </summary>
            OneWayToSource = BindingExpressionBase.BindingFlags.OneWayToSource,
            /// <summary> Target is initialized from the source (only) </summary>
            OneTime = BindingExpressionBase.BindingFlags.OneTime,
            /// <summary> Data flow obtained from target property default </summary>
            PropDefault = BindingExpressionBase.BindingFlags.PropDefault,

            /// <summary> Raise TargetUpdated event whenever a value flows from source to target </summary>
            NotifyOnTargetUpdated = BindingExpressionBase.BindingFlags.NotifyOnTargetUpdated,
            /// <summary> Raise SourceUpdated event whenever a value flows from target to source </summary>
            NotifyOnSourceUpdated = BindingExpressionBase.BindingFlags.NotifyOnSourceUpdated,
            /// <summary> Raise ValidationError event whenever there is a ValidationError on Update</summary>
            NotifyOnValidationError = BindingExpressionBase.BindingFlags.NotifyOnValidationError,

            /// <summary> Obtain trigger from target property default </summary>
            UpdateDefault = BindingExpressionBase.BindingFlags.UpdateDefault,
            /// <summary> Update the source value whenever the target value changes </summary>
            UpdateOnPropertyChanged = BindingExpressionBase.BindingFlags.UpdateOnPropertyChanged,
            /// <summary> Update the source value whenever the target element loses focus </summary>
            UpdateOnLostFocus = BindingExpressionBase.BindingFlags.UpdateOnLostFocus,
            /// <summary> Update the source value only when explicitly told to do so </summary>
            UpdateExplicitly = BindingExpressionBase.BindingFlags.UpdateExplicitly,

            /// <summary>
            /// Used to determine whether the Path was internally Generated (such as the implicit
            /// /InnerText from an XPath).  If it is, then it doesn't need to be serialized.
            /// </summary>
            PathGeneratedInternally = BindingExpressionBase.BindingFlags.PathGeneratedInternally,

            ValidatesOnExceptions   = BindingExpressionBase.BindingFlags.ValidatesOnExceptions,
            ValidatesOnDataErrors   = BindingExpressionBase.BindingFlags.ValidatesOnDataErrors,
            ValidatesOnNotifyDataErrors   = BindingExpressionBase.BindingFlags.ValidatesOnNotifyDataErrors,

            /// <summary> Flags describing data transfer </summary>
            PropagationMask = OneWay | TwoWay | OneWayToSource | OneTime | PropDefault,

            /// <summary> Flags describing update trigger </summary>
            UpdateMask      = UpdateDefault | UpdateOnPropertyChanged | UpdateOnLostFocus | UpdateExplicitly,

            /// <summary> Default value</summary>
            Default = BindingExpressionBase.BindingFlags.Default | ValidatesOnNotifyDataErrors,

            /// <summary> Error value, returned by FlagsFrom to indicate faulty input</summary>
            IllegalInput = BindingExpressionBase.BindingFlags.IllegalInput,
        }

        #endregion Protected Enums

        #region Protected Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Create an appropriate expression for this Binding, to be attached
        /// to the given DependencyProperty on the given DependencyObject.
        /// </summary>
        internal abstract BindingExpressionBase CreateBindingExpressionOverride(DependencyObject targetObject, DependencyProperty targetProperty, BindingExpressionBase owner);

        /// <summary> Return true if any of the given flags are set. </summary>
        internal bool TestFlag(BindingFlags flag)
        {
            return (_flags & flag) != 0;
        }

        /// <summary> Set the given flags. </summary>
        internal void SetFlag(BindingFlags flag)
        {
            _flags |= flag;
        }

        /// <summary> Clear the given flags. </summary>
        internal void ClearFlag(BindingFlags flag)
        {
            _flags &= ~flag;
        }

        /// <summary> Change the given flags to have the given value. </summary>
        internal void ChangeFlag(BindingFlags flag, bool value)
        {
            if (value)
                _flags |=  flag;
            else
                _flags &= ~flag;
        }

        /// <summary> Get the flags within the given mas. </summary>
        internal BindingFlags GetFlagsWithinMask(BindingFlags mask)
        {
            return (_flags & mask);
        }

        /// <summary> Change the flags within the given mask to have the given value. </summary>
        internal void ChangeFlagsWithinMask(BindingFlags mask, BindingFlags flags)
        {
            _flags = (_flags & ~mask) | (flags & mask);
        }

        /// <summary> Convert the given BindingMode to BindingFlags. </summary>
        internal static BindingFlags FlagsFrom(BindingMode bindingMode)
        {
            switch (bindingMode)
            {
                case BindingMode.OneWay:            return BindingFlags.OneWay;
                case BindingMode.TwoWay:            return BindingFlags.TwoWay;
                case BindingMode.OneWayToSource:    return BindingFlags.OneWayToSource;
                case BindingMode.OneTime:           return BindingFlags.OneTime;
                case BindingMode.Default:           return BindingFlags.PropDefault;
            }

            return BindingFlags.IllegalInput;
        }

        /// <summary> Convert the given UpdateSourceTrigger to BindingFlags. </summary>
        internal static BindingFlags FlagsFrom(UpdateSourceTrigger updateSourceTrigger)
        {
            switch (updateSourceTrigger)
            {
                case UpdateSourceTrigger.Default:           return BindingFlags.UpdateDefault;
                case UpdateSourceTrigger.PropertyChanged:   return BindingFlags.UpdateOnPropertyChanged;
                case UpdateSourceTrigger.LostFocus:         return BindingFlags.UpdateOnLostFocus;
                case UpdateSourceTrigger.Explicit:          return BindingFlags.UpdateExplicitly;
            }

            return BindingFlags.IllegalInput;
        }

        #endregion Protected Methods

        #region Internal Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        internal BindingFlags Flags { get { return _flags; } }

        internal virtual CultureInfo ConverterCultureInternal
        {
            get { return null; }
        }

        internal virtual Collection<ValidationRule> ValidationRulesInternal
        {
            get { return null; }
        }

        internal virtual bool ValidatesOnNotifyDataErrorsInternal
        {
            get { return false; }
        }

        #endregion Internal Properties

        #region Internal Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Create an appropriate expression for this Binding, to be attached
        /// to the given DependencyProperty on the given DependencyObject.
        /// </summary>
        internal BindingExpressionBase CreateBindingExpression(DependencyObject targetObject, DependencyProperty targetProperty)
        {
            _isSealed = true;
            return CreateBindingExpressionOverride(targetObject, targetProperty, null);
        }

        /// <summary>
        /// Create an appropriate expression for this Binding, to be attached
        /// to the given DependencyProperty on the given DependencyObject.
        /// </summary>
        internal BindingExpressionBase CreateBindingExpression(DependencyObject targetObject, DependencyProperty targetProperty, BindingExpressionBase owner)
        {
            _isSealed = true;
            return CreateBindingExpressionOverride(targetObject, targetProperty, owner);
        }

        // Throw if the binding is sealed.
        internal void CheckSealed()
        {
            if (_isSealed)
                throw new InvalidOperationException(SR.Get(SRID.ChangeSealedBinding));
        }

        // Return one of the special ValidationRules
        internal ValidationRule GetValidationRule(Type type)
        {
            if (TestFlag(BindingFlags.ValidatesOnExceptions) && type == typeof(System.Windows.Controls.ExceptionValidationRule))
                return System.Windows.Controls.ExceptionValidationRule.Instance;

            if (TestFlag(BindingFlags.ValidatesOnDataErrors) && type == typeof(System.Windows.Controls.DataErrorValidationRule))
                return System.Windows.Controls.DataErrorValidationRule.Instance;

            if (TestFlag(BindingFlags.ValidatesOnNotifyDataErrors) && type == typeof(System.Windows.Controls.NotifyDataErrorValidationRule))
                return System.Windows.Controls.NotifyDataErrorValidationRule.Instance;

            return LookupValidationRule(type);
        }

        internal virtual ValidationRule LookupValidationRule(Type type)
        {
            return null;
        }

        internal static ValidationRule LookupValidationRule(Type type, Collection<ValidationRule> collection)
        {
            if (collection == null)
                return null;

            for (int i=0; i<collection.Count; ++i)
            {
                if (type.IsInstanceOfType(collection[i]))
                    return collection[i];
            }

            return null;
        }

        // return a copy of the current binding, but with the given mode
        internal BindingBase Clone(BindingMode mode)
        {
            BindingBase clone = this.CreateClone();
            InitializeClone(clone, mode);
            return clone;
        }

        // initialize a clone
        internal virtual void InitializeClone(BindingBase clone, BindingMode mode)
        {
            clone._flags = _flags;
            CopyValue(Feature.FallbackValue, clone);
            clone._isSealed = _isSealed;
            CopyValue(Feature.StringFormat, clone);
            CopyValue(Feature.TargetNullValue, clone);
            CopyValue(Feature.BindingGroupName, clone);

            clone.ChangeFlagsWithinMask(BindingFlags.PropagationMask, FlagsFrom(mode));
        }

        internal abstract BindingBase CreateClone();

        #endregion Internal Methods

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        BindingFlags    _flags = BindingFlags.Default;
        bool            _isSealed;

        #endregion Private Fields

        #region Uncommon Values

        internal enum Feature
        {
            // BindingBase
            FallbackValue,
            StringFormat,
            TargetNullValue,
            BindingGroupName,
            Delay,

            // Binding
            XPath,
            Culture,
            AsyncState,
            ObjectSource,
            RelativeSource,
            ElementSource,
            Converter,
            ConverterParameter,
            ValidationRules,
            ExceptionFilterCallback,
            AttachedPropertiesInPath,

            // MultiBinding

            // PriorityBinding

            // Sentinel, for error checking.   Must be last.
            LastFeatureId
        }

        internal bool      HasValue(Feature id) { return _values.HasValue((int)id); }
        internal object    GetValue(Feature id, object defaultValue) { return _values.GetValue((int)id, defaultValue); }
        internal void      SetValue(Feature id, object value) { _values.SetValue((int)id, value); }
        internal void      SetValue(Feature id, object value, object defaultValue) { if (Object.Equals(value, defaultValue)) _values.ClearValue((int)id); else _values.SetValue((int)id, value); }
        internal void      ClearValue(Feature id) { _values.ClearValue((int)id); }
        internal void      CopyValue(Feature id, BindingBase clone) { if (HasValue(id)) { clone.SetValue(id, GetValue(id, null)); } }
        UncommonValueTable  _values;

        #endregion Uncommon Values
    }
}
