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
    sealed partial class EllipseGeometry : Geometry
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
        public new EllipseGeometry Clone()
        {
            return (EllipseGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new EllipseGeometry CloneCurrentValue()
        {
            return (EllipseGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RadiusXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EllipseGeometry target = ((EllipseGeometry) d);


            target.PropertyChanged(RadiusXProperty);
        }
        private static void RadiusYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EllipseGeometry target = ((EllipseGeometry) d);


            target.PropertyChanged(RadiusYProperty);
        }
        private static void CenterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EllipseGeometry target = ((EllipseGeometry) d);


            target.PropertyChanged(CenterProperty);
        }


        #region Public Properties

        /// <summary>
        ///     RadiusX - double.  Default value is 0.0.
        /// </summary>
        public double RadiusX
        {
            get
            {
                return (double) GetValue(RadiusXProperty);
            }
            set
            {
                SetValueInternal(RadiusXProperty, value);
            }
        }

        /// <summary>
        ///     RadiusY - double.  Default value is 0.0.
        /// </summary>
        public double RadiusY
        {
            get
            {
                return (double) GetValue(RadiusYProperty);
            }
            set
            {
                SetValueInternal(RadiusYProperty, value);
            }
        }

        /// <summary>
        ///     Center - Point.  Default value is new Point().
        /// </summary>
        public Point Center
        {
            get
            {
                return (Point) GetValue(CenterProperty);
            }
            set
            {
                SetValueInternal(CenterProperty, value);
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
            return new EllipseGeometry();
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
                Transform vTransform = Transform;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hTransform;
                if (vTransform == null ||
                    Object.ReferenceEquals(vTransform, Transform.Identity)
                    )
                {
                    hTransform = DUCE.ResourceHandle.Null;
                }
                else
                {
                    hTransform = ((DUCE.IResource)vTransform).GetHandle(channel);
                }

                // Obtain handles for animated properties
                DUCE.ResourceHandle hRadiusXAnimations = GetAnimationResourceHandle(RadiusXProperty, channel);
                DUCE.ResourceHandle hRadiusYAnimations = GetAnimationResourceHandle(RadiusYProperty, channel);
                DUCE.ResourceHandle hCenterAnimations = GetAnimationResourceHandle(CenterProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_ELLIPSEGEOMETRY data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdEllipseGeometry;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.hTransform = hTransform;
                    if (hRadiusXAnimations.IsNull)
                    {
                        data.RadiusX = RadiusX;
                    }
                    data.hRadiusXAnimations = hRadiusXAnimations;
                    if (hRadiusYAnimations.IsNull)
                    {
                        data.RadiusY = RadiusY;
                    }
                    data.hRadiusYAnimations = hRadiusYAnimations;
                    if (hCenterAnimations.IsNull)
                    {
                        data.Center = Center;
                    }
                    data.hCenterAnimations = hCenterAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_ELLIPSEGEOMETRY));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_ELLIPSEGEOMETRY))
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
        //    RadiusX
        //    RadiusY
        //    Center
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
        ///     The DependencyProperty for the EllipseGeometry.RadiusX property.
        /// </summary>
        public static readonly DependencyProperty RadiusXProperty;
        /// <summary>
        ///     The DependencyProperty for the EllipseGeometry.RadiusY property.
        /// </summary>
        public static readonly DependencyProperty RadiusYProperty;
        /// <summary>
        ///     The DependencyProperty for the EllipseGeometry.Center property.
        /// </summary>
        public static readonly DependencyProperty CenterProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_RadiusX = 0.0;
        internal const double c_RadiusY = 0.0;
        internal static Point s_Center = new Point();

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static EllipseGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 


            // Initializations
            Type typeofThis = typeof(EllipseGeometry);
            RadiusXProperty =
                  RegisterProperty("RadiusX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(RadiusXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            RadiusYProperty =
                  RegisterProperty("RadiusY",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(RadiusYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            CenterProperty =
                  RegisterProperty("Center",
                                   typeof(Point),
                                   typeofThis,
                                   new Point(),
                                   new PropertyChangedCallback(CenterPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
