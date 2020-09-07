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
    /// Represents a collating option.
    /// </summary>
    internal class CollateOption: PrintCapabilityOption
    {
        #region Constructors

        internal CollateOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collating option's value.
        /// </summary>
        public Collation Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the collating option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this collating option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal Collation _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents collate capability.
    /// </summary>
    abstract internal class CollateCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal CollateCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents collating options supported by the device.
        /// </summary>
        public Collection<CollateOption> CollateOptions
        {
            get
            {
                return _collateOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            CollateOption option = baseOption as CollateOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.CollateKeys.CollationNames,
                                    PrintSchemaTags.Keywords.CollateKeys.CollationEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (Collation)enumValue;
                    this.CollateOptions.Add(option);
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
            CollateOption option = new CollateOption(baseFeature);

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
                return (this.CollateOptions.Count > 0);
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

        internal Collection<CollateOption> _collateOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents document collate capability.
    /// </summary>
    internal class DocumentCollateCapability : CollateCapability
    {
        #region Constructors

        internal DocumentCollateCapability(InternalPrintCapabilities ownerPrintCap)
            : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            DocumentCollateCapability cap = new DocumentCollateCapability(printCap);
            cap._collateOptions = new Collection<CollateOption>();

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.CollateKeys.DocumentCollate;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// Represents collate setting.
    /// </summary>
    abstract internal class CollateSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new collate setting object.
        /// </summary>
        internal CollateSetting(InternalPrintTicket ownerPrintTicket, string featureName)
            : base(ownerPrintTicket)
        {
            this._featureName = featureName;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.CollateKeys.CollationNames,
                                       PrintSchemaTags.Keywords.CollateKeys.CollationEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this collate setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="Collation"/>.
        /// </exception>
        public Collation Value
        {
            get
            {
                return (Collation)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.CollationEnumMin ||
                    value > PrintSchema.CollationEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the collate setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this job collate setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Represents document collate setting.
    /// </summary>
    internal class DocumentCollateSetting : CollateSetting
    {
        #region Constructors

        internal DocumentCollateSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.CollateKeys.DocumentCollate)
        {
        }

        #endregion Constructors
    }
}