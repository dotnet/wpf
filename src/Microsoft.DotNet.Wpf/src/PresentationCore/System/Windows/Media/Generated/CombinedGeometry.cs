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
    sealed partial class CombinedGeometry : Geometry
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
        public new CombinedGeometry Clone()
        {
            return (CombinedGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new CombinedGeometry CloneCurrentValue()
        {
            return (CombinedGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GeometryCombineModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CombinedGeometry target = ((CombinedGeometry) d);


            target.PropertyChanged(GeometryCombineModeProperty);
        }
        private static void Geometry1PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            CombinedGeometry target = ((CombinedGeometry) d);


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

            target.PropertyChanged(Geometry1Property);
        }
        private static void Geometry2PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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


            CombinedGeometry target = ((CombinedGeometry) d);


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

            target.PropertyChanged(Geometry2Property);
        }


        #region Public Properties

        /// <summary>
        ///     GeometryCombineMode - GeometryCombineMode.  Default value is GeometryCombineMode.Union.
        /// </summary>
        public GeometryCombineMode GeometryCombineMode
        {
            get
            {
                return (GeometryCombineMode) GetValue(GeometryCombineModeProperty);
            }
            set
            {
                SetValueInternal(GeometryCombineModeProperty, value);
            }
        }

        /// <summary>
        ///     Geometry1 - Geometry.  Default value is Geometry.Empty.
        /// </summary>
        public Geometry Geometry1
        {
            get
            {
                return (Geometry) GetValue(Geometry1Property);
            }
            set
            {
                SetValueInternal(Geometry1Property, value);
            }
        }

        /// <summary>
        ///     Geometry2 - Geometry.  Default value is Geometry.Empty.
        /// </summary>
        public Geometry Geometry2
        {
            get
            {
                return (Geometry) GetValue(Geometry2Property);
            }
            set
            {
                SetValueInternal(Geometry2Property, value);
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
            return new CombinedGeometry();
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
                Geometry vGeometry1 = Geometry1;
                Geometry vGeometry2 = Geometry2;

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
                DUCE.ResourceHandle hGeometry1 = vGeometry1 != null ? ((DUCE.IResource)vGeometry1).GetHandle(channel) : DUCE.ResourceHandle.Null;
                DUCE.ResourceHandle hGeometry2 = vGeometry2 != null ? ((DUCE.IResource)vGeometry2).GetHandle(channel) : DUCE.ResourceHandle.Null;

                // Pack & send command packet
                DUCE.MILCMD_COMBINEDGEOMETRY data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdCombinedGeometry;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.hTransform = hTransform;
                    data.GeometryCombineMode = GeometryCombineMode;
                    data.hGeometry1 = hGeometry1;
                    data.hGeometry2 = hGeometry2;

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_COMBINEDGEOMETRY));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_COMBINEDGEOMETRY))
                {
                    Transform vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);
                    Geometry vGeometry1 = Geometry1;
                    if (vGeometry1 != null) ((DUCE.IResource)vGeometry1).AddRefOnChannel(channel);
                    Geometry vGeometry2 = Geometry2;
                    if (vGeometry2 != null) ((DUCE.IResource)vGeometry2).AddRefOnChannel(channel);

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
                    Geometry vGeometry1 = Geometry1;
                    if (vGeometry1 != null) ((DUCE.IResource)vGeometry1).ReleaseOnChannel(channel);
                    Geometry vGeometry2 = Geometry2;
                    if (vGeometry2 != null) ((DUCE.IResource)vGeometry2).ReleaseOnChannel(channel);

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
        //    GeometryCombineMode
        //    Geometry1
        //    Geometry2
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 3;
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
        ///     The DependencyProperty for the CombinedGeometry.GeometryCombineMode property.
        /// </summary>
        public static readonly DependencyProperty GeometryCombineModeProperty;
        /// <summary>
        ///     The DependencyProperty for the CombinedGeometry.Geometry1 property.
        /// </summary>
        public static readonly DependencyProperty Geometry1Property;
        /// <summary>
        ///     The DependencyProperty for the CombinedGeometry.Geometry2 property.
        /// </summary>
        public static readonly DependencyProperty Geometry2Property;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal const GeometryCombineMode c_GeometryCombineMode = GeometryCombineMode.Union;
        internal static Geometry s_Geometry1 = Geometry.Empty;
        internal static Geometry s_Geometry2 = Geometry.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static CombinedGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.  (Windows OS 

            Debug.Assert(s_Geometry1 == null || s_Geometry1.IsFrozen,
                "Detected context bound default value CombinedGeometry.s_Geometry1 (See OS Bug #947272).");


            Debug.Assert(s_Geometry2 == null || s_Geometry2.IsFrozen,
                "Detected context bound default value CombinedGeometry.s_Geometry2 (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(CombinedGeometry);
            GeometryCombineModeProperty =
                  RegisterProperty("GeometryCombineMode",
                                   typeof(GeometryCombineMode),
                                   typeofThis,
                                   GeometryCombineMode.Union,
                                   new PropertyChangedCallback(GeometryCombineModePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsGeometryCombineModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            Geometry1Property =
                  RegisterProperty("Geometry1",
                                   typeof(Geometry),
                                   typeofThis,
                                   Geometry.Empty,
                                   new PropertyChangedCallback(Geometry1PropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            Geometry2Property =
                  RegisterProperty("Geometry2",
                                   typeof(Geometry),
                                   typeofThis,
                                   Geometry.Empty,
                                   new PropertyChangedCallback(Geometry2PropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
