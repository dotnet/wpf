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
    /// Represents a page media type option.
    /// </summary>
    internal class MediaTypeOption: PrintCapabilityOption
    {
        #region Constructors

        internal MediaTypeOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the media type's value.
        /// </summary>
        public PageMediaType Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the media type to human-readable string.
        /// </summary>
        /// <returns>A string that represents this media type.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal PageMediaType _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page media type capability.
    /// </summary>
    internal class PageMediaTypeCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageMediaTypeCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents page media types supported by the device.
        /// </summary>
        public Collection<MediaTypeOption> MediaTypes
        {
            get
            {
                return _mediaTypes;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageMediaTypeCapability cap = new PageMediaTypeCapability(printCap);
            cap._mediaTypes = new Collection<MediaTypeOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            MediaTypeOption option = baseOption as MediaTypeOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageMediaTypeKeys.MediaTypeNames,
                                    PrintSchemaTags.Keywords.PageMediaTypeKeys.MediaTypeEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PageMediaType)enumValue;
                    this.MediaTypes.Add(option);
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
            MediaTypeOption option = new MediaTypeOption(baseFeature);

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
                return (this.MediaTypes.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageMediaTypeKeys.Self;
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

        internal Collection<MediaTypeOption> _mediaTypes;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page media type setting.
    /// </summary>
    internal class PageMediaTypeSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page media type setting object.
        /// </summary>
        internal PageMediaTypeSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageMediaTypeKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageMediaTypeKeys.MediaTypeNames,
                                       PrintSchemaTags.Keywords.PageMediaTypeKeys.MediaTypeEnums),
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the media type setting's value.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageMediaType"/>.
        /// </exception>
        public PageMediaType Value
        {
            get
            {
                return (PageMediaType)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.PageMediaTypeEnumMin ||
                    value > PrintSchema.PageMediaTypeEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page media type setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page media type setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}
