// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
//        using System.Threading; 
//        using System.Collections;
//        using System.Windows.Forms;
        using System.Drawing.Design;
        using System.ComponentModel;
//        using System.Drawing.Drawing2D;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
    #endregion using
        
    /// <summary>
    /// Summary description for MatchingInfo.
    /// Helper class for the matching result of Glyphs
    /// </summary>
     [SerializableAttribute]
    [Editor(typeof(MatchingInfoEditor), typeof(UITypeEditor))]
    public class MatchingInfo: IComparable//, ISerializable
    {
        #region Constants
            /// <summary>
            /// The histogram length
            /// </summary>
            public const int HistogramLength = 10;
        #endregion Constants

        #region Properties
            #region Private Properties
                private float[] _histogram = new float[HistogramLength];
                private float _error = 0f;
                private Rectangle _rectangle = new Rectangle();
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The matching error 
                /// </summary>
                public float Error
                {
                    get { return _error; }
                    set { _error = value; }
                }
                /// <summary>
                /// The diff histogram
                /// </summary>
                public float[] Histogram
                {
                    get { return _histogram; }
                    set { _histogram = value; }
                }
                /// <summary>
                /// The bounding box of the result match
                /// </summary>
                public Rectangle BoundingBox
                {
                    get { return _rectangle; }
                    set { _rectangle = value; }
                }
                /// <summary>
                /// Phony bool used in matching
                /// </summary>
                public bool Phony = false;
            #endregion Public Properties
        #endregion Properties

        #region IComparable Members
            /// <summary>
            /// Compares two MatchingInfo based on the error
            /// </summary>
            public int CompareTo(object obj)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("object obj");
                }

                MatchingInfo minf = obj as MatchingInfo;

                if (minf == null)
                {
                    throw new Exception("Can't compare different types");
                }

                if (Error < minf.Error)
                {
                    return -1;
                }
                else if (Error == minf.Error)
                {
                    return 0;
                }

                return 1;
            }
            /// <summary>
            /// Compare this matching info to another one
            /// </summary>
            /// <param name="obj">The MatchingInfo to compare against</param>
            /// <returns>true if MatchingInfo are the same, false otherwise</returns>
            public override bool Equals(object obj)
            {
                return this == (MatchingInfo)obj;
            }
            /// <summary>
            /// Get the HashCode for object 
            /// </summary>
            /// <returns>The associated HashCode</returns>
            public override int GetHashCode()
            {
                return this.Error.GetHashCode();
                //  return base.GetHashCode();
            }
            /// <summary>
            /// Compare two MatchingInfo object for Equality
            /// </summary>
            /// <param name="matchingInfo1">The first MatchingInfo</param>
            /// <param name="matchingInfo2">The seconf MatchingInfo</param>
            /// <returns>true if objects are equals, false otherwise</returns>
            static public bool operator == (MatchingInfo matchingInfo1, MatchingInfo matchingInfo2)
            {
                if (matchingInfo1 == null && matchingInfo2 == null)
                {
                    return true;
                }
                if (matchingInfo1 == null || matchingInfo2 == null)
                {
                    return false;
                }
                return matchingInfo1.Error == matchingInfo2.Error;
            }
            /// <summary>
            /// Compare two MatchingInfo object for Inequality
            /// </summary>
            /// <param name="matchingInfo1">The first MatchingInfo</param>
            /// <param name="matchingInfo2">The seconf MatchingInfo</param>
            /// <returns>true if objects are not equals, false if they are equal</returns>
            static public bool operator !=(MatchingInfo matchingInfo1, MatchingInfo matchingInfo2)
            {
                if (matchingInfo1 == null && matchingInfo2 == null)
                {
                    return false;
                }
                if (matchingInfo1 == null || matchingInfo2 == null)
                {
                    return true;
                }
                return matchingInfo1.Error != matchingInfo2.Error;
            }
            /// <summary>
            /// Determine if the first object is bigger than the second one 
            /// </summary>
            /// <param name="matchingInfo1">The first MatchingInfo</param>
            /// <param name="matchingInfo2">The seconf MatchingInfo</param>
            /// <returns>true if the first Matching info is bigger than the second one, false otherwise</returns>
            static public bool operator >(MatchingInfo matchingInfo1, MatchingInfo matchingInfo2)
            {
                if (matchingInfo1 == null && matchingInfo2 == null)
                {
                    return false;
                }
                if (matchingInfo1 == null || matchingInfo2 == null)
                {
                    if (matchingInfo1 == null)
                    {
                        return false;
                    }
                    return true;
                }
                return matchingInfo1.Error > matchingInfo2.Error;
            }
            /// <summary>
            /// Determine if the first object is smaller than the second one 
            /// </summary>
            /// <param name="matchingInfo1">The first MatchingInfo</param>
            /// <param name="matchingInfo2">The seconf MatchingInfo</param>
            /// <returns>true if the first Matching info is smaller than the second one, false otherwise</returns>
            static public bool operator <(MatchingInfo matchingInfo1, MatchingInfo matchingInfo2)
            {
                if (matchingInfo1 == null && matchingInfo2 == null)
                {
                    return false;
                }

                if (matchingInfo1 == null || matchingInfo2 == null)
                {
                    if (matchingInfo1 == null)
                    {
                        return true;
                    }

                    return false;
                }

                return matchingInfo1.Error < matchingInfo2.Error;
            }
        #endregion

/*
        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // TODO:  Add MatchingInfo.ISerializable.GetObjectData implementation
            }
        #endregion
*/
    }
}
