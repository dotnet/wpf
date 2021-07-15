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
    /// Represents a borderless option.
    /// </summary>
    internal class BorderlessOption: PrintCapabilityOption
    {
        #region Constructors

        internal BorderlessOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the borderless option's value.
        /// </summary>
        public PageBorderless Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the borderless option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this borderless option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal PageBorderless _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page borderless capability.
    /// </summary>
    internal class PageBorderlessCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageBorderlessCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents borderless options supported by the device.
        /// </summary>
        public Collection<BorderlessOption> BorderlessOptions
        {
            get
            {
                return _borderlessOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageBorderlessCapability cap = new PageBorderlessCapability(printCap);
            cap._borderlessOptions = new Collection<BorderlessOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            BorderlessOption option = baseOption as BorderlessOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageBorderlessKeys.BorderlessNames,
                                    PrintSchemaTags.Keywords.PageBorderlessKeys.BorderlessEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PageBorderless)enumValue;
                    this.BorderlessOptions.Add(option);
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
            BorderlessOption option = new BorderlessOption(baseFeature);

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
                return (this.BorderlessOptions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageBorderlessKeys.Self;
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

        internal Collection<BorderlessOption> _borderlessOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page borderless setting.
    /// </summary>
    internal class PageBorderlessSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page borderless setting object.
        /// </summary>
        internal PageBorderlessSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageBorderlessKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageBorderlessKeys.BorderlessNames,
                                       PrintSchemaTags.Keywords.PageBorderlessKeys.BorderlessEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this page borderless setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageBorderless"/>.
        /// </exception>
        public PageBorderless Value
        {
            get
            {
                return (PageBorderless)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.PageBorderlessEnumMin ||
                    value > PrintSchema.PageBorderlessEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page borderless setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page borderless setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}