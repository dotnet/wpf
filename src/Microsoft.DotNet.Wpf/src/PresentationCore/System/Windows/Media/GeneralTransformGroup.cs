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

namespace System.Windows.Media
{
    /// <summary>
    /// GeneralTrasnform group
    /// </summary>
    [ContentProperty("Children")]
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public sealed partial class GeneralTransformGroup : GeneralTransform
    {
        #region Constructors

        ///<summary>
        /// Default Constructor
        ///</summary>
        public GeneralTransformGroup() { }

        #endregion  

        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">input point</param>
        /// <param name="result">output point</param>
        /// <returns>True if the point is transformed successfully</returns>
        public override bool TryTransform(Point inPoint, out Point result)
        {
            result = inPoint;
            if ((Children == null) || (Children.Count == 0))
            {
                return false;
            }

            Point inP = inPoint;
            bool fPointTransformed = true;
            // transform the point through each of the transforms
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children.Internal_GetItem(i).TryTransform(inPoint, out result) == false)
                {
                    fPointTransformed = false;
                }

                inPoint = result;
            }

            return fPointTransformed;
        }

        /// <summary>
        /// Transforms the bounding box to the smallest axis aligned bounding box
        /// that contains all the points in the original bounding box
        /// </summary>
        /// <param name="rect">Input bounding rect</param>
        /// <returns>Transformed bounding rect</returns>
        public override Rect TransformBounds(Rect rect)
        {
            if ((Children == null) || (Children.Count == 0))
            {
                return rect;
            }

            Rect result = rect;
            for (int i = 0; i < Children.Count; i++)
            {
                result = Children.Internal_GetItem(i).TransformBounds(result);
            }

            return result;
        }

        /// <summary>
        /// Returns the inverse transform if it has an inverse, null otherwise
        /// </summary>        
        public override GeneralTransform Inverse
        {
            get
            {
                ReadPreamble();

                if ((Children == null) || (Children.Count == 0))
                {
                    return null;
                }

                GeneralTransformGroup group = new GeneralTransformGroup();
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    GeneralTransform g = Children.Internal_GetItem(i).Inverse;

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
        internal override Transform AffineTransform
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get
            {
                if ((Children == null) || (Children.Count == 0))
                {
                    return null;
                }

                Matrix matrix = Matrix.Identity;
                foreach (GeneralTransform gt in Children)
                {
                    Transform t = gt.AffineTransform;
                    if (t != null)
                    {
                        matrix *= t.Value;
                    }
                }                

                return new MatrixTransform(matrix);
            }
        }
}
}
