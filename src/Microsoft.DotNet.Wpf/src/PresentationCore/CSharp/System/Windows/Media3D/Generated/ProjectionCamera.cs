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
    abstract partial class ProjectionCamera : Camera
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
        public new ProjectionCamera Clone()
        {
            return (ProjectionCamera)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ProjectionCamera CloneCurrentValue()
        {
            return (ProjectionCamera)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void NearPlaneDistancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProjectionCamera target = ((ProjectionCamera) d);


            target.PropertyChanged(NearPlaneDistanceProperty);
        }
        private static void FarPlaneDistancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProjectionCamera target = ((ProjectionCamera) d);


            target.PropertyChanged(FarPlaneDistanceProperty);
        }
        private static void PositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProjectionCamera target = ((ProjectionCamera) d);


            target.PropertyChanged(PositionProperty);
        }
        private static void LookDirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProjectionCamera target = ((ProjectionCamera) d);


            target.PropertyChanged(LookDirectionProperty);
        }
        private static void UpDirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProjectionCamera target = ((ProjectionCamera) d);


            target.PropertyChanged(UpDirectionProperty);
        }


        #region Public Properties

        /// <summary>
        ///     NearPlaneDistance - double.  Default value is (double)0.125.
        /// </summary>
        public double NearPlaneDistance
        {
            get
            {
                return (double) GetValue(NearPlaneDistanceProperty);
            }
            set
            {
                SetValueInternal(NearPlaneDistanceProperty, value);
            }
        }

        /// <summary>
        ///     FarPlaneDistance - double.  Default value is (double)Double.PositiveInfinity.
        /// </summary>
        public double FarPlaneDistance
        {
            get
            {
                return (double) GetValue(FarPlaneDistanceProperty);
            }
            set
            {
                SetValueInternal(FarPlaneDistanceProperty, value);
            }
        }

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
        ///     LookDirection - Vector3D.  Default value is new Vector3D(0,0,-1).
        /// </summary>
        public Vector3D LookDirection
        {
            get
            {
                return (Vector3D) GetValue(LookDirectionProperty);
            }
            set
            {
                SetValueInternal(LookDirectionProperty, value);
            }
        }

        /// <summary>
        ///     UpDirection - Vector3D.  Default value is new Vector3D(0,1,0).
        /// </summary>
        public Vector3D UpDirection
        {
            get
            {
                return (Vector3D) GetValue(UpDirectionProperty);
            }
            set
            {
                SetValueInternal(UpDirectionProperty, value);
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
        ///     The DependencyProperty for the ProjectionCamera.NearPlaneDistance property.
        /// </summary>
        public static readonly DependencyProperty NearPlaneDistanceProperty;
        /// <summary>
        ///     The DependencyProperty for the ProjectionCamera.FarPlaneDistance property.
        /// </summary>
        public static readonly DependencyProperty FarPlaneDistanceProperty;
        /// <summary>
        ///     The DependencyProperty for the ProjectionCamera.Position property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty;
        /// <summary>
        ///     The DependencyProperty for the ProjectionCamera.LookDirection property.
        /// </summary>
        public static readonly DependencyProperty LookDirectionProperty;
        /// <summary>
        ///     The DependencyProperty for the ProjectionCamera.UpDirection property.
        /// </summary>
        public static readonly DependencyProperty UpDirectionProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const double c_NearPlaneDistance = (double)0.125;
        internal const double c_FarPlaneDistance = (double)Double.PositiveInfinity;
        internal static Point3D s_Position = new Point3D();
        internal static Vector3D s_LookDirection = new Vector3D(0,0,-1);
        internal static Vector3D s_UpDirection = new Vector3D(0,1,0);

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ProjectionCamera()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(ProjectionCamera);
            NearPlaneDistanceProperty =
                  RegisterProperty("NearPlaneDistance",
                                   typeof(double),
                                   typeofThis,
                                   (double)0.125,
                                   new PropertyChangedCallback(NearPlaneDistancePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            FarPlaneDistanceProperty =
                  RegisterProperty("FarPlaneDistance",
                                   typeof(double),
                                   typeofThis,
                                   (double)Double.PositiveInfinity,
                                   new PropertyChangedCallback(FarPlaneDistancePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            PositionProperty =
                  RegisterProperty("Position",
                                   typeof(Point3D),
                                   typeofThis,
                                   new Point3D(),
                                   new PropertyChangedCallback(PositionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            LookDirectionProperty =
                  RegisterProperty("LookDirection",
                                   typeof(Vector3D),
                                   typeofThis,
                                   new Vector3D(0,0,-1),
                                   new PropertyChangedCallback(LookDirectionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            UpDirectionProperty =
                  RegisterProperty("UpDirection",
                                   typeof(Vector3D),
                                   typeofThis,
                                   new Vector3D(0,1,0),
                                   new PropertyChangedCallback(UpDirectionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
