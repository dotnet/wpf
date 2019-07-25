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
    sealed partial class MatrixCamera : Camera
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
        public new MatrixCamera Clone()
        {
            return (MatrixCamera)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new MatrixCamera CloneCurrentValue()
        {
            return (MatrixCamera)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ViewMatrixPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MatrixCamera target = ((MatrixCamera) d);


            target.PropertyChanged(ViewMatrixProperty);
        }
        private static void ProjectionMatrixPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MatrixCamera target = ((MatrixCamera) d);


            target.PropertyChanged(ProjectionMatrixProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ViewMatrix - Matrix3D.  Default value is Matrix3D.Identity.
        /// </summary>
        public Matrix3D ViewMatrix
        {
            get
            {
                return (Matrix3D) GetValue(ViewMatrixProperty);
            }
            set
            {
                SetValueInternal(ViewMatrixProperty, value);
            }
        }

        /// <summary>
        ///     ProjectionMatrix - Matrix3D.  Default value is Matrix3D.Identity.
        /// </summary>
        public Matrix3D ProjectionMatrix
        {
            get
            {
                return (Matrix3D) GetValue(ProjectionMatrixProperty);
            }
            set
            {
                SetValueInternal(ProjectionMatrixProperty, value);
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
            return new MatrixCamera();
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

                // Pack & send command packet
                DUCE.MILCMD_MATRIXCAMERA data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdMatrixCamera;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.htransform = hTransform;
                    data.viewMatrix = CompositionResourceManager.Matrix3DToD3DMATRIX(ViewMatrix);
                    data.projectionMatrix = CompositionResourceManager.Matrix3DToD3DMATRIX(ProjectionMatrix);

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_MATRIXCAMERA));
                }
            }
        }
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_MATRIXCAMERA))
                {
                    Transform3D vTransform = Transform;
                    if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);

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
        ///     The DependencyProperty for the MatrixCamera.ViewMatrix property.
        /// </summary>
        public static readonly DependencyProperty ViewMatrixProperty;
        /// <summary>
        ///     The DependencyProperty for the MatrixCamera.ProjectionMatrix property.
        /// </summary>
        public static readonly DependencyProperty ProjectionMatrixProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        internal static Matrix3D s_ViewMatrix = Matrix3D.Identity;
        internal static Matrix3D s_ProjectionMatrix = Matrix3D.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static MatrixCamera()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.



            // Initializations
            Type typeofThis = typeof(MatrixCamera);
            ViewMatrixProperty =
                  RegisterProperty("ViewMatrix",
                                   typeof(Matrix3D),
                                   typeofThis,
                                   Matrix3D.Identity,
                                   new PropertyChangedCallback(ViewMatrixPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ProjectionMatrixProperty =
                  RegisterProperty("ProjectionMatrix",
                                   typeof(Matrix3D),
                                   typeofThis,
                                   Matrix3D.Identity,
                                   new PropertyChangedCallback(ProjectionMatrixPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
