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
    sealed partial class VisualBrush : TileBrush
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
        public new VisualBrush Clone()
        {
            return (VisualBrush)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new VisualBrush CloneCurrentValue()
        {
            return (VisualBrush)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void VisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VisualBrush target = ((VisualBrush) d);


            Visual oldV = (Visual) e.OldValue;

            //
            // If the Visual required layout but it is changed before we do Layout
            // on that Visual, then we dont want the async LayoutCallback method to run,
            // nor do we want the LayoutUpdated handler to run. So we abort/remove them.
            //
            if (target._pendingLayout)
            {
                //
                // Visual has to be a UIElement since _pendingLayout flag is
                // true only if we added the LayoutUpdated handler which can
                // only be done on UIElement.
                //
                UIElement element = (UIElement)oldV;
                Debug.Assert(element != null);
                element.LayoutUpdated -= target.OnLayoutUpdated;

                Debug.Assert(target._DispatcherLayoutResult != null);
                Debug.Assert(target._DispatcherLayoutResult.Status == System.Windows.Threading.DispatcherOperationStatus.Pending);
                bool abortStatus = target._DispatcherLayoutResult.Abort();
                Debug.Assert(abortStatus);

                target._pendingLayout = false;
            }

            Visual newV = (Visual) e.NewValue;
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

            target.PropertyChanged(VisualProperty);
        }
        private static void AutoLayoutContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VisualBrush target = ((VisualBrush) d);


            target.PropertyChanged(AutoLayoutContentProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Visual - Visual.  Default value is null.
        /// </summary>
        public Visual Visual
        {
            get
            {
                return (Visual) GetValue(VisualProperty);
            }
            set
            {
                SetValueInternal(VisualProperty, value);
            }
        }

        /// <summary>
        ///     AutoLayoutContent - bool.  Default value is true.
        ///     If this property is true, this Brush will run layout on the contents of this Brush 
        ///     if the Visual is a non-parented UIElement.
        /// </summary>
        public bool AutoLayoutContent
        {
            get
            {
                return (bool) GetValue(AutoLayoutContentProperty);
            }
            set
            {
                SetValueInternal(AutoLayoutContentProperty, BooleanBoxes.Box(value));
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
            return new VisualBrush();
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
                Transform vTransform = Transform;
                Transform vRelativeTransform = RelativeTransform;
                Visual vVisual = Visual;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hTransform;
                if (vTransform == null ||
                    Object.ReferenceEquals(vTransform, Transform.Identity)
                    )
                {
                    hTransform = DUCE.ResourceHandle.Null;
                }
                else
                {
                    hTransform = ((DUCE.IResource)vTransform).GetHandle(channel);
                }
                DUCE.ResourceHandle hRelativeTransform;
                if (vRelativeTransform == null ||
                    Object.ReferenceEquals(vRelativeTransform, Transform.Identity)
                    )
                {
                    hRelativeTransform = DUCE.ResourceHandle.Null;
                }
                else
                {
                    hRelativeTransform = ((DUCE.IResource)vRelativeTransform).GetHandle(channel);
                }

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle  hVisual = vVisual != null ? ((DUCE.IResource)vVisual).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hOpacityAnimations = GetAnimationResourceHandle(OpacityProperty, channel);
                DUCE.ResourceHandle hViewportAnimations = GetAnimationResourceHandle(ViewportProperty, channel);
                DUCE.ResourceHandle hViewboxAnimations = GetAnimationResourceHandle(ViewboxProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_VISUALBRUSH data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdVisualBrush;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hOpacityAnimations.IsNull)
                    {
                        data.Opacity = Opacity;
                    }
                    data.hOpacityAnimations = hOpacityAnimations;
                    data.hTransform = hTransform;
                    data.hRelativeTransform = hRelativeTransform;
                    data.ViewportUnits = ViewportUnits;
                    data.ViewboxUnits = ViewboxUnits;
                    if (hViewportAnimations.IsNull)
                    {
                        data.Viewport = Viewport;
                    }
                    data.hViewportAnimations = hViewportAnimations;
                    if (hViewboxAnimations.IsNull)
                    {
                        data.Viewbox = Viewbox;
                    }
                    data.hViewboxAnimations = hViewboxAnimations;
                    data.Stretch = Stretch;
                    data.TileMode = TileMode;
                    data.AlignmentX = AlignmentX;
                    data.AlignmentY = AlignmentY;
                    data.CachingHint = (CachingHint)GetValue(RenderOptions.CachingHintProperty);
                    data.CacheInvalidationThresholdMinimum = (double)GetValue(RenderOptions.CacheInvalidationThresholdMinimumProperty);
                    data.CacheInvalidationThresholdMaximum = (double)GetValue(RenderOptions.CacheInvalidationThresholdMaximumProperty);
                    data.hVisual = hVisual;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_VISUALBRUSH));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_VISUALBRUSH))
                {
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);
                    Transform vRelativeTransform = RelativeTransform;
                    if (vRelativeTransform != null) ((DUCE.IResource)vRelativeTransform).AddRefOnChannel(channel);
                    Visual vVisual = Visual;
                    if (vVisual != null) vVisual.AddRefOnChannelForCyclicBrush(this, channel);
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
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).ReleaseOnChannel(channel);
                    Transform vRelativeTransform = RelativeTransform;
                    if (vRelativeTransform != null) ((DUCE.IResource)vRelativeTransform).ReleaseOnChannel(channel);
                    Visual vVisual = Visual;
                    if (vVisual != null) vVisual.ReleaseOnChannelForCyclicBrush(this, channel);
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
        //    Visual
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
        ///     The DependencyProperty for the VisualBrush.Visual property.
        /// </summary>
        public static readonly DependencyProperty VisualProperty;
        /// <summary>
        ///     The DependencyProperty for the VisualBrush.AutoLayoutContent property.
        /// </summary>
        public static readonly DependencyProperty AutoLayoutContentProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const bool c_AutoLayoutContent = true;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static VisualBrush()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(VisualBrush);
            VisualProperty =
                  RegisterProperty("Visual",
                                   typeof(Visual),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(VisualPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AutoLayoutContentProperty =
                  RegisterProperty("AutoLayoutContent",
                                   typeof(bool),
                                   typeofThis,
                                   true,
                                   new PropertyChangedCallback(AutoLayoutContentPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
