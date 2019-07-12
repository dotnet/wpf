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
    sealed partial class PathFigure : Animatable
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
        public new PathFigure Clone()
        {
            return (PathFigure)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new PathFigure CloneCurrentValue()
        {
            return (PathFigure)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------




        #region Public Properties

        /// <summary>
        ///     StartPoint - Point.  Default value is new Point().
        /// </summary>
        public Point StartPoint
        {
            get
            {
                return (Point) GetValue(StartPointProperty);
            }
            set
            {
                SetValueInternal(StartPointProperty, value);
            }
        }

        /// <summary>
        ///     IsFilled - bool.  Default value is true.
        /// </summary>
        public bool IsFilled
        {
            get
            {
                return (bool) GetValue(IsFilledProperty);
            }
            set
            {
                SetValueInternal(IsFilledProperty, BooleanBoxes.Box(value));
            }
        }

        /// <summary>
        ///     Segments - PathSegmentCollection.  Default value is new FreezableDefaultValueFactory(PathSegmentCollection.Empty).
        /// </summary>
        public PathSegmentCollection Segments
        {
            get
            {
                return (PathSegmentCollection) GetValue(SegmentsProperty);
            }
            set
            {
                SetValueInternal(SegmentsProperty, value);
            }
        }

        /// <summary>
        ///     IsClosed - bool.  Default value is false.
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return (bool) GetValue(IsClosedProperty);
            }
            set
            {
                SetValueInternal(IsClosedProperty, BooleanBoxes.Box(value));
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
            return new PathFigure();
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

        //
        //  This property finds the correct initial size for the _effectiveValues store on the
        //  current DependencyObject as a performance optimization
        //
        //  This includes:
        //    StartPoint
        //    Segments
        //    IsClosed
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 3;
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
        ///     The DependencyProperty for the PathFigure.StartPoint property.
        /// </summary>
        public static readonly DependencyProperty StartPointProperty;
        /// <summary>
        ///     The DependencyProperty for the PathFigure.IsFilled property.
        /// </summary>
        public static readonly DependencyProperty IsFilledProperty;
        /// <summary>
        ///     The DependencyProperty for the PathFigure.Segments property.
        /// </summary>
        public static readonly DependencyProperty SegmentsProperty;
        /// <summary>
        ///     The DependencyProperty for the PathFigure.IsClosed property.
        /// </summary>
        public static readonly DependencyProperty IsClosedProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static Point s_StartPoint = new Point();
        internal const bool c_IsFilled = true;
        internal static PathSegmentCollection s_Segments = PathSegmentCollection.Empty;
        internal const bool c_IsClosed = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static PathFigure()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.

            Debug.Assert(s_Segments == null || s_Segments.IsFrozen,
                "Detected context bound default value PathFigure.s_Segments (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(PathFigure);
            StartPointProperty =
                  RegisterProperty("StartPoint",
                                   typeof(Point),
                                   typeofThis,
                                   new Point(),
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            IsFilledProperty =
                  RegisterProperty("IsFilled",
                                   typeof(bool),
                                   typeofThis,
                                   true,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            SegmentsProperty =
                  RegisterProperty("Segments",
                                   typeof(PathSegmentCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(PathSegmentCollection.Empty),
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            IsClosedProperty =
                  RegisterProperty("IsClosed",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
