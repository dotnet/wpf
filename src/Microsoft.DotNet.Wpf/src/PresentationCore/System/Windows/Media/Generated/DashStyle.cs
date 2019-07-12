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
    sealed partial class DashStyle : Animatable, DUCE.IResource
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
        public new DashStyle Clone()
        {
            return (DashStyle)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DashStyle CloneCurrentValue()
        {
            return (DashStyle)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void OffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DashStyle target = ((DashStyle) d);


            target.PropertyChanged(OffsetProperty);
        }
        private static void DashesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DashStyle target = ((DashStyle) d);


            target.PropertyChanged(DashesProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Offset - double.  Default value is 0.0.
        /// </summary>
        public double Offset
        {
            get
            {
                return (double) GetValue(OffsetProperty);
            }
            set
            {
                SetValueInternal(OffsetProperty, value);
            }
        }

        /// <summary>
        ///     Dashes - DoubleCollection.  Default value is new FreezableDefaultValueFactory(DoubleCollection.Empty).
        /// </summary>
        public DoubleCollection Dashes
        {
            get
            {
                return (DoubleCollection) GetValue(DashesProperty);
            }
            set
            {
                SetValueInternal(DashesProperty, value);
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
            return new DashStyle();
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
                DoubleCollection vDashes = Dashes;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hOffsetAnimations = GetAnimationResourceHandle(OffsetProperty, channel);

                // Store the count of this resource's contained collections in local variables.
                int DashesCount = (vDashes == null) ? 0 : vDashes.Count;

                // Pack & send command packet
                DUCE.MILCMD_DASHSTYLE data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdDashStyle;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hOffsetAnimations.IsNull)
                    {
                        data.Offset = Offset;
                    }
                    data.hOffsetAnimations = hOffsetAnimations;
                    data.DashesSize = (uint)(sizeof(Double) * DashesCount);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_DASHSTYLE),
                        (int)(data.DashesSize)
                        );


                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < DashesCount; i++)
                    {
                        Double resource = vDashes.Internal_GetItem(i);
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(Double)
                            );
                    }

                    channel.EndCommand();
                }
            }
        }
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire()) 
            {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_DASHSTYLE))
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
        //    Dashes
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
        ///     The DependencyProperty for the DashStyle.Offset property.
        /// </summary>
        public static readonly DependencyProperty OffsetProperty;
        /// <summary>
        ///     The DependencyProperty for the DashStyle.Dashes property.
        /// </summary>
        public static readonly DependencyProperty DashesProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const double c_Offset = 0.0;
        internal static DoubleCollection s_Dashes = DoubleCollection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DashStyle()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app

            Debug.Assert(s_Dashes == null || s_Dashes.IsFrozen,
                "Detected context bound default value DashStyle.s_Dashes (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(DashStyle);
            OffsetProperty =
                  RegisterProperty("Offset",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            DashesProperty =
                  RegisterProperty("Dashes",
                                   typeof(DoubleCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                   new PropertyChangedCallback(DashesPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
