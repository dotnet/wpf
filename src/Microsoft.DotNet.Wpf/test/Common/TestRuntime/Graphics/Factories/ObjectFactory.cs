// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Make anything and everything you can think of. Created specifically for 
    /// enabling reference-type animations in AnimationAPITest.
    /// </summary>
    public class ObjectFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object MakeObject(string value)
        {
            switch (value)
            {
                case "WhiteBrush": return WhiteBrush;
                case "BlackBrush": return BlackBrush;
                case "RedBrush": return RedBrush;
                case "BlackMaterial": return BlackMaterial;
                case "GreenMaterial": return GreenMaterial;
                case "BlueMaterial": return BlueMaterial;
                case "ShinyBlue": return ShinyBlue;
                case "ShinyRed": return ShinyRed;
                case "ShinyBlack": return ShinyBlack;

                case "SingleTriangle": return SingleTriangle;
                case "DoubleTriangle": return DoubleTriangle;
                case "Diamond": return Diamond;
                case "Evolution": return Evolution;
                case "Spheres": return Spheres;
                case "Discs": return Discs;

                case "RotateX30": return RotateX30;
                case "RotateY300": return RotateY300;
                case "RotateZ90": return RotateZ90;
                case "Rotate0": return Rotate0;
                case "Translate": return Translate;
                case "Rotate": return Rotate;
                case "Scale": return Scale;
                case "TranslateRotate": return TranslateRotate;
                case "RotateTranslate": return RotateTranslate;
                case "ScaleTranslate": return ScaleTranslate;

                // All of the following Collections are meant to be used with MeshFactory.FullScreenMesh
                case "StretchLeft": return StretchLeft;
                case "StretchRight": return StretchRight;
                case "StretchUp": return StretchUp;
                case "Perpendicular": return Perpendicular;
                case "ScatteredOut": return ScatteredOut;
                case "ScatteredIn": return ScatteredIn;
                case "UVRotate90": return UVRotate90;
                case "UVRotate180": return UVRotate180;
                case "UVRotate270": return UVRotate270;
                case "PacmanRight": return PacmanRight;
                case "PacmanLeft": return PacmanLeft;
                case "PacmanUp": return PacmanUp;
            }
            throw new ArgumentException("Specified object (" + value + ") cannot be created");
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object WhiteBrush { get { return Brushes.White.Clone(); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object BlackBrush { get { return Brushes.Black.Clone(); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object RedBrush { get { return Brushes.Red.Clone(); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object BlackMaterial { get { return MaterialFactory.Black; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object GreenMaterial { get { return MaterialFactory.Green; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object BlueMaterial { get { return MaterialFactory.Blue; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object ShinyBlue
        {
            get
            {
                Material[] materials = new Material[] { MaterialFactory.Blue, MaterialFactory.DefaultSpecular };
                return new MaterialCollection(materials);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object ShinyRed
        {
            get
            {
                Material[] materials = new Material[] { MaterialFactory.Red, MaterialFactory.DefaultSpecular };
                return new MaterialCollection(materials);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object ShinyBlack
        {
            get
            {
                Material[] materials = new Material[] { MaterialFactory.Black, MaterialFactory.DefaultSpecular };
                return new MaterialCollection(materials);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object SingleTriangle { get { return MeshFactory.SingleFrontFacingTriangle; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object DoubleTriangle { get { return MeshFactory.DoubleTriangleMesh; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Diamond { get { return MeshFactory.CreateFlatDisc(4); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Evolution
        {
            get
            {
                Model3D[] models = new Model3D[]{
                        new GeometryModel3D( MeshFactory.SingleFrontFacingTriangle, MaterialFactory.Red ),
                        new GeometryModel3D( MeshFactory.DoubleTriangleMesh, MaterialFactory.Green ),
                        new GeometryModel3D( MeshFactory.CreateFlatDisc( 4 ), MaterialFactory.Blue ),
                        LightFactory.WhiteAmbient };
                models[0].Transform = new TranslateTransform3D(new Vector3D(-2, 0, 0));
                models[2].Transform = new TranslateTransform3D(new Vector3D(2, 0, 0));
                return new Model3DCollection(models);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Spheres
        {
            get
            {
                Model3D[] models = new Model3D[]{
                        new GeometryModel3D( MeshFactory.Sphere( 24,48,0.5 ), MaterialFactory.Red ),
                        new GeometryModel3D( MeshFactory.Sphere( 24,48,1.0 ), MaterialFactory.Green ),
                        new GeometryModel3D( MeshFactory.Sphere( 24,48,1.5 ), MaterialFactory.Blue ),
                        LightFactory.WhitePoint };
                models[0].Transform = new TranslateTransform3D(new Vector3D(-1.5, 0, 0));
                models[2].Transform = new TranslateTransform3D(new Vector3D(2.5, 0, 0));
                return new Model3DCollection(models);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Discs
        {
            get
            {
                Model3D[] models = new Model3D[]{
                        new GeometryModel3D( MeshFactory.CreateFlatDisc( 5 ), MaterialFactory.Red ),
                        new GeometryModel3D( MeshFactory.CreateFlatDisc( 6 ), MaterialFactory.Green ),
                        new GeometryModel3D( MeshFactory.CreateFlatDisc( 8 ), MaterialFactory.Blue ),
                        LightFactory.WhiteAmbient };
                models[0].Transform = new TranslateTransform3D(new Vector3D(-2, 0, 0));
                models[2].Transform = new TranslateTransform3D(new Vector3D(2, 0, 0));
                return new Model3DCollection(models);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object RotateX30 { get { return new AxisAngleRotation3D(Const.xAxis, 30); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object RotateY300 { get { return new AxisAngleRotation3D(Const.yAxis, 300); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object RotateZ90 { get { return new AxisAngleRotation3D(Const.zAxis, 90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Rotate0 { get { return new AxisAngleRotation3D(Const.v0, 90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Translate { get { return Const.tt1; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Rotate { get { return Const.rtZ135; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Scale { get { return new ScaleTransform3D(new Vector3D(2, 2, 2)); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object TranslateRotate
        {
            get
            {
                Transform3D[] transforms = new Transform3D[] { new TranslateTransform3D(new Vector3D(1, 0, 0)), Const.rtZ135 };
                return new Transform3DCollection(transforms);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object RotateTranslate
        {
            get
            {
                Transform3D[] transforms = new Transform3D[] { Const.rtZ135, new TranslateTransform3D(new Vector3D(1, 0, 0)) };
                return new Transform3DCollection(transforms);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object ScaleTranslate
        {
            get
            {
                Transform3D[] transforms = new Transform3D[] { new ScaleTransform3D(new Vector3D(2, 2, 2)), new TranslateTransform3D(new Vector3D(1, 0, 0)) };
                return new Transform3DCollection(transforms);
            }
        }


        // This is the mesh we're dealing with for these objects
        //
        //  2 +-----+ 3
        //    |\    |
        //    |  \  |
        //    |    \|
        //  0 +-----+ 1
        //

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object StretchLeft { get { return Point3DCollection.Parse("-2,-2,0  1,-1,0  -2,2,0  1,1,0"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object StretchRight { get { return Point3DCollection.Parse("-1,-1,0  2,-2,0  -1,1,0  2,2,0"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object StretchUp { get { return Point3DCollection.Parse("-1,-1,0  1,-1,0  -2,2,0  2,2,0"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object Perpendicular { get { return Vector3DCollection.Parse("0,0,1  0,0,1  0,0,1  0,0,1"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object ScatteredOut { get { return Vector3DCollection.Parse("-1,-1,1  1,-1,1  -1,1,1  1,1,1"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object ScatteredIn { get { return Vector3DCollection.Parse("1,1,1  -1,1,1  1,-1,1  -1,-1,1"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object UVRotate90 { get { return PointCollection.Parse("0,0  0,1  1,0  1,1"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object UVRotate180 { get { return PointCollection.Parse("1,0  0,0  1,1  0,1"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object UVRotate270 { get { return PointCollection.Parse("1,1  1,0  0,1  0,0"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object PacmanRight { get { return Int32Collection.Parse("0 1 2 0 3 2"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object PacmanLeft { get { return Int32Collection.Parse("0 1 3 2 1 3"); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static object PacmanUp { get { return Int32Collection.Parse("0 1 2 0 1 3"); } }
    }
}
