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
    sealed partial class ImageDrawing : Drawing
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
        public new ImageDrawing Clone()
        {
            return (ImageDrawing)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ImageDrawing CloneCurrentValue()
        {
            return (ImageDrawing)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ImageSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }


            ImageDrawing target = ((ImageDrawing) d);


            ImageSource oldV = (ImageSource) e.OldValue;
            ImageSource newV = (ImageSource) e.NewValue;
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

            target.PropertyChanged(ImageSourceProperty);
        }
        private static void RectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ImageDrawing target = ((ImageDrawing) d);


            target.PropertyChanged(RectProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ImageSource - ImageSource.  Default value is null.
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return (ImageSource) GetValue(ImageSourceProperty);
            }
            set
            {
                SetValueInternal(ImageSourceProperty, value);
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
            return new ImageDrawing();
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
                ImageSource vImageSource = ImageSource;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hImageSource = vImageSource != null ? ((DUCE.IResource)vImageSource).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hRectAnimations = GetAnimationResourceHandle(RectProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_IMAGEDRAWING data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdImageDrawing;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.hImageSource = hImageSource;
                    if (hRectAnimations.IsNull)
                    {
                        data.Rect = Rect;
                    }
                    data.hRectAnimations = hRectAnimations;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_IMAGEDRAWING));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_IMAGEDRAWING))
                {
                    ImageSource vImageSource = ImageSource;
                    if (vImageSource != null) ((DUCE.IResource)vImageSource).AddRefOnChannel(channel);

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
                    ImageSource vImageSource = ImageSource;
                    if (vImageSource != null) ((DUCE.IResource)vImageSource).ReleaseOnChannel(channel);

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
        ///     The DependencyProperty for the ImageDrawing.ImageSource property.
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty;
        /// <summary>
        ///     The DependencyProperty for the ImageDrawing.Rect property.
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

        static ImageDrawing()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 



            // Initializations
            Type typeofThis = typeof(ImageDrawing);
            ImageSourceProperty =
                  RegisterProperty("ImageSource",
                                   typeof(ImageSource),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(ImageSourcePropertyChanged),
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
