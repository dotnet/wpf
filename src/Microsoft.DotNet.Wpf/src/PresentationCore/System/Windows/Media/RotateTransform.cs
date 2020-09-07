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
    /// Create a rotation transformation in degrees.
    ///</summary>
    public sealed partial class RotateTransform : Transform
    {
        ///<summary>
        ///
        ///</summary>
        public RotateTransform()
        {
        }

        ///<summary>
        /// Create a rotation transformation in degrees.
        ///</summary>
        ///<param name="angle">The angle of rotation in degrees.</param>
        public RotateTransform(double angle)
        {
            Angle = angle;
        }

        ///<summary>
        /// Create a rotation transformation in degrees.
        ///</summary>
        public RotateTransform(
            double angle,
            double centerX,
            double centerY
            ) : this(angle)
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
                
                Matrix m = new Matrix();

                m.RotateAt(Angle, CenterX, CenterY);

                return m;
            }
        }
        
        ///<summary>
        /// Returns true if transformation matches the identity transform.
        ///</summary>
        internal override bool IsIdentity
        {
            get 
            {
                return Angle == 0 && CanFreeze;
            }
        }
    }
}
