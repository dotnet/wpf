﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++




Abstract:

    Definition and implementation of this public feature/parameter related types.


--*/

using System.Xml;
using System.Collections.ObjectModel;

using System.Printing;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents an output stapling option.
    /// </summary>
    internal class StaplingOption: PrintCapabilityOption
    {
        #region Constructors

        internal StaplingOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the output stapling option's value.
        /// </summary>
        public Stapling Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the output stapling option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this output stapling option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal Stapling _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents output stapling capability.
    /// </summary>
    internal abstract class StapleCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal StapleCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents output stapling options supported by the device.
        /// </summary>
        public Collection<StaplingOption> StaplingOptions
        {
            get
            {
                return _staplingOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal sealed override bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            StaplingOption option = baseOption as StaplingOption;

            // Validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.StapleKeys.StaplingNames,
                                    PrintSchemaTags.Keywords.StapleKeys.StaplingEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (Stapling)enumValue;
                    this.StaplingOptions.Add(option);
                    added = true;
                }
            }

            return added;
        }

        internal sealed override void AddSubFeatureCallback(PrintCapabilityFeature subFeature)
        {
            // no sub-feature
            return;
        }

        internal sealed override bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader)
        {
            // no feature property to handle
            return false;
        }

        internal sealed override PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature)
        {
            StaplingOption option = new StaplingOption(baseFeature);

            return option;
        }

        internal sealed override void OptionAttrCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option attribute to handle
            return;
        }

        /// <exception cref="XmlException">XML is not well-formed.</exception>
        internal sealed override bool OptionPropCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option property to handle
            return false;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal sealed override bool IsValid
        {
            get
            {
                return (this.StaplingOptions.Count > 0);
            }
        }

        internal abstract override string FeatureName
        {
            get;
        }

        internal sealed override bool HasSubFeature
        {
            get
            {
                return false;
            }
        }

        #endregion Internal Properties

        #region Internal Fields

        internal Collection<StaplingOption> _staplingOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents job output stapling capability.
    /// </summary>
    internal class JobStapleCapability : StapleCapability
    {
        #region Constructors

        internal JobStapleCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            JobStapleCapability cap = new JobStapleCapability(printCap)
            {
                _staplingOptions = new Collection<StaplingOption>()
            };

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal sealed override string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.StapleKeys.JobStaple;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// Represents output stapling setting.
    /// </summary>
    internal abstract class StapleSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new output stapling setting object.
        /// </summary>
        internal StapleSetting(InternalPrintTicket ownerPrintTicket, string featureName)
            : base(ownerPrintTicket)
        {
            this._featureName = featureName;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.StapleKeys.StaplingNames,
                                       PrintSchemaTags.Keywords.StapleKeys.StaplingEnums),
            };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the output stapling setting's value.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="Stapling"/>.
        /// </exception>
        public Stapling Value
        {
            get
            {
                return (Stapling)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.StaplingEnumMin ||
                    value > PrintSchema.StaplingEnumMax)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the output stapling setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this output stapling setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Represents job output stapling setting.
    /// </summary>
    internal class JobStapleSetting : StapleSetting
    {
        #region Constructors

        internal JobStapleSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.StapleKeys.JobStaple)
        {
        }

        #endregion Constructors
    }
}
