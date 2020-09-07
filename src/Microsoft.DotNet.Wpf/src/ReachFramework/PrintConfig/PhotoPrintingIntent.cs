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
    /// Represents a photo printing intent option.
    /// </summary>
    internal class PhotoPrintingIntentOption: PrintCapabilityOption
    {
        #region Constructors

        internal PhotoPrintingIntentOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the photo printing intent option's value.
        /// </summary>
        public PhotoPrintingIntent Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the photo printing intent option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this photo printing intent option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal PhotoPrintingIntent _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page photo printing intent capability.
    /// </summary>
    internal class PagePhotoPrintingIntentCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PagePhotoPrintingIntentCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents photo printing intent options supported by the device.
        /// </summary>
        public Collection<PhotoPrintingIntentOption> PhotoPrintingIntentOptions
        {
            get
            {
                return _intentOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PagePhotoPrintingIntentCapability cap = new PagePhotoPrintingIntentCapability(printCap);
            cap._intentOptions = new Collection<PhotoPrintingIntentOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            PhotoPrintingIntentOption option = baseOption as PhotoPrintingIntentOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.PhotoIntentNames,
                                    PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.PhotoIntentEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PhotoPrintingIntent)enumValue;
                    this.PhotoPrintingIntentOptions.Add(option);
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
            PhotoPrintingIntentOption option = new PhotoPrintingIntentOption(baseFeature);

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
                return (this.PhotoPrintingIntentOptions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.Self;
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

        internal Collection<PhotoPrintingIntentOption> _intentOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page photo printing intent setting.
    /// </summary>
    internal class PagePhotoPrintingIntentSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page photo printing intent setting object.
        /// </summary>
        internal PagePhotoPrintingIntentSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.PhotoIntentNames,
                                       PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.PhotoIntentEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this page photo printing intent setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PhotoPrintingIntent"/>.
        /// </exception>
        public PhotoPrintingIntent Value
        {
            get
            {
                return (PhotoPrintingIntent)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.PhotoPrintingIntentEnumMin ||
                    value > PrintSchema.PhotoPrintingIntentEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page photo printing intent setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page photo printing intent setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}