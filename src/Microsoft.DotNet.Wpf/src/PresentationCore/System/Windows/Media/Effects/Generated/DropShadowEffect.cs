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
    sealed partial class DropShadowEffect : Effect
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
        public new DropShadowEffect Clone()
        {
            return (DropShadowEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DropShadowEffect CloneCurrentValue()
        {
            return (DropShadowEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ShadowDepthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowEffect target = ((DropShadowEffect) d);


            target.PropertyChanged(ShadowDepthProperty);
        }
        private static void ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowEffect target = ((DropShadowEffect) d);


            target.PropertyChanged(ColorProperty);
        }
        private static void DirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowEffect target = ((DropShadowEffect) d);


            target.PropertyChanged(DirectionProperty);
        }
        private static void OpacityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowEffect target = ((DropShadowEffect) d);


            target.PropertyChanged(OpacityProperty);
        }
        private static void BlurRadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowEffect target = ((DropShadowEffect) d);


            target.PropertyChanged(BlurRadiusProperty);
        }
        private static void RenderingBiasPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropShadowEffect target = ((DropShadowEffect) d);


            target.PropertyChanged(RenderingBiasProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ShadowDepth - double.  Default value is 5.0.
        /// </summary>
        public double ShadowDepth
        {
            get
            {
                return (double) GetValue(ShadowDepthProperty);
            }
            set
            {
                SetValueInternal(ShadowDepthProperty, value);
            }
        }

        /// <summary>
        ///     Color - Color.  Default value is Colors.Black.
        /// </summary>
        public Color Color
        {
            get
            {
                return (Color) GetValue(ColorProperty);
            }
            set
            {
                SetValueInternal(ColorProperty, value);
            }
        }

        /// <summary>
        ///     Direction - double.  Default value is 315.0.
        /// </summary>
        public double Direction
        {
            get
            {
                return (double) GetValue(DirectionProperty);
            }
            set
            {
                SetValueInternal(DirectionProperty, value);
            }
        }

        /// <summary>
        ///     Opacity - double.  Default value is 1.0.
        /// </summary>
        public double Opacity
        {
            get
            {
                return (double) GetValue(OpacityProperty);
            }
            set
            {
                SetValueInternal(OpacityProperty, value);
            }
        }

        /// <summary>
        ///     BlurRadius - double.  Default value is 5.0.
        /// </summary>
        public double BlurRadius
        {
            get
            {
                return (double) GetValue(BlurRadiusProperty);
            }
            set
            {
                SetValueInternal(BlurRadiusProperty, value);
            }
        }

        /// <summary>
        ///     RenderingBias - RenderingBias.  Default value is RenderingBias.Performance.
        /// </summary>
        public RenderingBias RenderingBias
        {
            get
            {
                return (RenderingBias) GetValue(RenderingBiasProperty);
            }
            set
            {
                SetValueInternal(RenderingBiasProperty, value);
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
            return new DropShadowEffect();
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
                DUCE.ResourceHandle hShadowDepthAnimations = GetAnimationResourceHandle(ShadowDepthProperty, channel);
                DUCE.ResourceHandle hColorAnimations = GetAnimationResourceHandle(ColorProperty, channel);
                DUCE.ResourceHandle hDirectionAnimations = GetAnimationResourceHandle(DirectionProperty, channel);
                DUCE.ResourceHandle hOpacityAnimations = GetAnimationResourceHandle(OpacityProperty, channel);
                DUCE.ResourceHandle hBlurRadiusAnimations = GetAnimationResourceHandle(BlurRadiusProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_DROPSHADOWEFFECT data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdDropShadowEffect;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hShadowDepthAnimations.IsNull)
                    {
                        data.ShadowDepth = ShadowDepth;
                    }
                    data.hShadowDepthAnimations = hShadowDepthAnimations;
                    if (hColorAnimations.IsNull)
                    {
                        data.Color = CompositionResourceManager.ColorToMilColorF(Color);
                    }
                    data.hColorAnimations = hColorAnimations;
                    if (hDirectionAnimations.IsNull)
                    {
                        data.Direction = Direction;
                    }
                    data.hDirectionAnimations = hDirectionAnimations;
                    if (hOpacityAnimations.IsNull)
                    {
                        data.Opacity = Opacity;
                    }
                    data.hOpacityAnimations = hOpacityAnimations;
                    if (hBlurRadiusAnimations.IsNull)
                    {
                        data.BlurRadius = BlurRadius;
                    }
                    data.hBlurRadiusAnimations = hBlurRadiusAnimations;
                    data.RenderingBias = RenderingBias;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_DROPSHADOWEFFECT));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_DROPSHADOWEFFECT))
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
        ///     The DependencyProperty for the DropShadowEffect.ShadowDepth property.
        /// </summary>
        public static readonly DependencyProperty ShadowDepthProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowEffect.Color property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowEffect.Direction property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowEffect.Opacity property.
        /// </summary>
        public static readonly DependencyProperty OpacityProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowEffect.BlurRadius property.
        /// </summary>
        public static readonly DependencyProperty BlurRadiusProperty;
        /// <summary>
        ///     The DependencyProperty for the DropShadowEffect.RenderingBias property.
        /// </summary>
        public static readonly DependencyProperty RenderingBiasProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_ShadowDepth = 5.0;
        internal static Color s_Color = Colors.Black;
        internal const double c_Direction = 315.0;
        internal const double c_Opacity = 1.0;
        internal const double c_BlurRadius = 5.0;
        internal const RenderingBias c_RenderingBias = RenderingBias.Performance;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DropShadowEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(DropShadowEffect);
            ShadowDepthProperty =
                  RegisterProperty("ShadowDepth",
                                   typeof(double),
                                   typeofThis,
                                   5.0,
                                   new PropertyChangedCallback(ShadowDepthPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ColorProperty =
                  RegisterProperty("Color",
                                   typeof(Color),
                                   typeofThis,
                                   Colors.Black,
                                   new PropertyChangedCallback(ColorPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            DirectionProperty =
                  RegisterProperty("Direction",
                                   typeof(double),
                                   typeofThis,
                                   315.0,
                                   new PropertyChangedCallback(DirectionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OpacityProperty =
                  RegisterProperty("Opacity",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(OpacityPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            BlurRadiusProperty =
                  RegisterProperty("BlurRadius",
                                   typeof(double),
                                   typeofThis,
                                   5.0,
                                   new PropertyChangedCallback(BlurRadiusPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            RenderingBiasProperty =
                  RegisterProperty("RenderingBias",
                                   typeof(RenderingBias),
                                   typeofThis,
                                   RenderingBias.Performance,
                                   new PropertyChangedCallback(RenderingBiasPropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.Effects.ValidateEnums.IsRenderingBiasValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
