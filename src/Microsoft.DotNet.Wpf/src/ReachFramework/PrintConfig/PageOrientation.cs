﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Abstract:

    Definition and implementation of this public feature/parameter related types.



--*/

using System.Collections.ObjectModel;

using System.Printing;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents an orientation option.
    /// </summary>
    internal class OrientationOption: PrintCapabilityOption
    {
        #region Constructors

        internal OrientationOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the page orientation's value.
        /// </summary>
        public PageOrientation Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the orientation to human-readable string.
        /// </summary>
        /// <returns>A string that represents this orientation.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal PageOrientation _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page orientation capability.
    /// </summary>
    internal class PageOrientationCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageOrientationCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents orientations supported by the device.
        /// </summary>
        public Collection<OrientationOption> Orientations
        {
            get
            {
                return _orientations;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageOrientationCapability cap = new PageOrientationCapability(printCap)
            {
                _orientations = new Collection<OrientationOption>()
            };

            return cap;
        }

        internal sealed override bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            OrientationOption option = baseOption as OrientationOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageOrientationKeys.OrientationNames,
                                    PrintSchemaTags.Keywords.PageOrientationKeys.OrientationEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PageOrientation)enumValue;
                    this.Orientations.Add(option);
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
            OrientationOption option = new OrientationOption(baseFeature);

            return option;
        }

        internal sealed override void OptionAttrCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option attribute to handle
            return;
        }

        internal sealed override bool OptionPropCallback(PrintCapabilityOption option, XmlPrintCapReader reader)
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
                return (this.Orientations.Count > 0);
            }
        }

        internal sealed override string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageOrientationKeys.Self;
            }
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

        internal Collection<OrientationOption> _orientations;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page orientation setting.
    /// </summary>
    internal class PageOrientationSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page orientation setting object.
        /// </summary>
        internal PageOrientationSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageOrientationKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageOrientationKeys.OrientationNames,
                                       PrintSchemaTags.Keywords.PageOrientationKeys.OrientationEnums)
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of the page orientation setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageOrientation"/>.
        /// </exception>
        public PageOrientation Value
        {
            get
            {
                return (PageOrientation)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.PageOrientationEnumMin ||
                    value > PrintSchema.PageOrientationEnumMax)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page orientation setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page orientation setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}