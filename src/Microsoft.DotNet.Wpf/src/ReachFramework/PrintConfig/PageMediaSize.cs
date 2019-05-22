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
    /// Represents a fixed-size page media size option.
    /// </summary>
    internal class FixedMediaSizeOption : PrintCapabilityOption
    {
        #region Constructors

        internal FixedMediaSizeOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = 0;
            _mediaSizeWidth = _mediaSizeHeight = PrintSchema.UnspecifiedIntValue;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the page media size's value.
        /// </summary>
        public PageMediaSizeName Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        /// Gets the fixed page media size's width of the physical media, in 1/96 inch unit.
        /// </summary>
        public double MediaSizeWidth
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_mediaSizeWidth);
            }
        }

        /// <summary>
        /// Gets the fixed page media size's height of the physical media, in 1/96 inch unit.
        /// </summary>
        public double MediaSizeHeight
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_mediaSizeHeight);
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the non-custom page media size to human-readable string.
        /// </summary>
        /// <returns>A string that represents this non-custom page media size.</returns>
        public override string ToString()
        {
            return Value.ToString() + ": " + MediaSizeWidth.ToString(CultureInfo.CurrentCulture) +
                   " x " + MediaSizeHeight.ToString(CultureInfo.CurrentCulture);
        }

        #endregion Public Methods

        #region Internal Fields

        internal PageMediaSizeName _value;
        // Integer values in micron unit
        internal int _mediaSizeWidth, _mediaSizeHeight;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page media size capability.
    /// </summary>
    internal class PageMediaSizeCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageMediaSizeCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents fixed-size page media sizes supported by the device.
        /// </summary>
        public Collection<FixedMediaSizeOption> FixedMediaSizes
        {
            get
            {
                return _fixedSizes;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageMediaSizeCapability cap = new PageMediaSizeCapability(printCap);
            cap._fixedSizes = new Collection<FixedMediaSizeOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            FixedMediaSizeOption option = baseOption as FixedMediaSizeOption;
            bool complete = false;

            // All PageMediaSize options must have an option name
            if (option._optionName == null)
                return complete;

            int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeNames,
                                PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeEnums,
                                option._optionName);

            // We only support standard Print Schema options
            if (enumValue > 0)
            {
                option._value = (PageMediaSizeName)enumValue;
                if ((option._mediaSizeWidth > 0) ||
                    (option._mediaSizeHeight > 0))
                {
                    this.FixedMediaSizes.Add(option);
                    complete = true;
                }
            }

            return complete;
        }

        internal override sealed void AddSubFeatureCallback(PrintCapabilityFeature subFeature)
        {
            // PageMediaSize has no sub features.
            return;
        }

        internal override sealed bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader)
        {
            // no feature property to handle
            return false;
        }

        internal override sealed PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature)
        {
            FixedMediaSizeOption option = new FixedMediaSizeOption(baseFeature);

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
            FixedMediaSizeOption option = baseOption as FixedMediaSizeOption;
            bool handled = false;

            if (reader.CurrentElementNodeType == PrintSchemaNodeTypes.ScoredProperty)
            {
                handled = true;

                string sPropertyName = reader.CurrentElementNameAttrValue;

                if ((sPropertyName == PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth) ||
                    (sPropertyName == PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeHeight))
                {
                    if (reader.MoveToNextSchemaElement(reader.CurrentElementDepth + 1,
                                                       PrintSchemaNodeTypes.ScoredPropertyLevelTypes))
                    {
                        if (reader.CurrentElementNodeType == PrintSchemaNodeTypes.Value)
                        {
                            // Child property is Value for fixed media size
                            int     intValue = 0;
                            bool    convertOK = false;

                            try
                            {
                                intValue = XmlConvertHelper.ConvertStringToInt32(reader.CurrentElementTextValue);
                                convertOK = true;
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
                                Trace.WriteLine("-Error- Invalid int value '" +
                                                reader.CurrentElementTextValue +
                                                "' at line number " + reader._xmlReader.LineNumber +
                                                ", line position " + reader._xmlReader.LinePosition +
                                                ": " + e.Message);
                                #endif
                            }

                            if (convertOK)
                            {
                                if (sPropertyName == PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth)
                                {
                                    option._mediaSizeWidth = intValue;
                                }
                                else
                                {
                                    option._mediaSizeHeight = intValue;
                                }
                            }
                        }
                    }
                    else
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Error- Missing required Value or ParameterRef child-element at line number " +
                                         reader._xmlReader.LineNumber + ", line position " +
                                         reader._xmlReader.LinePosition);
                        #endif
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
                return (this.FixedMediaSizes.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageMediaSizeKeys.Self;
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

        internal Collection<FixedMediaSizeOption> _fixedSizes;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page media size setting.
    /// </summary>
    internal class PageMediaSizeSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page media size setting object.
        /// </summary>
        internal PageMediaSizeSetting(InternalPrintTicket ownerPrintTicket) : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageMediaSizeKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeNames,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeEnums),

                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth,
                                       PTPropValueTypes.PositiveIntValue),

                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeHeight,
                                       PTPropValueTypes.PositiveIntValue),

                // property map entries for non-PS custom size
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.CustomMediaSizeWidth,
                                       PTPropValueTypes.IntParamRefValue,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth,
                                       PrintSchemaTags.Keywords.ParameterDefs.PageMediaSizeMediaSizeWidth),

                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.CustomMediaSizeHeight,
                                       PTPropValueTypes.IntParamRefValue,
                                       PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeHeight,
                                       PrintSchemaTags.Keywords.ParameterDefs.PageMediaSizeMediaSizeHeight),
            };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the value of the page media size setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, the return value will be 0.
        /// </remarks>
        public PageMediaSizeName Value
        {
            get
            {
                return (PageMediaSizeName)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
        }

        /// <summary>
        /// Gets the the width of the physical media, in 1/96 inch unit.
        /// </summary>
        /// <remarks>
        /// If this dimension value is not specified yet, the return value will be
        /// <see cref="PrintSchema.UnspecifiedDoubleValue"/>.
        /// </remarks>
        public double MediaSizeWidth
        {
            get
            {
                int micronValue;

                micronValue = this[PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth];

                if ((micronValue == PrintSchema.UnspecifiedIntValue) &&
                    (this.Value == PageMediaSizeName.Unknown))
                {
                    // If we can't get the fixed-size width value, and the media size name is unknown,
                    // it could be the case of custom media size.
                    string optionName;
                    bool bIsPrivate;

                    if ((this.FeatureNode != null) &&
                        ((optionName = this.FeatureNode.GetOptionName(out bIsPrivate)) != null) &&
                        (optionName == PrintSchemaTags.Keywords.PageMediaSizeKeys.CustomMediaSize))
                    {
                        micronValue = this[PrintSchemaTags.Keywords.PageMediaSizeKeys.CustomMediaSizeWidth];
                    }
                }

                return UnitConverter.LengthValueFromMicronToDIP(micronValue);
            }
        }

        /// <summary>
        /// Gets the height of the physical media, in 1/96 inch unit.
        /// </summary>
        /// <remarks>
        /// If this dimension value is not specified yet, the return value will be
        /// <see cref="PrintSchema.UnspecifiedDoubleValue"/>.
        /// </remarks>
        public double MediaSizeHeight
        {
            get
            {
                int micronValue;

                micronValue = this[PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeHeight];

                if ((micronValue == PrintSchema.UnspecifiedIntValue) &&
                    (this.Value == PageMediaSizeName.Unknown))
                {
                    // If we can't get the fixed-size height value, and the media size name is unknown,
                    // it could be the case of custom media size.
                    string optionName;
                    bool bIsPrivate;

                    if ((this.FeatureNode != null) &&
                        ((optionName = this.FeatureNode.GetOptionName(out bIsPrivate)) != null) &&
                        (optionName == PrintSchemaTags.Keywords.PageMediaSizeKeys.CustomMediaSize))
                    {
                        micronValue = this[PrintSchemaTags.Keywords.PageMediaSizeKeys.CustomMediaSizeHeight];
                    }
                }

                return UnitConverter.LengthValueFromMicronToDIP(micronValue);
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Sets page media size setting to a standard fixed media size name.
        /// </summary>
        /// <param name="value">the standard fixed media size name</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// "value" is not one of the standard <see cref="PageMediaSizeName"/>.
        /// </exception>
        /// <remarks>
        /// This function will also automatically set page media size setting to
        /// fixed width and height physical media dimensions defined by Print Schema for
        /// the standard fixed media size name.
        /// </remarks>
        public void SetFixedMediaSize(PageMediaSizeName value)
        {
            InternalSetFixedMediaSize(value, true, 0, 0, false);

            // automatically set the dimension for the standard media sizes
            for (int i = 0; i < PrintSchema.StdMediaSizeTable.Length; i++)
            {
                if (PrintSchema.StdMediaSizeTable[i].SizeValue == value)
                {
                    // Our internal table stores size width/height values in microns, so no need to
                    // do unit conversion.
                    if (PrintSchema.StdMediaSizeTable[i].SizeW != -1)
                    {
                        this[PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth] =
                            PrintSchema.StdMediaSizeTable[i].SizeW;
                    }

                    if (PrintSchema.StdMediaSizeTable[i].SizeH != -1)
                    {
                        this[PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeHeight] =
                            PrintSchema.StdMediaSizeTable[i].SizeH;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Sets page media size setting to fixed width and height physical media dimensions.
        /// </summary>
        /// <param name="mediaSizeWidth">physical media dimension in the width direction, in 1/96 inch unit</param>
        /// <param name="mediaSizeHeight">physical media dimension in the height direction, in 1/96 inch unit</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Either "mediaSizeWidth" or "mediaSizeHeight" is not a positive dimension value.
        /// </exception>
        public void SetFixedMediaSize(double mediaSizeWidth, double mediaSizeHeight)
        {
            InternalSetFixedMediaSize(0, false, mediaSizeWidth, mediaSizeHeight, true);
        }

        /// <summary>
        /// Sets page media size setting to a standard fixed media size name and
        /// to fixed width and height physical media dimensions.
        /// </summary>
        /// <param name="value">the standard fixed media size name</param>
        /// <param name="mediaSizeWidth">physical media dimension in the width direction, in 1/96 inch unit</param>
        /// <param name="mediaSizeHeight">physical media dimension in the height direction, in 1/96 inch unit</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// "value" is not one of the standard <see cref="PageMediaSizeName"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Either "mediaSizeWidth" or "mediaSizeHeight" is not a positive dimension value.
        /// </exception>
        public void SetFixedMediaSize(PageMediaSizeName value,
                                      double mediaSizeWidth,
                                      double mediaSizeHeight)
        {
            InternalSetFixedMediaSize(value, true, mediaSizeWidth, mediaSizeHeight, true);
        }

        /// <summary>
        /// Converts the page media size setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page media size setting.</returns>
        public override string ToString()
        {
            return Value.ToString() +
                   ": MediaSizeWidth=" + MediaSizeWidth.ToString(CultureInfo.CurrentCulture) +
                   ", MediaSizeHeight=" + MediaSizeHeight.ToString(CultureInfo.CurrentCulture);
        }

        #endregion Public Methods

        #region Private Methods

        /// <exception cref="ArgumentOutOfRangeException">
        /// "value" is not one of the standard <see cref="PageMediaSizeName"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Either "mediaSizeWidth" or "mediaSizeHeight" is not a positive dimension value.
        /// </exception>
        private void InternalSetFixedMediaSize(PageMediaSizeName value,
                                               bool bSetValue,
                                               double mediaSizeWidth,
                                               double mediaSizeHeight,
                                               bool bSetWH)
        {
            if (bSetValue)
            {
                if (value < PrintSchema.PageMediaSizeNameEnumMin ||
                    value > PrintSchema.PageMediaSizeNameEnumMax)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
            }

            if (bSetWH)
            {
                if ( (mediaSizeWidth <= 0) || (mediaSizeHeight <= 0) )
                {
                    throw new ArgumentOutOfRangeException((mediaSizeWidth <= 0) ? "mediaSizeWidth" : "mediaSizeHeight",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                }
            }

            // Remove all XML content related to PageMediaSize feature
            ClearSetting();

            if (bSetValue)
            {
                this[PrintSchemaTags.Framework.OptionNameProperty] = (int)value;
            }

            if (bSetWH)
            {
                this[PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeWidth] =
                    UnitConverter.LengthValueFromDIPToMicron(mediaSizeWidth);
                this[PrintSchemaTags.Keywords.PageMediaSizeKeys.MediaSizeHeight] =
                    UnitConverter.LengthValueFromDIPToMicron(mediaSizeHeight);
            }
        }

        #endregion Private Methods
    }
}