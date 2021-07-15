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
    sealed partial class BitmapEffectInput : Animatable
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
        public new BitmapEffectInput Clone()
        {
            return (BitmapEffectInput)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapEffectInput CloneCurrentValue()
        {
            return (BitmapEffectInput)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void AreaToApplyEffectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapEffectInput target = ((BitmapEffectInput) d);


            target.PropertyChanged(AreaToApplyEffectProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Input - BitmapSource.  Default value is BitmapEffectInput.ContextInputSource.
        /// </summary>
        public BitmapSource Input
        {
            get
            {
                return (BitmapSource) GetValue(InputProperty);
            }
            set
            {
                SetValueInternal(InputProperty, value);
            }
        }

        /// <summary>
        ///     AreaToApplyEffectUnits - BrushMappingMode.  Default value is BrushMappingMode.RelativeToBoundingBox.
        /// </summary>
        public BrushMappingMode AreaToApplyEffectUnits
        {
            get
            {
                return (BrushMappingMode) GetValue(AreaToApplyEffectUnitsProperty);
            }
            set
            {
                SetValueInternal(AreaToApplyEffectUnitsProperty, value);
            }
        }

        /// <summary>
        ///     AreaToApplyEffect - Rect.  Default value is Rect.Empty.
        /// </summary>
        public Rect AreaToApplyEffect
        {
            get
            {
                return (Rect) GetValue(AreaToApplyEffectProperty);
            }
            set
            {
                SetValueInternal(AreaToApplyEffectProperty, value);
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
            return new BitmapEffectInput();
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
        ///     The DependencyProperty for the BitmapEffectInput.Input property.
        /// </summary>
        public static readonly DependencyProperty InputProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapEffectInput.AreaToApplyEffectUnits property.
        /// </summary>
        public static readonly DependencyProperty AreaToApplyEffectUnitsProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapEffectInput.AreaToApplyEffect property.
        /// </summary>
        public static readonly DependencyProperty AreaToApplyEffectProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static BitmapSource s_Input = BitmapEffectInput.ContextInputSource;
        internal const BrushMappingMode c_AreaToApplyEffectUnits = BrushMappingMode.RelativeToBoundingBox;
        internal static Rect s_AreaToApplyEffect = Rect.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BitmapEffectInput()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 
            Debug.Assert(s_Input == null || s_Input.IsFrozen,
                "Detected context bound default value BitmapEffectInput.s_Input");


            // Initializations
            Type typeofThis = typeof(BitmapEffectInput);
            InputProperty =
                  RegisterProperty("Input",
                                   typeof(BitmapSource),
                                   typeofThis,
                                   BitmapEffectInput.ContextInputSource,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AreaToApplyEffectUnitsProperty =
                  RegisterProperty("AreaToApplyEffectUnits",
                                   typeof(BrushMappingMode),
                                   typeofThis,
                                   BrushMappingMode.RelativeToBoundingBox,
                                   null,
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsBrushMappingModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AreaToApplyEffectProperty =
                  RegisterProperty("AreaToApplyEffect",
                                   typeof(Rect),
                                   typeofThis,
                                   Rect.Empty,
                                   new PropertyChangedCallback(AreaToApplyEffectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
