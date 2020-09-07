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
    #region CroppedBitmap
    /// <summary>
    /// CroppedBitmap provides caching functionality for a BitmapSource.
    /// </summary>
    public sealed partial class CroppedBitmap : Imaging.BitmapSource, ISupportInitialize
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CroppedBitmap() : base(true)
        {
        }

        /// <summary>
        /// Construct a CroppedBitmap
        /// </summary>
        /// <param name="source">BitmapSource to apply to the crop to</param>
        /// <param name="sourceRect">Source rect of the bitmap to use</param>
        public CroppedBitmap(BitmapSource source, Int32Rect sourceRect)
            : base(true) // Use base class virtuals
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            _bitmapInit.BeginInit();

            Source = source;
            SourceRect = sourceRect;

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

        private void ClonePrequel(CroppedBitmap otherCroppedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(CroppedBitmap otherCroppedBitmap)
        {
            EndInit();
        }

        ///
        /// Create the unmanaged resources
        ///
        internal override void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();        
            BitmapSourceSafeMILHandle wicClipper = null;

            Int32Rect rect = SourceRect;
            BitmapSource source = Source;

            if (rect.IsEmpty)
            {
                rect.Width = source.PixelWidth;
                rect.Height = source.PixelHeight;
            }

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    IntPtr wicFactory = factoryMaker.ImagingFactoryPtr;

                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapClipper(
                            wicFactory,
                            out wicClipper));

                    lock (_syncObject)
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICBitmapClipper.Initialize(
                                wicClipper,
                                source.WicSourceHandle,
                                ref rect));
                    }

                    //
                    // This is just a link in a BitmapSource chain. The memory is being used by
                    // the BitmapSource at the end of the chain, so no memory pressure needs
                    // to be added here.
                    //
                    WicSourceHandle = wicClipper;
                    _isSourceCached = source.IsSourceCached;
                    wicClipper = null;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
                }
                finally
                {
                    if (wicClipper != null)
                    {
                        wicClipper.Close();
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

            return true;
        }

        /// <summary>
        ///     Notification on source rect changing.
        /// </summary>
        private void SourceRectPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _sourceRect = (Int32Rect)e.NewValue;
            }
        }

        /// <summary>
        ///     Coerce Source
        /// </summary>
        private static object CoerceSource(DependencyObject d, object value)
        {
            CroppedBitmap bitmap = (CroppedBitmap)d;
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
        ///     Coerce SourceRect
        /// </summary>
        private static object CoerceSourceRect(DependencyObject d, object value)
        {
            CroppedBitmap bitmap = (CroppedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._sourceRect;
            }
            else
            {
                return value;
            }
        }

        #region Data members

        private BitmapSource _source;

        private Int32Rect _sourceRect;

        #endregion
    }

    #endregion // CroppedBitmap
}
