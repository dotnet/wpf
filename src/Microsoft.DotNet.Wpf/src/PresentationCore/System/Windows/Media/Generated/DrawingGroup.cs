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
    sealed partial class DrawingGroup : Drawing
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
        public new DrawingGroup Clone()
        {
            return (DrawingGroup)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DrawingGroup CloneCurrentValue()
        {
            return (DrawingGroup)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ChildrenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            DrawingGroup target = ((DrawingGroup) d);


            // If this is both non-null and mutable, we need to unhook the Changed event.
            DrawingCollection oldCollection = null;
            DrawingCollection newCollection = null;

            if ((e.OldValueSource != BaseValueSourceInternal.Default) || e.IsOldValueModified)
            {
                oldCollection = (DrawingCollection) e.OldValue;
                if ((oldCollection != null) && !oldCollection.IsFrozen)
                {
                    oldCollection.ItemRemoved -= target.ChildrenItemRemoved;
                    oldCollection.ItemInserted -= target.ChildrenItemInserted;
                }
            }

            // If this is both non-null and mutable, we need to hook the Changed event.
            if ((e.NewValueSource != BaseValueSourceInternal.Default) || e.IsNewValueModified)
            {
                newCollection = (DrawingCollection) e.NewValue;
                if ((newCollection != null) && !newCollection.IsFrozen)
                {
                    newCollection.ItemInserted += target.ChildrenItemInserted;
                    newCollection.ItemRemoved += target.ChildrenItemRemoved;
                }
            }
            if (oldCollection != newCollection && target.Dispatcher != null)
            {
                using (CompositionEngineLock.Acquire())
                {
                    DUCE.IResource targetResource = (DUCE.IResource)target;
                    int channelCount = targetResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = targetResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!targetResource.GetHandle(channel).IsNull);
                        // resource shouldn't be null because
                        // 1) If the field is one of our collections, we don't allow null elements
                        // 2) Codegen already made sure the collection contains DUCE.IResources
                        // ... so we'll Assert it

                        if (newCollection != null)
                        {
                            int count = newCollection.Count;
                            for (int i = 0; i < count; i++)
                            {
                                DUCE.IResource resource = newCollection.Internal_GetItem(i) as DUCE.IResource;
                                Debug.Assert(resource != null);
                                resource.AddRefOnChannel(channel);
                            }
                        }

                        if (oldCollection != null)
                        {
                            int count = oldCollection.Count;
                            for (int i = 0; i < count; i++)
                            {
                                DUCE.IResource resource = oldCollection.Internal_GetItem(i) as DUCE.IResource;
                                Debug.Assert(resource != null);
                                resource.ReleaseOnChannel(channel);
                            }
                        }
                    }
                }
            }
            target.PropertyChanged(ChildrenProperty);
        }
        private static void ClipGeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            DrawingGroup target = ((DrawingGroup) d);


            Geometry oldV = (Geometry) e.OldValue;
            Geometry newV = (Geometry) e.NewValue;
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

            target.PropertyChanged(ClipGeometryProperty);
        }
        private static void OpacityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(OpacityProperty);
        }
        private static void OpacityMaskPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            DrawingGroup target = ((DrawingGroup) d);


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

            target.PropertyChanged(OpacityMaskProperty);
        }
        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            DrawingGroup target = ((DrawingGroup) d);


            Transform oldV = (Transform) e.OldValue;
            Transform newV = (Transform) e.NewValue;
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

            target.PropertyChanged(TransformProperty);
        }
        private static void GuidelineSetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            DrawingGroup target = ((DrawingGroup) d);


            GuidelineSet oldV = (GuidelineSet) e.OldValue;
            GuidelineSet newV = (GuidelineSet) e.NewValue;
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

            target.PropertyChanged(GuidelineSetProperty);
        }
        private static void EdgeModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(RenderOptions.EdgeModeProperty);
        }
        private static void BitmapEffectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(BitmapEffectProperty);
        }
        private static void BitmapEffectInputPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(BitmapEffectInputProperty);
        }
        private static void BitmapScalingModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(RenderOptions.BitmapScalingModeProperty);
        }
        private static void ClearTypeHintPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(RenderOptions.ClearTypeHintProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Children - DrawingCollection.  Default value is new FreezableDefaultValueFactory(DrawingCollection.Empty).
        /// </summary>
        public DrawingCollection Children
        {
            get
            {
                return (DrawingCollection) GetValue(ChildrenProperty);
            }
            set
            {
                SetValueInternal(ChildrenProperty, value);
            }
        }

        /// <summary>
        ///     ClipGeometry - Geometry.  Default value is null.
        /// </summary>
        public Geometry ClipGeometry
        {
            get
            {
                return (Geometry) GetValue(ClipGeometryProperty);
            }
            set
            {
                SetValueInternal(ClipGeometryProperty, value);
            }
        }

        /// <summary>
        ///     Opacity - double.  Default value is 1.0.
        /// </summary>
        public double Opacity
        {
            get
            {
                return (double) GetValue(OpacityProperty);
            }
            set
            {
                SetValueInternal(OpacityProperty, value);
            }
        }

        /// <summary>
        ///     OpacityMask - Brush.  Default value is null.
        /// </summary>
        public Brush OpacityMask
        {
            get
            {
                return (Brush) GetValue(OpacityMaskProperty);
            }
            set
            {
                SetValueInternal(OpacityMaskProperty, value);
            }
        }

        /// <summary>
        ///     Transform - Transform.  Default value is null.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return (Transform) GetValue(TransformProperty);
            }
            set
            {
                SetValueInternal(TransformProperty, value);
            }
        }

        /// <summary>
        ///     GuidelineSet - GuidelineSet.  Default value is null.
        /// </summary>
        public GuidelineSet GuidelineSet
        {
            get
            {
                return (GuidelineSet) GetValue(GuidelineSetProperty);
            }
            set
            {
                SetValueInternal(GuidelineSetProperty, value);
            }
        }

        /// <summary>
        ///     BitmapEffect - BitmapEffect.  Default value is null.
        /// </summary>
        public BitmapEffect BitmapEffect
        {
            get
            {
                return (BitmapEffect) GetValue(BitmapEffectProperty);
            }
            set
            {
                SetValueInternal(BitmapEffectProperty, value);
            }
        }

        /// <summary>
        ///     BitmapEffectInput - BitmapEffectInput.  Default value is null.
        /// </summary>
        public BitmapEffectInput BitmapEffectInput
        {
            get
            {
                return (BitmapEffectInput) GetValue(BitmapEffectInputProperty);
            }
            set
            {
                SetValueInternal(BitmapEffectInputProperty, value);
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
            return new DrawingGroup();
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
                DrawingCollection vChildren = Children;
                Geometry vClipGeometry = ClipGeometry;
                Brush vOpacityMask = OpacityMask;
                Transform vTransform = Transform;
                GuidelineSet vGuidelineSet = GuidelineSet;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hClipGeometry = vClipGeometry != null ? ((DUCE.IResource)vClipGeometry).GetHandle(channel) : DUCE.ResourceHandle.Null;
                DUCE.ResourceHandle hOpacityMask = vOpacityMask != null ? ((DUCE.IResource)vOpacityMask).GetHandle(channel) : DUCE.ResourceHandle.Null;
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
                DUCE.ResourceHandle hGuidelineSet = vGuidelineSet != null ? ((DUCE.IResource)vGuidelineSet).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Obtain handles for animated properties
                DUCE.ResourceHandle hOpacityAnimations = GetAnimationResourceHandle(OpacityProperty, channel);

                // Store the count of this resource's contained collections in local variables.
                int ChildrenCount = (vChildren == null) ? 0 : vChildren.Count;

                // Pack & send command packet
                DUCE.MILCMD_DRAWINGGROUP data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdDrawingGroup;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.ChildrenSize = (uint)(sizeof(DUCE.ResourceHandle) * ChildrenCount);
                    data.hClipGeometry = hClipGeometry;
                    if (hOpacityAnimations.IsNull)
                    {
                        data.Opacity = Opacity;
                    }
                    data.hOpacityAnimations = hOpacityAnimations;
                    data.hOpacityMask = hOpacityMask;
                    data.hTransform = hTransform;
                    data.hGuidelineSet = hGuidelineSet;
                    data.EdgeMode = (EdgeMode)GetValue(RenderOptions.EdgeModeProperty);
                    data.bitmapScalingMode = (BitmapScalingMode)GetValue(RenderOptions.BitmapScalingModeProperty);
                    data.ClearTypeHint = (ClearTypeHint)GetValue(RenderOptions.ClearTypeHintProperty);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_DRAWINGGROUP),
                        (int)(data.ChildrenSize)
                        );


                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < ChildrenCount; i++)
                    {
                        DUCE.ResourceHandle resource = ((DUCE.IResource)vChildren.Internal_GetItem(i)).GetHandle(channel);;
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(DUCE.ResourceHandle)
                            );
                    }

                    channel.EndCommand();
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_DRAWINGGROUP))
                {
                    Geometry vClipGeometry = ClipGeometry;
                    if (vClipGeometry != null) ((DUCE.IResource)vClipGeometry).AddRefOnChannel(channel);
                    Brush vOpacityMask = OpacityMask;
                    if (vOpacityMask != null) ((DUCE.IResource)vOpacityMask).AddRefOnChannel(channel);
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);
                    GuidelineSet vGuidelineSet = GuidelineSet;
                    if (vGuidelineSet != null) ((DUCE.IResource)vGuidelineSet).AddRefOnChannel(channel);

                    DrawingCollection vChildren = Children;

                    if (vChildren != null)
                    {
                        int count = vChildren.Count;
                        for (int i = 0; i < count; i++)
                        {
                            ((DUCE.IResource) vChildren.Internal_GetItem(i)).AddRefOnChannel(channel);
                        }
                    }
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
                    Geometry vClipGeometry = ClipGeometry;
                    if (vClipGeometry != null) ((DUCE.IResource)vClipGeometry).ReleaseOnChannel(channel);
                    Brush vOpacityMask = OpacityMask;
                    if (vOpacityMask != null) ((DUCE.IResource)vOpacityMask).ReleaseOnChannel(channel);
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).ReleaseOnChannel(channel);
                    GuidelineSet vGuidelineSet = GuidelineSet;
                    if (vGuidelineSet != null) ((DUCE.IResource)vGuidelineSet).ReleaseOnChannel(channel);

                    DrawingCollection vChildren = Children;

                    if (vChildren != null)
                    {
                        int count = vChildren.Count;
                        for (int i = 0; i < count; i++)
                        {
                            ((DUCE.IResource) vChildren.Internal_GetItem(i)).ReleaseOnChannel(channel);
                        }
                    }
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

        private void ChildrenItemInserted(object sender, object item)
        {
            if (this.Dispatcher != null)
            {
                DUCE.IResource thisResource = (DUCE.IResource)this;
                using (CompositionEngineLock.Acquire())
                {
                    int channelCount = thisResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = thisResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!thisResource.GetHandle(channel).IsNull);

                        // We're on a channel, which means our dependents are also on the channel.
                        DUCE.IResource addResource = item as DUCE.IResource;
                        if (addResource != null)
                        {
                            addResource.AddRefOnChannel(channel);
                        }

                        UpdateResource(channel, true /* skip on channel check */);
                    }
                }
            }
        }

        private void ChildrenItemRemoved(object sender, object item)
        {
            if (this.Dispatcher != null)
            {
                DUCE.IResource thisResource = (DUCE.IResource)this;
                using (CompositionEngineLock.Acquire())
                {
                    int channelCount = thisResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = thisResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!thisResource.GetHandle(channel).IsNull);

                        UpdateResource(channel, true /* is on channel check */);

                        // We're on a channel, which means our dependents are also on the channel.
                        DUCE.IResource releaseResource = item as DUCE.IResource;
                        if (releaseResource != null)
                        {
                            releaseResource.ReleaseOnChannel(channel);
                        }
                    }
                }
            }
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
        ///     The DependencyProperty for the DrawingGroup.Children property.
        /// </summary>
        public static readonly DependencyProperty ChildrenProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.ClipGeometry property.
        /// </summary>
        public static readonly DependencyProperty ClipGeometryProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.Opacity property.
        /// </summary>
        public static readonly DependencyProperty OpacityProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.OpacityMask property.
        /// </summary>
        public static readonly DependencyProperty OpacityMaskProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.Transform property.
        /// </summary>
        public static readonly DependencyProperty TransformProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.GuidelineSet property.
        /// </summary>
        public static readonly DependencyProperty GuidelineSetProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.BitmapEffect property.
        /// </summary>
        public static readonly DependencyProperty BitmapEffectProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.BitmapEffectInput property.
        /// </summary>
        public static readonly DependencyProperty BitmapEffectInputProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static DrawingCollection s_Children = DrawingCollection.Empty;
        internal const double c_Opacity = 1.0;
        internal const EdgeMode c_EdgeMode = EdgeMode.Unspecified;
        internal const BitmapScalingMode c_BitmapScalingMode = BitmapScalingMode.Unspecified;
        internal const ClearTypeHint c_ClearTypeHint = ClearTypeHint.Auto;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DrawingGroup()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 

            Debug.Assert(s_Children == null || s_Children.IsFrozen,
                "Detected context bound default value DrawingGroup.s_Children (See OS Bug #947272).");


            RenderOptions.EdgeModeProperty.OverrideMetadata(
                typeof(DrawingGroup),
                new UIPropertyMetadata(EdgeMode.Unspecified,
                                       new PropertyChangedCallback(EdgeModePropertyChanged)));

            RenderOptions.BitmapScalingModeProperty.OverrideMetadata(
                typeof(DrawingGroup),
                new UIPropertyMetadata(BitmapScalingMode.Unspecified,
                                       new PropertyChangedCallback(BitmapScalingModePropertyChanged)));

            RenderOptions.ClearTypeHintProperty.OverrideMetadata(
                typeof(DrawingGroup),
                new UIPropertyMetadata(ClearTypeHint.Auto,
                                       new PropertyChangedCallback(ClearTypeHintPropertyChanged)));

            // Initializations
            Type typeofThis = typeof(DrawingGroup);
            ChildrenProperty =
                  RegisterProperty("Children",
                                   typeof(DrawingCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DrawingCollection.Empty),
                                   new PropertyChangedCallback(ChildrenPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ClipGeometryProperty =
                  RegisterProperty("ClipGeometry",
                                   typeof(Geometry),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(ClipGeometryPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            OpacityProperty =
                  RegisterProperty("Opacity",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(OpacityPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OpacityMaskProperty =
                  RegisterProperty("OpacityMask",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(OpacityMaskPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TransformProperty =
                  RegisterProperty("Transform",
                                   typeof(Transform),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(TransformPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            GuidelineSetProperty =
                  RegisterProperty("GuidelineSet",
                                   typeof(GuidelineSet),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(GuidelineSetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BitmapEffectProperty =
                  RegisterProperty("BitmapEffect",
                                   typeof(BitmapEffect),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BitmapEffectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BitmapEffectInputProperty =
                  RegisterProperty("BitmapEffectInput",
                                   typeof(BitmapEffectInput),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BitmapEffectInputPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
