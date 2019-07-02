// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
    #endregion using

    /// <summary>
    /// Summary description for EqualizeFilter.
    /// </summary>
    public class EqualizeFilter : Filter
    {
        #region Constantes
            private const string ADAPTIVE = "Adaptive Equalization";
        #endregion Constantes

        #region Properties
            /// <summary>
            /// Use Adaptive (ie : local) or global histogram equalization
            /// </summary>
            public bool AdaptiveEqualization
            {
                get 
                {
                    return (bool)this[ADAPTIVE].Parameter;
                }
                set 
                {
                    this[ADAPTIVE].Parameter = value;
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
                    return "Equalize the histogram of the image on a per channel basis (excluding Alpha channel).";
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Constructor
            /// </summary>
            public EqualizeFilter()
            {
                FilterParameter adaptiveFilter = new FilterParameter(ADAPTIVE, "Perform an adaptive (local) Equalization", false);
                AddParameter(adaptiveFilter);
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Equalize the image (Equalize the histogram on all channel, except alpha)
            /// (build and use the histogram of the image to do so)
            /// </summary>
            /// <param name="source">The image to be equalized</param>
            /// <returns>The image equalized</returns>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                // Check params
                if (source == null)
                { 
                    throw new ArgumentNullException("source", "Argument cannot be null");
                }

                int[] sum = new int[] { 0, 0, 0, 0 };
                Hashtable equalized = new Hashtable();
                Hashtable dico = IPUtil.BuildHistogram(source);
                IImageAdapter retVal = new ImageAdapter(source.Width, source.Height);

                // Init HashTable -- contains 4 ArrayList[] : 4 channels of 256 entries ( ArrayList of points )
                for (int channel = 1; channel < IPUtil.intColor.Length; channel++)     // Ignore Alpha Channel
                {
                    ArrayList[] list = new ArrayList[256];

                    for (int index = 0; index < 256; index++)
                    {
                        list[index] = new ArrayList();
                    }

                    equalized.Add(IPUtil.intColor[channel], list);
                }
                equalized[IPUtil.ALPHA] = ((ArrayList[])dico[IPUtil.ALPHA]).Clone();    // Clone Alpha channel from original Histogram


                // compute the sum per channel
                for (int channel = 0; channel < IPUtil.intColor.Length; channel++)
                {
                    ArrayList[] array = (ArrayList[])dico[IPUtil.intColor[channel]];
                    for (int index = 0; index < 256; index++)
                    { 
                        sum[channel] += array[index].Count;
                    }
                }

                // Stretch and Normalize the histogram
                // Transformation used : 
                //    (min <= i <= max)
                //    Normalize :   0.5 + ( Sum(0,i-1) + ( Sum(i-1,i) / 2 ) ) * 255 / Sum(0,255) 
                for (int channel = 1; channel < IPUtil.intColor.Length; channel++)    // Ignore Alpha channel
                {
                    ArrayList[] channelOriginal = (ArrayList[])dico[IPUtil.intColor[channel]];
                    ArrayList[] channelEqualized = (ArrayList[])equalized[IPUtil.intColor[channel]];
                    float equalizeConstant = 255.0f / ((sum[channel] != 0) ? sum[channel] : 1); 
                    int currentSum = 0;
                    float equalize = 0f;
                    for (int index = 0; index < 256; index++)
                    { 
                        equalize = 0.5f + (currentSum + channelOriginal[index].Count / 2) * equalizeConstant;
                        currentSum += channelOriginal[index].Count;
                        channelEqualized[(int)equalize].AddRange(channelOriginal[index]);
                    }
                }

                retVal = IPUtil.HistogramToIImageAdapter(equalized, source);

                equalized.Clear();
                dico.Clear();

                return retVal;

            }
        #endregion Methods
    }
}
