// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Abstract:

    Definition and implementation of this public feature/parameter related types.


--*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents an input bin option.
    /// </summary>
    internal class InputBinOption: PrintCapabilityOption
    {
        #region Constructors

        internal InputBinOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the input bin's value.
        /// </summary>
        public InputBin Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the input bin to human-readable string.
        /// </summary>
        /// <returns>A string that represents this input bin.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal InputBin _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents input bin capability.
    /// </summary>
    abstract internal class InputBinCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal InputBinCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents input bins supported by the device.
        /// </summary>
        public Collection<InputBinOption> InputBins
        {
            get
            {
                return _inputBins;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            InputBinOption option = baseOption as InputBinOption;

            // Validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.InputBinKeys.InputBinNames,
                                    PrintSchemaTags.Keywords.InputBinKeys.InputBinEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    // We require the InputBin option to have an option name
                    option._value = (InputBin)enumValue;
                    this.InputBins.Add(option);
                    added = true;
                }
            }

            return added;
        }

        internal override sealed void AddSubFeatureCallback(PrintCapabilityFeature subFeature)
        {
            // no sub-feature
        }

        internal override sealed bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader)
        {
            // no feature property to handle
            return false;
        }

        internal override sealed PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature)
        {
            InputBinOption option = new InputBinOption(baseFeature);

            return option;
        }

        internal override sealed void OptionAttrCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option attribute to handle
            return;
        }

        /// <exception cref="XmlException">XML is not well-formed.</exception>
        internal override sealed bool OptionPropCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option property to handle
            return false;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed bool IsValid
        {
            get
            {
                return (this.InputBins.Count > 0);
            }
        }

        internal abstract override string FeatureName
        {
            get;
        }

        internal override sealed bool HasSubFeature
        {
            get
            {
                return false;
            }
        }

        #endregion Internal Properties

        #region Internal Fields

        internal Collection<InputBinOption> _inputBins;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents job input bin capability.
    /// </summary>
    internal class JobInputBinCapability : InputBinCapability
    {
        #region Constructors

        internal JobInputBinCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            JobInputBinCapability cap = new JobInputBinCapability(printCap);
            cap._inputBins = new Collection<InputBinOption>();

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.InputBinKeys.JobInputBin;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// Represents document input bin capability.
    /// </summary>
    internal class DocumentInputBinCapability : InputBinCapability
    {
        #region Constructors

        internal DocumentInputBinCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            DocumentInputBinCapability cap = new DocumentInputBinCapability(printCap);
            cap._inputBins = new Collection<InputBinOption>();

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.InputBinKeys.DocumentInputBin;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// Represents page input bin capability.
    /// </summary>
    internal class PageInputBinCapability : InputBinCapability
    {
        #region Constructors

        internal PageInputBinCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageInputBinCapability cap = new PageInputBinCapability(printCap);
            cap._inputBins = new Collection<InputBinOption>();

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.InputBinKeys.PageInputBin;
            }
        }

        #endregion Internal Properties
    }


    /// <summary>
    /// Represents input bin setting.
    /// </summary>
    abstract internal class InputBinSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new input bin setting object.
        /// </summary>
        internal InputBinSetting(InternalPrintTicket ownerPrintTicket, string featureName)
            : base(ownerPrintTicket)
        {
            this._featureName = featureName;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.InputBinKeys.InputBinNames,
                                       PrintSchemaTags.Keywords.InputBinKeys.InputBinEnums),
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the input bin setting's value.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="InputBin"/>.
        /// </exception>
        public InputBin Value
        {
            get
            {
                return (InputBin)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.InputBinEnumMin ||
                    value > PrintSchema.InputBinEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the input bin setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this input bin setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Represents job input bin setting.
    /// </summary>
    internal class JobInputBinSetting : InputBinSetting
    {
        #region Constructors

        internal JobInputBinSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.InputBinKeys.JobInputBin)
        {
        }

        #endregion Constructors
    }

    /// <summary>
    /// Represents document input bin setting.
    /// </summary>
    internal class DocumentInputBinSetting : InputBinSetting
    {
        #region Constructors

        internal DocumentInputBinSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.InputBinKeys.DocumentInputBin)
        {
        }

        #endregion Constructors
    }
    /// <summary>
    /// Represents page input bin setting.
    /// </summary>
    internal class PageInputBinSetting : InputBinSetting
    {
        #region Constructors

        internal PageInputBinSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.InputBinKeys.PageInputBin)
        {
        }

        #endregion Constructors
    }
}