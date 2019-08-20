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
    sealed partial class TextEffect : Animatable
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
        public new TextEffect Clone()
        {
            return (TextEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TextEffect CloneCurrentValue()
        {
            return (TextEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static bool ValidatePositionStartValue(object value)
        {
            // This resource needs to be notified on new values being set.
            if (!OnPositionStartChanging((int) value))
            {
                return false;
            }
            return true;
        }
        private static bool ValidatePositionCountValue(object value)
        {
            // This resource needs to be notified on new values being set.
            if (!OnPositionCountChanging((int) value))
            {
                return false;
            }
            return true;
        }


        #region Public Properties

        /// <summary>
        ///     Transform - Transform.  Default value is null.
        ///     Transform on the text with this effect
        /// </summary>
        public Transform Transform
        {
            get
            {
                return (Transform) GetValue(TransformProperty);
            }
            set
            {
                SetValueInternal(TransformProperty, value);
            }
        }

        /// <summary>
        ///     Clip - Geometry.  Default value is null.
        ///     Clip on the text with this effect
        /// </summary>
        public Geometry Clip
        {
            get
            {
                return (Geometry) GetValue(ClipProperty);
            }
            set
            {
                SetValueInternal(ClipProperty, value);
            }
        }

        /// <summary>
        ///     Foreground - Brush.  Default value is null.
        ///     Foreground for the text with this effect
        /// </summary>
        public Brush Foreground
        {
            get
            {
                return (Brush) GetValue(ForegroundProperty);
            }
            set
            {
                SetValueInternal(ForegroundProperty, value);
            }
        }

        /// <summary>
        ///     PositionStart - int.  Default value is 0.
        ///     The codepoint index of the start of the text effect
        /// </summary>
        public int PositionStart
        {
            get
            {
                return (int) GetValue(PositionStartProperty);
            }
            set
            {
                SetValueInternal(PositionStartProperty, value);
            }
        }

        /// <summary>
        ///     PositionCount - int.  Default value is 0.
        ///     The glyph or codepoint count, based on the graunularity
        /// </summary>
        public int PositionCount
        {
            get
            {
                return (int) GetValue(PositionCountProperty);
            }
            set
            {
                SetValueInternal(PositionCountProperty, value);
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
            return new TextEffect();
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
        ///     The DependencyProperty for the TextEffect.Transform property.
        /// </summary>
        public static readonly DependencyProperty TransformProperty;
        /// <summary>
        ///     The DependencyProperty for the TextEffect.Clip property.
        /// </summary>
        public static readonly DependencyProperty ClipProperty;
        /// <summary>
        ///     The DependencyProperty for the TextEffect.Foreground property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty;
        /// <summary>
        ///     The DependencyProperty for the TextEffect.PositionStart property.
        /// </summary>
        public static readonly DependencyProperty PositionStartProperty;
        /// <summary>
        ///     The DependencyProperty for the TextEffect.PositionCount property.
        /// </summary>
        public static readonly DependencyProperty PositionCountProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal const int c_PositionStart = 0;
        internal const int c_PositionCount = 0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TextEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 



            // Initializations
            Type typeofThis = typeof(TextEffect);
            TransformProperty =
                  RegisterProperty("Transform",
                                   typeof(Transform),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ClipProperty =
                  RegisterProperty("Clip",
                                   typeof(Geometry),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ForegroundProperty =
                  RegisterProperty("Foreground",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            PositionStartProperty =
                  RegisterProperty("PositionStart",
                                   typeof(int),
                                   typeofThis,
                                   0,
                                   null,
                                   new ValidateValueCallback(ValidatePositionStartValue),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            PositionCountProperty =
                  RegisterProperty("PositionCount",
                                   typeof(int),
                                   typeofThis,
                                   0,
                                   null,
                                   new ValidateValueCallback(ValidatePositionCountValue),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
