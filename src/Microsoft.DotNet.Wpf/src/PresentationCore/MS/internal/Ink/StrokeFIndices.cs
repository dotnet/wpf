// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using MS.Internal;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Globalization;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink
{
    #region StrokeFIndices

    /// <summary>
    /// A helper struct that represents a fragment of a stroke spine.
    /// </summary>
    internal struct StrokeFIndices : IEquatable<StrokeFIndices>
    {
        #region Private statics
        private static readonly StrokeFIndices s_empty = new StrokeFIndices(AfterLast, BeforeFirst);
        private static readonly StrokeFIndices s_full = new StrokeFIndices(BeforeFirst, AfterLast);
        #endregion

        #region Internal API

        /// <summary>
        /// BeforeFirst
        /// </summary>
        /// <value></value>
        internal static double BeforeFirst { get { return double.MinValue; } }

        /// <summary>
        /// AfterLast
        /// </summary>
        /// <value></value>
        internal static double AfterLast { get { return double.MaxValue; } }

        /// <summary>
        /// StrokeFIndices
        /// </summary>
        /// <param name="beginFIndex">beginFIndex</param>
        /// <param name="endFIndex">endFIndex</param>
        internal StrokeFIndices(double beginFIndex, double endFIndex)
        {
            _beginFIndex = beginFIndex;
            _endFIndex = endFIndex;
        }

        /// <summary>
        /// BeginFIndex
        /// </summary>
        /// <value></value>
        internal double BeginFIndex
        {
            get { return _beginFIndex; }
            set { _beginFIndex = value; }
        }

        /// <summary>
        /// EndFIndex
        /// </summary>
        /// <value></value>
        internal double EndFIndex
        {
            get { return _endFIndex; }
            set { _endFIndex = value;}
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            return "{" + GetStringRepresentation(_beginFIndex) + "," + GetStringRepresentation(_endFIndex) + "}";
		}

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="strokeFIndices"></param>
        /// <returns></returns>
        public bool Equals(StrokeFIndices strokeFIndices)
        {
            return (strokeFIndices == this);
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
            return ((StrokeFIndices)obj == this);
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _beginFIndex.GetHashCode() ^ _endFIndex.GetHashCode();
        }

        /// <summary>
        /// operator ==
        /// </summary>
        /// <param name="sfiLeft"></param>
        /// <param name="sfiRight"></param>
        /// <returns></returns>
        public static bool operator ==(StrokeFIndices sfiLeft, StrokeFIndices sfiRight)
        {
            return (DoubleUtil.AreClose(sfiLeft._beginFIndex, sfiRight._beginFIndex)
                    && DoubleUtil.AreClose(sfiLeft._endFIndex, sfiRight._endFIndex));
        }

        /// <summary>
        /// operator !=
        /// </summary>
        /// <param name="sfiLeft"></param>
        /// <param name="sfiRight"></param>
        /// <returns></returns>
        public static bool operator !=(StrokeFIndices sfiLeft, StrokeFIndices sfiRight)
        {
            return !(sfiLeft == sfiRight);
        }

        internal static string GetStringRepresentation(double fIndex)
        {
            if (DoubleUtil.AreClose(fIndex, StrokeFIndices.BeforeFirst))
            {
                return "BeforeFirst";
            }
            if (DoubleUtil.AreClose(fIndex, StrokeFIndices.AfterLast))
            {
                return "AfterLast";
            }
            return fIndex.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///
        /// </summary>
        internal static StrokeFIndices Empty { get { return s_empty; } }

        /// <summary>
        ///
        /// </summary>
        internal static StrokeFIndices Full { get { return s_full; } }

        /// <summary>
        ///
        /// </summary>
        internal bool IsEmpty { get { return DoubleUtil.GreaterThanOrClose(_beginFIndex, _endFIndex); } }

        /// <summary>
        ///
        /// </summary>
        internal bool IsFull { get { return ((DoubleUtil.AreClose(_beginFIndex, BeforeFirst)) && (DoubleUtil.AreClose(_endFIndex,AfterLast))); } }


#if DEBUG
        /// <summary>
        ///
        /// </summary>
        private bool IsValid { get { return !double.IsNaN(_beginFIndex) && !double.IsNaN(_endFIndex) && _beginFIndex < _endFIndex; } }

#endif

        /// <summary>
        /// Compare StrokeFIndices based on the BeinFIndex
        /// </summary>
        /// <param name="fIndices"></param>
        /// <returns></returns>
        internal int CompareTo(StrokeFIndices fIndices)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(!double.IsNaN(_beginFIndex) && !double.IsNaN(_endFIndex) && DoubleUtil.LessThan(_beginFIndex, _endFIndex));
#endif
            if (DoubleUtil.AreClose(BeginFIndex, fIndices.BeginFIndex))
            {
                return 0;
            }
            else if (DoubleUtil.GreaterThan(BeginFIndex, fIndices.BeginFIndex))
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region  Fields

        private double _beginFIndex;
        private double _endFIndex;

        #endregion
    }

    #endregion
}
