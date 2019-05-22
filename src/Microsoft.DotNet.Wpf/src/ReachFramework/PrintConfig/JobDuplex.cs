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
    /// Represents a duplex option.
    /// </summary>
    internal class DuplexOption: PrintCapabilityOption
    {
        #region Constructors

        internal DuplexOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the duplexing option's value.
        /// </summary>
        public Duplexing Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the duplex option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this duplex option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal Duplexing _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents duplex capability.
    /// </summary>
    abstract internal class DuplexCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal DuplexCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents duplex options supported by the device.
        /// </summary>
        public Collection<DuplexOption> DuplexOptions
        {
            get
            {
                return _duplexOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            DuplexOption option = baseOption as DuplexOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.DuplexKeys.DuplexNames,
                                    PrintSchemaTags.Keywords.DuplexKeys.DuplexEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (Duplexing)enumValue;
                    this.DuplexOptions.Add(option);
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
            DuplexOption option = new DuplexOption(baseFeature);

            return option;
        }

        internal override sealed void OptionAttrCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option attribute to handle
            return;
        }

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
                return (this.DuplexOptions.Count > 0);
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

        internal Collection<DuplexOption> _duplexOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents job duplex capability.
    /// </summary>
    internal class JobDuplexCapability : DuplexCapability
    {
        #region Constructors

        internal JobDuplexCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            JobDuplexCapability cap = new JobDuplexCapability(printCap);
            cap._duplexOptions = new Collection<DuplexOption>();

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.DuplexKeys.JobDuplex;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// Represents duplex setting.
    /// </summary>
    abstract internal class DuplexSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new duplex setting object.
        /// </summary>
        internal DuplexSetting(InternalPrintTicket ownerPrintTicket, string featureName)
            : base(ownerPrintTicket)
        {
            this._featureName = featureName;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.DuplexKeys.DuplexNames,
                                       PrintSchemaTags.Keywords.DuplexKeys.DuplexEnums),
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this duplex setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="Duplexing"/>.
        /// </exception>
        public Duplexing Value
        {
            get
            {
                return (Duplexing)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.DuplexingEnumMin ||
                    value > PrintSchema.DuplexingEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the duplex setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this duplex setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Represents job duplex setting.
    /// </summary>
    internal class JobDuplexSetting : DuplexSetting
    {
        #region Constructors

        internal JobDuplexSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.DuplexKeys.JobDuplex)
        {
        }

        #endregion Constructors
    }
}