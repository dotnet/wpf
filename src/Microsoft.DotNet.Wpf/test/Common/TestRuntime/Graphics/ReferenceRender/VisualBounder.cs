// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{   
    /// <summary>
    ///  Calculate Bounds on Visual3Ds
    /// </summary>
    public class VisualBounder
    {
        /// <summary>
        /// Find the bounds of a visual NOT including this visual's Transform.
        /// This is how a Visual3D determines its content's bounds.
        /// </summary>
        public static Rect3D GetContentBounds(Visual3D visual)
        {
            if (visual is ModelVisual3D)
            {
                return ModelBounder.GetBounds(((ModelVisual3D)visual).Content);
            }

            return Rect3D.Empty;
        }

        /// <summary>
        /// Find the bounds of the children of a visual NOT including this visual's Transform.
        /// </summary>
        public static Rect3D GetChildrenBounds(Visual3D visual)
        {
            Rect3D bounds = Rect3D.Empty;

            if (visual is ModelVisual3D)
            {
                foreach (Visual3D v in ((ModelVisual3D)visual).Children)
                {
                    bounds.Union(GetChildrenBoundsRecursive((ModelVisual3D)v, Matrix3D.Identity));
                }
            }

            return bounds;
        }

        private static Rect3D GetChildrenBoundsRecursive(ModelVisual3D visual, Matrix3D tx)
        {
            Rect3D bounds = Rect3D.Empty;
            Matrix3D currentTransform = MatrixUtils.Multiply(MatrixUtils.Value(visual.Transform), tx);

            if (visual.Content != null)
            {
                bounds = ModelBounder.CalculateBounds(visual.Content, currentTransform);
            }

            foreach (Visual3D v in visual.Children)
            {
                if (v is ModelVisual3D)
                {
                    bounds.Union(GetChildrenBoundsRecursive((ModelVisual3D)v, currentTransform));
                }
            }

            return bounds;
        }
    }
}