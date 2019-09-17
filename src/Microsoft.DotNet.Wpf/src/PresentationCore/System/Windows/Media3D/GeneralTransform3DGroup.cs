// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MS.Internal;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Windows.Media.Composition;
using System.Diagnostics;
using MS.Internal.PresentationCore;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// GeneralTransform3D group
    /// </summary>
    [ContentProperty("Children")]
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public sealed partial class GeneralTransform3DGroup : GeneralTransform3D
    {
        #region Constructors

        ///<summary>
        /// Default Constructor
        ///</summary>
        public GeneralTransform3DGroup() { }

        #endregion  

        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">input point</param>
        /// <param name="result">output point</param>
        /// <returns>True if the point is transformed successfully</returns>
        public override bool TryTransform(Point3D inPoint, out Point3D result)
        {
            result = inPoint;

            // cache the children to avoid a repeated DP access
            GeneralTransform3DCollection children = Children;
            
            if ((children == null) || (children.Count == 0))
            {
                return false;
            }

            bool pointTransformed = true;
            // transform the point through each of the transforms
            for (int i = 0, count = children.Count; i < count; i++)
            {
                if (children._collection[i].TryTransform(inPoint, out result) == false)
                {
                    pointTransformed = false;
                    break;
                }

                inPoint = result;
            }

            return pointTransformed;
        }
        
        /// <summary>
        /// Transforms the bounding box to the smallest axis aligned bounding box
        /// that contains all the points in the original bounding box
        /// </summary>
        /// <param name="rect">Input bounding rect</param>
        /// <returns>Transformed bounding rect</returns>
        public override Rect3D TransformBounds(Rect3D rect)
        {
            // cache the children to avoid a repeated DP access
            GeneralTransform3DCollection children = Children;
                        
            if ((children == null) || (children.Count == 0))
            {
                return rect;
            }

            Rect3D result = rect;
            for (int i = 0, count = children.Count; i < count; i++)
            {
                result = children._collection[i].TransformBounds(result);
            }

            return result;
        }

        /// <summary>
        /// Returns the inverse transform if it has an inverse, null otherwise
        /// </summary>        
        public override GeneralTransform3D Inverse
        {
            get
            {
                // cache the children to avoid a repeated DP access
                GeneralTransform3DCollection children = Children;
                            
                if ((children == null) || (children.Count == 0))
                {
                    return null;
                }

                GeneralTransform3DGroup group = new GeneralTransform3DGroup();
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    GeneralTransform3D g = children._collection[i].Inverse;

                    // if any of the transforms does not have an inverse,
                    // then the entire group does not have one
                    if (g == null)
                        return null;

                    group.Children.Add(g);
                }

                return group;
            }
        }

        /// <summary>
        /// Returns a best effort affine transform
        /// </summary>        
        internal override Transform3D AffineTransform
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get
            {
                // cache the children to avoid a repeated DP access
                GeneralTransform3DCollection children = Children;
                
                if ((children == null) || (children.Count == 0))
                {
                    return null;
                }

                Matrix3D matrix = Matrix3D.Identity;
                for (int i = 0, count = children.Count; i < count; i++)
                {
                    Transform3D t = children._collection[i].AffineTransform;
                    
                    t.Append(ref matrix);                    
                }                

                return new MatrixTransform3D(matrix);
            }
        }                     
    }
}
