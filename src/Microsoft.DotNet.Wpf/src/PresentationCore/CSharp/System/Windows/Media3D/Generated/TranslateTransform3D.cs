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
    sealed partial class TranslateTransform3D : AffineTransform3D
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
        public new TranslateTransform3D Clone()
        {
            return (TranslateTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TranslateTransform3D CloneCurrentValue()
        {
            return (TranslateTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void OffsetXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform3D target = ((TranslateTransform3D) d);

            target._cachedOffsetXValue = (double)e.NewValue;


            target.PropertyChanged(OffsetXProperty);
        }
        private static void OffsetYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform3D target = ((TranslateTransform3D) d);

            target._cachedOffsetYValue = (double)e.NewValue;


            target.PropertyChanged(OffsetYProperty);
        }
        private static void OffsetZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform3D target = ((TranslateTransform3D) d);

            target._cachedOffsetZValue = (double)e.NewValue;


            target.PropertyChanged(OffsetZProperty);
        }


        #region Public Properties

        /// <summary>
        ///     OffsetX - double.  Default value is 0.0.
        /// </summary>
        public double OffsetX
        {
            get
            {
                ReadPreamble();
                return _cachedOffsetXValue;
            }
            set
            {
                SetValueInternal(OffsetXProperty, value);
            }
        }

        /// <summary>
        ///     OffsetY - double.  Default value is 0.0.
        /// </summary>
        public double OffsetY
        {
            get
            {
                ReadPreamble();
                return _cachedOffsetYValue;
            }
            set
            {
                SetValueInternal(OffsetYProperty, value);
            }
        }

        /// <summary>
        ///     OffsetZ - double.  Default value is 0.0.
        /// </summary>
        public double OffsetZ
        {
            get
            {
                ReadPreamble();
                return _cachedOffsetZValue;
            }
            set
            {
                SetValueInternal(OffsetZProperty, value);
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
            return new TranslateTransform3D();
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
                DUCE.ResourceHandle hOffsetXAnimations = GetAnimationResourceHandle(OffsetXProperty, channel);
                DUCE.ResourceHandle hOffsetYAnimations = GetAnimationResourceHandle(OffsetYProperty, channel);
                DUCE.ResourceHandle hOffsetZAnimations = GetAnimationResourceHandle(OffsetZProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_TRANSLATETRANSFORM3D data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdTranslateTransform3D;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hOffsetXAnimations.IsNull)
                    {
                        data.offsetX = OffsetX;
                    }
                    data.hOffsetXAnimations = hOffsetXAnimations;
                    if (hOffsetYAnimations.IsNull)
                    {
                        data.offsetY = OffsetY;
                    }
                    data.hOffsetYAnimations = hOffsetYAnimations;
                    if (hOffsetZAnimations.IsNull)
                    {
                        data.offsetZ = OffsetZ;
                    }
                    data.hOffsetZAnimations = hOffsetZAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_TRANSLATETRANSFORM3D));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_TRANSLATETRANSFORM3D))
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
        ///     The DependencyProperty for the TranslateTransform3D.OffsetX property.
        /// </summary>
        public static readonly DependencyProperty OffsetXProperty;
        /// <summary>
        ///     The DependencyProperty for the TranslateTransform3D.OffsetY property.
        /// </summary>
        public static readonly DependencyProperty OffsetYProperty;
        /// <summary>
        ///     The DependencyProperty for the TranslateTransform3D.OffsetZ property.
        /// </summary>
        public static readonly DependencyProperty OffsetZProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private double _cachedOffsetXValue = 0.0;
        private double _cachedOffsetYValue = 0.0;
        private double _cachedOffsetZValue = 0.0;

        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_OffsetX = 0.0;
        internal const double c_OffsetY = 0.0;
        internal const double c_OffsetZ = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TranslateTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(TranslateTransform3D);
            OffsetXProperty =
                  RegisterProperty("OffsetX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OffsetYProperty =
                  RegisterProperty("OffsetY",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OffsetZProperty =
                  RegisterProperty("OffsetZ",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetZPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
