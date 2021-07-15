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
    sealed partial class GeometryModel3D : Model3D
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
        public new GeometryModel3D Clone()
        {
            return (GeometryModel3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GeometryModel3D CloneCurrentValue()
        {
            return (GeometryModel3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            GeometryModel3D target = ((GeometryModel3D) d);


            Geometry3D oldV = (Geometry3D) e.OldValue;
            Geometry3D newV = (Geometry3D) e.NewValue;
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

            target.PropertyChanged(GeometryProperty);
        }
        private static void MaterialPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GeometryModel3D target = ((GeometryModel3D) d);


            target.MaterialPropertyChangedHook(e);



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



            Material oldV = (Material) e.OldValue;
            Material newV = (Material) e.NewValue;
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

            target.PropertyChanged(MaterialProperty);
        }
        private static void BackMaterialPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GeometryModel3D target = ((GeometryModel3D) d);


            target.BackMaterialPropertyChangedHook(e);



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



            Material oldV = (Material) e.OldValue;
            Material newV = (Material) e.NewValue;
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

            target.PropertyChanged(BackMaterialProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Geometry - Geometry3D.  Default value is null.
        /// </summary>
        public Geometry3D Geometry
        {
            get
            {
                return (Geometry3D) GetValue(GeometryProperty);
            }
            set
            {
                SetValueInternal(GeometryProperty, value);
            }
        }

        /// <summary>
        ///     Material - Material.  Default value is null.
        /// </summary>
        public Material Material
        {
            get
            {
                return (Material) GetValue(MaterialProperty);
            }
            set
            {
                SetValueInternal(MaterialProperty, value);
            }
        }

        /// <summary>
        ///     BackMaterial - Material.  Default value is null.
        /// </summary>
        public Material BackMaterial
        {
            get
            {
                return (Material) GetValue(BackMaterialProperty);
            }
            set
            {
                SetValueInternal(BackMaterialProperty, value);
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
            return new GeometryModel3D();
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
                Transform3D vTransform = Transform;
                Geometry3D vGeometry = Geometry;
                Material vMaterial = Material;
                Material vBackMaterial = BackMaterial;

                // Obtain handles for properties that implement DUCE.IResource
                DUCE.ResourceHandle hTransform;
                if (vTransform == null ||
                    Object.ReferenceEquals(vTransform, Transform3D.Identity)
                    )
                {
                    hTransform = DUCE.ResourceHandle.Null;
                }
                else
                {
                    hTransform = ((DUCE.IResource)vTransform).GetHandle(channel);
                }
                DUCE.ResourceHandle hGeometry = vGeometry != null ? ((DUCE.IResource)vGeometry).GetHandle(channel) : DUCE.ResourceHandle.Null;
                DUCE.ResourceHandle hMaterial = vMaterial != null ? ((DUCE.IResource)vMaterial).GetHandle(channel) : DUCE.ResourceHandle.Null;
                DUCE.ResourceHandle hBackMaterial = vBackMaterial != null ? ((DUCE.IResource)vBackMaterial).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Pack & send command packet
                DUCE.MILCMD_GEOMETRYMODEL3D data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdGeometryModel3D;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.htransform = hTransform;
                    data.hgeometry = hGeometry;
                    data.hmaterial = hMaterial;
                    data.hbackMaterial = hBackMaterial;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_GEOMETRYMODEL3D));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_GEOMETRYMODEL3D))
                {
                    Transform3D vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);
                    Geometry3D vGeometry = Geometry;
                    if (vGeometry != null) ((DUCE.IResource)vGeometry).AddRefOnChannel(channel);
                    Material vMaterial = Material;
                    if (vMaterial != null) ((DUCE.IResource)vMaterial).AddRefOnChannel(channel);
                    Material vBackMaterial = BackMaterial;
                    if (vBackMaterial != null) ((DUCE.IResource)vBackMaterial).AddRefOnChannel(channel);

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
                    Transform3D vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).ReleaseOnChannel(channel);
                    Geometry3D vGeometry = Geometry;
                    if (vGeometry != null) ((DUCE.IResource)vGeometry).ReleaseOnChannel(channel);
                    Material vMaterial = Material;
                    if (vMaterial != null) ((DUCE.IResource)vMaterial).ReleaseOnChannel(channel);
                    Material vBackMaterial = BackMaterial;
                    if (vBackMaterial != null) ((DUCE.IResource)vBackMaterial).ReleaseOnChannel(channel);

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
        ///     The DependencyProperty for the GeometryModel3D.Geometry property.
        /// </summary>
        public static readonly DependencyProperty GeometryProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryModel3D.Material property.
        /// </summary>
        public static readonly DependencyProperty MaterialProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryModel3D.BackMaterial property.
        /// </summary>
        public static readonly DependencyProperty BackMaterialProperty;

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

        static GeometryModel3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(GeometryModel3D);
            GeometryProperty =
                  RegisterProperty("Geometry",
                                   typeof(Geometry3D),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(GeometryPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            MaterialProperty =
                  RegisterProperty("Material",
                                   typeof(Material),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(MaterialPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BackMaterialProperty =
                  RegisterProperty("BackMaterial",
                                   typeof(Material),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BackMaterialPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
