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
    #region ColorConvertedBitmap
    /// <summary>
    /// ColorConvertedBitmap provides caching functionality for a BitmapSource.
    /// </summary>
    public sealed partial class ColorConvertedBitmap : Imaging.BitmapSource, ISupportInitialize
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ColorConvertedBitmap() : base(true)
        {
        }

        /// <summary>
        /// Construct a ColorConvertedBitmap
        /// </summary>
        /// <param name="source">Input BitmapSource to color convert</param>
        /// <param name="sourceColorContext">Source Color Context</param>
        /// <param name="destinationColorContext">Destination Color Context</param>
        /// <param name="format">Destination Pixel format</param>
        public ColorConvertedBitmap(BitmapSource source, ColorContext sourceColorContext, ColorContext destinationColorContext, PixelFormat format)
            : base(true) // Use base class virtuals
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (sourceColorContext == null)
            {
                throw new ArgumentNullException("sourceColorContext");
            }

            if (destinationColorContext == null)
            {
                throw new ArgumentNullException("destinationColorContext");
            }

            _bitmapInit.BeginInit();

            Source = source;
            SourceColorContext = sourceColorContext;
            DestinationColorContext = destinationColorContext;
            DestinationFormat = format;

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

        private void ClonePrequel(ColorConvertedBitmap otherColorConvertedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(ColorConvertedBitmap otherColorConvertedBitmap)
        {
            EndInit();
        }

        ///
        /// Create the unmanaged resources
        ///
        internal override void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle wicConverter = null;

            HRESULT.Check(UnsafeNativeMethods.WICCodec.CreateColorTransform(
                    out wicConverter));

            lock (_syncObject)
            {
                Guid fmtDestFmt = DestinationFormat.Guid;

                HRESULT.Check(UnsafeNativeMethods.WICColorTransform.Initialize(
                        wicConverter,
                        Source.WicSourceHandle,
                        SourceColorContext.ColorContextHandle,
                        DestinationColorContext.ColorContextHandle,
                        ref fmtDestFmt));
            }

            //
            // This is just a link in a BitmapSource chain. The memory is being used by
            // the BitmapSource at the end of the chain, so no memory pressure needs
            // to be added here.
            //
            WicSourceHandle = wicConverter;
            _isSourceCached = Source.IsSourceCached;

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

            if (SourceColorContext == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Color_NullColorContext));
                }
                return false;
            }

            if (DestinationColorContext == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Image_NoArgument, "DestinationColorContext"));
                }
                return false;
            }

            return true;
}

        /// <summary>
        ///     Notification on source colorcontext changing.
        /// </summary>
        private void SourceColorContextPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _sourceColorContext = e.NewValue as ColorContext;
            }
        }

        /// <summary>
        ///     Notification on destination colorcontext changing.
        /// </summary>
        private void DestinationColorContextPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationColorContext = e.NewValue as ColorContext;
            }
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
        ///     Coerce Source
        /// </summary>
        private static object CoerceSource(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
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
        ///     Coerce SourceColorContext
        /// </summary>
        private static object CoerceSourceColorContext(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._sourceColorContext;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce DestinationColorContext
        /// </summary>
        private static object CoerceDestinationColorContext(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationColorContext;
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
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationFormat;
            }
            else
            {
                return value;
            }
        }

        #region Data members

        private BitmapSource _source;

        private ColorContext _sourceColorContext;

        private ColorContext _destinationColorContext;

        private PixelFormat _destinationFormat;

        #endregion
    }

    #endregion // ColorConvertedBitmap
}
