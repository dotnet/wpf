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
    sealed partial class ScaleTransform : Transform
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
        public new ScaleTransform Clone()
        {
            return (ScaleTransform)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ScaleTransform CloneCurrentValue()
        {
            return (ScaleTransform)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ScaleXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(ScaleXProperty);
        }
        private static void ScaleYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(ScaleYProperty);
        }
        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(CenterYProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ScaleX - double.  Default value is 1.0.
        /// </summary>
        public double ScaleX
        {
            get
            {
                return (double) GetValue(ScaleXProperty);
            }
            set
            {
                SetValueInternal(ScaleXProperty, value);
            }
        }

        /// <summary>
        ///     ScaleY - double.  Default value is 1.0.
        /// </summary>
        public double ScaleY
        {
            get
            {
                return (double) GetValue(ScaleYProperty);
            }
            set
            {
                SetValueInternal(ScaleYProperty, value);
            }
        }

        /// <summary>
        ///     CenterX - double.  Default value is 0.0.
        /// </summary>
        public double CenterX
        {
            get
            {
                return (double) GetValue(CenterXProperty);
            }
            set
            {
                SetValueInternal(CenterXProperty, value);
            }
        }

        /// <summary>
        ///     CenterY - double.  Default value is 0.0.
        /// </summary>
        public double CenterY
        {
            get
            {
                return (double) GetValue(CenterYProperty);
            }
            set
            {
                SetValueInternal(CenterYProperty, value);
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
            return new ScaleTransform();
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
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                base.UpdateResource(channel, skipOnChannelCheck);

                // Obtain handles for animated properties
                DUCE.ResourceHandle hScaleXAnimations = GetAnimationResourceHandle(ScaleXProperty, channel);
                DUCE.ResourceHandle hScaleYAnimations = GetAnimationResourceHandle(ScaleYProperty, channel);
                DUCE.ResourceHandle hCenterXAnimations = GetAnimationResourceHandle(CenterXProperty, channel);
                DUCE.ResourceHandle hCenterYAnimations = GetAnimationResourceHandle(CenterYProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_SCALETRANSFORM data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdScaleTransform;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hScaleXAnimations.IsNull)
                    {
                        data.ScaleX = ScaleX;
                    }
                    data.hScaleXAnimations = hScaleXAnimations;
                    if (hScaleYAnimations.IsNull)
                    {
                        data.ScaleY = ScaleY;
                    }
                    data.hScaleYAnimations = hScaleYAnimations;
                    if (hCenterXAnimations.IsNull)
                    {
                        data.CenterX = CenterX;
                    }
                    data.hCenterXAnimations = hCenterXAnimations;
                    if (hCenterYAnimations.IsNull)
                    {
                        data.CenterY = CenterY;
                    }
                    data.hCenterYAnimations = hCenterYAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_SCALETRANSFORM));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_SCALETRANSFORM))
                {
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





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.ScaleX property.
        /// </summary>
        public static readonly DependencyProperty ScaleXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.ScaleY property.
        /// </summary>
        public static readonly DependencyProperty ScaleYProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_ScaleX = 1.0;
        internal const double c_ScaleY = 1.0;
        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ScaleTransform()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(ScaleTransform);
            ScaleXProperty =
                  RegisterProperty("ScaleX",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ScaleXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ScaleYProperty =
                  RegisterProperty("ScaleY",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ScaleYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            CenterXProperty =
                  RegisterProperty("CenterX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(CenterXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            CenterYProperty =
                  RegisterProperty("CenterY",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(CenterYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
