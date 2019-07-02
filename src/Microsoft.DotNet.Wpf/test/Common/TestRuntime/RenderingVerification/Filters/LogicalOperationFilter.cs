// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// Summary description for LogicalOperationFilter.
    /// </summary>
    public class LogicalOperationFilter : Filter
    {
        #region Constants
            private const string AND = "And";
            private const string OR  = "Or";
            private const string XOR = "Xor";
            private const string NOT = "Not";
            private const string MASK = "Mask";
            private const string IGNOREALPHA = "IgnoreAlpha";
        
        #endregion Constants

        #region Delegates
            private delegate IColor ProcessOperatorHandler(IColor source, IColor mask);
        #endregion Delegates

        #region Properties
            #region Private Properties
                private ProcessOperatorHandler _processOperator;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Apply the AND operator on on the image using the mask image
                /// </summary>
                public bool And 
                {
                    get
                    {
                        return (bool)this[AND].Parameter;
                    }
                    set 
                    {
                        ResetAllParam();
                        _processOperator = new ProcessOperatorHandler(ProcessAnd);
                        this[AND].Parameter = value;
                    }
                }
                /// <summary>
                /// Apply the OR operator on on the image using the mask image
                /// </summary>
                /// <value></value>
                public bool Or
                {
                    get
                    {
                        return (bool)this[OR].Parameter;
                    }
                    set
                    {
                        ResetAllParam();
                        _processOperator = new ProcessOperatorHandler(ProcessOr);
                        this[OR].Parameter = value;
                    }
                }
                /// <summary>
                /// Apply the XOR operator on on the image using the mask image
                /// </summary>
                /// <value></value>
                public bool Xor
                {
                    get
                    {
                        return (bool)this[XOR].Parameter;
                    }
                    set
                    {
                        ResetAllParam();
                        _processOperator = new ProcessOperatorHandler(ProcessXor);
                        this[XOR].Parameter = value;
                    }
                }
                /// <summary>
                /// Apply the NOT operator on the image
                /// </summary>
                /// <value></value>
                public bool Not
                {
                    get
                    {
                        return (bool)this[NOT].Parameter;
                    }
                    set
                    {
                        ResetAllParam();
                        _processOperator = new ProcessOperatorHandler(ProcessNot);
                        this[NOT].Parameter = value;
                    }
                }

                /// <summary>
                /// The image to use as mask for most of the logical operation (excluding "not")
                /// </summary>
                /// <value></value>
                public IImageAdapter MaskImage
                { 
                    get 
                    {
                        return (IImageAdapter)this[MASK].Parameter;
                    }
                    set 
                    {
                        this[MASK].Parameter = value;
                    }
                }
                /// <summary>
                /// Perform the operation using the Alpha channel or not
                /// </summary>
                /// <value></value>
                public bool IgnoreAlphaChannel
                {
                    get 
                    {
                        return (bool)this[IGNOREALPHA].Parameter;
                    }
                    set 
                    {
                        this[IGNOREALPHA].Parameter = value;
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
                        return "Perform logical operation (And/Or/Xor/Not) on the image";
                    }
                }

            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an instance of the LogicalOperationFilter class
            /// </summary>
            public LogicalOperationFilter()
            {
                FilterParameter ANDparam = new FilterParameter(AND, "Apply the Logical operation AND on this IImageAdapter", false);
                FilterParameter ORparam = new FilterParameter(OR, "Apply the Logical operation OR on this IImageAdapter", false);
                FilterParameter XORparam = new FilterParameter(XOR, "Apply the Logical operation XOR on this IImageAdapter", false);
                FilterParameter maskParam= new FilterParameter(MASK, "Mask to be used on the original image", (ImageAdapter)new ImageAdapter(1,1));
                FilterParameter ignoreAlpha = new FilterParameter(IGNOREALPHA, "Include - or not - the alpha channel in the operation", true);

                FilterParameter NOTparam = new FilterParameter(NOT, "Apply the Logical operation NOT on this IImageAdapter", true);
                
                AddParameter(ANDparam);
                AddParameter(ORparam);
                AddParameter(XORparam);
                AddParameter(NOTparam);
                AddParameter(maskParam);
                AddParameter(ignoreAlpha);
            }

        #endregion Constructors

        #region Methods
            #region Private Methods
                private IColor ProcessAnd(IColor source, IColor mask)
                { 
                    ColorByte retVal = new ColorByte();
                    Color sourceColor = source.ToColor();
                    Color maskColor = mask.ToColor();
                    retVal.A = (IgnoreAlphaChannel) ? source.A: (byte)(sourceColor.A & maskColor.A);
                    retVal.R = (byte)(sourceColor.R & maskColor.R);
                    retVal.G = (byte)(sourceColor.G & maskColor.G);
                    retVal.B = (byte)(sourceColor.B & maskColor.B);
                    return retVal;
                }
                private IColor ProcessOr(IColor source, IColor mask)
                { 
                    ColorByte retVal = new ColorByte();
                    Color sourceColor = source.ToColor(); 
                    Color maskColor = mask.ToColor(); 
                    retVal.A = (IgnoreAlphaChannel) ? source.A : (byte)(sourceColor.A | maskColor.A);
                    retVal.R = (byte)(sourceColor.R | maskColor.R);
                    retVal.G = (byte)(sourceColor.G | maskColor.G);
                    retVal.B = (byte)(sourceColor.B | maskColor.B);
                    return retVal;
                }
                private IColor ProcessXor(IColor source, IColor mask)
                { 
                    ColorByte retVal = new ColorByte();
                    Color sourceColor = source.ToColor(); 
                    Color maskColor = mask.ToColor(); 
                    retVal.A = (IgnoreAlphaChannel) ? source.A : (byte)(sourceColor.A ^ maskColor.A);
                    retVal.R = (byte)(sourceColor.R ^ maskColor.R);
                    retVal.G = (byte)(sourceColor.G ^ maskColor.G);
                    retVal.B = (byte)(sourceColor.B ^ maskColor.B);
                    return retVal;
                }
                private IColor ProcessNot(IColor source, IColor mask)
                { 
                    ColorDouble retVal = new ColorDouble();
                    retVal.Alpha = (IgnoreAlphaChannel) ? source.Alpha : 1.0 - source.Alpha;
                    retVal.Red = 1.0 - source.Red;
                    retVal.Green = 1.0 - source.Green;
                    retVal.Blue = 1.0 - source.Blue;
                    return retVal;
                }

                private void ResetAllParam()
                {
                    this[AND].Parameter = false;
                    this[OR].Parameter = false;
                    this[XOR].Parameter = false;
                    this[NOT].Parameter = false;
                }
            #endregion Private Methods
            #region Protected Methods
                /// <summary>
                /// Perform the selected logical operation
                /// </summary>
                /// <param name="source">The IImageAdapter to use </param>
                /// <returns>The modified image</returns>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    if (source == null)
                    { 
                        throw new ArgumentNullException("source", "param must be a valid instance (null was passed in)");
                    }

                    int height = source.Height;
                    int width = source.Width;
                    IImageAdapter retVal = new ImageAdapter(width, height);
                    IImageAdapter maskImage = MaskImage;
                    if(And)
                    {
                        _processOperator = new ProcessOperatorHandler(ProcessAnd);
                    }
                    else
                    {
                        if (Or)
                        {
                            _processOperator = new ProcessOperatorHandler(ProcessOr);
                        }
                        else 
                        { 
                            if (Xor)
                            {
                                _processOperator = new ProcessOperatorHandler(ProcessXor);
                            }
                            else 
                            {
                                if (Not)
                                {
                                    _processOperator = new ProcessOperatorHandler(ProcessNot);
                                    maskImage = retVal;
                                }
                                else 
                                {
                                    throw new RenderingVerificationException("Unvalid param selected, you must select at least one logical operation");
                                }
                            }
                        }
                    }

                    int maskWidth = maskImage.Width;
                    int maskHeight = maskImage.Height;
                    for (int y = 0; y < height; y++)
                    { 
                        for (int x = 0; x < width; x++)
                        { 
                            if (x < maskWidth && y < maskHeight)
                            {
                                retVal[x,y] = _processOperator(source[x, y], maskImage[x, y]);
                            }
                            else
                            {
                                retVal[x, y] = (IColor)source[x,y].Clone();
                                retVal[x,y].IsEmpty = true;
                            }
                        }
                    }
                    return retVal;
                }

            #endregion Protected Methods
        #endregion Methods

    }
}
