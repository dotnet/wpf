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
    #region TransformedBitmap
    /// <summary>
    /// TransformedBitmap provides caching functionality for a BitmapSource.
    /// </summary>
    public sealed partial class TransformedBitmap : Imaging.BitmapSource, ISupportInitialize
    {
        /// <summary>
        /// TransformedBitmap construtor
        /// </summary>
        public TransformedBitmap()
            : base(true) // Use base class virtuals
        {
        }

        /// <summary>
        /// Construct a TransformedBitmap with the given newTransform
        /// </summary>
        /// <param name="source">BitmapSource to apply to the newTransform to</param>
        /// <param name="newTransform">Transform to apply to the bitmap</param>
        public TransformedBitmap(BitmapSource source, Transform newTransform)
            : base(true) // Use base class virtuals
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (newTransform == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_NoArgument, "Transform"));
            }

            if (!CheckTransform(newTransform))
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_OnlyOrthogonal));
            }

            _bitmapInit.BeginInit();

            Source = source;
            Transform = newTransform;

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

        private void ClonePrequel(TransformedBitmap otherTransformedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(TransformedBitmap otherTransformedBitmap)
        {
            EndInit();
        }

        /// <summary>
        /// Check the transformation to see if it's a simple scale and/or rotation and/or flip.
        /// </summary>
        internal bool CheckTransform(Transform newTransform)
        {
            Matrix m = newTransform.Value;
            bool canHandle = false;

            if ( (DoubleUtil.IsZero(m.M11) && DoubleUtil.IsZero(m.M22)) ||
                 (DoubleUtil.IsZero(m.M12) && DoubleUtil.IsZero(m.M21)) )
            {
                canHandle = true;
            }

            return canHandle;
        }

        /// <summary>
        /// Check the transformation to see if it's a simple scale and/or rotation and/or flip.
        /// </summary>
        internal void GetParamsFromTransform(
            Transform newTransform,
            out double scaleX,
            out double scaleY,
            out WICBitmapTransformOptions options)
        {
            Matrix m = newTransform.Value;

            if (DoubleUtil.IsZero(m.M12) && DoubleUtil.IsZero(m.M21))
            {
                scaleX = Math.Abs(m.M11);
                scaleY = Math.Abs(m.M22);

                options = WICBitmapTransformOptions.WICBitmapTransformRotate0;

                if (m.M11 < 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                }

                if (m.M22 < 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                }
            }
            else
            {
                Debug.Assert(DoubleUtil.IsZero(m.M11) && DoubleUtil.IsZero(m.M22));

                scaleX = Math.Abs(m.M12);
                scaleY = Math.Abs(m.M21);

                options = WICBitmapTransformOptions.WICBitmapTransformRotate90;

                if (m.M12 < 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                }

                if (m.M21 >= 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                }
            }
        }

        ///
        /// Create the unmanaged resources
        ///
        internal override void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle wicTransformer = null;

            double scaleX, scaleY;
            WICBitmapTransformOptions options;

            GetParamsFromTransform(Transform, out scaleX, out scaleY, out options);

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    IntPtr wicFactory = factoryMaker.ImagingFactoryPtr;

                    wicTransformer = _source.WicSourceHandle;

                    if (!DoubleUtil.IsOne(scaleX) || !DoubleUtil.IsOne(scaleY))
                    {
                        uint width = Math.Max(1, (uint)(scaleX * _source.PixelWidth + 0.5));
                        uint height = Math.Max(1, (uint)(scaleY * _source.PixelHeight + 0.5));

                        HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapScaler(
                                wicFactory,
                                out wicTransformer));

                        lock (_syncObject)
                        {
                            HRESULT.Check(UnsafeNativeMethods.WICBitmapScaler.Initialize(
                                    wicTransformer,
                                    _source.WicSourceHandle,
                                    width,
                                    height,
                                    WICInterpolationMode.Fant));
                        }
                    }

                    if (options != WICBitmapTransformOptions.WICBitmapTransformRotate0)
                    {
                        // Rotations are extremely slow if we're pulling from a decoder because we end
                        // up decoding multiple times.  Caching the source lets us rotate faster at the cost
                        // of increased memory usage.
                        wicTransformer = CreateCachedBitmap(
                            null,
                            wicTransformer,
                            BitmapCreateOptions.PreservePixelFormat,
                            BitmapCacheOption.Default,
                            _source.Palette);
                        // BitmapSource.CreateCachedBitmap already calculates memory pressure for
                        // the new bitmap, so there's no need to do it before setting it to
                        // WicSourceHandle.

                        BitmapSourceSafeMILHandle rotator = null;

                        HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapFlipRotator(
                                wicFactory,
                                out rotator));

                        lock (_syncObject)
                        {
                            HRESULT.Check(UnsafeNativeMethods.WICBitmapFlipRotator.Initialize(
                                    rotator,
                                    wicTransformer,
                                    options));
                        }

                        wicTransformer = rotator;
                    }

                    // If we haven't introduced either a scaler or rotator, add a null rotator
                    // so that our WicSourceHandle isn't the same as our Source's.
                    if (options == WICBitmapTransformOptions.WICBitmapTransformRotate0 &&
                        DoubleUtil.IsOne(scaleX) && DoubleUtil.IsOne(scaleY))
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapFlipRotator(
                                wicFactory,
                                out wicTransformer));

                        lock (_syncObject)
                        {
                            HRESULT.Check(UnsafeNativeMethods.WICBitmapFlipRotator.Initialize(
                                    wicTransformer,
                                    _source.WicSourceHandle,
                                    WICBitmapTransformOptions.WICBitmapTransformRotate0));
                        }
                    }

                    WicSourceHandle = wicTransformer;
                    _isSourceCached = _source.IsSourceCached;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
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

            Transform transform = Transform;
            if (transform == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Image_NoArgument, "Transform"));
                }
                return false;
            }

            if (!CheckTransform(transform))
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Image_OnlyOrthogonal));
                }
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Notification on transform changing.
        /// </summary>
        private void TransformPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _transform = e.NewValue as Transform;
            }
        }

        /// <summary>
        ///     Coerce Source
        /// </summary>
        private static object CoerceSource(DependencyObject d, object value)
        {
            TransformedBitmap bitmap = (TransformedBitmap)d;
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
        ///     Coerce Transform
        /// </summary>
        private static object CoerceTransform(DependencyObject d, object value)
        {
            TransformedBitmap bitmap = (TransformedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._transform;
            }
            else
            {
                return value;
            }
        }

        #region Data Members

        private BitmapSource _source;

        private Transform _transform;

        #endregion
    }

    #endregion // TransformedBitmap
}
