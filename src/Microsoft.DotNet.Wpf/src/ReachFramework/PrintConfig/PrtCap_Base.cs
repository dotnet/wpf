// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of PrintCapabilities base types.


--*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Printing;
using MS.Internal.Printing.Configuration;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

namespace MS.Internal.Printing.Configuration
{
    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Abstract base class of <see cref="InternalPrintCapabilities"/> feature.
    /// </summary>
    abstract internal class PrintCapabilityFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the PrintCapabilityFeature class.
        /// </summary>
        /// <param name="ownerPrintCap">The <see cref="InternalPrintCapabilities"/> object this feature belongs to.</param>
        protected PrintCapabilityFeature(InternalPrintCapabilities ownerPrintCap)
        {
            this.OwnerPrintCap = ownerPrintCap;
        }

        #endregion Constructors

        #region Internal Methods

        // derived types must implement following abstract methods

        /// <summary>
        /// Standard PrintCapabilities feature add-new-option callback.
        /// </summary>
        internal abstract bool AddOptionCallback(PrintCapabilityOption option);

        /// <summary>
        /// Standard PrintCapabilities feature add-sub-feature callback.
        /// </summary>
        internal abstract void AddSubFeatureCallback(PrintCapabilityFeature subFeature);

        /// <summary>
        /// Standard PrintCapabilities feature feature-property calllback.
        /// </summary>
        internal abstract bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader);

        /// <summary>
        /// Standard PrintCapabilities feature new-option callback.
        /// </summary>
        internal abstract PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature);

        /// <summary>
        /// Standard PrintCapabilities feature option-attribute callback.
        /// </summary>
        internal abstract void OptionAttrCallback(PrintCapabilityOption option, XmlPrintCapReader reader);

        /// <summary>
        /// Standard PrintCapabilities feature option-property callback.
        /// </summary>
        internal abstract bool OptionPropCallback(PrintCapabilityOption option, XmlPrintCapReader reader);

        #endregion Internal Methods

        #region Internal Properties

        internal InternalPrintCapabilities OwnerPrintCap
        {
            get
            {
                return _ownerPrintCap;
            }
            set
            {
                _ownerPrintCap = value;
            }
        }

        // derived types must implement these abstract properties
        internal abstract bool IsValid
        {
            get;
        }

        internal abstract string FeatureName
        {
            get;
        }

        internal abstract bool HasSubFeature
        {
            get;
        }

        #endregion Internal Properties

        #region Internal/Private Fields

        private InternalPrintCapabilities _ownerPrintCap;

        #endregion Internal/Private Fields
    }

    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Abstract base class of <see cref="PrintCapabilityFeature"/> option.
    /// </summary>
    abstract internal class PrintCapabilityOption
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the PrintCapabilityOption class.
        /// </summary>
        /// <param name="ownerFeature">The <see cref="PrintCapabilityFeature"/> object this option belongs to.</param>
        protected PrintCapabilityOption(PrintCapabilityFeature ownerFeature)
        {
            this.OwnerFeature = ownerFeature;
        }

        #endregion Constructors

        #region Internal Properties

        internal PrintCapabilityFeature OwnerFeature
        {
            get
            {
                return _ownerFeature;
            }
            set
            {
                _ownerFeature = value;
            }
        }

        #endregion Internal Properties

        #region Internal/Private Fields

        internal string _optionName;

        private PrintCapabilityFeature _ownerFeature;

        #endregion Internal/Private Fields
    }

    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Abstract base class of <see cref="InternalPrintCapabilities"/> parameter definition.
    /// </summary>
    abstract internal class ParameterDefinition
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the ParameterDefinition class.
        /// </summary>
        protected ParameterDefinition()
        {
        }

        #endregion Constructors

        #region Internal Methods

        // derived types must implement following abstract methods

        /// <summary>
        /// Standard PrintCapabilities parameter-def property callback.
        /// </summary>
        internal abstract bool ParamDefPropCallback(ParameterDefinition baseParam, XmlPrintCapReader reader);

        #endregion Internal Methods

        #region Internal Properties

        internal string ParameterName
        {
            get
            {
                return _parameterName;
            }
            set
            {
                _parameterName = value;
            }
        }

        // derived types must implement these abstract properties
        internal abstract bool IsValid
        {
            get;
        }

        #endregion Internal Properties

        #region Internal/Private Fields

        private string _parameterName;

        #endregion Internal/Private Fields
    }

    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Derived class from <see cref="ParameterDefinition"/> for parameter definition with non-negative integer value.
    /// </summary>
    internal class NonNegativeIntParameterDefinition : ParameterDefinition
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the NonNegativeIntParameterDefinition class.
        /// </summary>
        internal NonNegativeIntParameterDefinition() : base()
        {
            _minValue = _maxValue = _defaultValue = PrintSchema.UnspecifiedIntValue;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the parameter's maximum integer value.
        /// </summary>
        public int MaxValue
        {
            get
            {
                return _maxValue;
            }
        }

        /// <summary>
        /// Gets the parameter's minimum integer value.
        /// </summary>
        public int MinValue
        {
            get
            {
                return _minValue;
            }
        }

        /// <summary>
        /// Gets the parameter's default integer value.
        /// </summary>
        public int DefaultValue
        {
            get
            {
                return _defaultValue;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts this <see cref="NonNegativeIntParameterDefinition"/> object to a human-readable string.
        /// </summary>
        /// <returns>A string that represents this <see cref="NonNegativeIntParameterDefinition"/> object.</returns>
        public override string ToString()
        {
            return ParameterName + ": min=" + MinValue + ", max=" + MaxValue +
                   ", default=" + DefaultValue;
        }

        #endregion Public Methods

        #region Internal Methods

        // With current design, all parameter-def properties are generic to any specific parameters,
        // so we just need to implement the prop-callback at the base class level and all derived
        // classes will inherit the implementation.
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        internal override sealed bool ParamDefPropCallback(ParameterDefinition baseParam, XmlPrintCapReader reader)
        {
            NonNegativeIntParameterDefinition param = baseParam as NonNegativeIntParameterDefinition;
            bool handled = true;

            #if _DEBUG
            Trace.Assert(reader.CurrentElementNodeType == PrintSchemaNodeTypes.Property,
                         "THIS SHOULD NOT HAPPEN: NonNegativeIntParamDefPropCallback() gets non-Property node");
            #endif

            if (reader.CurrentElementPSFNameAttrValue == PrintSchemaTags.Keywords.ParameterProps.DefaultValue)
            {
                try
                {
                    param._defaultValue = reader.GetCurrentPropertyIntValueWithException();
                }
                // We want to catch internal FormatException to skip recoverable XML content syntax error
                #pragma warning suppress 56502
                #if _DEBUG
                catch (FormatException e)
                #else
                catch (FormatException)
                #endif
                {
                    #if _DEBUG
                    Trace.WriteLine("-Error- " + e.Message);
                    #endif
                }
            }
            else if (reader.CurrentElementPSFNameAttrValue == PrintSchemaTags.Keywords.ParameterProps.MaxValue)
            {
                try
                {
                    param._maxValue = reader.GetCurrentPropertyIntValueWithException();
                }
                // We want to catch internal FormatException to skip recoverable XML content syntax error
                #pragma warning suppress 56502
                #if _DEBUG
                catch (FormatException e)
                #else
                catch (FormatException)
                #endif
                {
                    #if _DEBUG
                    Trace.WriteLine("-Error- " + e.Message);
                    #endif
                }
            }
            else if (reader.CurrentElementPSFNameAttrValue == PrintSchemaTags.Keywords.ParameterProps.MinValue)
            {
                try
                {
                    param._minValue = reader.GetCurrentPropertyIntValueWithException();
                }
                // We want to catch internal FormatException to skip recoverable XML content syntax error
                #pragma warning suppress 56502
                #if _DEBUG
                catch (FormatException e)
                #else
                catch (FormatException)
                #endif
                {
                    #if _DEBUG
                    Trace.WriteLine("-Error- " + e.Message);
                    #endif
                }
            }
            else
            {
                handled = false;

                #if _DEBUG
                Trace.WriteLine("-Warning- skip unknown Property '" +
                                reader.CurrentElementNameAttrValue + "' at line " +
                                reader._xmlReader.LineNumber + ", position " +
                                reader._xmlReader.LinePosition);
                #endif
            }

            return handled;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed bool IsValid
        {
            get
            {
                if ((MinValue >= 0) && (MaxValue >= 0) && (DefaultValue >= 0))
                {
                    return true;
                }
                else
                {
                    #if _DEBUG
                    Trace.WriteLine("-Warning- negative integer is invalid in " + this.ToString());
                    #endif

                    return false;
                }
            }
        }

        #endregion Internal Properties

        #region Internal Fields

        internal int _maxValue;
        internal int _minValue;
        internal int _defaultValue;

        #endregion Internal Fields
    }

    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Derived class from <see cref="NonNegativeIntParameterDefinition"/> for parameter definition with non-negative double value.
    /// </summary>
    internal class LengthParameterDefinition : NonNegativeIntParameterDefinition
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the LengthParameterDefinition class.
        /// </summary>
        internal LengthParameterDefinition() : base()
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the parameter's maximum value, in 1/96 inch unit.
        /// </summary>
        public new double MaxValue
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_maxValue);
            }
        }

        /// <summary>
        /// Gets the parameter's minimum value, in 1/96 inch unit.
        /// </summary>
        public new double MinValue
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_minValue);
            }
        }

        /// <summary>
        /// Gets the parameter's default value, in 1/96 inch unit.
        /// </summary>
        public new double DefaultValue
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_defaultValue);
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts this <see cref="LengthParameterDefinition"/> object to a human-readable string.
        /// </summary>
        /// <returns>A string that represents this <see cref="LengthParameterDefinition"/> object.</returns>
        public override string ToString()
        {
            return ParameterName + ": min=" + MinValue + ", max=" + MaxValue +
                   ", default=" + DefaultValue;
        }

        #endregion Public Methods
    }

    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Abstract base class of <see cref="InternalPrintCapabilities"/> root-level property.
    /// </summary>
    abstract internal class PrintCapabilityRootProperty
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the PrintCapabilityRootProperty class.
        /// </summary>
        protected PrintCapabilityRootProperty()
        {
        }

        #endregion Constructors

        #region Internal Methods

        // derived types must implement following abstract method
        /// <summary>
        /// Standard PrintCapabilities root-level property's callback to parse and build itself.
        /// </summary>
        internal abstract bool BuildProperty(XmlPrintCapReader reader);

        #endregion Internal Methods
    }
}