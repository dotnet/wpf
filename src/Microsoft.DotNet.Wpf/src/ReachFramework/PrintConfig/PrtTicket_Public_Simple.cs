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
    /// Represents page media size setting.
    /// </summary>
    public sealed class PageMediaSize
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of <see cref="PageMediaSize"/> with the specified media size name.
        /// </summary>
        /// <param name="mediaSizeName">name of the media size</param>
        public PageMediaSize(PageMediaSizeName mediaSizeName)
        {
            _mediaSizeName = mediaSizeName;
            _width = _height = null;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PageMediaSize"/> with the specified dimensions.
        /// </summary>
        /// <param name="width">width of the physical media (in 1/96 inch unit)</param>
        /// <param name="height">height of the physical media (in 1/96 inch unit)</param>
        public PageMediaSize(double width, double height)
        {
            _mediaSizeName = null;
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PageMediaSize"/> with the specified media size name
        /// and dimensions.
        /// </summary>
        /// <param name="mediaSizeName">name of the media size</param>
        /// <param name="width">width of the physical media (in 1/96 inch unit)</param>
        /// <param name="height">height of the physical media (in 1/96 inch unit)</param>
        public PageMediaSize(PageMediaSizeName mediaSizeName, double width, double height)
        {
            _mediaSizeName = mediaSizeName;
            _width = width;
            _height = height;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the media size name. Returns null if the media size name is not specified.
        /// </summary>
        public Nullable<PageMediaSizeName> PageMediaSizeName
        {
            get
            {
                return _mediaSizeName;
            }
        }

        /// <summary>
        /// Gets the physical media width dimension (in 1/96 inch unit). Returns null if the dimension is not specified.
        /// </summary>
        public Nullable<double> Width
        {
            get
            {
                return _width;
            }
        }

        /// <summary>
        /// Gets the physical media height dimension (in 1/96 inch unit). Returns null if the dimension is not specified.
        /// </summary>
        public Nullable<double> Height
        {
            get
            {
                return _height;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page media size setting to human-readable string.
        /// </summary>
        /// <returns>String that shows the page media size setting.</returns>
        public override string ToString()
        {
            return ((PageMediaSizeName != null) ? String.Format(CultureInfo.CurrentCulture, "{0}", PageMediaSizeName) : "Null") + " (" +
                   ((Width != null) ? String.Format(CultureInfo.CurrentCulture, "{0}", Width) : "Null") +
                   " x " +
                   ((Height != null) ? String.Format(CultureInfo.CurrentCulture, "{0}", Height) : "Null") +
                   ")";
        }
        #endregion Public Methods

        #region Private Properties

        private Nullable<PageMediaSizeName> _mediaSizeName;
        private Nullable<double> _width;
        private Nullable<double> _height;

        #endregion Private Properties
    }

    /// <summary>
    /// Represents page resolution setting.
    /// </summary>
    public sealed class PageResolution
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of <see cref="PageResolution"/> with the specified resolution values.
        /// </summary>
        /// <param name="resolutionX">x component of the resolution (in DPI unit)</param>
        /// <param name="resolutionY">y component of the resolution (in DPI unit)</param>
        public PageResolution(int resolutionX, int resolutionY)
        {
            _x = resolutionX;
            _y = resolutionY;
            _qualitative = null;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PageResolution"/> with the specified resolution quality label.
        /// </summary>
        /// <param name="qualitative">quality label of the resolution</param>
        public PageResolution(PageQualitativeResolution qualitative)
        {
            _x = null;
            _y = null;
            _qualitative = qualitative;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PageResolution"/> with the specified resolution values
        /// and quality label.
        /// </summary>
        /// <param name="resolutionX">x component of the resolution (in DPI unit)</param>
        /// <param name="resolutionY">y component of the resolution (in DPI unit)</param>
        /// <param name="qualitative">quality label of the resolution</param>
        public PageResolution(int resolutionX, int resolutionY, PageQualitativeResolution qualitative)
        {
            _x = resolutionX;
            _y = resolutionY;
            _qualitative = qualitative;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the x component of the resolution (in DPI unit). Returns null if the x component is not specified.
        /// </summary>
        public Nullable<int> X
        {
            get
            {
                return _x;
            }
        }

        /// <summary>
        /// Gets the y component of the resolution (in DPI unit). Returns null if the y component is not specified.
        /// </summary>
        public Nullable<int> Y
        {
            get
            {
                return _y;
            }
        }

        /// <summary>
        /// Gets the quality label of the resolution. Returns null if the quality label is not specified.
        /// </summary>
        public Nullable<PageQualitativeResolution> QualitativeResolution
        {
            get
            {
                return _qualitative;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the page resolution setting to human-readable string.
        /// </summary>
        /// <returns>String that shows the page resolution setting.</returns>
        public override string ToString()
        {
            return ((X != null) ? String.Format(CultureInfo.CurrentCulture, "{0}", X) : "Null") +
                   " x " +
                   ((Y != null) ? String.Format(CultureInfo.CurrentCulture, "{0}", Y) : "Null") +
                   " (QualitativeResolution: " +
                   ((QualitativeResolution != null) ? QualitativeResolution.ToString() : "Null") +
                   ")";
        }

        #endregion Public Methods

        #region Private Fields

        private Nullable<int> _x;
        private Nullable<int> _y;
        private Nullable<PageQualitativeResolution> _qualitative;

        #endregion private Fields
    }

    /// <summary>
    /// Represents print settings.
    /// </summary>
    public sealed class PrintTicket : System.ComponentModel.INotifyPropertyChanged
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of <see cref="PrintTicket"/> with no print settings.
        /// </summary>
        public PrintTicket()
        {
            _printTicket = new InternalPrintTicket();
            _setterCache = new Dictionary<CapabilityName, object>();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PrintTicket"/> with settings based on the XML form of PrintTicket.
        /// </summary>
        /// <param name="xmlStream"><see cref="Stream"/> object containing the XML form of PrintTicket.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="xmlStream"/> parameter is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// The <see cref="Stream"/> object specified by <paramref name="xmlStream"/> parameter doesn't contain a
        /// well-formed XML PrintTicket. The exception object's <see cref="Exception.Message"/> property describes
        /// why the XML content is not well-formed PrintTicket. If not null, the exception object's
        /// <see cref="Exception.InnerException"/> property provides more details.
        /// </exception>
        /// <remarks>
        /// This constructor doesn't reset the current position of the stream back to the original position
        /// before the constructor is called. It's the caller's responsibility to reset the stream current position
        /// if needed.
        /// </remarks>
        public PrintTicket(Stream xmlStream)
        {
            _printTicket = new InternalPrintTicket(xmlStream);
            _setterCache = new Dictionary<CapabilityName, object>();
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates a duplicate of the current <see cref="PrintTicket"/> object.
        /// </summary>
        /// <returns>The duplicated <see cref="PrintTicket"/> object.</returns>
        public PrintTicket Clone()
        {
            PrintTicket clonePT = new PrintTicket(this.GetXmlStream());

            return clonePT;
        }

        /// <summary>
        /// Gets a <see cref="MemoryStream"/> object that contains the XML form of current <see cref="PrintTicket"/> object.
        /// </summary>
        /// <returns>The <see cref="MemoryStream"/> object that contains the XML form of current <see cref="PrintTicket"/> object.</returns>
        public MemoryStream GetXmlStream()
        {
            ExecuteCachedSetters();

            return _printTicket.XmlStream;
        }

        /// <summary>
        /// Saves the XML form of current <see cref="PrintTicket"/> object to the specified stream.
        /// </summary>
        /// <param name="outStream">The <see cref="Stream"/> object to which you want to save.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="outStream"/> parameter is null.
        /// </exception>
        /// <remarks>
        /// This function doesn't reset the current position of the stream back to the original position
        /// before the function is called. It's the caller's responsibility to reset the stream current position
        /// if needed.
        /// </remarks>
        public void SaveTo(Stream outStream)
        {
            ExecuteCachedSetters();

            _printTicket.SaveTo(outStream);
        }

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// Gets or sets the document collate setting.
        /// </summary>
        /// <remarks>
        /// If the document collate setting is not specified, this property will return null.
        /// If caller sets this property to null, document collate setting will become unspecified and
        /// document collate setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="Collation"/> values.
        /// </exception>
        public Nullable<Collation> Collation
        {
            get
            {
                Collation valueGot = GetEnumValueFromCacheOrXml<Collation>(
                                         CapabilityName.DocumentCollate,
                                         _printTicket.DocumentCollate.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.CollationEnumMin ||
                     value > PrintSchema.CollationEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<Collation>(
                    CapabilityName.DocumentCollate,
                    (value == null) ? (Collation)PrintSchema.EnumUnspecifiedValue : (Collation)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("Collation");
            }
        }

        /// <summary>
        /// Gets or sets the job copy count setting.
        /// </summary>
        /// <remarks>
        /// If the job copy count setting is not specified, this property will return null.
        /// If caller sets this property to null, job copy count setting will become unspecified and
        /// job copy count setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive value.
        /// </exception>
        public Nullable<int> CopyCount
        {
            get
            {
                int copyValue = _printTicket.JobCopyCount.Value;

                if (copyValue > 0)
                    return copyValue;
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    _printTicket.JobCopyCount.ClearSetting();
                }
                else
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException("value",
                                      PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }

                    // no need to cache set calls
                    _printTicket.JobCopyCount.Value = (int)value;
                }

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("CopyCount");
            }
        }

        /// <summary>
        /// Gets or sets the device font substitution setting.
        /// </summary>
        /// <remarks>
        /// If the device font substitution setting is not specified, this property will return null.
        /// If caller sets this property to null, device font substitution setting will become unspecified and
        /// device font substitution setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="DeviceFontSubstitution"/> values.
        /// </exception>
        public Nullable<DeviceFontSubstitution> DeviceFontSubstitution
        {
            get
            {
                DeviceFontSubstitution valueGot = GetEnumValueFromCacheOrXml<DeviceFontSubstitution>(
                                                      CapabilityName.PageDeviceFontSubstitution,
                                                      _printTicket.PageDeviceFontSubstitution.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.DeviceFontSubstitutionEnumMin ||
                     value > PrintSchema.DeviceFontSubstitutionEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<DeviceFontSubstitution>(
                    CapabilityName.PageDeviceFontSubstitution,
                    (value == null) ? (DeviceFontSubstitution)PrintSchema.EnumUnspecifiedValue : (DeviceFontSubstitution)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("DeviceFontSubstitution");
            }
        }

        /// <summary>
        /// Gets or sets the job duplex setting.
        /// </summary>
        /// <remarks>
        /// If the job duplex setting is not specified, this property will return null.
        /// If caller sets this property to null, job duplex setting will become unspecified and
        /// job duplex setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="Duplexing"/> values.
        /// </exception>
        public Nullable<Duplexing> Duplexing
        {
            get
            {
                Duplexing valueGot = GetEnumValueFromCacheOrXml<Duplexing>(
                                         CapabilityName.JobDuplex,
                                         _printTicket.JobDuplex.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.DuplexingEnumMin ||
                     value > PrintSchema.DuplexingEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<Duplexing>(
                    CapabilityName.JobDuplex,
                    (value == null) ? (Duplexing)PrintSchema.EnumUnspecifiedValue : (Duplexing)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("Duplexing");
            }
        }

        /// <summary>
        /// Gets or sets the input bin setting.
        /// </summary>
        /// <remarks>
        /// Print Schema defines three input bin settings: JobInputBin, DocumentInputBin and PageInputBin.
        /// 
        /// If none of the JobInputBin, DocumentInputBin or PageInputBin setting is specified in the XML form of current
        /// <see cref="PrintTicket"/> object, then this property will return null. Otherwise, this property will return
        /// the input bin setting for JobInputBin if JobInputBin is specified, or for DocumentInputBin if DocumentInputBin
        /// is specified, or for PageInputBin.
        /// 
        /// If caller sets this property to null, input bin setting will become unspecified and any JobInputBin, DocumentInputBin,
        /// or PageInputBin setting content in the XML form of current <see cref="PrintTicket"/> object will be removed.
        /// 
        /// If caller sets this property to a standard <see cref="InputBin"/> value, then in the XML form of current
        /// <see cref="PrintTicket"/> object, JobInputBin setting will be set to the specified value, and any DocumentInputBin or
        /// PageInputBin setting content will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="InputBin"/> values.
        /// </exception>
        public Nullable<InputBin> InputBin
        {
            get
            {
                // get the JobInputBin setting
                InputBin valueGot = GetEnumValueFromCacheOrXml<InputBin>(
                                        CapabilityName.JobInputBin,
                                        _printTicket.JobInputBin.Value);

                // if no JobInputBin setting, then get the DocumentInputBin setting
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                {
                    valueGot = GetEnumValueFromCacheOrXml<InputBin>(
                                   CapabilityName.DocumentInputBin,
                                   _printTicket.DocumentInputBin.Value);
                }

                // if neither JobInputBin nor DocumentInputBin setting exists, then get the PageInputBin setting
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                {
                    valueGot = GetEnumValueFromCacheOrXml<InputBin>(
                                   CapabilityName.PageInputBin,
                                   _printTicket.PageInputBin.Value);
                }

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.InputBinEnumMin ||
                    value > PrintSchema.InputBinEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                // always set the job-scope input bin setting to increase portability
                AddSetterEnumValueToCache<InputBin>(
                    CapabilityName.JobInputBin,
                    (value == null) ? (InputBin)PrintSchema.EnumUnspecifiedValue : (InputBin)value);

                // need to remove document-scope and page-scope input bin setting so only job-scope
                // input bin setting will be persisted
                AddSetterEnumValueToCache<InputBin>(
                    CapabilityName.DocumentInputBin, (InputBin)PrintSchema.EnumUnspecifiedValue);
                AddSetterEnumValueToCache<InputBin>(
                    CapabilityName.PageInputBin, (InputBin)PrintSchema.EnumUnspecifiedValue);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("InputBin");
            }
        }

        /// <summary>
        /// Gets or sets the page output color setting.
        /// </summary>
        /// <remarks>
        /// If the page output color setting is not specified, this property will return null.
        /// If caller sets this property to null, page output color setting will become unspecified and
        /// page output color setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="OutputColor"/> values.
        /// </exception>
        public Nullable<OutputColor> OutputColor
        {
            get
            {
                OutputColor valueGot = GetEnumValueFromCacheOrXml<OutputColor>(
                                           CapabilityName.PageOutputColor,
                                           _printTicket.PageOutputColor.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.OutputColorEnumMin ||
                     value > PrintSchema.OutputColorEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<OutputColor>(
                    CapabilityName.PageOutputColor,
                    (value == null) ? (OutputColor)PrintSchema.EnumUnspecifiedValue : (OutputColor)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("OutputColor");
            }
        }

        /// <summary>
        /// Gets or sets the page output quality setting.
        /// </summary>
        /// <remarks>
        /// If the page output quality setting is not specified, this property will return null.
        /// If caller sets this property to null, page output quality setting will become unspecified and
        /// page output quality setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="OutputQuality"/> values.
        /// </exception>
        public Nullable<OutputQuality> OutputQuality
        {
            get
            {
                OutputQuality valueGot = GetEnumValueFromCacheOrXml<OutputQuality>(
                                             CapabilityName.PageOutputQuality,
                                             _printTicket.PageOutputQuality.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.OutputQualityEnumMin ||
                     value > PrintSchema.OutputQualityEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<OutputQuality>(
                    CapabilityName.PageOutputQuality,
                    (value == null) ? (OutputQuality)PrintSchema.EnumUnspecifiedValue : (OutputQuality)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("OutputQuality");
            }
        }

        /// <summary>
        /// Gets or sets the page borderless setting.
        /// </summary>
        /// <remarks>
        /// If the page borderless setting is not specified, this property will return null.
        /// If caller sets this property to null, page borderless setting will become unspecified and
        /// page borderless setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageBorderless"/> values.
        /// </exception>
        public Nullable<PageBorderless> PageBorderless
        {
            get
            {
                PageBorderless valueGot = GetEnumValueFromCacheOrXml<PageBorderless>(
                                              CapabilityName.PageBorderless,
                                              _printTicket.PageBorderless.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.PageBorderlessEnumMin ||
                     value > PrintSchema.PageBorderlessEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<PageBorderless>(
                    CapabilityName.PageBorderless,
                    (value == null) ? (PageBorderless)PrintSchema.EnumUnspecifiedValue : (PageBorderless)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageBorderless");
            }
        }

        /// <summary>
        /// Gets or sets the page media size setting.
        /// </summary>
        /// <remarks>
        /// If the page media size setting is not specified, this property will return null.
        /// If caller sets this property to null, page media size setting will become unspecified and
        /// page media size setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Set call doesn't set either <see ref="PageMediaSize.PageMediaSizeName"/> property or the pair
        /// of <see ref="PageMediaSize.Width"/> and <see ref="PageMediaSize.Height"/> properties.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set <see ref="PageMediaSize.PageMediaSizeName"/> property is not one of the
        /// standard <see cref="PageMediaSizeName"/> values.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set <see ref="PageMediaSize.Width"/> property or <see ref="PageMediaSize.Height"/>
        /// property is not a positive value.
        /// </exception>
        public PageMediaSize PageMediaSize
        {
            get
            {
                // return the most recent cached set operation if it exists
                if ((_setterCache != null) &&
                    _setterCache.ContainsKey(CapabilityName.PageMediaSize))
                {
                    object cacheObj;

                    _setterCache.TryGetValue(CapabilityName.PageMediaSize, out cacheObj);

                    return (PageMediaSize)cacheObj;
                }

                // if we fail to get the cached set operation, then we return based on XML content
                if ((_printTicket.PageMediaSize.MediaSizeWidth > 0) &&
                    (_printTicket.PageMediaSize.MediaSizeHeight > 0))
                {
                    if ((int)_printTicket.PageMediaSize.Value != PrintSchema.EnumUnspecifiedValue)
                    {
                        // There are valid media size name and width/height values
                        return new PageMediaSize(_printTicket.PageMediaSize.Value,
                                                 _printTicket.PageMediaSize.MediaSizeWidth,
                                                 _printTicket.PageMediaSize.MediaSizeHeight);
                    }
                    else
                    {
                        // There are only valid width/height values
                        return new PageMediaSize(_printTicket.PageMediaSize.MediaSizeWidth,
                                                 _printTicket.PageMediaSize.MediaSizeHeight);
                    }
                }
                else if ((int)_printTicket.PageMediaSize.Value != PrintSchema.EnumUnspecifiedValue)
                {
                    // No valid width/height values but we do have valid meida size name value
                    return new PageMediaSize(_printTicket.PageMediaSize.Value);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                // first performing input parameter validation
                if (value != null)
                {
                    // either media name or the width/height values need to be specified
                    if ((value.PageMediaSizeName == null) &&
                        (value.Width == null || value.Height == null))
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }

                    // if size name is specified, it needs to be valid name
                    if ((value.PageMediaSizeName != null) &&
                        (value.PageMediaSizeName < PrintSchema.PageMediaSizeNameEnumMin ||
                         value.PageMediaSizeName > PrintSchema.PageMediaSizeNameEnumMax))
                    {
                        throw new ArgumentOutOfRangeException("value.PageMediaSizeName");
                    }

                    // if width or height value is specified, it needs to be positive
                    if ((value.Width != null) && (value.Width <= 0))
                    {
                        throw new ArgumentOutOfRangeException("value",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }

                    if ((value.Height != null) && (value.Height <= 0))
                    {
                        throw new ArgumentOutOfRangeException("value",
                                  PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }
                }

                // then adding the set operation to the cache (replacing older set operations)
                if (_setterCache.ContainsKey(CapabilityName.PageMediaSize))
                {
                    _setterCache.Remove(CapabilityName.PageMediaSize);
                }

                _setterCache.Add(CapabilityName.PageMediaSize, value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageMediaSize");
            }
        }

        /// <summary>
        /// Gets or sets the page media type setting.
        /// </summary>
        /// <remarks>
        /// If the page media type setting is not specified, this property will return null.
        /// If caller sets this property to null, page media type setting will become unspecified and
        /// page media type setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageMediaType"/> values.
        /// </exception>
        public Nullable<PageMediaType> PageMediaType
        {
            get
            {
                PageMediaType valueGot = GetEnumValueFromCacheOrXml<PageMediaType>(
                                             CapabilityName.PageMediaType,
                                             _printTicket.PageMediaType.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.PageMediaTypeEnumMin ||
                     value > PrintSchema.PageMediaTypeEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<PageMediaType>(
                    CapabilityName.PageMediaType,
                    (value == null) ? (PageMediaType)PrintSchema.EnumUnspecifiedValue : (PageMediaType)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageMediaType");
            }
        }

        /// <summary>
        /// Gets or sets the job page order setting.
        /// </summary>
        /// <remarks>
        /// If the job page order setting is not specified, this property will return null.
        /// If caller sets this property to null, job page order setting will become unspecified and
        /// job page order setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageOrder"/> values.
        /// </exception>
        public Nullable<PageOrder> PageOrder
        {
            get
            {
                PageOrder valueGot = GetEnumValueFromCacheOrXml<PageOrder>(
                                         CapabilityName.JobPageOrder,
                                         _printTicket.JobPageOrder.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.PageOrderEnumMin ||
                     value > PrintSchema.PageOrderEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<PageOrder>(
                    CapabilityName.JobPageOrder,
                    (value == null) ? (PageOrder)PrintSchema.EnumUnspecifiedValue : (PageOrder)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageOrder");
            }
        }

        /// <summary>
        /// Gets or sets the page orientation setting.
        /// </summary>
        /// <remarks>
        /// If the page orientation setting is not specified, this property will return null.
        /// If caller sets this property to null, page orientation setting will become unspecified and
        /// page orientation setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PageOrientation"/> values.
        /// </exception>
        public Nullable<PageOrientation> PageOrientation
        {
            get
            {
                PageOrientation valueGot = GetEnumValueFromCacheOrXml<PageOrientation>(
                                               CapabilityName.PageOrientation,
                                               _printTicket.PageOrientation.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.PageOrientationEnumMin ||
                     value > PrintSchema.PageOrientationEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<PageOrientation>(
                    CapabilityName.PageOrientation,
                    (value == null) ? (PageOrientation)PrintSchema.EnumUnspecifiedValue : (PageOrientation)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageOrientation");
            }
        }

        /// <summary>
        /// Gets or sets the page resolution setting.
        /// </summary>
        /// <remarks>
        /// If the page resolution setting is not specified, this property will return null.
        /// If caller sets this property to null, page resolution setting will become unspecified and
        /// page resolution setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Set call doesn't set either <see ref="PageResolution.QualitativeResolution"/> property or the pair
        /// of <see ref="PageResolution.X"/> and <see ref="PageResolution.Y"/> properties.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set <see ref="PageResolution.QualitativeResolution"/> property is not one of the
        /// standard <see cref="PageQualitativeResolution"/> values.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set <see ref="PageResolution.X"/> property or <see ref="PageResolution.Y"/>
        /// property is not a positive value.
        /// </exception>
        public PageResolution PageResolution
        {
            get
            {
                // return the most recent cached set operation if it exists
                if ((_setterCache != null) &&
                    _setterCache.ContainsKey(CapabilityName.PageResolution))
                {
                    object cacheObj;

                    _setterCache.TryGetValue(CapabilityName.PageResolution, out cacheObj);

                    return (PageResolution)cacheObj;
                }

                // if we fail to get the cached set operation, then we return based on XML content
                if ((_printTicket.PageResolution.ResolutionX > 0) &&
                    (_printTicket.PageResolution.ResolutionY > 0))
                {
                    if ((int)_printTicket.PageResolution.QualitativeResolution != PrintSchema.EnumUnspecifiedValue)
                    {
                        // There are valid x/y and quality label values.
                        return new PageResolution(_printTicket.PageResolution.ResolutionX,
                                                  _printTicket.PageResolution.ResolutionY,
                                                  _printTicket.PageResolution.QualitativeResolution);
                    }
                    else
                    {
                        // There are only valid x/y values.
                        return new PageResolution(_printTicket.PageResolution.ResolutionX,
                                                  _printTicket.PageResolution.ResolutionY);
                    }
                }
                else if ((int)_printTicket.PageResolution.QualitativeResolution != PrintSchema.EnumUnspecifiedValue)
                {
                    // No valid x/y values but we do have valid quality label value.
                    return new PageResolution(_printTicket.PageResolution.QualitativeResolution);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                // first performing input parameter validation
                if (value != null)
                {
                    if ((value.QualitativeResolution == null) &&
                        (value.X == null || value.Y == null))
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }

                    // If quality lable is specified, it needs to be valid value
                    if ((value.QualitativeResolution != null) &&
                        (value.QualitativeResolution < PrintSchema.PageQualitativeResolutionEnumMin ||
                         value.QualitativeResolution > PrintSchema.PageQualitativeResolutionEnumMax))
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }

                    // If specified, resolution X and Y values must be positive
                    if ((value.X != null) && (value.X <= 0))
                    {
                        throw new ArgumentOutOfRangeException("value",
                                      PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }

                    if ((value.Y != null) && (value.Y <= 0))
                    {
                        throw new ArgumentOutOfRangeException("value",
                                      PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }
                }

                // then adding the set operation to the cache (replacing older set operations)
                if (_setterCache.ContainsKey(CapabilityName.PageResolution))
                {
                    _setterCache.Remove(CapabilityName.PageResolution);
                }

                _setterCache.Add(CapabilityName.PageResolution, value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageResolution");
            }
        }

        /// <summary>
        /// Gets or sets the page scaling factor (in percentage unit) setting.
        /// </summary>
        /// <remarks>
        /// If the page scaling factor setting is not specified, this property will return null.
        /// If caller sets this property to null, page scaling factor setting will become unspecified and
        /// page scaling factor setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive value.
        /// </exception>
        public Nullable<int> PageScalingFactor
        {
            get
            {
                if ((_printTicket.PageScaling.Value == PageScaling.CustomSquare) &&
                    (_printTicket.PageScaling.CustomSquareScale > 0))
                {
                    return _printTicket.PageScaling.CustomSquareScale;
                }
                else if ((_printTicket.PageScaling.Value == PageScaling.Custom) &&
                         (_printTicket.PageScaling.CustomScaleWidth > 0))
                {
                    return _printTicket.PageScaling.CustomScaleWidth;
                }
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    _printTicket.PageScaling.ClearSetting();
                }
                else
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException("value",
                                      PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }

                    // no need to cache set calls
                    _printTicket.PageScaling.SetCustomSquareScaling((int)value);
                }

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PageScalingFactor");
            }
        }

        /// <summary>
        /// Gets or sets the job pages-per-sheet setting.
        /// </summary>
        /// <remarks>
        /// If the job pages-per-sheet setting is not specified, this property will return null.
        /// If caller sets this property to null, job pages-per-sheet setting will become unspecified and
        /// job pages-per-sheet setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// (Pages-per-sheet is to output multiple logical pages to a single physical sheet.)
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not a positive value.
        /// </exception>
        public Nullable<int> PagesPerSheet
        {
            get
            {
                int nupValue = _printTicket.JobNUp.PagesPerSheet;

                if (nupValue > 0)
                    return nupValue;
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    _printTicket.JobNUp.ClearSetting();
                }
                else
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException("value",
                                      PTUtility.GetTextFromResource("ArgumentException.PositiveValue"));
                    }

                    // no need to cache set calls
                    _printTicket.JobNUp.PagesPerSheet = (int)value;
                }

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PagesPerSheet");
            }
        }

        /// <summary>
        /// Gets or sets the job pages-per-sheet direction setting.
        /// </summary>
        /// <remarks>
        /// If the job pages-per-sheet direction setting is not specified, this property will return null.
        /// If caller sets this property to null, job pages-per-sheet direction setting will become unspecified and
        /// job pages-per-sheet direction setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// (Pages-per-sheet is to output multiple logical pages to a single physical sheet.)
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PagesPerSheetDirection"/> values.
        /// </exception>
        public Nullable<PagesPerSheetDirection> PagesPerSheetDirection
        {
            get
            {
                PagesPerSheetDirection valueGot = GetEnumValueFromCacheOrXml<PagesPerSheetDirection>(
                                         CapabilityName.JobNUp,
                                         _printTicket.JobNUp.PresentationDirection.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.PagesPerSheetDirectionEnumMin ||
                     value > PrintSchema.PagesPerSheetDirectionEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<PagesPerSheetDirection>(
                    CapabilityName.JobNUp,
                    (value == null) ? (PagesPerSheetDirection)PrintSchema.EnumUnspecifiedValue : (PagesPerSheetDirection)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PagesPerSheetDirection");
            }
        }

        /// <summary>
        /// Gets or sets the page photo printing intent setting.
        /// </summary>
        /// <remarks>
        /// If the page photo printing intent setting is not specified, this property will return null.
        /// If caller sets this property to null, page photo printing intent setting will become unspecified and
        /// page photo printing intent setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="PhotoPrintingIntent"/> values.
        /// </exception>
        public Nullable<PhotoPrintingIntent> PhotoPrintingIntent
        {
            get
            {
                PhotoPrintingIntent valueGot = GetEnumValueFromCacheOrXml<PhotoPrintingIntent>(
                                                   CapabilityName.PagePhotoPrintingIntent,
                                                   _printTicket.PagePhotoPrintingIntent.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.PhotoPrintingIntentEnumMin ||
                     value > PrintSchema.PhotoPrintingIntentEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<PhotoPrintingIntent>(
                    CapabilityName.PagePhotoPrintingIntent,
                    (value == null) ? (PhotoPrintingIntent)PrintSchema.EnumUnspecifiedValue : (PhotoPrintingIntent)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("PhotoPrintingIntent");
            }
        }

        /// <summary>
        /// Gets or sets the job output stapling setting.
        /// </summary>
        /// <remarks>
        /// If the job output stapling setting is not specified, this property will return null.
        /// If caller sets this property to null, job output stapling setting will become unspecified and
        /// job output stapling setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="Stapling"/> values.
        /// </exception>
        public Nullable<Stapling> Stapling
        {
            get
            {
                Stapling valueGot = GetEnumValueFromCacheOrXml<Stapling>(
                                         CapabilityName.JobStaple,
                                         _printTicket.JobStaple.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.StaplingEnumMin ||
                     value > PrintSchema.StaplingEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<Stapling>(
                    CapabilityName.JobStaple,
                    (value == null) ? (Stapling)PrintSchema.EnumUnspecifiedValue : (Stapling)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("Stapling");
            }
        }

        /// <summary>
        /// Gets or sets the TrueType font handling mode setting.
        /// </summary>
        /// <remarks>
        /// If the TrueType font handling mode setting is not specified, this property will return null.
        /// If caller sets this property to null, TrueType font handling mode setting will become unspecified and
        /// TrueType font handling mode setting content in the XML form of current <see cref="PrintTicket"/> object
        /// will be removed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value to set is not one of the standard <see cref="TrueTypeFontMode"/> values.
        /// </exception>
        public Nullable<TrueTypeFontMode> TrueTypeFontMode
        {
            get
            {
                TrueTypeFontMode valueGot = GetEnumValueFromCacheOrXml<TrueTypeFontMode>(
                                                CapabilityName.PageTrueTypeFontMode,
                                                _printTicket.PageTrueTypeFontMode.Value);

                // handle Unspecified => null conversion
                if ((int)valueGot == PrintSchema.EnumUnspecifiedValue)
                    return null;
                else
                    return valueGot;
            }
            set
            {
                if ((value != null) &&
                    (value < PrintSchema.TrueTypeFontModeEnumMin ||
                     value > PrintSchema.TrueTypeFontModeEnumMax))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                AddSetterEnumValueToCache<TrueTypeFontMode>(
                    CapabilityName.PageTrueTypeFontMode,
                    (value == null) ? (TrueTypeFontMode)PrintSchema.EnumUnspecifiedValue : (TrueTypeFontMode)value);

                // Raise PropertyChanged event for IPropertyChange implementation
                NotifyPropertyChanged("TrueTypeFontMode");
            }
        }

        #endregion Public Properties

        #region IPropertyChange implementation

        /// <summary>
        ///
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called for any public property as it is getting changed
        /// to notify anyone registered for property change notifications that the
        /// property has changed.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        private void NotifyPropertyChanged(string name)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }

            _isSettingChanged = true;
        }

        #endregion IPropertyChange implementation

        #region Internal Properties

        /// <summary>
        /// Gets a boolean to indicate whether or not public setting in the PrinTicket has been changed by the client.
        /// </summary>
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        internal bool IsSettingChanged
        {
            get
            {
                return _isSettingChanged;
            }
        }

        #endregion Internal Properties

        #region Internal Methods
        
        // Like PTProvider.cs:PackStreamToNativeBuffer().
        internal string ToXmlString()
        {
            MemoryStream ms = new MemoryStream();

            // We might be tempted to drop the base-encoded DEVMODE string
            // ('<psf:ParameterInit name="ns0001:PageDevmodeSnapshot">')
            // from the PrintTicket when saving it to the stream.  However,
            // two PrintTicket's can have the same public fields but still be
            // different due to DEVMODE -- this can happen when PrintTicket's
            // are generated from DEVMODE's, and cannot directly reflect all the
            // information from DEVMODE's
            this.SaveTo(ms);

            // windows/wcp/Print/Reach/PrintConfig/PrtTicket_Public.cs:InternalPrintTicket()
            // assumes the XML is UTF8-encoded so this is safe.
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        
        #endregion Internal Methods
        
        #region Private Methods

        private EnumType GetEnumValueFromCacheOrXml<EnumType>(CapabilityName feature,
                                                              EnumType xmlValue)
        {
            EnumType valueGot;

            if ((_setterCache != null) &&
                _setterCache.ContainsKey(feature))
            {
                object cacheObj;

                _setterCache.TryGetValue(feature, out cacheObj);

                valueGot = (EnumType)cacheObj;
            }
            else
            {
                valueGot = xmlValue;
            }

            return valueGot;
        }

        private void AddSetterEnumValueToCache<EnumType>(CapabilityName feature,
                                                         EnumType value)
        {
            // Newer value replaces old value in the cache.
            if (_setterCache.ContainsKey(feature))
            {
                _setterCache.Remove(feature);
            }

            _setterCache.Add(feature, value);
        }

        private void ExecuteMediaSizeSetter(object cacheValue)
        {
            if (cacheValue == null)
            {
                _printTicket.PageMediaSize.ClearSetting();
            }
            else
            {
                PageMediaSize mediaSize = (PageMediaSize)cacheValue;

                if (mediaSize.PageMediaSizeName != null)
                {
                    // If we have non-null media name, the property setter validation code should have verified
                    // it's not the special Unspecified value of 0.
                    if ((int)mediaSize.PageMediaSizeName != PrintSchema.EnumUnknownValue)
                    {
                        if ((mediaSize.Width != null) && (mediaSize.Height != null))
                        {
                            _printTicket.PageMediaSize.SetFixedMediaSize((PageMediaSizeName)mediaSize.PageMediaSizeName,
                                                                         (double)mediaSize.Width, (double)mediaSize.Height);
                        }
                        else
                        {
                            _printTicket.PageMediaSize.SetFixedMediaSize((PageMediaSizeName)mediaSize.PageMediaSizeName);
                        }
                    }
                    else
                    {
                        if ((mediaSize.Width != null) && (mediaSize.Height != null))
                        {
                            _printTicket.PageMediaSize.SetFixedMediaSize((double)mediaSize.Width, (double)mediaSize.Height);
                        }
                        else
                        {
                            // no-op
                        }
                    }
                }
                else
                {
                    // If we have no valid media name, the property setter validation code should have already
                    // guaranteed any cached set operation will have valid width/height values.
                    _printTicket.PageMediaSize.SetFixedMediaSize((double)mediaSize.Width, (double)mediaSize.Height);
                }
            }
        }

        private void ExecuteResolutionSetter(object cacheValue)
        {
            if (cacheValue == null)
            {
                _printTicket.PageResolution.ClearSetting();
            }
            else
            {
                PageResolution resoluton = (PageResolution)cacheValue;

                if ((resoluton.X != null) || (resoluton.Y != null) ||
                    ((resoluton.QualitativeResolution != null) &&
                     ((int)resoluton.QualitativeResolution != PrintSchema.EnumUnknownValue)))
                {
                    // If any of the 3 properties are set by client, we need to do a ClearSetting before setting
                    // the correponding scored properties, since PageResolution scored property setters only affect
                    // their own XML fragment.
                    _printTicket.PageResolution.ClearSetting();
                }

                if (resoluton.X != null)
                    _printTicket.PageResolution.ResolutionX = (int)resoluton.X;

                if (resoluton.Y != null)
                    _printTicket.PageResolution.ResolutionY = (int)resoluton.Y;

                if ((resoluton.QualitativeResolution != null) &&
                    ((int)resoluton.QualitativeResolution != PrintSchema.EnumUnknownValue))
                {
                    // If we have non-null quality label, the property setter validation code should have verified
                    // it's not the special Unspecified value of 0.
                    _printTicket.PageResolution.QualitativeResolution =
                        (PageQualitativeResolution)resoluton.QualitativeResolution;
                }
            }
        }

        private void ExecuteGeneralEnumSetters(CapabilityName feature, object cacheValue)
        {
            if ((int)cacheValue == PrintSchema.EnumUnspecifiedValue)
            {
                _printTicket.GetBasePTFeatureObject(feature).ClearSetting();
            }
            else if ((int)cacheValue != PrintSchema.EnumUnknownValue)
            {
                // If the value to set is EnumUnknownValue, then we treat the set call as no-op.
                switch (feature)
                {
                    case CapabilityName.DocumentCollate:
                        _printTicket.DocumentCollate.Value = (Collation)cacheValue;
                        break;

                    case CapabilityName.PageDeviceFontSubstitution:
                        _printTicket.PageDeviceFontSubstitution.Value =
                            (DeviceFontSubstitution)cacheValue;
                        break;

                    case CapabilityName.JobDuplex:
                        _printTicket.JobDuplex.Value = (Duplexing)cacheValue;
                        break;

                    case CapabilityName.JobInputBin:
                        _printTicket.JobInputBin.Value = (InputBin)cacheValue;
                        break;

                    case CapabilityName.PageOutputColor:
                        _printTicket.PageOutputColor.Value = (OutputColor)cacheValue;
                        break;

                    case CapabilityName.PageOutputQuality:
                        _printTicket.PageOutputQuality.Value = (OutputQuality)cacheValue;
                        break;

                    case CapabilityName.PageBorderless:
                        _printTicket.PageBorderless.Value = (PageBorderless)cacheValue;
                        break;

                    case CapabilityName.PageMediaType:
                        _printTicket.PageMediaType.Value = (PageMediaType)cacheValue;
                        break;

                    case CapabilityName.JobPageOrder:
                        _printTicket.JobPageOrder.Value = (PageOrder)cacheValue;
                        break;

                    case CapabilityName.PageOrientation:
                        _printTicket.PageOrientation.Value = (PageOrientation)cacheValue;
                        break;

                    case CapabilityName.JobNUp:
                        _printTicket.JobNUp.PresentationDirection.Value =
                            (PagesPerSheetDirection)cacheValue;
                        break;

                    case CapabilityName.PagePhotoPrintingIntent:
                        _printTicket.PagePhotoPrintingIntent.Value = (PhotoPrintingIntent)cacheValue;
                        break;

                    case CapabilityName.JobStaple:
                        _printTicket.JobStaple.Value = (Stapling)cacheValue;
                        break;

                    case CapabilityName.PageTrueTypeFontMode:
                        _printTicket.PageTrueTypeFontMode.Value = (TrueTypeFontMode)cacheValue;
                        break;

                    default:
                        break;
                }
            }
        }

        private void ExecuteCachedSetters()
        {
            object cacheValue;

            // Console.WriteLine("\n --- Executing Setters ---\n");
            foreach (CapabilityName feature in _setterCache.Keys)
            {
                _setterCache.TryGetValue(feature, out cacheValue);

                // Console.WriteLine("{0} - {1}", feature, cacheValue);

                if (feature == CapabilityName.PageMediaSize)
                {
                    ExecuteMediaSizeSetter(cacheValue);
                }
                else if (feature == CapabilityName.PageResolution)
                {
                    ExecuteResolutionSetter(cacheValue);
                }
                else
                {
                    ExecuteGeneralEnumSetters(feature, cacheValue);
                }
            }

            _setterCache.Clear();
        }

        #endregion Private Methods

        #region Private Fields

        // simple PT/PC API is based on internal object-hierarchy based PT/PC classes
        private InternalPrintTicket _printTicket;

        // bool to indicate whether or not any public setting in the PrinTicket has been changed by the client
        private bool _isSettingChanged;

        // dictionary to cache PrintTicket setter calls. The cache is needed to defer changing the content of the
        // underlying PrintTicket XML until client requests serialization of the XML content.
        private Dictionary<CapabilityName, object> _setterCache;

        #endregion Private Fields
    }
}
