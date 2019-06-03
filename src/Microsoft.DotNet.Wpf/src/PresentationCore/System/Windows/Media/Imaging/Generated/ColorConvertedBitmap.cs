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
    sealed partial class ColorConvertedBitmap : BitmapSource
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
        public new ColorConvertedBitmap Clone()
        {
            return (ColorConvertedBitmap)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ColorConvertedBitmap CloneCurrentValue()
        {
            return (ColorConvertedBitmap)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = ((ColorConvertedBitmap) d);


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
        private static void SourceColorContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = ((ColorConvertedBitmap) d);


            target.SourceColorContextPropertyChangedHook(e);

            target.PropertyChanged(SourceColorContextProperty);
        }
        private static void DestinationColorContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = ((ColorConvertedBitmap) d);


            target.DestinationColorContextPropertyChangedHook(e);

            target.PropertyChanged(DestinationColorContextProperty);
        }
        private static void DestinationFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = ((ColorConvertedBitmap) d);


            target.DestinationFormatPropertyChangedHook(e);

            target.PropertyChanged(DestinationFormatProperty);
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
        ///     SourceColorContext - ColorContext.  Default value is null.
        /// </summary>
        public ColorContext SourceColorContext
        {
            get
            {
                return (ColorContext) GetValue(SourceColorContextProperty);
            }
            set
            {
                SetValueInternal(SourceColorContextProperty, value);
            }
        }

        /// <summary>
        ///     DestinationColorContext - ColorContext.  Default value is null.
        /// </summary>
        public ColorContext DestinationColorContext
        {
            get
            {
                return (ColorContext) GetValue(DestinationColorContextProperty);
            }
            set
            {
                SetValueInternal(DestinationColorContextProperty, value);
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
            return new ColorConvertedBitmap();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceColorConvertedBitmap);

            base.CloneCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceColorConvertedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceColorConvertedBitmap);

            base.CloneCurrentValueCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceColorConvertedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceColorConvertedBitmap);

            base.GetAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceColorConvertedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceColorConvertedBitmap);

            base.GetCurrentValueAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceColorConvertedBitmap);
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
        ///     The DependencyProperty for the ColorConvertedBitmap.Source property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty;
        /// <summary>
        ///     The DependencyProperty for the ColorConvertedBitmap.SourceColorContext property.
        /// </summary>
        public static readonly DependencyProperty SourceColorContextProperty;
        /// <summary>
        ///     The DependencyProperty for the ColorConvertedBitmap.DestinationColorContext property.
        /// </summary>
        public static readonly DependencyProperty DestinationColorContextProperty;
        /// <summary>
        ///     The DependencyProperty for the ColorConvertedBitmap.DestinationFormat property.
        /// </summary>
        public static readonly DependencyProperty DestinationFormatProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static BitmapSource s_Source = null;
        internal static ColorContext s_SourceColorContext = null;
        internal static ColorContext s_DestinationColorContext = null;
        internal static PixelFormat s_DestinationFormat = PixelFormats.Pbgra32;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ColorConvertedBitmap()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 
            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value ColorConvertedBitmap.s_Source (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(ColorConvertedBitmap);
            SourceProperty =
                  RegisterProperty("Source",
                                   typeof(BitmapSource),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(SourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSource));
            SourceColorContextProperty =
                  RegisterProperty("SourceColorContext",
                                   typeof(ColorContext),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(SourceColorContextPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSourceColorContext));
            DestinationColorContextProperty =
                  RegisterProperty("DestinationColorContext",
                                   typeof(ColorContext),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(DestinationColorContextPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceDestinationColorContext));
            DestinationFormatProperty =
                  RegisterProperty("DestinationFormat",
                                   typeof(PixelFormat),
                                   typeofThis,
                                   PixelFormats.Pbgra32,
                                   new PropertyChangedCallback(DestinationFormatPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceDestinationFormat));
        }

        #endregion Constructors
    }
}
