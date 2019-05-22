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
using System.Text;

using System.Printing;
using MS.Internal.Printing.Configuration;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents a scaling option.
    /// </summary>
    internal class ScalingOption: PrintCapabilityOption
    {
        #region Constructors

        internal ScalingOption(PrintCapabilityFeature ownerFeature) : base(ownerFeature)
        {
            _value = PageScaling.Unspecified;

            // We use negative index value to indicate that the custom option's
            // ParameterRef element hasn't been parsed and accepted yet.

            _scaleWIndex = _scaleHIndex = _squareScaleIndex = -1;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the page scaling option's value.
        /// </summary>
        public PageScaling Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        /// Gets the <see cref="ScalingScaleWidthCapability"/> object that specifies the custom scaling option's scaling percentage capability in the width direction.
        /// </summary>
        /// <remarks>
        /// For non-custom scaling options, this property's value is always null.
        /// </remarks>
        public ScalingScaleWidthCapability CustomScaleWidth
        {
            get
            {
                if (_scaleWIndex < 0)
                    return null;

                #if _DEBUG
                Trace.Assert(Value == PageScaling.Custom, "THIS SHOULD NOT HAPPEN: non-Custom scaling option has ScaleWidth");
                #endif

                return (ScalingScaleWidthCapability)(this.OwnerFeature.OwnerPrintCap._pcLocalParamDefs[_scaleWIndex]);
            }
        }

        /// <summary>
        /// Gets the <see cref="ScalingScaleHeightCapability"/> object that specifies the custom scaling option's scaling percentage capability in the height direction.
        /// </summary>
        /// <remarks>
        /// For non-custom scaling options, this property's value is always null.
        /// </remarks>
        public ScalingScaleHeightCapability CustomScaleHeight
        {
            get
            {
                if (_scaleHIndex < 0)
                    return null;

                #if _DEBUG
                Trace.Assert(Value == PageScaling.Custom, "THIS SHOULD NOT HAPPEN: non-Custom scaling option has ScaleHeight");
                #endif

                return (ScalingScaleHeightCapability)(this.OwnerFeature.OwnerPrintCap._pcLocalParamDefs[_scaleHIndex]);
            }
        }

        public ScalingSquareScaleCapability CustomSquareScale
        {
            get
            {
                if (_squareScaleIndex < 0)
                    return null;

                return (ScalingSquareScaleCapability)(this.OwnerFeature.OwnerPrintCap._pcLocalParamDefs[_squareScaleIndex]);
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the scaling option to human-readable string.
        /// </summary>
        /// <returns>A string that represents this scaling option.</returns>
        public override string ToString()
        {
            return Value.ToString() + ": " +
                   ((CustomScaleWidth != null) ? CustomScaleWidth.ToString() : "null") + " x " +
                   ((CustomScaleHeight != null) ? CustomScaleHeight.ToString() : "null") + " x " +
                   ((CustomSquareScale != null) ? CustomSquareScale.ToString() : "null");
        }

        #endregion Public Methods

        #region Internal Fields

        internal PageScaling _value;

        internal int _scaleWIndex, _scaleHIndex, _squareScaleIndex;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page scaling capability.
    /// </summary>
    internal class PageScalingCapability : PrintCapabilityFeature
    {
        #region Constructors

        internal PageScalingCapability(InternalPrintCapabilities ownerPrintCap) : base(ownerPrintCap)
        {
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection object that represents page scaling options supported by the device.
        /// </summary>
        public Collection<ScalingOption> ScalingOptions
        {
            get
            {
                return _scalingOptions;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal static PrintCapabilityFeature NewFeatureCallback(InternalPrintCapabilities printCap)
        {
            PageScalingCapability cap = new PageScalingCapability(printCap);
            cap._scalingOptions = new Collection<ScalingOption>();

            return cap;
        }

        internal override sealed bool AddOptionCallback(PrintCapabilityOption baseOption)
        {
            bool complete = false;

            ScalingOption option = baseOption as ScalingOption;

            if (option._optionName != null)
            {
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                    PrintSchemaTags.Keywords.PageScalingKeys.ScalingNames,
                                    PrintSchemaTags.Keywords.PageScalingKeys.ScalingEnums,
                                    option._optionName);

                if (enumValue > 0)
                {
                    option._value = (PageScaling)enumValue;
                }
            }

            if (option.Value != PageScaling.Unspecified)
            {
                if (option.Value == PageScaling.None)
                {
                    // Non-custom scaling option doesn't need any ParameterRefs
                    complete = true;
                }
                else if (option.Value == PageScaling.Custom)
                {
                    // Custom scaling option must have the 2 scale ParameterRefs
                    if ((option._scaleWIndex >= 0) &&
                        (option._scaleHIndex >= 0))
                    {
                        complete = true;
                    }
                    else
                    {
                        // Need to reset the local parameter required flag since we are
                        // ignoring the custom scaling option
                        if (option._scaleWIndex >= 0)
                        {
                            option.OwnerFeature.OwnerPrintCap.SetLocalParameterDefAsRequired(option._scaleWIndex, false);
                        }

                        if (option._scaleHIndex >= 0)
                        {
                            option.OwnerFeature.OwnerPrintCap.SetLocalParameterDefAsRequired(option._scaleHIndex, false);
                        }
                    }
                }
                else if (option.Value == PageScaling.CustomSquare)
                {
                    // Custom square scaling option must have the scale ParameterRef
                    if (option._squareScaleIndex >= 0)
                    {
                        complete = true;
                    }
                    else
                    {
                        // Need to reset the local parameter required flag since we are
                        // ignoring the custom scaling option
                        if (option._squareScaleIndex >= 0)
                        {
                            option.OwnerFeature.OwnerPrintCap.SetLocalParameterDefAsRequired(option._squareScaleIndex, false);
                        }
                    }
                }
            }

            if (complete)
            {
                this.ScalingOptions.Add(option);
            }

            return complete;
        }

        internal override sealed void AddSubFeatureCallback(PrintCapabilityFeature subFeature)
        {
            // PageScaling has no sub features.
            return;
        }

        internal override sealed bool FeaturePropCallback(PrintCapabilityFeature feature, XmlPrintCapReader reader)
        {
            // no feature property to handle
            return false;
        }

        internal override sealed PrintCapabilityOption NewOptionCallback(PrintCapabilityFeature baseFeature)
        {
            ScalingOption newOption = new ScalingOption(baseFeature);

            return newOption;
        }

        internal override sealed void OptionAttrCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            // no option attribute to handle
            return;
        }

        /// <exception cref="XmlException">XML is not well-formed.</exception>
        internal override sealed bool OptionPropCallback(PrintCapabilityOption baseOption, XmlPrintCapReader reader)
        {
            bool handled = false;

            ScalingOption option = baseOption as ScalingOption;

            if (reader.CurrentElementNodeType == PrintSchemaNodeTypes.ScoredProperty)
            {
                handled = true;

                string sPropertyName = reader.CurrentElementNameAttrValue;

                if ( (sPropertyName == PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleWidth) ||
                     (sPropertyName == PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleHeight) ||
                     (sPropertyName == PrintSchemaTags.Keywords.PageScalingKeys.CustomSquareScale) )
                {
                    // custom or custom square scaling properties
                    try
                    {
                        string paramRefName = reader.GetCurrentPropertyParamRefNameWithException();

                        if ( (sPropertyName == PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleWidth) &&
                             (paramRefName == PrintSchemaTags.Keywords.ParameterDefs.PageScalingScaleWidth) )
                        {
                            option._scaleWIndex = (int)PrintSchemaLocalParameterDefs.PageScalingScaleWidth;

                            // Mark the local parameter-def as required
                            option.OwnerFeature.OwnerPrintCap.SetLocalParameterDefAsRequired(option._scaleWIndex, true);
                        }
                        else if ( (sPropertyName == PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleHeight) &&
                                  (paramRefName == PrintSchemaTags.Keywords.ParameterDefs.PageScalingScaleHeight) )
                        {
                            option._scaleHIndex = (int)PrintSchemaLocalParameterDefs.PageScalingScaleHeight;

                            // Mark the local parameter-def as required
                            option.OwnerFeature.OwnerPrintCap.SetLocalParameterDefAsRequired(option._scaleHIndex, true);
                        }
                        else if ( (sPropertyName == PrintSchemaTags.Keywords.PageScalingKeys.CustomSquareScale) &&
                                  (paramRefName == PrintSchemaTags.Keywords.ParameterDefs.PageSquareScalingScale) )
                        {
                            option._squareScaleIndex = (int)PrintSchemaLocalParameterDefs.PageSquareScalingScale;

                            // Mark the local parameter-def as required
                            option.OwnerFeature.OwnerPrintCap.SetLocalParameterDefAsRequired(option._squareScaleIndex, true);
                        }
                        else
                        {
                            #if _DEBUG
                            Trace.WriteLine("-Warning- skip unknown ParameterRef " + paramRefName +
                                            "' at line number " + reader._xmlReader.LineNumber +
                                            ", line position " + reader._xmlReader.LinePosition);
                            #endif
                        }
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
                return (this.ScalingOptions.Count > 0);
            }
        }

        internal override sealed string FeatureName
        {
            get
            {
                return PrintSchemaTags.Keywords.PageScalingKeys.Self;
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

        internal Collection<ScalingOption> _scalingOptions;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents page scaling setting.
    /// </summary>
    internal class PageScalingSetting : PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new page scaling setting object.
        /// </summary>
        internal PageScalingSetting(InternalPrintTicket ownerPrintTicket) : base(ownerPrintTicket)
        {
            this._featureName = PrintSchemaTags.Keywords.PageScalingKeys.Self;

            this._propertyMaps = new PTPropertyMapEntry[] {
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Framework.OptionNameProperty,
                                       PTPropValueTypes.EnumStringValue,
                                       PrintSchemaTags.Keywords.PageScalingKeys.ScalingNames,
                                       PrintSchemaTags.Keywords.PageScalingKeys.ScalingEnums),
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleWidth,
                                       PTPropValueTypes.IntParamRefValue,
                                       PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleWidth,
                                       PrintSchemaTags.Keywords.ParameterDefs.PageScalingScaleWidth),
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleHeight,
                                       PTPropValueTypes.IntParamRefValue,
                                       PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleHeight,
                                       PrintSchemaTags.Keywords.ParameterDefs.PageScalingScaleHeight),
                new PTPropertyMapEntry(this,
                                       PrintSchemaTags.Keywords.PageScalingKeys.CustomSquareScale,
                                       PTPropValueTypes.IntParamRefValue,
                                       PrintSchemaTags.Keywords.PageScalingKeys.CustomSquareScale,
                                       PrintSchemaTags.Keywords.ParameterDefs.PageSquareScalingScale),
                };
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the value of the page scaling setting.
        /// </summary>
        /// <remarks>
        /// If the setting is not specified yet, the return value will be
        /// <see cref="PageScaling.Unspecified"/>.
        /// </remarks>
        public PageScaling Value
        {
            get
            {
                return (PageScaling)this[PrintSchemaTags.Framework.OptionNameProperty];
            }
        }

        /// <summary>
        /// Gets the custom page scaling setting's scaling percentage in the width direction.
        /// </summary>
        /// <remarks>
        /// If current page scaling setting is not <see cref="PageScaling.Custom"/>,
        /// the return value will be <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        public int CustomScaleWidth
        {
            get
            {
                if (this.Value != PageScaling.Custom)
                    return PrintSchema.UnspecifiedIntValue;

                return this[PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleWidth];
            }
        }

        /// <summary>
        /// Gets the custom page scaling setting's scaling percentage in the height direction.
        /// </summary>
        /// <remarks>
        /// If current page scaling setting is not <see cref="PageScaling.Custom"/>,
        /// the return value will be <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        public int CustomScaleHeight
        {
            get
            {
                if (this.Value != PageScaling.Custom)
                    return PrintSchema.UnspecifiedIntValue;

                return this[PrintSchemaTags.Keywords.PageScalingKeys.CustomScaleHeight];
            }
        }

        /// <summary>
        /// Gets the custom square page scaling setting's scaling percentage.
        /// </summary>
        /// <remarks>
        /// If current page scaling setting is not <see cref="PageScaling.CustomSquare"/>,
        /// the return value will be <see cref="PrintSchema.UnspecifiedIntValue"/>.
        /// </remarks>
        public int CustomSquareScale
        {
            get
            {
                if (this.Value != PageScaling.CustomSquare)
                    return PrintSchema.UnspecifiedIntValue;

                return this[PrintSchemaTags.Keywords.PageScalingKeys.CustomSquareScale];
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Sets page scaling setting to custom square scaling.
        /// </summary>
        /// <param name="squareScale">custom square scaling percentage</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// "squareScale" is not non-negative value.
        /// </exception>
        public void SetCustomSquareScaling(int squareScale)
        {
            // Scaling percentage value must be non-negative. We do allow negative scaling offset values.
            if (squareScale < 0)
            {
                throw new ArgumentOutOfRangeException("squareScale",
                                                      PTUtility.GetTextFromResource("ArgumentException.NonNegativeValue"));
            }

            // Remove all XML content related to PageScaling feature
            // any way to avoid redundant NotifyPropertyChanged calls?
            ClearSetting();

            this[PrintSchemaTags.Framework.OptionNameProperty] = (int)PageScaling.CustomSquare;

            this[PrintSchemaTags.Keywords.PageScalingKeys.CustomSquareScale] = squareScale;
        }

        /// <summary>
        /// Converts the page scaling setting to human-readable string.
        /// </summary>
        /// <returns>A string that represents this page scaling setting.</returns>
        public override string ToString()
        {
            if ((Value != PageScaling.Custom) || (Value != PageScaling.CustomSquare))
            {
                return Value.ToString();
            }
            else
            {
                return Value.ToString() +
                       ", ScaleWidth=" + CustomScaleWidth.ToString(CultureInfo.CurrentCulture) +
                       ", ScaleHeight=" + CustomScaleHeight.ToString(CultureInfo.CurrentCulture) +
                       ", SquareScale=" + CustomSquareScale.ToString(CultureInfo.CurrentCulture);
            }
        }

        #endregion Public Methods
    }
}