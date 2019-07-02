// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Collections;
    #endregion using

    /// <summary>
    /// Summary description for OrderStatisticFilter.
    /// </summary>
    public class OrderStatisticFilter : Filter
    {
        #region Constants
            private const string MEAN = "Mean";
            private const string MIN = "Min";
            private const string MAX = "Max";
            private const string MASKSIZE = "MaskSize";
        #endregion Constants

        #region Properties
            /// <summary>
            /// Filter will be using the MEAN value found in the masked area
            /// </summary>
            /// <value></value>
            public bool Mean
            {
                get 
                {
                    return (bool)this[MEAN].Parameter;
                }
                set 
                {
                    this[MEAN].Parameter = value;
                }
            }
            /// <summary>
            /// Filter will be using the MAX value found in the masked area
            /// </summary>
            /// <value></value>
            public bool Max
            {
                get 
                {
                    return (bool)this[MAX].Parameter;
                }
                set 
                {
                    this[MAX].Parameter = value;
                }
            }
            /// <summary>
            /// Filter will be using the MIN value found in the masked area
            /// </summary>
            /// <value></value>
            public bool Min
            {
                get 
                {
                    return (bool)this[MIN].Parameter;
                }
                set 
                {
                    this[MIN].Parameter = value;
                }
            }
            /// <summary>
            /// Get/set the size of the mask (a square of n * n pixel)
            /// </summary>
            /// <value></value>
            public int MaskSize
            {
                get 
                {
                    return (int)this[MASKSIZE].Parameter;
                }
                set 
                {
                    this[MASKSIZE].Parameter = value;
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
                    return "Remove noise from an image by getting the mean/max/min color of the surrounding pixel; usefull for salt and pepper corruption.";
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instantiate a OrderStatisticFilter class
            /// </summary>
            public OrderStatisticFilter ()
            {
                FilterParameter mean = new FilterParameter (MEAN, "Replace every pixel by the mean of the ones nearby ", true);
                FilterParameter min = new FilterParameter (MAX, "Replace every pixel by the min of the ones nearby ", false);
                FilterParameter max = new FilterParameter (MIN, "Replace every pixel by the max of the ones nearby ", false);
                FilterParameter maskSize = new FilterParameter (MASKSIZE, "The area to apply the filter on", 3);

                AddParameter(mean);
                AddParameter(min);
                AddParameter(max);
                AddParameter (maskSize);
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Perform the statistical operation
            /// </summary>
            /// <param name="source">The image to use for the operation</param>
            /// <returns>The Filtered image</returns>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                if (source == null)
                { 
                    throw new ArgumentNullException("Source cannot be set to 'null'");
                }

                int sourceHeight = source.Height;
                int sourceWidth = source.Width;
                int deltaLeftTop = (MaskSize - 1) / 2;  // same as Math.Floor((maskSize-1)/2);
                int deltaRightBottom = MaskSize / 2; // same as Math.Ceiling((masksize-1) /2);
                int minX = 0, minY = 0, maxX = 0, maxY = 0;
                double alpha = 0.0, red = 0.0, green = 0.0, blue = 0.0;
                int minIndex = 0;
                int maxIndex = MaskSize - 1;
                int meanIndex = maxIndex * maxIndex / 2;

                // init the variable holding the data
/*
                Dictionary<int, List<double>> sortedLists = new Dictionary<int,List<double>>(4);
                sortedLists.Add(IPUtil.ALPHA, new List<double>(MaskSize * MaskSize));
                sortedLists.Add(IPUtil.RED, new List<double>(MaskSize * MaskSize));
                sortedLists.Add(IPUtil.GREEN, new List<double>(MaskSize * MaskSize));
                sortedLists.Add(IPUtil.BLUE, new List<double>(MaskSize * MaskSize));
*/
                Hashtable sortedLists = new Hashtable(4);

                sortedLists.Add(IPUtil.ALPHA, new ArrayList(MaskSize * MaskSize));
                sortedLists.Add(IPUtil.RED, new ArrayList(MaskSize * MaskSize));
                sortedLists.Add(IPUtil.GREEN, new ArrayList(MaskSize * MaskSize));
                sortedLists.Add(IPUtil.BLUE, new ArrayList(MaskSize * MaskSize));

                ImageAdapter retVal = new ImageAdapter(sourceWidth, sourceHeight);
                IColor color = null;

                for (int y = 0; y < sourceHeight; y++)
                {
                    // compute region of interest on the y axis
                    minY = y - deltaLeftTop;
                    if (minY < 0) { minY = 0;}
                    maxY = y + deltaRightBottom;
                    if (maxY >= sourceHeight) { maxY = sourceHeight - 1; }

                    for (int x = 0; x < sourceWidth; x++)
                    {
                        color = (IColor)source[x, y].Clone(); color.IsEmpty = true;

                        // cleanup the lists
                        IEnumerator iter = sortedLists.Keys.GetEnumerator();
                        while(iter.MoveNext())
                        {
                            ((ArrayList)sortedLists[iter.Current]).Clear();
                        }

                        // compute region of interest on the x axis
                        minX = x - deltaLeftTop;
                        if (minX< 0) {minX = 0;}
                        maxX = x + deltaRightBottom;
                        if (maxX >= sourceWidth) { maxX = sourceWidth - 1;}
                        // compute the max index and mean
                        maxIndex = (maxX - minX + 1) * (maxY - minY + 1);
                        meanIndex = maxIndex / 2;
                        maxIndex--;


                        // Store every pixel for the ROI
                        for (int mx = minX; mx <= maxX; mx++)
                        {
                            for (int my = minY; my <= maxY; my++)
                            { 
                                ((ArrayList)sortedLists[IPUtil.ALPHA]).Add(source[mx, my].Alpha);
                                ((ArrayList)sortedLists[IPUtil.RED]).Add(source[mx, my].Red);
                                ((ArrayList)sortedLists[IPUtil.GREEN]).Add(source[mx, my].Green);
                                ((ArrayList)sortedLists[IPUtil.BLUE]).Add(source[mx, my].Blue);
                            }
                        }
                        // Sort the lists
                        iter.Reset();
                        while(iter.MoveNext())
                        { 
                            ArrayList list = (ArrayList)sortedLists[iter.Current];
                            list.Sort();
                        }

                        if (Mean)
                        {
                            alpha = (double)((ArrayList)sortedLists[IPUtil.ALPHA])[meanIndex];
                            red = (double)((ArrayList)sortedLists[IPUtil.RED])[meanIndex];
                            green = (double)((ArrayList)sortedLists[IPUtil.GREEN])[meanIndex];
                            blue = (double)((ArrayList)sortedLists[IPUtil.BLUE])[meanIndex];
                        }
                        if (Max)
                        {
                            alpha = (double)((ArrayList)sortedLists[IPUtil.ALPHA])[maxIndex];
                            red = (double)((ArrayList)sortedLists[IPUtil.RED])[maxIndex];
                            green = (double)((ArrayList)sortedLists[IPUtil.GREEN])[maxIndex];
                            blue = (double)((ArrayList)sortedLists[IPUtil.BLUE])[maxIndex];
                        }
                        if (Min)
                        {
                            alpha = (double)((ArrayList)sortedLists[IPUtil.ALPHA])[minIndex];
                            red = (double)((ArrayList)sortedLists[IPUtil.RED])[minIndex];
                            green = (double)((ArrayList)sortedLists[IPUtil.GREEN])[minIndex];
                            blue = (double)((ArrayList)sortedLists[IPUtil.BLUE])[minIndex];
                        }
                        color.ExtendedAlpha = alpha;
                        color.ExtendedRed = red;
                        color.ExtendedGreen = green;
                        color.ExtendedBlue = blue;

                        retVal[x, y] = color;
                    }
                }

                return retVal;
            }
        #endregion Methods

    }
}
