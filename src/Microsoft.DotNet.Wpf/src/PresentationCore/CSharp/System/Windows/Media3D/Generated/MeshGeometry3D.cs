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
    sealed partial class MeshGeometry3D : Geometry3D
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
        public new MeshGeometry3D Clone()
        {
            return (MeshGeometry3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new MeshGeometry3D CloneCurrentValue()
        {
            return (MeshGeometry3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void PositionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(PositionsProperty);
        }
        private static void NormalsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(NormalsProperty);
        }
        private static void TextureCoordinatesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(TextureCoordinatesProperty);
        }
        private static void TriangleIndicesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(TriangleIndicesProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Positions - Point3DCollection.  Default value is new FreezableDefaultValueFactory(Point3DCollection.Empty).
        /// </summary>
        public Point3DCollection Positions
        {
            get
            {
                return (Point3DCollection) GetValue(PositionsProperty);
            }
            set
            {
                SetValueInternal(PositionsProperty, value);
            }
        }

        /// <summary>
        ///     Normals - Vector3DCollection.  Default value is new FreezableDefaultValueFactory(Vector3DCollection.Empty).
        /// </summary>
        public Vector3DCollection Normals
        {
            get
            {
                return (Vector3DCollection) GetValue(NormalsProperty);
            }
            set
            {
                SetValueInternal(NormalsProperty, value);
            }
        }

        /// <summary>
        ///     TextureCoordinates - PointCollection.  Default value is new FreezableDefaultValueFactory(PointCollection.Empty).
        /// </summary>
        public PointCollection TextureCoordinates
        {
            get
            {
                return (PointCollection) GetValue(TextureCoordinatesProperty);
            }
            set
            {
                SetValueInternal(TextureCoordinatesProperty, value);
            }
        }

        /// <summary>
        ///     TriangleIndices - Int32Collection.  Default value is new FreezableDefaultValueFactory(Int32Collection.Empty).
        /// </summary>
        public Int32Collection TriangleIndices
        {
            get
            {
                return (Int32Collection) GetValue(TriangleIndicesProperty);
            }
            set
            {
                SetValueInternal(TriangleIndicesProperty, value);
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
            return new MeshGeometry3D();
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
                Point3DCollection vPositions = Positions;
                Vector3DCollection vNormals = Normals;
                PointCollection vTextureCoordinates = TextureCoordinates;
                Int32Collection vTriangleIndices = TriangleIndices;

                // Store the count of this resource's contained collections in local variables.
                int PositionsCount = (vPositions == null) ? 0 : vPositions.Count;
                int NormalsCount = (vNormals == null) ? 0 : vNormals.Count;
                int TextureCoordinatesCount = (vTextureCoordinates == null) ? 0 : vTextureCoordinates.Count;
                int TriangleIndicesCount = (vTriangleIndices == null) ? 0 : vTriangleIndices.Count;

                // Pack & send command packet
                DUCE.MILCMD_MESHGEOMETRY3D data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdMeshGeometry3D;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.PositionsSize = (uint)(sizeof(MilPoint3F) * PositionsCount);
                    data.NormalsSize = (uint)(sizeof(MilPoint3F) * NormalsCount);
                    data.TextureCoordinatesSize = (uint)(sizeof(Point) * TextureCoordinatesCount);
                    data.TriangleIndicesSize = (uint)(sizeof(Int32) * TriangleIndicesCount);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_MESHGEOMETRY3D),
                        (int)(data.PositionsSize + 
                              data.NormalsSize + 
                              data.TextureCoordinatesSize + 
                              data.TriangleIndicesSize)
                        );


                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < PositionsCount; i++)
                    {
                        MilPoint3F resource = CompositionResourceManager.Point3DToMilPoint3F(vPositions.Internal_GetItem(i));
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(MilPoint3F)
                            );
                    }

                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < NormalsCount; i++)
                    {
                        MilPoint3F resource = CompositionResourceManager.Vector3DToMilPoint3F(vNormals.Internal_GetItem(i));
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(MilPoint3F)
                            );
                    }

                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < TextureCoordinatesCount; i++)
                    {
                        Point resource = vTextureCoordinates.Internal_GetItem(i);
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(Point)
                            );
                    }

                    // Copy this collection's elements (or their handles) to reserved data
                    for(int i = 0; i < TriangleIndicesCount; i++)
                    {
                        Int32 resource = vTriangleIndices.Internal_GetItem(i);
                        channel.AppendCommandData(
                            (byte*)&resource,
                            sizeof(Int32)
                            );
                    }

                    channel.EndCommand();
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_MESHGEOMETRY3D))
                {
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
        //    Positions
        //    Normals
        //    TextureCoordinates
        //    TriangleIndices
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 4;
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
        ///     The DependencyProperty for the MeshGeometry3D.Positions property.
        /// </summary>
        public static readonly DependencyProperty PositionsProperty;
        /// <summary>
        ///     The DependencyProperty for the MeshGeometry3D.Normals property.
        /// </summary>
        public static readonly DependencyProperty NormalsProperty;
        /// <summary>
        ///     The DependencyProperty for the MeshGeometry3D.TextureCoordinates property.
        /// </summary>
        public static readonly DependencyProperty TextureCoordinatesProperty;
        /// <summary>
        ///     The DependencyProperty for the MeshGeometry3D.TriangleIndices property.
        /// </summary>
        public static readonly DependencyProperty TriangleIndicesProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Point3DCollection s_Positions = Point3DCollection.Empty;
        internal static Vector3DCollection s_Normals = Vector3DCollection.Empty;
        internal static PointCollection s_TextureCoordinates = PointCollection.Empty;
        internal static Int32Collection s_TriangleIndices = Int32Collection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static MeshGeometry3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.

            Debug.Assert(s_Positions == null || s_Positions.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_Positions (See OS Bug #947272).");


            Debug.Assert(s_Normals == null || s_Normals.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_Normals (See OS Bug #947272).");


            Debug.Assert(s_TextureCoordinates == null || s_TextureCoordinates.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_TextureCoordinates (See OS Bug #947272).");


            Debug.Assert(s_TriangleIndices == null || s_TriangleIndices.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_TriangleIndices (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(MeshGeometry3D);
            PositionsProperty =
                  RegisterProperty("Positions",
                                   typeof(Point3DCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(Point3DCollection.Empty),
                                   new PropertyChangedCallback(PositionsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            NormalsProperty =
                  RegisterProperty("Normals",
                                   typeof(Vector3DCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(Vector3DCollection.Empty),
                                   new PropertyChangedCallback(NormalsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TextureCoordinatesProperty =
                  RegisterProperty("TextureCoordinates",
                                   typeof(PointCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(PointCollection.Empty),
                                   new PropertyChangedCallback(TextureCoordinatesPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TriangleIndicesProperty =
                  RegisterProperty("TriangleIndices",
                                   typeof(Int32Collection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(Int32Collection.Empty),
                                   new PropertyChangedCallback(TriangleIndicesPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
