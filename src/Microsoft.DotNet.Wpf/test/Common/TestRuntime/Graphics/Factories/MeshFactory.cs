// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using Microsoft.Test.Graphics.TestTypes;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class MeshFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D MakeMesh(string mesh)
        {
            string[] parsedMesh = mesh.Split(' ');

            switch (parsedMesh[0])
            {
                case "DoubleSidedTriangle": return DoubleSidedTriangle;
                case "SingleFrontFacingTriangle": return SingleFrontFacingTriangle;
                case "SingleBackFacingTriangle": return SingleBackFacingTriangle;
                case "UnitPlaneTriangle": return UnitPlaneTriangle;
                case "DoubleTriangleMesh": return DoubleTriangleMesh;
                case "FullScreenMesh": return FullScreenMesh;
                case "PlaneXZ": return PlaneXZ;
                case "SimpleCubeMesh": return SimpleCubeMesh;
                case "SmoothEdgeCube": return SmoothEdgeCube;
                case "HardEdgeCube": return HardEdgeCube;
                case "OverlappingPlanes": return OverlappingPlanes;
                case "FrontAndBack": return FrontAndBack;
                case "BackAndFront": return BackAndFront;
                case "BadPositions1": return BadPositions1;
                case "BadPositions2": return BadPositions2;
                case "BadPositions4": return BadPositions4;
                case "BadPositions5": return BadPositions5;
                case "NullPositions1": return NullPositions1;
                case "NullPositions2": return NullPositions2;
                case "NullPositions3": return NullPositions3;
                case "Null": return null;
                case "BadIndex1": return BadIndex1;
                case "BadIndex2": return BadIndex2;
                case "BadIndex3": return BadIndex3;
                case "BadIndex4": return BadIndex4;
                case "BadIndex5": return BadIndex5;
                case "BadIndex6": return BadIndex6;
                case "BadIndexNoNormals": return BadIndexNoNormals;
                case "BadIndexNullNormals": return BadIndexNullNormals;
                case "BadIndexMissingSomeNormals": return BadIndexMissingSomeNormals;
                case "Positions": return Positions;
                case "PositionsElseNull": return PositionsElseNull;
                case "PositionsNormals": return PositionsNormals;
                case "PositionsUV": return PositionsUV;
                case "PositionsIndices": return PositionsIndices;
                case "PositionsNormalsUV": return PositionsNormalsUV;
                case "PositionsNormalsIndices": return PositionsNormalsIndices;
                case "PositionsUVIndices": return PositionsUVIndices;
                case "PositionsNormalsUVIndices": return PositionsNormalsUVIndices;
                case "DefaultSphere": return Sphere(25, 50, 1.0);

                case "Spiral":
                    return Spiral(
                        StringConverter.ToInt(parsedMesh[1]),
                        StringConverter.ToInt(parsedMesh[2]),
                        StringConverter.ToDouble(parsedMesh[3]),
                        StringConverter.ToDouble(parsedMesh[4]));

                case "Sphere":
                    return Sphere(StringConverter.ToInt(parsedMesh[1]), StringConverter.ToInt(parsedMesh[2]), StringConverter.ToDouble(parsedMesh[3]));

                case "HolySphere":
                    return HolySphere(StringConverter.ToInt(parsedMesh[1]), StringConverter.ToInt(parsedMesh[2]), StringConverter.ToDouble(parsedMesh[3]), StringConverter.ToDouble(parsedMesh[4]));

                case "PlaneXY":
                    return PlaneXY(
                        StringConverter.ToPoint(parsedMesh[1]),
                        StringConverter.ToPoint(parsedMesh[2]),
                        StringConverter.ToDouble(parsedMesh[3]),
                        StringConverter.ToInt(parsedMesh[4]),
                        StringConverter.ToInt(parsedMesh[5]));

                case "CreateRandomGrid":
                    return CreateRandomGrid(StringConverter.ToInt(parsedMesh[1]), StringConverter.ToInt(parsedMesh[2]), StringConverter.ToDouble(parsedMesh[3]));

                case "CreateFlatGrid":
                    return CreateFlatGrid(StringConverter.ToInt(parsedMesh[1]), StringConverter.ToInt(parsedMesh[2]), StringConverter.ToDouble(parsedMesh[3]));

                case "CreateFlatGridUV":
                    return CreateFlatGridUV(
                        StringConverter.ToInt(parsedMesh[1]),
                        StringConverter.ToInt(parsedMesh[2]),
                        StringConverter.ToDouble(parsedMesh[3]),
                        StringConverter.ToPoint(parsedMesh[4]),
                        StringConverter.ToPoint(parsedMesh[5]));

                case "CreateFlatGridRandomUV":
                    return CreateFlatGridRandomUV(
                        StringConverter.ToInt(parsedMesh[1]),
                        StringConverter.ToInt(parsedMesh[2]),
                        StringConverter.ToDouble(parsedMesh[3]),
                        StringConverter.ToPoint(parsedMesh[4]),
                        StringConverter.ToPoint(parsedMesh[5]),
                        StringConverter.ToInt(parsedMesh[6])
                        );

                case "CreateGridFromImage":
                    return CreateGridFromImage(parsedMesh[1], StringConverter.ToDouble(parsedMesh[2]));

                case "CreateFlatDisc":
                    return CreateFlatDisc(StringConverter.ToInt(parsedMesh[1]));
            }
            throw new ArgumentException("Specified mesh (" + mesh + ") cannot be created");
        }

        /// <summary>
        /// A double-sided triangle (not culled unless outside of camera frustum)
        /// </summary>
        public static MeshGeometry3D DoubleSidedTriangle
        {
            get
            {
                //  2,4 +
                //      |\
                //      |  \      // both winding orders are added
                //      |    \
                //  0,3 +-----+ 1,5
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));

                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 1));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(3);
                mesh.TriangleIndices.Add(4);
                mesh.TriangleIndices.Add(5);

                return mesh;
            }
        }

        /// <summary>
        /// A triangle facing the +z side (culled if camera is on -z side)
        /// </summary>
        public static MeshGeometry3D SingleFrontFacingTriangle
        {
            get
            {
                //  2 +
                //    |\
                //    |  \
                //    |    \
                //  0 +-----+ 1
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));

                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);

                return mesh;
            }
        }

        /// <summary>
        /// A triangle facing the -z side (culled if camera is on +z side)
        /// </summary>
        public static MeshGeometry3D SingleBackFacingTriangle
        {
            get
            {
                //  1 +
                //    |\
                //    |  \     // invisible from front
                //    |    \
                //  0 +-----+ 2
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));

                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));

                mesh.TextureCoordinates.Add(new Point(1, 1));    // Beware! Becuase this triangle faces
                mesh.TextureCoordinates.Add(new Point(1, 0));    // backwards, the texture coords are
                mesh.TextureCoordinates.Add(new Point(0, 1));    // different from SingleTriangleMesh

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);

                return mesh;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D UnitPlaneTriangle
        {
            get
            {
                //  1 +
                //    |\
                //    |  \
                //    |    \
                //  2 +-----+ 0  // vertex 2 is 1 unit away from origin (in +z direction)
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(1, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(0, 0, 1));

                mesh.Normals.Add(new Vector3D(1, 1, 1));
                mesh.Normals.Add(new Vector3D(1, 1, 1));
                mesh.Normals.Add(new Vector3D(1, 1, 1));

                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(0, 1));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);

                return mesh;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D DoubleTriangleMesh
        {
            get
            {
                //        2 +
                //          |\
                //          |  \
                //         0|    \
                //  3 +-----+-----+ 1
                //     \    |
                //       \  |
                //         \|
                //        4 +
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(-1, 0, 0));
                mesh.Positions.Add(new Point3D(0, -1, 0));

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));

                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(0, 0));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(3);
                mesh.TriangleIndices.Add(4);

                return mesh;
            }
        }

        /// <summary>
        /// Two triangles which form a rectangle using CCW winding
        /// This mesh is only visible if camera is in positive Z!
        /// </summary>
        public static MeshGeometry3D FullScreenMesh
        {
            get
            {
                //  2 +-----+ 3
                //    |\    |
                //    |  \  |
                //    |    \|
                //  0 +-----+ 1
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(-1, -1, 0));
                mesh.Positions.Add(new Point3D(1, -1, 0));
                mesh.Positions.Add(new Point3D(-1, 1, 0));
                mesh.Positions.Add(new Point3D(1, 1, 0));

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));

                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 0));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);

                mesh.TriangleIndices.Add(3);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(1);

                return mesh;
            }
        }

        /// <summary>
        /// Multiple planes stacked 1 unit apart in Z
        /// </summary>
        public static MeshGeometry3D OverlappingPlanes
        {
            get
            {
                //      10----11
                //     2-----3|
                //    6-----7||
                //    ||    |||
                //    | 8 - ||9
                //    |0 - -|1
                //    4-----5
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(-1, -1, 0));
                mesh.Positions.Add(new Point3D(1, -1, 0));
                mesh.Positions.Add(new Point3D(-1, 1, 0));
                mesh.Positions.Add(new Point3D(1, 1, 0));
                mesh.Positions.Add(new Point3D(-1, -1, 1));
                mesh.Positions.Add(new Point3D(1, -1, 1));
                mesh.Positions.Add(new Point3D(-1, 1, 1));
                mesh.Positions.Add(new Point3D(1, 1, 1));
                mesh.Positions.Add(new Point3D(-1, -1, -1));
                mesh.Positions.Add(new Point3D(1, -1, -1));
                mesh.Positions.Add(new Point3D(-1, 1, -1));
                mesh.Positions.Add(new Point3D(1, 1, -1));

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));

                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 0));
                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 0));
                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 0));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(3);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(1);

                mesh.TriangleIndices.Add(4);
                mesh.TriangleIndices.Add(5);
                mesh.TriangleIndices.Add(6);
                mesh.TriangleIndices.Add(7);
                mesh.TriangleIndices.Add(6);
                mesh.TriangleIndices.Add(5);

                mesh.TriangleIndices.Add(8);
                mesh.TriangleIndices.Add(9);
                mesh.TriangleIndices.Add(10);
                mesh.TriangleIndices.Add(11);
                mesh.TriangleIndices.Add(10);
                mesh.TriangleIndices.Add(9);

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D FrontAndBack
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                PlaneXYHelper(mesh, new Point(-1.5, 1), new Point(.5, -1), 0.25, 15, 15, true);
                PlaneXYHelper(mesh, new Point(-.5, 1), new Point(1.5, -1), -0.25, 15, 15, false);
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BackAndFront
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                PlaneXYHelper(mesh, new Point(-1.5, 1), new Point(.5, -1), -0.25, 15, 15, true);
                PlaneXYHelper(mesh, new Point(-.5, 1), new Point(1.5, -1), 0.25, 15, 15, false);
                return mesh;
            }
        }

        /// <summary>
        /// Two triangles which form a rectangle using CCW winding
        /// This mesh is only visible if camera is in positive Y!
        /// </summary>
        public static MeshGeometry3D PlaneXZ
        {
            get
            {
                //  2 +-----+ 3
                //    |\    |
                //    |  \  |
                //    |    \|
                //  0 +-----+ 1
                //
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(-1, 0, 1));
                mesh.Positions.Add(new Point3D(1, 0, 1));
                mesh.Positions.Add(new Point3D(-1, 0, -1));
                mesh.Positions.Add(new Point3D(1, 0, -1));

                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));

                mesh.TextureCoordinates.Add(new Point(0, 1));
                mesh.TextureCoordinates.Add(new Point(1, 1));
                mesh.TextureCoordinates.Add(new Point(0, 0));
                mesh.TextureCoordinates.Add(new Point(1, 0));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);

                mesh.TriangleIndices.Add(3);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(1);

                return mesh;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D SimpleCubeMesh
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();

                // Do it this way to add coverage for "set" properties of MeshGeometry3D.
                // Verification is done by rendering verification
                Point3DCollection positions = new Point3DCollection();
                Vector3DCollection normals = new Vector3DCollection();
                PointCollection textureCoordinates = new PointCollection();
                Int32Collection triangleIndices = new Int32Collection();

                //       4-----5
                //      /|    /|
                //     / |   / |
                //    0-----1  |
                //    |  7--|--6
                //    | /   | /
                //    |/    |/
                //    3-----2
                //
                positions.Add(new Point3D(-1, 1, 1));   // 0
                positions.Add(new Point3D(1, 1, 1));    // 1
                positions.Add(new Point3D(1, -1, 1));   // 2
                positions.Add(new Point3D(-1, -1, 1));  // 3
                positions.Add(new Point3D(-1, 1, -1));  // 4
                positions.Add(new Point3D(1, 1, -1));   // 5
                positions.Add(new Point3D(1, -1, -1));  // 6
                positions.Add(new Point3D(-1, -1, -1)); // 7

                // Normals point diagonally out from each vertex: (vertex - origin) = normal
                normals.Add(new Vector3D(-1, 1, 1));
                normals.Add(new Vector3D(1, 1, 1));
                normals.Add(new Vector3D(1, -1, 1));
                normals.Add(new Vector3D(-1, -1, 1));
                normals.Add(new Vector3D(-1, 1, -1));
                normals.Add(new Vector3D(1, 1, -1));
                normals.Add(new Vector3D(1, -1, -1));
                normals.Add(new Vector3D(-1, -1, -1));

                // Plausible tex-coords:
                // There's no good way to do this-
                //      Two faces will always be flipped and two will be messed up
                //      I chose to let left and right be flipped and top and bottom be messed up
                textureCoordinates.Add(new Point(0, 0));
                textureCoordinates.Add(new Point(1, 0));
                textureCoordinates.Add(new Point(1, 1));
                textureCoordinates.Add(new Point(0, 1));
                textureCoordinates.Add(new Point(1, 0));
                textureCoordinates.Add(new Point(0, 0));
                textureCoordinates.Add(new Point(0, 1));
                textureCoordinates.Add(new Point(1, 1));

                // Front face
                triangleIndices.Add(0);
                triangleIndices.Add(2);
                triangleIndices.Add(1);
                triangleIndices.Add(0);
                triangleIndices.Add(3);
                triangleIndices.Add(2);

                // Right face
                triangleIndices.Add(1);
                triangleIndices.Add(6);
                triangleIndices.Add(5);
                triangleIndices.Add(1);
                triangleIndices.Add(2);
                triangleIndices.Add(6);

                // Left face
                triangleIndices.Add(4);
                triangleIndices.Add(3);
                triangleIndices.Add(0);
                triangleIndices.Add(4);
                triangleIndices.Add(7);
                triangleIndices.Add(3);

                // Back face
                triangleIndices.Add(5);
                triangleIndices.Add(7);
                triangleIndices.Add(4);
                triangleIndices.Add(5);
                triangleIndices.Add(6);
                triangleIndices.Add(7);

                // Top face
                triangleIndices.Add(0);
                triangleIndices.Add(1);
                triangleIndices.Add(4);
                triangleIndices.Add(1);
                triangleIndices.Add(5);
                triangleIndices.Add(4);

                // Bottom face
                triangleIndices.Add(2);
                triangleIndices.Add(3);
                triangleIndices.Add(7);
                triangleIndices.Add(2);
                triangleIndices.Add(7);
                triangleIndices.Add(6);

                mesh.Positions = positions;
                mesh.Normals = normals;
                mesh.TextureCoordinates = textureCoordinates;
                mesh.TriangleIndices = triangleIndices;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D SmoothEdgeCube
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();

                Point3DCollection positions = new Point3DCollection();
                Vector3DCollection normals = new Vector3DCollection();
                PointCollection textureCoordinates = new PointCollection();
                Int32Collection triangleIndices = new Int32Collection();

                // The difference between this mesh and SimpleCubeMesh is that we will
                //  triplicate vertices so that we can have valid texture coordinates.
                //
                // Normals still point diagonally out from each vertex: (vertex - origin) = normal
                //
                //       e-----f       m     n       u-----v
                //       |     |      /|    /|      /     /
                //       |     |     / |   / |     /     /
                //    a-----b  |    i  |  j  |    q-----r
                //    |  h--|--g    |  p  |  o       x-----w
                //    |     |       | /   | /       /     /
                //    |     |       |/    |/       /     /
                //    d-----c       l     k       t-----s
                //

                // Front and back faces
                positions.Add(new Point3D(-1, 1, 1));     // a
                normals.Add(new Vector3D(-1, 1, 1));
                textureCoordinates.Add(new Point(0, 0));

                positions.Add(new Point3D(1, 1, 1));      // b
                normals.Add(new Vector3D(1, 1, 1));
                textureCoordinates.Add(new Point(1, 0));

                positions.Add(new Point3D(1, -1, 1));     // c
                normals.Add(new Vector3D(1, -1, 1));
                textureCoordinates.Add(new Point(1, 1));

                positions.Add(new Point3D(-1, -1, 1));    // d
                normals.Add(new Vector3D(-1, -1, 1));
                textureCoordinates.Add(new Point(0, 1));

                positions.Add(new Point3D(-1, 1, -1));    // e
                normals.Add(new Vector3D(-1, 1, -1));
                textureCoordinates.Add(new Point(1, 0));

                positions.Add(new Point3D(1, 1, -1));     // f
                normals.Add(new Vector3D(1, 1, -1));
                textureCoordinates.Add(new Point(0, 0));

                positions.Add(new Point3D(1, -1, -1));    // g
                normals.Add(new Vector3D(1, -1, -1));
                textureCoordinates.Add(new Point(0, 1));

                positions.Add(new Point3D(-1, -1, -1));   // h
                normals.Add(new Vector3D(-1, -1, -1));
                textureCoordinates.Add(new Point(1, 1));

                // Front face
                triangleIndices.Add(0);
                triangleIndices.Add(2);
                triangleIndices.Add(1);
                triangleIndices.Add(0);
                triangleIndices.Add(3);
                triangleIndices.Add(2);

                // Back face
                triangleIndices.Add(5);
                triangleIndices.Add(7);
                triangleIndices.Add(4);
                triangleIndices.Add(5);
                triangleIndices.Add(6);
                triangleIndices.Add(7);

                // Right and left faces
                positions.Add(new Point3D(-1, 1, 1));     // i
                normals.Add(new Vector3D(-1, 1, 1));
                textureCoordinates.Add(new Point(1, 0));

                positions.Add(new Point3D(1, 1, 1));      // j
                normals.Add(new Vector3D(1, 1, 1));
                textureCoordinates.Add(new Point(0, 0));

                positions.Add(new Point3D(1, -1, 1));     // k
                normals.Add(new Vector3D(1, -1, 1));
                textureCoordinates.Add(new Point(0, 1));

                positions.Add(new Point3D(-1, -1, 1));    // l
                normals.Add(new Vector3D(-1, -1, 1));
                textureCoordinates.Add(new Point(1, 1));

                positions.Add(new Point3D(-1, 1, -1));    // m
                normals.Add(new Vector3D(-1, 1, -1));
                textureCoordinates.Add(new Point(0, 0));

                positions.Add(new Point3D(1, 1, -1));     // n
                normals.Add(new Vector3D(1, 1, -1));
                textureCoordinates.Add(new Point(1, 0));

                positions.Add(new Point3D(1, -1, -1));    // o
                normals.Add(new Vector3D(1, -1, -1));
                textureCoordinates.Add(new Point(1, 1));

                positions.Add(new Point3D(-1, -1, -1));   // p
                normals.Add(new Vector3D(-1, -1, -1));
                textureCoordinates.Add(new Point(0, 1));

                // Right face
                triangleIndices.Add(9);
                triangleIndices.Add(14);
                triangleIndices.Add(13);
                triangleIndices.Add(9);
                triangleIndices.Add(10);
                triangleIndices.Add(14);

                // Left face
                triangleIndices.Add(12);
                triangleIndices.Add(11);
                triangleIndices.Add(8);
                triangleIndices.Add(12);
                triangleIndices.Add(15);
                triangleIndices.Add(11);

                // Right and left faces
                positions.Add(new Point3D(-1, 1, 1));     // q
                normals.Add(new Vector3D(-1, 1, 1));
                textureCoordinates.Add(new Point(0, 1));

                positions.Add(new Point3D(1, 1, 1));      // r
                normals.Add(new Vector3D(1, 1, 1));
                textureCoordinates.Add(new Point(1, 1));

                positions.Add(new Point3D(1, -1, 1));     // s
                normals.Add(new Vector3D(1, -1, 1));
                textureCoordinates.Add(new Point(0, 1));

                positions.Add(new Point3D(-1, -1, 1));    // t
                normals.Add(new Vector3D(-1, -1, 1));
                textureCoordinates.Add(new Point(1, 1));

                positions.Add(new Point3D(-1, 1, -1));    // u
                normals.Add(new Vector3D(-1, 1, -1));
                textureCoordinates.Add(new Point(0, 0));

                positions.Add(new Point3D(1, 1, -1));     // v
                normals.Add(new Vector3D(1, 1, -1));
                textureCoordinates.Add(new Point(1, 0));

                positions.Add(new Point3D(1, -1, -1));    // w
                normals.Add(new Vector3D(1, -1, -1));
                textureCoordinates.Add(new Point(0, 0));

                positions.Add(new Point3D(-1, -1, -1));   // x
                normals.Add(new Vector3D(-1, -1, -1));
                textureCoordinates.Add(new Point(1, 0));

                // Top face
                triangleIndices.Add(16);
                triangleIndices.Add(17);
                triangleIndices.Add(20);
                triangleIndices.Add(17);
                triangleIndices.Add(21);
                triangleIndices.Add(20);

                // Bottom face
                triangleIndices.Add(18);
                triangleIndices.Add(19);
                triangleIndices.Add(23);
                triangleIndices.Add(18);
                triangleIndices.Add(23);
                triangleIndices.Add(22);

                mesh.Positions = positions;
                mesh.Normals = normals;
                mesh.TextureCoordinates = textureCoordinates;
                mesh.TriangleIndices = triangleIndices;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D HardEdgeCube
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();

                // Front face:
                //    0 +-----+ 1
                //      |     |
                //      |     |
                //    2 +-----+ 3

                mesh.Positions.Add(new Point3D(-1, 1, 1));
                mesh.Positions.Add(new Point3D(1, 1, 1));
                mesh.Positions.Add(new Point3D(-1, -1, 1));
                mesh.Positions.Add(new Point3D(1, -1, 1));

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));

                mesh.TextureCoordinates.Add(new Point(0.00, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.25, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.00, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.25, 0.75));

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(1);
                mesh.TriangleIndices.Add(2);
                mesh.TriangleIndices.Add(3);

                // Right face:
                //    4 +-----+ 5
                //      |     |
                //      |     |
                //    6 +-----+ 7

                mesh.Positions.Add(new Point3D(1, 1, 1));
                mesh.Positions.Add(new Point3D(1, 1, -1));
                mesh.Positions.Add(new Point3D(1, -1, 1));
                mesh.Positions.Add(new Point3D(1, -1, -1));

                mesh.Normals.Add(new Vector3D(1, 0, 0));
                mesh.Normals.Add(new Vector3D(1, 0, 0));
                mesh.Normals.Add(new Vector3D(1, 0, 0));
                mesh.Normals.Add(new Vector3D(1, 0, 0));

                mesh.TextureCoordinates.Add(new Point(0.25, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.50, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.25, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.50, 0.75));

                mesh.TriangleIndices.Add(4);
                mesh.TriangleIndices.Add(6);
                mesh.TriangleIndices.Add(5);
                mesh.TriangleIndices.Add(5);
                mesh.TriangleIndices.Add(6);
                mesh.TriangleIndices.Add(7);

                // Back face:
                //    8 +-----+ 9
                //      |     |
                //      |     |
                //   10 +-----+ 11

                mesh.Positions.Add(new Point3D(1, 1, -1));
                mesh.Positions.Add(new Point3D(-1, 1, -1));
                mesh.Positions.Add(new Point3D(1, -1, -1));
                mesh.Positions.Add(new Point3D(-1, -1, -1));

                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));

                mesh.TextureCoordinates.Add(new Point(0.50, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.75, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.50, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.75, 0.75));

                mesh.TriangleIndices.Add(8);
                mesh.TriangleIndices.Add(10);
                mesh.TriangleIndices.Add(9);
                mesh.TriangleIndices.Add(9);
                mesh.TriangleIndices.Add(10);
                mesh.TriangleIndices.Add(11);

                // Left face:
                //   12 +-----+ 13
                //      |     |
                //      |     |
                //   14 +-----+ 15

                mesh.Positions.Add(new Point3D(-1, 1, -1));
                mesh.Positions.Add(new Point3D(-1, 1, 1));
                mesh.Positions.Add(new Point3D(-1, -1, -1));
                mesh.Positions.Add(new Point3D(-1, -1, 1));

                mesh.Normals.Add(new Vector3D(-1, 0, 0));
                mesh.Normals.Add(new Vector3D(-1, 0, 0));
                mesh.Normals.Add(new Vector3D(-1, 0, 0));
                mesh.Normals.Add(new Vector3D(-1, 0, 0));

                mesh.TextureCoordinates.Add(new Point(0.75, 0.25));
                mesh.TextureCoordinates.Add(new Point(1.00, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.75, 0.75));
                mesh.TextureCoordinates.Add(new Point(1.00, 0.75));

                mesh.TriangleIndices.Add(12);
                mesh.TriangleIndices.Add(14);
                mesh.TriangleIndices.Add(13);
                mesh.TriangleIndices.Add(13);
                mesh.TriangleIndices.Add(14);
                mesh.TriangleIndices.Add(15);

                // The top and bottom faces are a little different...
                // To make texturing more consistent with sphere,
                //  we tesselate the top and bottom faces into 4 triangles.
                //
                //        back
                //    d +-------+ c     // a needs to be added twice (for u=0 and u=1)
                //      | \   / |
                // left |   e   | right // e needs to be added 4 times because each has different u
                //      | /   \ |
                //    a +-------+ b     // normal points out of screen
                //        front

                // Top face:
                mesh.Positions.Add(new Point3D(-1, 1, 1)); // a  (16)
                mesh.Positions.Add(new Point3D(0, 1, 0)); //  e (17)
                mesh.Positions.Add(new Point3D(1, 1, 1)); // b  (18)
                mesh.Positions.Add(new Point3D(0, 1, 0)); //  e (19)
                mesh.Positions.Add(new Point3D(1, 1, -1)); // c  (20)
                mesh.Positions.Add(new Point3D(0, 1, 0)); //  e (21)
                mesh.Positions.Add(new Point3D(-1, 1, -1)); // d  (22)
                mesh.Positions.Add(new Point3D(0, 1, 0)); //  e (23)
                mesh.Positions.Add(new Point3D(-1, 1, 1)); // a  (24)

                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));
                mesh.Normals.Add(new Vector3D(0, 1, 0));

                mesh.TextureCoordinates.Add(new Point(0.000, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.125, 0.00));
                mesh.TextureCoordinates.Add(new Point(0.250, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.375, 0.00));
                mesh.TextureCoordinates.Add(new Point(0.500, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.625, 0.00));
                mesh.TextureCoordinates.Add(new Point(0.750, 0.25));
                mesh.TextureCoordinates.Add(new Point(0.875, 0.00));
                mesh.TextureCoordinates.Add(new Point(1.000, 0.25));

                // Add triangles in reverse order because it looks nicer in code
                mesh.TriangleIndices.Add(24);
                mesh.TriangleIndices.Add(23);
                mesh.TriangleIndices.Add(22);

                mesh.TriangleIndices.Add(22);
                mesh.TriangleIndices.Add(21);
                mesh.TriangleIndices.Add(20);

                mesh.TriangleIndices.Add(20);
                mesh.TriangleIndices.Add(19);
                mesh.TriangleIndices.Add(18);

                mesh.TriangleIndices.Add(18);
                mesh.TriangleIndices.Add(17);
                mesh.TriangleIndices.Add(16);

                //        front
                //    a +-------+ b
                //      | \   / |
                // left |   e   | right
                //      | /   \ |
                //    d +-------+ c     // normal points out of screen
                //        back

                // Bottom face:
                mesh.Positions.Add(new Point3D(-1, -1, 1)); // a  (25)
                mesh.Positions.Add(new Point3D(0, -1, 0)); //  e (26)
                mesh.Positions.Add(new Point3D(1, -1, 1)); // b  (27)
                mesh.Positions.Add(new Point3D(0, -1, 0)); //  e (28)
                mesh.Positions.Add(new Point3D(1, -1, -1)); // c  (29)
                mesh.Positions.Add(new Point3D(0, -1, 0)); //  e (30)
                mesh.Positions.Add(new Point3D(-1, -1, -1)); // d  (31)
                mesh.Positions.Add(new Point3D(0, -1, 0)); //  e (32)
                mesh.Positions.Add(new Point3D(-1, -1, 1)); // a  (33)

                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));
                mesh.Normals.Add(new Vector3D(0, -1, 0));

                mesh.TextureCoordinates.Add(new Point(0.000, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.125, 1.00));
                mesh.TextureCoordinates.Add(new Point(0.250, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.375, 1.00));
                mesh.TextureCoordinates.Add(new Point(0.500, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.625, 1.00));
                mesh.TextureCoordinates.Add(new Point(0.750, 0.75));
                mesh.TextureCoordinates.Add(new Point(0.875, 1.00));
                mesh.TextureCoordinates.Add(new Point(1.000, 0.75));

                mesh.TriangleIndices.Add(25);
                mesh.TriangleIndices.Add(26);
                mesh.TriangleIndices.Add(27);

                mesh.TriangleIndices.Add(27);
                mesh.TriangleIndices.Add(28);
                mesh.TriangleIndices.Add(29);

                mesh.TriangleIndices.Add(29);
                mesh.TriangleIndices.Add(30);
                mesh.TriangleIndices.Add(31);

                mesh.TriangleIndices.Add(31);
                mesh.TriangleIndices.Add(32);
                mesh.TriangleIndices.Add(33);

                return mesh;
            }
        }

        /// <summary>
        /// Create a thin spiraling mesh (looks like a cut-out from a cylinder).
        /// </summary>
        /// <param name="coils"></param>
        /// <param name="longitude">The number of panels per revolution</param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static MeshGeometry3D Spiral(int coils, int longitude, double radius, double height)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            int segments = longitude * coils;
            double thickness = height / (2 * (coils + 1)); // Some random ratio I think looks good.
            double deltaY = (height - thickness) / (double)segments;
            double deltaV = deltaY / height;
            double deltaTheta = 2.0 * Math.PI / longitude;

            // Order of vertex creation:
            //  - Start at (0,h/2,r)  (h=height, r=radius)
            //      - y goes from h/2 to -h/2 at a rate that will allow 3 complete loops.
            //      - vertices are generated CW around the +y axis. They are r units away from the y-axis.

            double y0 = height / 2.0;
            double y1 = y0 - thickness;
            double v0 = 0.0;
            double v1 = thickness / height;

            for (int c = 0; c < coils; c++)
            {
                double theta = 0.0;

                for (int lon = 0; lon <= longitude; lon++)
                {
                    double u = 1 - ((double)lon / (double)longitude);
                    double x = radius * -Math.Sin(theta);
                    double z = radius * Math.Cos(theta);
                    if (lon == longitude - 1)
                    {
                        theta = 2.0 * Math.PI;  // Close the gap in case of precision error
                    }
                    else
                    {
                        theta += deltaTheta;
                    }

                    Point3D p0 = new Point3D(x, y0, z);
                    Point3D p1 = new Point3D(x, y1, z);
                    Vector3D norm = new Vector3D(x, 0, z);

                    mesh.Positions.Add(p0);
                    mesh.Positions.Add(p1);
                    mesh.Normals.Add(norm);
                    mesh.Normals.Add(norm);
                    mesh.TextureCoordinates.Add(new Point(u, v0));
                    mesh.TextureCoordinates.Add(new Point(u, v1));

                    if (lon != longitude)
                    {
                        // Create a panel.
                        // The loop just created the top right/bottom right vertices
                        // It is okay to reference a vertex that has not been created yet.
                        //
                        //   topR+2 +-----------+ 2*(lon+c*(longitude+1))   // +1 because of the extra vertex on the seam
                        //          |           |
                        //          |           |
                        //   topR+3 +-----------+ topR+1

                        int topRight = 2 * (lon + c * (longitude + 1));
                        mesh.TriangleIndices.Add(topRight);
                        mesh.TriangleIndices.Add(topRight + 2);
                        mesh.TriangleIndices.Add(topRight + 1);
                        mesh.TriangleIndices.Add(topRight + 3);
                        mesh.TriangleIndices.Add(topRight + 1);
                        mesh.TriangleIndices.Add(topRight + 2);

                        y0 -= deltaY;
                        y1 -= deltaY;
                        v0 += deltaV;
                        v1 += deltaV;
                    }
                }
            }
            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D Sphere(int latitude, int longitude, double radius)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double latTheta = 0.0;
            double latDeltaTheta = Math.PI / latitude;
            double lonTheta = 0.0;
            double lonDeltaTheta = 2.0 * Math.PI / longitude;

            Point3D origin = new Point3D(0, 0, 0);

            // Order of vertex creation:
            //  - For each latitude strip (y := [+radius,-radius] by -increment)
            //      - start at (-x,y,0)
            //      - For each longitude line (CCW about +y ... meaning +y points out of the paper)
            //          - generate vertex for latitude-longitude intersection

            // So if you have a 2x1 texture applied to this sphere:
            //      +---+---+
            //      | A | B |
            //      +---+---+
            // A camera pointing down -z with up = +y will see the "A" half of the texture.
            // "A" is considered to be the front of the sphere.

            for (int lat = 0; lat <= latitude; lat++)
            {
                double v = (double)lat / (double)latitude;
                double y = radius * Math.Cos(latTheta);
                double r = radius * Math.Sin(latTheta);

                if (lat == latitude - 1)
                {
                    latTheta = Math.PI;     // Close the gap in case of precision error
                }
                else
                {
                    latTheta += latDeltaTheta;
                }

                lonTheta = Math.PI;

                for (int lon = 0; lon <= longitude; lon++)
                {
                    double u = (double)lon / (double)longitude;
                    double x = r * Math.Cos(lonTheta);
                    double z = r * Math.Sin(lonTheta);
                    if (lon == longitude - 1)
                    {
                        lonTheta = Math.PI;     // Close the gap in case of precision error
                    }
                    else
                    {
                        lonTheta -= lonDeltaTheta;
                    }

                    Point3D p = new Point3D(x, y, z);
                    Vector3D norm = p - origin;

                    mesh.Positions.Add(p);
                    mesh.Normals.Add(norm);
                    mesh.TextureCoordinates.Add(new Point(u, v));

                    if (lat != 0 && lon != 0)
                    {
                        // The loop just created the bottom right vertex (lat * (longitude + 1) + lon)
                        //  (the +1 comes because of the extra vertex on the seam)
                        // We only create panels when we're at the bottom-right vertex
                        //  (bottom-left, top-right, top-left have all been created by now)
                        //
                        //          +-----------+ x - (longitude + 1)
                        //          |           |
                        //          |           |
                        //      x-1 +-----------+ x

                        int bottomRight = lat * (longitude + 1) + lon;
                        int bottomLeft = bottomRight - 1;
                        int topRight = bottomRight - (longitude + 1);
                        int topLeft = topRight - 1;

                        // Wind counter-clockwise
                        mesh.TriangleIndices.Add(bottomLeft);
                        mesh.TriangleIndices.Add(topRight);
                        mesh.TriangleIndices.Add(topLeft);

                        mesh.TriangleIndices.Add(bottomRight);
                        mesh.TriangleIndices.Add(topRight);
                        mesh.TriangleIndices.Add(bottomLeft);
                    }
                }
            }

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D NonIndexedSphere(int latitude, int longitude, double radius)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double latTheta = 0.0;
            double latDeltaTheta = Math.PI / latitude;
            double lonTheta = 0.0;
            double lonDeltaTheta = 2.0 * Math.PI / longitude;

            Point3D origin = new Point3D(0, 0, 0);

            // Order of vertex creation:
            //  - For each latitude strip (y := [+radius,-radius] by -increment)
            //      - start at (-x,y,0)
            //      - For each longitude line (CCW about +y ... meaning +y points out of the paper)
            //          - generate vertex for latitude-longitude intersection

            // So if you have a 2x1 texture applied to this sphere:
            //      +---+---+
            //      | A | B |
            //      +---+---+
            // A camera pointing down -z with up = +y will see the "A" half of the texture.
            // "A" is considered to be the front of the sphere.

            for (int lat = 0; lat < latitude; lat++)
            {
                double v0 = (double)lat / (double)latitude;
                double y0 = radius * Math.Cos(latTheta);
                double r0 = radius * Math.Sin(latTheta);

                if (lat == latitude - 1)
                {
                    latTheta = Math.PI;     // Close the gap in case of precision error
                }
                else
                {
                    latTheta += latDeltaTheta;
                }
                double v1 = (double)(lat + 1) / (double)latitude;
                double y1 = radius * Math.Cos(latTheta);
                double r1 = radius * Math.Sin(latTheta);

                lonTheta = Math.PI;

                for (int lon = 0; lon < longitude; lon++)
                {
                    double u0 = (double)lon / (double)longitude;
                    double x00 = r0 * Math.Cos(lonTheta);
                    double x01 = r1 * Math.Cos(lonTheta);
                    double z00 = r0 * Math.Sin(lonTheta);
                    double z01 = r1 * Math.Sin(lonTheta);
                    if (lon == longitude - 1)
                    {
                        lonTheta = Math.PI;     // Close the gap in case of precision error
                    }
                    else
                    {
                        lonTheta -= lonDeltaTheta;
                    }
                    double u1 = (double)(lon + 1) / (double)longitude;
                    double x10 = r0 * Math.Cos(lonTheta);
                    double x11 = r1 * Math.Cos(lonTheta);
                    double z10 = r0 * Math.Sin(lonTheta);
                    double z11 = r1 * Math.Sin(lonTheta);

                    // (x00,y0,z00) 0--------2 (x10,y0,z10)
                    //              |        |
                    //              |        |
                    //              |        |
                    // (x01,y1,z01) 1--------3 (x11,y1,z11)
                    //
                    Point3D p0 = new Point3D(x00, y0, z00);
                    Point3D p1 = new Point3D(x01, y1, z01);
                    Point3D p2 = new Point3D(x10, y0, z10);
                    Point3D p3 = new Point3D(x11, y1, z11);

                    Vector3D n0 = p0 - origin;
                    Vector3D n1 = p1 - origin;
                    Vector3D n2 = p2 - origin;
                    Vector3D n3 = p3 - origin;

                    Point uv0 = new Point(u0, v0);
                    Point uv1 = new Point(u0, v1);
                    Point uv2 = new Point(u1, v0);
                    Point uv3 = new Point(u1, v1);

                    mesh.Positions.Add(p0);
                    mesh.Positions.Add(p1);
                    mesh.Positions.Add(p2);
                    mesh.Positions.Add(p3);
                    mesh.Positions.Add(p2);
                    mesh.Positions.Add(p1);

                    mesh.Normals.Add(n0);
                    mesh.Normals.Add(n1);
                    mesh.Normals.Add(n2);
                    mesh.Normals.Add(n3);
                    mesh.Normals.Add(n2);
                    mesh.Normals.Add(n1);

                    mesh.TextureCoordinates.Add(uv0);
                    mesh.TextureCoordinates.Add(uv1);
                    mesh.TextureCoordinates.Add(uv2);
                    mesh.TextureCoordinates.Add(uv3);
                    mesh.TextureCoordinates.Add(uv2);
                    mesh.TextureCoordinates.Add(uv1);
                }
            }

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D HolySphere(int latitude, int longitude, double radius, double extensionFactor)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double latTheta = Math.PI;
            double latDeltaTheta = latTheta / latitude;
            double lonTheta = 0.0;
            double lonDeltaTheta = 2.0 * Math.PI / longitude;

            int indices = 0;
            Point3D origin = new Point3D(0, 0, 0);

            // Order of vertex creation (slightly different from Sphere!!):
            //  - For each latitude strip (y := [-radius,+radius] by +increment)
            //      - start at (-x,y,0)
            //      - For each longitude line (CCW about +y ... meaning +y points out of the paper)
            //          - generate vertex for latitude-longitude intersection

            // So if you have a 2x1 texture applied to this sphere:
            //      +---+---+
            //      | A | B |
            //      +---+---+
            // A camera pointing down -z with up = +y will see the "A" half of the texture.
            // "A" is considered to be the front of the sphere.

            for (int lat = 0; lat < latitude; lat++)
            {
                double v0 = (double)lat / (double)latitude;
                double y0 = radius * Math.Cos(latTheta);
                double r0 = radius * Math.Sin(latTheta);
                if (lat == latitude - 1)
                {
                    latTheta = 0.0;
                }
                else
                {
                    latTheta -= latDeltaTheta;
                }
                double v1 = (double)(lat + 1) / (double)latitude;
                double y1 = radius * Math.Cos(latTheta);
                double r1 = radius * Math.Sin(latTheta);

                lonTheta = Math.PI;

                for (int lon = 0; lon < longitude; lon++)
                {
                    double u0 = (double)lon / (double)longitude;
                    double x00 = r0 * Math.Cos(lonTheta);
                    double x01 = r1 * Math.Cos(lonTheta);
                    double z00 = r0 * Math.Sin(lonTheta);
                    double z01 = r1 * Math.Sin(lonTheta);
                    if (lon == longitude - 1)
                    {
                        lonTheta = Math.PI;
                    }
                    else
                    {
                        lonTheta -= lonDeltaTheta;
                    }
                    double u1 = (double)(lon + 1) / (double)longitude;
                    double x10 = r0 * Math.Cos(lonTheta);
                    double x11 = r1 * Math.Cos(lonTheta);
                    double z10 = r0 * Math.Sin(lonTheta);
                    double z11 = r1 * Math.Sin(lonTheta);

                    Point tex0 = new Point(u0, 1 - v0);
                    Point tex1 = new Point(u0, 1 - v1);
                    Point tex2 = new Point(u1, 1 - v0);
                    Point tex3 = new Point(u1, 1 - v1);

                    Point3D p0 = new Point3D(x00, y0, z00);
                    Point3D p1 = new Point3D(x01, y1, z01);
                    Point3D p2 = new Point3D(x10, y0, z10);
                    Point3D p3 = new Point3D(x11, y1, z11);

                    Vector3D norm0 = p0 - origin;
                    Vector3D norm1 = p1 - origin;
                    Vector3D norm2 = p2 - origin;
                    Vector3D norm3 = p3 - origin;

                    Vector3D norm = MathEx.Normalize(norm0 + norm1 + norm2 + norm3);
                    norm *= extensionFactor;

                    // Push the panel away from the surface of the sphere (this is what makes it "holy")
                    p0 += norm;
                    p1 += norm;
                    p2 += norm;
                    p3 += norm;

                    mesh.Positions.Add(p0);
                    mesh.Positions.Add(p1);
                    mesh.Positions.Add(p2);
                    mesh.Positions.Add(p3);

                    mesh.Normals.Add(norm0);
                    mesh.Normals.Add(norm1);
                    mesh.Normals.Add(norm2);
                    mesh.Normals.Add(norm3);

                    mesh.TextureCoordinates.Add(tex0);
                    mesh.TextureCoordinates.Add(tex1);
                    mesh.TextureCoordinates.Add(tex2);
                    mesh.TextureCoordinates.Add(tex3);

                    // (x01,y1,z01) 1--------3 (x11,y1,z11)
                    //              |        |
                    //              |        |
                    //              |        |
                    // (x00,y0,z00) 0--------2 (x10,y0,z10)
                    //
                    mesh.TriangleIndices.Add(indices);
                    mesh.TriangleIndices.Add(indices + 2);
                    mesh.TriangleIndices.Add(indices + 1);
                    mesh.TriangleIndices.Add(indices + 1);
                    mesh.TriangleIndices.Add(indices + 2);
                    mesh.TriangleIndices.Add(indices + 3);

                    indices += 4;
                }
            }

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PlaneXY(Point topLeft, Point bottomRight, double z, int xTessellations, int yTessellations)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            PlaneXYHelper(mesh, topLeft, bottomRight, z, xTessellations, yTessellations, true);
            return mesh;
        }

        private static void PlaneXYHelper(MeshGeometry3D mesh, Point topLeft, Point bottomRight, double z, int xTessellations, int yTessellations, bool frontFacing)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException("mesh", "Cannot be null");
            }

            Vector3D normal = frontFacing ? new Vector3D(0, 0, 1) : new Vector3D(0, 0, -1);
            Point bottomLeft = MathEx.Min(topLeft, bottomRight);
            Point topRight = MathEx.Max(topLeft, bottomRight);
            double deltaX = (topRight.X - bottomLeft.X) / (double)xTessellations;
            double deltaY = (topRight.Y - bottomLeft.Y) / (double)yTessellations;
            double x = bottomLeft.X;
            double y = bottomLeft.Y;
            double deltaU = 1.0 / (double)xTessellations;
            double deltaV = 1.0 / (double)yTessellations;
            double u = 0;
            double v = 1;
            int index = mesh.Positions.Count;

            for (int j = 0; j < yTessellations; j++)
            {
                double y2 = y + deltaY;
                double v2 = v - deltaV;

                for (int i = 0; i < xTessellations; i++)
                {
                    //   (x,y2) +-------+ (x2,y2)
                    //          | \     |
                    //          |   \   |
                    //          |     \ |
                    //    (x,y) +-------+ (x2,y)
                    //
                    double x2 = x + deltaX;
                    double u2 = u + deltaU;
                    mesh.Positions.Add(new Point3D(x, y, z));
                    mesh.Positions.Add(new Point3D(x2, y, z));
                    mesh.Positions.Add(new Point3D(x, y2, z));
                    mesh.Positions.Add(new Point3D(x2, y2, z));
                    mesh.Normals.Add(normal);
                    mesh.Normals.Add(normal);
                    mesh.Normals.Add(normal);
                    mesh.Normals.Add(normal);
                    mesh.TextureCoordinates.Add(new Point(u, v));
                    mesh.TextureCoordinates.Add(new Point(u2, v));
                    mesh.TextureCoordinates.Add(new Point(u, v2));
                    mesh.TextureCoordinates.Add(new Point(u2, v2));
                    if (frontFacing)
                    {
                        mesh.TriangleIndices.Add(index++);    // 0
                        mesh.TriangleIndices.Add(index++);    // 1
                        mesh.TriangleIndices.Add(index++);    // 2
                        mesh.TriangleIndices.Add(index--);    // 3
                        mesh.TriangleIndices.Add(index--);    // 2
                        mesh.TriangleIndices.Add(index++);    // 1
                        index += 2;
                    }
                    else
                    {
                        mesh.TriangleIndices.Add(index);      // 0
                        mesh.TriangleIndices.Add(index + 2);    // 2
                        mesh.TriangleIndices.Add(index + 1);    // 1
                        mesh.TriangleIndices.Add(index + 3);    // 3
                        mesh.TriangleIndices.Add(index + 1);    // 1
                        mesh.TriangleIndices.Add(index + 2);    // 2
                        index += 4;
                    }
                    x += deltaX;
                    u += deltaU;
                }
                x = bottomLeft.X;
                u = 0;
                y += deltaY;
                v -= deltaV;
            }
        }

        /// <summary>
        /// Creates a grid mesh with random height points
        /// </summary>
        /// <param name="xSize">number of points in x</param>
        /// <param name="zSize">number of points in z</param>
        /// <param name="yMax">maximum random height</param>
        /// <returns>A Grid mesh of random heights</returns>
        public static MeshGeometry3D CreateRandomGrid(int xSize, int zSize, double yMax)
        {
            return CreateRandomGrid(xSize, zSize, yMax, EnvironmentWrapper.TickCount);
        }

        /// <summary>
        /// Creates a grid mesh with random height points
        /// </summary>
        /// <param name="xSize">number of points in x</param>
        /// <param name="zSize">number of points in z</param>
        /// <param name="yMax">maximum random height</param>
        /// <param name="randomSeed">Seed used for random number generation</param>
        /// <returns>A Grid mesh of random heights</returns>
        public static MeshGeometry3D CreateRandomGrid(int xSize, int zSize, double yMax, int randomSeed)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            Random rand = new Random(randomSeed);

            for (int z = 0; z < zSize; z++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    mesh.Positions.Add(new Point3D((double)x, rand.NextDouble() * yMax, (double)z));
                    mesh.TextureCoordinates.Add(new Point(x / (double)xSize, z / (double)zSize));
                }
            }
            AddGridTriangles(ref mesh, xSize, zSize);

            return mesh;
        }

        /// <summary>
        /// Creates a flat grid mesh with constant height points
        /// </summary>
        /// <param name="xSize">number of points in x</param>
        /// <param name="zSize">number of points in z</param>
        /// <param name="yPos">y height for all points</param>
        /// <returns>A Grid mesh of constant heights</returns>
        public static MeshGeometry3D CreateFlatGrid(int xSize, int zSize, double yPos)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int z = 0; z < zSize; z++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    mesh.Positions.Add(new Point3D((double)x, yPos, (double)z));
                    mesh.TextureCoordinates.Add(new Point(x / (double)xSize, z / (double)zSize));
                    mesh.Normals.Add(new Vector3D(0, 1, 0));
                }
            }
            AddGridTriangles(ref mesh, xSize, zSize);

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D CreateFlatGridUV(int xSize, int ySize, double zPos, Point uvMin, Point uvMax)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double dx = 1.0 / (double)(xSize - 1);
            double dy = 1.0 / (double)(ySize - 1);

            double uvXdelta = dx * (uvMax.X - uvMin.X);
            double uvYdelta = dy * (uvMax.Y - uvMin.Y);

            // loop from Y=1 to Y=-1 so that this matches the winding order of other grids
            //   when calling AddGridTriangles()
            for (int y = ySize - 1; y >= 0; y--)
            {
                for (int x = 0; x < xSize; x++)
                {
                    mesh.Positions.Add(new Point3D(
                        (-1 * (x * dx)) + (1 * (1 - (x * dx))), // X
                        (-1 * (y * dy)) + (1 * (1 - (y * dy))), // Y
                        zPos));                                 // Z
                    mesh.TextureCoordinates.Add(
                        new Point(
                            ((x * dx) * uvXdelta) - uvMin.X,
                            ((y * dy) * uvYdelta) - uvMin.Y
                        )
                    );
                    mesh.Normals.Add(new Vector3D(0, 0, 1));
                }
            }
            AddGridTriangles(ref mesh, xSize, ySize);

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D CreateFlatGridRandomUV(int xSize, int ySize, double zPos, Point uvMin, Point uvMax, int seed)
        {
            Random rand = new Random(seed);

            MeshGeometry3D mesh = new MeshGeometry3D();

            double dx = 1.0 / (double)(xSize - 1);
            double dy = 1.0 / (double)(ySize - 1);

            double uvXdelta = dx * (uvMax.X - uvMin.X);
            double uvYdelta = dy * (uvMax.Y - uvMin.Y);

            // loop from Y=1 to Y=-1 so that this matches the winding order of other grids
            //   when calling AddGridTriangles()
            for (int y = ySize - 1; y >= 0; y--)
            {
                for (int x = 0; x < xSize; x++)
                {
                    mesh.Positions.Add(new Point3D(
                        (-1 * (x * dx)) + (1 * (1 - (x * dx))), // X
                        (-1 * (y * dy)) + (1 * (1 - (y * dy))), // Y
                        zPos));                                 // Z
                    mesh.TextureCoordinates.Add(
                        new Point(
                            (rand.NextDouble() * uvXdelta) - uvMin.X,
                            (rand.NextDouble() * uvYdelta) - uvMin.Y
                        )
                    );
                    mesh.Normals.Add(new Vector3D(0, 0, 1));
                }
            }
            AddGridTriangles(ref mesh, xSize, ySize);

            return mesh;
        }

        /// <summary>
        /// Creates a grid mesh with elevation read from an image file
        /// </summary>
        /// <param name="imageFile">heightmap bitmap. RGB sum intensity determines height</param>
        /// <param name="yMax">maximum y value for grid points</param>
        /// <returns>A Grid mesh of heights specified by a heighmap
        ///     or null if there are issues with the file</returns>
        public static MeshGeometry3D CreateGridFromImage(string imageFile, double yMax)
        {
            Color[,] pixels = ColorOperations.ToColorArray(new BitmapImage(new Uri(imageFile, UriKind.RelativeOrAbsolute)));

            MeshGeometry3D mesh = new MeshGeometry3D();
            int xSize = pixels.GetLength(0);
            int zSize = pixels.GetLength(1);

            for (int z = 0; z < zSize; z++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    int r = pixels[x, z].R;
                    int g = pixels[x, z].G;
                    int b = pixels[x, z].B;

                    // averaging RGB values and normalizing to 0-1
                    Point3D pos = new Point3D(x, ((r + g + b) / 765.0) * yMax, z);
                    mesh.Positions.Add(pos);
                    // setting UV mapping to whole mesh
                    mesh.TextureCoordinates.Add(new Point(x / (double)xSize, z / (double)zSize));
                }
            }
            AddGridTriangles(ref mesh, xSize, zSize);

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D NormalizedHeightFieldFromBitmap(BitmapSource image)
        {
            Color[,] pixels = ColorOperations.ToColorArray(image);
            MeshGeometry3D mesh = new MeshGeometry3D();
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            double ySize = (double)(height - 1);
            double xSize = (double)(width - 1);

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = pixels[x, y].R;
                    int g = pixels[x, y].G;
                    int b = pixels[x, y].B;
                    Point3D pos = new Point3D(
                        (2 * x / xSize) - 1.0,
                        ((2 * y / ySize) - 1.0),
                        ((r + g + b) / 765.0));
                    mesh.Positions.Add(pos);
                    mesh.TextureCoordinates.Add(new Point(x / xSize, 1 - (y / ySize)));
                }
            }
            AddGridTriangles(ref mesh, width, height);
            MeshOperations.GenerateNormals(mesh, false);

            return mesh;
        }

        internal static void AddGridTriangles(ref MeshGeometry3D mesh, int xSize, int zSize)
        {
            // Each grid cell needs 4 points, so we loop from 1 onward.
            // This way we are certain than z-1 and x-1 are valid indices.
            for (int z = 1; z < zSize; z++)
            {
                for (int x = 1; x < xSize; x++)
                {
                    // We need a reference to four adjacent points to make a grid cell.
                    // The cell consists of two triangles.
                    int indexCurrent = x + z * xSize;
                    int indexUp = x + (z - 1) * xSize;
                    int indexLeft = (x - 1) + z * xSize;
                    int indexUpLeft = (x - 1) + (z - 1) * xSize;

                    // I like to alternate the tessellation to form a /\/\ pattern,
                    //    instead of a //// pattern or a \\\\ pattern for adjacent grid cells.
                    // Either one could be use instead.
                    if (((x + z) % 2) == 0)
                    {
                        // Front face
                        mesh.TriangleIndices.Add(indexCurrent);
                        mesh.TriangleIndices.Add(indexUp);
                        mesh.TriangleIndices.Add(indexLeft);
                        mesh.TriangleIndices.Add(indexLeft);
                        mesh.TriangleIndices.Add(indexUp);
                        mesh.TriangleIndices.Add(indexUpLeft);
                    }
                    else
                    {
                        // Front face
                        mesh.TriangleIndices.Add(indexCurrent);
                        mesh.TriangleIndices.Add(indexUp);
                        mesh.TriangleIndices.Add(indexUpLeft);
                        mesh.TriangleIndices.Add(indexCurrent);
                        mesh.TriangleIndices.Add(indexUpLeft);
                        mesh.TriangleIndices.Add(indexLeft);
                    }
                }
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D CreateFlatDisc(int spokes)
        {
            if (spokes < 3)
            {
                throw new ArgumentOutOfRangeException("There should be at least 3 spokes per disc.");
            }

            MeshGeometry3D mesh = new MeshGeometry3D();
            double twoPi = 2.0 * Math.PI;
            double midpoint = spokes / 2.0;

            // Add circumference points
            for (int i = 0; i < spokes; i++)
            {
                //     543
                //   6  |  2
                //  7   |   1
                //  8---n---0
                //  9   |
                //   ...|
                //
                double t = (double)i / (double)spokes;
                double x = Math.Cos(t * twoPi);
                double y = Math.Sin(t * twoPi);

                mesh.Positions.Add(new Point3D(x, y, 0));
                mesh.TextureCoordinates.Add(new Point((i < midpoint) ? 2 * t : 2 - (2 * t), 1)); // Autoreverse!
                mesh.Normals.Add(new Vector3D(0, 0, 1));

                // It's okay to reference vertices that don't exist yet (n and i+1)
                mesh.TriangleIndices.Add(spokes);
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add((i + 1) % spokes);
            }

            // Add center of disk ( TriangleIndex: numTriangles )
            mesh.Positions.Add(new Point3D(0, 0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.Normals.Add(new Vector3D(0, 0, 1));

            return mesh;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D CreateGlassFrame(Rect clientArea, double radius, double height, int steps)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double startAngle = 0;
            double endAngle = 0;
            double angleDelta = 0;

            // top-left
            startAngle = Math.PI;
            endAngle = Math.PI * 1.5;
            angleDelta = (endAngle - startAngle) / steps;
            for (int tl = 0; tl <= steps; tl++)
            {
                Point endPoint = new Point(
                        clientArea.TopLeft.X + radius * Math.Cos(startAngle + angleDelta * tl),
                        clientArea.TopLeft.Y + radius * Math.Sin(startAngle + angleDelta * tl));
                AddStrip(mesh, clientArea.TopLeft, endPoint, height, steps, false);
            }

            // top-right
            startAngle = endAngle;
            endAngle = Math.PI * 2.0;
            angleDelta = (endAngle - startAngle) / steps;
            for (int tr = 0; tr <= steps; tr++)
            {
                Point endPoint = new Point(
                        clientArea.TopRight.X + radius * Math.Cos(startAngle + angleDelta * tr),
                        clientArea.TopRight.Y + radius * Math.Sin(startAngle + angleDelta * tr));
                AddStrip(mesh, clientArea.TopRight, endPoint, height, steps, false);
            }

            // bottom-right
            startAngle = 0;
            endAngle = Math.PI * 0.5;
            angleDelta = (endAngle - startAngle) / steps;
            for (int br = 0; br <= steps; br++)
            {
                Point endPoint = new Point(
                        clientArea.BottomRight.X + radius * Math.Cos(startAngle + angleDelta * br),
                        clientArea.BottomRight.Y + radius * Math.Sin(startAngle + angleDelta * br));
                AddStrip(mesh, clientArea.BottomRight, endPoint, height, steps, false);
            }

            // bottom-left
            startAngle = endAngle;
            endAngle = Math.PI;
            angleDelta = (endAngle - startAngle) / steps;
            for (int bl = 0; bl <= steps; bl++)
            {
                Point endPoint = new Point(
                        clientArea.BottomLeft.X + radius * Math.Cos(startAngle + angleDelta * bl),
                        clientArea.BottomLeft.Y + radius * Math.Sin(startAngle + angleDelta * bl));
                AddStrip(mesh, clientArea.BottomLeft, endPoint, height, steps, false);
            }

            // connect with initial part
            AddStrip(
                    mesh,
                    new Point() /* dummy */,
                    new Point() /* dummy */,
                    height      /* dummy */,
                    steps       /* dummy */,
                    true);

            // add central square
            Point current;
            int index = mesh.Positions.Count;
            current = clientArea.TopLeft;
            mesh.Positions.Add(new Point3D(current.X, current.Y, height));
            mesh.TextureCoordinates.Add(current);
            current = clientArea.TopRight;
            mesh.Positions.Add(new Point3D(current.X, current.Y, height));
            mesh.TextureCoordinates.Add(current);
            current = clientArea.BottomLeft;
            mesh.Positions.Add(new Point3D(current.X, current.Y, height));
            mesh.TextureCoordinates.Add(current);
            current = clientArea.BottomRight;
            mesh.Positions.Add(new Point3D(current.X, current.Y, height));
            mesh.TextureCoordinates.Add(current);
            mesh.TriangleIndices.Add(index + 2);
            mesh.TriangleIndices.Add(index);
            mesh.TriangleIndices.Add(index + 1);
            mesh.TriangleIndices.Add(index + 3);
            mesh.TriangleIndices.Add(index + 2);
            mesh.TriangleIndices.Add(index + 1);

            // Generate Normals
            MeshOperations.GenerateNormals(mesh, true);

            return mesh;
        }

        private static void AddStrip(MeshGeometry3D mesh, Point start, Point end, double height, int steps, bool connectEnds)
        {
            Vector delta = (end - start) / steps;
            double angleDelta = (Math.PI * 0.5) / steps;

            int startIndex = mesh.Positions.Count;
            int oldStartIndex = mesh.Positions.Count - (steps + 1);

            if (connectEnds)
            {
                startIndex = 0;
            }
            else
            {
                // positions
                for (int i = 0; i <= steps; i++)
                {
                    Point current = start + (i * delta);
                    mesh.Positions.Add(new Point3D(current.X, current.Y, height * Math.Cos(i * angleDelta)));
                    mesh.TextureCoordinates.Add(current);
                }
            }

            if (oldStartIndex >= 0)
            {
                // triangles
                for (int i = 0; i < steps; i++)
                {
                    int bottomLeft = oldStartIndex + i;
                    int bottomRight = oldStartIndex + i + 1;
                    int topLeft = startIndex + i;
                    int topRight = startIndex + i + 1;

                    mesh.TriangleIndices.Add(topLeft);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(topRight);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(bottomRight);
                    mesh.TriangleIndices.Add(topRight);

                }
            }
        }

        #region Bad Meshes

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadPositions1
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadPositions2
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadPositions4
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(0, 0, 0));
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadPositions5
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(1, 0, 0));
                mesh.Positions.Add(new Point3D(0, 1, 0));
                mesh.Positions.Add(new Point3D(0, 0, 0));
                mesh.Positions.Add(new Point3D(0, -1, 0));
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D NullPositions1
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = null;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D NullPositions2
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = null;
                mesh.Normals = null;
                mesh.TextureCoordinates = null;
                mesh.TriangleIndices = null;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D NullPositions3
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = null;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndex1
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too large)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[count / 2] = mesh.Positions.Count;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndex2
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too small)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[count / 2] = -1;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndex3
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too large)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[(count / 2) - 1] = mesh.Positions.Count;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndex4
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too small)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[(count / 2) - 1] = -1;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndex5
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too large)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[(count / 2) - 2] = mesh.Positions.Count;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndex6
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too small)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[(count / 2) - 2] = -1;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndexNoNormals
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too large)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[count / 2] = mesh.Positions.Count;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndexNullNormals
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = null;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                // index out of range (too large)
                int count = mesh.TriangleIndices.Count;
                mesh.TriangleIndices[count / 2] = mesh.Positions.Count;
                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D BadIndexMissingSomeNormals
        {
            get
            {
                MeshGeometry3D mesh = NonIndexedSphere(12, 24, 1.5);
                MeshOperations.GenerateTriangleIndices(mesh);
                int normals = mesh.Normals.Count;
                int indices = mesh.TriangleIndices.Count;

                // delete 1/2 of the normals
                int index = normals / 2;
                while (index < normals)
                {
                    mesh.Normals.RemoveAt(index);
                    normals--;
                }

                // make 1/3 of triangles disappear
                mesh.TriangleIndices[(indices * 2) / 3] = mesh.Positions.Count;
                return mesh;
            }
        }

        #endregion

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D Positions
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsElseNull
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = null;
                mesh.TextureCoordinates = null;
                mesh.TriangleIndices = null;

                return mesh;
            }
        }
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsNormals
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsUV
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.TextureCoordinates = PlaneMesh.UVs;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsIndices
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.TriangleIndices = PlaneMesh.Indices;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsNormalsUV
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsNormalsIndices
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TriangleIndices = PlaneMesh.Indices;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsUVIndices
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                return mesh;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MeshGeometry3D PositionsNormalsUVIndices
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.Positions = PlaneMesh.Positions;
                mesh.Normals = PlaneMesh.Normals;
                mesh.TextureCoordinates = PlaneMesh.UVs;
                mesh.TriangleIndices = PlaneMesh.Indices;

                return mesh;
            }
        }

        /// <summary>
        /// Creates the collections necessary for creating a plane mesh.
        /// </summary>
        private class PlaneMesh
        {
            // Using ints so we don't get surprised by precision error during for-loops
            // These will actually be scaled by the MeshPositions function since we don't want coords that large
            private const int step = 1;
            private const int xmin = -10;
            private const int xmax = 10;
            private const int ymin = -10;
            private const int ymax = 10;

            public static Point3DCollection Positions
            {
                get
                {
                    Point3DCollection positions = new Point3DCollection();

                    // Create a flat grid with 20 panels x 20 panels
                    for (int y = ymax; y > ymin; y -= step)
                    {
                        for (int x = xmin; x < xmax; x += step)
                        {
                            //    0 +-------+ 2,4
                            //      |       |
                            //      |       |   // Some vertices are added twice
                            //      |       |
                            //  1,5 +-------+ 3
                            //
                            double X = x / 10.0;
                            double Y = y / 10.0;
                            double Step = step / 10.0;
                            positions.Add(new Point3D(X, Y, 0));
                            positions.Add(new Point3D(X, Y - Step, 0));
                            positions.Add(new Point3D(X + Step, Y, 0));
                            positions.Add(new Point3D(X + Step, Y - Step, 0));
                            positions.Add(new Point3D(X + Step, Y, 0));
                            positions.Add(new Point3D(X, Y - Step, 0));
                        }
                    }
                    return positions;
                }
            }
            public static Vector3DCollection Normals
            {
                get
                {
                    Vector3DCollection normals = new Vector3DCollection();
                    for (double y = ymax; y > ymin; y -= step)
                    {
                        for (double x = xmin; x < xmax; x += step)
                        {
                            normals.Add(new Vector3D(0, 0, 1));
                            normals.Add(new Vector3D(0, 0, 1));
                            normals.Add(new Vector3D(0, 0, 1));
                            normals.Add(new Vector3D(0, 0, 1));
                            normals.Add(new Vector3D(0, 0, 1));
                            normals.Add(new Vector3D(0, 0, 1));
                        }
                    }
                    return normals;
                }
            }
            public static PointCollection UVs
            {
                get
                {
                    PointCollection uvs = new PointCollection();
                    double uStep = step / (double)(xmax - xmin);
                    double vStep = step / (double)(ymax - ymin);
                    double u = 0;
                    double v = 0;
                    for (double y = ymax; y > ymin; y -= step)
                    {
                        u = 0;
                        for (double x = xmin; x < xmax; x += step)
                        {
                            // These match the positions created by "Positions"
                            uvs.Add(new Point(u, v));
                            uvs.Add(new Point(u, v + vStep));
                            uvs.Add(new Point(u + uStep, v));
                            uvs.Add(new Point(u + uStep, v + vStep));
                            uvs.Add(new Point(u + uStep, v));
                            uvs.Add(new Point(u, v + vStep));
                            u += uStep;
                        }
                        v += vStep;
                    }
                    return uvs;
                }
            }
            public static Int32Collection Indices
            {
                get
                {
                    Int32Collection indices = new Int32Collection();
                    int index = 0;
                    for (double y = ymax; y > ymin; y -= step)
                    {
                        for (double x = xmin; x < xmax; x += step)
                        {
                            indices.Add(index++);
                            indices.Add(index++);
                            indices.Add(index++);
                            indices.Add(index++);
                            indices.Add(index++);
                            indices.Add(index++);
                        }
                    }
                    return indices;
                }
            }
        }
    }
}
