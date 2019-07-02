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
    /// Summary description for ContrastFilter.
    /// </summary>
    public class ContrastFilter : Filter
    {
        #region Properties
            /// <summary>
            /// Get the description for this filter
            /// </summary>
            /// <value></value>
            public override string FilterDescription
            {
                get
                {
                    return "Increase the Contrast by stretching the histogram of color (Per channel based).";
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Constuctor
            /// </summary>
            public ContrastFilter()
            {
            }

        #endregion Constructors

        #region Methods
            /// <summary>
            /// Contrast the image (Contrast by expanding the histogram on all channel)
            /// </summary>
            /// <param name="source">The image to be Contrasted</param>
            /// <returns>The image Contrasted</returns>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                // Check Params
                if (source == null)
                { 
                    throw new ArgumentNullException("source", "Argument cannot be null");
                }

                IImageAdapter retVal = new ImageAdapter(source.Width, source.Height);
                int[] min = new int[] { (int)source[0, 0].MinChannelValue, (int)source[0, 0].MinChannelValue, (int)source[0, 0].MinChannelValue, (int)source[0, 0].MinChannelValue };
                int[] max = new int[] { (int)source[0, 0].MaxChannelValue, (int)source[0, 0].MaxChannelValue, (int)source[0, 0].MaxChannelValue, (int)source[0, 0].MaxChannelValue };
                Hashtable dico = IPUtil.BuildHistogram(source);
                Hashtable stretched = new Hashtable();
                for (int channel = 1; channel < IPUtil.intColor.Length; channel++)   // Ignore Alpha Channel
                    {
                        ArrayList[] list = new ArrayList[256];
                        for (int index = 0; index < 256; index++)
                        {
                            list[index] = new ArrayList();
                        }
                        stretched.Add(IPUtil.intColor[channel], list);
                    }
                    stretched[IPUtil.ALPHA] = ((ArrayList[])dico[IPUtil.ALPHA]).Clone();    // Clone Alpha channel from original Histogram


                // Find the min and max value of the histogram
                for (int channel = 1; channel < IPUtil.intColor.Length; channel++) // ignore the alpha channel
                {
                    bool minFound = false;
                    ArrayList[] array = (ArrayList[])dico[IPUtil.intColor[channel]];

                    for (int index = 0; index < 256; index++)
                    {
                        if (array[index].Count != 0)
                        {
                            max[channel] = index;
                            minFound = true;
                        }
                        else
                        {
                            if (!minFound)
                            {
                                min[channel] = index;
                            }
                        }
                    }
                }

                // Stretch the histogram
                // Transformation used : 
                //    (min <= i <= max)
                //    Stretch   :   (i - min) * 255 / ( max - min) + 0.5
                for (int channel = 1; channel < IPUtil.intColor.Length; channel++)  // Ignore the Alpha Channel
                {
                    ArrayList[] channelOriginal = (ArrayList[])dico[IPUtil.intColor[channel]];
                    ArrayList[] channelStretched = (ArrayList[])stretched[IPUtil.intColor[channel]];
                    int minChannel = min[channel];
                    int maxChannel = max[channel];
                    float stretchConstant = (float)source[0,0].NormalizedValue / (float)(maxChannel - minChannel);
                    float stretch = 0f;

                    for (int index = minChannel; index <= maxChannel; index++)
                    {
                        stretch = (index - minChannel) * stretchConstant + 0.5f;
                        channelStretched[(int)stretch].AddRange(channelOriginal[index]);
                    }
                }

                retVal = IPUtil.HistogramToIImageAdapter(stretched, source);

                dico.Clear();
                stretched.Clear();

                return retVal;
            }

        #endregion Methods

    }
}
