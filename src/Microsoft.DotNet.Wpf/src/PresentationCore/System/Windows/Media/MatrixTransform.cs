// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using System.Windows.Media;
using System;
using System.Windows;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Security;
using System.Collections;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;  
using System.Windows.Media.Composition;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    ///<summary>
    /// Create an arbitrary matrix transformation.
    ///</summary>
    public sealed partial class MatrixTransform : Transform
    {
        #region Constructors

        ///<summary>
        ///
        ///</summary>
        public MatrixTransform() 
        {
        }
        
        ///<summary>
        /// Create an arbitrary matrix transformation.
        ///</summary>
        ///<param name="m11">Matrix value at position 1,1</param>
        ///<param name="m12">Matrix value at position 1,2</param>
        ///<param name="m21">Matrix value at position 2,1</param>
        ///<param name="m22">Matrix value at position 2,2</param>
        ///<param name="offsetX">Matrix value at position 3,1</param>
        ///<param name="offsetY">Matrix value at position 3,2</param>
        public MatrixTransform(
            double m11, 
            double m12,
            double m21, 
            double m22,
            double offsetX, 
            double offsetY
            )
        {
            Matrix = new Matrix(m11, m12, m21, m22, offsetX, offsetY);
        }

        ///<summary>
        /// Create a matrix transformation from constant transform.
        ///</summary>
        ///<param name="matrix">The constant matrix transformation.</param>
        public MatrixTransform(Matrix matrix)
        {
            Matrix = matrix;
        }

        #endregion

        ///<summary>
        /// Return the current transformation value.
        ///</summary>
        public override Matrix Value
        {
            get 
            {
                ReadPreamble();
                
                return Matrix;
            }
        }
        
        #region Internal Methods

        ///<summary>
        /// Returns true if transformation matches the identity transform.
        ///</summary>
        internal override bool IsIdentity
        {
            get 
            {
                return Matrix.IsIdentity && CanFreeze;
            }
        }

        internal override bool CanSerializeToString() { return CanFreeze; }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            if (!CanSerializeToString())
            {
                return base.ConvertToString(format, provider);
            }

            return ((IFormattable)Matrix).ToString(format, provider);
        }
        
        internal override void TransformRect(ref Rect rect)
        {
            Matrix matrix = Matrix;
            MatrixUtil.TransformRect(ref rect, ref matrix);
        }

        internal override void MultiplyValueByMatrix(ref Matrix result, ref Matrix matrixToMultiplyBy)
        {
            result = Matrix;
            MatrixUtil.MultiplyMatrix(ref result, ref matrixToMultiplyBy);
        }

        #endregion Internal Methods
    }
}
