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
    /// Represents an output color option.
    /// </summary>
    internal class OutputColorOption: PrintCapabilityOption
    {
        #region Constructors

        internal OutputColorOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the output color option's value.
        /// </summary>
        public OutputColor Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the output color option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this output color option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal OutputColor _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page output color capability.
    /// </summary>
    internal class PageOutputColorCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageOutputColorCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents output color options supported by the device.
        /// </summary>
        public Collection<OutputColorOption> OutputColors
        {
            get
            {
                return _outputColors;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageOutputColorCapability cap = new PageOutputColorCapability(printCap);
            cap._outputColors = new Collection<OutputColorOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            OutputColorOption option = baseOption as OutputColorOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageOutputColorKeys.ColorNames,
                                    PrintSchemaTags.Keywords.PageOutputColorKeys.ColorEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (OutputColor)enumValue;
                    this.OutputColors.Add(option);
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
            OutputColorOption option = new OutputColorOption(baseFeature);

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
                return (this.OutputColors.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageOutputColorKeys.Self;
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

        internal Collection<OutputColorOption> _outputColors;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page output color setting.
    /// </summary>
    internal class PageOutputColorSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page output color setting object.
        /// </summary>
        internal PageOutputColorSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageOutputColorKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageOutputColorKeys.ColorNames,
                                       PrintSchemaTags.Keywords.PageOutputColorKeys.ColorEnums),
            };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the output color setting's value.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="OutputColor"/>.
        /// </exception>
        public OutputColor Value
        {
            get
            {
                return (OutputColor)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.OutputColorEnumMin ||
                    value > PrintSchema.OutputColorEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page output color setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page output color setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}
