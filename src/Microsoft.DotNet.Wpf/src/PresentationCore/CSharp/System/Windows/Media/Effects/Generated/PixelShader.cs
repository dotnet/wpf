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
    sealed partial class PixelShader : Animatable, DUCE.IResource
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
        public new PixelShader Clone()
        {
            return (PixelShader)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new PixelShader CloneCurrentValue()
        {
            return (PixelShader)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void UriSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PixelShader target = ((PixelShader) d);


            target.UriSourcePropertyChangedHook(e);

            target.PropertyChanged(UriSourceProperty);
        }
        private static void ShaderRenderModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PixelShader target = ((PixelShader) d);


            target.PropertyChanged(ShaderRenderModeProperty);
        }


        #region Public Properties

        /// <summary>
        ///     UriSource - Uri.  Default value is null.
        /// </summary>
        public Uri UriSource
        {
            get
            {
                return (Uri) GetValue(UriSourceProperty);
            }
            set
            {
                SetValueInternal(UriSourceProperty, value);
            }
        }

        /// <summary>
        ///     ShaderRenderMode - ShaderRenderMode.  Default value is ShaderRenderMode.Auto.
        /// </summary>
        public ShaderRenderMode ShaderRenderMode
        {
            get
            {
                return (ShaderRenderMode) GetValue(ShaderRenderModeProperty);
            }
            set
            {
                SetValueInternal(ShaderRenderModeProperty, value);
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
            return new PixelShader();
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
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire()) 
            {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_PIXELSHADER))
                {
                    AddRefOnChannelAnimations(channel);


                    UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
                }

                return _duceResource.GetHandle(channel);
            }
        }
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire()) 
            {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                if (_duceResource.ReleaseOnChannel(channel))
                {
                    ReleaseOnChannelAnimations(channel);
}
            }
        }
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            DUCE.ResourceHandle h;
            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                h = _duceResource.GetHandle(channel);
            }
            return h;
        }
        int DUCE.IResource.GetChannelCount()
        {
            // must already be in composition lock here
            return _duceResource.GetChannelCount();
        }
        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            // in a lock already
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
        //    UriSource
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
        ///     The DependencyProperty for the PixelShader.UriSource property.
        /// </summary>
        public static readonly DependencyProperty UriSourceProperty;
        /// <summary>
        ///     The DependencyProperty for the PixelShader.ShaderRenderMode property.
        /// </summary>
        public static readonly DependencyProperty ShaderRenderModeProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Uri s_UriSource = null;
        internal const ShaderRenderMode c_ShaderRenderMode = ShaderRenderMode.Auto;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static PixelShader()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app. 



            // Initializations
            Type typeofThis = typeof(PixelShader);
            UriSourceProperty =
                  RegisterProperty("UriSource",
                                   typeof(Uri),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(UriSourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ShaderRenderModeProperty =
                  RegisterProperty("ShaderRenderMode",
                                   typeof(ShaderRenderMode),
                                   typeofThis,
                                   ShaderRenderMode.Auto,
                                   new PropertyChangedCallback(ShaderRenderModePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.Effects.ValidateEnums.IsShaderRenderModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
