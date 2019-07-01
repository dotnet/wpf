// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    ///  Make 3D scenes
    /// </summary>
    public class SceneFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup MakeScene(string scene)
        {
            switch (scene)
            {
                case "SingleTriangle":
                    return SingleTriangle;

                case "UnitPlane":
                    return UnitPlane;

                case "MultiParent":
                    return MultiParent;

                case "SpherePlusBackdrop":
                    return SpherePlusBackdrop;
#if SSL
            case "Box":
                    return Box;

            case "TriangleAndLines":
                    return TriangleAndLines;

            case "UnitPlaneAndLines":
                    return UnitPlaneAndLines;

            case "Clippable":
                    return Clippable;

            case "SphereInABox":
                    return SphereInABox;

            case "LayeredLines":
                    return LayeredLines;
#endif
                case "LayeredMeshes":
                    return LayeredMeshes;

                case "Xbox":
                    return Xbox;

                case "Group":
                    return Group;

                case "GroupNullChildren":
                    return GroupNullChildren;

                case "GroupSphere":
                    return GroupSphere(50, 50, 1.0, MaterialFactory.Default);

                case "LowResGroupSphere":
                    return GroupSphere(12, 24, 1.0, MaterialFactory.Default);

                case "BipedWalker":
                    return BipedWalker(false);
            }
            throw new ArgumentException("Specified scene (" + scene + ") cannot be created");
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup SingleTriangle
        {
            get
            {
                Light light = LightFactory.WhiteAmbient;
                GeometryModel3D model = new GeometryModel3D();
                model.Geometry = MeshFactory.SingleFrontFacingTriangle;
                model.Material = MaterialFactory.Default;
                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection(new Model3D[] { light, model });

                light.SetValue(Const.NameProperty, "Light");
                model.SetValue(Const.NameProperty, "Model");
                parent.SetValue(Const.NameProperty, "Parent");

                return parent;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup UnitPlane
        {
            get
            {
                Light light = LightFactory.WhiteAmbient;
                GeometryModel3D model = new GeometryModel3D();
                model.Geometry = MeshFactory.UnitPlaneTriangle;
                model.Material = MaterialFactory.Default;
                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection(new Model3D[] { light, model });

                light.SetValue(Const.NameProperty, "Light");
                model.SetValue(Const.NameProperty, "Model");
                parent.SetValue(Const.NameProperty, "Parent");

                return parent;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup MultiParent
        {
            get
            {
                Light light = LightFactory.WhiteAmbient;
                MeshGeometry3D mesh = MeshFactory.SingleFrontFacingTriangle;
                Model3D model = new GeometryModel3D(mesh, MaterialFactory.Default);

                Model3DGroup child1 = new Model3DGroup();
                child1.Children = new Model3DCollection(new Model3D[] { model });
                Model3DGroup child2 = new Model3DGroup();
                child2.Children = new Model3DCollection(new Model3D[] { model });
                Model3DGroup child3 = new Model3DGroup();
                child3.Children = new Model3DCollection(new Model3D[] { model });
                Model3DGroup child4 = new Model3DGroup();
                child4.Children = new Model3DCollection(new Model3D[] { model });
                child2.Transform = new TranslateTransform3D(new Vector3D(-1, 0, -1));
                child3.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(-1, 1, 0), 180), new Point3D(.5, .5, 0));
                child4.Transform = new ScaleTransform3D(new Vector3D(.5, .5, .5), new Point3D(-.5, -.5, -.5));

                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection(new Model3D[] { light, child1, child2, child3, child4 });

                parent.SetValue(Const.NameProperty, "Parent");
                light.SetValue(Const.NameProperty, "Light");
                child1.SetValue(Const.NameProperty, "Child1");
                child2.SetValue(Const.NameProperty, "Child2");
                child3.SetValue(Const.NameProperty, "Child3");
                child4.SetValue(Const.NameProperty, "Child4");
                mesh.SetValue(Const.NameProperty, "Mesh");
                model.SetValue(Const.NameProperty, "Model");

                return parent;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup SpherePlusBackdrop
        {
            get
            {
                GeometryModel3D sphere = new GeometryModel3D();
                sphere.Geometry = MeshFactory.Sphere(10, 20, 1.0);
                sphere.Material = MaterialFactory.Blue;

                GeometryModel3D cube = new GeometryModel3D();
                cube.Geometry = MeshFactory.SimpleCubeMesh;
                cube.BackMaterial = MaterialFactory.White;
                cube.Transform = new ScaleTransform3D(6, 6, 6);

                Model3DGroup group = new Model3DGroup();
                group.Children.Add(cube);
                group.Children.Add(sphere);
                group.Children.Add(LightFactory.WhiteDirectionalNegAll);

                return group;
            }
        }

#if SSL
        /// <summary>
        /// This one doesn't have a light in it, but screen space lines doesn't need it anyway
        /// </summary>
        public static Model3DGroup  Box
        {
            get
            {
                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Points.Add( new Point3D( -1,-1,1 ) );
                lines.Points.Add( new Point3D( 1,-1,1 ) );
                lines.Points.Add( new Point3D( 1,1,1 ) );
                lines.Points.Add( new Point3D( -1,1,1 ) );
                lines.Thickness = 10.0;
                lines.Color = Colors.Blue;

                Model3DGroup front = new Model3DGroup();
                front.Children = new Model3DCollection( new Model3D[]{ lines } );
                Model3DGroup left = new Model3DGroup();
                left.Children = new Model3DCollection( new Model3D[]{ lines } );
                Model3DGroup back = new Model3DGroup();
                back.Children = new Model3DCollection( new Model3D[]{ lines } );
                Model3DGroup right = new Model3DGroup();
                right.Children = new Model3DCollection( new Model3D[]{ lines } );
                RotateTransform3D tx = new RotateTransform3D( new AxisAngleRotation3D( Const.yAxis, 90 ) );
                right.Transform = tx.Clone();
                ((AxisAngleRotation3D)tx.Rotation).Angle = 180;
                back.Transform = tx.Clone();
                ((AxisAngleRotation3D)tx.Rotation).Angle = 270;
                left.Transform = tx.Clone();

                Model3DGroup box = new Model3DGroup();
                box.Children = new Model3DCollection( new Model3D[]{ front, left, back, right } );

                front.SetValue( Const.NameProperty, "BoxFront" );
                left.SetValue( Const.NameProperty, "BoxLeft" );
                back.SetValue( Const.NameProperty, "BoxBack" );
                right.SetValue( Const.NameProperty, "BoxRight" );
                box.SetValue( Const.NameProperty, "BoxGroup" );

                return box;
            }
        }



        public static Model3DGroup  TriangleAndLines
        {
            get
            {
                Light light = LightFactory.WhiteDirectionalNegZ;
                ScreenSpaceLines3D lines = ScreenSpaceLinesFactory.HourGlass;
                MeshGeometry3D mesh = MeshFactory.SingleFrontFacingTriangle;
                GeometryModel3D model = new GeometryModel3D();
                model.Geometry = mesh;
                model.Material = MaterialFactory.Default;

                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection( new Model3D[]{ light, lines, model } );

                light.SetValue( Const.NameProperty, "Light" );
                lines.SetValue( Const.NameProperty, "Lines" );
                mesh.SetValue( Const.NameProperty, "Mesh" );
                model.SetValue( Const.NameProperty, "Model" );
                parent.SetValue( Const.NameProperty, "Parent" );

                return parent;
            }
        }



        public static Model3DGroup  UnitPlaneAndLines
        {
            get
            {
                Light light = LightFactory.WhiteDirectionalNegZ;
                ScreenSpaceLines3D lines = ScreenSpaceLinesFactory.HourGlass;
                MeshGeometry3D mesh = MeshFactory.UnitPlaneTriangle;
                GeometryModel3D model = new GeometryModel3D( mesh, MaterialFactory.Default );

                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection( new Model3D[]{ light, lines, model } );

                light.SetValue( Const.NameProperty, "Light" );
                lines.SetValue( Const.NameProperty, "Lines" );
                mesh.SetValue( Const.NameProperty, "Mesh" );
                model.SetValue( Const.NameProperty, "Model" );
                parent.SetValue( Const.NameProperty, "Parent" );

                return parent;
            }
        }



        public static Model3DGroup  Clippable
        {
            get
            {
                Light light = LightFactory.WhiteDirectionalNegZ;
                ScreenSpaceLines3D lines = ScreenSpaceLinesFactory.Quad;
                MeshGeometry3D mesh = MeshFactory.UnitPlaneTriangle;
                GeometryModel3D model = new GeometryModel3D( mesh, MaterialFactory.Default );

                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection( new Model3D[]{ light, lines, model } );

                light.SetValue( Const.NameProperty, "Light" );
                lines.SetValue( Const.NameProperty, "Lines" );
                mesh.SetValue( Const.NameProperty, "Mesh" );
                model.SetValue( Const.NameProperty, "Model" );
                parent.SetValue( Const.NameProperty, "Parent" );

                return parent;
            }
        }



        public static Model3DGroup  SphereInABox
        {
            get
            {
                Light light = LightFactory.WhiteDirectionalNegZ;
                Model3DGroup lines = Box;
                MeshGeometry3D mesh = MeshFactory.Sphere( 12,24,1.1 );
                GeometryModel3D model = new GeometryModel3D( mesh, MaterialFactory.Default );

                Model3DGroup parent = new Model3DGroup();
                parent.Children = new Model3DCollection( new Model3D[]{ light, lines, model } );

                light.SetValue( Const.NameProperty, "Light" );
                lines.SetValue( Const.NameProperty, "Lines" );
                mesh.SetValue( Const.NameProperty, "Mesh" );
                model.SetValue( Const.NameProperty, "Model" );
                parent.SetValue( Const.NameProperty, "Parent" );

                return parent;
            }
        }



        public static Model3DGroup  LayeredLines
        {
            get
            {
                ScreenSpaceLines3D front = ScreenSpaceLinesFactory.Fat;
                front.Color = Colors.Red;
                front.Transform = new TranslateTransform3D( new Vector3D( 0,0.75,0 ) );

                ScreenSpaceLines3D middle = ScreenSpaceLinesFactory.Fat;
                middle.Color = Colors.Green;
                middle.Transform = new TranslateTransform3D( new Vector3D( 0.2,0.25,-0.5 ) );

                ScreenSpaceLines3D back = ScreenSpaceLinesFactory.Fat;
                back.Color = Colors.Blue;
                back.Transform = new TranslateTransform3D( new Vector3D( -0.2,-0.75,-5 ) );

                Model3DGroup group = new Model3DGroup();
                group.Children = new Model3DCollection( new Model3D[]{ middle, front, back } );
                return group;
            }
        }
#endif

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup LayeredMeshes
        {
            get
            {
                GeometryModel3D front = new GeometryModel3D(MeshFactory.FullScreenMesh, MaterialFactory.Red);
                front.Transform = new TranslateTransform3D(new Vector3D(0, 0.75, 0));

                GeometryModel3D middle = new GeometryModel3D(MeshFactory.FullScreenMesh, MaterialFactory.Green);
                middle.Transform = new TranslateTransform3D(new Vector3D(0.2, 0.25, -0.5));

                GeometryModel3D back = new GeometryModel3D(MeshFactory.FullScreenMesh, MaterialFactory.Blue);
                back.Transform = new TranslateTransform3D(new Vector3D(-0.2, -0.75, -5));

                Model3DGroup group = new Model3DGroup();
                group.Children = new Model3DCollection(new Model3D[] { middle, front, back, LightFactory.WhiteAmbient });
                return group;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup Xbox
        {
            get
            {
                Model3DGroup group = new Model3DGroup();
                group.Children.Add(MakeXboxChunk(1, 1, 1));
                group.Children.Add(MakeXboxChunk(1, 1, -1));
                group.Children.Add(MakeXboxChunk(1, -1, 1));
                group.Children.Add(MakeXboxChunk(1, -1, -1));
                group.Children.Add(MakeXboxChunk(-1, 1, 1));
                group.Children.Add(MakeXboxChunk(-1, 1, -1));
                group.Children.Add(MakeXboxChunk(-1, -1, 1));
                group.Children.Add(MakeXboxChunk(-1, -1, -1));
                return group;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        private static GeometryModel3D MakeXboxChunk(int x, int y, int z)
        {
            Matrix3D matrix = new Matrix3D();
            matrix.M14 = x * 0.5;
            matrix.M24 = y * 0.5;
            matrix.M34 = z * 0.5;
            matrix.M44 = 2.5;
            GeometryModel3D result = new GeometryModel3D(MeshFactory.SimpleCubeMesh, MaterialFactory.Green);
            result.Transform = new MatrixTransform3D(matrix);
            return result;
        }

        /// <summary>
        /// Facilitates Hit Testing tests.  This is just an empty Model3DGroup.
        /// </summary>
        public static Model3DGroup Group
        {
            get
            {
                Model3DGroup group = new Model3DGroup();
                return group;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup GroupNullChildren
        {
            get
            {
                Model3DGroup group = new Model3DGroup();
                group.Children = null;
                return group;
            }
        }

        /// <summary>
        /// Creates a BobSphere(tm) where each face is a separate model.
        /// </summary>
        public static Model3DGroup GroupSphere(int latitude, int longitude, double radius, Material material)
        {
            Model3DGroup group = new Model3DGroup();

            double latTheta = Math.PI;
            double latDeltaTheta = latTheta / latitude;

            double lonTheta = 0.0;
            double lonDeltaTheta = 2.0 * Math.PI / longitude;

            Point3D origin = new Point3D(0, 0, 0);

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
                    MeshGeometry3D mesh = new MeshGeometry3D();

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

                    Point3D p0 = new Point3D(x00, y0, z00);
                    Point3D p1 = new Point3D(x01, y1, z01);
                    Point3D p2 = new Point3D(x10, y0, z10);
                    Point3D p3 = new Point3D(x11, y1, z11);

                    Vector3D norm0 = p0 - origin;
                    Vector3D norm1 = p1 - origin;
                    Vector3D norm2 = p2 - origin;
                    Vector3D norm3 = p3 - origin;

                    Point tex0 = new Point(u0, 1 - v0);
                    Point tex1 = new Point(u0, 1 - v1);
                    Point tex2 = new Point(u1, 1 - v0);
                    Point tex3 = new Point(u1, 1 - v1);

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

                    mesh.TriangleIndices.Add(0);  // 0
                    mesh.TriangleIndices.Add(1);  // 1
                    mesh.TriangleIndices.Add(2);  // 2
                    mesh.TriangleIndices.Add(3);  // 3
                    mesh.TriangleIndices.Add(2);  // 2
                    mesh.TriangleIndices.Add(1);  // 1

                    mesh.TriangleIndices.Add(2);  // 0
                    mesh.TriangleIndices.Add(1);  // 1
                    mesh.TriangleIndices.Add(0);  // 2
                    mesh.TriangleIndices.Add(1);  // 3
                    mesh.TriangleIndices.Add(2);  // 2
                    mesh.TriangleIndices.Add(3);  // 1

                    GeometryModel3D model = new GeometryModel3D(mesh, material);
                    group.Children.Add(model);
                }
            }

            return group;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup QuadTreeHeightField(
                BitmapSource image,
                int cellSizeX,
                int cellSizeY,
                Material material)
        {
            // Create a base mesh on the XY plane normalized to -1,1 on both axis.
            // Z goes from 0 to 1.
            MeshGeometry3D mesh = MeshFactory.NormalizedHeightFieldFromBitmap(image);

            int width = image.PixelWidth;
            int height = image.PixelWidth;

            return QuadTreeHeightFieldRecursive(
                    new Int32Rect(0, 0, width, height),
                    mesh,
                    material,
                    cellSizeX,
                    cellSizeY,
                    width,
                    height);
        }

        private static Model3DGroup QuadTreeHeightFieldRecursive(
                Int32Rect parentCell,
                MeshGeometry3D mesh,
                Material material,
                int cellSizeX,
                int cellSizeY,
                int originalWidth,
                int originalHeight)
        {
            Model3DGroup currentLevel = new Model3DGroup();

            // check end condition
            if (parentCell.Height <= cellSizeY && parentCell.Width <= cellSizeX)
            {
                // create submesh
                MeshGeometry3D subMesh = new MeshGeometry3D();

                for (int y = parentCell.Y; y < (parentCell.Y + parentCell.Height); y++)
                {
                    for (int x = parentCell.X; x < (parentCell.X + parentCell.Width); x++)
                    {
                        Point3D position = mesh.Positions[x + (y * originalWidth)];
                        Vector3D normal = mesh.Normals[x + (y * originalWidth)];
                        Point uv = mesh.TextureCoordinates[x + (y * originalWidth)];
                        subMesh.Positions.Add(position);
                        subMesh.Normals.Add(normal);
                        subMesh.TextureCoordinates.Add(uv);
                    }
                }
                MeshFactory.AddGridTriangles(ref subMesh, parentCell.Width, parentCell.Height);
                currentLevel.Children.Add(new GeometryModel3D(subMesh, material));
            }
            else
            {
                // divide into 4 ...
                int halfWidth = parentCell.Width / 2;
                int halfHeight = parentCell.Height / 2;

                // add four sub-nodes to this level
                Int32Rect tl = new Int32Rect(
                        parentCell.X,
                        parentCell.Y,
                        halfWidth + 1,
                        halfHeight + 1);
                Int32Rect tr = new Int32Rect(
                        parentCell.X + halfWidth,
                        parentCell.Y,
                        parentCell.Width - halfWidth,
                        halfHeight + 1);
                Int32Rect bl = new Int32Rect(
                        parentCell.X,
                        parentCell.Y + halfHeight,
                        halfWidth + 1,
                        parentCell.Height - halfHeight);
                Int32Rect br = new Int32Rect(
                        parentCell.X + halfWidth,
                        parentCell.Y + halfHeight,
                        parentCell.Width - halfWidth,
                        parentCell.Height - halfHeight);

                currentLevel.Children.Add(QuadTreeHeightFieldRecursive(
                        tl,
                        mesh,
                        material,
                        cellSizeX,
                        cellSizeY,
                        originalWidth,
                        originalHeight));
                currentLevel.Children.Add(QuadTreeHeightFieldRecursive(
                        tr,
                        mesh,
                        material,
                        cellSizeX,
                        cellSizeY,
                        originalWidth,
                        originalHeight));
                currentLevel.Children.Add(QuadTreeHeightFieldRecursive(
                        bl,
                        mesh,
                        material,
                        cellSizeX,
                        cellSizeY,
                        originalWidth,
                        originalHeight));
                currentLevel.Children.Add(QuadTreeHeightFieldRecursive(
                        br,
                        mesh,
                        material,
                        cellSizeX,
                        cellSizeY,
                        originalWidth,
                        originalHeight));
            }

            return currentLevel;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup BipedWalker(bool animated)
        {
            // Simple color scheme ...
            return BipedWalker(
                new DiffuseMaterial(Brushes.Yellow),
                new DiffuseMaterial(Brushes.GreenYellow),
                new DiffuseMaterial(Brushes.Orange),
                new DiffuseMaterial(Brushes.Gray),
                animated);
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3DGroup BipedWalker(
                Material headMaterial,
                Material bodyMaterial,
                Material legMaterial,
                Material footMaterial,
                bool animated)
        {

            // Create meshes
            // hip
            MeshGeometry3D hipMesh = MeshFactory.Sphere(10, 10, 0.25);
            GeometryModel3D hipModel = new GeometryModel3D(hipMesh, bodyMaterial);
            // Use the same material for front and back
            hipModel.BackMaterial = bodyMaterial;
            // head
            MeshGeometry3D headMesh = MeshFactory.Sphere(10, 20, 0.45);
            GeometryModel3D headModel = new GeometryModel3D(headMesh, headMaterial);
            headModel.Transform = new TranslateTransform3D(new Vector3D(0, .6, 0));
            // Use the same material for front and back
            headModel.BackMaterial = headMaterial;
            // Create Groups
            Model3DGroup body = new Model3DGroup();
            Model3DGroup hip = new Model3DGroup();
            Model3DGroup leftLeg = new Model3DGroup();
            Model3DGroup rightLeg = new Model3DGroup();
            // Create Parts
            leftLeg.Transform = new TranslateTransform3D(new Vector3D(.3, 0, 0));
            leftLeg.Children.Add(Leg(legMaterial, footMaterial));
            rightLeg.Transform = new TranslateTransform3D(new Vector3D(-.3, 0, 0));
            rightLeg.Children.Add(Leg(legMaterial, footMaterial));
            hip.Transform = new TranslateTransform3D(new Vector3D(0, 0, 0));
            // Connect Parts
            hip.Children.Add(leftLeg);
            hip.Children.Add(rightLeg);
            hip.Children.Add(hipModel);
            hip.Children.Add(headModel);
            body.Children.Add(hip);

            // Add animation by request
            if (animated)
            {
                // Find code path to actual transforms
                Model3DGroup leftKnee = ((Model3DGroup)((Model3DGroup)leftLeg.Children[0]).Children[0]);
                Model3DGroup rightKnee = ((Model3DGroup)((Model3DGroup)rightLeg.Children[0]).Children[0]);
                Model3DGroup leftAnkle = ((Model3DGroup)((Model3DGroup)leftKnee.Children[0]).Children[0]);
                Model3DGroup rightAnkle = ((Model3DGroup)((Model3DGroup)rightKnee.Children[0]).Children[0]);
                // Animate based on current transforms
                AddAnimatedWalkCycle(
                        (TranslateTransform3D)hip.Transform,
                        (RotateTransform3D)leftLeg.Children[0].Transform,
                        (RotateTransform3D)leftKnee.Children[0].Transform,
                        (RotateTransform3D)leftAnkle.Children[0].Transform,
                        (RotateTransform3D)rightLeg.Children[0].Transform,
                        (RotateTransform3D)rightKnee.Children[0].Transform,
                        (RotateTransform3D)rightAnkle.Children[0].Transform,
                        2000);
            }
            return body;
        }

        private static Model3DGroup Leg(Material legMaterial, Material footMaterial)
        {
            // Create parts
            MeshGeometry3D footMesh = MeshFactory.Sphere(4, 6, 1.1);
            // MeshGeometry3D legMesh = MeshFactory.Spiral( 3, 8, 1.1, 2.2 );
            MeshGeometry3D legMesh = MeshFactory.Sphere(8, 6, 1.1);
            GeometryModel3D footModel = new GeometryModel3D(footMesh, footMaterial);
            // Use the same material for front and back
            footModel.BackMaterial = footMaterial;
            GeometryModel3D topModel = new GeometryModel3D(legMesh, legMaterial);
            // Use the same material for front and back
            topModel.BackMaterial = legMaterial;
            GeometryModel3D bottomModel = new GeometryModel3D(legMesh, legMaterial);
            // Use the same material for front and back
            bottomModel.BackMaterial = legMaterial;

            // Create transforms for parts
            Transform3DGroup legPieceTransform = new Transform3DGroup();
            legPieceTransform.Children.Add(new TranslateTransform3D(new Vector3D(0, -1, 0)));
            legPieceTransform.Children.Add(new ScaleTransform3D(new Vector3D(.1, .3, .1), new Point3D()));
            Transform3DGroup footPieceTransform = new Transform3DGroup();
            footPieceTransform.Children.Add(new TranslateTransform3D(new Vector3D(0, -1, 0.5)));
            footPieceTransform.Children.Add(new ScaleTransform3D(new Vector3D(.2, .1, .3), new Point3D()));

            // Create groups
            Model3DGroup legJoint = new Model3DGroup();
            Model3DGroup knee = new Model3DGroup();
            Model3DGroup kneeJoint = new Model3DGroup();
            Model3DGroup ankle = new Model3DGroup();
            Model3DGroup ankleJoint = new Model3DGroup();

            // mix and match transforms to pieces ...
            footModel.Transform = footPieceTransform;
            ankleJoint.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -20));
            ankleJoint.Children.Add(footModel);
            ankle.Transform = new TranslateTransform3D(new Vector3D(0, -.6, 0));
            ankle.Children.Add(ankleJoint);
            bottomModel.Transform = legPieceTransform;
            kneeJoint.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 40));
            kneeJoint.Children.Add(ankle); // always add groups first
            kneeJoint.Children.Add(bottomModel);
            knee.Transform = new TranslateTransform3D(new Vector3D(0, -.6, 0));
            knee.Children.Add(kneeJoint);
            topModel.Transform = legPieceTransform;
            legJoint.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -20));
            legJoint.Children.Add(knee); // always add groups first
            legJoint.Children.Add(topModel);

            return legJoint;
        }

        private static void AddAnimatedWalkCycle(
                TranslateTransform3D hipPosition,
                RotateTransform3D leftLegRotation,
                RotateTransform3D leftKneeRotation,
                RotateTransform3D leftAnkleRotation,
                RotateTransform3D rightLegRotation,
                RotateTransform3D rightKneeRotation,
                RotateTransform3D rightAnkleRotation,
                int durationInMs)
        {
            DoubleAnimationUsingKeyFrames leftLegRotationAnimation = new DoubleAnimationUsingKeyFrames();
            leftLegRotationAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(durationInMs));
            leftLegRotationAnimation.AutoReverse = false;
            leftLegRotationAnimation.RepeatBehavior = RepeatBehavior.Forever;

            // Clone all rotations
            DoubleAnimationUsingKeyFrames leftKneeRotationAnimation = leftLegRotationAnimation.Clone();
            DoubleAnimationUsingKeyFrames leftAnkleRotationAnimation = leftLegRotationAnimation.Clone();
            DoubleAnimationUsingKeyFrames rightLegRotationAnimation = leftLegRotationAnimation.Clone();
            DoubleAnimationUsingKeyFrames rightKneeRotationAnimation = leftLegRotationAnimation.Clone();
            DoubleAnimationUsingKeyFrames rightAnkleRotationAnimation = leftLegRotationAnimation.Clone();

            DoubleAnimationUsingKeyFrames hipPositionAnimation = new DoubleAnimationUsingKeyFrames();
            hipPositionAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(durationInMs));
            hipPositionAnimation.AutoReverse = false;
            hipPositionAnimation.RepeatBehavior = RepeatBehavior.Forever;

            DoubleKeyFrame current;
            // top leg keys
            DoubleKeyFrame topTipContact = new LinearDoubleKeyFrame(40);
            DoubleKeyFrame topCrossOverBent = new LinearDoubleKeyFrame(-25);
            DoubleKeyFrame topAnkleContact = new LinearDoubleKeyFrame(-40);
            DoubleKeyFrame topCrossOverStraight = new LinearDoubleKeyFrame(0);
            // knee leg keys
            DoubleKeyFrame kneeStraight = new LinearDoubleKeyFrame(0);
            DoubleKeyFrame kneeBent = new LinearDoubleKeyFrame(130);
            // ankle keys
            DoubleKeyFrame ankleTipContact = new LinearDoubleKeyFrame(40);
            DoubleKeyFrame ankleCrossOverBent = new LinearDoubleKeyFrame(0);
            DoubleKeyFrame ankleAnkleContact = new LinearDoubleKeyFrame(-30);
            DoubleKeyFrame ankleGroundContact = new LinearDoubleKeyFrame(10);
            DoubleKeyFrame ankleCrossOverStraight = new LinearDoubleKeyFrame(0);

            // Left leg - top segment
            current = (DoubleKeyFrame)topTipContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.0);
            leftLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topCrossOverBent.Clone();
            current.KeyTime = KeyTime.FromPercent(0.25);
            leftLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topAnkleContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.5);
            leftLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topCrossOverStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.75);
            leftLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topTipContact.Clone();
            current.KeyTime = KeyTime.FromPercent(1.0);
            leftLegRotationAnimation.KeyFrames.Add(current);
            leftLegRotation.Rotation.BeginAnimation(
                    AxisAngleRotation3D.AngleProperty, leftLegRotationAnimation);
            // Right leg - top segment
            current = (DoubleKeyFrame)topAnkleContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.0);
            rightLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topCrossOverStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.25);
            rightLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topTipContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.5);
            rightLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topCrossOverBent.Clone();
            current.KeyTime = KeyTime.FromPercent(0.75);
            rightLegRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)topAnkleContact.Clone();
            current.KeyTime = KeyTime.FromPercent(1);
            rightLegRotationAnimation.KeyFrames.Add(current);
            rightLegRotation.Rotation.BeginAnimation(
                    AxisAngleRotation3D.AngleProperty, rightLegRotationAnimation);

            // Left leg - knee segment
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.0);
            leftKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeBent.Clone();
            current.KeyTime = KeyTime.FromPercent(0.25);
            leftKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.5);
            leftKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.75);
            leftKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(1.0);
            leftKneeRotationAnimation.KeyFrames.Add(current);
            leftKneeRotation.Rotation.BeginAnimation(
                    AxisAngleRotation3D.AngleProperty, leftKneeRotationAnimation);
            // Right leg - knee segment
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.0);
            rightKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.25);
            rightKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.5);
            rightKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeBent.Clone();
            current.KeyTime = KeyTime.FromPercent(0.75);
            rightKneeRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)kneeStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(1);
            rightKneeRotationAnimation.KeyFrames.Add(current);
            rightKneeRotation.Rotation.BeginAnimation(
                    AxisAngleRotation3D.AngleProperty, rightKneeRotationAnimation);

            // Left leg - ankle segment
            current = (DoubleKeyFrame)ankleTipContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.0);
            leftAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleCrossOverBent.Clone();
            current.KeyTime = KeyTime.FromPercent(0.25);
            leftAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleAnkleContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.5);
            leftAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleGroundContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.625);
            leftAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleCrossOverStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.75);
            leftAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleTipContact.Clone();
            current.KeyTime = KeyTime.FromPercent(1.0);
            leftAnkleRotationAnimation.KeyFrames.Add(current);
            leftAnkleRotation.Rotation.BeginAnimation(
                    AxisAngleRotation3D.AngleProperty, leftAnkleRotationAnimation);
            // Right leg - ankle segment
            current = (DoubleKeyFrame)ankleAnkleContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.0);
            rightAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleGroundContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.125);
            rightAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleCrossOverStraight.Clone();
            current.KeyTime = KeyTime.FromPercent(0.25);
            rightAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleTipContact.Clone();
            current.KeyTime = KeyTime.FromPercent(0.5);
            rightAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleCrossOverBent.Clone();
            current.KeyTime = KeyTime.FromPercent(0.75);
            rightAnkleRotationAnimation.KeyFrames.Add(current);
            current = (DoubleKeyFrame)ankleAnkleContact.Clone();
            current.KeyTime = KeyTime.FromPercent(1);
            rightAnkleRotationAnimation.KeyFrames.Add(current);
            rightAnkleRotation.Rotation.BeginAnimation(
                    AxisAngleRotation3D.AngleProperty, rightAnkleRotationAnimation);

            // Body
            DoubleKeyFrame currentPosition;
            // body keys
            DoubleKeyFrame bodyDown = new LinearDoubleKeyFrame(0);
            DoubleKeyFrame bodyUp = new LinearDoubleKeyFrame(0.125);
            currentPosition = (DoubleKeyFrame)bodyDown.Clone();
            currentPosition.KeyTime = KeyTime.FromPercent(0.0);
            hipPositionAnimation.KeyFrames.Add(currentPosition);
            currentPosition = (DoubleKeyFrame)bodyUp.Clone();
            currentPosition.KeyTime = KeyTime.FromPercent(0.25);
            hipPositionAnimation.KeyFrames.Add(currentPosition);
            currentPosition = (DoubleKeyFrame)bodyDown.Clone();
            currentPosition.KeyTime = KeyTime.FromPercent(0.5);
            hipPositionAnimation.KeyFrames.Add(currentPosition);
            currentPosition = (DoubleKeyFrame)bodyUp.Clone();
            currentPosition.KeyTime = KeyTime.FromPercent(0.75);
            hipPositionAnimation.KeyFrames.Add(currentPosition);
            currentPosition = (DoubleKeyFrame)bodyDown.Clone();
            currentPosition.KeyTime = KeyTime.FromPercent(1.0);
            hipPositionAnimation.KeyFrames.Add(currentPosition);
            hipPosition.BeginAnimation(TranslateTransform3D.OffsetYProperty, hipPositionAnimation);
        }
    }
}
