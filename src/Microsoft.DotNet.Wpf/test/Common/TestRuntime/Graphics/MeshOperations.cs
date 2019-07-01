// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// Static helper class that holds mesh utilities
    /// </summary>
    public class MeshOperations
    {
        private MeshOperations()
        {
            // Helper function container class, so prevent instantiation.
        }

        /// <summary/>
        public static void RemoveNullFields(MeshGeometry3D mesh)
        {
            if (mesh.Positions == null)
            {
                mesh.Positions = new Point3DCollection();
            }
            if (mesh.Normals == null)
            {
                mesh.Normals = new Vector3DCollection();
            }
            if (mesh.TextureCoordinates == null)
            {
                mesh.TextureCoordinates = new PointCollection();
            }
            if (mesh.TriangleIndices == null)
            {
                mesh.TriangleIndices = new Int32Collection();
            }
        }

        /// <summary/>
        public static void RemoveBogusTriangles(MeshGeometry3D mesh)
        {
            // Avalon bails out at the sign of the first bad TriangleIndex.
            // So we will remove all TriangleIndices from then on.

            int maxPositionIndex = mesh.Positions.Count - 1;
            int numIndices = mesh.TriangleIndices.Count;
            int clipIndex = numIndices;
            for (int n = 0; n < numIndices; n++)
            {
                int positionIndex = mesh.TriangleIndices[n];
                if (positionIndex < 0 || maxPositionIndex < positionIndex)
                {
                    clipIndex = n;
                    break;
                }
            }
            while (clipIndex < numIndices)
            {
                mesh.TriangleIndices.RemoveAt(clipIndex);
                numIndices--;
            }
        }

        /// <summary/>
        public static void GenerateTriangleIndices(MeshGeometry3D mesh)
        {
            for (int n = 0; n < mesh.Positions.Count; n++)
            {
                mesh.TriangleIndices.Add(n);
            }
        }

        /// <summary/>
        public static void GenerateTextureCoordinates(MeshGeometry3D mesh)
        {
            for (int n = 0; n < mesh.Positions.Count; n++)
            {
                mesh.TextureCoordinates.Add(new Point(0, 0));
            }
        }

        /// <summary/>
        public static void GenerateNormals(MeshGeometry3D mesh, bool clockwise)
        {
            int numPositions = mesh.Positions.Count;
            Vector3D[] normals = new Vector3D[numPositions];

            int numTriangles = mesh.TriangleIndices.Count / 3;
            Vector3D normal, lineAB, lineAC;
            for (int n = 0; n < numTriangles; n++)
            {
                int indexA = mesh.TriangleIndices[n * 3];
                int indexB = mesh.TriangleIndices[n * 3 + 1];
                int indexC = mesh.TriangleIndices[n * 3 + 2];

                // calculate face normal
                lineAB = mesh.Positions[indexB] - mesh.Positions[indexA];
                lineAC = mesh.Positions[indexC] - mesh.Positions[indexA];

                // support winding order
                if (clockwise)
                {
                    normal = MathEx.CrossProduct(lineAC, lineAB);
                }
                else
                {
                    normal = MathEx.CrossProduct(lineAB, lineAC);
                }

                // accumulate vertex normal
                normals[indexA] += normal;
                normals[indexB] += normal;
                normals[indexC] += normal;
            }

            // Normalize and add the ones we have not yet specified in the mesh.
            for (int i = mesh.Normals.Count; i < numPositions; i++)
            {
                mesh.Normals.Add(MathEx.Normalize(normals[i]));
            }
        }

        /// <summary/>
        public static void CalculateRefractedUV(MeshGeometry3D mesh, Vector3D viewDirection, double indexOfRefraction)
        {
            viewDirection = MathEx.Normalize(viewDirection);

            for (int i = 0; i < mesh.Positions.Count; i++)
            {
                Point3D position = mesh.Positions[i];
                // Real Time Rendering (2nd edition), 6.28 - p246
                Vector3D refracted = MathEx.Normalize(-1.0 * indexOfRefraction * mesh.Normals[i] + viewDirection);
                // make refracted ray intersect ground plane
                refracted = refracted * (position.Z / refracted.Z);
                //refracted = viewDirection;
                refracted.X += position.X;
                refracted.Y += position.Y;
                double Ucoord = refracted.X;
                double Vcoord = refracted.Y;
                mesh.TextureCoordinates[i] = new Point(Ucoord, -Vcoord);
            }
        }
    }
}
