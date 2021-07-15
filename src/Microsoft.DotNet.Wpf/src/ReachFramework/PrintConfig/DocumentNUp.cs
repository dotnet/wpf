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

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents an NUp option.
    /// </summary>
    internal class NUpOption: PrintCapabilityOption
    {
        #region Constructors

        internal NUpOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _pagesPerSheet = PrintSchema.UnspecifiedIntValue;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the NUp option's value of number of logical pages per physical sheet.
        /// </summary>
        public int PagesPerSheet
        {
            get
            {
                return _pagesPerSheet;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the NUp option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this NUp option.</returns>
        public override string ToString()
        {
            return PagesPerSheet.ToString(CultureInfo.CurrentCulture);
        }

        #endregion Public Methods

        #region Internal Fields

        internal int _pagesPerSheet;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents NUp capability.
    /// </summary>
    abstract internal class NUpCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal NUpCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents document NUp options supported by the device.
        /// </summary>
        public Collection<NUpOption> NUps
        {
            get
            {
                return _nUps;
            }
        }

        /// <summary>
        /// Gets a Boolean value indicating whether the document NUp capability supports presentation direction.
        /// </summary>
        public bool SupportsPresentationDirection
        {
            get
            {
                return (PresentationDirectionCapability != null);
            }
        }

        /// <summary>
        /// Gets the <see cref="NUpPresentationDirectionCapability"/> object that represents the device's document NUp presentation direction capability.
        /// </summary>
        public NUpPresentationDirectionCapability PresentationDirectionCapability
        {
            get
            {
                return _presentationDirectionCap;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            NUpOption option = baseOption as NUpOption;

            // validate the option is complete before adding it to the collection
            if (option.PagesPerSheet > 0)
            {
                this.NUps.Add(option);
                added = true;
            }

            return added;
        }

        internal override sealed void AddSubFeatureCallback(PrintCapabilityFeature subFeature)
        {
            _presentationDirectionCap = subFeature as NUpPresentationDirectionCapability;
        }

        internal override sealed bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader)
        {
            // no feature property to handle
            return false;
        }

        internal override sealed PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature)
        {
            NUpOption option = new NUpOption(baseFeature);

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
            NUpOption option = baseOption as NUpOption;
            bool handled = false;

            // we are only handling scored property here
            if (reader.CurrentElementNodeType == PrintSchemaNodeTypes.ScoredProperty)
            {
                if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.NUpKeys.PagesPerSheet)
                {
                    try
                    {
                        option._pagesPerSheet = reader.GetCurrentPropertyIntValueWithException();
                    }
                    // We want to catch internal FormatException to skip recoverable XML content syntax error
                    #pragma warning suppress 56502
                    #if _DEBUG
                    catch (FormatException e)
                    #else
                    catch (FormatException)
                    #endif
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Error- " + e.Message);
                        #endif
                    }

                    handled = true;
                }
            }

            return handled;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal override sealed bool IsValid
        {
            get
            {
                bool valid = false;

                if (this.NUps.Count > 0)
                {
                    valid = true;

                    if (this.PresentationDirectionCapability != null)
                    {
                        valid = false;

                        if (this.PresentationDirectionCapability.PresentationDirections.Count > 0)
                        {
                            valid = true;
                        }
                    }
                }

                return valid;
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
                return true;
            }
        }

        #endregion Internal Properties

        #region Internal Fields

        internal Collection<NUpOption> _nUps;
        internal NUpPresentationDirectionCapability _presentationDirectionCap;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents job NUp capability.
    /// </summary>
    internal class JobNUpCapability : NUpCapability
    {
        #region Constructors

        internal JobNUpCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            JobNUpCapability cap = new JobNUpCapability(printCap);
            cap._nUps = new Collection<NUpOption>();

            return cap;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal sealed override string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.NUpKeys.JobNUp;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// Represents an NUp presentation direction option.
    /// </summary>
    internal class NUpPresentationDirectionOption: PrintCapabilityOption
    {
        #region Constructors

        internal NUpPresentationDirectionOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the NUp presentation direction option's value.
        /// </summary>
        public PagesPerSheetDirection Value
        {
            get
            {
                return _value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the NUp presentation direction option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this NUp presentation direction option.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods

        #region Internal Fields

        internal PagesPerSheetDirection _value;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents NUp presentation direction capability.
    /// </summary>
    internal class NUpPresentationDirectionCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal NUpPresentationDirectionCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents document NUp presentation
        /// direction options supported by the device.
        /// </summary>
        public Collection<NUpPresentationDirectionOption> PresentationDirections
        {
            get
            {
                return _presentationDirections;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            NUpPresentationDirectionCapability cap = new NUpPresentationDirectionCapability(printCap);
            cap._presentationDirections = new Collection<NUpPresentationDirectionOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            NUpPresentationDirectionOption option = baseOption as NUpPresentationDirectionOption;

            // validate the option is complete before adding it to the collection
            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.NUpKeys.DirectionNames,
                                    PrintSchemaTags.Keywords.NUpKeys.DirectionEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PagesPerSheetDirection)enumValue;
                    this.PresentationDirections.Add(option);
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
            NUpPresentationDirectionOption option = new NUpPresentationDirectionOption(baseFeature);

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
                return (this.PresentationDirections.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.NUpKeys.PresentationDirection;
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

        internal Collection<NUpPresentationDirectionOption> _presentationDirections;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents NUp setting.
    /// </summary>
    abstract internal class NUpSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new NUp setting object.
        /// </summary>
        internal NUpSetting(InternalPrintTicket ownerPrintTicket, string featureName) : base(ownerPrintTicket)
        {
            this._featureName = featureName;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.NUpKeys.PagesPerSheet,
                                       PTPropValueTypes.PositiveIntValue),
            };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of number of logical pages per physical sheet.
        /// </summary>
        /// <remarks>
        /// If this setting is not specified yet, getter will return <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive integer.
        /// </exception>
        public int PagesPerSheet
        {
            get
            {
                return this[PrintSchemaTags.Keywords.NUpKeys.PagesPerSheet];
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                }

                this[PrintSchemaTags.Keywords.NUpKeys.PagesPerSheet] = value;
            }
        }

        /// <summary>
        /// Gets a <see cref="NUpPresentationDirectionSetting"/> object that specifies the document NUp presentation direction setting.
        /// </summary>
        public NUpPresentationDirectionSetting PresentationDirection
        {
            get
            {
                if (_presentationDirection == null)
                {
                    _presentationDirection = new NUpPresentationDirectionSetting(this._ownerPrintTicket);

                    // This is a sub-feature so we need to set its parent feature field
                    _presentationDirection._parentFeature = this;
                }

                return _presentationDirection;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the document NUp setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this document NUp setting.</returns>
        public override string ToString()
        {
            return PagesPerSheet.ToString(CultureInfo.CurrentCulture) + ", " +
                   PresentationDirection.ToString();
        }

        #endregion Public Methods

        #region Private Fields

        private NUpPresentationDirectionSetting _presentationDirection;

        #endregion Private Fields
    }

    /// <summary>
    /// Represents job NUp setting.
    /// </summary>
    internal class JobNUpSetting : NUpSetting
    {
        #region Constructors

        internal JobNUpSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket, PrintSchemaTags.Keywords.NUpKeys.JobNUp)
        {
        }

        #endregion Constructors
    }

    /// <summary>
    /// Represents NUp presentation direction setting.
    /// </summary>
    internal class NUpPresentationDirectionSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new NUp presentation direction setting object.
        /// </summary>
        internal NUpPresentationDirectionSetting(InternalPrintTicket ownerPrintTicket)
            : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.NUpKeys.PresentationDirection;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.NUpKeys.DirectionNames,
                                       PrintSchemaTags.Keywords.NUpKeys.DirectionEnums),
            };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of this NUp presentation direction setting.
        /// </summary>
        /// <remarks>
        /// If the direction setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PagesPerSheetDirection"/>.
        /// </exception>
        public PagesPerSheetDirection Value
        {
            get
            {
                return (PagesPerSheetDirection)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
            set
            {
                if (value < PrintSchema.PagesPerSheetDirectionEnumMin ||
                    value > PrintSchema.PagesPerSheetDirectionEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the NUp presentation direction setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this NUp presentation direction setting.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}
