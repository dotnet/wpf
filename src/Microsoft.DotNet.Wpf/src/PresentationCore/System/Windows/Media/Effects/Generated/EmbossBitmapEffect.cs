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
    sealed partial class EmbossBitmapEffect : BitmapEffect
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
        public new EmbossBitmapEffect Clone()
        {
            return (EmbossBitmapEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new EmbossBitmapEffect CloneCurrentValue()
        {
            return (EmbossBitmapEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void LightAnglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EmbossBitmapEffect target = ((EmbossBitmapEffect) d);


            target.PropertyChanged(LightAngleProperty);
        }
        private static void ReliefPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EmbossBitmapEffect target = ((EmbossBitmapEffect) d);


            target.PropertyChanged(ReliefProperty);
        }


        #region Public Properties

        /// <summary>
        ///     LightAngle - double.  Default value is 45.0.
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
        ///     Relief - double.  Default value is 0.44.
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
            return new EmbossBitmapEffect();
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
        ///     The DependencyProperty for the EmbossBitmapEffect.LightAngle property.
        /// </summary>
        public static readonly DependencyProperty LightAngleProperty;
        /// <summary>
        ///     The DependencyProperty for the EmbossBitmapEffect.Relief property.
        /// </summary>
        public static readonly DependencyProperty ReliefProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const double c_LightAngle = 45.0;
        internal const double c_Relief = 0.44;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static EmbossBitmapEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app



            // Initializations
            Type typeofThis = typeof(EmbossBitmapEffect);
            LightAngleProperty =
                  RegisterProperty("LightAngle",
                                   typeof(double),
                                   typeofThis,
                                   45.0,
                                   new PropertyChangedCallback(LightAnglePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ReliefProperty =
                  RegisterProperty("Relief",
                                   typeof(double),
                                   typeofThis,
                                   0.44,
                                   new PropertyChangedCallback(ReliefPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
