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
    abstract partial class GradientBrush : Brush
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
        public new GradientBrush Clone()
        {
            return (GradientBrush)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GradientBrush CloneCurrentValue()
        {
            return (GradientBrush)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ColorInterpolationModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GradientBrush target = ((GradientBrush) d);


            target.PropertyChanged(ColorInterpolationModeProperty);
        }
        private static void MappingModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GradientBrush target = ((GradientBrush) d);


            target.PropertyChanged(MappingModeProperty);
        }
        private static void SpreadMethodPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GradientBrush target = ((GradientBrush) d);


            target.PropertyChanged(SpreadMethodProperty);
        }
        private static void GradientStopsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GradientBrush target = ((GradientBrush) d);


            target.PropertyChanged(GradientStopsProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ColorInterpolationMode - ColorInterpolationMode.  Default value is ColorInterpolationMode.SRgbLinearInterpolation.
        /// </summary>
        public ColorInterpolationMode ColorInterpolationMode
        {
            get
            {
                return (ColorInterpolationMode) GetValue(ColorInterpolationModeProperty);
            }
            set
            {
                SetValueInternal(ColorInterpolationModeProperty, value);
            }
        }

        /// <summary>
        ///     MappingMode - BrushMappingMode.  Default value is BrushMappingMode.RelativeToBoundingBox.
        /// </summary>
        public BrushMappingMode MappingMode
        {
            get
            {
                return (BrushMappingMode) GetValue(MappingModeProperty);
            }
            set
            {
                SetValueInternal(MappingModeProperty, value);
            }
        }

        /// <summary>
        ///     SpreadMethod - GradientSpreadMethod.  Default value is GradientSpreadMethod.Pad.
        /// </summary>
        public GradientSpreadMethod SpreadMethod
        {
            get
            {
                return (GradientSpreadMethod) GetValue(SpreadMethodProperty);
            }
            set
            {
                SetValueInternal(SpreadMethodProperty, value);
            }
        }

        /// <summary>
        ///     GradientStops - GradientStopCollection.  Default value is new FreezableDefaultValueFactory(GradientStopCollection.Empty).
        /// </summary>
        public GradientStopCollection GradientStops
        {
            get
            {
                return (GradientStopCollection) GetValue(GradientStopsProperty);
            }
            set
            {
                SetValueInternal(GradientStopsProperty, value);
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

        //
        //  This property finds the correct initial size for the _effectiveValues store on the
        //  current DependencyObject as a performance optimization
        //
        //  This includes:
        //    GradientStops
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 1;
            }
        }



        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the GradientBrush.ColorInterpolationMode property.
        /// </summary>
        public static readonly DependencyProperty ColorInterpolationModeProperty;
        /// <summary>
        ///     The DependencyProperty for the GradientBrush.MappingMode property.
        /// </summary>
        public static readonly DependencyProperty MappingModeProperty;
        /// <summary>
        ///     The DependencyProperty for the GradientBrush.SpreadMethod property.
        /// </summary>
        public static readonly DependencyProperty SpreadMethodProperty;
        /// <summary>
        ///     The DependencyProperty for the GradientBrush.GradientStops property.
        /// </summary>
        public static readonly DependencyProperty GradientStopsProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const ColorInterpolationMode c_ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;
        internal const BrushMappingMode c_MappingMode = BrushMappingMode.RelativeToBoundingBox;
        internal const GradientSpreadMethod c_SpreadMethod = GradientSpreadMethod.Pad;
        internal static GradientStopCollection s_GradientStops = GradientStopCollection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static GradientBrush()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_GradientStops == null || s_GradientStops.IsFrozen,
                "Detected context bound default value GradientBrush.s_GradientStops (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(GradientBrush);
            ColorInterpolationModeProperty =
                  RegisterProperty("ColorInterpolationMode",
                                   typeof(ColorInterpolationMode),
                                   typeofThis,
                                   ColorInterpolationMode.SRgbLinearInterpolation,
                                   new PropertyChangedCallback(ColorInterpolationModePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsColorInterpolationModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            MappingModeProperty =
                  RegisterProperty("MappingMode",
                                   typeof(BrushMappingMode),
                                   typeofThis,
                                   BrushMappingMode.RelativeToBoundingBox,
                                   new PropertyChangedCallback(MappingModePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsBrushMappingModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            SpreadMethodProperty =
                  RegisterProperty("SpreadMethod",
                                   typeof(GradientSpreadMethod),
                                   typeofThis,
                                   GradientSpreadMethod.Pad,
                                   new PropertyChangedCallback(SpreadMethodPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsGradientSpreadMethodValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            GradientStopsProperty =
                  RegisterProperty("GradientStops",
                                   typeof(GradientStopCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(GradientStopCollection.Empty),
                                   new PropertyChangedCallback(GradientStopsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
