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
    sealed partial class ScaleTransform3D : AffineTransform3D
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
        public new ScaleTransform3D Clone()
        {
            return (ScaleTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ScaleTransform3D CloneCurrentValue()
        {
            return (ScaleTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ScaleXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedScaleXValue = (double)e.NewValue;


            target.PropertyChanged(ScaleXProperty);
        }
        private static void ScaleYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedScaleYValue = (double)e.NewValue;


            target.PropertyChanged(ScaleYProperty);
        }
        private static void ScaleZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedScaleZValue = (double)e.NewValue;


            target.PropertyChanged(ScaleZProperty);
        }
        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedCenterXValue = (double)e.NewValue;


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedCenterYValue = (double)e.NewValue;


            target.PropertyChanged(CenterYProperty);
        }
        private static void CenterZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedCenterZValue = (double)e.NewValue;


            target.PropertyChanged(CenterZProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ScaleX - double.  Default value is 1.0.
        /// </summary>
        public double ScaleX
        {
            get
            {
                ReadPreamble();
                return _cachedScaleXValue;
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
                ReadPreamble();
                return _cachedScaleYValue;
            }
            set
            {
                SetValueInternal(ScaleYProperty, value);
            }
        }

        /// <summary>
        ///     ScaleZ - double.  Default value is 1.0.
        /// </summary>
        public double ScaleZ
        {
            get
            {
                ReadPreamble();
                return _cachedScaleZValue;
            }
            set
            {
                SetValueInternal(ScaleZProperty, value);
            }
        }

        /// <summary>
        ///     CenterX - double.  Default value is 0.0.
        /// </summary>
        public double CenterX
        {
            get
            {
                ReadPreamble();
                return _cachedCenterXValue;
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
                ReadPreamble();
                return _cachedCenterYValue;
            }
            set
            {
                SetValueInternal(CenterYProperty, value);
            }
        }

        /// <summary>
        ///     CenterZ - double.  Default value is 0.0.
        /// </summary>
        public double CenterZ
        {
            get
            {
                ReadPreamble();
                return _cachedCenterZValue;
            }
            set
            {
                SetValueInternal(CenterZProperty, value);
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
            return new ScaleTransform3D();
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
                DUCE.ResourceHandle hScaleZAnimations = GetAnimationResourceHandle(ScaleZProperty, channel);
                DUCE.ResourceHandle hCenterXAnimations = GetAnimationResourceHandle(CenterXProperty, channel);
                DUCE.ResourceHandle hCenterYAnimations = GetAnimationResourceHandle(CenterYProperty, channel);
                DUCE.ResourceHandle hCenterZAnimations = GetAnimationResourceHandle(CenterZProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_SCALETRANSFORM3D data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdScaleTransform3D;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hScaleXAnimations.IsNull)
                    {
                        data.scaleX = ScaleX;
                    }
                    data.hScaleXAnimations = hScaleXAnimations;
                    if (hScaleYAnimations.IsNull)
                    {
                        data.scaleY = ScaleY;
                    }
                    data.hScaleYAnimations = hScaleYAnimations;
                    if (hScaleZAnimations.IsNull)
                    {
                        data.scaleZ = ScaleZ;
                    }
                    data.hScaleZAnimations = hScaleZAnimations;
                    if (hCenterXAnimations.IsNull)
                    {
                        data.centerX = CenterX;
                    }
                    data.hCenterXAnimations = hCenterXAnimations;
                    if (hCenterYAnimations.IsNull)
                    {
                        data.centerY = CenterY;
                    }
                    data.hCenterYAnimations = hCenterYAnimations;
                    if (hCenterZAnimations.IsNull)
                    {
                        data.centerZ = CenterZ;
                    }
                    data.hCenterZAnimations = hCenterZAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_SCALETRANSFORM3D));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_SCALETRANSFORM3D))
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
        ///     The DependencyProperty for the ScaleTransform3D.ScaleX property.
        /// </summary>
        public static readonly DependencyProperty ScaleXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.ScaleY property.
        /// </summary>
        public static readonly DependencyProperty ScaleYProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.ScaleZ property.
        /// </summary>
        public static readonly DependencyProperty ScaleZProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.CenterZ property.
        /// </summary>
        public static readonly DependencyProperty CenterZProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private double _cachedScaleXValue = 1.0;
        private double _cachedScaleYValue = 1.0;
        private double _cachedScaleZValue = 1.0;
        private double _cachedCenterXValue = 0.0;
        private double _cachedCenterYValue = 0.0;
        private double _cachedCenterZValue = 0.0;

        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_ScaleX = 1.0;
        internal const double c_ScaleY = 1.0;
        internal const double c_ScaleZ = 1.0;
        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;
        internal const double c_CenterZ = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ScaleTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(ScaleTransform3D);
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
            ScaleZProperty =
                  RegisterProperty("ScaleZ",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ScaleZPropertyChanged),
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
            CenterZProperty =
                  RegisterProperty("CenterZ",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(CenterZPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
