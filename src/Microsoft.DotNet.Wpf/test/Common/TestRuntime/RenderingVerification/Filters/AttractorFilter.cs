// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using 
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// An Attractor Filter. Pixels close to the attractor values are kept, filtered out otherwise
    /// </summary>
    public class AttractorFilter: Filter
    {
        #region Constants
            private const string THRESHOLD = "Threshold";
            private const string ATTRACTORS = "Attractors";
            private const string AUTOMATIC = "Automatic";
            private const string AUTOMATICNUMBEROFCOLORS = "AutomaticNumberOfColors";
        #endregion Constants
        
        #region Properties
            private float[] _delta = new float[3];

            /// <summary>
            /// The Threshold value
            /// </summary>
            public double ColorAttractorThreshold
            {
                get
                {
                    return (double)this[THRESHOLD].Parameter;
                }
                set
                {
                    this[THRESHOLD].Parameter = value;
                }
            }
            /// <summary>
            /// The Attractors (off if automatic is on)
            /// </summary>
            public Color[] Attractors
            {
                get
                {
                    return (Color[])this[ATTRACTORS].Parameter;
                }

                set
                {
                    if (value != null)
                    {
                        this[ATTRACTORS].Parameter = value;
                    }
                }

            }
            /// <summary>
            /// Turning this to be true lead to a binarize filter (with AutomaticRange as range)
            /// </summary>
            /// <value></value>
            public bool Automatic
            {
                get { return (bool)this[AUTOMATIC].Parameter; }
                set{ this[AUTOMATIC].Parameter = value; }
            }
            /// <summary>
            /// The range bewtween two value when binarizing
            /// Note : Useful only if Automatic is true
            /// Note : This value cannot be set to zero
            /// </summary>
            /// <value></value>
            public byte[] AutomaticNumberOfColors
            {
                get { return (byte[])this[AUTOMATICNUMBEROFCOLORS].Parameter; }
                set 
                {
                    if (value == null) { throw new ArgumentNullException("AutomaticNumberOfColors", "value must be set to a valid instance (null passed in)"); }
                    if (value.Length != 3) { throw new ArgumentException("exactly 4 parameters must be passed (RGB)"); }
                    foreach(byte val in value)
                    {
                        if (val <= 0) { throw new ArgumentOutOfRangeException("AutomaticNumberOfColors", "All entries  must be > 0"); }
                    }
                    this[AUTOMATICNUMBEROFCOLORS].Parameter = value;
                }
            }

            /// <summary>
            /// Get the description for this filter
            /// </summary>
            /// <value></value>
            public override string FilterDescription
            {
                get
                {
                    return "Binarize an image by replacing color with the closest color defined in 'Attractors'";
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Attractor Filter constructor
            /// </summary>
            public AttractorFilter()
            {
                FilterParameter threshold = new FilterParameter(THRESHOLD, "The threshold value", (double)1024);
                FilterParameter attractors = new FilterParameter(ATTRACTORS, "The Attractor Normalized Colors", (Color[])new Color[]{Color.White, Color.Black});
                FilterParameter automatic = new FilterParameter(AUTOMATIC, "Turn the binarize to be automatic", (bool)true);
                FilterParameter automaticNumberOfColors = new FilterParameter(AUTOMATICNUMBEROFCOLORS, "The number of color per channel (used by automatic binarizing)", new byte[] { 255, 255, 255 });

                AddParameter(threshold);
                AddParameter(attractors);
                AddParameter(automatic);
                AddParameter(automaticNumberOfColors);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private IColor ComputeClosestColor(IColor currentColor)
                {
                    IColor retVal = (IColor)currentColor.Clone();

                    if (Automatic)
                    {
                        retVal.R = (byte)( _delta[0] * ((int)(currentColor.R / _delta[0]) + .5f) - 1 );
                        retVal.G = (byte)( _delta[1] * ((int)(currentColor.G / _delta[1]) + .5f) - 1 );
                        retVal.B = (byte)( _delta[2] * ((int)(currentColor.B / _delta[2]) + .5f) - 1 );
                    }
                    else 
                    {
                        double minDelta = double.MaxValue;
                        IColor diffColor = ColorByte.Empty;
                        for (int index = 0; index < Attractors.Length; index++)
                        {
                            double diff = ImageComparator.ColorDifferenceARGB(new ColorByte(Attractors[index]), currentColor, out diffColor);
                            if (diff < ColorAttractorThreshold && diff < minDelta)
                            {
                                minDelta = diff;
                                retVal.ARGB = Attractors[index].ToArgb();
                            }
                            else 
                            {
                                retVal.IsEmpty = true;
                            }
                        }
                    }
                    return retVal;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    IImageAdapter retVal = null;

                    if (source != null)
                    {
                        int width = source.Width;
                        int height = source.Height;
                        retVal = new ImageAdapter(source.Width, source.Height, ColorByte.Empty);
                        if (Automatic)
                        {
                            _delta[0] = 256f / AutomaticNumberOfColors[0];
                            _delta[1] = 256f / AutomaticNumberOfColors[1];
                            _delta[2] = 256f / AutomaticNumberOfColors[2];
                        }


                        for (int j = 0; j < height; j++)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                retVal[i, j] = ComputeClosestColor(source[i, j]);
                            }
                        }
                    }

                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
