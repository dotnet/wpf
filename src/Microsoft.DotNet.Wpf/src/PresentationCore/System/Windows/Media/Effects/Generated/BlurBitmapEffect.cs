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
    sealed partial class BlurBitmapEffect : BitmapEffect
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
        public new BlurBitmapEffect Clone()
        {
            return (BlurBitmapEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BlurBitmapEffect CloneCurrentValue()
        {
            return (BlurBitmapEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BlurBitmapEffect target = ((BlurBitmapEffect) d);


            target.PropertyChanged(RadiusProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Radius - double.  Default value is 5.0.
        /// </summary>
        public double Radius
        {
            get
            {
                return (double) GetValue(RadiusProperty);
            }
            set
            {
                SetValueInternal(RadiusProperty, value);
            }
        }

        /// <summary>
        ///     KernelType - KernelType.  Default value is KernelType.Gaussian.
        /// </summary>
        public KernelType KernelType
        {
            get
            {
                return (KernelType) GetValue(KernelTypeProperty);
            }
            set
            {
                SetValueInternal(KernelTypeProperty, value);
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
            return new BlurBitmapEffect();
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
        ///     The DependencyProperty for the BlurBitmapEffect.Radius property.
        /// </summary>
        public static readonly DependencyProperty RadiusProperty;
        /// <summary>
        ///     The DependencyProperty for the BlurBitmapEffect.KernelType property.
        /// </summary>
        public static readonly DependencyProperty KernelTypeProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const double c_Radius = 5.0;
        internal const KernelType c_KernelType = KernelType.Gaussian;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BlurBitmapEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(BlurBitmapEffect);
            RadiusProperty =
                  RegisterProperty("Radius",
                                   typeof(double),
                                   typeofThis,
                                   5.0,
                                   new PropertyChangedCallback(RadiusPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            KernelTypeProperty =
                  RegisterProperty("KernelType",
                                   typeof(KernelType),
                                   typeofThis,
                                   KernelType.Gaussian,
                                   null,
                                   new ValidateValueCallback(System.Windows.Media.Effects.ValidateEnums.IsKernelTypeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
