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
    /// Polar implementation of IDescriptor 
    /// </summary>
    [SerializableAttribute()]
    [XmlRootAttribute("PolarMomentDescriptor")]
    internal class PolarMomentDescriptor: MomentDescriptorBase, ISerializable
    {
        #region Properties
            /// <summary>
            /// The angle between the horizon and the max distance (median to extreme point)
            /// </summary>
            public float AngleThetaMax = 0f;
        #endregion Properties

        #region Constructor
            /// <summary>
            /// Creates a new PolarMomentDescriptor instance -- needed for Serialization, will be called by the formatter on Deserialization
            /// </summary>
            public PolarMomentDescriptor(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                AngleThetaMax = (float)info.GetValue("AngleThetaMax", typeof(float));
            }
            /// <summary>
            /// Creates a new PolarMomentDescriptor instance.
            /// </summary>
            public PolarMomentDescriptor()
            {
            }
        #endregion Constructor

        #region IDescriptor interface implementation
            /// <summary>
            /// Compute all Descriptors for the bitmap.
            /// </summary>
            /// <param name="silhouetteExtraPixels">The pixels participating in the silhouette</param>
            public override void ComputeDescriptor(Pixel[] silhouetteExtraPixels)
            {
                throw new NotImplementedException("TODO !");
            }
            /// <summary>
            /// Implement the ISerializable unique interface -- will be called by the formatter on serialization
            /// </summary>
            /// <param name="info">The SeralizationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("AngleThetaMax", AngleThetaMax);
            }
        #endregion IDescriptor interface implementation
    }
}

