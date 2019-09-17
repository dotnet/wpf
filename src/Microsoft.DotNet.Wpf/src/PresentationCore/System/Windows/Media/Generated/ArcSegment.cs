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
    sealed partial class ArcSegment : PathSegment
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
        public new ArcSegment Clone()
        {
            return (ArcSegment)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ArcSegment CloneCurrentValue()
        {
            return (ArcSegment)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------




        #region Public Properties

        /// <summary>
        ///     Point - Point.  Default value is new Point().
        /// </summary>
        public Point Point
        {
            get
            {
                return (Point) GetValue(PointProperty);
            }
            set
            {
                SetValueInternal(PointProperty, value);
            }
        }

        /// <summary>
        ///     Size - Size.  Default value is new Size().
        /// </summary>
        public Size Size
        {
            get
            {
                return (Size) GetValue(SizeProperty);
            }
            set
            {
                SetValueInternal(SizeProperty, value);
            }
        }

        /// <summary>
        ///     RotationAngle - double.  Default value is 0.0.
        /// </summary>
        public double RotationAngle
        {
            get
            {
                return (double) GetValue(RotationAngleProperty);
            }
            set
            {
                SetValueInternal(RotationAngleProperty, value);
            }
        }

        /// <summary>
        ///     IsLargeArc - bool.  Default value is false.
        /// </summary>
        public bool IsLargeArc
        {
            get
            {
                return (bool) GetValue(IsLargeArcProperty);
            }
            set
            {
                SetValueInternal(IsLargeArcProperty, BooleanBoxes.Box(value));
            }
        }

        /// <summary>
        ///     SweepDirection - SweepDirection.  Default value is SweepDirection.Counterclockwise.
        /// </summary>
        public SweepDirection SweepDirection
        {
            get
            {
                return (SweepDirection) GetValue(SweepDirectionProperty);
            }
            set
            {
                SetValueInternal(SweepDirectionProperty, value);
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
            return new ArcSegment();
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
        ///     The DependencyProperty for the ArcSegment.Point property.
        /// </summary>
        public static readonly DependencyProperty PointProperty;
        /// <summary>
        ///     The DependencyProperty for the ArcSegment.Size property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty;
        /// <summary>
        ///     The DependencyProperty for the ArcSegment.RotationAngle property.
        /// </summary>
        public static readonly DependencyProperty RotationAngleProperty;
        /// <summary>
        ///     The DependencyProperty for the ArcSegment.IsLargeArc property.
        /// </summary>
        public static readonly DependencyProperty IsLargeArcProperty;
        /// <summary>
        ///     The DependencyProperty for the ArcSegment.SweepDirection property.
        /// </summary>
        public static readonly DependencyProperty SweepDirectionProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static Point s_Point = new Point();
        internal static Size s_Size = new Size();
        internal const double c_RotationAngle = 0.0;
        internal const bool c_IsLargeArc = false;
        internal const SweepDirection c_SweepDirection = SweepDirection.Counterclockwise;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ArcSegment()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 



            // Initializations
            Type typeofThis = typeof(ArcSegment);
            PointProperty =
                  RegisterProperty("Point",
                                   typeof(Point),
                                   typeofThis,
                                   new Point(),
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            SizeProperty =
                  RegisterProperty("Size",
                                   typeof(Size),
                                   typeofThis,
                                   new Size(),
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSize));
            RotationAngleProperty =
                  RegisterProperty("RotationAngle",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            IsLargeArcProperty =
                  RegisterProperty("IsLargeArc",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            SweepDirectionProperty =
                  RegisterProperty("SweepDirection",
                                   typeof(SweepDirection),
                                   typeofThis,
                                   SweepDirection.Counterclockwise,
                                   null,
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsSweepDirectionValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
