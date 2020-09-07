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
    sealed partial class VideoDrawing : Drawing
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
        public new VideoDrawing Clone()
        {
            return (VideoDrawing)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new VideoDrawing CloneCurrentValue()
        {
            return (VideoDrawing)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void PlayerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VideoDrawing target = ((VideoDrawing) d);


            MediaPlayer oldV = (MediaPlayer) e.OldValue;
            MediaPlayer newV = (MediaPlayer) e.NewValue;
            System.Windows.Threading.Dispatcher dispatcher = target.Dispatcher;

            if (dispatcher != null)
            {
                DUCE.IResource targetResource = (DUCE.IResource)target;
                using (CompositionEngineLock.Acquire())
                {
                    int channelCount = targetResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = targetResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!targetResource.GetHandle(channel).IsNull);
                        target.ReleaseResource(oldV,channel);
                        target.AddRefResource(newV,channel);
                    }
                }
            }

            target.PropertyChanged(PlayerProperty);
        }
        private static void RectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VideoDrawing target = ((VideoDrawing) d);


            target.PropertyChanged(RectProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Player - MediaPlayer.  Default value is null.
        /// </summary>
        public MediaPlayer Player
        {
            get
            {
                return (MediaPlayer) GetValue(PlayerProperty);
            }
            set
            {
                SetValueInternal(PlayerProperty, value);
            }
        }

        /// <summary>
        ///     Rect - Rect.  Default value is Rect.Empty.
        /// </summary>
        public Rect Rect
        {
            get
            {
                return (Rect) GetValue(RectProperty);
            }
            set
            {
                SetValueInternal(RectProperty, value);
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
            return new VideoDrawing();
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
                MediaPlayer vPlayer = Player;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hPlayer = vPlayer != null ? ((DUCE.IResource)vPlayer).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hRectAnimations = GetAnimationResourceHandle(RectProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_VIDEODRAWING data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdVideoDrawing;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.hPlayer = hPlayer;
                    if (hRectAnimations.IsNull)
                    {
                        data.Rect = Rect;
                    }
                    data.hRectAnimations = hRectAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_VIDEODRAWING));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_VIDEODRAWING))
                {
                    MediaPlayer vPlayer = Player;
                    if (vPlayer != null) ((DUCE.IResource)vPlayer).AddRefOnChannel(channel);

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
                    MediaPlayer vPlayer = Player;
                    if (vPlayer != null) ((DUCE.IResource)vPlayer).ReleaseOnChannel(channel);

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
        ///     The DependencyProperty for the VideoDrawing.Player property.
        /// </summary>
        public static readonly DependencyProperty PlayerProperty;
        /// <summary>
        ///     The DependencyProperty for the VideoDrawing.Rect property.
        /// </summary>
        public static readonly DependencyProperty RectProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Rect s_Rect = Rect.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static VideoDrawing()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  



            // Initializations
            Type typeofThis = typeof(VideoDrawing);
            PlayerProperty =
                  RegisterProperty("Player",
                                   typeof(MediaPlayer),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(PlayerPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            RectProperty =
                  RegisterProperty("Rect",
                                   typeof(Rect),
                                   typeofThis,
                                   Rect.Empty,
                                   new PropertyChangedCallback(RectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
