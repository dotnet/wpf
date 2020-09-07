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
    /// Represents a page order option.
    /// </summary>
    internal class PageOrderOption: PrintCapabilityOption
    {
        #region Constructors

        internal PageOrderOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the page order option's value.
        /// </summary>
        public PageOrder Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page order option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page order option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal PageOrder _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents job page order capability.
    /// </summary>
    internal class JobPageOrderCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal JobPageOrderCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents page order options supported by the device.
        /// </summary>
        public Collection<PageOrderOption> PageOrderOptions
        {
            get
            {
                return _orderOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            JobPageOrderCapability cap = new JobPageOrderCapability(printCap);
            cap._orderOptions = new Collection<PageOrderOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            PageOrderOption option = baseOption as PageOrderOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.JobPageOrderKeys.PageOrderNames,
                                    PrintSchemaTags.Keywords.JobPageOrderKeys.PageOrderEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PageOrder)enumValue;
                    this.PageOrderOptions.Add(option);
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
            PageOrderOption option = new PageOrderOption(baseFeature);

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
                return (this.PageOrderOptions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.JobPageOrderKeys.Self;
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

        internal Collection<PageOrderOption> _orderOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents job page order setting.
    /// </summary>
    internal class JobPageOrderSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new job page order setting object.
        /// </summary>
        internal JobPageOrderSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.JobPageOrderKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.JobPageOrderKeys.PageOrderNames,
                                       PrintSchemaTags.Keywords.JobPageOrderKeys.PageOrderEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this job page order setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageOrder"/>.
        /// </exception>
        public PageOrder Value
        {
            get
            {
                return (PageOrder)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.PageOrderEnumMin ||
                    value > PrintSchema.PageOrderEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the job page order setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this job page order setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}