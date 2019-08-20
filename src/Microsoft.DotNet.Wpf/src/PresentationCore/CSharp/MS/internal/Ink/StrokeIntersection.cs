// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.Ink;
using MS.Utility;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Globalization;

namespace System.Windows.Ink
{
    /// <summary>
    /// A helper struct that represents a fragment of a stroke spine.
    /// </summary>
    internal struct StrokeIntersection
    {
        #region Private statics
        private static StrokeIntersection s_empty = new StrokeIntersection(AfterLast, AfterLast, BeforeFirst, BeforeFirst);
        private static StrokeIntersection s_full = new StrokeIntersection(BeforeFirst, BeforeFirst, AfterLast, AfterLast);
        #endregion

        #region Public API

        /// <summary>
        /// BeforeFirst
        /// </summary>
        /// <value></value>
        internal static double BeforeFirst { get { return StrokeFIndices.BeforeFirst; } }

        /// <summary>
        /// AfterLast
        /// </summary>
        /// <value></value>
        internal static double AfterLast { get { return StrokeFIndices.AfterLast; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hitBegin"></param>
        /// <param name="inBegin"></param>
        /// <param name="inEnd"></param>
        /// <param name="hitEnd"></param>
        internal StrokeIntersection(double hitBegin, double inBegin, double inEnd, double hitEnd)
        {
            //ISSUE-2004/12/06-XiaoTu: should we validate the input?
            _hitSegment = new StrokeFIndices(hitBegin, hitEnd);
            _inSegment = new StrokeFIndices(inBegin, inEnd);
        }

        /// <summary>
        /// hitBeginFIndex
        /// </summary>
        /// <value></value>
        internal double HitBegin
        {
            set { _hitSegment.BeginFIndex = value; }
        }

        /// <summary>
        /// hitEndFIndex
        /// </summary>
        /// <value></value>
        internal double HitEnd
        {
            get { return _hitSegment.EndFIndex; }
            set { _hitSegment.EndFIndex = value; }
        }


        /// <summary>
        /// InBegin
        /// </summary>
        /// <value></value>
        internal double InBegin
        {
            get { return _inSegment.BeginFIndex; }
            set { _inSegment.BeginFIndex = value; }
        }

        /// <summary>
        /// InEnd
        /// </summary>
        /// <value></value>
        internal double InEnd
        {
            get { return _inSegment.EndFIndex; }
            set { _inSegment.EndFIndex = value; }
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            return "{"  + StrokeFIndices.GetStringRepresentation(_hitSegment.BeginFIndex) + ","
                        + StrokeFIndices.GetStringRepresentation(_inSegment.BeginFIndex)  + ","
                        + StrokeFIndices.GetStringRepresentation(_inSegment.EndFIndex)    + ","
                        + StrokeFIndices.GetStringRepresentation(_hitSegment.EndFIndex)   + "}";
        }


        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(Object obj)
        {
            // Check for null and compare run-time types
            if (obj == null || GetType() != obj.GetType())
                return false;
            return ((StrokeIntersection)obj == this);
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _hitSegment.GetHashCode() ^ _inSegment.GetHashCode();
        }


        /// <summary>
        /// operator ==
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(StrokeIntersection left, StrokeIntersection right)
        {
            return (left._hitSegment == right._hitSegment && left._inSegment == right._inSegment);
        }

        /// <summary>
        /// operator !=
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(StrokeIntersection left, StrokeIntersection right)
        {
            return !(left == right);
        }

        #endregion

        #region Internal API

        /// <summary>
        ///
        /// </summary>
        internal static StrokeIntersection Full { get { return s_full; } }

        /// <summary>
        ///
        /// </summary>
        internal bool IsEmpty { get { return _hitSegment.IsEmpty; } }


        /// <summary>
        ///
        /// </summary>
        internal StrokeFIndices HitSegment
        {
            get { return _hitSegment; }
        }

        /// <summary>
        ///
        /// </summary>
        internal StrokeFIndices InSegment
        {
            get { return _inSegment; }
        }

        #endregion

        #region Internal static methods

        /// <summary>
        /// Get the "in-segments" of the intersections.
        /// </summary>
        internal static StrokeFIndices[] GetInSegments(StrokeIntersection[] intersections)
        {
            System.Diagnostics.Debug.Assert(intersections != null);
            System.Diagnostics.Debug.Assert(intersections.Length > 0);

            List<StrokeFIndices> inFIndices = new List<StrokeFIndices>(intersections.Length);
            for (int j = 0; j < intersections.Length; j++)
            {
                System.Diagnostics.Debug.Assert(!intersections[j].IsEmpty);
                if (!intersections[j].InSegment.IsEmpty)
                {
                    if (inFIndices.Count > 0 &&
                        inFIndices[inFIndices.Count - 1].EndFIndex >=
                        intersections[j].InSegment.BeginFIndex)
                    {
                        //merge
                        StrokeFIndices sfiPrevious = inFIndices[inFIndices.Count - 1];
                        sfiPrevious.EndFIndex = intersections[j].InSegment.EndFIndex;
                        inFIndices[inFIndices.Count - 1] = sfiPrevious;
                    }
                    else
                    {
                        inFIndices.Add(intersections[j].InSegment);
                    }
                }
            }
            return inFIndices.ToArray();
        }

        /// <summary>
        /// Get the "hit-segments"
        /// </summary>
        internal static StrokeFIndices[] GetHitSegments(StrokeIntersection[] intersections)
        {
            System.Diagnostics.Debug.Assert(intersections != null);
            System.Diagnostics.Debug.Assert(intersections.Length > 0);

            List<StrokeFIndices> hitFIndices = new List<StrokeFIndices>(intersections.Length);
            for (int j = 0; j < intersections.Length; j++)
            {
                System.Diagnostics.Debug.Assert(!intersections[j].IsEmpty);
                if (!intersections[j].HitSegment.IsEmpty)
                {
                    if (hitFIndices.Count > 0 &&
                        hitFIndices[hitFIndices.Count - 1].EndFIndex >=
                        intersections[j].HitSegment.BeginFIndex)
                    {
                        //merge
                        StrokeFIndices sfiPrevious = hitFIndices[hitFIndices.Count - 1];
                        sfiPrevious.EndFIndex = intersections[j].HitSegment.EndFIndex;
                        hitFIndices[hitFIndices.Count - 1] = sfiPrevious;
                    }
                    else
                    {
                        hitFIndices.Add(intersections[j].HitSegment);
                    }
                }
            }
            return hitFIndices.ToArray();
        }

        #endregion

        #region  Fields

        private StrokeFIndices _hitSegment;
        private StrokeFIndices _inSegment;

        #endregion
    }
}
