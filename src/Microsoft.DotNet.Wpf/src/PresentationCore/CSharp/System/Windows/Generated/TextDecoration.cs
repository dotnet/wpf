// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Converters;
using MS.Internal.Collections;
using MS.Utility;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows
{
    sealed partial class TextDecoration : Animatable
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
        public new TextDecoration Clone()
        {
            return (TextDecoration)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TextDecoration CloneCurrentValue()
        {
            return (TextDecoration)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------




        #region Public Properties

        /// <summary>
        ///     Pen - Pen.  Default value is null.
        ///     The pen used to draw the text decoration
        /// </summary>
        public Pen Pen
        {
            get
            {
                return (Pen) GetValue(PenProperty);
            }
            set
            {
                SetValueInternal(PenProperty, value);
            }
        }

        /// <summary>
        ///     PenOffset - double.  Default value is 0.0.
        ///     The offset of the text decoration to the location specified.
        /// </summary>
        public double PenOffset
        {
            get
            {
                return (double) GetValue(PenOffsetProperty);
            }
            set
            {
                SetValueInternal(PenOffsetProperty, value);
            }
        }

        /// <summary>
        ///     PenOffsetUnit - TextDecorationUnit.  Default value is TextDecorationUnit.FontRecommended.
        ///     The unit type we use to interpret the offset value.
        /// </summary>
        public TextDecorationUnit PenOffsetUnit
        {
            get
            {
                return (TextDecorationUnit) GetValue(PenOffsetUnitProperty);
            }
            set
            {
                SetValueInternal(PenOffsetUnitProperty, value);
            }
        }

        /// <summary>
        ///     PenThicknessUnit - TextDecorationUnit.  Default value is TextDecorationUnit.FontRecommended.
        ///     The unit type we use to interpret the thickness value.
        /// </summary>
        public TextDecorationUnit PenThicknessUnit
        {
            get
            {
                return (TextDecorationUnit) GetValue(PenThicknessUnitProperty);
            }
            set
            {
                SetValueInternal(PenThicknessUnitProperty, value);
            }
        }

        /// <summary>
        ///     Location - TextDecorationLocation.  Default value is TextDecorationLocation.Underline.
        ///     The Location of the text decorations
        /// </summary>
        public TextDecorationLocation Location
        {
            get
            {
                return (TextDecorationLocation) GetValue(LocationProperty);
            }
            set
            {
                SetValueInternal(LocationProperty, value);
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
            return new TextDecoration();
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
        ///     The DependencyProperty for the TextDecoration.Pen property.
        /// </summary>
        public static readonly DependencyProperty PenProperty;
        /// <summary>
        ///     The DependencyProperty for the TextDecoration.PenOffset property.
        /// </summary>
        public static readonly DependencyProperty PenOffsetProperty;
        /// <summary>
        ///     The DependencyProperty for the TextDecoration.PenOffsetUnit property.
        /// </summary>
        public static readonly DependencyProperty PenOffsetUnitProperty;
        /// <summary>
        ///     The DependencyProperty for the TextDecoration.PenThicknessUnit property.
        /// </summary>
        public static readonly DependencyProperty PenThicknessUnitProperty;
        /// <summary>
        ///     The DependencyProperty for the TextDecoration.Location property.
        /// </summary>
        public static readonly DependencyProperty LocationProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const double c_PenOffset = 0.0;
        internal const TextDecorationUnit c_PenOffsetUnit = TextDecorationUnit.FontRecommended;
        internal const TextDecorationUnit c_PenThicknessUnit = TextDecorationUnit.FontRecommended;
        internal const TextDecorationLocation c_Location = TextDecorationLocation.Underline;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TextDecoration()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 



            // Initializations
            Type typeofThis = typeof(TextDecoration);
            PenProperty =
                  RegisterProperty("Pen",
                                   typeof(Pen),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            PenOffsetProperty =
                  RegisterProperty("PenOffset",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            PenOffsetUnitProperty =
                  RegisterProperty("PenOffsetUnit",
                                   typeof(TextDecorationUnit),
                                   typeofThis,
                                   TextDecorationUnit.FontRecommended,
                                   null,
                                   new ValidateValueCallback(System.Windows.ValidateEnums.IsTextDecorationUnitValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            PenThicknessUnitProperty =
                  RegisterProperty("PenThicknessUnit",
                                   typeof(TextDecorationUnit),
                                   typeofThis,
                                   TextDecorationUnit.FontRecommended,
                                   null,
                                   new ValidateValueCallback(System.Windows.ValidateEnums.IsTextDecorationUnitValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            LocationProperty =
                  RegisterProperty("Location",
                                   typeof(TextDecorationLocation),
                                   typeofThis,
                                   TextDecorationLocation.Underline,
                                   null,
                                   new ValidateValueCallback(System.Windows.ValidateEnums.IsTextDecorationLocationValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
