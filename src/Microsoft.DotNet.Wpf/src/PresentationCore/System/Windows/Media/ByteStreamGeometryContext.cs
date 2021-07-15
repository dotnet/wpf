// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This class is used by the StreamGeometry class to generate an inlined,
// flattened geometry stream.
//

using MS.Utility;
using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Security;

namespace System.Windows.Media
{
    /// <summary>
    ///     ByteStreamGeometryContext
    /// </summary>
    internal class ByteStreamGeometryContext : CapacityStreamGeometryContext
    {
        #region Constructors

        /// <summary>
        /// Creates a geometry stream context.
        /// </summary>
        internal ByteStreamGeometryContext()
        {
            // For now, we just write this into the stream.  We'll update its fields as we go.
            MIL_PATHGEOMETRY tempPath = new MIL_PATHGEOMETRY();

            unsafe
            {
                AppendData((byte*)&tempPath, sizeof(MIL_PATHGEOMETRY));

                // Initialize the size to include the MIL_PATHGEOMETRY itself
                // All other fields are intentionally left as 0;
                _currentPathGeometryData.Size = (uint)sizeof(MIL_PATHGEOMETRY);
            }
        }

        #endregion Constructors
        
        #region Public Methods

        /// <summary>
        /// Closes the StreamContext and flushes the content.
        /// Afterwards the StreamContext can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        public override void Close()
        {
            VerifyApi();
            ((IDisposable)this).Dispose();
        }

        /// <summary>
        /// BeginFigure - Start a new figure.
        /// </summary>
        public override void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
        {
            VerifyApi();

            // Don't forget to close out the previous segment/figure
            FinishFigure();

            // Remember the location - we set this only after successful allocation in case it throws
            // and we're re-entered.
            int oldOffset = _currOffset;

            MIL_PATHFIGURE tempFigure;

            unsafe
            {
                AppendData((byte*)&tempFigure, sizeof(MIL_PATHFIGURE));
            }

            _currentPathFigureDataOffset = oldOffset;
            _currentPathFigureData.StartPoint = startPoint;
            _currentPathFigureData.Flags |= isFilled ? MilPathFigureFlags.IsFillable : 0;
            _currentPathFigureData.Flags |= isClosed ? MilPathFigureFlags.IsClosed : 0;
            _currentPathFigureData.BackSize = _lastFigureSize;
            _currentPathFigureData.Size = (UInt32)(_currOffset - _currentPathFigureDataOffset);
        }

        /// <summary>
        /// LineTo - append a LineTo to the current figure.
        /// </summary>
        public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            unsafe
            {
                Point* scratchForLine = stackalloc Point[1];
                scratchForLine[0] = point;
                GenericPolyTo(scratchForLine,
                              1 /* count */,
                              isStroked,
                              isSmoothJoin,
                              false /* does not have curves */,
                              MIL_SEGMENT_TYPE.MilSegmentPolyLine);
            }
        }

        /// <summary>
        /// QuadraticBezierTo - append a QuadraticBezierTo to the current figure.
        /// </summary>
        public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            unsafe
            {
                Point* scratchForQuadraticBezier = stackalloc Point[2];
                scratchForQuadraticBezier[0] = point1;
                scratchForQuadraticBezier[1] = point2;
                GenericPolyTo(scratchForQuadraticBezier,
                              2 /* count */,
                              isStroked,
                              isSmoothJoin,
                              true /* has curves */,
                              MIL_SEGMENT_TYPE.MilSegmentPolyQuadraticBezier);
            }
        }

        /// <summary>
        /// BezierTo - apply a BezierTo to the current figure.
        /// </summary>
        public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            unsafe
            {
                Point* scratchForBezier = stackalloc Point[3];
                scratchForBezier[0] = point1;
                scratchForBezier[1] = point2;
                scratchForBezier[2] = point3;
                GenericPolyTo(scratchForBezier,
                              3 /* count */,
                              isStroked,
                              isSmoothJoin,
                              true /* has curves */,
                              MIL_SEGMENT_TYPE.MilSegmentPolyBezier);
            }
        }

        /// <summary>
        /// PolyLineTo - append a PolyLineTo to the current figure.
        /// </summary>
        public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            GenericPolyTo(points, 
                          isStroked, 
                          isSmoothJoin, 
                          false /* does not have curves */,
                          1 /* pointCountMultiple */,
                          MIL_SEGMENT_TYPE.MilSegmentPolyLine);
        }

        /// <summary>
        /// PolyQuadraticBezierTo - append a PolyQuadraticBezierTo to the current figure.
        /// </summary>
        public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            GenericPolyTo(points, 
                          isStroked, 
                          isSmoothJoin, 
                          true /* has curves */,
                          2 /* pointCountMultiple */,
                          MIL_SEGMENT_TYPE.MilSegmentPolyQuadraticBezier);
        }

        /// <summary>
        /// PolyBezierTo - append a PolyBezierTo to the current figure.
        /// </summary>
        public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            GenericPolyTo(points, 
                          isStroked, 
                          isSmoothJoin, 
                          true /* has curves */,
                          3 /* pointCountMultiple */,
                          MIL_SEGMENT_TYPE.MilSegmentPolyBezier);
        }

        /// <summary>
        /// ArcTo - append an ArcTo to the current figure.
        /// </summary>
        public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
        {
            VerifyApi();

            if (_currentPathFigureDataOffset == -1)
            {
                throw new InvalidOperationException(SR.Get(SRID.StreamGeometry_NeedBeginFigure));
            }

            FinishSegment();

            MIL_SEGMENT_ARC arcToSegment = new MIL_SEGMENT_ARC();
            arcToSegment.Type = MIL_SEGMENT_TYPE.MilSegmentArc;

            arcToSegment.Flags |= isStroked ? 0 : MILCoreSegFlags.SegIsAGap;
            arcToSegment.Flags |= isSmoothJoin ? MILCoreSegFlags.SegSmoothJoin : 0;
            arcToSegment.Flags |= MILCoreSegFlags.SegIsCurved;
            arcToSegment.BackSize = _lastSegmentSize;

            arcToSegment.Point = point;
            arcToSegment.Size = size;
            arcToSegment.XRotation = rotationAngle;
            arcToSegment.LargeArc = (uint)(isLargeArc ? 1 : 0);
            arcToSegment.Sweep = (uint)(sweepDirection == SweepDirection.Clockwise ? 1 : 0);

            int offsetToArcToSegment = _currOffset;

            unsafe
            {
                AppendData((byte*)(&arcToSegment), sizeof(MIL_SEGMENT_ARC));
                _lastSegmentSize = (UInt32)sizeof(MIL_SEGMENT_ARC);
            }

            // Update the current path figure data
            _currentPathFigureData.Flags |= isStroked ? 0 : MilPathFigureFlags.HasGaps;

            _currentPathFigureData.Flags |= MilPathFigureFlags.HasCurves;
            
            _currentPathFigureData.Count++;

            // Always keep the OffsetToLastSegment and Size accurate
            _currentPathFigureData.Size = (UInt32)(_currOffset - _currentPathFigureDataOffset);

            _currentPathFigureData.OffsetToLastSegment = 
                (UInt32)(offsetToArcToSegment - _currentPathFigureDataOffset);
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// GetData - Retrieves the data stream built by this Context.
        /// </summary>
        internal byte[] GetData()
        {
            ShrinkToFit();

            return _chunkList[0];
        }

        override internal void SetClosedState(bool isClosed)
        {
            if (_currentPathFigureDataOffset == -1)
            {
                throw new InvalidOperationException(SR.Get(SRID.StreamGeometry_NeedBeginFigure));
            }

            // Clear out the IsClosed flag, then set it as appropriate.
            _currentPathFigureData.Flags &= ~MilPathFigureFlags.IsClosed;
            _currentPathFigureData.Flags |= isClosed ? MilPathFigureFlags.IsClosed : 0;
        }
        
        #endregion Internal Methods

        #region Private Methods
        
        /// <summary>
        /// This verifies that the API can be called at this time. 
        /// </summary>
        private void VerifyApi()
        {
            VerifyAccess();

            if (_disposed)
            {
                throw new ObjectDisposedException("ByteStreamGeometryContext");
            }
        }

        /// <summary>
        /// CloseCore - This method is implemented by derived classes to hand off the content 
        /// to its eventual destination.
        /// </summary>
        protected virtual void CloseCore(byte[] geometryData) {}

        /// <summary>
        /// This is the same as the Close call:
        /// Closes the Context and flushes the content.
        /// Afterwards the Context can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        internal override void DisposeCore()
        {
            if (!_disposed)
            {
                FinishFigure();

                unsafe
                {
                    // We have to have at least this much data already in the stream
                    checked
                    {
                        Debug.Assert(sizeof(MIL_PATHGEOMETRY) <= _currOffset);
                        Debug.Assert(_currentPathGeometryData.Size == (uint)_currOffset);
                    }

                    fixed (MIL_PATHGEOMETRY* pCurrentPathGeometryData = &_currentPathGeometryData)
                    {
                        OverwriteData((byte *)pCurrentPathGeometryData, 0, sizeof(MIL_PATHGEOMETRY));
                    }
                }

                ShrinkToFit();

                CloseCore(_chunkList[0]);

                _disposed = true;
            }
        }

/// <summary>
        /// ReadData - reads data from a specified location in the buffer
        /// </summary>
        /// <param name="pbData">
        ///   byte* pointing to at least cbDataSize bytes into which will be copied the desired data
        /// </param>
        /// <param name="bufferOffset"> int - the offset, in bytes, of the requested data. Must be >= 0. </param>
        /// <param name="cbDataSize"> int - the size, in bytes, of the requested data. Must be >= 0. </param>
        private unsafe void ReadData(byte* pbData,
                                      int bufferOffset,
                                      int cbDataSize)
        {
            Invariant.Assert(cbDataSize >= 0);
            Invariant.Assert(bufferOffset >= 0);

            //
            // Since this only ever gets called to read the
            // whole buffer, we could do this entirely using safe code.
            //

            checked
            {
                Invariant.Assert(_currOffset >= bufferOffset+cbDataSize);
            }

            ReadWriteData(true /* reading */, pbData, cbDataSize, 0, ref bufferOffset);
        }
        
        /// <summary>
        /// OverwriteData - overwrite data in the buffer.
        /// </summary>
        /// <param name="pbData">
        ///   byte* pointing to at least cbDataSize bytes which will be copied to the stream.
        /// </param>
        /// <param name="bufferOffset"> int - the offset, in bytes, at which the data should be writen. Must be >= 0. </param>
        /// <param name="cbDataSize"> int - the size, in bytes, of pbData. Must be >= 0. </param>
        private unsafe void OverwriteData(byte* pbData,
                                      int bufferOffset,
                                      int cbDataSize)
        {
            Invariant.Assert(cbDataSize >= 0);

            checked
            {
                int newOffset = bufferOffset + cbDataSize;

                Invariant.Assert(newOffset <= _currOffset);
            }

            ReadWriteData(false /* writing */, pbData, cbDataSize, 0, ref bufferOffset);
        }
        
        /// <summary>
        /// AppendData - append data to the buffer.
        /// </summary>
        /// <param name="pbData">
        ///   byte* pointing to at least cbDataSize bytes which will be copied to the stream.
        /// </param>
        /// <param name="cbDataSize"> int - the size, in bytes, of pbData. Must be >= 0. </param>
        private unsafe void AppendData(byte* pbData,
                                      int cbDataSize)
        {
            Invariant.Assert(cbDataSize >= 0);

            int newOffset;

            checked
            {
                newOffset = _currOffset + cbDataSize;
            }

            if (_chunkList.Count == 0)
            {
                byte[] chunk = ByteStreamGeometryContext.AcquireChunkFromPool();
                _chunkList.Add(chunk);
            }

            ReadWriteData(false /* writing */, pbData, cbDataSize, _chunkList.Count-1, ref _currChunkOffset);

            _currOffset = newOffset;
        }
        
        /// <summary>
        /// ShrinkToFit - Shrink the data to fit in exactly one chunk
        /// </summary>
        internal void ShrinkToFit()
        {
            Debug.Assert(_chunkList.Count != 0);

            if (_chunkList.Count > 1 ||
                _chunkList[0].Length != _currOffset)
            {
                byte [] buffer = new byte[_currOffset];

                unsafe
                {
                    fixed (byte *pbData = buffer)
                    {
                        ReadData(pbData, 0, _currOffset);
                    }
                }

                ByteStreamGeometryContext.ReturnChunkToPool(_chunkList[0]);

                // The common case is a single chunk in a SingleItemList held by the FrugalStructList.
                // Avoid tearing down and recreating the SingleItemList by updating the lone element in-place,
                // especially since ShrinkToFit is called from DisposeCore when this object is about to die.
                if (_chunkList.Count == 1)
                {
                    _chunkList[0] = buffer;
                }
                else
                {
                    _chunkList = new FrugalStructList<byte[]>();
                    _chunkList.Add(buffer);
                }
            }
        }
    
        /// <summary>
        /// ReadWriteData - read from/write to buffer.
        /// </summary>
        /// <param name="reading"> bool - is the buffer read from or written to?</param>
        /// <param name="pbData">
        ///   byte* pointing to at least cbDataSize bytes which will be copied to/from the stream.
        /// </param>
        /// <param name="cbDataSize"> int - the size, in bytes, of pbData. Must be >= 0. </param>
        /// <param name="currentChunk"> the current chunk to start writing to/reading from </param>
        /// <param name="bufferOffset"> in/out: the current position in the current chunk. </param> 
        private unsafe void ReadWriteData(bool reading,
                                          byte* pbData,
                                          int cbDataSize,
                                          int currentChunk,
                                          ref int bufferOffset)
        {
            Invariant.Assert(cbDataSize >= 0);

            // Skip past irrelevant chunks
            while (bufferOffset > _chunkList[currentChunk].Length)
            {
                bufferOffset -= _chunkList[currentChunk].Length;
                currentChunk++;
            }

            // Arithmetic should be checked by the caller (AppendData or OverwriteData)

            while (cbDataSize > 0)
            {
                int cbDataForThisChunk = Math.Min(cbDataSize,
                    _chunkList[currentChunk].Length - bufferOffset);

                if (cbDataForThisChunk > 0)
                {
                    // At this point, _buffer must be non-null and
                    // _buffer.Length must be >= newOffset
                    Invariant.Assert((_chunkList[currentChunk] != null) 
                        && (_chunkList[currentChunk].Length >= bufferOffset + cbDataForThisChunk));

                    // Also, because pinning a 0-length buffer fails, we assert this too.
                    Invariant.Assert(_chunkList[currentChunk].Length > 0);

                    if (reading)
                    {
                        Marshal.Copy(_chunkList[currentChunk], bufferOffset, (IntPtr)pbData, cbDataForThisChunk);
                    }
                    else
                    {
                        Marshal.Copy((IntPtr)pbData, _chunkList[currentChunk], bufferOffset, cbDataForThisChunk);
                    }

                    cbDataSize -= cbDataForThisChunk;
                    pbData += cbDataForThisChunk;
                    bufferOffset += cbDataForThisChunk;
                }

                if (cbDataSize > 0)
                {
                    checked {currentChunk++;}

                    if (_chunkList.Count == currentChunk)
                    {
                        Invariant.Assert(!reading);

                        // Exponential growth early on. Later, linear growth.
                        int newChunkSize = Math.Min(2*_chunkList[_chunkList.Count-1].Length, c_maxChunkSize);

                        _chunkList.Add(new byte[newChunkSize]);
                    }

                    bufferOffset = 0;
                }
            }
        }

        /// <summary>
        /// FinishFigure - called to completed any outstanding Figure which may be present.
        /// If there is one, we write its data into the stream at the appropriate offset
        /// and update the path's flags/size/figure count/etc based on this Figure.
        /// After this call, a new figure needs to be started for any segment-building APIs
        /// to be legal.
        /// </summary>
        private void FinishFigure()
        {
            if (_currentPathFigureDataOffset != -1)
            {
                FinishSegment();
                
                unsafe
                {
                    // We have to have at least this much data already in the stream
                    checked
                    {
                        Debug.Assert(_currentPathFigureDataOffset + sizeof(MIL_PATHFIGURE) <= _currOffset);
                    }

                    fixed (MIL_PATHFIGURE* pCurrentPathFigureData = &_currentPathFigureData)
                    {
                        OverwriteData((byte *)pCurrentPathFigureData, _currentPathFigureDataOffset, sizeof(MIL_PATHFIGURE));
                    }
                }

                _currentPathGeometryData.Flags |= ((_currentPathFigureData.Flags & MilPathFigureFlags.HasCurves) != 0) ? MilPathGeometryFlags.HasCurves : 0;
                _currentPathGeometryData.Flags |= ((_currentPathFigureData.Flags & MilPathFigureFlags.HasGaps) != 0) ? MilPathGeometryFlags.HasGaps : 0;
                _currentPathGeometryData.Flags |= ((_currentPathFigureData.Flags & MilPathFigureFlags.IsFillable) == 0) ? MilPathGeometryFlags.HasHollows : 0;
                _currentPathGeometryData.FigureCount++;
                _currentPathGeometryData.Size = (UInt32)(_currOffset);

                _lastFigureSize = _currentPathFigureData.Size;

                // Initialize _currentPathFigureData (this really just 0's out the memory)
                _currentPathFigureDataOffset = -1;
                _currentPathFigureData = new MIL_PATHFIGURE();

                // We must also clear _lastSegmentSize, since there is now no "last segment"
                _lastSegmentSize = 0;
            }
        }

        /// <summary>
        /// FinishSegment - called to completed any outstanding Segment which may be present.
        /// If there is one, we write its data into the stream at the appropriate offset
        /// and update the figure's flags/size/segment count/etc based on this Segment.
        /// </summary>
        private void FinishSegment()
        {
            if (_currentPolySegmentDataOffset != -1)
            {
                unsafe
                {
                    // We have to have at least this much data already in the stream
                    checked
                    {
                        Debug.Assert(_currentPolySegmentDataOffset + sizeof(MIL_SEGMENT_POLY) <= _currOffset);
                    }

                    fixed (MIL_SEGMENT_POLY* pCurrentPolySegmentData = &_currentPolySegmentData)
                    {
                        OverwriteData((byte *)pCurrentPolySegmentData, _currentPolySegmentDataOffset, sizeof(MIL_SEGMENT_POLY));
                    }

                    _lastSegmentSize = (UInt32)(sizeof(MIL_SEGMENT_POLY) + (sizeof(Point) * _currentPolySegmentData.Count));
                }

                // Update the current path figure data
                if ((_currentPolySegmentData.Flags & MILCoreSegFlags.SegIsAGap) != 0)
                {
                    _currentPathFigureData.Flags |= MilPathFigureFlags.HasGaps;
                }

                if ((_currentPolySegmentData.Flags & MILCoreSegFlags.SegIsCurved) != 0)
                {
                    _currentPathFigureData.Flags |= MilPathFigureFlags.HasCurves;
                }

                _currentPathFigureData.Count++;

                // Always keep the OffsetToLastSegment and Size accurate
                _currentPathFigureData.Size = (UInt32)(_currOffset - _currentPathFigureDataOffset);

                _currentPathFigureData.OffsetToLastSegment = 
                    (UInt32)(_currentPolySegmentDataOffset - _currentPathFigureDataOffset);

                // Initialize _currentPolySegmentData (this really just 0's out the memory)
                _currentPolySegmentDataOffset = -1;
                _currentPolySegmentData = new MIL_SEGMENT_POLY();
            }
        }

        private void GenericPolyTo(IList<Point> points,
                                   bool isStroked, 
                                   bool isSmoothJoin,
                                   bool hasCurves,
                                   int pointCountMultiple,
                                   MIL_SEGMENT_TYPE segmentType)
        {
            if (_currentPathFigureDataOffset == -1)
            {
                throw new InvalidOperationException(SR.Get(SRID.StreamGeometry_NeedBeginFigure));
            }

            if (points == null)
            {
                return;
            }

            int count = points.Count;
            count -= count % pointCountMultiple;

            if (count <= 0)
            {
                return;
            }

            GenericPolyToHelper(isStroked, isSmoothJoin, hasCurves, segmentType);

            for (int i = 0; i < count; i++)
            {
                Point p = points[i];

                unsafe
                {
                    AppendData((byte*)&p, sizeof(Point));
                }

                _currentPolySegmentData.Count++;
            }
        }

        unsafe private void GenericPolyTo(Point* points,
                                   int count,
                                   bool isStroked,
                                   bool isSmoothJoin,
                                   bool hasCurves,
                                   MIL_SEGMENT_TYPE segmentType)
        {
            Debug.Assert(points != null);
            Debug.Assert(count > 0);

            if (_currentPathFigureDataOffset == -1)
            {
                throw new InvalidOperationException(SR.Get(SRID.StreamGeometry_NeedBeginFigure));
            }

            GenericPolyToHelper(isStroked, isSmoothJoin, hasCurves, segmentType);

            AppendData((byte*)points, sizeof(Point) * count);
            _currentPolySegmentData.Count += (uint)count;
        }

        private void GenericPolyToHelper(bool isStroked, bool isSmoothJoin, bool hasCurves, MIL_SEGMENT_TYPE segmentType)
        {
            // Do we need to finish the old segment?
            // Yes, if there is an old segment and if its type or flags are different from 
            // the new segment.
            if ((_currentPolySegmentDataOffset != -1) &&
                 (
                   (_currentPolySegmentData.Type != segmentType) ||
                   (((_currentPolySegmentData.Flags & MILCoreSegFlags.SegIsAGap) == 0) != isStroked) ||
                   (((_currentPolySegmentData.Flags & MILCoreSegFlags.SegSmoothJoin) != 0) != isSmoothJoin)
                 )
               )
            {
                FinishSegment();
            }

            // Do we need to start a new segment?
            if (_currentPolySegmentDataOffset == -1)
            {
                MIL_SEGMENT_POLY tempSegment;
                int oldOffset = _currOffset;

                unsafe
                {
                    AppendData((byte*)&tempSegment, sizeof(MIL_SEGMENT_POLY));
                }

                _currentPolySegmentDataOffset = oldOffset;
                _currentPolySegmentData.Type = segmentType;
                _currentPolySegmentData.Flags |= isStroked ? 0 : MILCoreSegFlags.SegIsAGap;
                _currentPolySegmentData.Flags |= hasCurves ? MILCoreSegFlags.SegIsCurved : 0;
                _currentPolySegmentData.Flags |= isSmoothJoin ? MILCoreSegFlags.SegSmoothJoin : 0;
                _currentPolySegmentData.BackSize = _lastSegmentSize;
            }

            // Assert that everything is ready to go
            Debug.Assert((_currentPolySegmentDataOffset != -1) &&
                         (_currentPolySegmentData.Type == segmentType) &&
                         (((_currentPolySegmentData.Flags & MILCoreSegFlags.SegIsAGap) == 0) == isStroked) &&
                         (((_currentPolySegmentData.Flags & MILCoreSegFlags.SegSmoothJoin) != 0) == isSmoothJoin));
}

        /// <summary>
        /// Grab a pre-allocated chunk (default-sized byte array) from the pool.
        /// </summary>
        /// <returns>The chunk, either from the pool or freshly allocated</returns>
        private static byte[] AcquireChunkFromPool()
        {
            byte[] chunk = ByteStreamGeometryContext._pooledChunk;
            if (chunk == null)
            {
                // Pooled chunk not available
                return new byte[c_defaultChunkSize];
            }

            // Indicate that the pooled chunk is in use
            ByteStreamGeometryContext._pooledChunk = null;
            return chunk;
        }

        /// <summary>
        /// Return a chunk back to the pool.
        /// </summary>
        /// <param name="chunk">The chunk to return. After this method returns, the chunk is owned by the pool
        /// and may not be used again by the caller.</param>
        private static void ReturnChunkToPool(byte[] chunk)
        {
            if (chunk.Length == c_defaultChunkSize)
            {
                ByteStreamGeometryContext._pooledChunk = chunk;
            }
        }

        #endregion Private Methods
        
        #region Fields

        private bool _disposed;
        private int _currChunkOffset;
        FrugalStructList<byte []> _chunkList;
        private int _currOffset;
        private MIL_PATHGEOMETRY _currentPathGeometryData;
        private MIL_PATHFIGURE _currentPathFigureData;
        private int _currentPathFigureDataOffset = -1;
        private MIL_SEGMENT_POLY _currentPolySegmentData;
        private int _currentPolySegmentDataOffset = -1;
        private UInt32 _lastSegmentSize = 0;
        private UInt32 _lastFigureSize = 0;

        private const int c_defaultChunkSize = 2*1024;
        private const int c_maxChunkSize = 1024*1024;

        [ThreadStatic]
        static byte[] _pooledChunk;

        #endregion Fields
    }
}
