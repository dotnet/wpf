// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;

namespace Microsoft.Test.RenderingVerification.Filters
{
    /// <summary>
    /// Type of fading to use in the OpacityPoints Filter
    /// </summary>
    public enum FadingType
    {
        /// <summary>
        /// Fade from opaque to translucent
        /// </summary>
        OpaqueToTranslucent = 1,
        /// <summary>
        /// Fade from translucent to opaque
        /// </summary>
        TranslucentToOpaque = 2
    }

    /// <summary>
    /// Summary description for FadeToBackFilter.
    /// </summary>
    public class OpacityPointsFilter : Filter
    {
        #region Constants
            private const string RADIUS = "Radius";
            private const string POINTS = "Points";
            private const string FADINGTYPE = "FadingType";
        #endregion Constants

        #region Properties
            #region Private properties
                private static string _description = string.Empty;
            #endregion Private properties
            #region Public properties
                /// <summary>
                /// Radius of the fading (varies between 0.0 and double.MaxValue)
                /// </summary>
                public double Radius
                {
                    get
                    {
                        return (double)this[RADIUS].Parameter;
                    }
                    set
                    {
                        if (value > 0.0 && value <= double.MaxValue)
                        {
                            this[RADIUS].Parameter = value;
                            return;
                        }
                        throw new ArgumentOutOfRangeException ("Radius", value, "The value passed in is invalid, should be between 0.0 and double.MaxValue");
                    }
                }
                /// <summary>
                /// The points where the fading start
                /// </summary>
                /// <value></value>
                public System.Drawing.Point[] Points
                {
                    get
                    {
                        return (System.Drawing.Point[])this[POINTS].Parameter;
                    }
                    set
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException ("Points", "Must assign a valid array of Points (null passed in)");
                        }
                        this[POINTS].Parameter = value;
                    }
                }
                /// <summary>
                /// The type of fading expected
                /// </summary>
                /// <value></value>
                public FadingType FadingDirection
                {
                    get 
                    {
                        return (FadingType)this[FADINGTYPE].Parameter;
                    }
                    set 
                    {
                        if (value != FadingType.OpaqueToTranslucent && value != FadingType.TranslucentToOpaque)
                        {
                            throw new ArgumentOutOfRangeException("FadingDirection", value, "The providedType is not a valid FadingType");
                        }
                        this[FADINGTYPE].Parameter = value;
                    }
                }
                /// <summary>
                /// Self description of the filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get { return _description; }
                }
                //TODO : Gaussian fading
            #endregion Public properties
        #endregion properties

        #region Constructor
            /// <summary>
            /// Create a new instance of the OpacityPoints filter
            /// </summary>
            public OpacityPointsFilter ()
            {
                FilterParameter radius = new FilterParameter (RADIUS, "Radius of the Fading", (double)100.0);
                FilterParameter points = new FilterParameter (POINTS, "Radius of the Glow", (System.Drawing.Point[]) new System.Drawing.Point[0]);
                FilterParameter fadingType = new FilterParameter(FADINGTYPE, "The Type of Fading", (FadingType)FadingType.OpaqueToTranslucent);

                AddParameter(fadingType);
                AddParameter (points);
                AddParameter (radius);
            }
            static OpacityPointsFilter()
            {
                _description = "Descrease Alpha linearly to create a fading effect";
            }
        #endregion Constructor

        #region Methods
            #region Private Methods
                private double[] ComputeFading()
                {
                    double[] retVal = new double[(int)Radius];
                    for(int index = 0; index < Radius; index++)
                    {
                        retVal[index] = (Radius - index) / Radius;
                    }
                    return retVal;
                }
                private double ComputeDistance(int x, int y, System.Drawing.Point point)
                {
                    return Math.Sqrt ((x - point.X) * (x - point.X) + (y - point.Y) * (y - point.Y));
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter (IImageAdapter source)
                {
                    // Check params
                    if (source == null)
                    {
                        throw new ArgumentNullException ("source", "Parameter passed in must be a valid instance of an object implementing IImageAdapter (null was passed in)");
                    }

                    IImageAdapter retVal = (IImageAdapter)source.Clone ();
                    if (Points.Length == 0) { return retVal; }
                    double[] fadingLUT = ComputeFading ();
                    for (int y = 0; y < source.Height; y++)
                    {
                        for (int x = 0; x < source.Width; x++)
                        {
                            if (FadingDirection == FadingType.OpaqueToTranslucent)
                            {
                                retVal[x, y].ExtendedAlpha = 0.0;
                            }
                            foreach (System.Drawing.Point point in Points)
                            {
                                double dist = ComputeDistance (x, y, point);
                                if ((int)dist >= fadingLUT.Length) { continue; }
                                if (FadingDirection == FadingType.OpaqueToTranslucent)
                                {
                                    retVal[x, y].ExtendedAlpha += source[x, y].ExtendedAlpha * fadingLUT[(int)dist];
                                }
                                else 
                                {
                                    retVal[x, y].ExtendedAlpha -= source[x, y].ExtendedAlpha * fadingLUT[(int)dist];
                                }
                            }
                        }
                    }

                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
