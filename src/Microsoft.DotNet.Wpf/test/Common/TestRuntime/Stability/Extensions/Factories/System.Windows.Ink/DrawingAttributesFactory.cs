// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Ink;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create DrawingAttributes.
    /// </summary>
    internal class DrawingAttributesFactory : DiscoverableFactory<DrawingAttributes>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Matrix to set DrawingAttributes StylusTipTransform property.
        /// </summary>
        public Matrix Matrix { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a DrawingAttributes.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DrawingAttributes Create(DeterministicRandom random)
        {
            DrawingAttributes drawingAttributes = new DrawingAttributes();

            drawingAttributes.Color = random.NextStaticProperty<Color>(typeof(Colors));
            drawingAttributes.FitToCurve = random.NextBool();
            //Set Height and Width value (0, 100000].
            drawingAttributes.Height = (1 - random.NextDouble()) * 10000;
            drawingAttributes.Width = (1 - random.NextDouble()) * 10000;
            drawingAttributes.IgnorePressure = random.NextBool();
            drawingAttributes.IsHighlighter = random.NextBool();
            drawingAttributes.StylusTip = random.NextEnum<StylusTip>();
            if (ValidateMatrix()) //Set StylusTipTransform only if matrix is valid.
            {
                drawingAttributes.StylusTipTransform = Matrix;
            }

            return drawingAttributes;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// The matrix set to StylusTipTransform must be an invertible matrix.
        ///  -and-
        /// The OffsetX and OffsetY property of the matrix must be zero.
        /// </summary>
        private bool ValidateMatrix()
        {
            if (Matrix.OffsetX != 0 || Matrix.OffsetY != 0)
            {
                return false;
            }

            //If the determinant value of matrix is 0, then matrix isn't an invertible matrix.  
            if (CalculateMatrix() == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate determinant of matrix
        /// M11      M12      0
        /// M21      M22      0
        /// OffsetX  OffsetY  1
        /// </summary>
        private double CalculateMatrix()
        {
            return (Matrix.M11 * Matrix.M22) - (Matrix.M12 * Matrix.M21);
        }

        #endregion
    }
}
