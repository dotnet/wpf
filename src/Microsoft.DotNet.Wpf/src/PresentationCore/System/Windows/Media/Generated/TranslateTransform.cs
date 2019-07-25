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
    sealed partial class TranslateTransform : Transform
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
        public new TranslateTransform Clone()
        {
            return (TranslateTransform)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TranslateTransform CloneCurrentValue()
        {
            return (TranslateTransform)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void XPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform target = ((TranslateTransform) d);


            target.PropertyChanged(XProperty);
        }
        private static void YPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform target = ((TranslateTransform) d);


            target.PropertyChanged(YProperty);
        }


        #region Public Properties

        /// <summary>
        ///     X - double.  Default value is 0.0.
        /// </summary>
        public double X
        {
            get
            {
                return (double) GetValue(XProperty);
            }
            set
            {
                SetValueInternal(XProperty, value);
            }
        }

        /// <summary>
        ///     Y - double.  Default value is 0.0.
        /// </summary>
        public double Y
        {
            get
            {
                return (double) GetValue(YProperty);
            }
            set
            {
                SetValueInternal(YProperty, value);
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
            return new TranslateTransform();
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
                DUCE.ResourceHandle hXAnimations = GetAnimationResourceHandle(XProperty, channel);
                DUCE.ResourceHandle hYAnimations = GetAnimationResourceHandle(YProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_TRANSLATETRANSFORM data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdTranslateTransform;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hXAnimations.IsNull)
                    {
                        data.X = X;
                    }
                    data.hXAnimations = hXAnimations;
                    if (hYAnimations.IsNull)
                    {
                        data.Y = Y;
                    }
                    data.hYAnimations = hYAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_TRANSLATETRANSFORM));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_TRANSLATETRANSFORM))
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
        ///     The DependencyProperty for the TranslateTransform.X property.
        /// </summary>
        public static readonly DependencyProperty XProperty;
        /// <summary>
        ///     The DependencyProperty for the TranslateTransform.Y property.
        /// </summary>
        public static readonly DependencyProperty YProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_X = 0.0;
        internal const double c_Y = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TranslateTransform()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app



            // Initializations
            Type typeofThis = typeof(TranslateTransform);
            XProperty =
                  RegisterProperty("X",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(XPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            YProperty =
                  RegisterProperty("Y",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(YPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
