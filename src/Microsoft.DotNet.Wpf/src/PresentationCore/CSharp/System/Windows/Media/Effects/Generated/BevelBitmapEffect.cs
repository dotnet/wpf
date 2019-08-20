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
    sealed partial class BevelBitmapEffect : BitmapEffect
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
        public new BevelBitmapEffect Clone()
        {
            return (BevelBitmapEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BevelBitmapEffect CloneCurrentValue()
        {
            return (BevelBitmapEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void BevelWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BevelBitmapEffect target = ((BevelBitmapEffect) d);


            target.PropertyChanged(BevelWidthProperty);
        }
        private static void ReliefPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BevelBitmapEffect target = ((BevelBitmapEffect) d);


            target.PropertyChanged(ReliefProperty);
        }
        private static void LightAnglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BevelBitmapEffect target = ((BevelBitmapEffect) d);


            target.PropertyChanged(LightAngleProperty);
        }
        private static void SmoothnessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BevelBitmapEffect target = ((BevelBitmapEffect) d);


            target.PropertyChanged(SmoothnessProperty);
        }


        #region Public Properties

        /// <summary>
        ///     BevelWidth - double.  Default value is 5.0.
        /// </summary>
        public double BevelWidth
        {
            get
            {
                return (double) GetValue(BevelWidthProperty);
            }
            set
            {
                SetValueInternal(BevelWidthProperty, value);
            }
        }

        /// <summary>
        ///     Relief - double.  Default value is 0.3.
        /// </summary>
        public double Relief
        {
            get
            {
                return (double) GetValue(ReliefProperty);
            }
            set
            {
                SetValueInternal(ReliefProperty, value);
            }
        }

        /// <summary>
        ///     LightAngle - double.  Default value is 135.0.
        /// </summary>
        public double LightAngle
        {
            get
            {
                return (double) GetValue(LightAngleProperty);
            }
            set
            {
                SetValueInternal(LightAngleProperty, value);
            }
        }

        /// <summary>
        ///     Smoothness - double.  Default value is 0.2.
        /// </summary>
        public double Smoothness
        {
            get
            {
                return (double) GetValue(SmoothnessProperty);
            }
            set
            {
                SetValueInternal(SmoothnessProperty, value);
            }
        }

        /// <summary>
        ///     EdgeProfile - EdgeProfile.  Default value is EdgeProfile.Linear.
        /// </summary>
        public EdgeProfile EdgeProfile
        {
            get
            {
                return (EdgeProfile) GetValue(EdgeProfileProperty);
            }
            set
            {
                SetValueInternal(EdgeProfileProperty, value);
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
            return new BevelBitmapEffect();
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
        ///     The DependencyProperty for the BevelBitmapEffect.BevelWidth property.
        /// </summary>
        public static readonly DependencyProperty BevelWidthProperty;
        /// <summary>
        ///     The DependencyProperty for the BevelBitmapEffect.Relief property.
        /// </summary>
        public static readonly DependencyProperty ReliefProperty;
        /// <summary>
        ///     The DependencyProperty for the BevelBitmapEffect.LightAngle property.
        /// </summary>
        public static readonly DependencyProperty LightAngleProperty;
        /// <summary>
        ///     The DependencyProperty for the BevelBitmapEffect.Smoothness property.
        /// </summary>
        public static readonly DependencyProperty SmoothnessProperty;
        /// <summary>
        ///     The DependencyProperty for the BevelBitmapEffect.EdgeProfile property.
        /// </summary>
        public static readonly DependencyProperty EdgeProfileProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const double c_BevelWidth = 5.0;
        internal const double c_Relief = 0.3;
        internal const double c_LightAngle = 135.0;
        internal const double c_Smoothness = 0.2;
        internal const EdgeProfile c_EdgeProfile = EdgeProfile.Linear;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BevelBitmapEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 



            // Initializations
            Type typeofThis = typeof(BevelBitmapEffect);
            BevelWidthProperty =
                  RegisterProperty("BevelWidth",
                                   typeof(double),
                                   typeofThis,
                                   5.0,
                                   new PropertyChangedCallback(BevelWidthPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ReliefProperty =
                  RegisterProperty("Relief",
                                   typeof(double),
                                   typeofThis,
                                   0.3,
                                   new PropertyChangedCallback(ReliefPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            LightAngleProperty =
                  RegisterProperty("LightAngle",
                                   typeof(double),
                                   typeofThis,
                                   135.0,
                                   new PropertyChangedCallback(LightAnglePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            SmoothnessProperty =
                  RegisterProperty("Smoothness",
                                   typeof(double),
                                   typeofThis,
                                   0.2,
                                   new PropertyChangedCallback(SmoothnessPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            EdgeProfileProperty =
                  RegisterProperty("EdgeProfile",
                                   typeof(EdgeProfile),
                                   typeofThis,
                                   EdgeProfile.Linear,
                                   null,
                                   new ValidateValueCallback(System.Windows.Media.Effects.ValidateEnums.IsEdgeProfileValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
