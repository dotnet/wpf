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
    sealed partial class BitmapCache : CacheMode
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
        public new BitmapCache Clone()
        {
            return (BitmapCache)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapCache CloneCurrentValue()
        {
            return (BitmapCache)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RenderAtScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCache target = ((BitmapCache) d);


            target.PropertyChanged(RenderAtScaleProperty);
        }
        private static void SnapsToDevicePixelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCache target = ((BitmapCache) d);


            target.PropertyChanged(SnapsToDevicePixelsProperty);
        }
        private static void EnableClearTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCache target = ((BitmapCache) d);


            target.PropertyChanged(EnableClearTypeProperty);
        }


        #region Public Properties

        /// <summary>
        ///     RenderAtScale - double.  Default value is 1.0.
        /// </summary>
        public double RenderAtScale
        {
            get
            {
                return (double) GetValue(RenderAtScaleProperty);
            }
            set
            {
                SetValueInternal(RenderAtScaleProperty, value);
            }
        }

        /// <summary>
        ///     SnapsToDevicePixels - bool.  Default value is false.
        /// </summary>
        public bool SnapsToDevicePixels
        {
            get
            {
                return (bool) GetValue(SnapsToDevicePixelsProperty);
            }
            set
            {
                SetValueInternal(SnapsToDevicePixelsProperty, BooleanBoxes.Box(value));
            }
        }

        /// <summary>
        ///     EnableClearType - bool.  Default value is false.
        /// </summary>
        public bool EnableClearType
        {
            get
            {
                return (bool) GetValue(EnableClearTypeProperty);
            }
            set
            {
                SetValueInternal(EnableClearTypeProperty, BooleanBoxes.Box(value));
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
            return new BitmapCache();
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
                DUCE.ResourceHandle hRenderAtScaleAnimations = GetAnimationResourceHandle(RenderAtScaleProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_BITMAPCACHE data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdBitmapCache;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hRenderAtScaleAnimations.IsNull)
                    {
                        data.RenderAtScale = RenderAtScale;
                    }
                    data.hRenderAtScaleAnimations = hRenderAtScaleAnimations;
                    data.SnapsToDevicePixels = CompositionResourceManager.BooleanToUInt32(SnapsToDevicePixels);
                    data.EnableClearType = CompositionResourceManager.BooleanToUInt32(EnableClearType);

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_BITMAPCACHE));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_BITMAPCACHE))
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
        ///     The DependencyProperty for the BitmapCache.RenderAtScale property.
        /// </summary>
        public static readonly DependencyProperty RenderAtScaleProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCache.SnapsToDevicePixels property.
        /// </summary>
        public static readonly DependencyProperty SnapsToDevicePixelsProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCache.EnableClearType property.
        /// </summary>
        public static readonly DependencyProperty EnableClearTypeProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_RenderAtScale = 1.0;
        internal const bool c_SnapsToDevicePixels = false;
        internal const bool c_EnableClearType = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BitmapCache()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 



            // Initializations
            Type typeofThis = typeof(BitmapCache);
            RenderAtScaleProperty =
                  RegisterProperty("RenderAtScale",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(RenderAtScalePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            SnapsToDevicePixelsProperty =
                  RegisterProperty("SnapsToDevicePixels",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(SnapsToDevicePixelsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            EnableClearTypeProperty =
                  RegisterProperty("EnableClearType",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(EnableClearTypePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
