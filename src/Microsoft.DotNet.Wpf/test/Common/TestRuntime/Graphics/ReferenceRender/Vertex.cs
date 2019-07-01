// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// A container for per-vertex triangle data for test rendering    
    /// </summary>
    internal class Vertex
    {
        // eye space
        public Point4D Position;
        public Vector3D Normal;

        // projected space
        public Point3D ProjectedPosition;

        // per-vertex data
        public Point TextureCoordinates;
        public Color Color;
        public Color ColorTolerance;

        // interpolation aids
        public double W;
        public double DistanceFromLine;
        public double DistanceFromLine2D;

        // precomputed lighting
        public Color[] PrecomputedLight;
        public Color[] PrecomputedLightTolerance;

        // UV-Tolerance
        public Point UVToleranceMin;
        public Point UVToleranceMax;

        // model space
        public Point3D ModelSpacePosition;
        public Vector3D ModelSpaceNormal;

        public double MipMapFactor;

        // Convenience accessors for computed values
        public double OneOverW
        {
            get
            {
                return 1.0 / W;
            }
        }

        public double UOverW
        {
            get
            {
                return TextureCoordinates.X / W;
            }
        }

        public double VOverW
        {
            get
            {
                return TextureCoordinates.Y / W;
            }
        }

        public double U
        {
            get
            {
                return TextureCoordinates.X;
            }
        }

        public double V
        {
            get
            {
                return TextureCoordinates.Y;
            }
        }

        public double PositionZ
        {
            get
            {
                return (Position.W == 1.0) ? Position.Z : Position.Z / Position.W;
            }
        }

        public Point3D PositionAsPoint3D
        {
            get
            {
                return MathEx.DivideByW(Position);
            }
        }

        virtual public void Project(Matrix3D projectionMatrix)
        {
            // After we project, we need to divide by W,
            // to allow perspective, but we save it as
            // well so that we can do perspective correct
            // interpolation later.
            Point4D projectedPoint = this.Position;
            projectedPoint *= projectionMatrix;
            this.W = projectedPoint.W;
            this.ProjectedPosition = MathEx.DivideByW(projectedPoint);
        }

    }
}
