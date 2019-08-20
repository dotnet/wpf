// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of RenderData.
//              A RenderData is the backing store for a Drawing or the contents
//              of a Visual.  It contains a data stream which is a byte array
//              containing renderdata instructions and an array of dependent resource.
//
//

using MS.Internal;
using MS.Utility;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Security;

namespace System.Windows.Media
{
    /// <summary>
    /// RenderData
    /// A RenderData is the backing store for a Drawing or the contents
    /// of a Visual.  It contains a data stream which is a byte array
    /// containing renderdata instructions and an array of dependent resource.
    ///
    /// NOTE: RenderData is a not a fully functional Freezable
    /// </summary>
    internal partial class RenderData : Freezable, DUCE.IResource, IDrawingContent
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        internal RenderData()
        {
            // RenderData is a transient object that does not want to participate
            // as the InheritanceContext of any of its dependents.  (It can be
            // the Freezable context.)
            CanBeInheritanceContext = false;
        }

        /// <summary>
        /// RecordHeader - This struct is the header for each record entry
        /// </summary>
        internal struct RecordHeader
        {
            public int Size;
            public MILCMD Id;
        }

        private enum PushType
        {
            BitmapEffect,
            Other
        }
 
        /// <summary>
        /// WriteDataRecord - writes a data record in the form of "size - id - data"
        /// The Length of the data packed is "size" - (2 * sizeof(int)).
        /// Note that the cbRecordSize param is *only* the size of the record itself.  The Size
        /// written to the stream will be larger (by sizeof(RecordHeader)) because it includes the size
        /// itself and the id.
        /// </summary>
        /// <param name="id"> MILCMD - the record id </param>
        /// <param name="pbRecord">
        ///   byte* pointing to at least cbRecordSize bytes which will be copied to the stream.
        /// </param>
        /// <param name="cbRecordSize"> int - the size, in bytes, of pbRecord. Must be >= 0. </param>
        public unsafe void WriteDataRecord(MILCMD id,
                                           byte* pbRecord,
                                           int cbRecordSize)
        {
            Debug.Assert(cbRecordSize >= 0);

            // The records must always be padded to be QWORD aligned.
            Debug.Assert((_curOffset % 8) == 0);
            Debug.Assert((cbRecordSize % 8) == 0);
            Debug.Assert((sizeof(RecordHeader) % 8) == 0);

            int totalSize, newOffset;
            checked
            {
                totalSize = cbRecordSize + sizeof(RecordHeader);
                newOffset = _curOffset + totalSize;
            }

            // Do we need to increase the buffer size?
            // Yes, if there's no buffer or if the buffer is too small.
            if ((_buffer == null) || (newOffset > _buffer.Length))
            {
                EnsureBuffer(newOffset);
            }

            // At this point, _buffer must be non-null and
            // _buffer.Length must be >= newOffset
            Debug.Assert((_buffer != null) && (_buffer.Length >= newOffset));

            // Also, because pinning a 0-length buffer fails, we assert this too.
            Debug.Assert(_buffer.Length > 0);

            RecordHeader header;

            header.Size = totalSize;
            header.Id = id;

            Marshal.Copy((IntPtr)(&header), this._buffer, _curOffset, sizeof(RecordHeader));
            Marshal.Copy((IntPtr)pbRecord, this._buffer, _curOffset + sizeof(RecordHeader), cbRecordSize);

            _curOffset += totalSize;
        }



        #region IDrawingContent

        /// <summary>
        /// Returns the bounding box occupied by the content
        /// </summary>
        /// <returns>
        /// Bounding box occupied by the content
        /// </returns>
        public Rect GetContentBounds(BoundsDrawingContextWalker ctx)
        {
            Debug.Assert(ctx != null);

            DrawingContextWalk(ctx);
            return ctx.Bounds;
        }

        /// <summary>
        /// Forward the current value of the content to the DrawingContextWalker
        /// methods.
        /// </summary>
        /// <param name="walker"> DrawingContextWalker to forward content to. </param>
        public void WalkContent(DrawingContextWalker walker)
        {
            DrawingContextWalk(walker);
        }

        /// <summary>
        /// Determines whether or not a point exists within the content
        /// </summary>
        /// <param name="point"> Point to hit-test for. </param>
        /// <returns>
        /// 'true' if the point exists within the content, 'false' otherwise
        /// </returns>
        public bool HitTestPoint(Point point)
        {
            HitTestDrawingContextWalker ctx = new HitTestWithPointDrawingContextWalker(point);

            DrawingContextWalk(ctx);

            return ctx.IsHit;
        }

        /// <summary>
        /// Hit-tests a geometry against this content
        /// </summary>
        /// <param name="geometry"> PathGeometry to hit-test for. </param>
        /// <returns>
        /// IntersectionDetail describing the result of the hit-test
        /// </returns>
        public IntersectionDetail HitTestGeometry(PathGeometry geometry)
        {
            HitTestDrawingContextWalker ctx =
                new HitTestWithGeometryDrawingContextWalker(geometry);

            DrawingContextWalk(ctx);

            return ctx.IntersectionDetail;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new RenderData();
        }

        // We don't need to call ReadPreamble() because this is an internal class.
        // Plus, the extra VerifyAccess() calls might be an issue.
        //
        // We don't need to call WritePreamble() because this cannot be frozen
        // (FreezeCore always returns false)
        //
        // We don't need to call WritePostscript() because we only care if children
        // below us change.
        //
        // About the calls to Invariant.Assert(false)... we're only implementing
        // Freezable to hook up parent pointers from the children Freezables
        // to the RenderData. RenderData should never be cloned or frozen and
        // the class is internal so we'll just put in some Asserts to make sure
        // we don't do it in the future.

        protected override void CloneCore(Freezable source)
        {
            Invariant.Assert(false);
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            Invariant.Assert(false);
        }

        protected override bool FreezeCore(bool isChecking)
        {
            return false;
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            Invariant.Assert(false);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            Invariant.Assert(false);
        }

        /// <summary>
        /// Propagates an event handler to Freezables and AnimationClockResources
        /// referenced by the content.
        /// </summary>
        /// <param name="handler"> Event handler to propagate </param>
        /// <param name="adding"> 'true' to add the handler, 'false' to remove it </param>
        public void PropagateChangedHandler(EventHandler handler, bool adding)
        {
            Debug.Assert(!this.IsFrozen);

            if (adding)
            {
                this.Changed += handler;
            }
            else
            {
                this.Changed -= handler;
            }

            for (int i = 0, count = _dependentResources.Count; i < count; i++)
            {
                Freezable freezableResource = _dependentResources[i] as Freezable;
                if (freezableResource != null)
                {
                    // Ideally, we would call OFPC(null, freezable) in AddDependentResource
                    // but RenderData never removes resources so nothing would ever remove
                    // the context pointer. Fortunately, content calls PropagateChangedHandler
                    // when it cares and when it stops caring about its resources. Thus, we'll
                    // do all context hookup here.
                    if (adding)
                    {
                        OnFreezablePropertyChanged(null, freezableResource);
                    }
                    else
                    {
                        OnFreezablePropertyChanged(freezableResource, null);
                    }
                }
                else
                {
                    // If it's not a Freezable it may be an AnimationClockResource, which we
                    // also need to handle.
                    AnimationClockResource clockResource = _dependentResources[i] as AnimationClockResource;

                    if (clockResource != null)
                    {
                        // if it is a clock, it better not be a Freezable too or we'll
                        // end up firing the handler twice
                        Debug.Assert(_dependentResources[i] as Freezable == null);

                        clockResource.PropagateChangedHandlersCore(handler, adding);
                    }
                }
            }
        }


        /// <summary>
        /// Returns the stack depth for the last top level effect that was pushed
        /// If no effects are currently on the stack, returns 0
        /// </summary>
        internal int BitmapEffectStackDepth
        {
            get
            {
                return _bitmapEffectStackDepth;
            }

            set
            {
                _bitmapEffectStackDepth = value;
            }
        }


        /// <summary>
        /// keep track where on the stack, the effect was pushed
        /// we do this only for top level effects
        /// </summary>
        /// <param name="stackDepth"></param>
        internal void BeginTopLevelBitmapEffect(int stackDepth)
        {
            BitmapEffectStackDepth = stackDepth;
        }

        /// <summary>
        /// Reset the stack depth
        /// </summary>
        internal void EndTopLevelBitmapEffect()
        {
            BitmapEffectStackDepth = 0;
        }

        /// <summary>
        /// Returns the size of the renderdata
        /// </summary>
        public int DataSize
        {
            get
            {
                return _curOffset;
            }
        }

        #endregion IDrawingContent

        #region DUCE

        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire())
            {
                // AddRef'ing or Releasing the renderdata itself doesn't propgate through the dependents,
                // unless our ref goes from or to 0.  This is why we have this if statement guarding
                // the inner loop.
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, DUCE.ResourceType.TYPE_RENDERDATA))
                {
                    // First we AddRefOnChannel each of the dependent resources,
                    // then we update our own.
                    for (int i = 0; i < _dependentResources.Count; i++)
                    {
                        DUCE.IResource resource = _dependentResources[i] as DUCE.IResource;

                        if (resource != null)
                        {
                            resource.AddRefOnChannel(channel);
                        }
                    }

                    UpdateResource(channel);
                }

                return _duceResource.GetHandle(channel);
            }
        }

        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire())
            {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                // AddRef'ing or Releasing the renderdata itself doesn't propgate through the dependents,
                // unless our ref goes from or to 0.  This is why we have this if statement guarding
                // the inner loop.
                if (_duceResource.ReleaseOnChannel(channel))
                {
                    for (int i = 0; i < _dependentResources.Count; i++)
                    {
                        DUCE.IResource resource = _dependentResources[i] as DUCE.IResource;

                        if (resource != null)
                        {
                            resource.ReleaseOnChannel(channel);
                        }
                    }
                }
            }
        }

        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            DUCE.ResourceHandle handle;

            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                // This method is a short cut and must only be called while the ref count
                // of this resource on this channel is non-zero.  Thus we assert that this
                // resource is already on this channel.
                Debug.Assert(_duceResource.IsOnChannel(channel));

                handle = _duceResource.GetHandle(channel);
            }

            return handle;
        }

        int DUCE.IResource.GetChannelCount()
        {
            return _duceResource.GetChannelCount();
        }

        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            return _duceResource.GetChannel(index);
        }

        /// <summary>
        /// This is only implemented by Visual and Visual3D.
        /// </summary>
        void DUCE.IResource.RemoveChildFromParent(DUCE.IResource parent, DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is only implemented by Visual and Visual3D.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.Get3DHandle(DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }
        #endregion DUCE

        public uint AddDependentResource(Object o)
        {
            // Append the resource to the internal array.
            if (o == null)
            {
                return 0;
            }
            else
            {
                return (uint)(_dependentResources.Add(o) + 1);
            }
        }

        #region Internal Resource Methods

        private void UpdateResource(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));

            MarshalToDUCE(channel);
        }

        #endregion Internal Resource Methods

        #region Private Methods

        /// <summary>
        /// EnsureBuffer - this method ensures that the capacity is at least equal to cbRequiredSize.
        /// </summary>
        /// <param name="cbRequiredSize"> int - the new minimum size required.  Must be >= 0. </param>
        private void EnsureBuffer(int cbRequiredSize)
        {
            Debug.Assert(cbRequiredSize >= 0);

            // If we don't have a buffer, this is easy: we simply allocate a new one of the appropriate size.
            if (_buffer == null)
            {
                _buffer = new byte[cbRequiredSize];
            }
            else
            {
                // For efficiency, we shouldn't have been called if there's already enough room
                Debug.Assert(_buffer.Length < cbRequiredSize);

                // The new size will be 1.5 x the previous size, or the min size required (whichever is larger)
                // We perform the 1.5x math by taking 2x of the length and subtracting 0.5x the length because
                // the 2x and 0.5x can be figured via shifts.  This is ~2x faster than performing the floating
                // point math.
                int newSize = Math.Max((_buffer.Length << 1) - (_buffer.Length >> 1), cbRequiredSize);

                // This is a double-check against the math above - if newSize isn't at least cbRequiredSize,
                // this growth function is broken.
                Debug.Assert(newSize >= cbRequiredSize);

                byte[] _newBuffer = new byte[newSize];

                _buffer.CopyTo(_newBuffer, 0);

                _buffer = _newBuffer;
            }
        }

        /// <summary>
        /// DependentLookup - given an index into the dependent resource array,
        /// we return null if the index is 0, else we return the dependent at index - 1.
        /// </summary>
        /// <param name="index"> uint - 1-based index into the dependent array, 0 means "no lookup". </param>
        private object DependentLookup(uint index)
        {
            Debug.Assert(index <= (uint)Int32.MaxValue);

            if (index == 0)
            {
                return null;
            }

            Debug.Assert(_dependentResources.Count >= index);

            return _dependentResources[(int)index - 1];
        }

        #endregion Private Methods

        #region Private Fields

        // The buffer into which the renderdata is written
        private byte[] _buffer;

        // The offset of the beginning of the next record
        // We ensure that the types in our instruction structs are correctly aligned wrt. their
        // size for read/write access, assuming that the instruction struct sits at an 8-byte
        // boundary.  Thus _curOffset must always be at an 8-byte boundary to begin writing
        // an instruction.
        private int _curOffset;

        private int _bitmapEffectStackDepth;
        
        private FrugalStructList<Object> _dependentResources = new FrugalStructList<Object>();

        // DUCE resource
        private DUCE.MultiChannelResource _duceResource = new DUCE.MultiChannelResource();

        #endregion Private Fields
    }
}
