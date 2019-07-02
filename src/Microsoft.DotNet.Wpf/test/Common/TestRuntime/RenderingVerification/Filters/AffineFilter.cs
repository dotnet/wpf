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
    /// The usual Linear Filter
    /// the original image is transformed with 
    /// I' = A*I + BIopt + C
    /// where
    /// 
    /// I = image to process
    /// Iopt = optionnal image
    /// 
    /// A = Left   coeffcient
    /// B = right  coeffcient
    /// C = Offset coefficient
    /// </summary>
    public class AffineFilter : Filter
    {
        #region Constants
            private const string IMAGE     = "Image";
            private const string OFFSET_X  = "OffsetX";
            private const string OFFSET_Y  = "OffsetY";
            private const string LEFT      = "LeftCoefficient";
            private const string RIGHT     = "RightCoefficient";
            private const string OFFSET    = "OffsetCoefficient";
            private const string NORMALIZE_TRANSFORM = "NormalizeTransform";
        #endregion Constants

        #region Properties
            #region Public Properties
                /// <summary>
                /// The optionnal image for linear composition
                /// </summary>
                public IImageAdapter Image
                {
                    get
                    {
                        return (IImageAdapter)this[IMAGE].Parameter;
                    }
                    set
                    {
                        this[IMAGE].Parameter = value;
                    }
                }

                /// <summary>
                /// The X offset of the optionnal image
                /// </summary>
                public int OffsetX
                {
                    get
                    {
                        return (int)this[OFFSET_X].Parameter;
                    }
                    set
                    {
                        this[OFFSET_X].Parameter = value;
                    }
                }

                /// <summary>
                /// The Y offset of the optionnal image
                /// </summary>
                public int OffsetY
                {
                    get
                    {
                        return (int)this[OFFSET_Y].Parameter;
                    }
                    set
                    {
                        this[OFFSET_Y].Parameter = value;
                    }
                }

                /// <summary>
                /// The offset to apply to the transform
                /// new = Ax + By + C
                /// where 
                /// A = LeftCoefficient
                /// B = RightCoefficient
                /// C = OffsetCoefficient
                /// </summary>
                public double OffsetCoefficient
                {
                    get
                    {
                        return (double)this[OFFSET].Parameter;
                    }
                    set
                    {
                        this[OFFSET].Parameter = value;
                    }
                }

                /// <summary>
                /// The offset to apply to the transform
                /// new = Ax + By + C
                /// where 
                /// A = LeftCoefficient
                /// B = RightCoefficient
                /// C = OffsetCoefficient
                /// </summary>
                public double LeftCoefficient
                {
                    get
                    {
                        return (double)this[LEFT].Parameter;
                    }
                    set
                    {
                        this[LEFT].Parameter = value;
                    }
                }

                /// <summary>
                /// The offset to apply to the transform
                /// new = Ax + By + C
                /// where 
                /// A = LeftCoefficient
                /// B = RightCoefficient
                /// C = OffsetCoefficient
                /// </summary>
                public double RightCoefficient
                {
                    get
                    {
                        return (double)this[RIGHT].Parameter;
                    }
                    set
                    {
                        this[RIGHT].Parameter = value;
                    }
                }

                /// <summary>
                /// Indicates if The overall weight (A+B+C) of the transform should be normalized to 1
                /// where 
                /// A = LeftCoefficient
                /// B = RightCoefficient
                /// C = OffsetCoefficient
                /// </summary>
                public bool NormalizeTransform
                {
                    get
                    {
                        return (bool)this[NORMALIZE_TRANSFORM].Parameter;
                    }
                    set
                    {
                        this[NORMALIZE_TRANSFORM].Parameter = value;
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
                        return "Perform an affine operation on the two images (this + the one passed as Image parameter)";
                    }
                }
            #endregion Public Properties
        #endregion Properties
    
        #region Constructors
            /// <summary>
            /// Affine Filter constructor
            /// </summary>
            public AffineFilter()
            {
                FilterParameter image = new FilterParameter(IMAGE, "Image to blend", new ImageAdapter(0, 0));   
                FilterParameter offsetX = new FilterParameter(OFFSET_X, "X offset", (int)0);
                FilterParameter offsetY = new FilterParameter(OFFSET_Y, "Y offset", (int)0);
                FilterParameter offsetCoefficient = new FilterParameter(OFFSET, "offset coeeficient", (double)0.0);
                FilterParameter leftCoefficient = new FilterParameter(LEFT, "left coefficient", (double)1.0);
                FilterParameter rightCoefficient = new FilterParameter(RIGHT, "right coefficient", (double)0.0);
                FilterParameter normalizeTransform = new FilterParameter(NORMALIZE_TRANSFORM, "The Post Process will normalize the results", true);

                AddParameter(image);
                AddParameter(leftCoefficient);
                AddParameter(rightCoefficient);
                AddParameter(offsetCoefficient);
                AddParameter(offsetX);
                AddParameter(offsetY);
                AddParameter(normalizeTransform);
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// filter implementation
            /// </summary>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                if (!NormalizeTransform && (LeftCoefficient + RightCoefficient + OffsetCoefficient == 0.0))
                {
                    throw new ArgumentException("Cant' divide by 0, LeftCoefficient+RightCoefficient+OffsetCoefficient must be != 0.0");
                }
                            
                IImageAdapter iret = null;
                if (source != null)
                {
                    int width = source.Width;
                    int height = source.Height;

                    iret = new ImageAdapter(width, height);

                    double lNormCoeff = LeftCoefficient + RightCoefficient + OffsetCoefficient;

                    if(!NormalizeTransform)
                    {
                        lNormCoeff = 1.0;
                    }               
            
                    if (Image == null || (Image.Width + Image.Height) == 0)
                    {
                        for (int j =0;j < height;j++)
                        {
                            for (int i =0;i < width;i++)
                            {
                                IColor lcol = source[i,j];
                                lcol.Red   = LeftCoefficient * lcol.Red   / (lNormCoeff);                       
                                lcol.Green = LeftCoefficient * lcol.Green / (lNormCoeff);                       
                                lcol.Blue  = LeftCoefficient * lcol.Blue  / (lNormCoeff);                       
                                iret[i,j]  = lcol;
                            }
                        }
                    }
                    else
                    {
                        int tagWidth  = Image.Width;
                        int tagHeight = Image.Height;

                        for (int j =0;j < height;j++)
                        {
                            for (int i =0;i < width;i++)
                            {
                                        
                                IColor lcol = source[i,j];
                                iret[i,j] = lcol;
                                
                                if(
                                    i<tagWidth && j<tagHeight &&
                                    i-OffsetX>=0 && j-OffsetY>=0 &&     
                                    i-OffsetX<width && j-OffsetY<height
                                )
                                    {
                                    
                                        IColor scol = Image[i,j];
                                    
                                        if(scol.IsEmpty == false)
                                        {
                                            lcol.Red   = (LeftCoefficient*lcol.Red   + RightCoefficient*scol.Red)  / (lNormCoeff);
                                            lcol.Green = (LeftCoefficient*lcol.Green + RightCoefficient*scol.Green)/ (lNormCoeff);
                                            lcol.Blue  = (LeftCoefficient*lcol.Blue  + RightCoefficient*scol.Blue) / (lNormCoeff);
                                            iret[i,j]  = lcol;
                                        }
                                    }
                        
                            }
                        }
                    }
                }
                return iret;
            }
        #endregion Methods
    }
}
