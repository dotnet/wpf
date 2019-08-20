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
    sealed partial class MaterialGroup : Material
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
        public new MaterialGroup Clone()
        {
            return (MaterialGroup)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new MaterialGroup CloneCurrentValue()
        {
            return (MaterialGroup)base.CloneCurrentValue();
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


            MaterialGroup target = ((MaterialGroup) d);


            // If this is both non-null and mutable, we need to unhook the Changed event.
            MaterialCollection oldCollection = null;
            MaterialCollection newCollection = null;

            if ((e.OldValueSource != BaseValueSourceInternal.Default) || e.IsOldValueModified)
            {
                oldCollection = (MaterialCollection) e.OldValue;
                if ((oldCollection != null) && !oldCollection.IsFrozen)
                {
                    oldCollection.ItemRemoved -= target.ChildrenItemRemoved;
                    oldCollection.ItemInserted -= target.ChildrenItemInserted;
                }
            }

            // If this is both non-null and mutable, we need to hook the Changed event.
            if ((e.NewValueSource != BaseValueSourceInternal.Default) || e.IsNewValueModified)
            {
                newCollection = (MaterialCollection) e.NewValue;
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


        #region Public Properties

        /// <summary>
        ///     Children - MaterialCollection.  Default value is new FreezableDefaultValueFactory(MaterialCollection.Empty).
        /// </summary>
        public MaterialCollection Children
        {
            get
            {
                return (MaterialCollection) GetValue(ChildrenProperty);
            }
            set
            {
                SetValueInternal(ChildrenProperty, value);
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
            return new MaterialGroup();
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
                MaterialCollection vChildren = Children;

                // Store the count of this resource's contained collections in local variables.
                int ChildrenCount = (vChildren == null) ? 0 : vChildren.Count;

                // Pack & send command packet
                DUCE.MILCMD_MATERIALGROUP data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdMaterialGroup;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.ChildrenSize = (uint)(sizeof(DUCE.ResourceHandle) * ChildrenCount);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_MATERIALGROUP),
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
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_MATERIALGROUP))
                {
                    MaterialCollection vChildren = Children;

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
                    MaterialCollection vChildren = Children;

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

        //
        //  This property finds the correct initial size for the _effectiveValues store on the
        //  current DependencyObject as a performance optimization
        //
        //  This includes:
        //    Children
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
        ///     The DependencyProperty for the MaterialGroup.Children property.
        /// </summary>
        public static readonly DependencyProperty ChildrenProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static MaterialCollection s_Children = MaterialCollection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static MaterialGroup()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.

            Debug.Assert(s_Children == null || s_Children.IsFrozen,
                "Detected context bound default value MaterialGroup.s_Children (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(MaterialGroup);
            ChildrenProperty =
                  RegisterProperty("Children",
                                   typeof(MaterialCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(MaterialCollection.Empty),
                                   new PropertyChangedCallback(ChildrenPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
