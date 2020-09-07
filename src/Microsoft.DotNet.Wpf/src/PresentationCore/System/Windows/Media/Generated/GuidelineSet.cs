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
    sealed partial class GuidelineSet : Animatable, DUCE.IResource
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
        public new GuidelineSet Clone()
        {
            return (GuidelineSet)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GuidelineSet CloneCurrentValue()
        {
            return (GuidelineSet)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GuidelinesXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GuidelineSet target = ((GuidelineSet) d);


            target.PropertyChanged(GuidelinesXProperty);
        }
        private static void GuidelinesYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GuidelineSet target = ((GuidelineSet) d);


            target.PropertyChanged(GuidelinesYProperty);
        }
        private static void IsDynamicPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GuidelineSet target = ((GuidelineSet) d);


            target.PropertyChanged(IsDynamicProperty);
        }


        #region Public Properties

        /// <summary>
        ///     GuidelinesX - DoubleCollection.  Default value is new FreezableDefaultValueFactory(DoubleCollection.Empty).
        /// </summary>
        public DoubleCollection GuidelinesX
        {
            get
            {
                return (DoubleCollection) GetValue(GuidelinesXProperty);
            }
            set
            {
                SetValueInternal(GuidelinesXProperty, value);
            }
        }

        /// <summary>
        ///     GuidelinesY - DoubleCollection.  Default value is new FreezableDefaultValueFactory(DoubleCollection.Empty).
        /// </summary>
        public DoubleCollection GuidelinesY
        {
            get
            {
                return (DoubleCollection) GetValue(GuidelinesYProperty);
            }
            set
            {
                SetValueInternal(GuidelinesYProperty, value);
            }
        }

        /// <summary>
        ///     IsDynamic - bool.  Default value is false.
        /// </summary>
        internal bool IsDynamic
        {
            get
            {
                return (bool) GetValue(IsDynamicProperty);
            }
            set
            {
                SetValueInternal(IsDynamicProperty, BooleanBoxes.Box(value));
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
            return new GuidelineSet();
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
                DoubleCollection vGuidelinesX = GuidelinesX;
                DoubleCollection vGuidelinesY = GuidelinesY;

                // Store the count of this resource's contained collections in local variables.
                int GuidelinesXCount = (vGuidelinesX == null) ? 0 : vGuidelinesX.Count;
                int GuidelinesYCount = (vGuidelinesY == null) ? 0 : vGuidelinesY.Count;

                // Pack & send command packet
                DUCE.MILCMD_GUIDELINESET data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdGuidelineSet;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.GuidelinesXSize = (uint)(sizeof(Double) * GuidelinesXCount);
                    data.GuidelinesYSize = (uint)(sizeof(Double) * GuidelinesYCount);
                    data.IsDynamic = CompositionResourceManager.BooleanToUInt32(IsDynamic);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_GUIDELINESET),
                        (int)(data.GuidelinesXSize + 
                              data.GuidelinesYSize)
                        );


                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < GuidelinesXCount; i++)
                    {
                        Double resource = vGuidelinesX.Internal_GetItem(i);
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(Double)
                            );
                    }

                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < GuidelinesYCount; i++)
                    {
                        Double resource = vGuidelinesY.Internal_GetItem(i);
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
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_GUIDELINESET))
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





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the GuidelineSet.GuidelinesX property.
        /// </summary>
        public static readonly DependencyProperty GuidelinesXProperty;
        /// <summary>
        ///     The DependencyProperty for the GuidelineSet.GuidelinesY property.
        /// </summary>
        public static readonly DependencyProperty GuidelinesYProperty;
        /// <summary>
        ///     The DependencyProperty for the GuidelineSet.IsDynamic property.
        /// </summary>
        internal static readonly DependencyProperty IsDynamicProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static DoubleCollection s_GuidelinesX = DoubleCollection.Empty;
        internal static DoubleCollection s_GuidelinesY = DoubleCollection.Empty;
        internal const bool c_IsDynamic = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static GuidelineSet()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 

            Debug.Assert(s_GuidelinesX == null || s_GuidelinesX.IsFrozen,
                "Detected context bound default value GuidelineSet.s_GuidelinesX (See OS Bug #947272).");


            Debug.Assert(s_GuidelinesY == null || s_GuidelinesY.IsFrozen,
                "Detected context bound default value GuidelineSet.s_GuidelinesY (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(GuidelineSet);
            GuidelinesXProperty =
                  RegisterProperty("GuidelinesX",
                                   typeof(DoubleCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                   new PropertyChangedCallback(GuidelinesXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            GuidelinesYProperty =
                  RegisterProperty("GuidelinesY",
                                   typeof(DoubleCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                   new PropertyChangedCallback(GuidelinesYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            IsDynamicProperty =
                  RegisterProperty("IsDynamic",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(IsDynamicPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
