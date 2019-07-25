// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System.IO;
using System.Windows.Markup;
using System.ComponentModel;

using System;
using System.Windows.Data;
using System.Diagnostics;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    ///     Condition for a multiple property or data trigger
    /// </summary>
    [XamlSetMarkupExtensionAttribute("ReceiveMarkupExtension")]
    [XamlSetTypeConverterAttribute("ReceiveTypeConverter")] 
    public sealed class Condition : ISupportInitialize
    {
        /// <summary>
        ///     Constructor with no property reference nor value
        /// </summary>
        public Condition()
        {
            _property = null;
            _binding = null;
        }

        /// <summary>
        ///     Constructor for creating a Condition
        /// </summary>
        public Condition( DependencyProperty conditionProperty, object conditionValue ) :
            this(conditionProperty, conditionValue, null)
        {
            // Call Forwarded
        }

        /// <summary>
        ///     Constructor for creating a Condition with the given property
        /// and value instead of creating an empty one and setting values later.
        /// </summary>
        /// <remarks>
        ///     This constructor does parameter validation, which before doesn't
        /// happen until Seal() is called.  We can do it here because we get
        /// both at the same time.
        /// </remarks>
        public Condition( DependencyProperty conditionProperty, object conditionValue, string sourceName )
        {
            if(conditionProperty == null)
            {
                throw new ArgumentNullException(nameof(conditionProperty));
            }

            if(!conditionProperty.IsValidValue(conditionValue))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPropertyValue, conditionValue, conditionProperty.Name));
            }

            _property = conditionProperty;
            Value    = conditionValue;
            _sourceName = sourceName;
        }

        /// <summary>
        ///     Constructor for creating a Condition with the given binding declaration.
        /// and value.
        /// </summary>
        public Condition( BindingBase binding, object conditionValue )
        {
            if(binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            Binding = binding;
            Value  = conditionValue;
        }

        /// <summary>
        ///     DepedencyProperty of the conditional
        /// </summary>
        [Ambient]
        [DefaultValue(null)]
        public DependencyProperty Property
        {
            get { return _property; }
            set
            {
                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Condition"));
                }

                if (_binding != null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ConditionCannotUseBothPropertyAndBinding));
                }

                _property = value;
            }
        }

        /// <summary>
        ///     Binding of the conditional
        /// </summary>
        [DefaultValue(null)]
        public BindingBase Binding
        {
            get { return _binding; }
            set
            {
                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Condition"));
                }

                if(_property != null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ConditionCannotUseBothPropertyAndBinding));
                }

                _binding = value;
            }
        }

        /// <summary>
        ///     Value of the condition (equality check)
        /// </summary>
        [TypeConverter(typeof(System.Windows.Markup.SetterTriggerConditionValueConverter))]
        public object Value
        {
            get { return _value; }
            set
            {
                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Condition"));
                }

                if (value is MarkupExtension)
                {
                    throw new ArgumentException(SR.Get(SRID.ConditionValueOfMarkupExtensionNotSupported,
                                                       value.GetType().Name));
                }

                if(value is Expression)
                {
                    throw new ArgumentException(SR.Get(SRID.ConditionValueOfExpressionNotSupported));
                }

                _value = value;
            }
        }

        /// <summary>
        /// The x:Name of the object whose property shall
        /// trigger the associated setters to be applied.
        /// If null, then this is the object being Styled
        /// and not anything under its Template Tree.
        /// </summary>
        [DefaultValue(null)]
        public string SourceName
        {
            get
            {
                return _sourceName;
            }
            set
            {
                if(_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Condition"));
                }

                _sourceName = value;
            }
        }

        /// <summary>
        ///     Seal the condition so that it can no longer be modified
        /// </summary>
        internal void Seal(ValueLookupType type)
        {
            if (_sealed)
            {
                return;
            }

            _sealed = true;

            // Ensure valid condition
            if (_property != null && _binding != null)
                throw new InvalidOperationException(SR.Get(SRID.ConditionCannotUseBothPropertyAndBinding));

            switch (type)
            {
                case ValueLookupType.Trigger:
                case ValueLookupType.PropertyTriggerResource:
                    if (_property == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.NullPropertyIllegal, "Property"));
                    }

                    if (!_property.IsValidValue(_value))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.InvalidPropertyValue, _value, _property.Name));
                    }
                    break;

                case ValueLookupType.DataTrigger:
                case ValueLookupType.DataTriggerResource:
                    if (_binding == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.NullPropertyIllegal, "Binding"));
                    }
                    break;

                default:
                    throw new InvalidOperationException(SR.Get(SRID.UnexpectedValueTypeForCondition, type));
            }

            // Freeze the condition value
            StyleHelper.SealIfSealable(_value);
        }


        #region ISupportInitialize Members

        void ISupportInitialize.BeginInit()
        {
        }

        void ISupportInitialize.EndInit()
        {
            // Resolve all properties here
            if (_unresolvedProperty != null)
            {
                try
                {
                    Property = DependencyPropertyConverter.ResolveProperty(_serviceProvider,
                        SourceName, _unresolvedProperty);
                }
                finally
                {
                    _unresolvedProperty = null;
                }
            }
            if (_unresolvedValue != null)
            {
                try
                {
                    Value = SetterTriggerConditionValueConverter.ResolveValue(_serviceProvider,
                        Property, _cultureInfoForTypeConverter, _unresolvedValue);
                }
                finally
                {
                    _unresolvedValue = null;
                }
            }
            _serviceProvider = null;
            _cultureInfoForTypeConverter = null;
        }

        #endregion

        public static void ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException(nameof(targetObject));
            }
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            Condition condition = targetObject as Condition;
            if (condition != null && eventArgs.Member.Name == "Binding" && eventArgs.MarkupExtension is BindingBase)
            {
                condition.Binding = eventArgs.MarkupExtension as BindingBase;

                eventArgs.Handled = true;
            }
        }

        public static void ReceiveTypeConverter(object targetObject, XamlSetTypeConverterEventArgs eventArgs)
        {
            Condition condition = targetObject as Condition;
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(targetObject));
            }
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            if (eventArgs.Member.Name == "Property")
            {
                condition._unresolvedProperty = eventArgs.Value;
                condition._serviceProvider = eventArgs.ServiceProvider;
                condition._cultureInfoForTypeConverter = eventArgs.CultureInfo;

                eventArgs.Handled = true;
            }
            else if (eventArgs.Member.Name == "Value")
            {
                condition._unresolvedValue = eventArgs.Value;
                condition._serviceProvider = eventArgs.ServiceProvider;
                condition._cultureInfoForTypeConverter = eventArgs.CultureInfo;

                eventArgs.Handled = true;
            }
        }


        private bool _sealed = false;

        private DependencyProperty _property;
        private BindingBase _binding;
        private object _value = DependencyProperty.UnsetValue;
        private string _sourceName = null;
        private object _unresolvedProperty = null;
        private object _unresolvedValue = null;
        private ITypeDescriptorContext _serviceProvider = null;
        private CultureInfo _cultureInfoForTypeConverter = null;
    }
}

