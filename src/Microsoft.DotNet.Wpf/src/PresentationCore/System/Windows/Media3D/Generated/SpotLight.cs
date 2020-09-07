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
    sealed partial class SpotLight : PointLightBase
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
        public new SpotLight Clone()
        {
            return (SpotLight)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new SpotLight CloneCurrentValue()
        {
            return (SpotLight)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void DirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpotLight target = ((SpotLight) d);


            target.PropertyChanged(DirectionProperty);
        }
        private static void OuterConeAnglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpotLight target = ((SpotLight) d);


            target.PropertyChanged(OuterConeAngleProperty);
        }
        private static void InnerConeAnglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpotLight target = ((SpotLight) d);


            target.PropertyChanged(InnerConeAngleProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Direction - Vector3D.  Default value is new Vector3D(0,0,-1).
        /// </summary>
        public Vector3D Direction
        {
            get
            {
                return (Vector3D) GetValue(DirectionProperty);
            }
            set
            {
                SetValueInternal(DirectionProperty, value);
            }
        }

        /// <summary>
        ///     OuterConeAngle - double.  Default value is 90.0.
        /// </summary>
        public double OuterConeAngle
        {
            get
            {
                return (double) GetValue(OuterConeAngleProperty);
            }
            set
            {
                SetValueInternal(OuterConeAngleProperty, value);
            }
        }

        /// <summary>
        ///     InnerConeAngle - double.  Default value is 180.0.
        /// </summary>
        public double InnerConeAngle
        {
            get
            {
                return (double) GetValue(InnerConeAngleProperty);
            }
            set
            {
                SetValueInternal(InnerConeAngleProperty, value);
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
            return new SpotLight();
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

                // Read values of properties into local variables
                Transform3D vTransform = Transform;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hTransform;
                if (vTransform == null ||
                    Object.ReferenceEquals(vTransform, Transform3D.Identity)
                    )
                {
                    hTransform = DUCE.ResourceHandle.Null;
                }
                else
                {
                    hTransform = ((DUCE.IResource)vTransform).GetHandle(channel);
                }

                // Obtain handles for animated properties
                DUCE.ResourceHandle hColorAnimations = GetAnimationResourceHandle(ColorProperty, channel);
                DUCE.ResourceHandle hPositionAnimations = GetAnimationResourceHandle(PositionProperty, channel);
                DUCE.ResourceHandle hRangeAnimations = GetAnimationResourceHandle(RangeProperty, channel);
                DUCE.ResourceHandle hConstantAttenuationAnimations = GetAnimationResourceHandle(ConstantAttenuationProperty, channel);
                DUCE.ResourceHandle hLinearAttenuationAnimations = GetAnimationResourceHandle(LinearAttenuationProperty, channel);
                DUCE.ResourceHandle hQuadraticAttenuationAnimations = GetAnimationResourceHandle(QuadraticAttenuationProperty, channel);
                DUCE.ResourceHandle hDirectionAnimations = GetAnimationResourceHandle(DirectionProperty, channel);
                DUCE.ResourceHandle hOuterConeAngleAnimations = GetAnimationResourceHandle(OuterConeAngleProperty, channel);
                DUCE.ResourceHandle hInnerConeAngleAnimations = GetAnimationResourceHandle(InnerConeAngleProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_SPOTLIGHT data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdSpotLight;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.htransform = hTransform;
                    if (hColorAnimations.IsNull)
                    {
                        data.color = CompositionResourceManager.ColorToMilColorF(Color);
                    }
                    data.hColorAnimations = hColorAnimations;
                    if (hPositionAnimations.IsNull)
                    {
                        data.position = CompositionResourceManager.Point3DToMilPoint3F(Position);
                    }
                    data.hPositionAnimations = hPositionAnimations;
                    if (hRangeAnimations.IsNull)
                    {
                        data.range = Range;
                    }
                    data.hRangeAnimations = hRangeAnimations;
                    if (hConstantAttenuationAnimations.IsNull)
                    {
                        data.constantAttenuation = ConstantAttenuation;
                    }
                    data.hConstantAttenuationAnimations = hConstantAttenuationAnimations;
                    if (hLinearAttenuationAnimations.IsNull)
                    {
                        data.linearAttenuation = LinearAttenuation;
                    }
                    data.hLinearAttenuationAnimations = hLinearAttenuationAnimations;
                    if (hQuadraticAttenuationAnimations.IsNull)
                    {
                        data.quadraticAttenuation = QuadraticAttenuation;
                    }
                    data.hQuadraticAttenuationAnimations = hQuadraticAttenuationAnimations;
                    if (hDirectionAnimations.IsNull)
                    {
                        data.direction = CompositionResourceManager.Vector3DToMilPoint3F(Direction);
                    }
                    data.hDirectionAnimations = hDirectionAnimations;
                    if (hOuterConeAngleAnimations.IsNull)
                    {
                        data.outerConeAngle = OuterConeAngle;
                    }
                    data.hOuterConeAngleAnimations = hOuterConeAngleAnimations;
                    if (hInnerConeAngleAnimations.IsNull)
                    {
                        data.innerConeAngle = InnerConeAngle;
                    }
                    data.hInnerConeAngleAnimations = hInnerConeAngleAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_SPOTLIGHT));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_SPOTLIGHT))
                {
                    Transform3D vTransform = Transform;
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
                    Transform3D vTransform = Transform;
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





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the SpotLight.Direction property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty;
        /// <summary>
        ///     The DependencyProperty for the SpotLight.OuterConeAngle property.
        /// </summary>
        public static readonly DependencyProperty OuterConeAngleProperty;
        /// <summary>
        ///     The DependencyProperty for the SpotLight.InnerConeAngle property.
        /// </summary>
        public static readonly DependencyProperty InnerConeAngleProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Vector3D s_Direction = new Vector3D(0,0,-1);
        internal const double c_OuterConeAngle = 90.0;
        internal const double c_InnerConeAngle = 180.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static SpotLight()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(SpotLight);
            DirectionProperty =
                  RegisterProperty("Direction",
                                   typeof(Vector3D),
                                   typeofThis,
                                   new Vector3D(0,0,-1),
                                   new PropertyChangedCallback(DirectionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OuterConeAngleProperty =
                  RegisterProperty("OuterConeAngle",
                                   typeof(double),
                                   typeofThis,
                                   90.0,
                                   new PropertyChangedCallback(OuterConeAnglePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            InnerConeAngleProperty =
                  RegisterProperty("InnerConeAngle",
                                   typeof(double),
                                   typeofThis,
                                   180.0,
                                   new PropertyChangedCallback(InnerConeAnglePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
