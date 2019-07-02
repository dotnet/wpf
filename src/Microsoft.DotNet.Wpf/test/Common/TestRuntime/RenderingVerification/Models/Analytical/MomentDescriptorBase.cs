// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical.Descriptors
{
    #region usings
        using System;
        using System.IO;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using System.Drawing.Drawing2D;
        using System.Xml.Serialization;
        using System.Runtime.Serialization;
        using Microsoft.Test.RenderingVerification;
        using Microsoft.Test.RenderingVerification.Model.Analytical;
    #endregion usings

    /// <summary>
    /// Base class for Moment Descriptors.
    /// </summary>
    [SerializableAttribute()]
    public abstract class MomentDescriptorBase : DescriptorBase, ISerializable
    {
        #region Properties
            #region Private data.
                private ICriterion[] _criteria = new ICriterion[0];
                private int xMin = int.MaxValue;
                private int xMax = int.MinValue;
                private int yMin = int.MaxValue;
                private int yMax = int.MinValue;
            #endregion Private data.
            #region Public/Protected data.
                /// <summary>
                /// Xmin
                /// </summary>
                public int XMin
                {
                    get { return xMin; }
                }
                /// <summary>
                /// Xmax
                /// </summary>
                public int XMax
                {
                    get { return xMax; }
                }
                /// <summary>
                /// Ymin
                /// </summary>
                public int YMin
                {
                    get { return yMin; }
                }
                /// <summary>
                /// Ymax
                /// </summary>
                public int YMax
                {
                    get { return yMax; }
                }
                /// <summary>
                /// The median value of the x coordinate shape/texture
                /// </summary>
                public float xMedian = 0f;
                /// <summary>
                /// The median value of the y coordinate shape/texture
                /// </summary>
                public float yMedian = 0f;
                /// <summary>
                /// The median value of the x coordinate silhouette
                /// </summary>
                protected float xMedianSilhouette = 0f;
                /// <summary>
                /// The median value of the y coordinate silhouette
                /// </summary>
                protected float yMedianSilhouette = 0f;
                /// <summary>
                /// The area of the descriptor bounding box.
                /// </summary>
                public int BoundingBoxArea
                {
                    get { return (yMax - yMin + 1) * (xMax - xMin + 1); }
                }
                /// <summary>
                /// The collection of pixel participating in the silhouette, shape and texture 
                /// </summary>
                protected Pixel[] ParticipatingPixels = new Pixel[0];
            #endregion Public/Protected data.
        #endregion Properties

        #region Interfaces implementation
            /// <summary>
            /// Collection of Criteria applied to this descriptor.
            /// </summary>
            /// <value></value>
            public override ICriterion[] Criteria 
            {
                get 
                {
                    return _criteria;
                }
                set 
                {
                    if (value == null) 
                    {
                        _criteria = new ICriterion[0];
                    }
                    else
                    {
                        _criteria = value;
                    }
                }
            }
            /// <summary>
            /// The Descriptor's bounding rectangle 
            /// </summary>
            /// <value></value>
            public override RenderRect BoundingBox
            {
                get
                {
                    return new RenderRect(xMin, yMin, xMax, yMax);
                }
            }
            /// <summary>
            /// Compute all Descriptors for the bitmap.
            /// </summary>
            /// <param name="silhouetteExtraPixels">The pixels participating in the silhouette</param>
            public override void ComputeDescriptor(Pixel[] silhouetteExtraPixels)
            {
                xMedianSilhouette = 0;
                yMedianSilhouette = 0;
                IColor colorSilhouette = new ColorDouble();

                if (silhouetteExtraPixels != null)
                {
                    foreach (Pixel silPixel in silhouetteExtraPixels)
                    {
                        // Silhouette only stuff
                        xMedianSilhouette += silPixel.X;
                        yMedianSilhouette += silPixel.Y;

                        // compute color average
                        colorSilhouette.ExtendedAlpha += silPixel.Color.ExtendedAlpha;
                        colorSilhouette.ExtendedRed += silPixel.Color.ExtendedRed;
                        colorSilhouette.ExtendedGreen += silPixel.Color.ExtendedGreen;
                        colorSilhouette.ExtendedBlue += silPixel.Color.ExtendedBlue;
                    }
                }

                xMedianSilhouette += xMedian * ParticipatingPixels.Length;
                yMedianSilhouette += yMedian * ParticipatingPixels.Length;
                IColor colorClone = (IColor)((ColorDouble)DescriptorDependentObjects["shapecoloraverage"]).Clone();
                colorSilhouette.ExtendedAlpha += colorClone.ExtendedAlpha * ParticipatingPixels.Length;
                colorSilhouette.ExtendedRed += colorClone.ExtendedRed * ParticipatingPixels.Length;
                colorSilhouette.ExtendedGreen += colorClone.ExtendedGreen * ParticipatingPixels.Length;
                colorSilhouette.ExtendedBlue += colorClone.ExtendedBlue * ParticipatingPixels.Length;

                Normalize(((silhouetteExtraPixels == null) ? ParticipatingPixels.Length : silhouetteExtraPixels.Length + ParticipatingPixels.Length), ref xMedianSilhouette, ref yMedianSilhouette, colorSilhouette);
                DescriptorDependentObjects["silhouettecoloraverage"] = colorSilhouette;
            }
            /// <summary>
            /// Determine the distances between this and other descriptor passed in using the list of criteria passed in
            /// </summary>
            /// <param name="momentDescriptor">The descriptor to compare against</param>
            /// <returns>an hashtable containing the ICriterion type as key and distance to descriptor as value.</returns>
            public override Hashtable DistancesToDescriptor(IDescriptor momentDescriptor)
            {
                if (momentDescriptor == null)
                {
                    throw new ArgumentNullException("cartersianMomentDescriptor", "Descriptor passed in cannot be null");
                }
                if (momentDescriptor.GetType() != this.GetType())
                {
                    throw new InvalidCastException("Can only compare similar descriptor, descriptor passed in must be of type '" + this.GetType().ToString() + "' (type '" + momentDescriptor.GetType().ToString() + "' was passed in) ");
                }

                Hashtable retVal = new Hashtable(Criteria.Length);
                foreach (ICriterion criterion in Criteria)
                {
                    criterion.Pass(this, momentDescriptor);
                    retVal.Add(criterion.GetType(), criterion.DistanceBetweenDescriptors);
                }
                return retVal;
            }
            /// <summary>
            /// Add all the pixel participating in the shape
            /// </summary>
            /// <param name="pixels">The collection of "Pixel" paricipating in the shape</param>
            public override void SetParticipatingPixels(Pixel[] pixels)
            {
                ParticipatingPixels = pixels;
                IColor shapeColorAverage = (IColor)DescriptorDependentObjects["shapecoloraverage"];
                xMedian = 0f;
                yMedian = 0f;

                foreach (Pixel entry in pixels)
                {
                    xMedian += entry.X;
                    yMedian += entry.Y;

                    // compute color average
                    shapeColorAverage.ExtendedAlpha += entry.Color.ExtendedAlpha;
                    shapeColorAverage.ExtendedRed += entry.Color.ExtendedRed;
                    shapeColorAverage.ExtendedGreen += entry.Color.ExtendedGreen;
                    shapeColorAverage.ExtendedBlue+= entry.Color.ExtendedBlue;

                    if (entry.X > xMax) xMax = entry.X;
                    if (entry.X < xMin) xMin = entry.X;
                    if (entry.Y > yMax) yMax = entry.Y;
                    if (entry.Y < yMin) yMin = entry.Y;
                }
                Normalize(pixels.Length, ref xMedian, ref yMedian, shapeColorAverage);
            }
/*
            /// <summary>
            /// Adds a pixel to the Silhouette descriptor.
            /// </summary>
            /// <param name="pixels">The collection of pixels to add </param>
            protected internal void AddParticipatingPixelsSilhouette(Pixel[] pixels)
            {
                for (int t = 0; t < pixels.Length; t++)
                {
                    _xMedianSilhouette += pixels[t].X;
                    _yMedianSilhouette += pixels[t].Y;
                    _weightSilhouette += 1.0f;

                    // compute color average
                    IColor colorSilhouette = _silhouetteColorAverage;
                    colorSilhouette.ExtendedAlpha += pixels[t].Color.ExtendedAlpha;
                    colorSilhouette.ExtendedRed += pixels[t].Color.ExtendedRed;
                    colorSilhouette.ExtendedGreen += pixels[t].Color.ExtendedGreen;
                    colorSilhouette.ExtendedBlue += pixels[t].Color.ExtendedBlue;
                }
            }
*/
        #endregion Interfaces implementation

        #region Constructors
            /// <summary>
            /// Create a new instance of this type
            /// </summary>
            protected MomentDescriptorBase() : base()
            {
                DescriptorDependentObjects.Add("silhouette", new DescriptorSquareMatrix());
                DescriptorDependentObjects.Add("shape", new DescriptorSquareMatrix());
                DescriptorDependentObjects.Add("texture", new DescriptorSquareMatrix());
                DescriptorDependentObjects.Add("shapecoloraverage", new ColorDouble());
                DescriptorDependentObjects.Add("silhouettecoloraverage", new ColorDouble());
            }
            /// <summary>
            /// Create a new instance of this type -- constructor defined for serializatiom
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            protected MomentDescriptorBase(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                xMin = (int)info.GetInt32("xMin");
                yMin = (int)info.GetInt32("yMin");
                xMax = (int)info.GetInt32("xMax");
                yMax = (int)info.GetInt32("yMax");
                xMedian = (float)info.GetDouble("xMedian");
                yMedian = (float)info.GetDouble("yMedian");
                xMedianSilhouette = (float)info.GetDouble("xMedianSilhouette");
                yMedianSilhouette = (float)info.GetDouble("yMedianSilhouette");
                DescriptorSquareMatrix silhouette = (DescriptorSquareMatrix)info.GetValue("Silhouette", typeof(DescriptorSquareMatrix));
                DescriptorSquareMatrix shape = (DescriptorSquareMatrix)info.GetValue("Shape", typeof(DescriptorSquareMatrix));
                DescriptorSquareMatrix texture = (DescriptorSquareMatrix)info.GetValue("Texture", typeof(DescriptorSquareMatrix));
                ColorDouble shapeColorAverage = (ColorDouble)info.GetValue("ShapeColorAverage", typeof(ColorDouble));
                ColorDouble silhouetteColorAverage = (ColorDouble)info.GetValue("SilhouetteColorAverage", typeof(ColorDouble));
                DescriptorDependentObjects.Add("silhouette", silhouette);
                DescriptorDependentObjects.Add("shape", shape);
                DescriptorDependentObjects.Add("texture", texture);
                DescriptorDependentObjects.Add("shapecoloraverage",shapeColorAverage);
                DescriptorDependentObjects.Add("silhouettecoloraverage", silhouetteColorAverage);

            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Get the Maximum distance between the median and the corners
            /// </summary>
            /// <returns></returns>
            protected internal float GetMaxDistance(float medianX, float medianY)
            {
                float[] cornersX = new float[] { xMin-medianX, xMax-medianX, xMin-medianX, xMax-medianX};
                float[] cornersY = new float[] { yMin-medianY, yMin-medianY, yMax-medianY, yMax-medianY };

                // Get the maximum distance between median and extreme point
                // Note : if point are at the same distance, top left is considered to be the further
                float dist = float.MinValue;
                float temp = dist; 
                for (int t = 0; t < cornersX.Length; t++)
                {
                    temp = (float)Math.Sqrt (cornersX[t] * cornersX[t] + cornersY[t] * cornersY[t]);
                    if (temp > dist) { dist = temp; }
                }
                return dist;
            }
            /// <summary>
            /// Normalizes the xMedian and yMedian fields according
            /// to the descriptor weight.
            /// </summary>
            private void Normalize(int pixelCount , ref float medianX, ref float medianY, IColor colorAverage)
            {
                if (pixelCount != 0.0)
                {
                    medianX /= pixelCount;
                    medianY /= pixelCount;

                    // Shape color
                    colorAverage.ExtendedAlpha  /= pixelCount;
                    colorAverage.ExtendedRed    /= pixelCount;
                    colorAverage.ExtendedGreen  /= pixelCount;
                    colorAverage.ExtendedBlue   /= pixelCount;
                }
            }
            /// <summary>
            /// ISerializable unique Method implementation, called by the formatter
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                object[,] properties = new object[,] { { "xMin", xMin }, { "yMin", yMin }, { "xMax", xMax }, { "yMax", yMax }, { "xMedian", xMedian }, { "yMedian", yMedian }, { "xMedianSilhouette", xMedianSilhouette }, { "yMedianSilhouette", yMedianSilhouette }, { "Silhouette", DescriptorDependentObjects["silhouette"] }, { "Shape", DescriptorDependentObjects["shape"] }, { "Texture", DescriptorDependentObjects["texture"] }, { "SilhouetteColorAverage", DescriptorDependentObjects["silhouettecoloraverage"] }, { "ShapeColorAverage", DescriptorDependentObjects["shapecoloraverage"] } };
                for (int index = 0; index <= properties.GetUpperBound(0); index++)
                {
                    info.AddValue((string)properties[index, 0], properties[index, 1]);
                }
            }
        #endregion Methods
    }
}
