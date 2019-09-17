// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;  
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    ///<summary>
    /// Create a skew X transformation.
    ///</summary>
    public sealed partial class SkewTransform : Transform
    {
        ///<summary>
        /// 
        ///</summary>
        public SkewTransform()
        {
        }

        ///<summary>
        ///
        ///</summary>
        public SkewTransform(double angleX, double angleY)
        {
            AngleX = angleX;
            AngleY = angleY;
        }

        ///<summary>
        ///
        ///</summary>
        public SkewTransform(double angleX, double angleY, double centerX, double centerY) : this(angleX, angleY)
        {
            CenterX = centerX;
            CenterY = centerY;
        }

        ///<summary>
        /// Return the current transformation value.
        ///</summary>
        public override Matrix Value
        {
            get 
            {
                ReadPreamble();
                
                Matrix matrix = new Matrix();

                double angleX = AngleX;
                double angleY = AngleY;
                double centerX = CenterX;
                double centerY = CenterY;

                bool hasCenter = centerX != 0 || centerY != 0;
                
                if (hasCenter)
                {
                    matrix.Translate(-centerX, -centerY);
                }

                matrix.Skew(angleX, angleY);

                if (hasCenter)
                {
                    matrix.Translate(centerX, centerY);
                }

                return matrix;
            }       
        }
        
        ///<summary>
        /// Returns true if transformation matches the identity transform.
        ///</summary>
        internal override bool IsIdentity
        {
            get 
            {
                return AngleX == 0 && AngleY == 0 && CanFreeze;
            }
        }        
    }
}
