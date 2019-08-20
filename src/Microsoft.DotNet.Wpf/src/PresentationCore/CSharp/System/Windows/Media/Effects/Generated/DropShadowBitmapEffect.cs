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
using MS.Internal.Collections;
using MS.Internal.KnownBoxes;
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
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media.Effects
{
    sealed partial class DropShadowBitmapEffect : BitmapEffect
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
        public new DropShadowBitmapEffect Clone()
        {
            return (DropShadowBitmapEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DropShadowBitmapEffect CloneCurrentValue()
        {
            return (DropShadowBitmapEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ShadowDepthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowBitmapEffect target = ((DropShadowBitmapEffect) d);


            target.PropertyChanged(ShadowDepthProperty);
        }
        private static void ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowBitmapEffect target = ((DropShadowBitmapEffect) d);


            target.PropertyChanged(ColorProperty);
        }
        private static void DirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowBitmapEffect target = ((DropShadowBitmapEffect) d);


            target.PropertyChanged(DirectionProperty);
        }
        private static void NoisePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowBitmapEffect target = ((DropShadowBitmapEffect) d);


            target.PropertyChanged(NoiseProperty);
        }
        private static void OpacityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowBitmapEffect target = ((DropShadowBitmapEffect) d);


            target.PropertyChanged(OpacityProperty);
        }
        private static void SoftnessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowBitmapEffect target = ((DropShadowBitmapEffect) d);


            target.PropertyChanged(SoftnessProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ShadowDepth - double.  Default value is 5.0.
        /// </summary>
        public double ShadowDepth
        {
            get
            {
                return (double) GetValue(ShadowDepthProperty);
            }
            set
            {
                SetValueInternal(ShadowDepthProperty, value);
            }
        }

        /// <summary>
        ///     Color - Color.  Default value is Colors.Black.
        /// </summary>
        public Color Color
        {
            get
            {
                return (Color) GetValue(ColorProperty);
            }
            set
            {
                SetValueInternal(ColorProperty, value);
            }
        }

        /// <summary>
        ///     Direction - double.  Default value is 315.0.
        /// </summary>
        public double Direction
        {
            get
            {
                return (double) GetValue(DirectionProperty);
            }
            set
            {
                SetValueInternal(DirectionProperty, value);
            }
        }

        /// <summary>
        ///     Noise - double.  Default value is 0.0.
        /// </summary>
        public double Noise
        {
            get
            {
                return (double) GetValue(NoiseProperty);
            }
            set
            {
                SetValueInternal(NoiseProperty, value);
            }
        }

        /// <summary>
        ///     Opacity - double.  Default value is 1.0.
        /// </summary>
        public double Opacity
        {
            get
            {
                return (double) GetValue(OpacityProperty);
            }
            set
            {
                SetValueInternal(OpacityProperty, value);
            }
        }

        /// <summary>
        ///     Softness - double.  Default value is 0.5.
        /// </summary>
        public double Softness
        {
            get
            {
                return (double) GetValue(SoftnessProperty);
            }
            set
            {
                SetValueInternal(SoftnessProperty, value);
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
            return new DropShadowBitmapEffect();
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
        ///     The DependencyProperty for the DropShadowBitmapEffect.ShadowDepth property.
        /// </summary>
        public static readonly DependencyProperty ShadowDepthProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowBitmapEffect.Color property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowBitmapEffect.Direction property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowBitmapEffect.Noise property.
        /// </summary>
        public static readonly DependencyProperty NoiseProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowBitmapEffect.Opacity property.
        /// </summary>
        public static readonly DependencyProperty OpacityProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowBitmapEffect.Softness property.
        /// </summary>
        public static readonly DependencyProperty SoftnessProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const double c_ShadowDepth = 5.0;
        internal static Color s_Color = Colors.Black;
        internal const double c_Direction = 315.0;
        internal const double c_Noise = 0.0;
        internal const double c_Opacity = 1.0;
        internal const double c_Softness = 0.5;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DropShadowBitmapEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(DropShadowBitmapEffect);
            ShadowDepthProperty =
                  RegisterProperty("ShadowDepth",
                                   typeof(double),
                                   typeofThis,
                                   5.0,
                                   new PropertyChangedCallback(ShadowDepthPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ColorProperty =
                  RegisterProperty("Color",
                                   typeof(Color),
                                   typeofThis,
                                   Colors.Black,
                                   new PropertyChangedCallback(ColorPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            DirectionProperty =
                  RegisterProperty("Direction",
                                   typeof(double),
                                   typeofThis,
                                   315.0,
                                   new PropertyChangedCallback(DirectionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            NoiseProperty =
                  RegisterProperty("Noise",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(NoisePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OpacityProperty =
                  RegisterProperty("Opacity",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(OpacityPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            SoftnessProperty =
                  RegisterProperty("Softness",
                                   typeof(double),
                                   typeofThis,
                                   0.5,
                                   new PropertyChangedCallback(SoftnessPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
