// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical
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
    #endregion usings

    /// <summary>
    /// 4x4 Matrix containing the Descriptor value
    /// </summary>
    [SerializableAttribute()]
    public class DescriptorSquareMatrix
    {
        #region Properties
            #region Public Properties
                /// <summary>
                /// The  4x4 array containing the descriptor flatted into a 1 dimension array
                /// Note :
                ///     * Needs to be 1 dimension Array since XmlSerialization does not support multi dimensional array 
                ///     * Needs to be public for Xml Serialization
                /// </summary>
                [XmlArrayAttribute("MatrixDescriptor")]
                [XmlArrayItemAttribute("Entry", typeof(float))]
                public float[] FlatDescriptorMatrix = null;
            #endregion Public Properties
            #region Static Properties
                /// <summary>
                /// The Length of the Matrix
                /// </summary>
                [XmlIgnoreAttribute()]
                public static readonly int Length = 4;
            #endregion Static Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instantiate a new DescriptorSquareMatrix object 
            /// </summary>
            public DescriptorSquareMatrix()
            {
                FlatDescriptorMatrix = new float[Length * Length];
            }
        #endregion Constructors

        #region Public Methods
            /// <summary>
            /// Get/set the value at the specified location
            /// </summary>
            /// <param name="x">The x value</param>
            /// <param name="y">The y value</param>
            /// <returns>The float located at the x,y location</returns>
            [XmlIgnoreAttribute()]
            public float this[int x, int y]
            {
                get
                {
                    if (x > Length || y > Length || x < 0 || y < 0)
                    {
                        throw new IndexOutOfRangeException("index must be between 0 and " + Length);
                    }

                    return FlatDescriptorMatrix[x + y * Length];
                }
                set
                {
                    if (x > Length || y > Length || x < 0 || y < 0)
                    {
                        throw new IndexOutOfRangeException("index must be between 0 and " + Length);
                    }

                    FlatDescriptorMatrix[x + y * Length] = value;
                }
            }
        #endregion Public Methods

        #region Implicit casting implementation
            /// <summary>
            /// Convert a DescriptorSquareMatrix into a float[,]
            /// </summary>
            /// <param name="descriptor">The DescriptorSquareMatrix object to convert</param>
            /// <returns>A float[,] object</returns>
            public static implicit operator float[,](DescriptorSquareMatrix descriptor)
            {
                float[,] retVal = new float[Length, Length];

                for (int y = 0; y < Length; y++)
                {
                    for (int x = 0; x < Length; x++)
                    {
                        retVal[x, y] = descriptor[x, y];
                    }
                }

                return retVal;
            }
            /// <summary>
            /// Convert a float[,] into a DescriptorSquareMatrix
            /// </summary>
            /// <param name="array">The float[,] object to convert</param>
            /// <returns>A DescriptorSquareMatrix object</returns>
            public static implicit operator DescriptorSquareMatrix(float[,] array)
            {
                DescriptorSquareMatrix retVal = new DescriptorSquareMatrix();

                if (array.GetLength(0) != Length || array.GetLength(1) != Length)
                {
                    throw new InvalidCastException("Array passed in must be a square array or length '" + Length + "' by '" + Length + "' ");
                }

                for (int y = 0; y < Length; y++)
                {
                    for (int x = 0; x < Length; x++)
                    {
                        retVal[x, y] = array[x, y];
                    }
                }

                return retVal;
            }
        #endregion Implicit casting implementation
    }
}
