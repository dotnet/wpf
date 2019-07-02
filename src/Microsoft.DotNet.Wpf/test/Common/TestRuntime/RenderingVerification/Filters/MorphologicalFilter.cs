// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.ComponentModel;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using
    
    /// <summary>
    /// The usual Linear Filter
    /// </summary>
    public class MorphologicalFilter : Filter
    {

        #region Constants
            private const string ITERATION = "Iteration";
            private const string OUTWARD = "Outward";
            private const string FOREGROUNDCOLOR= "ForegroundColor";
            private const string BACKGROUNDCOLOR = "BackgroundColor";
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The number of iteration
                /// </summary>
                public int Iteration
                {
                    get
                    {
                        return (int)this[ITERATION].Parameter;
                    }
                    set
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException("Iteration", "Value to be set must be positive (or zero)");
                        }

                        this[ITERATION].Parameter = value;
                    }
                }
                /// <summary>
                /// Direction of the filter (inward or outward
                /// </summary>
                public bool Outward
                {
                    get
                    {
                        return (bool)this[OUTWARD].Parameter;
                    }
                    set
                    {
                        this[OUTWARD].Parameter = value;
                    }
                }
                /// <summary>
                /// Direction of the filter (inward or outward
                /// </summary>
                [TypeConverterAttribute(typeof(RenderingColorConverter))] 
                [EditorAttribute(typeof(RenderingColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
                public IColor ForegroundColor
                {
                    get
                    {
                        return (IColor)this[FOREGROUNDCOLOR].Parameter;
                    }
                    set
                    {
                        this[FOREGROUNDCOLOR].Parameter = (IColor)value;
                    }
                }
                /// <summary>
                /// Direction of the filter (inward or outward
                /// </summary>
//                [TypeConverterAttribute(typeof(IColorConverter))] 
                [TypeConverterAttribute(typeof(RenderingColorConverter))] 
                [EditorAttribute(typeof(RenderingColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
                public IColor BackgroundColor
                {
                    get
                    {
                        return (IColor)this[BACKGROUNDCOLOR].Parameter;
                    }
                    set
                    {
                        this[BACKGROUNDCOLOR].Parameter = (IColor)value;
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
                        return "Basic glow operation";
                    }
                }
            #endregion Public Properties
        #endregion Properties
        
        #region Constructors
            /// <summary>
            /// Gaussian Filter constructor
            /// </summary>
            public MorphologicalFilter()
            {
                IColor white = new ColorByte(255,255,255,255);
                IColor black = new ColorByte(255,0,0,0);
                FilterParameter iteration = new FilterParameter(ITERATION, "Number of Iteration", (int)1);
                FilterParameter outward = new FilterParameter(OUTWARD ,"Direction of the morphological filter", true);
                FilterParameter foregroundColor = new FilterParameter(FOREGROUNDCOLOR ,"ForeGround color", (IColor)white);
                FilterParameter backgroundColor = new FilterParameter(BACKGROUNDCOLOR, "BackGround color", (IColor)black);

                AddParameter(iteration);
                AddParameter(outward);
                AddParameter(foregroundColor);
                AddParameter(backgroundColor);

                ForegroundColor = white;
                BackgroundColor = black;
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// filter implementation
            /// </summary>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                ImageAdapter iret = null;
                if (source != null)
                {
                    int width = source.Width;
                    int height = source.Height;
                                    
                    iret = new ImageAdapter(width,height);
                    bool [,] bmap = new bool[width,height];
                    bool [,] buffer = new bool[width,height];
                    
                    // build the boolean map to process
                    for (int j =0;j < height;j++)
                    {
                        for (int i =0;i < width;i++)
                        {
                            IColor lcol = source[i,j];
                            if (lcol.IsEmpty == true)
                            {
                                bmap[i,j] = false;
                            }
                            else
                            {
                                bmap[i,j] = true;
                            }
                        }
                    }
                                    
                    // proceed with iterations
                    for (int iter = 0; iter < Iteration; iter++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            for (int i = 0; i < width; i++)
                            {
                                // intialize buffer but do not override previous morph effect
                                if(buffer[i,j]==false)
                                {
                                    buffer[i,j] = bmap[i,j];
                                }
                                                            
                                if (bmap[i,j] == true)
                                {
                                    int cnt = 0;
                                    int ocnt = 0;
                                    for (int k = -1; k <= 1; k++)
                                    {
                                        for (int l = -1; l <= 1; l++)
                                        {
                                            if (k + j >= 0 && k + j < height && i + l >= 0 && i + l < width)
                                            {
                                                ocnt++;
                                                if (bmap[i+l, j+k] == true)
                                                {
                                                    cnt++;
                                                }
                                            }
                                        }
                                    }
                                    
                                    if (cnt > 0 && cnt < ocnt)
                                    {
                                        if (Outward == true)
                                        {
                                            for (int k = -1; k <= 1; k++)
                                            {
                                                for (int l = -1; l <= 1; l++)
                                                {
                                                    if (k + j >= 0 && k + j < height && i + l >= 0 && i + l < width)
                                                    {
                                                        buffer[i+l, j+k] = true;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int k = -1; k <= 1; k++)
                                            {
                                                for (int l = -1; l <= 1; l++)
                                                {
                                                    if (k + j >= 0 && k + j < height && i + l >= 0 && i + l < width)
                                                    {
                                                        buffer[i+l, j+k] = false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        bool [,]tmp = buffer;
                        buffer=bmap;
                        bmap = tmp; 
                    }

                    IColor vfc = ForegroundColor;
                    vfc.IsEmpty = false;
                    ForegroundColor = vfc;
                    IColor vbc = BackgroundColor;
                    vbc.IsEmpty = true;
                    BackgroundColor = vbc;

                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            if (bmap[i,j] == false)
                            {
                                iret[i,j] = BackgroundColor;
                            }
                            else
                            {
                                iret[i,j] = ForegroundColor;
                            }
                        }
                    }
                }
                return iret;
            }
        #endregion Methods
    }
}
