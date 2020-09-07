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
using MS.Internal.KnownBoxes;
using MS.Internal.Collections;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Media.Converters;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media
{
    abstract partial class TileBrush : Brush
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
        public new TileBrush Clone()
        {
            return (TileBrush)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TileBrush CloneCurrentValue()
        {
            return (TileBrush)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ViewportUnitsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(ViewportUnitsProperty);
        }
        private static void ViewboxUnitsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(ViewboxUnitsProperty);
        }
        private static void ViewportPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(ViewportProperty);
        }
        private static void ViewboxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(ViewboxProperty);
        }
        private static void StretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(StretchProperty);
        }
        private static void TileModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(TileModeProperty);
        }
        private static void AlignmentXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(AlignmentXProperty);
        }
        private static void AlignmentYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(AlignmentYProperty);
        }
        private static void CachingHintPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(RenderOptions.CachingHintProperty);
        }
        private static void CacheInvalidationThresholdMinimumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(RenderOptions.CacheInvalidationThresholdMinimumProperty);
        }
        private static void CacheInvalidationThresholdMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TileBrush target = ((TileBrush) d);


            target.PropertyChanged(RenderOptions.CacheInvalidationThresholdMaximumProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ViewportUnits - BrushMappingMode.  Default value is BrushMappingMode.RelativeToBoundingBox.
        /// </summary>
        public BrushMappingMode ViewportUnits
        {
            get
            {
                return (BrushMappingMode) GetValue(ViewportUnitsProperty);
            }
            set
            {
                SetValueInternal(ViewportUnitsProperty, value);
            }
        }

        /// <summary>
        ///     ViewboxUnits - BrushMappingMode.  Default value is BrushMappingMode.RelativeToBoundingBox.
        /// </summary>
        public BrushMappingMode ViewboxUnits
        {
            get
            {
                return (BrushMappingMode) GetValue(ViewboxUnitsProperty);
            }
            set
            {
                SetValueInternal(ViewboxUnitsProperty, value);
            }
        }

        /// <summary>
        ///     Viewport - Rect.  Default value is new Rect(0,0,1,1).
        /// </summary>
        public Rect Viewport
        {
            get
            {
                return (Rect) GetValue(ViewportProperty);
            }
            set
            {
                SetValueInternal(ViewportProperty, value);
            }
        }

        /// <summary>
        ///     Viewbox - Rect.  Default value is new Rect(0,0,1,1).
        /// </summary>
        public Rect Viewbox
        {
            get
            {
                return (Rect) GetValue(ViewboxProperty);
            }
            set
            {
                SetValueInternal(ViewboxProperty, value);
            }
        }

        /// <summary>
        ///     Stretch - Stretch.  Default value is Stretch.Fill.
        /// </summary>
        public Stretch Stretch
        {
            get
            {
                return (Stretch) GetValue(StretchProperty);
            }
            set
            {
                SetValueInternal(StretchProperty, value);
            }
        }

        /// <summary>
        ///     TileMode - TileMode.  Default value is TileMode.None.
        /// </summary>
        public TileMode TileMode
        {
            get
            {
                return (TileMode) GetValue(TileModeProperty);
            }
            set
            {
                SetValueInternal(TileModeProperty, value);
            }
        }

        /// <summary>
        ///     AlignmentX - AlignmentX.  Default value is AlignmentX.Center.
        /// </summary>
        public AlignmentX AlignmentX
        {
            get
            {
                return (AlignmentX) GetValue(AlignmentXProperty);
            }
            set
            {
                SetValueInternal(AlignmentXProperty, value);
            }
        }

        /// <summary>
        ///     AlignmentY - AlignmentY.  Default value is AlignmentY.Center.
        /// </summary>
        public AlignmentY AlignmentY
        {
            get
            {
                return (AlignmentY) GetValue(AlignmentYProperty);
            }
            set
            {
                SetValueInternal(AlignmentYProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods





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
        ///     The DependencyProperty for the TileBrush.ViewportUnits property.
        /// </summary>
        public static readonly DependencyProperty ViewportUnitsProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.ViewboxUnits property.
        /// </summary>
        public static readonly DependencyProperty ViewboxUnitsProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.Viewport property.
        /// </summary>
        public static readonly DependencyProperty ViewportProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.Viewbox property.
        /// </summary>
        public static readonly DependencyProperty ViewboxProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.Stretch property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.TileMode property.
        /// </summary>
        public static readonly DependencyProperty TileModeProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.AlignmentX property.
        /// </summary>
        public static readonly DependencyProperty AlignmentXProperty;
        /// <summary>
        ///     The DependencyProperty for the TileBrush.AlignmentY property.
        /// </summary>
        public static readonly DependencyProperty AlignmentYProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const BrushMappingMode c_ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
        internal const BrushMappingMode c_ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
        internal static Rect s_Viewport = new Rect(0,0,1,1);
        internal static Rect s_Viewbox = new Rect(0,0,1,1);
        internal const Stretch c_Stretch = Stretch.Fill;
        internal const TileMode c_TileMode = TileMode.None;
        internal const AlignmentX c_AlignmentX = AlignmentX.Center;
        internal const AlignmentY c_AlignmentY = AlignmentY.Center;
        internal const CachingHint c_CachingHint = CachingHint.Unspecified;
        internal const double c_CacheInvalidationThresholdMinimum = 0.707;
        internal const double c_CacheInvalidationThresholdMaximum = 1.414;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TileBrush()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 

            RenderOptions.CachingHintProperty.OverrideMetadata(
                typeof(TileBrush),
                new UIPropertyMetadata(CachingHint.Unspecified,
                                       new PropertyChangedCallback(CachingHintPropertyChanged)));

            RenderOptions.CacheInvalidationThresholdMinimumProperty.OverrideMetadata(
                typeof(TileBrush),
                new UIPropertyMetadata(0.707,
                                       new PropertyChangedCallback(CacheInvalidationThresholdMinimumPropertyChanged)));

            RenderOptions.CacheInvalidationThresholdMaximumProperty.OverrideMetadata(
                typeof(TileBrush),
                new UIPropertyMetadata(1.414,
                                       new PropertyChangedCallback(CacheInvalidationThresholdMaximumPropertyChanged)));

            // Initializations
            Type typeofThis = typeof(TileBrush);
            ViewportUnitsProperty =
                  RegisterProperty("ViewportUnits",
                                   typeof(BrushMappingMode),
                                   typeofThis,
                                   BrushMappingMode.RelativeToBoundingBox,
                                   new PropertyChangedCallback(ViewportUnitsPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsBrushMappingModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ViewboxUnitsProperty =
                  RegisterProperty("ViewboxUnits",
                                   typeof(BrushMappingMode),
                                   typeofThis,
                                   BrushMappingMode.RelativeToBoundingBox,
                                   new PropertyChangedCallback(ViewboxUnitsPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsBrushMappingModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ViewportProperty =
                  RegisterProperty("Viewport",
                                   typeof(Rect),
                                   typeofThis,
                                   new Rect(0,0,1,1),
                                   new PropertyChangedCallback(ViewportPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ViewboxProperty =
                  RegisterProperty("Viewbox",
                                   typeof(Rect),
                                   typeofThis,
                                   new Rect(0,0,1,1),
                                   new PropertyChangedCallback(ViewboxPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            StretchProperty =
                  RegisterProperty("Stretch",
                                   typeof(Stretch),
                                   typeofThis,
                                   Stretch.Fill,
                                   new PropertyChangedCallback(StretchPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsStretchValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TileModeProperty =
                  RegisterProperty("TileMode",
                                   typeof(TileMode),
                                   typeofThis,
                                   TileMode.None,
                                   new PropertyChangedCallback(TileModePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsTileModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AlignmentXProperty =
                  RegisterProperty("AlignmentX",
                                   typeof(AlignmentX),
                                   typeofThis,
                                   AlignmentX.Center,
                                   new PropertyChangedCallback(AlignmentXPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsAlignmentXValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AlignmentYProperty =
                  RegisterProperty("AlignmentY",
                                   typeof(AlignmentY),
                                   typeofThis,
                                   AlignmentY.Center,
                                   new PropertyChangedCallback(AlignmentYPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsAlignmentYValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
