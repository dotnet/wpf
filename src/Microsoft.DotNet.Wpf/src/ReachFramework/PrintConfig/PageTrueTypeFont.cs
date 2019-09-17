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
    /// Represents a TrueType font handling mode option.
    /// </summary>
    internal class TrueTypeFontModeOption: PrintCapabilityOption
    {
        #region Constructors

        internal TrueTypeFontModeOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the TrueType font handling mode's value.
        /// </summary>
        public TrueTypeFontMode Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the TrueType font handling mode to human-readable string.
        /// </summary>
        /// <returns>A string that represents this TrueType font handling mode.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal TrueTypeFontMode _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page TrueType font handling mode capability.
    /// </summary>
    internal class PageTrueTypeFontModeCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageTrueTypeFontModeCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents TrueType font handling modes
        /// supported by the device.
        /// </summary>
        public Collection<TrueTypeFontModeOption> TrueTypeFontModes
        {
            get
            {
                return _trueTypeFontModes;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageTrueTypeFontModeCapability cap = new PageTrueTypeFontModeCapability(printCap);
            cap._trueTypeFontModes = new Collection<TrueTypeFontModeOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            TrueTypeFontModeOption option = baseOption as TrueTypeFontModeOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.ModeNames,
                                    PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.ModeEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (TrueTypeFontMode)enumValue;
                    this.TrueTypeFontModes.Add(option);
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
            TrueTypeFontModeOption option = new TrueTypeFontModeOption(baseFeature);

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
                return (this.TrueTypeFontModes.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.Self;
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

        internal Collection<TrueTypeFontModeOption> _trueTypeFontModes;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page TrueType font handling mode setting.
    /// </summary>
    internal class PageTrueTypeFontModeSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page TrueType font handling mode setting object.
        /// </summary>
        internal PageTrueTypeFontModeSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.ModeNames,
                                       PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.ModeEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of the page TrueType font handling mode setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="TrueTypeFontMode"/>.
        /// </exception>
        public TrueTypeFontMode Value
        {
            get
            {
                return (TrueTypeFontMode)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.TrueTypeFontModeEnumMin ||
                    value > PrintSchema.TrueTypeFontModeEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page TrueType font handling mode setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page TrueType font handling mode setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}