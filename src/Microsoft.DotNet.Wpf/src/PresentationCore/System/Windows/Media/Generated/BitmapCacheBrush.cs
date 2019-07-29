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
    sealed partial class BitmapCacheBrush : Brush
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
        public new BitmapCacheBrush Clone()
        {
            return (BitmapCacheBrush)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapCacheBrush CloneCurrentValue()
        {
            return (BitmapCacheBrush)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void TargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            target.PropertyChanged(TargetProperty);
        }
        private static void BitmapCachePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            BitmapCache oldV = (BitmapCache) e.OldValue;
            BitmapCache newV = (BitmapCache) e.NewValue;
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

            target.PropertyChanged(BitmapCacheProperty);
        }
        private static void AutoLayoutContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            target.PropertyChanged(AutoLayoutContentProperty);
        }
        private static void InternalTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


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

            target.PropertyChanged(InternalTargetProperty);
        }
        private static void AutoWrapTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCacheBrush target = ((BitmapCacheBrush) d);


            target.PropertyChanged(AutoWrapTargetProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Target - Visual.  Default value is null.
        /// </summary>
        public Visual Target
        {
            get
            {
                return (Visual) GetValue(TargetProperty);
            }
            set
            {
                SetValueInternal(TargetProperty, value);
            }
        }

        /// <summary>
        ///     BitmapCache - BitmapCache.  Default value is null.
        /// </summary>
        public BitmapCache BitmapCache
        {
            get
            {
                return (BitmapCache) GetValue(BitmapCacheProperty);
            }
            set
            {
                SetValueInternal(BitmapCacheProperty, value);
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

        /// <summary>
        ///     InternalTarget - Visual.  Default value is null.
        /// </summary>
        internal Visual InternalTarget
        {
            get
            {
                return (Visual) GetValue(InternalTargetProperty);
            }
            set
            {
                SetValueInternal(InternalTargetProperty, value);
            }
        }

        /// <summary>
        ///     AutoWrapTarget - bool.  Default value is false.
        ///     If true, this Brush wrap its Target visual in a ContainerVisual, which will allow 
        ///     the brush to support rendering all properties on the visual above the cache to 
        ///     match VisualBrush's behavior.
        /// </summary>
        internal bool AutoWrapTarget
        {
            get
            {
                return (bool) GetValue(AutoWrapTargetProperty);
            }
            set
            {
                SetValueInternal(AutoWrapTargetProperty, BooleanBoxes.Box(value));
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
            return new BitmapCacheBrush();
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
                BitmapCache vBitmapCache = BitmapCache;
                Visual vInternalTarget = InternalTarget;

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
                DUCE.ResourceHandle hBitmapCache = vBitmapCache != null ? ((DUCE.IResource)vBitmapCache).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle  hInternalTarget = vInternalTarget != null ? ((DUCE.IResource)vInternalTarget).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hOpacityAnimations = GetAnimationResourceHandle(OpacityProperty, channel);

                // Pack & send command packet
                DUCE.MILCMD_BITMAPCACHEBRUSH data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdBitmapCacheBrush;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (hOpacityAnimations.IsNull)
                    {
                        data.Opacity = Opacity;
                    }
                    data.hOpacityAnimations = hOpacityAnimations;
                    data.hTransform = hTransform;
                    data.hRelativeTransform = hRelativeTransform;
                    data.hBitmapCache = hBitmapCache;
                    data.hInternalTarget = hInternalTarget;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_BITMAPCACHEBRUSH));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_BITMAPCACHEBRUSH))
                {
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);
                    Transform vRelativeTransform = RelativeTransform;
                    if (vRelativeTransform != null) ((DUCE.IResource)vRelativeTransform).AddRefOnChannel(channel);
                    BitmapCache vBitmapCache = BitmapCache;
                    if (vBitmapCache != null) ((DUCE.IResource)vBitmapCache).AddRefOnChannel(channel);
                    Visual vInternalTarget = InternalTarget;
                    if (vInternalTarget != null) vInternalTarget.AddRefOnChannelForCyclicBrush(this, channel);
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
                    BitmapCache vBitmapCache = BitmapCache;
                    if (vBitmapCache != null) ((DUCE.IResource)vBitmapCache).ReleaseOnChannel(channel);
                    Visual vInternalTarget = InternalTarget;
                    if (vInternalTarget != null) vInternalTarget.ReleaseOnChannelForCyclicBrush(this, channel);
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
        //    Target
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
        ///     The DependencyProperty for the BitmapCacheBrush.Target property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.BitmapCache property.
        /// </summary>
        public static readonly DependencyProperty BitmapCacheProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.AutoLayoutContent property.
        /// </summary>
        public static readonly DependencyProperty AutoLayoutContentProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.InternalTarget property.
        /// </summary>
        internal static readonly DependencyProperty InternalTargetProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCacheBrush.AutoWrapTarget property.
        /// </summary>
        internal static readonly DependencyProperty AutoWrapTargetProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const bool c_AutoLayoutContent = true;
        internal const bool c_AutoWrapTarget = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BitmapCacheBrush()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 



            // Initializations
            Type typeofThis = typeof(BitmapCacheBrush);
            StaticInitialize(typeofThis);
            TargetProperty =
                  RegisterProperty("Target",
                                   typeof(Visual),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(TargetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BitmapCacheProperty =
                  RegisterProperty("BitmapCache",
                                   typeof(BitmapCache),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BitmapCachePropertyChanged),
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
            InternalTargetProperty =
                  RegisterProperty("InternalTarget",
                                   typeof(Visual),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(InternalTargetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            AutoWrapTargetProperty =
                  RegisterProperty("AutoWrapTarget",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(AutoWrapTargetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
