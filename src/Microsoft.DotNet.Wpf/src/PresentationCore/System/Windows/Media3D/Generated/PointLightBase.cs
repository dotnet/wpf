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
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media.Media3D.Converters;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Windows.Media.Imaging;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media.Media3D
{
    abstract partial class PointLightBase : Light
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
        public new PointLightBase Clone()
        {
            return (PointLightBase)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new PointLightBase CloneCurrentValue()
        {
            return (PointLightBase)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void PositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PointLightBase target = ((PointLightBase) d);


            target.PropertyChanged(PositionProperty);
        }
        private static void RangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PointLightBase target = ((PointLightBase) d);


            target.PropertyChanged(RangeProperty);
        }
        private static void ConstantAttenuationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PointLightBase target = ((PointLightBase) d);


            target.PropertyChanged(ConstantAttenuationProperty);
        }
        private static void LinearAttenuationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PointLightBase target = ((PointLightBase) d);


            target.PropertyChanged(LinearAttenuationProperty);
        }
        private static void QuadraticAttenuationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PointLightBase target = ((PointLightBase) d);


            target.PropertyChanged(QuadraticAttenuationProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Position - Point3D.  Default value is new Point3D().
        /// </summary>
        public Point3D Position
        {
            get
            {
                return (Point3D) GetValue(PositionProperty);
            }
            set
            {
                SetValueInternal(PositionProperty, value);
            }
        }

        /// <summary>
        ///     Range - double.  Default value is Double.PositiveInfinity.
        /// </summary>
        public double Range
        {
            get
            {
                return (double) GetValue(RangeProperty);
            }
            set
            {
                SetValueInternal(RangeProperty, value);
            }
        }

        /// <summary>
        ///     ConstantAttenuation - double.  Default value is 1.0.
        /// </summary>
        public double ConstantAttenuation
        {
            get
            {
                return (double) GetValue(ConstantAttenuationProperty);
            }
            set
            {
                SetValueInternal(ConstantAttenuationProperty, value);
            }
        }

        /// <summary>
        ///     LinearAttenuation - double.  Default value is 0.0.
        /// </summary>
        public double LinearAttenuation
        {
            get
            {
                return (double) GetValue(LinearAttenuationProperty);
            }
            set
            {
                SetValueInternal(LinearAttenuationProperty, value);
            }
        }

        /// <summary>
        ///     QuadraticAttenuation - double.  Default value is 0.0.
        /// </summary>
        public double QuadraticAttenuation
        {
            get
            {
                return (double) GetValue(QuadraticAttenuationProperty);
            }
            set
            {
                SetValueInternal(QuadraticAttenuationProperty, value);
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
        ///     The DependencyProperty for the PointLightBase.Position property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty;
        /// <summary>
        ///     The DependencyProperty for the PointLightBase.Range property.
        /// </summary>
        public static readonly DependencyProperty RangeProperty;
        /// <summary>
        ///     The DependencyProperty for the PointLightBase.ConstantAttenuation property.
        /// </summary>
        public static readonly DependencyProperty ConstantAttenuationProperty;
        /// <summary>
        ///     The DependencyProperty for the PointLightBase.LinearAttenuation property.
        /// </summary>
        public static readonly DependencyProperty LinearAttenuationProperty;
        /// <summary>
        ///     The DependencyProperty for the PointLightBase.QuadraticAttenuation property.
        /// </summary>
        public static readonly DependencyProperty QuadraticAttenuationProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static Point3D s_Position = new Point3D();
        internal const double c_Range = Double.PositiveInfinity;
        internal const double c_ConstantAttenuation = 1.0;
        internal const double c_LinearAttenuation = 0.0;
        internal const double c_QuadraticAttenuation = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static PointLightBase()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(PointLightBase);
            PositionProperty =
                  RegisterProperty("Position",
                                   typeof(Point3D),
                                   typeofThis,
                                   new Point3D(),
                                   new PropertyChangedCallback(PositionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            RangeProperty =
                  RegisterProperty("Range",
                                   typeof(double),
                                   typeofThis,
                                   Double.PositiveInfinity,
                                   new PropertyChangedCallback(RangePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ConstantAttenuationProperty =
                  RegisterProperty("ConstantAttenuation",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ConstantAttenuationPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            LinearAttenuationProperty =
                  RegisterProperty("LinearAttenuation",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(LinearAttenuationPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            QuadraticAttenuationProperty =
                  RegisterProperty("QuadraticAttenuation",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(QuadraticAttenuationPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
