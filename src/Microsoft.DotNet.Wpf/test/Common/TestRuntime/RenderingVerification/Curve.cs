// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region using
    using System;
    using System.IO;
    using System.Xml;
    using System.Drawing;
    using System.Collections;
    using System.Globalization;
    using System.Security.Permissions;
    #endregion using

    /// <summary>
    /// Summary description for Curve.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class Curve : ICloneable
    {
        #region Properties
            #region Private Properties
                private ArrayList _values = new ArrayList();
                private Hashtable _namedValues = new Hashtable();
                private CurveTolerance _curveTolerance = new CurveTolerance();
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the list of point (list of ControlPoint) defining the upper limit for the curve
                /// </summary>
                public CurveTolerance CurveTolerance
                {
                    get { return _curveTolerance; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("CurveTolerance", "A valid instance of the object must be passed in  null passed in.)"); }
                        _curveTolerance = value;
                    }
                }
                /// <summary>
                /// Get the values to be analyzed (List of double[])
                /// </summary>
                public ArrayList Values
                {
                    get { return _values; }
                }
                /// <summary>
                /// Get the named values to be analyzed
                /// </summary>
                public Hashtable NamedValues
                {
                    get { return _namedValues; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instanciate a curve object
            /// </summary>
            public Curve () {}
        #endregion Constructors

        #region Methods
            #region Public Methods
/*
                /// <summary>
                /// Add data to be analyzed
                /// </summary>
                /// <param name="data">The combined values of channels</param>
                public void AddData(double[] data)
                {
                    // Clone : we don't want to change the caller's value (arrays are passed by ref, not value)
                    data = (double[])data.Clone();
                    AddNamedData(data, "Error Histogram");
                }
*/
                /// <summary>
                /// Clear/Reset data to be analyzed
                /// </summary>
                public void ResetData()
                {
                    _namedValues.Clear();
                    _values.Clear();
                }
                /// <summary>
                /// Add data to be analyzed
                /// </summary>
                /// <param name="data">The combined values of channels</param>
                /// <param name="name">The name to associate the data with</param>
                public void AddNamedData(double[] data, string name)
                {
                    // Clone : we don't want to change the caller's value (arrays are passed by ref, not value)
                    data = (double[])data.Clone();
                    for (int j = 0; j < data.Length; j++)
                    {
                        // check validity of data passed in
                        if (data[j] < 0.0 || data[j] > 1.0) { throw new ArgumentOutOfRangeException("data", data[j], "All values must be normalized (between 0.0 and 1.0) -- entry '" + j + "' is not normalized"); }
                    }
                    _values.Add(data);

                    if(_namedValues.Contains(data) == false) { _namedValues.Add(data, name); }
                    else { _namedValues[data] = name; }
                }
                /// <summary>
                /// Check if the data passed in are without the Tolerance
                /// </summary>
                /// <param name="data">The data to be analyzed</param>
                /// <param name="verboseLogging">Set this to tru to get verbose information</param>
                /// <returns>true if the data is bounded by the Tolerance, false otherwise</returns>
                public bool TestValues(double[] data, bool verboseLogging)
                {
                    bool bPass = true;
                    if (data == null) { throw new ArgumentNullException("data", "value null be set to a valid instance of double[] (null passed in)"); }

                    AddNamedData(data, "Error Histogram");

                    double[] threshold = new double[data.Length];
                    for (int j = 0; j < data.Length; j++)
                    {
                        threshold[j] = 1.0; // double.MaxValue;
                    }

                    if (CurveTolerance.Entries == null || CurveTolerance.Entries.Count == 0)
                    {
                        // No Tolerance ( default every entry to 0 )
                        for (int t = 0; t < 255; t++)
                        {
                            threshold[t] = 0.0;
                        }
                    }
                    else
                    {
                        for (int t = 0; t < threshold.Length; t++)
                        {
                            threshold[t] = CurveTolerance.InterpolatedValue((byte)t);
                        }
                    }

                    double sentinel = 1.0;
                    for (int j = 0; j < data.Length; j++)
                    {
                        if (threshold[j] != sentinel && threshold[j] != 1.0) { sentinel = threshold[j]; }
                        threshold[j] = sentinel;

                        // we do not test on 0 - for UI usability
                        if (j > 0 && threshold[j] < data[j]) { bPass = false; }
                        if (verboseLogging) { Console.WriteLine(string.Format("{0,-6} {1,-6} {2,-8} {3,-5}", bPass, j, threshold[j], data[j], (threshold[j] >= data[j] ? "Pass" : "Fail"))); }
                    }
                    if (verboseLogging) { Console.WriteLine ("\nDiff Test: " + (bPass == true ? "Pass\n" : "Fail\n")); }

                    return bPass;
                }
/*
                /// <summary>
                /// Check if the data stored are without the Tolerance
                /// </summary>
                /// <param name="verboseLogging">Set this to tru to get verbose information</param>
                /// <returns>true if the data is bounded by the Tolerance, false otherwise</returns>
                public bool TestValues(bool verboseLogging)
                {
                    if (_values.Count == 0) { throw new RenderingVerificationException("No data found, use AddData before calling this API (or use the overloaded one)."); }
                    double[] data = (double[])_values[_values.Count - 1];

                    return TestValues(data, verboseLogging);
                }
*/
            #endregion Public Methods
        #endregion Methods

        #region ICloneable Members
            /// <summary>
            /// Return a deep copy of the curve object
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                Curve retVal = new Curve();
                retVal._values = (ArrayList) this._values.Clone();
                retVal._namedValues = (Hashtable)this._namedValues.Clone();
                retVal._curveTolerance = (CurveTolerance)this._curveTolerance.Clone();
                return retVal;
            }
        #endregion ICloneable Members
    }
}
