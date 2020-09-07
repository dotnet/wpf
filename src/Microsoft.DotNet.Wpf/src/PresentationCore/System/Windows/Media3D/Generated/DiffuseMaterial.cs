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
    sealed partial class DiffuseMaterial : Material
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
        public new DiffuseMaterial Clone()
        {
            return (DiffuseMaterial)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DiffuseMaterial CloneCurrentValue()
        {
            return (DiffuseMaterial)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DiffuseMaterial target = ((DiffuseMaterial) d);


            target.PropertyChanged(ColorProperty);
        }
        private static void AmbientColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DiffuseMaterial target = ((DiffuseMaterial) d);


            target.PropertyChanged(AmbientColorProperty);
        }
        private static void BrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            DiffuseMaterial target = ((DiffuseMaterial) d);


            Brush oldV = (Brush) e.OldValue;
            Brush newV = (Brush) e.NewValue;
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

            target.PropertyChanged(BrushProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Color - Color.  Default value is Colors.White.
        /// </summary>
        public Color Color
        {
            get
            {
                return (Color) GetValue(ColorProperty);
            }
            set
            {
                SetValueInternal(ColorProperty, value);
            }
        }

        /// <summary>
        ///     AmbientColor - Color.  Default value is Colors.White.
        /// </summary>
        public Color AmbientColor
        {
            get
            {
                return (Color) GetValue(AmbientColorProperty);
            }
            set
            {
                SetValueInternal(AmbientColorProperty, value);
            }
        }

        /// <summary>
        ///     Brush - Brush.  Default value is null.
        /// </summary>
        public Brush Brush
        {
            get
            {
                return (Brush) GetValue(BrushProperty);
            }
            set
            {
                SetValueInternal(BrushProperty, value);
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
            return new DiffuseMaterial();
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
                Brush vBrush = Brush;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hBrush = vBrush != null ? ((DUCE.IResource)vBrush).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Pack & send command packet
                DUCE.MILCMD_DIFFUSEMATERIAL data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdDiffuseMaterial;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.color = CompositionResourceManager.ColorToMilColorF(Color);
                    data.ambientColor = CompositionResourceManager.ColorToMilColorF(AmbientColor);
                    data.hbrush = hBrush;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_DIFFUSEMATERIAL));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_DIFFUSEMATERIAL))
                {
                    Brush vBrush = Brush;
                    if (vBrush != null) ((DUCE.IResource)vBrush).AddRefOnChannel(channel);

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
                    Brush vBrush = Brush;
                    if (vBrush != null) ((DUCE.IResource)vBrush).ReleaseOnChannel(channel);

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
        //    Brush
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 1;
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
        ///     The DependencyProperty for the DiffuseMaterial.Color property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty;
        /// <summary>
        ///     The DependencyProperty for the DiffuseMaterial.AmbientColor property.
        /// </summary>
        public static readonly DependencyProperty AmbientColorProperty;
        /// <summary>
        ///     The DependencyProperty for the DiffuseMaterial.Brush property.
        /// </summary>
        public static readonly DependencyProperty BrushProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Color s_Color = Colors.White;
        internal static Color s_AmbientColor = Colors.White;
        internal static Brush s_Brush = null;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DiffuseMaterial()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.

            Debug.Assert(s_Brush == null || s_Brush.IsFrozen,
                "Detected context bound default value DiffuseMaterial.s_Brush (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(DiffuseMaterial);
            ColorProperty =
                  RegisterProperty("Color",
                                   typeof(Color),
                                   typeofThis,
                                   Colors.White,
                                   new PropertyChangedCallback(ColorPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AmbientColorProperty =
                  RegisterProperty("AmbientColor",
                                   typeof(Color),
                                   typeofThis,
                                   Colors.White,
                                   new PropertyChangedCallback(AmbientColorPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BrushProperty =
                  RegisterProperty("Brush",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BrushPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
