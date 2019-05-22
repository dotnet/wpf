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
    /// Represents a device font substitution option.
    /// </summary>
    internal class DeviceFontSubstitutionOption: PrintCapabilityOption
    {
        #region Constructors

        internal DeviceFontSubstitutionOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the device font substitution option's value.
        /// </summary>
        public DeviceFontSubstitution Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the device font substitution option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this device font substitution option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal DeviceFontSubstitution _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page device font substitution capability.
    /// </summary>
    internal class PageDeviceFontSubstitutionCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageDeviceFontSubstitutionCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents page device font substitution
        /// options supported by the device.
        /// </summary>
        public Collection<DeviceFontSubstitutionOption> DeviceFontSubstitutionOptions
        {
            get
            {
                return _substOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageDeviceFontSubstitutionCapability cap = new PageDeviceFontSubstitutionCapability(printCap);
            cap._substOptions = new Collection<DeviceFontSubstitutionOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            DeviceFontSubstitutionOption option = baseOption as DeviceFontSubstitutionOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.SubstitutionNames,
                                    PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.SubstitutionEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (DeviceFontSubstitution)enumValue;
                    this.DeviceFontSubstitutionOptions.Add(option);
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
            DeviceFontSubstitutionOption option = new DeviceFontSubstitutionOption(baseFeature);

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
                return (this.DeviceFontSubstitutionOptions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.Self;
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

        internal Collection<DeviceFontSubstitutionOption> _substOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page device font substitution setting.
    /// </summary>
    internal class PageDeviceFontSubstitutionSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page device font substitution setting object.
        /// </summary>
        internal PageDeviceFontSubstitutionSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.SubstitutionNames,
                                       PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.SubstitutionEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this page device font substitution setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="DeviceFontSubstitution"/>.
        /// </exception>
        public DeviceFontSubstitution Value
        {
            get
            {
                return (DeviceFontSubstitution)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.DeviceFontSubstitutionEnumMin ||
                    value > PrintSchema.DeviceFontSubstitutionEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page device font substitution setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page device font substitution setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}