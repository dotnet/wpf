// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Markup;

using Microsoft.Test.Graphics.TestTypes;

using FileMode = System.IO.FileMode;

#if !STANDALONE_BUILD
using TrustedFileStream = Microsoft.Test.Security.Wrappers.FileStreamSW;
using TrustedStreamWriter = Microsoft.Test.Security.Wrappers.StreamWriterSW;
#else
using TrustedFileStream = System.IO.FileStream;
using TrustedStreamWriter = System.IO.StreamWriter;
#endif

namespace Microsoft.Test.Graphics.ReferenceRender
{

    /// <summary>
    /// Render an entire 3D scene using ModelRenderer
    /// </summary>
    public class Bounds
    {
        /// <summary/>
        public Bounds(Size windowSize, Rect viewportBounds, bool clipToViewport)
        {
            this.windowSize = windowSize;
            this.viewportBounds = viewportBounds;
            this.clipToViewport = clipToViewport;
        }

        /// <summary>
        /// Convert Device Independent bounds to Device Dependent bounds
        /// </summary>
        public void ConvertToAbsolutePixels()
        {
            windowSize = MathEx.ConvertToAbsolutePixels(windowSize);
            viewportBounds = MathEx.ConvertToAbsolutePixels(viewportBounds);
        }
        /// <summary/>
        public void ScaleWidthAndHeight(int scalar)
        {
            windowSize = new Size(windowSize.Width * scalar, windowSize.Height * scalar);
            viewportBounds = new Rect(
                                    viewportBounds.X,
                                    viewportBounds.Y,
                                    viewportBounds.Width * scalar,
                                    viewportBounds.Height * scalar);
        }

        /// <summary/>
        public Size WindowSize { get { return windowSize; } }
        /// <summary/>
        public Rect ViewportBounds { get { return viewportBounds; } }
        /// <summary/>
        public bool ClipToViewport { get { return clipToViewport; } }
        /// <summary/>
        public Rect RenderBounds
        {
            get { return clipToViewport ? viewportBounds : new Rect(windowSize); }
        }

        private Size windowSize;
        private Rect viewportBounds;
        private bool clipToViewport;
    }

    /// <summary/>
    public sealed class SceneRenderer
    {
        /// <summary/>
        public SceneRenderer(Size windowSize, Viewport3D viewport, Color background)
            : this(windowSize, viewport, background, DepthTestFunction.LessThanOrEqualTo)
        {
        }

        /// <summary/>
        public SceneRenderer(Size windowSize, Viewport3D viewport, Color background, DepthTestFunction depthTest)
        {
            // TODO: robbrow - 48403
            //      This is a workaround for bug #1371564.  Remove it when the bug is fixed.
            RenderTolerance.IgnoreViewportBorders = true;

            InitializeThis(
                    viewport.Camera,
                    ExtractModels(viewport.Children),
                    new Bounds(windowSize, ComputeBounds(viewport), viewport.ClipToBounds),
                    background,
                    depthTest);
        }

        /// <summary/>
        public SceneRenderer(Size windowSize, Viewport3DVisual visual, Color background)
            : this(windowSize, visual, background, DepthTestFunction.LessThanOrEqualTo)
        {
        }

        /// <summary/>
        public SceneRenderer(Size windowSize, Viewport3DVisual visual, Color background, DepthTestFunction depthTest)
        {
            bool clipToBounds = visual.Clip != null && MathEx.ContainsCloseEnough(visual.Viewport, visual.Clip.Bounds);
            InitializeThis(
                    visual.Camera,
                    ExtractModels(visual.Children),
                    new Bounds(windowSize, visual.Viewport, clipToBounds),
                    background,
                    depthTest);
        }

        /// <summary/>
        public SceneRenderer(Camera camera, Model3DGroup scene, Bounds bounds, Color background)
            : this(camera, scene, bounds, background, DepthTestFunction.LessThanOrEqualTo)
        {
        }

        /// <summary/>
        public SceneRenderer(Camera camera, Model3DGroup scene, Bounds bounds, Color background, DepthTestFunction depthTest)
        {
            InitializeThis(camera, scene, bounds, background, depthTest);
        }

        private Rect ComputeBounds(Viewport3D viewport)
        {
            double x = 0;
            double y = 0;
            Window w = FindContainingWindow(viewport);
            if (w != null)
            {
                // NOTE! Any changes made to viewport.RenderTransform will not be reflected by TransformToAncestor
                // until you re-render the viewport with that new RenderTransform.  Composition team says it's by design.

                GeneralTransform gt = viewport.TransformToAncestor(w);
                Point p = gt.Transform(new Point(0, 0));

                // Because of the above behavior, we must manually undo the RenderTransform
                //  because we don't want to factor in the RenderTransform just yet.
                // It is handled like an effect at the end of the rendering pass.
                if (viewport.RenderTransform != null)
                {
                    p = viewport.RenderTransform.Inverse.Transform(p);
                }
                x = p.X;
                y = p.Y;
            }
            double width = MathEx.FallbackIfNaN(viewport.Width, viewport.ActualWidth);
            double height = MathEx.FallbackIfNaN(viewport.Height, viewport.ActualHeight);
            return new Rect(x, y, width, height);
        }

        private Window FindContainingWindow(FrameworkElement fe)
        {
            if (fe == null)
            {
                return null;
            }
            if (fe is Window)
            {
                return fe as Window;
            }
            return FindContainingWindow(fe.Parent as FrameworkElement);
        }

        private Model3DGroup ExtractModels(Visual3DCollection visuals)
        {
            Model3DGroup group = new Model3DGroup();

            // "visuals" cannot be null because we are not allowed
            //  to set the ModelVisual3D.Children property.
            System.Diagnostics.Debug.Assert(visuals != null, "Visual3DCollection should not be able to be null");

            foreach (Visual3D visual in visuals)
            {
                if (visual is ModelVisual3D)
                {
                    group.Children.Add(CreateModelGroupFromModelVisual3D((ModelVisual3D)visual));
                }

                #if TARGET_NET3_5 
                else if (visual is ModelUIElement3D)
                {
                    group.Children.Add(CreateModelGroupFromModelUIElement3D((ModelUIElement3D)visual));
                }
                else if (visual is Viewport2DVisual3D)
                {
                    group.Children.Add(CreateModelGroupFromViewportVisual3D((Viewport2DVisual3D)visual));
                }
                #endif

                else
                {
                    throw new NotSupportedException("Only ModelVisual3D (+ViewportVisual3D & ModelUIElement3D in 3.5 build) is supported at this time");
                }
            }
            return group;
        }

        #if TARGET_NET3_5 
        private Model3DGroup CreateModelGroupFromViewportVisual3D(Viewport2DVisual3D viewport2DVisual3D)
        {
            // Create a Model3DGroup that contains this visual's Children and Content.
            Model3DGroup group = new Model3DGroup();

            //Pack ViewportVisual3D Content as an Un-transformed GeometryModel3D w/ no back
            GeometryModel3D model = new GeometryModel3D();
            model.Material = viewport2DVisual3D.Material;
            VisualBrush childVisual = new VisualBrush();
            childVisual.ViewportUnits = BrushMappingMode.Absolute;
            childVisual.TileMode = TileMode.None;
            childVisual.Visual = viewport2DVisual3D.Visual;
            PaintChildOntoHostMaterial(model.Material, childVisual);
            model.Geometry = viewport2DVisual3D.Geometry;
            group.Children.Add(model);

            // Put the transform on with the group.
            group.Transform = viewport2DVisual3D.Transform;

            return group;
        }

        private void PaintChildOntoHostMaterial(Material material, VisualBrush childVisual)
        {
            bool foundMaterialToSwap = false;
            Stack<Material> materialStack = new Stack<Material>();
            materialStack.Push(material);

            //Stop searching for swappable material after first. This is how WPF logic behaves.
            while (materialStack.Count > 0 && !foundMaterialToSwap)
            {
                Material currMaterial = materialStack.Pop();
                bool isChildHost = (Boolean)currMaterial.GetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty);

                if (isChildHost)
                {
                    if (currMaterial is DiffuseMaterial)
                    {
                        DiffuseMaterial diffMaterial = (DiffuseMaterial)currMaterial;
                        diffMaterial.Brush = childVisual;                        
                    }
                    else if (currMaterial is EmissiveMaterial)
                    {
                        EmissiveMaterial emmMaterial = (EmissiveMaterial)currMaterial;
                        emmMaterial.Brush = childVisual;                        
                    }
                    else if (currMaterial is SpecularMaterial)
                    {
                        SpecularMaterial specMaterial = (SpecularMaterial)currMaterial;
                        specMaterial.Brush = childVisual;
                    }                   
                    foundMaterialToSwap = true;
                }
                if (currMaterial is MaterialGroup)
                {
                    MaterialGroup matGroup = (MaterialGroup)currMaterial;
                    foreach (Material m in matGroup.Children)
                    {
                        materialStack.Push(m);
                    }
                }
            }
        }
        
        private Model3DGroup CreateModelGroupFromModelUIElement3D(ModelUIElement3D visual)
        {
            // Create a Model3DGroup that contains this ModelUIElement's Model.
            Model3DGroup group = new Model3DGroup();
            if (visual.Model != null)
            {
                group.Children.Add(visual.Model);
            }
        
            // Need to put Content and Children under the same transform.
            group.Transform = visual.Transform;

            return group;
        }
#endif

        private Model3DGroup CreateModelGroupFromModelVisual3D(ModelVisual3D visual)
        {
            // Create a Model3DGroup that contains this visual's Children and Content.
            Model3DGroup group = ExtractModels(visual.Children);
            if (visual.Content != null)
            {
                // Order doesn't really matter, but I like the Content to be first in the collection.
                group.Children.Insert(0, visual.Content);
            }

            // Need to put Content and Children under the same transform.
            group.Transform = visual.Transform;

            return group;
        }        

        private void InitializeThis(Camera camera, Model3DGroup scene, Bounds bounds, Color background, DepthTestFunction depthTest)
        {
            this.background = background;
            bounds.ConvertToAbsolutePixels();

            ExtractLightsAndPrimitives(scene);

            this.camera = camera.CloneCurrentValue();
            AdjustCamera(this.camera, scene);

            // If we are set for 4XAA, we render the model 4 times as big
            if (SceneRenderer.enableAntiAliasedRendering)
            {
                bounds.ScaleWidthAndHeight(2);
            }

            // Create the buffer for this scene
            //For integer conversion, bound on ceiling for consistency w/Dev Bits
            buffer = new RenderBuffer((int)Math.Ceiling(bounds.WindowSize.Width), (int)Math.Ceiling(bounds.WindowSize.Height));

            renderer = new ModelRenderer(bounds, this.camera, buffer, lights);
            renderer.DepthTest = depthTest;

            this.bounds = bounds;
            this.opacity = 1.0;
            this.opacityMask = null;
            this.clip = null;
            this.transform = null;
            this.effect = null;
            this.effectInput = null;
        }

        private void ExtractLightsAndPrimitives(Model3DGroup group)
        {
            ArrayList lightList = new ArrayList();
            ArrayList primitiveList = new ArrayList();

            ExtractLightsAndPrimitivesRecursive(group.CloneCurrentValue(), Matrix3D.Identity, lightList, primitiveList);

            lights = new Light[lightList.Count];
            primitives = new Model3D[primitiveList.Count];

            lightList.CopyTo(lights);
            primitiveList.CopyTo(primitives);
        }

        private void ExtractLightsAndPrimitivesRecursive(Model3D model, Matrix3D tx, IList lightList, IList primitiveList)
        {
            // There's an issue with CloneCurrentValue: it only returns a copy if the animatable is
            // actually animating, otherwise it returns a reference to the original...
            Model3D current = model.CloneCurrentValue();
            // ... so we check if we got the same thing, and in that case we use the .Copy() method!
            if (Model3D.ReferenceEquals(current, model))
            {
                current = model.Clone();
            }
            model = current;
            model.Transform = new MatrixTransform3D(MatrixUtils.Value(model.Transform) * tx);

            if (model is Model3DGroup)
            {
                foreach (Model3D m in ObjectUtils.GetChildren((Model3DGroup)model))
                {
                    // Accessing "model.Transform.Value" is null-safe because we set it a few lines ago.
                    ExtractLightsAndPrimitivesRecursive(m, model.Transform.Value, lightList, primitiveList);
                }
            }
            else if (model is Light)
            {
                lightList.Add(model);
            }
            else if (model is GeometryModel3D)
            {
                primitiveList.Add(model);
            }
#if SSL
            else if ( model is ScreenSpaceLines3D )
            {
                if ( enableAntiAliasedRendering )
                {
                    // we need to adjust SSL since they depend on screen-space :)
                    (model as ScreenSpaceLines3D).Thickness *= 2;
                }
                primitiveList.Add( model );
            }
#endif
            else
            {
                throw new NotSupportedException("I cannot render Model3D of type: " + model.GetType());
            }
        }

        private void AdjustCamera(Camera camera, Model3DGroup scene)
        {
            ProjectionCamera pc = camera as ProjectionCamera;
            if (pc == null)
            {
                // Can't adjust MatrixCamera
                return;
            }

            if (double.IsNaN(pc.NearPlaneDistance) ||
                 double.IsNaN(pc.FarPlaneDistance) ||
                 double.IsPositiveInfinity(pc.NearPlaneDistance) ||
                 double.IsNegativeInfinity(pc.FarPlaneDistance) ||
                 pc.NearPlaneDistance > pc.FarPlaneDistance)
            {
                // Don't render, and don't use NaN.
                pc.NearPlaneDistance = double.MaxValue;
                pc.FarPlaneDistance = double.MaxValue;
                return;
            }

            bool adjustNearPlane = double.IsNegativeInfinity(pc.NearPlaneDistance);
            bool adjustFarPlane = double.IsPositiveInfinity(pc.FarPlaneDistance);

            if (!adjustNearPlane && !adjustFarPlane)
            {
                // Camera is fine.  Leave it alone.
                return;
            }

            Matrix3D view = MatrixUtils.ViewMatrix(this.camera);
            Rect3D sceneBounds = ModelBounder.CalculateBounds(scene, view);

            if (sceneBounds.IsEmpty)
            {
                // It doesn't really matter what these are since there's nothing in the scene.
                // But I do like to avoid infinity...
                pc.NearPlaneDistance = double.MaxValue;
                pc.FarPlaneDistance = double.MaxValue;
            }
            else
            {
                // sceneBounds is aligned to the axes defined by the camera's view matrix,
                //  but it's facing the wrong way!
                //
                //      +-----------+
                //      |sceneBounds|
                //  o---|-----------|---> look direction (-z axis)
                //      |           |
                //      +-----------+
                //      ^           ^
                //   Z+sizeZ        Z
                //
                // o is the camera position (also origin; z == 0)
                // This works regardless of the camera's position relative to the sceneBounds

                double np = pc.NearPlaneDistance;
                double fp = pc.FarPlaneDistance;
                double zBufferEpsilon = Math.Pow(2, -10);  // Add a little padding to the scene bounds
                double sceneNearClip = -(sceneBounds.Z + sceneBounds.SizeZ + zBufferEpsilon);
                double sceneFarClip = -(sceneBounds.Z - zBufferEpsilon);

                // Shrink the user specified clipping planes to the tightest fit around
                //  the scene without modifying what is visible.
                // Do not expand user's clipping plane values (unless we need a little more zBuffer tolerance)!

                if (adjustFarPlane)
                {
                    pc.FarPlaneDistance = sceneFarClip;
                }
                if (adjustNearPlane)
                {
                    pc.NearPlaneDistance = sceneNearClip;
                }

                if (pc.NearPlaneDistance > pc.FarPlaneDistance)
                {
                    // Don't render.
                    pc.NearPlaneDistance = double.MaxValue;
                    pc.FarPlaneDistance = double.MaxValue;
                    return;
                }

                if (pc is PerspectiveCamera && pc.NearPlaneDistance <= 0)
                {
                    // We don't currently handle this case correctly.  Just don't crash.
                    pc.NearPlaneDistance = 0.125;
                }

                AdjustCameraTolerance(pc, adjustNearPlane, adjustFarPlane);
            }
        }

        private void AdjustCameraTolerance(ProjectionCamera camera, bool adjustNearPlane, bool adjustFarPlane)
        {
            double newNearPlane = 0.0;
            double newFarPlane = 0.0;
            double e = 0.125 / (Math.Pow(2, 24) - 1);
            double np = camera.NearPlaneDistance;
            double fp = camera.FarPlaneDistance;

            if (camera is OrthographicCamera)
            {
                // alsteven's magic formula
                newNearPlane = (np * (1 - e) - fp * e) / (1 - 2 * e);
                newFarPlane = (np * e + fp * (e - 1)) / (2 * e - 1);
            }
            else // camera is PerspectiveCamera
            {
                // alsteven's magic formula
                newNearPlane = (np * fp * (2 * e - 1)) / (e * (np + fp) - fp);
                newFarPlane = (np * fp * (2 * e - 1)) / (e * (np + fp) - np);
            }

            System.Diagnostics.Debug.Assert(newNearPlane <= camera.NearPlaneDistance, "New near plane may not clip more than the user asks");
            System.Diagnostics.Debug.Assert(newFarPlane >= camera.FarPlaneDistance, "New far plane may not clip more than the user asks");

            if (adjustNearPlane)
            {
                camera.NearPlaneDistance = newNearPlane;
                RenderTolerance.NearPlaneTolerance = 0.0;
            }
            else
            {
                RenderTolerance.NearPlaneTolerance = camera.NearPlaneDistance - newNearPlane;
            }

            if (adjustFarPlane)
            {
                camera.FarPlaneDistance = newFarPlane;
                RenderTolerance.FarPlaneTolerance = 0.0;
            }
            else
            {
                RenderTolerance.FarPlaneTolerance = newFarPlane - camera.FarPlaneDistance;
            }
        }

        /// <summary/>
        public RenderBuffer Render()
        {
            return Render(InterpolationMode.Gouraud);
        }

        /// <summary/>
        public RenderBuffer Render(InterpolationMode interpolation)
        {
            return Render(interpolation, true);
        }

        /// <summary/>
        public RenderBuffer RenderWithoutBackground()
        {
            return Render(InterpolationMode.Gouraud, false);
        }

        private RenderBuffer Render(InterpolationMode interpolation, bool renderBackground)
        {
            for (int i = 0; i < primitives.Length; i++)
            {
                // Render one model to composite buffer
                renderer.Render(primitives[i], interpolation);
            }
            AddClippingTolerance();

            if (opacity != 1.0)
            {
                buffer.ApplyOpacityMask(new SolidColorBrush(ColorOperations.ColorFromArgb(opacity, 0, 0, 0)));
            }
            buffer.ApplyOpacityMask(opacityMask);
            buffer.ApplyEffect(effect, effectInput);
            buffer.ApplyClip(clip);
            buffer.ApplyTransform(transform);

            if (renderBackground)
            {
                buffer.AddBackground(background);
            }

            // Add default tolerances adds tolerance for all rendered pixels.
            // The reason we do this after adding the background is because specular lighting
            //  does not write to the z-buffer, and therefore we will not get default tolerance
            //  near the falloff region on meshes that only use a SpecularMaterial.
            buffer.AddDefaultTolerances();

            if (SceneRenderer.enableAntiAliasedRendering)
            {
                // If we are set for 4XAA, we already rendered this 4X as big.
                // Do a downsampling blend to get the AA result.
                buffer = buffer.DownSample4X();
            }
            buffer.EnsureCorrectBitDepth();

            return buffer;
        }

        /// <summary>
        /// Add tolerance around the ViewportBounds if the ClipToBounds property is set.
        /// </summary>
        private void AddClippingTolerance()
        {
            if (bounds.ClipToViewport && RenderTolerance.ViewportClippingTolerance > 0)
            {
                SilhouetteEdgeTriangle[] edgeTriangles = new SilhouetteEdgeTriangle[8];
                double tolerance = RenderTolerance.ViewportClippingTolerance;
                double left = bounds.ViewportBounds.Left;
                double right = bounds.ViewportBounds.Right;
                double top = bounds.ViewportBounds.Top;
                double bottom = bounds.ViewportBounds.Bottom;
                Point3D topLeft = new Point3D(left, top, 0);
                Point3D topRight = new Point3D(right, top, 0);
                Point3D bottomLeft = new Point3D(left, bottom, 0);
                Point3D bottomRight = new Point3D(right, bottom, 0);
                edgeTriangles[0] = new SilhouetteEdgeTriangle(topLeft, topRight, tolerance);
                edgeTriangles[1] = new SilhouetteEdgeTriangle(topRight, topLeft, tolerance);
                edgeTriangles[2] = new SilhouetteEdgeTriangle(topRight, bottomRight, tolerance);
                edgeTriangles[3] = new SilhouetteEdgeTriangle(bottomRight, topRight, tolerance);
                edgeTriangles[4] = new SilhouetteEdgeTriangle(bottomRight, bottomLeft, tolerance);
                edgeTriangles[5] = new SilhouetteEdgeTriangle(bottomLeft, bottomRight, tolerance);
                edgeTriangles[6] = new SilhouetteEdgeTriangle(bottomLeft, topLeft, tolerance);
                edgeTriangles[7] = new SilhouetteEdgeTriangle(topLeft, bottomLeft, tolerance);

                RenderToToleranceShader ignoreClipBounds = new RenderToToleranceShader(edgeTriangles, buffer);

                // Rasterize to WindowBounds because RenderBounds (==ViewportBounds in this case)
                //  will clip the outside of the silhouette edge triangles.
                ignoreClipBounds.Rasterize(new Rect(bounds.WindowSize));
            }
        }

        /// <summary/>
        public void SaveSelectedSubSceneAsXaml(Point[] selectionPixels, string fileName)
        {
            // Add Those triangles which we selected
            Model3DGroup mg = new Model3DGroup();
            List<string> supportFiles = new List<string>();
            for (int i = 0; i < primitives.Length; i++)
            {
                if (primitives[i] is GeometryModel3D)
                {
                    GeometryModel3D gModel = (GeometryModel3D)primitives[i];

                    MeshGeometry3D saveMesh = null;
                    if (selectionPixels != null)
                    {
                        // Only save those meshes that actually had failed geometry
                        saveMesh = renderer.ExtractTrianglesAtPoints(
                                (MeshGeometry3D)gModel.Geometry,
                                gModel.Transform,
                                selectionPixels);
                    }
                    else
                    {
                        // Save the entire mesh
                        saveMesh = (MeshGeometry3D)gModel.Geometry;
                    }
                    if (saveMesh != null && saveMesh.Positions.Count > 0)
                    {
                        // Work around serialization problems with non URI based images
                        SerializeGeneratedWorkaround(gModel.Material, fileName, supportFiles);
                        SerializeGeneratedWorkaround(gModel.BackMaterial, fileName, supportFiles);
                        GeometryModel3D saveModel = new GeometryModel3D(saveMesh, gModel.Material);
                        saveModel.BackMaterial = gModel.BackMaterial;
                        saveModel.Transform = gModel.Transform;
                        mg.Children.Add(saveModel);
                    }
                }
            }
            // Add all lights
            for (int i = 0; i < lights.Length; i++)
            {
                mg.Children.Add(lights[i]);
            }

            // Set the viewport
            Viewport3D savedViewport = new Viewport3D();
            ModelVisual3D modelVisual = new ModelVisual3D();
            modelVisual.Content = mg;
            savedViewport.Children.Add(modelVisual);
            savedViewport.Camera = camera;
            // Add the viewport to a panel with a Background
            DockPanel parent = new DockPanel();
            parent.Background = new SolidColorBrush(background);
            parent.Children.Add(savedViewport);

            using (TrustedStreamWriter sw = new TrustedStreamWriter(fileName))
            {
                string serialization = XamlWriter.Save(parent);
                // Make the thing readable ...
                serialization = serialization.Replace("xml:space=\"preserve\"", "");
                serialization = serialization.Replace("><", ">\n<");
                int indentLevel = -1;
                string indent = "   ";
                bool lastLineClosed = false;
                foreach (string line in serialization.Split('\n'))
                {
                    if (line.StartsWith("</"))
                    {
                        indentLevel--;
                        lastLineClosed = true;
                    }
                    else if (line.StartsWith("<"))
                    {
                        if (!lastLineClosed)
                        {
                            indentLevel++;
                        }
                        lastLineClosed = false;
                        if (line.Contains("/>"))
                        {
                            lastLineClosed = true;
                        }
                    }
                    for (int i = 0; i < indentLevel; i++)
                    {
                        sw.Write(indent);
                    }
                    sw.WriteLine(line);
                }

                // Add a list of support files added
                sw.WriteLine("<!--");
                sw.WriteLine("   Minimal XAML Repro brought to you by Avalon 3D test.");
                sw.WriteLine();
                sw.WriteLine("   Related support files needed:");
                foreach (string supportFile in supportFiles)
                {
                    sw.WriteLine("      " + supportFile);
                }
                sw.WriteLine("-->");
            }
        }

        private void SerializeGeneratedWorkaround(Material material, string masterFileName, List<string> supportFiles)
        {
            // This is method is needed because of this issue:
            // (Bug 908217 - Windows OS Bugs - ARCHITECTURAL QUESTION: Serialization: serialization of ImageData asumes URI )
            //
            // NOTE: If there are any memory-generated objects that don't serialize,
            //    these are replaced by equivalent file-based ones that do.

            if (material is MaterialGroup)
            {
                foreach (Material m in ((MaterialGroup)material).Children)
                {
                    SerializeGeneratedWorkaround(m, masterFileName, supportFiles);
                }
            }
            else
            {
                Brush brush = null;
                if (material is DiffuseMaterial)
                {
                    DiffuseMaterial dm = (DiffuseMaterial)material;
                    brush = dm.Brush;
                }
                else if (material is EmissiveMaterial)
                {
                    EmissiveMaterial em = (EmissiveMaterial)material;
                    brush = em.Brush;
                }
                else if (material is SpecularMaterial)
                {
                    SpecularMaterial sm = (SpecularMaterial)material;
                    brush = sm.Brush;
                }

                if (brush is ImageBrush)
                {
                    ImageBrush ib = (ImageBrush)brush;

                    // NOTE: Other forms of memory-generated bitmaps may need the same treatment.
                    if (ib.ImageSource is CachedBitmap)
                    {
                        CachedBitmap cb = (CachedBitmap)ib.ImageSource;
                        // Generate new file Name
                        int fileIndex = supportFiles.Count;
                        string fileName = masterFileName.Replace(".xaml", "_support_")
                                + fileIndex.ToString() + ".png";
                        // Save as an image file (PNG to keep transparency)

                        PhotoConverter.SaveImageAs(cb, fileName);

                        // Remember the name
                        supportFiles.Add(fileName);
                        // Now replace this in the old brush
                        ib.ImageSource = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));
                        ib.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    }
                }
                // The default case is for this to be a NO-OP
            }
        }

        internal static bool EnableAntiAliasedRendering
        {
            get { return enableAntiAliasedRendering; }
            set { enableAntiAliasedRendering = value; }
        }

        internal Geometry Clip
        {
            get { return clip; }
            set { clip = value; }
        }

        internal double Opacity
        {
            get { return opacity; }
            set { opacity = value; }
        }

        internal Brush OpacityMask
        {
            get { return opacityMask; }
            set { opacityMask = value; }
        }

        internal Transform Transform
        {
            get { return transform; }
            set { transform = value; }
        }

        internal BitmapEffect BitmapEffect
        {
            get { return effect; }
            set { effect = value; }
        }

        internal BitmapEffectInput BitmapEffectInput
        {
            get { return effectInput; }
            set { effectInput = value; }
        }

        private ModelRenderer renderer;
        private Bounds bounds;
        private Light[] lights;
        private Model3D[] primitives;
        private Color background;
        private Camera camera;
        private RenderBuffer buffer;
        private Geometry clip;
        private double opacity;
        private Brush opacityMask;
        private Transform transform;
        private BitmapEffect effect;
        private BitmapEffectInput effectInput;

        private static bool enableAntiAliasedRendering = false;
    }
}
