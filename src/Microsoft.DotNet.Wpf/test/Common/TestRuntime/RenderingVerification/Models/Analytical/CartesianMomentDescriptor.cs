// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical.Descriptors
{
    #region Namespaces.
        using System;
        using System.IO;
        using System.Xml;
        using System.Text;
        using System.Drawing;
        using System.Reflection;
        using System.Collections;
        using System.Globalization;
        using System.Drawing.Imaging;
        using System.Drawing.Drawing2D;
        using System.Xml.Serialization;
        using System.Runtime.Serialization;
        using Microsoft.Test.RenderingVerification.Model.Analytical;
    #endregion Namespaces.

    /// <summary>
    /// Cartesian implementation of IDescriptor 
    /// </summary>
    [SerializableAttribute()]
    [XmlRootAttribute("CartesianMomentDescriptor")]
    public class CartesianMomentDescriptor : MomentDescriptorBase, ISerializable
    {
        #region Constructor
            /// <summary>
            /// Creates a new CartesianMomentDescriptor instance.
            /// </summary>
            protected CartesianMomentDescriptor(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
            /// <summary>
            /// Creates a new CartesianMomentDescriptor instance.
            /// </summary>
            public CartesianMomentDescriptor() : base()
            {
            }
        #endregion Constructor

        #region IDescriptor interface implementation
            /// <summary>
            /// Compute all Descriptors for the bitmap.
            /// </summary>
            /// <param name="silhouetteExtraPixels">The pixels participating in the silhouette</param>
            public override void ComputeDescriptor(Pixel[] silhouetteExtraPixels )
            {
                base.ComputeDescriptor(silhouetteExtraPixels);
                float dist = GetMaxDistance(xMedian, yMedian);
                float distSilhouette = GetMaxDistance(xMedianSilhouette, yMedianSilhouette);

                DescriptorSquareMatrix silhouette = (DescriptorSquareMatrix)DescriptorDependentObjects["silhouette"];
                DescriptorSquareMatrix shape = (DescriptorSquareMatrix)DescriptorDependentObjects["shape"];
                DescriptorSquareMatrix texture = (DescriptorSquareMatrix)DescriptorDependentObjects["texture"];

                foreach (Pixel pixel in ParticipatingPixels)
                {
                    // compute shape
                    float xOffset = ((float)pixel.X - xMedian) / dist;
                    float yOffset = ((float)pixel.Y - yMedian) / dist;
                    float xOffsetSilhouette = ((float)pixel.X - xMedianSilhouette) / distSilhouette;
                    float yOffsetSilhouette = ((float)pixel.Y - yMedianSilhouette) / distSilhouette;

                    // WARNING: we add an offest of 1.0f to distinguish black from background
                    // otherwise info is lost and black objects have the same ExtDescriptors : a null matrix
                    float luminance = (float)((pixel.Color.ExtendedBlue + pixel.Color.ExtendedRed + pixel.Color.ExtendedGreen) / 3.0) + 1.0f;
                                        
                    float yterm = 1.0f;
                    float ytermSilhouette = 1.0f;
                    for (int l = 0; l < DescriptorSquareMatrix.Length; l++)
                    {
                        float xterm = 1.0f;
                        float xtermSilhouette = 1.0f;
                        for (int m = 0; m < DescriptorSquareMatrix.Length; m++)
                        {
                            float xyprod = xterm * yterm;
                            silhouette[l,m] += xtermSilhouette * ytermSilhouette;
                            shape[l, m] += xyprod;
                            texture[l, m] += luminance * xyprod;
                            xterm *= xOffset;
                            xtermSilhouette *= xOffsetSilhouette;
                        }
                        yterm *= yOffset;
                        ytermSilhouette *= yOffsetSilhouette;
                    }
                }
                if (silhouetteExtraPixels != null)
                {
                    foreach (Pixel pixel in silhouetteExtraPixels)
                    {
                        // compute silhouette (add extra pixels)
                        float xOffsetSilhouette = ((float)pixel.X - xMedianSilhouette) / distSilhouette;
                        float yOffsetSilhouette = ((float)pixel.Y - yMedianSilhouette) / distSilhouette;

                        float ytermSilhouette = 1.0f;
                        for (int l = 0; l < DescriptorSquareMatrix.Length; l++)
                        {
                            float xtermSilhouette = 1.0f;
                            for (int m = 0; m < DescriptorSquareMatrix.Length; m++)
                            {
                                silhouette[l, m] += xtermSilhouette * ytermSilhouette;
                                xtermSilhouette *= xOffsetSilhouette;
                            }
                            ytermSilhouette *= yOffsetSilhouette;
                        }
                    }
                }

                // Normalize the matrices
                for (int y = 0; y < DescriptorSquareMatrix.Length; y++)
                {
                    for (int x = 0; x < DescriptorSquareMatrix.Length; x++)
                    {
                        silhouette[x, y] /= BoundingBoxArea;
                        shape[x, y] /= BoundingBoxArea;
                        texture[x, y] /= BoundingBoxArea;
                    }
                }
            }
        #endregion IDescriptor interface implementation
    }
}

