// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++




Abstract:

    Definition and implementation of this public feature/parameter related types.



--*/

using System;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents an output quality option.
    /// </summary>
    internal class OutputQualityOption: PrintCapabilityOption
    {
        #region Constructors

        internal OutputQualityOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the output quality option's value.
        /// </summary>
        public OutputQuality Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the output quality option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this output quality option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal OutputQuality _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page output quality capability.
    /// </summary>
    internal class PageOutputQualityCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageOutputQualityCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents output quality options supported by the device.
        /// </summary>
        public Collection<OutputQualityOption> OutputQualityOptions
        {
            get
            {
                return _qualityOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageOutputQualityCapability cap = new PageOutputQualityCapability(printCap);
            cap._qualityOptions = new Collection<OutputQualityOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            OutputQualityOption option = baseOption as OutputQualityOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageOutputQualityKeys.OutputQualityNames,
                                    PrintSchemaTags.Keywords.PageOutputQualityKeys.OutputQualityEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (OutputQuality)enumValue;
                    this.OutputQualityOptions.Add(option);
                    added = true;
                }
            }

            return added;
        }

        internal override sealed void AddSubFeatureCallback(PrintCapabilityFeature subFeature)
        {
            // no sub-feature
            return;
        }

        internal override sealed bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader)
        {
            // no feature property to handle
            return false;
        }

        internal override sealed PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature)
        {
            OutputQualityOption option = new OutputQualityOption(baseFeature);

            return option;
        }

        internal override sealed void OptionAttrCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option attribute to handle
            return;
        }

        internal override sealed bool OptionPropCallback(PrintCapabilityOption option, XmlPrintCapReader reader)
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
                return (this.OutputQualityOptions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageOutputQualityKeys.Self;
            }
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

        internal Collection<OutputQualityOption> _qualityOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page output quality setting.
    /// </summary>
    internal class PageOutputQualitySetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page output quality setting object.
        /// </summary>
        internal PageOutputQualitySetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageOutputQualityKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageOutputQualityKeys.OutputQualityNames,
                                       PrintSchemaTags.Keywords.PageOutputQualityKeys.OutputQualityEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this page output quality setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="OutputQuality"/>.
        /// </exception>
        public OutputQuality Value
        {
            get
            {
                return (OutputQuality)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.OutputQualityEnumMin ||
                    value > PrintSchema.OutputQualityEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page output quality setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page output quality setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}