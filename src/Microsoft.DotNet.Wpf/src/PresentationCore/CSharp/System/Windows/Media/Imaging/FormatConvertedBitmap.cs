// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//


using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Security;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Imaging
{
    #region FormatConvertedBitmap

    /// <summary>
    /// FormatConvertedBitmap provides caching functionality for a BitmapSource.
    /// </summary>
    public sealed partial class FormatConvertedBitmap : Imaging.BitmapSource, ISupportInitialize
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public FormatConvertedBitmap() : base(true)
        {
        }

        /// <summary>
        /// Construct a FormatConvertedBitmap
        /// </summary>
        /// <param name="source">BitmapSource to apply to the format conversion to</param>
        /// <param name="destinationFormat">Destionation Format to  apply to the bitmap</param>
        /// <param name="destinationPalette">Palette if format is palettized</param>
        /// <param name="alphaThreshold">Alpha threshold</param>
        public FormatConvertedBitmap(BitmapSource source, PixelFormat destinationFormat, BitmapPalette destinationPalette, double alphaThreshold)
            : base(true) // Use base class virtuals
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (alphaThreshold < (double)(0.0) || alphaThreshold > (double)(100.0))
            {
                throw new ArgumentException(SR.Get(SRID.Image_AlphaThresholdOutOfRange));
            }

            _bitmapInit.BeginInit();

            Source = source;
            DestinationFormat = destinationFormat;
            DestinationPalette = destinationPalette;
            AlphaThreshold = alphaThreshold;

            _bitmapInit.EndInit();
            FinalizeCreation();
        }

        // ISupportInitialize

        /// <summary>
        /// Prepare the bitmap to accept initialize paramters.
        /// </summary>
        public void BeginInit()
        {
            WritePreamble();
            _bitmapInit.BeginInit();
        }

        /// <summary>
        /// Prepare the bitmap to accept initialize paramters.
        /// </summary>
        public void EndInit()
        {
            WritePreamble();
            _bitmapInit.EndInit();

            IsValidForFinalizeCreation(/* throwIfInvalid = */ true);
            FinalizeCreation();
        }

        private void ClonePrequel(FormatConvertedBitmap otherFormatConvertedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(FormatConvertedBitmap otherFormatConvertedBitmap)
        {
            EndInit();
        }

        ///
        /// Create the unmanaged resources
        ///
        internal override void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle wicFormatter = null;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    IntPtr wicFactory = factoryMaker.ImagingFactoryPtr;

                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateFormatConverter(
                            wicFactory,
                            out wicFormatter));

                    SafeMILHandle internalPalette;
                    if (DestinationPalette != null)
                        internalPalette = DestinationPalette.InternalPalette;
                    else
                        internalPalette = new SafeMILHandle();

                    Guid format = DestinationFormat.Guid;

                    lock (_syncObject)
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICFormatConverter.Initialize(
                                wicFormatter,
                                Source.WicSourceHandle,
                                ref format,
                                DitherType.DitherTypeErrorDiffusion,
                                internalPalette,
                                AlphaThreshold,
                                WICPaletteType.WICPaletteTypeOptimal
                                ));
                    }

                    //
                    // This is just a link in a BitmapSource chain. The memory is being used by
                    // the BitmapSource at the end of the chain, so no memory pressure needs
                    // to be added here.
                    //
                    WicSourceHandle = wicFormatter;

                    // Even if our source is cached, format conversion is expensive and so we'll
                    // always maintain our own cache for the purpose of rendering.
                    _isSourceCached = false;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
                }
                finally
                {
                    if (wicFormatter != null)
                    {
                        wicFormatter.Close();
                    }
                }
            }

            CreationCompleted = true;
            UpdateCachedSettings();
        }

        /// <summary>
        ///     Notification on source changing.
        /// </summary>
        private void SourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                BitmapSource newSource = e.NewValue as BitmapSource;
                _source = newSource;
                RegisterDownloadEventSource(_source);
                _syncObject = (newSource != null) ? newSource.SyncObject : _bitmapInit;
            }
        }

        internal override bool IsValidForFinalizeCreation(bool throwIfInvalid)
        {
            if (Source == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Image_NoArgument, "Source"));
                }
                return false;
            }
            if (DestinationFormat.Palettized)
            {
                if (DestinationPalette == null)
                {
                    if (throwIfInvalid)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Image_IndexedPixelFormatRequiresPalette));
                    }
                    return false;
                }
                else if ((1 << DestinationFormat.BitsPerPixel) < DestinationPalette.Colors.Count)
                {
                    if (throwIfInvalid)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Image_PaletteColorsDoNotMatchFormat));
                    }
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Notification on destination format changing.
        /// </summary>
        private void DestinationFormatPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationFormat = (PixelFormat)e.NewValue;
            }
        }

        /// <summary>
        ///     Notification on destination palette changing.
        /// </summary>
        private void DestinationPalettePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationPalette = e.NewValue as BitmapPalette;
            }
        }

        /// <summary>
        ///     Notification on alpha threshold changing.
        /// </summary>
        private void AlphaThresholdPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _alphaThreshold = (double)e.NewValue;
            }
        }

        /// <summary>
        ///     Coerce Source
        /// </summary>
        private static object CoerceSource(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._source;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce DestinationFormat
        /// </summary>
        private static object CoerceDestinationFormat(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationFormat;
            }
            else
            {
                //
                // If the client is trying to create a FormatConvertedBitmap with a
                // DestinationFormat == PixelFormats.Default, then coerce it to either
                // the Source bitmaps format (providing the Source bitmap is non-null)
                // or the original DP value.
                //
                if (((PixelFormat)value).Format == PixelFormatEnum.Default)
                {
                    if (bitmap.Source != null)
                    {
                        return bitmap.Source.Format;
                    }
                    else
                    {
                        return bitmap._destinationFormat;
                    }
                }
                else
                {
                    return value;
                }
            }
        }

        /// <summary>
        ///     Coerce DestinationPalette
        /// </summary>
        private static object CoerceDestinationPalette(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationPalette;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce Transform
        /// </summary>
        private static object CoerceAlphaThreshold(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._alphaThreshold;
            }
            else
            {
                return value;
            }
        }

        #region Data Members

        private BitmapSource _source;

        private PixelFormat _destinationFormat;

        private BitmapPalette _destinationPalette;

        private double _alphaThreshold;

        #endregion
    }

    #endregion // FormatConvertedBitmap
}
