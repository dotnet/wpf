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
    /// Calculate Bounds on Models
    /// </summary>
    public class ModelBounder
    {
        /// <summary>
        /// Find the bounds of a model taking into account its Transform property.
        /// </summary>
        public static Rect3D GetBounds(Model3D model)
        {
            return CalculateBounds(model, Matrix3D.Identity);
        }

        /// <summary>
        /// Find the bounds of a model taking into account its Transform * (some arbitrary transform - e.g. Projection)
        /// </summary>
        public static Rect3D CalculateBounds(Model3D model, Matrix3D tx)
        {
            if (model == null)
            {
                return Rect3D.Empty;
            }

            Rect3D bounds = Rect3D.Empty;
            model = model.CloneCurrentValue();
            Matrix3D newTx = MatrixUtils.Multiply(MatrixUtils.Value(model.Transform), tx);

            if (model is Model3DGroup)
            {
                foreach (Model3D m in ObjectUtils.GetChildren((Model3DGroup)model))
                {
                    bounds.Union(CalculateBounds(m, newTx));
                }
            }
            else if (model is GeometryModel3D)
            {
                bounds = CalculateBounds((GeometryModel3D)model, newTx);
            }
#if SSL
            else if ( model is ScreenSpaceLines3D )
            {
                bounds = CalculateBounds( (ScreenSpaceLines3D)model, tx );
            }
#endif
            else if (model is Light)
            {
                // Do nothing.  Light has no bounds.
            }
            else
            {
                throw new NotSupportedException("I do not calculate bounds on Model3D of type: " + model.GetType());
            }

            return bounds;
        }

        private static Rect3D CalculateBounds(GeometryModel3D model, Matrix3D tx)
        {
            if (model.Geometry is MeshGeometry3D)
            {
                return CalculateBounds((MeshGeometry3D)model.Geometry, tx);
            }
            else
            {
                throw new NotSupportedException("I do not calculate bounds on Geometry of type: " + model.Geometry.GetType());
            }
        }

        private static Rect3D CalculateBounds(MeshGeometry3D mesh, Matrix3D tx)
        {
            return CalculateBounds(mesh.Positions, tx);
        }


#if SSL
        private static Rect3D   CalculateBounds( ScreenSpaceLines3D lines, Matrix3D tx )
        {
            return CalculateBounds( lines.Points, tx );
        }
#endif

        private static Rect3D CalculateBounds(Point3DCollection points, Matrix3D tx)
        {
            double xMin = double.PositiveInfinity;
            double yMin = double.PositiveInfinity;
            double zMin = double.PositiveInfinity;
            double xMax = double.NegativeInfinity;
            double yMax = double.NegativeInfinity;
            double zMax = double.NegativeInfinity;

            foreach (Point3D p in points)
            {
                Point3D point = MatrixUtils.Transform(p, tx);
                if (point.X < xMin)
                {
                    xMin = point.X;
                }
                if (point.Y < yMin)
                {
                    yMin = point.Y;
                }
                if (point.Z < zMin)
                {
                    zMin = point.Z;
                }
                if (point.X > xMax)
                {
                    xMax = point.X;
                }
                if (point.Y > yMax)
                {
                    yMax = point.Y;
                }
                if (point.Z > zMax)
                {
                    zMax = point.Z;
                }
            }
            return new Rect3D(xMin, yMin, zMin, xMax - xMin, yMax - yMin, zMax - zMin);
        }
    }
}