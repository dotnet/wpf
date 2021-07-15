// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Diagnostics;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media.Imaging
{
    sealed partial class FormatConvertedBitmap : BitmapSource
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new FormatConvertedBitmap Clone()
        {
            return (FormatConvertedBitmap)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new FormatConvertedBitmap CloneCurrentValue()
        {
            return (FormatConvertedBitmap)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = ((FormatConvertedBitmap) d);


            target.SourcePropertyChangedHook(e);



            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }



            target.PropertyChanged(SourceProperty);
        }
        private static void DestinationFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = ((FormatConvertedBitmap) d);


            target.DestinationFormatPropertyChangedHook(e);

            target.PropertyChanged(DestinationFormatProperty);
        }
        private static void DestinationPalettePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = ((FormatConvertedBitmap) d);


            target.DestinationPalettePropertyChangedHook(e);

            target.PropertyChanged(DestinationPaletteProperty);
        }
        private static void AlphaThresholdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = ((FormatConvertedBitmap) d);


            target.AlphaThresholdPropertyChangedHook(e);

            target.PropertyChanged(AlphaThresholdProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Source - BitmapSource.  Default value is null.
        /// </summary>
        public BitmapSource Source
        {
            get
            {
                return (BitmapSource) GetValue(SourceProperty);
            }
            set
            {
                SetValueInternal(SourceProperty, value);
            }
        }

        /// <summary>
        ///     DestinationFormat - PixelFormat.  Default value is PixelFormats.Pbgra32.
        /// </summary>
        public PixelFormat DestinationFormat
        {
            get
            {
                return (PixelFormat) GetValue(DestinationFormatProperty);
            }
            set
            {
                SetValueInternal(DestinationFormatProperty, value);
            }
        }

        /// <summary>
        ///     DestinationPalette - BitmapPalette.  Default value is null.
        /// </summary>
        public BitmapPalette DestinationPalette
        {
            get
            {
                return (BitmapPalette) GetValue(DestinationPaletteProperty);
            }
            set
            {
                SetValueInternal(DestinationPaletteProperty, value);
            }
        }

        /// <summary>
        ///     AlphaThreshold - double.  Default value is 0.0.
        /// </summary>
        public double AlphaThreshold
        {
            get
            {
                return (double) GetValue(AlphaThresholdProperty);
            }
            set
            {
                SetValueInternal(AlphaThresholdProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new FormatConvertedBitmap();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceFormatConvertedBitmap);

            base.CloneCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceFormatConvertedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceFormatConvertedBitmap);

            base.CloneCurrentValueCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceFormatConvertedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceFormatConvertedBitmap);

            base.GetAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceFormatConvertedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceFormatConvertedBitmap);

            base.GetCurrentValueAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceFormatConvertedBitmap);
        }


        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods









        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the FormatConvertedBitmap.Source property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty;
        /// <summary>
        ///     The DependencyProperty for the FormatConvertedBitmap.DestinationFormat property.
        /// </summary>
        public static readonly DependencyProperty DestinationFormatProperty;
        /// <summary>
        ///     The DependencyProperty for the FormatConvertedBitmap.DestinationPalette property.
        /// </summary>
        public static readonly DependencyProperty DestinationPaletteProperty;
        /// <summary>
        ///     The DependencyProperty for the FormatConvertedBitmap.AlphaThreshold property.
        /// </summary>
        public static readonly DependencyProperty AlphaThresholdProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static BitmapSource s_Source = null;
        internal static PixelFormat s_DestinationFormat = PixelFormats.Pbgra32;
        internal static BitmapPalette s_DestinationPalette = null;
        internal const double c_AlphaThreshold = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static FormatConvertedBitmap()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 

            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value FormatConvertedBitmap.s_Source (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(FormatConvertedBitmap);
            SourceProperty =
                  RegisterProperty("Source",
                                   typeof(BitmapSource),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(SourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSource));
            DestinationFormatProperty =
                  RegisterProperty("DestinationFormat",
                                   typeof(PixelFormat),
                                   typeofThis,
                                   PixelFormats.Pbgra32,
                                   new PropertyChangedCallback(DestinationFormatPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceDestinationFormat));
            DestinationPaletteProperty =
                  RegisterProperty("DestinationPalette",
                                   typeof(BitmapPalette),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(DestinationPalettePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceDestinationPalette));
            AlphaThresholdProperty =
                  RegisterProperty("AlphaThreshold",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(AlphaThresholdPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceAlphaThreshold));
        }

        #endregion Constructors
    }
}
