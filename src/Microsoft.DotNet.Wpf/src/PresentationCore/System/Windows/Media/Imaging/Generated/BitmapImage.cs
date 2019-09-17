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
    sealed partial class BitmapImage : BitmapSource
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
        public new BitmapImage Clone()
        {
            return (BitmapImage)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapImage CloneCurrentValue()
        {
            return (BitmapImage)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void UriCachePolicyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.UriCachePolicyPropertyChangedHook(e);

            target.PropertyChanged(UriCachePolicyProperty);
        }
        private static void UriSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.UriSourcePropertyChangedHook(e);

            target.PropertyChanged(UriSourceProperty);
        }
        private static void StreamSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.StreamSourcePropertyChangedHook(e);

            target.PropertyChanged(StreamSourceProperty);
        }
        private static void DecodePixelWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.DecodePixelWidthPropertyChangedHook(e);

            target.PropertyChanged(DecodePixelWidthProperty);
        }
        private static void DecodePixelHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.DecodePixelHeightPropertyChangedHook(e);

            target.PropertyChanged(DecodePixelHeightProperty);
        }
        private static void RotationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.RotationPropertyChangedHook(e);

            target.PropertyChanged(RotationProperty);
        }
        private static void SourceRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.SourceRectPropertyChangedHook(e);

            target.PropertyChanged(SourceRectProperty);
        }
        private static void CreateOptionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.CreateOptionsPropertyChangedHook(e);

            target.PropertyChanged(CreateOptionsProperty);
        }
        private static void CacheOptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage) d);


            target.CacheOptionPropertyChangedHook(e);

            target.PropertyChanged(CacheOptionProperty);
        }


        #region Public Properties

        /// <summary>
        ///     UriCachePolicy - RequestCachePolicy.  Default value is null.
        /// </summary>
        [TypeConverter(typeof(System.Windows.Media.RequestCachePolicyConverter))]
        public RequestCachePolicy UriCachePolicy
        {
            get
            {
                return (RequestCachePolicy) GetValue(UriCachePolicyProperty);
            }
            set
            {
                SetValueInternal(UriCachePolicyProperty, value);
            }
        }

        /// <summary>
        ///     UriSource - Uri.  Default value is null.
        /// </summary>
        public Uri UriSource
        {
            get
            {
                return (Uri) GetValue(UriSourceProperty);
            }
            set
            {
                SetValueInternal(UriSourceProperty, value);
            }
        }

        /// <summary>
        ///     StreamSource - Stream.  Default value is null.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
                         public Stream StreamSource
        {
            get
            {
                return (Stream) GetValue(StreamSourceProperty);
            }
            set
            {
                SetValueInternal(StreamSourceProperty, value);
            }
        }

        /// <summary>
        ///     DecodePixelWidth - int.  Default value is 0.
        /// </summary>
        public int DecodePixelWidth
        {
            get
            {
                return (int) GetValue(DecodePixelWidthProperty);
            }
            set
            {
                SetValueInternal(DecodePixelWidthProperty, value);
            }
        }

        /// <summary>
        ///     DecodePixelHeight - int.  Default value is 0.
        /// </summary>
        public int DecodePixelHeight
        {
            get
            {
                return (int) GetValue(DecodePixelHeightProperty);
            }
            set
            {
                SetValueInternal(DecodePixelHeightProperty, value);
            }
        }

        /// <summary>
        ///     Rotation - Rotation.  Default value is Rotation.Rotate0.
        /// </summary>
        public Rotation Rotation
        {
            get
            {
                return (Rotation) GetValue(RotationProperty);
            }
            set
            {
                SetValueInternal(RotationProperty, value);
            }
        }

        /// <summary>
        ///     SourceRect - Int32Rect.  Default value is Int32Rect.Empty.
        /// </summary>
        public Int32Rect SourceRect
        {
            get
            {
                return (Int32Rect) GetValue(SourceRectProperty);
            }
            set
            {
                SetValueInternal(SourceRectProperty, value);
            }
        }

        /// <summary>
        ///     CreateOptions - BitmapCreateOptions.  Default value is BitmapCreateOptions.None.
        /// </summary>
        public BitmapCreateOptions CreateOptions
        {
            get
            {
                return (BitmapCreateOptions) GetValue(CreateOptionsProperty);
            }
            set
            {
                SetValueInternal(CreateOptionsProperty, value);
            }
        }

        /// <summary>
        ///     CacheOption - BitmapCacheOption.  Default value is BitmapCacheOption.Default.
        /// </summary>
        public BitmapCacheOption CacheOption
        {
            get
            {
                return (BitmapCacheOption) GetValue(CacheOptionProperty);
            }
            set
            {
                SetValueInternal(CacheOptionProperty, value);
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
            return new BitmapImage();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceBitmapImage);

            base.CloneCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceBitmapImage);
        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceBitmapImage);

            base.CloneCurrentValueCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceBitmapImage);
        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceBitmapImage);

            base.GetAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceBitmapImage);
        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage) source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceBitmapImage);

            base.GetCurrentValueAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceBitmapImage);
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
        ///     The DependencyProperty for the BitmapImage.UriCachePolicy property.
        /// </summary>
        public static readonly DependencyProperty UriCachePolicyProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.UriSource property.
        /// </summary>
        public static readonly DependencyProperty UriSourceProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.StreamSource property.
        /// </summary>
        public static readonly DependencyProperty StreamSourceProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.DecodePixelWidth property.
        /// </summary>
        public static readonly DependencyProperty DecodePixelWidthProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.DecodePixelHeight property.
        /// </summary>
        public static readonly DependencyProperty DecodePixelHeightProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.Rotation property.
        /// </summary>
        public static readonly DependencyProperty RotationProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.SourceRect property.
        /// </summary>
        public static readonly DependencyProperty SourceRectProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.CreateOptions property.
        /// </summary>
        public static readonly DependencyProperty CreateOptionsProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapImage.CacheOption property.
        /// </summary>
        public static readonly DependencyProperty CacheOptionProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static RequestCachePolicy s_UriCachePolicy = null;
        internal static Uri s_UriSource = null;
        internal static Stream s_StreamSource = null;
        internal const int c_DecodePixelWidth = 0;
        internal const int c_DecodePixelHeight = 0;
        internal const Rotation c_Rotation = Rotation.Rotate0;
        internal static Int32Rect s_SourceRect = Int32Rect.Empty;
        internal static BitmapCreateOptions s_CreateOptions = BitmapCreateOptions.None;
        internal static BitmapCacheOption s_CacheOption = BitmapCacheOption.Default;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BitmapImage()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 



            // Initializations
            Type typeofThis = typeof(BitmapImage);
            UriCachePolicyProperty =
                  RegisterProperty("UriCachePolicy",
                                   typeof(RequestCachePolicy),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(UriCachePolicyPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceUriCachePolicy));
            UriSourceProperty =
                  RegisterProperty("UriSource",
                                   typeof(Uri),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(UriSourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceUriSource));
            StreamSourceProperty =
                  RegisterProperty("StreamSource",
                                   typeof(Stream),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(StreamSourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceStreamSource));
            DecodePixelWidthProperty =
                  RegisterProperty("DecodePixelWidth",
                                   typeof(int),
                                   typeofThis,
                                   0,
                                   new PropertyChangedCallback(DecodePixelWidthPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceDecodePixelWidth));
            DecodePixelHeightProperty =
                  RegisterProperty("DecodePixelHeight",
                                   typeof(int),
                                   typeofThis,
                                   0,
                                   new PropertyChangedCallback(DecodePixelHeightPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceDecodePixelHeight));
            RotationProperty =
                  RegisterProperty("Rotation",
                                   typeof(Rotation),
                                   typeofThis,
                                   Rotation.Rotate0,
                                   new PropertyChangedCallback(RotationPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.Imaging.ValidateEnums.IsRotationValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceRotation));
            SourceRectProperty =
                  RegisterProperty("SourceRect",
                                   typeof(Int32Rect),
                                   typeofThis,
                                   Int32Rect.Empty,
                                   new PropertyChangedCallback(SourceRectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSourceRect));
            CreateOptionsProperty =
                  RegisterProperty("CreateOptions",
                                   typeof(BitmapCreateOptions),
                                   typeofThis,
                                   BitmapCreateOptions.None,
                                   new PropertyChangedCallback(CreateOptionsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceCreateOptions));
            CacheOptionProperty =
                  RegisterProperty("CacheOption",
                                   typeof(BitmapCacheOption),
                                   typeofThis,
                                   BitmapCacheOption.Default,
                                   new PropertyChangedCallback(CacheOptionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceCacheOption));
        }

        #endregion Constructors
    }
}
