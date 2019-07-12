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
    sealed partial class AxisAngleRotation3D : Rotation3D
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
        public new AxisAngleRotation3D Clone()
        {
            return (AxisAngleRotation3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new AxisAngleRotation3D CloneCurrentValue()
        {
            return (AxisAngleRotation3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void AxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AxisAngleRotation3D target = ((AxisAngleRotation3D) d);


            target.AxisPropertyChangedHook(e);

            target.PropertyChanged(AxisProperty);
        }
        private static void AnglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AxisAngleRotation3D target = ((AxisAngleRotation3D) d);


            target.AnglePropertyChangedHook(e);

            target.PropertyChanged(AngleProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Axis - Vector3D.  Default value is new Vector3D(0,1,0).
        /// </summary>
        public Vector3D Axis
        {
            get
            {
                return (Vector3D) GetValue(AxisProperty);
            }
            set
            {
                SetValueInternal(AxisProperty, value);
            }
        }

        /// <summary>
        ///     Angle - double.  Default value is (double)0.0.
        /// </summary>
        public double Angle
        {
            get
            {
                return (double) GetValue(AngleProperty);
            }
            set
            {
                SetValueInternal(AngleProperty, value);
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
            return new AxisAngleRotation3D();
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
                DUCE.ResourceHandle hAxisAnimations = GetAnimationResourceHandle(AxisProperty, channel);
                DUCE.ResourceHandle hAngleAnimations = GetAnimationResourceHandle(AngleProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_AXISANGLEROTATION3D data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdAxisAngleRotation3D;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hAxisAnimations.IsNull)
                    {
                        data.axis = CompositionResourceManager.Vector3DToMilPoint3F(Axis);
                    }
                    data.hAxisAnimations = hAxisAnimations;
                    if (hAngleAnimations.IsNull)
                    {
                        data.angle = Angle;
                    }
                    data.hAngleAnimations = hAngleAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_AXISANGLEROTATION3D));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_AXISANGLEROTATION3D))
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
        ///     The DependencyProperty for the AxisAngleRotation3D.Axis property.
        /// </summary>
        public static readonly DependencyProperty AxisProperty;
        /// <summary>
        ///     The DependencyProperty for the AxisAngleRotation3D.Angle property.
        /// </summary>
        public static readonly DependencyProperty AngleProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Vector3D s_Axis = new Vector3D(0,1,0);
        internal const double c_Angle = (double)0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static AxisAngleRotation3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(AxisAngleRotation3D);
            AxisProperty =
                  RegisterProperty("Axis",
                                   typeof(Vector3D),
                                   typeofThis,
                                   new Vector3D(0,1,0),
                                   new PropertyChangedCallback(AxisPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            AngleProperty =
                  RegisterProperty("Angle",
                                   typeof(double),
                                   typeofThis,
                                   (double)0.0,
                                   new PropertyChangedCallback(AnglePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
