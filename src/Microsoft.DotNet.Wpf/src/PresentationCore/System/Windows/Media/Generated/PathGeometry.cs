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
    sealed partial class PathGeometry : Geometry
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
        public new PathGeometry Clone()
        {
            return (PathGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new PathGeometry CloneCurrentValue()
        {
            return (PathGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void FillRulePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PathGeometry target = ((PathGeometry) d);


            target.PropertyChanged(FillRuleProperty);
        }
        private static void FiguresPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PathGeometry target = ((PathGeometry) d);


            target.FiguresPropertyChangedHook(e);




            target.PropertyChanged(FiguresProperty);
        }


        #region Public Properties

        /// <summary>
        ///     FillRule - FillRule.  Default value is FillRule.EvenOdd.
        /// </summary>
        public FillRule FillRule
        {
            get
            {
                return (FillRule) GetValue(FillRuleProperty);
            }
            set
            {
                SetValueInternal(FillRuleProperty, FillRuleBoxes.Box(value));
            }
        }

        /// <summary>
        ///     Figures - PathFigureCollection.  Default value is new FreezableDefaultValueFactory(PathFigureCollection.Empty).
        /// </summary>
        public PathFigureCollection Figures
        {
            get
            {
                return (PathFigureCollection) GetValue(FiguresProperty);
            }
            set
            {
                SetValueInternal(FiguresProperty, value);
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
            return new PathGeometry();
        }



        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal override void UpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            ManualUpdateResource(channel, skipOnChannelCheck);
            base.UpdateResource(channel, skipOnChannelCheck);
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_PATHGEOMETRY))
                {
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);

                    AddRefOnChannelAnimations(channel);


                    UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
                }

                return _duceResource.GetHandle(channel);
}
        internal override void ReleaseOnChannelCore(DUCE.Channel channel)
        {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                if (_duceResource.ReleaseOnChannel(channel))
                {
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).ReleaseOnChannel(channel);

                    ReleaseOnChannelAnimations(channel);
}
}
        internal override DUCE.ResourceHandle GetHandleCore(DUCE.Channel channel)
        {
            // Note that we are in a lock here already.
            return _duceResource.GetHandle(channel);
        }
        internal override int GetChannelCountCore()
        {
            // must already be in composition lock here
            return _duceResource.GetChannelCount();
        }
        internal override DUCE.Channel GetChannelCore(int index)
        {
            // Note that we are in a lock here already.
            return _duceResource.GetChannel(index);
        }


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
        //    Figures
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 1;
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
        ///     The DependencyProperty for the PathGeometry.FillRule property.
        /// </summary>
        public static readonly DependencyProperty FillRuleProperty;
        /// <summary>
        ///     The DependencyProperty for the PathGeometry.Figures property.
        /// </summary>
        public static readonly DependencyProperty FiguresProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const FillRule c_FillRule = FillRule.EvenOdd;
        internal static PathFigureCollection s_Figures = PathFigureCollection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static PathGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 

            Debug.Assert(s_Figures == null || s_Figures.IsFrozen,
                "Detected context bound default value PathGeometry.s_Figures (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(PathGeometry);
            FillRuleProperty =
                  RegisterProperty("FillRule",
                                   typeof(FillRule),
                                   typeofThis,
                                   FillRule.EvenOdd,
                                   new PropertyChangedCallback(FillRulePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsFillRuleValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            FiguresProperty =
                  RegisterProperty("Figures",
                                   typeof(PathFigureCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(PathFigureCollection.Empty),
                                   new PropertyChangedCallback(FiguresPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
