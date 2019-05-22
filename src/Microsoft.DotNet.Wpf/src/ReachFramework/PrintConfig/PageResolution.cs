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
    /// Represents a page resolution option.
    /// </summary>
    internal class ResolutionOption: PrintCapabilityOption
    {
        #region Constructors

        internal ResolutionOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _resolutionX = _resolutionY = PrintSchema.UnspecifiedIntValue;
            _qualityValue = 0;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the page resolution's quality label.
        /// </summary>
        public PageQualitativeResolution QualitativeResolution
        {
            get
            {
                return _qualityValue;
            }
        }

        /// <summary>
        /// Gets the page resolution's X component.
        /// </summary>
        public int ResolutionX
        {
            get
            {
                return _resolutionX;
            }
        }

        /// <summary>
        /// Gets the page resolution's Y component.
        /// </summary>
        public int ResolutionY
        {
            get
            {
                return _resolutionY;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page resolution to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page resolution.</returns>
        public override string ToString()
        {
            return ResolutionX.ToString(CultureInfo.CurrentCulture) + " x " +
                   ResolutionY.ToString(CultureInfo.CurrentCulture) + " (Qualitative: " +
                   QualitativeResolution.ToString() + ")";
        }

        #endregion Public Methods

        #region Internal Fields

        internal int _resolutionX;
        internal int _resolutionY;

        internal PageQualitativeResolution _qualityValue;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page resolution capability.
    /// </summary>
    internal class PageResolutionCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageResolutionCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents page resolutions supported by the device.
        /// </summary>
        public Collection<ResolutionOption> Resolutions
        {
            get
            {
                return _resolutions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageResolutionCapability cap = new PageResolutionCapability(printCap);
            cap._resolutions = new Collection<ResolutionOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool added = false;

            ResolutionOption option = baseOption as ResolutionOption;

            // Validate the option is complete before adding it to the collection
            // QualitativeResolution is NOT required
            if ((option.ResolutionX > 0) &&
                (option.ResolutionY > 0))
            {
                this.Resolutions.Add(option);
                added = true;
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
            ResolutionOption option = new ResolutionOption(baseFeature);

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
            ResolutionOption option = baseOption as ResolutionOption;
            bool handled = false;

            if (reader.CurrentElementNodeType == PrintSchemaNodeTypes.ScoredProperty)
            {
                handled = true;

                if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionX)
                {
                    try
                    {
                        option._resolutionX = reader.GetCurrentPropertyIntValueWithException();
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
                }
                else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionY)
                {
                    try
                    {
                        option._resolutionY = reader.GetCurrentPropertyIntValueWithException();
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
                }
                else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageResolutionKeys.QualitativeResolution)
                {
                    int enumValue;

                    if (PrintSchemaMapper.CurrentPropertyQValueToEnumValue(reader,
                                              PrintSchemaTags.Keywords.PageResolutionKeys.QualityNames,
                                              PrintSchemaTags.Keywords.PageResolutionKeys.QualityEnums,
                                              out enumValue))
                    {
                        option._qualityValue = (PageQualitativeResolution)enumValue;
                    }
                }
                else
                {
                    handled = false;

                    #if _DEBUG
                    Trace.WriteLine("-Warning- skip unknown ScoredProperty '" +
                                    reader.CurrentElementNameAttrValue + "' at line " +
                                    reader._xmlReader.LineNumber + ", position " +
                                    reader._xmlReader.LinePosition);
                    #endif
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
                return (this.Resolutions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageResolutionKeys.Self;
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

        internal Collection<ResolutionOption> _resolutions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page resolution setting.
    /// </summary>
    internal class PageResolutionSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page resolution setting object.
        /// </summary>
        internal PageResolutionSetting(InternalPrintTicket ownerPrintTicket) : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageResolutionKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionX,
                                       PTPropValueTypes.PositiveIntValue),
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionY,
                                       PTPropValueTypes.PositiveIntValue),
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageResolutionKeys.QualitativeResolution,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageResolutionKeys.QualityNames,
                                       PrintSchemaTags.Keywords.PageResolutionKeys.QualityEnums),
            };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the page resolution setting's X component.
        /// </summary>
        /// <remarks>
        /// If this component is not specified yet, getter will return <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive integer.
        /// </exception>
        public int ResolutionX
        {
            get
            {
                return this[PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionX];
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                }

                this[PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionX] = value;
            }
        }

        /// <summary>
        /// Gets or sets the page resolution setting's Y component.
        /// </summary>
        /// <remarks>
        /// If this component is not specified yet, getter will return <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive integer.
        /// </exception>
        public int ResolutionY
        {
            get
            {
                return this[PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionY];
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                }

                this[PrintSchemaTags.Keywords.PageResolutionKeys.ResolutionY] = value;
            }
        }

        /// <summary>
        /// Gets or sets the page resolution setting's quality label.
        /// </summary>
        /// <remarks>
        /// If this quality label setting is not specified yet, getter will return 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageQualitativeResolution"/>.
        /// </exception>
        public PageQualitativeResolution QualitativeResolution
        {
            get
            {
                return (PageQualitativeResolution)this[PrintSchemaTags.Keywords.PageResolutionKeys.QualitativeResolution];
            }
            set
            {
                if (value < PrintSchema.PageQualitativeResolutionEnumMin ||
                    value > PrintSchema.PageQualitativeResolutionEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this[PrintSchemaTags.Keywords.PageResolutionKeys.QualitativeResolution] = (int)value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page resolution setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page resolution setting.</returns>
        public override string ToString()
        {
            return ResolutionX.ToString(CultureInfo.CurrentCulture) + "x" +
                   ResolutionY.ToString(CultureInfo.CurrentCulture) +
                   "(qualitative: " + QualitativeResolution.ToString() + ")";
        }

        #endregion Public Methods
    }
}
