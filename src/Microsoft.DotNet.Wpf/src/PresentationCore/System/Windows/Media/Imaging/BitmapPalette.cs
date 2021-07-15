// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using MS.Internal.PresentationCore; //SecurityHelper
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// BitmapPalette class
    /// </summary>
    public sealed class BitmapPalette : DispatcherObject
    {
        #region Constructors

        /// <summary>
        /// No public default constructor
        /// </summary>
        private BitmapPalette()
        {
        }

        /// <summary>
        /// Create a palette from the list of colors.
        /// </summary>
        public BitmapPalette(IList<Color> colors)
        {
            if (colors == null)
            {
                throw new ArgumentNullException("colors");
            }

            int count = colors.Count;

            if (count < 1 || count > 256)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_PaletteZeroColors, null));
            }

            Color[] colorArray = new Color[count];

            for (int i = 0; i < count; ++i)
            {
                colorArray[i] = colors[i];
            }

            _colors = new PartialList<Color>(colorArray);

            _palette = CreateInternalPalette();

            UpdateUnmanaged();
        }

        /// <summary>
        /// Construct BitmapPalette from a BitmapSource.
        ///
        /// If the BitmapSource is already palettized, the corresponding
        /// palette is returned. Otherwise, a new palette is constructed from
        /// an analysis of the bitmap.
        /// </summary>
        /// <param name="bitmapSource">Bitmap to use for analysis</param>
        /// <param name="maxColorCount">Maximum number of colors</param>
        public BitmapPalette(BitmapSource bitmapSource, int maxColorCount)
        {
            // Note: we will never return a palette from BitmapPalettes.

            if (bitmapSource == null)
            {
                throw new ArgumentNullException("bitmapSource");
            }

            SafeMILHandle unmanagedBitmap = bitmapSource.WicSourceHandle;

            _palette = CreateInternalPalette();

            lock (bitmapSource.SyncObject)
            {
                HRESULT.Check(UnsafeNativeMethods.WICPalette.InitializeFromBitmap(
                            _palette,
                            unmanagedBitmap,
                            maxColorCount,
                            false));
            }

            UpdateManaged();
        }

        /// <summary>
        /// Constructs a bitmap from a known WICPaletteType (does not perform
        /// caching).
        ///
        /// Note: It is an error to modify the Color property of the
        /// constructed BitmapPalette.  Indeed, the returned BitmapPalette
        /// should probably be immediately frozen. Additionally, outside users
        /// will have no knowledge that this is a predefined palette (or which
        /// predefined palette it is). It is thus highly recommended that only
        /// the BitmapPalettes class use this constructor.
        /// </summary>
        internal BitmapPalette(WICPaletteType paletteType,
                bool addtransparentColor)
        {
            switch (paletteType)
            {
                case WICPaletteType.WICPaletteTypeFixedBW:
                case WICPaletteType.WICPaletteTypeFixedHalftone8:
                case WICPaletteType.WICPaletteTypeFixedHalftone27:
                case WICPaletteType.WICPaletteTypeFixedHalftone64:
                case WICPaletteType.WICPaletteTypeFixedHalftone125:
                case WICPaletteType.WICPaletteTypeFixedHalftone216:
                case WICPaletteType.WICPaletteTypeFixedHalftone252:
                case WICPaletteType.WICPaletteTypeFixedHalftone256:
                case WICPaletteType.WICPaletteTypeFixedGray4:
                case WICPaletteType.WICPaletteTypeFixedGray16:
                case WICPaletteType.WICPaletteTypeFixedGray256:
                    break;

                default:
                    throw new System.ArgumentException(SR.Get(SRID.Image_PaletteFixedType, paletteType));
            }

            _palette = CreateInternalPalette();

            HRESULT.Check(UnsafeNativeMethods.WICPalette.InitializePredefined(
                        _palette,
                        paletteType,
                        addtransparentColor));

            // Fill in the Colors property.
            UpdateManaged();
        }

        internal BitmapPalette(SafeMILHandle unmanagedPalette)
        {
            _palette = unmanagedPalette;

            // Fill in the Colors property.
            UpdateManaged();
        }

        #endregion // Constructors

        #region Factory Methods

        /// <summary>
        /// Create a BitmapPalette from an unmanaged BitmapSource. If the
        /// bitmap is not paletteized, we return BitmapPalette.Empty. If the
        /// palette is of a known type, we will use BitmapPalettes.
        /// </summary>
        static internal BitmapPalette CreateFromBitmapSource(BitmapSource source)
        {
            Debug.Assert(source != null);

            SafeMILHandle bitmapSource = source.WicSourceHandle;
            Debug.Assert(bitmapSource != null && !bitmapSource.IsInvalid);

            SafeMILHandle unmanagedPalette = CreateInternalPalette();

            BitmapPalette palette;

            // Don't throw on the HRESULT from this method.  If it returns failure,
            // that likely means that the source doesn't have a palette.
            lock (source.SyncObject)
            {
                int hr = UnsafeNativeMethods.WICBitmapSource.CopyPalette(
                            bitmapSource,
                            unmanagedPalette);

                if (hr != HRESULT.S_OK)
                {
                    return null;
                }
            }

            WICPaletteType paletteType;
            bool hasAlpha;

            HRESULT.Check(UnsafeNativeMethods.WICPalette.GetType(unmanagedPalette, out paletteType));
            HRESULT.Check(UnsafeNativeMethods.WICPalette.HasAlpha(unmanagedPalette, out hasAlpha));

            if (paletteType == WICPaletteType.WICPaletteTypeCustom ||
                paletteType == WICPaletteType.WICPaletteTypeOptimal)
            {
                palette = new BitmapPalette(unmanagedPalette);
            }
            else
            {
                palette = BitmapPalettes.FromMILPaletteType(paletteType, hasAlpha);
                Debug.Assert(palette != null);
            }

            return palette;
        }

        #endregion // Factory Methods

        #region Properties

        /// <summary>
        /// The contents of the palette.
        /// </summary>
        public IList<Color> Colors
        {
            get
            {
                return _colors;
            }
        }

        #endregion // Properties

        #region Internal Properties

        internal SafeMILHandle InternalPalette
        {
            get
            {
                if (_palette == null || _palette.IsInvalid)
                {
                    _palette = CreateInternalPalette();
                }

                return _palette;
            }
        }

        #endregion // Internal Properties

        #region Static / Private Methods

        /// Returns if the Palette has any alpha within its colors
        internal static bool DoesPaletteHaveAlpha(BitmapPalette palette)
        {
            if (palette != null)
            {
                foreach (Color color in palette.Colors)
                {
                    if (color.A != 255)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static internal SafeMILHandle CreateInternalPalette()
        {
            SafeMILHandle palette = null;

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreatePalette(
                            myFactory.ImagingFactoryPtr,
                            out palette));
                Debug.Assert(palette != null && !palette.IsInvalid);
            }

            return palette;
        }

        /// <summary>
        /// Copy Colors down into the IMILPalette.
        /// </summary>
        /// Critical - is an unsafe method, calls into native code
        /// TreatAsSafe - No inputs are provided, no information is exposed.
        unsafe private void UpdateUnmanaged()
        {
            Debug.Assert(_palette != null && !_palette.IsInvalid);

            int numColors = Math.Min(256, _colors.Count);

            ImagePaletteColor[] paletteColorArray = new ImagePaletteColor[numColors];

            for (int i = 0; i < numColors; ++i)
            {
                Color color = _colors[i];
                paletteColorArray[i].B = color.B;
                paletteColorArray[i].G = color.G;
                paletteColorArray[i].R = color.R;
                paletteColorArray[i].A = color.A;
            }

            fixed (void* paletteColorArrayPinned = paletteColorArray)
            {
                HRESULT.Check(UnsafeNativeMethods.WICPalette.InitializeCustom(
                            _palette,
                            (IntPtr)paletteColorArrayPinned,
                            numColors));
            }
        }

        /// <summary>
        /// Copy the colors from IMILBitmapPalette into Colors.
        /// </summary>
        private void UpdateManaged()
        {
            Debug.Assert(_palette != null && !_palette.IsInvalid);

            int numColors = 0;
            int cActualColors = 0;
            HRESULT.Check(UnsafeNativeMethods.WICPalette.GetColorCount(_palette,
                        out numColors));

            List<Color> colors = new List<Color>();

            if (numColors < 1 || numColors > 256)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_PaletteZeroColors, null));
            }
            else
            {
                ImagePaletteColor[] paletteColorArray = new ImagePaletteColor[numColors];
                unsafe
                {
                    fixed(void* paletteColorArrayPinned = paletteColorArray)
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICPalette.GetColors(
                                    _palette,
                                    numColors,
                                    (IntPtr)paletteColorArrayPinned,
                                    out cActualColors));

                        Debug.Assert(cActualColors == numColors);
                    }
                }

                for (int i = 0; i < numColors; ++i)
                {
                    ImagePaletteColor c = paletteColorArray[i];

                    colors.Add(Color.FromArgb(c.A, c.R, c.G, c.B));
                }
            }

            _colors = new PartialList<Color>(colors);
        }

        #endregion // Private Methods

        /// <summary>
        /// ImagePaletteColor structure -- convenience for Interop
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct ImagePaletteColor
        {
            /// <summary>
            /// blue channel: 0 - 255
            /// </summary>
            public byte B;
            /// <summary>
            /// green channel: 0 - 255
            /// </summary>
            public byte G;
            /// <summary>
            /// red channel: 0 - 255
            /// </summary>
            public byte R;
            /// <summary>
            /// alpha channel: 0 - 255
            /// </summary>
            public byte A;
        };

        // Note: We have a little trickery going on here. When a new BitmapPalette is
        // cloned, _palette isn't copied and so is reset to null. This means that the
        // next call to InternalPalette will create a new IWICPalette, which is exactly
        // the behavior that we want.
        private SafeMILHandle _palette = null; // IWICPalette*

        private IList<Color> _colors = new PartialList<Color>(new List<Color>());
    }
}


