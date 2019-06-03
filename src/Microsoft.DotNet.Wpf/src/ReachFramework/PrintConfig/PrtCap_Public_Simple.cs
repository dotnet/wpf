// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of simple public PT/PC classes.



--*/

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace System.Printing
{
    /// <summary>
    /// Represents page imageable area capability.
    /// </summary>
    public sealed class PageImageableArea
    {
        #region Constructors

        internal PageImageableArea(double originW, double originH, double extentW, double extentH)
        {
            _originW = originW;
            _originH = originH;
            _extentW = extentW;
            _extentH = extentH;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the horizontal origin (in 1/96 inch unit) of the imageable area relative to the application media size.
        /// </summary>
        public double OriginWidth
        {
            get
            {
                return _originW;
            }
        }

        /// <summary>
        /// Gets the vertical origin (in 1/96 inch unit) of the imageable area relative to the application media size.
        /// </summary>
        public double OriginHeight
        {
            get
            {
                return _originH;
            }
        }

        /// <summary>
        /// Gets the horizontal distance (in 1/96 inch unit) between the origin and the bounding limit
        /// of the application media size.
        /// </summary>
        public double ExtentWidth
        {
            get
            {
                return _extentW;
            }
        }

        /// <summary>
        /// Gets the vertical distance (in 1/96 inch unit) between the origin and the bounding limit
        /// of the application media size.
        /// </summary>
        public double ExtentHeight
        {
            get
            {
                return _extentH;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page imageable area capability to human-readable string.
        /// </summary>
        /// <returns>String that shows the page imageable area capability.</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "({0}, {1}), ({2}, {3})",
                                 OriginWidth, OriginHeight, ExtentWidth, ExtentHeight);
        }

        #endregion Public Methods

        #region Private Fields

        private double _originW;
        private double _originH;
        private double _extentW;
        private double _extentH;

        #endregion private Fields
    }

    /// <summary>
    /// Represents supported range for page scaling factor setting.
    /// </summary>
    public sealed class PageScalingFactorRange
    {
        #region Constructors

        internal PageScalingFactorRange(int scaleMin, int scaleMax)
        {
            _scaleMin = scaleMin;
            _scaleMax = scaleMax;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the minimum page scaling factor (in percentage unit) supported.
        /// </summary>
        public int MinimumScale
        {
            get
            {
                return _scaleMin;
            }
        }

        /// <summary>
        /// Gets the maximum page scaling factor (in percentage unit) supported.
        /// </summary>
        public int MaximumScale
        {
            get
            {
                return _scaleMax;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page scaling factor range to human-readable string.
        /// </summary>
        /// <returns>String that shows the page scaling factor range.</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "({0}, {1})",
                                 MinimumScale, MaximumScale);
        }

        #endregion Public Methods

        #region Private Fields

        private int _scaleMin;
        private int _scaleMax;

        #endregion private Fields
    }

    /// <summary>
    /// Represents a printer's printing capabilities.
    /// </summary>
    public sealed class PrintCapabilities
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of <see cref="PrintCapabilities"/> with printing capabilities
        /// based on the XML form of PrintCapabilities.
        /// </summary>
        /// <param name="xmlStream"><see cref="Stream"/> object containing the XML form of PrintCapabilities.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="xmlStream"/> parameter is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// The <see cref="Stream"/> object specified by <paramref name="xmlStream"/> parameter doesn't contain a
        /// well-formed XML PrintCapabilities. The exception object's <see cref="Exception.Message"/> property describes
        /// why the XML is not well-formed PrintCapabilities. If not null, the exception object's
        /// <see cref="Exception.InnerException"/> property provides more details.
        /// </exception>
        public PrintCapabilities(Stream xmlStream)
        {
            _printCap = new InternalPrintCapabilities(xmlStream);
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets a read-only collection of <see cref="Collation"/> that represents the printer's document collate capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no document collate capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<Collation> CollationCapability
        {
            get
            {
                if (_collationCap == null)
                {
                    List<Collation> valueSet = new List<Collation>();

                    if (_printCap.SupportsCapability(CapabilityName.DocumentCollate))
                    {
                        foreach (CollateOption option in _printCap.DocumentCollateCapability.CollateOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _collationCap = valueSet.AsReadOnly();
                }

                return _collationCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="DeviceFontSubstitution"/> that represents the printer's
        /// device font substitution capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no device font substitution capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<DeviceFontSubstitution> DeviceFontSubstitutionCapability
        {
            get
            {
                if (_deviceFontCap == null)
                {
                    List<DeviceFontSubstitution> valueSet = new List<DeviceFontSubstitution>();

                    if (_printCap.SupportsCapability(CapabilityName.PageDeviceFontSubstitution))
                    {
                        foreach (DeviceFontSubstitutionOption option in _printCap.PageDeviceFontSubstitutionCapability.DeviceFontSubstitutionOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _deviceFontCap = valueSet.AsReadOnly();

                }

                return _deviceFontCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="Duplexing"/> that represents the printer's job duplex capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no job duplex capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<Duplexing> DuplexingCapability
        {
            get
            {
                if (_duplexingCap == null)
                {
                    List<Duplexing> valueSet = new List<Duplexing>();

                    if (_printCap.SupportsCapability(CapabilityName.JobDuplex))
                    {
                        foreach (DuplexOption option in _printCap.JobDuplexCapability.DuplexOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _duplexingCap = valueSet.AsReadOnly();
                }

                return _duplexingCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="InputBin"/> that represents the printer's input bin capability.
        /// </summary>
        /// <remarks>
        /// Print Schema defines three input bin capabilities: JobInputBin, DocumentInputBin and PageInputBin.
        /// 
        /// If none of the JobInputBin, DocumentInputBin or PageInputBin capability is supported by the printer,
        /// then the returned collection will be empty. Otherwise, the returned collection will represent the printer's
        /// input bin capability for JobInputBin if JobInputBin is supported, or for DocumentInputBin if DocumentInputBin
        /// is supported, or for PageInputBin.
        /// </remarks>
        public ReadOnlyCollection<InputBin> InputBinCapability
        {
            get
            {
                if (_inputBinCap == null)
                {
                    List<InputBin> valueSet = new List<InputBin>();

                    Collection<InputBinOption> inputBinOptions = null;

                    if (_printCap.SupportsCapability(CapabilityName.JobInputBin))
                    {
                        // read the JobInputBin capability
                        inputBinOptions = _printCap.JobInputBinCapability.InputBins;
                    }
                    else if (_printCap.SupportsCapability(CapabilityName.DocumentInputBin))
                    {
                        // read the DocumentInputBin capability if JobInputBin is not supported
                        inputBinOptions = _printCap.DocumentInputBinCapability.InputBins;
                    }
                    else if (_printCap.SupportsCapability(CapabilityName.PageInputBin))
                    {
                        // read the PageInputBin capability if neither JobInputBin nor DocumentInputBin is supported
                        inputBinOptions = _printCap.PageInputBinCapability.InputBins;
                    }

                    if (inputBinOptions != null)
                    {
                        foreach (InputBinOption option in inputBinOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _inputBinCap = valueSet.AsReadOnly();
                }

                return _inputBinCap;
            }
        }

        /// <summary>
        /// Gets the maximum job copy count the printer can support.
        /// </summary>
        /// <remarks>
        /// If the printer doesn't report maximum job copy count supported, this property will return null.
        /// </remarks>
        public Nullable<int> MaxCopyCount
        {
            get
            {
                if (_maxCopyCount == null)
                {
                    if (_printCap.SupportsCapability(CapabilityName.JobCopyCount) &&
                        (_printCap.JobCopyCountCapability.MaxValue != PrintSchema.UnspecifiedIntValue))
                    {
                        _maxCopyCount = _printCap.JobCopyCountCapability.MaxValue;
                    }
                }

                return _maxCopyCount;
            }
        }

        /// <summary>
        /// Gets the horizontal dimension (in 1/96 inch unit) of the application media size relative to the PageOrientation.
        /// </summary>
        public Nullable<double> OrientedPageMediaWidth
        {
            get
            {
                if (_orientedPageMediaWidth == null)
                {
                    if (_printCap.SupportsCapability(CapabilityName.PageImageableSize) &&
                        (_printCap.PageImageableSizeCapability.ImageableSizeWidth != PrintSchema.UnspecifiedDoubleValue))
                    {
                        _orientedPageMediaWidth = _printCap.PageImageableSizeCapability.ImageableSizeWidth;
                    }
                }

                return _orientedPageMediaWidth;
            }
        }

        /// <summary>
        /// Gets the vertical dimension (in 1/96 inch unit) of the application media size relative to the PageOrientation.
        /// </summary>
        public Nullable<double> OrientedPageMediaHeight
        {
            get
            {
                if (_orientedPageMediaHeight == null)
                {
                    if (_printCap.SupportsCapability(CapabilityName.PageImageableSize) &&
                        (_printCap.PageImageableSizeCapability.ImageableSizeHeight != PrintSchema.UnspecifiedDoubleValue))
                    {
                        _orientedPageMediaHeight = _printCap.PageImageableSizeCapability.ImageableSizeHeight;
                    }
                }

                return _orientedPageMediaHeight;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="OutputColor"/> that represents the printer's
        /// page output color capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page output color capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<OutputColor> OutputColorCapability
        {
            get
            {
                if (_outputColorCap == null)
                {
                    List<OutputColor> valueSet = new List<OutputColor>();

                    if (_printCap.SupportsCapability(CapabilityName.PageOutputColor))
                    {
                        foreach (OutputColorOption option in _printCap.PageOutputColorCapability.OutputColors)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _outputColorCap = valueSet.AsReadOnly();
                }

                return _outputColorCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="OutputQuality"/> that represents the printer's
        /// page output quality capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page output quality capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<OutputQuality> OutputQualityCapability
        {
            get
            {
                if (_outputQualityCap == null)
                {
                    List<OutputQuality> valueSet = new List<OutputQuality>();

                    if (_printCap.SupportsCapability(CapabilityName.PageOutputQuality))
                    {
                        foreach (OutputQualityOption option in _printCap.PageOutputQualityCapability.OutputQualityOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _outputQualityCap = valueSet.AsReadOnly();
                }

                return _outputQualityCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PageBorderless"/> that represents the printer's
        /// page borderless capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page borderless capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PageBorderless> PageBorderlessCapability
        {
            get
            {
                if (_pageBorderlessCap == null)
                {
                    List<PageBorderless> valueSet = new List<PageBorderless>();

                    if (_printCap.SupportsCapability(CapabilityName.PageBorderless))
                    {
                        foreach (BorderlessOption option in _printCap.PageBorderlessCapability.BorderlessOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _pageBorderlessCap = valueSet.AsReadOnly();
                }

                return _pageBorderlessCap;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageImageableArea"/> that represents the printer's page imageable area capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page imageable area capability, this property will return null.
        /// </remarks>
        public PageImageableArea PageImageableArea
        {
            get
            {
                if (_pageImageableArea == null)
                {
                    if (_printCap.SupportsCapability(CapabilityName.PageImageableSize) &&
                        (_printCap.PageImageableSizeCapability.ImageableArea != null))
                    {
                        CanvasImageableArea area = _printCap.PageImageableSizeCapability.ImageableArea;

                        if ((area.OriginWidth != PrintSchema.UnspecifiedDoubleValue) &&
                            (area.OriginHeight != PrintSchema.UnspecifiedDoubleValue) &&
                            (area.ExtentWidth != PrintSchema.UnspecifiedDoubleValue) &&
                            (area.ExtentHeight != PrintSchema.UnspecifiedDoubleValue))
                        {
                            _pageImageableArea = new PageImageableArea(area.OriginWidth,
                                                                       area.OriginHeight,
                                                                       area.ExtentWidth,
                                                                       area.ExtentHeight);
                        }
                    }
                }

                return _pageImageableArea;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PageMediaSize"/> that represents the printer's
        /// page media size capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page media size capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PageMediaSize> PageMediaSizeCapability
        {
            get
            {
                if (_pageMediaSizeCap == null)
                {
                    List<PageMediaSize> mediaSet = new List<PageMediaSize>();

                    if (_printCap.SupportsCapability(CapabilityName.PageMediaSize))
                    {
                        foreach (FixedMediaSizeOption option in _printCap.PageMediaSizeCapability.FixedMediaSizes)
                        {
                            if ((option.MediaSizeWidth != PrintSchema.UnspecifiedDoubleValue) &&
                                (option.MediaSizeHeight != PrintSchema.UnspecifiedDoubleValue))
                            {
                                mediaSet.Add(new PageMediaSize(option.Value,
                                                               option.MediaSizeWidth,
                                                               option.MediaSizeHeight));
                            }
                            else
                            {
                                mediaSet.Add(new PageMediaSize(option.Value));
                            }
                        }
                    }

                    _pageMediaSizeCap = mediaSet.AsReadOnly();
                }

                return _pageMediaSizeCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PageMediaType"/> that represents the printer's
        /// page media type capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page media type capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PageMediaType> PageMediaTypeCapability
        {
            get
            {
                if (_pageMediaTypeCap == null)
                {
                    List<PageMediaType> valueSet = new List<PageMediaType>();

                    if (_printCap.SupportsCapability(CapabilityName.PageMediaType))
                    {
                        foreach (MediaTypeOption option in _printCap.PageMediaTypeCapability.MediaTypes)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _pageMediaTypeCap = valueSet.AsReadOnly();
                }

                return _pageMediaTypeCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PageOrder"/> that represents the printer's
        /// job page order capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no job page order capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PageOrder> PageOrderCapability
        {
            get
            {
                if (_pageOrderCap == null)
                {
                    List<PageOrder> valueSet = new List<PageOrder>();

                    if (_printCap.SupportsCapability(CapabilityName.JobPageOrder))
                    {
                        foreach (PageOrderOption option in _printCap.JobPageOrderCapability.PageOrderOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _pageOrderCap = valueSet.AsReadOnly();
                }

                return _pageOrderCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PageOrientation"/> that represents the printer's
        /// page orientation capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page orientation capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PageOrientation> PageOrientationCapability
        {
            get
            {
                if (_pageOrientationCap == null)
                {
                    List<PageOrientation> valueSet = new List<PageOrientation>();

                    if (_printCap.SupportsCapability(CapabilityName.PageOrientation))
                    {
                        foreach (OrientationOption option in _printCap.PageOrientationCapability.Orientations)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _pageOrientationCap = valueSet.AsReadOnly();
                }

                return _pageOrientationCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PageResolution"/> that represents the printer's
        /// page resolution capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page resolution capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PageResolution> PageResolutionCapability
        {
            get
            {
                if (_pageResolutionCap == null)
                {
                    List<PageResolution> resSet = new List<PageResolution>();

                    if (_printCap.SupportsCapability(CapabilityName.PageResolution))
                    {
                        foreach (ResolutionOption option in _printCap.PageResolutionCapability.Resolutions)
                        {
                            if ((int)option.QualitativeResolution != PrintSchema.EnumUnspecifiedValue)
                            {
                                resSet.Add(new PageResolution(option.ResolutionX,
                                                              option.ResolutionY,
                                                              option.QualitativeResolution));
                            }
                            else
                            {
                                resSet.Add(new PageResolution(option.ResolutionX,
                                                              option.ResolutionY));
                            }
                        }
                    }

                    _pageResolutionCap = resSet.AsReadOnly();
                }

                return _pageResolutionCap;
            }
        }

        /// <summary>
        /// Gets a <see cref="PageScalingFactorRange"/> that represents the printer's supported range
        /// for page scaling factor.
        /// </summary>
        /// <remarks>
        /// If the printer has no page scaling capability, this property will return null.
        /// </remarks>
        public PageScalingFactorRange PageScalingFactorRange
        {
            get
            {
                if (_pageScalingFactorRange == null)
                {
                    if (_printCap.SupportsCapability(CapabilityName.PageScaling))
                    {
                        int min = -1, max = -1;

                        foreach (ScalingOption option in _printCap.PageScalingCapability.ScalingOptions)
                        {
                            //
                            // Going through the scaling options and find out the smallest [min, max] range
                            //
                            if (option.Value == PageScaling.Custom)
                            {
                                if ((min == -1) ||
                                    (option.CustomScaleWidth.MinValue > min))
                                {
                                    min = option.CustomScaleWidth.MinValue;
                                }

                                if ((max == -1) ||
                                    (option.CustomScaleWidth.MaxValue < max))
                                {
                                    max = option.CustomScaleWidth.MaxValue;
                                }

                                if (option.CustomScaleHeight.MinValue > min)
                                    min = option.CustomScaleHeight.MinValue;

                                if (option.CustomScaleHeight.MaxValue < max)
                                    max = option.CustomScaleHeight.MaxValue;
                            }
                            else if (option.Value == PageScaling.CustomSquare)
                            {
                                if ((min == -1) ||
                                    (option.CustomSquareScale.MinValue > min))
                                {
                                    min = option.CustomSquareScale.MinValue;
                                }

                                if ((max == -1) ||
                                    (option.CustomSquareScale.MaxValue < max))
                                {
                                    max = option.CustomSquareScale.MaxValue;
                                }
                            }
                        }

                        if ((min > 0) && (max > 0))
                        {
                            _pageScalingFactorRange = new PageScalingFactorRange(min, max);
                        }
                    }
                }

                return _pageScalingFactorRange;
            }
        }

        /// <summary>
        /// Gets a read-only collection of integers that represents the printer's job pages-per-sheet capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no job pages-per-sheet capability, the returned collection will be empty.
        /// (Pages-per-sheet is to output multiple logical pages to a single physical sheet.)
        /// </remarks>
        public ReadOnlyCollection<int> PagesPerSheetCapability
        {
            get
            {
                if (_pagesPerSheetCap == null)
                {
                    List<int> valueSet = new List<int>();

                    if (_printCap.SupportsCapability(CapabilityName.JobNUp))
                    {
                        foreach (NUpOption option in _printCap.JobNUpCapability.NUps)
                        {
                            if (!valueSet.Contains(option.PagesPerSheet))
                            {
                                valueSet.Add(option.PagesPerSheet);
                            }
                        }
                    }

                    _pagesPerSheetCap = valueSet.AsReadOnly();
                }

                return _pagesPerSheetCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PagesPerSheetDirection"/> that represents the printer's
        /// job pages-per-sheet direction capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no job pages-per-sheet direction capability, the returned collection will be empty.
        /// (Pages-per-sheet is to output multiple logical pages to a single physical sheet.)
        /// </remarks>
        public ReadOnlyCollection<PagesPerSheetDirection> PagesPerSheetDirectionCapability
        {
            get
            {
                if (_pagesPerSheetDirectionCap == null)
                {
                    List<PagesPerSheetDirection> valueSet = new List<PagesPerSheetDirection>();

                    if (_printCap.SupportsCapability(CapabilityName.JobNUp) &&
                        _printCap.JobNUpCapability.SupportsPresentationDirection)
                    {
                        foreach (NUpPresentationDirectionOption option in
                                 _printCap.JobNUpCapability.PresentationDirectionCapability.PresentationDirections)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _pagesPerSheetDirectionCap = valueSet.AsReadOnly();
                }

                return _pagesPerSheetDirectionCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="PhotoPrintingIntent"/> that represents the printer's
        /// page photo printing intent capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no page photo printing intent capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<PhotoPrintingIntent> PhotoPrintingIntentCapability
        {
            get
            {
                if (_photoIntentCap == null)
                {
                    List<PhotoPrintingIntent> valueSet = new List<PhotoPrintingIntent>();

                    if (_printCap.SupportsCapability(CapabilityName.PagePhotoPrintingIntent))
                    {
                        foreach (PhotoPrintingIntentOption option in _printCap.PagePhotoPrintingIntentCapability.PhotoPrintingIntentOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _photoIntentCap = valueSet.AsReadOnly();
                }

                return _photoIntentCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="Stapling"/> that represents the printer's job output stapling capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no job output stapling capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<Stapling> StaplingCapability
        {
            get
            {
                if (_staplingCap == null)
                {
                    List<Stapling> valueSet = new List<Stapling>();

                    if (_printCap.SupportsCapability(CapabilityName.JobStaple))
                    {
                        foreach (StaplingOption option in _printCap.JobStapleCapability.StaplingOptions)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _staplingCap = valueSet.AsReadOnly();
                }

                return _staplingCap;
            }
        }

        /// <summary>
        /// Gets a read-only collection of <see cref="TrueTypeFontMode"/> that represents the printer's
        /// TrueType font handling mode capability.
        /// </summary>
        /// <remarks>
        /// If the printer has no TrueType font handling mode capability, the returned collection will be empty.
        /// </remarks>
        public ReadOnlyCollection<TrueTypeFontMode> TrueTypeFontModeCapability
        {
            get
            {
                if (_ttFontCap == null)
                {
                    List<TrueTypeFontMode> valueSet = new List<TrueTypeFontMode>();

                    if (_printCap.SupportsCapability(CapabilityName.PageTrueTypeFontMode))
                    {
                        foreach (TrueTypeFontModeOption option in _printCap.PageTrueTypeFontModeCapability.TrueTypeFontModes)
                        {
                            if (!valueSet.Contains(option.Value))
                            {
                                valueSet.Add(option.Value);
                            }
                        }
                    }

                    _ttFontCap = valueSet.AsReadOnly();
                }

                return _ttFontCap;
            }
        }

        #endregion Public Properties

        #region Private Fields

        private InternalPrintCapabilities _printCap;

        private ReadOnlyCollection<Collation> _collationCap;
        private ReadOnlyCollection<DeviceFontSubstitution> _deviceFontCap;
        private ReadOnlyCollection<Duplexing> _duplexingCap;
        private ReadOnlyCollection<InputBin> _inputBinCap;
        private Nullable<int> _maxCopyCount;
        private Nullable<double> _orientedPageMediaWidth;
        private Nullable<double> _orientedPageMediaHeight;
        private ReadOnlyCollection<OutputColor> _outputColorCap;
        private ReadOnlyCollection<OutputQuality> _outputQualityCap;
        private ReadOnlyCollection<PageBorderless> _pageBorderlessCap;
        private PageImageableArea _pageImageableArea;
        private ReadOnlyCollection<PageMediaSize> _pageMediaSizeCap;
        private ReadOnlyCollection<PageMediaType> _pageMediaTypeCap;
        private ReadOnlyCollection<PageOrder> _pageOrderCap;
        private ReadOnlyCollection<PageOrientation> _pageOrientationCap;
        private ReadOnlyCollection<PageResolution> _pageResolutionCap;
        private PageScalingFactorRange _pageScalingFactorRange;
        private ReadOnlyCollection<int> _pagesPerSheetCap;
        private ReadOnlyCollection<PagesPerSheetDirection> _pagesPerSheetDirectionCap;
        private ReadOnlyCollection<PhotoPrintingIntent> _photoIntentCap;
        private ReadOnlyCollection<Stapling> _staplingCap;
        private ReadOnlyCollection<TrueTypeFontMode> _ttFontCap;

        #endregion Private Fields
    }
}