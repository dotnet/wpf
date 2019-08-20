// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implementation of the class StreamGeometry
//

using System;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Markup;
using System.Windows.Converters;
using System.Runtime.InteropServices;
using System.Security;
using MS.Win32;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    #region StreamGeometry
    /// <summary>
    /// StreamGeometry
    /// </summary>
    [TypeConverter(typeof(GeometryConverter))]
    public sealed partial class StreamGeometry : Geometry
    {
        #region Constructors
        /// <summary>
        ///
        /// </summary>
        public StreamGeometry()
        {
        }
        #endregion

        /// <summary>
        /// Opens the StreamGeometry for population.
        /// </summary>
        public StreamGeometryContext Open()
        {
            WritePreamble();

            return new StreamGeometryCallbackContext(this);
        }


        /// <summary>
        /// Remove all figures
        /// </summary>
        public void Clear()
        {
            WritePreamble();

            _data = null;
            SetDirty();
            RegisterForAsyncUpdateResource();
        }

        /// <summary>
        /// Returns true if this geometry is empty
        /// </summary>
        public override bool IsEmpty()
        {
            ReadPreamble();

            if ((_data == null) || (_data.Length <= 0))
            {
                return true;
            }

            unsafe
            {
                Invariant.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                fixed (byte *pbPathData = _data)
                {
                    MIL_PATHGEOMETRY* pPathGeometry = (MIL_PATHGEOMETRY*)pbPathData;

                    return pPathGeometry->FigureCount <= 0;
                }
            }
        }

        /// <summary>
        /// AreBoundsValid Property - returns true if the bounds are valid, false otherwise.
        /// If true, the bounds are stored in the bounds param.
        /// </summary>
        private bool AreBoundsValid(ref MilRectD bounds)
        {
            if (IsEmpty())
            {
                return false;
            }

            unsafe
            {
                Debug.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                fixed (byte* pbPathData = _data)
                {
                    MIL_PATHGEOMETRY* pGeometry = (MIL_PATHGEOMETRY*)pbPathData;

                    bool areBoundsValid = (pGeometry->Flags & MilPathGeometryFlags.BoundsValid) != 0;

                    if (areBoundsValid)
                    {
                        bounds = pGeometry->Bounds;
                    }

                    return areBoundsValid;
                }
            }
        }

        /// <summary>
        /// CacheBounds - store the calculated bounds in the data stream.
        /// </summary>
        private void CacheBounds(ref MilRectD bounds)
        {
            unsafe
            {
                Debug.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                fixed (byte* pbPathData = _data)
                {
                    MIL_PATHGEOMETRY* pGeometry = (MIL_PATHGEOMETRY*)pbPathData;

                    pGeometry->Flags |= MilPathGeometryFlags.BoundsValid;
                    pGeometry->Bounds = bounds;
                }
            }
        }

        /// <summary>
        /// SetDirty - indicate that the cached bounds on this Geometry are not valid.
        /// </summary>
        internal void SetDirty()
        {
            if (!IsEmpty())
            {
                unsafe
                {
                    Debug.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                    fixed (byte* pbPathData = _data)
                    {
                        MIL_PATHGEOMETRY* pGeometry = (MIL_PATHGEOMETRY*)pbPathData;

                        pGeometry->Flags &= ~MilPathGeometryFlags.BoundsValid;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the bounds of this StreamGeometry as an axis-aligned bounding box
        /// </summary>
        public override Rect Bounds
        {
            get
            {
                ReadPreamble();

                if (IsEmpty())
                {
                    return Rect.Empty;
                }
                else
                {
                    MilRectD bounds = new MilRectD();

                    if (!AreBoundsValid(ref bounds))
                    {
                        // Update the cached bounds
                        bounds = PathGeometry.GetPathBoundsAsRB(
                            GetPathGeometryData(),
                            null,   // pen
                            Matrix.Identity,
                            StandardFlatteningTolerance,
                            ToleranceType.Absolute,
                            false);  // Do not skip non-fillable figures

                        CacheBounds(ref bounds);
                    }

                    return bounds.AsRect;
                }
            }
        }

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        public override bool MayHaveCurves()
        {
            // IsEmpty() calls ReadPreamble()
            if (IsEmpty())
            {
                return false;
            }

            unsafe
            {
                Invariant.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                fixed (byte* pbPathData = (this._data))
                {
                    MIL_PATHGEOMETRY* pPathGeometryData = (MIL_PATHGEOMETRY*)pbPathData;
                    return (pPathGeometryData->Flags & MilPathGeometryFlags.HasCurves) != 0;
                }
            }
        }

        #region Internal

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        internal bool HasHollows()
        {
            // IsEmpty() calls ReadPreamble()
            if (IsEmpty())
            {
                return false;
            }

            unsafe
            {
                Invariant.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                fixed (byte* pbPathData = (this._data))
                {
                    MIL_PATHGEOMETRY* pPathGeometryData = (MIL_PATHGEOMETRY*)pbPathData;
                    return (pPathGeometryData->Flags & MilPathGeometryFlags.HasHollows) != 0;
                }
            }
        }

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        internal bool HasGaps()
        {
            // IsEmpty() calls ReadPreamble()
            if (IsEmpty())
            {
                return false;
            }

            unsafe
            {
                Invariant.Assert((_data != null) && (_data.Length >= sizeof(MIL_PATHGEOMETRY)));
                fixed (byte* pbPathData = (this._data))
                {
                    MIL_PATHGEOMETRY* pPathGeometryData = (MIL_PATHGEOMETRY*)pbPathData;
                    return (pPathGeometryData->Flags & MilPathGeometryFlags.HasGaps) != 0;
                }
            }
        }

        /// <summary>
        /// Called from the StreamGeometryContext when it is closed.
        /// </summary>
        internal void Close(byte[] _buffer)
        {
            SetDirty();
            _data = _buffer;

            RegisterForAsyncUpdateResource();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.OnChanged">Freezable.OnChanged</see>.
        /// </summary>
        protected override void OnChanged()
        {
            SetDirty();

            base.OnChanged();
        }

        /// <summary>
        /// GetAsPathGeometry - return a PathGeometry version of this Geometry
        /// </summary>
        internal override PathGeometry GetAsPathGeometry()
        {
            PathStreamGeometryContext ctx = new PathStreamGeometryContext(FillRule, Transform);
            PathGeometry.ParsePathGeometryData(GetPathGeometryData(), ctx);

            return ctx.GetPathGeometry();
        }

        /// <summary>
        /// GetTransformedFigureCollection - inherited from Geometry.
        /// Basically, this is used to collapse this Geometry into an existing PathGeometry.
        /// </summary>
        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            PathGeometry thisAsPathGeometry = GetAsPathGeometry();

            if (null != thisAsPathGeometry)
            {
                return thisAsPathGeometry.GetTransformedFigureCollection(transform);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Can serialize "this" to a string
        /// </summary>
        internal override bool CanSerializeToString()
        {
            Transform transform = Transform;

            return (((transform == null) || transform.IsIdentity) &&
                    !HasHollows() && !HasGaps());
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            // Consider serializing Data more efficiently.

            return GetAsPathGeometry().ConvertToString(format, provider);
        }

        private void InvalidateResourceFigures(object sender, EventArgs args)
        {
            // This is necessary to invalidate the cached bounds.
            SetDirty();
            RegisterForAsyncUpdateResource();
        }

        /// <summary>
        /// GetPathGeometryData - returns a struct which contains this Geometry represented
        /// as a path geometry's serialized format.
        /// </summary>
        internal override PathGeometryData GetPathGeometryData()
        {
            if (IsEmpty())
            {
                return Geometry.GetEmptyPathGeometryData();
            }

            PathGeometryData data = new PathGeometryData();
            data.FillRule = FillRule;
            data.Matrix = CompositionResourceManager.TransformToMilMatrix3x2D(Transform);
            data.SerializedData = _data;

            return data;
        }

        internal override void TransformPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            SetDirty();
        }

        #endregion

        #region DUCE

        private unsafe int GetFigureSize(byte* pbPathData)
        {
            MIL_PATHGEOMETRY* pPathGeometryData = (MIL_PATHGEOMETRY*)pbPathData;
            return pPathGeometryData == null ? 0 : (int)pPathGeometryData->Size;
        }

        internal override void UpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                checked
                {
                    Transform vTransform = Transform;

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

                    DUCE.MILCMD_PATHGEOMETRY data;
                    data.Type = MILCMD.MilCmdPathGeometry;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.hTransform = hTransform;
                    data.FillRule = FillRule;

                    byte[] pathDataToMarshal = _data == null ?
                        Geometry.GetEmptyPathGeometryData().SerializedData :
                        _data;

                    unsafe
                    {
                        fixed (byte* pbPathData = pathDataToMarshal)
                        {
                            data.FiguresSize = (uint)GetFigureSize(pbPathData);

                            channel.BeginCommand(
                                (byte*)&data,
                                sizeof(DUCE.MILCMD_PATHGEOMETRY),
                                (int)data.FiguresSize
                                );

                            channel.AppendCommandData(pbPathData, (int)data.FiguresSize);
                        }

                        channel.EndCommand();
                    }
                }
            }

            base.UpdateResource(channel, skipOnChannelCheck);
        }

        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_PATHGEOMETRY))
            {
                Transform vTransform = Transform;

                if (vTransform != null) ((DUCE.IResource)vTransform).AddRefOnChannel(channel);

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
            }
        }

        internal override DUCE.ResourceHandle GetHandleCore(DUCE.Channel channel)
        {
            // Note that we are in a lock here already.
            return _duceResource.GetHandle(channel);
        }

        internal override int GetChannelCountCore()
        {
            return _duceResource.GetChannelCount();
        }

        internal override DUCE.Channel GetChannelCore(int index)
        {
            return _duceResource.GetChannel(index);
        }

        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            base.CloneCore(source);

            StreamGeometry sourceStream = (StreamGeometry) source;

            if ((sourceStream._data != null) && (sourceStream._data.Length > 0))
            {
                _data = new byte[sourceStream._data.Length];
                sourceStream._data.CopyTo(_data, 0);
            }
        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            base.CloneCurrentValueCore(source);

            StreamGeometry sourceStream = (StreamGeometry) source;

            if ((sourceStream._data != null) && (sourceStream._data.Length > 0))
            {
                _data = new byte[sourceStream._data.Length];
                sourceStream._data.CopyTo(_data, 0);
            }
        }

        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            base.GetAsFrozenCore(source);

            StreamGeometry sourceStream = (StreamGeometry) source;

            if ((sourceStream._data != null) && (sourceStream._data.Length > 0))
            {
                _data = new byte[sourceStream._data.Length];
                sourceStream._data.CopyTo(_data, 0);
            }
        }

        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            base.GetCurrentValueAsFrozenCore(source);

            StreamGeometry sourceStream = (StreamGeometry) source;

            if ((sourceStream._data != null) && (sourceStream._data.Length > 0))
            {
                _data = new byte[sourceStream._data.Length];
                sourceStream._data.CopyTo(_data, 0);
            }
        }

        #endregion DUCE

        #region Data

        private byte[] _data;
        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        #endregion
    }
    #endregion

    #region StreamGeometryCallbackContext
    internal class StreamGeometryCallbackContext: ByteStreamGeometryContext
    {
        /// <summary>
        /// Creates a geometry stream context which is associated with a given owner
        /// </summary>
        internal StreamGeometryCallbackContext(StreamGeometry owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// CloseCore - This method is implemented by derived classes to hand off the content
        /// to its eventual destination.
        /// </summary>
        protected override void CloseCore(byte[] data)
        {
            _owner.Close(data);
        }

        private StreamGeometry _owner;
    }
    #endregion StreamGeometryCallbackContext
}

