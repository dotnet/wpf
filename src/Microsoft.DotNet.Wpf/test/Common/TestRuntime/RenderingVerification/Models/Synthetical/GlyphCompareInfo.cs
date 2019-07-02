// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Collections;
        using System.Runtime.Serialization;
   #endregion using

    /// <summary>
    /// Summary description for GlyphCompareInfo.
    /// </summary>
    [SerializableAttribute]
    public class GlyphCompareInfo : ISerializable
    {
        #region Properties
            #region Private Properties
                private bool _edgesOnly = false;
                private int _maxMatch = 0;
                private double _errorMax = double.NaN;
                private ArrayList _matches = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// performs the matching by matching the edges of the text only (removes gradient and much of the color (fg/bg) effects)
                /// </summary>
                public bool EdgesOnly
                {
                    get { return _edgesOnly; }
                    set { _edgesOnly = value; }
                }
                /// <summary>
                /// The maximum error to still trigger detection
                /// This value is expressed in percentage and normalized (ie : expect value between 0.0 and 1.0 )
                /// </summary>
                public double ErrorMax
                {
                    get { return _errorMax; }
                    set
                    {
                        if (value < 0.0) { throw new ArgumentOutOfRangeException("ErrorMax", value, "Value must be between 0.0 (included) and 1.0 (excluded)"); }
                        if (value >= 1.0) { throw new ArgumentOutOfRangeException("ErrorMax", value, "Value must be between 0.0 (included) and 1.0 (excluded)"); }
                        _errorMax = value;
                    }
                }
                /// <summary>
                /// The maximum number of matching allowed 
                /// </summary>
                public int MaxMatch
                {
                    get { return _maxMatch; }
                    set { _maxMatch = value; }
                }
                /// <summary>
                /// The array of Matching Results
                /// </summary>
                public ArrayList Matches
                {
                    get { return _matches; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor
            /// </summary>
            public GlyphCompareInfo() 
            {
                _maxMatch = 1;
                _errorMax = 0.01;   // Default : 1 percent tolerance
                _edgesOnly = false; // by default check the whole glyph, not only the edges
                _matches = new ArrayList();

            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphCompareInfo(SerializationInfo info, StreamingContext context)
            {
                _edgesOnly = (bool)info.GetValue("EdgesOnly", typeof(bool));
                _maxMatch = (int)info.GetValue("MaxMatch",typeof(int));
                _errorMax = (double)info.GetValue("ErrorMax", typeof(double));
                _matches = new ArrayList();

            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            /// <summary>
            /// Serialization Method
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("EdgesOnly", EdgesOnly);
                info.AddValue("MaxMatch", MaxMatch);
                info.AddValue("ErrorMax", ErrorMax);
            }
        #endregion
    }
}
