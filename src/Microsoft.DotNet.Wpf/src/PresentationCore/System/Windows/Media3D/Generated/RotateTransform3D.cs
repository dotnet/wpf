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
    sealed partial class RotateTransform3D : AffineTransform3D
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
        public new RotateTransform3D Clone()
        {
            return (RotateTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new RotateTransform3D CloneCurrentValue()
        {
            return (RotateTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RotateTransform3D target = ((RotateTransform3D) d);

            target._cachedCenterXValue = (double)e.NewValue;


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RotateTransform3D target = ((RotateTransform3D) d);

            target._cachedCenterYValue = (double)e.NewValue;


            target.PropertyChanged(CenterYProperty);
        }
        private static void CenterZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RotateTransform3D target = ((RotateTransform3D) d);

            target._cachedCenterZValue = (double)e.NewValue;


            target.PropertyChanged(CenterZProperty);
        }
        private static void RotationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            RotateTransform3D target = ((RotateTransform3D) d);


            target._cachedRotationValue = (Rotation3D)e.NewValue;

            Rotation3D oldV = (Rotation3D) e.OldValue;
            Rotation3D newV = (Rotation3D) e.NewValue;
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

            target.PropertyChanged(RotationProperty);
        }


        #region Public Properties

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

        /// <summary>
        ///     Rotation - Rotation3D.  Default value is Rotation3D.Identity.
        /// </summary>
        public Rotation3D Rotation
        {
            get
            {
                ReadPreamble();
                return _cachedRotationValue;
            }
            set
            {
                SetValueInternal(RotationProperty, value);
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
            return new RotateTransform3D();
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
                Rotation3D vRotation = Rotation;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hRotation = vRotation != null ? ((DUCE.IResource)vRotation).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hCenterXAnimations = GetAnimationResourceHandle(CenterXProperty, channel);
                DUCE.ResourceHandle hCenterYAnimations = GetAnimationResourceHandle(CenterYProperty, channel);
                DUCE.ResourceHandle hCenterZAnimations = GetAnimationResourceHandle(CenterZProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_ROTATETRANSFORM3D data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdRotateTransform3D;
                    data.Handle = _duceResource.GetHandle(channel);
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
                    data.hrotation = hRotation;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_ROTATETRANSFORM3D));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_ROTATETRANSFORM3D))
                {
                    Rotation3D vRotation = Rotation;
                    if (vRotation != null) ((DUCE.IResource)vRotation).AddRefOnChannel(channel);

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
                    Rotation3D vRotation = Rotation;
                    if (vRotation != null) ((DUCE.IResource)vRotation).ReleaseOnChannel(channel);

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
        ///     The DependencyProperty for the RotateTransform3D.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the RotateTransform3D.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;
        /// <summary>
        ///     The DependencyProperty for the RotateTransform3D.CenterZ property.
        /// </summary>
        public static readonly DependencyProperty CenterZProperty;
        /// <summary>
        ///     The DependencyProperty for the RotateTransform3D.Rotation property.
        /// </summary>
        public static readonly DependencyProperty RotationProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private double _cachedCenterXValue = 0.0;
        private double _cachedCenterYValue = 0.0;
        private double _cachedCenterZValue = 0.0;
        private Rotation3D _cachedRotationValue = Rotation3D.Identity;

        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;
        internal const double c_CenterZ = 0.0;
        internal static Rotation3D s_Rotation = Rotation3D.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static RotateTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.

            Debug.Assert(s_Rotation == null || s_Rotation.IsFrozen,
                "Detected context bound default value RotateTransform3D.s_Rotation (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(RotateTransform3D);
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
            RotationProperty =
                  RegisterProperty("Rotation",
                                   typeof(Rotation3D),
                                   typeofThis,
                                   Rotation3D.Identity,
                                   new PropertyChangedCallback(RotationPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
