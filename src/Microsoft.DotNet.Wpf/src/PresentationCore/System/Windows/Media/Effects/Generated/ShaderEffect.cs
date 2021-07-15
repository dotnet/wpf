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
using MS.Internal.KnownBoxes;
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
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows.Media.Effects
{
    partial class ShaderEffect : Effect
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
        public new ShaderEffect Clone()
        {
            return (ShaderEffect)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ShaderEffect CloneCurrentValue()
        {
            return (ShaderEffect)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void PixelShaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShaderEffect target = ((ShaderEffect) d);


            target.PixelShaderPropertyChangedHook(e);



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



            PixelShader oldV = (PixelShader) e.OldValue;
            PixelShader newV = (PixelShader) e.NewValue;
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

            target.PropertyChanged(PixelShaderProperty);
        }


        #region Public Properties

        /// <summary>
        ///     PixelShader - PixelShader.  Default value is null.
        /// </summary>
        protected PixelShader PixelShader
        {
            get
            {
                return (PixelShader) GetValue(PixelShaderProperty);
            }
            set
            {
                SetValueInternal(PixelShaderProperty, value);
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
            // ShaderEffect is an abstract type, so
            // explicitly create the invoking type via
            // Activator. 
            return (Freezable)Activator.CreateInstance(this.GetType());
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
            ManualUpdateResource(channel, skipOnChannelCheck);
            base.UpdateResource(channel, skipOnChannelCheck);
        }
        private DUCE.ResourceHandle GeneratedAddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_SHADEREFFECT))
                {
                    PixelShader vPixelShader = PixelShader;
                    if (vPixelShader != null) ((DUCE.IResource)vPixelShader).AddRefOnChannel(channel);

                    AddRefOnChannelAnimations(channel);


                    UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
                }

                return _duceResource.GetHandle(channel);
}
        private void GeneratedReleaseOnChannelCore(DUCE.Channel channel)
        {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                if (_duceResource.ReleaseOnChannel(channel))
                {
                    PixelShader vPixelShader = PixelShader;
                    if (vPixelShader != null) ((DUCE.IResource)vPixelShader).ReleaseOnChannel(channel);

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
        ///     The DependencyProperty for the ShaderEffect.PixelShader property.
        /// </summary>
        protected static readonly DependencyProperty PixelShaderProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();



        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ShaderEffect()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app



            // Initializations
            Type typeofThis = typeof(ShaderEffect);
            PixelShaderProperty =
                  RegisterProperty("PixelShader",
                                   typeof(PixelShader),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(PixelShaderPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
