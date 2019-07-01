// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Markup;

using Microsoft.Test.Graphics.ReferenceRender;

using FileMode = System.IO.FileMode;

#if !STANDALONE_BUILD
using TrustedFileStream = Microsoft.Test.Security.Wrappers.FileStreamSW;
using Microsoft.Test.Graphics.Factories;
#else
using TrustedFileStream = System.IO.FileStream;
using Microsoft.Test.Graphics.Factories;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Services for parsing Variations
    /// </summary>
    public abstract class TestObjects
    {
        /// <summary/>
        public TestObjects(Variation v)
        {
            this.variation = v;
            this.viewport = null;
            this.visual = null;
            this.content = null;

            string ctb = v["ClipToBounds"];
            string o = v["Opacity"];
            string om = v["OpacityMask"];
            string c = v["Clip"];
            string t = v["Transform"];
            string e = v["Effect"];
            string ei = v["EffectInput"];
            string em = v["EdgeMode"];

            clipToBounds = (ctb == null) ? false : StringConverter.ToBool(ctb);
            clip = (c == null) ? null : GeometryFactory.MakeGeometry(c);
            opacity = (o == null) ? 1.0 : StringConverter.ToDouble(o);
            opacityMask = (om == null) ? null : BrushFactory.MakeBrush(om);
            transform = (t == null) ? null : Transform2DFactory.MakeTransform2D(t);
            effect = (e == null) ? null : EffectFactory.MakeEffect(e);
            effectInput = (ei == null) ? null : EffectInputFactory.MakeEffectInput(ei);
            edgeMode = (em == null) ? EdgeMode.Unspecified : (EdgeMode)Enum.Parse(typeof(EdgeMode), em);

            // Don't do DPI scaling here.  Avalon needs unscaled input.
            // We will do the scale in the verification code.
            MathEx.RelativeToAbsolute(opacityMask, new Rect(0, 0, v.WindowWidth, v.WindowHeight));

            // Can't scale effectInput here because we don't know the rendered bounds yet.
        }

        /// <summary/>
        public Visual Content
        {
            get
            {
                if (content == null)
                {
                    if (variation.UseViewport3D)
                    {
                        Canvas canvas = new Canvas();
                        canvas.Background = Brushes.Transparent;
                        canvas.Width = variation.WindowWidth;
                        canvas.Height = variation.WindowHeight;
                        canvas.Children.Add(Viewport);

                        content = canvas;
                    }
                    else
                    {
                        content = Visual;
                    }
                }
                return content;
            }
        }

        /// <summary/>
        public Viewport3D Viewport
        {
            get
            {
                if (viewport == null)
                {
                    viewport = CreateViewport3D();
                    Rect rect = variation.ViewportRect;

                    viewport.Width = rect.Width;
                    viewport.Height = rect.Height;
                    viewport.SetValue(Canvas.LeftProperty, rect.Left);
                    viewport.SetValue(Canvas.TopProperty, rect.Top);
                    viewport.ClipToBounds = clipToBounds;
                    viewport.Opacity = opacity;
                    viewport.OpacityMask = opacityMask;
                    viewport.Clip = clip;
                    viewport.RenderTransform = transform;
#pragma warning disable 0618
                    viewport.BitmapEffect = effect;
                    viewport.BitmapEffectInput = effectInput;
#pragma warning restore 0618
                    RenderOptions.SetEdgeMode(viewport, edgeMode);
                    viewport.FlowDirection = variation.FlowDirection;
                }
                return viewport;
            }
        }

        /// <summary/>
        public Viewport3DVisual Visual
        {
            get
            {
                if (visual == null)
                {
                    visual = CreateVisual();
                    visual.Viewport = variation.ViewportRect;
                    if (clipToBounds)
                    {
                        if (clip == null)
                        {
                            clip = new RectangleGeometry(variation.ViewportRect);
                        }
                        else
                        {
                            GeometryGroup group = new GeometryGroup();
                            group.Children.Add(new RectangleGeometry(variation.ViewportRect));
                            group.Children.Add(clip);
                            clip = group;
                        }
                    }
                    visual.Opacity = opacity;
                    visual.OpacityMask = opacityMask;
                    visual.Clip = clip;
#pragma warning disable 0618
                    visual.Transform = transform;
                    visual.BitmapEffect = effect;
                    if (effectInput != null)
                    {
                        visual.BitmapEffectInput = effectInput;
                    }
#pragma warning restore 0618
                    RenderOptions.SetEdgeMode(visual, edgeMode);
                }
                return visual;
            }
        }

        /// <summary/>
        public virtual SceneRenderer SceneRenderer
        {
            get
            {
                Transform flowTransform = transform;
                if (variation.FlowDirection == FlowDirection.RightToLeft)
                {
                    // RTL layout pushes a flip in the transform-to-ancestor stack.
                    // This will undo/redo it.
                    flowTransform = new MatrixTransform(
                            MatrixUtils.Value(transform) *
                            MatrixUtils.Scale(-1, 1, new Point(variation.WindowWidth / 2.0, 0)));
                }

                SceneRenderer renderer;
                if (variation.UseViewport3D)
                {
                    // SceneRenderer needs us to undo the Flow transform added by Avalon.
                    Viewport.RenderTransform = flowTransform;
                    renderer = new SceneRenderer(variation.WindowSize, Viewport, variation.BackgroundColor);
                    // Now we can set it back.
                    Viewport.RenderTransform = transform;
                }
                else
                {
                    renderer = new SceneRenderer(variation.WindowSize, Visual, variation.BackgroundColor);
                }

                renderer.Clip = clip;
                renderer.Opacity = opacity;
                renderer.OpacityMask = opacityMask;
                renderer.Transform = flowTransform;
                renderer.BitmapEffect = effect;
                renderer.BitmapEffectInput = effectInput;

                return renderer;
            }
        }

        /// <summary/>
        protected abstract Viewport3D CreateViewport3D();
        /// <summary/>
        protected abstract Viewport3DVisual CreateVisual();

        /// <summary/>
        protected Variation variation;
        private Viewport3D viewport;
        private Viewport3DVisual visual;
        private Visual content;
        private bool clipToBounds;
        private Geometry clip;
        private double opacity;
        private Brush opacityMask;
        private Transform transform;
        private BitmapEffect effect;
        private BitmapEffectInput effectInput;
        private EdgeMode edgeMode;
    }

    /// <summary/>
    public class UnitTestObjects : TestObjects
    {
        /// <summary/>
        public UnitTestObjects(Variation v)
            : base(v)
        {
            model = FactoryParser.MakeModel(v);
            camera = FactoryParser.MakeCamera(v);
            light = FactoryParser.MakeLight(v);
            transform = FactoryParser.MakeTransform3D(v);
            modelVisual = CreateModelVisual(v);
        }

        private ModelVisual3D CreateModelVisual(Variation v)
        {
            Model3DGroup group = new Model3DGroup();
            group.Children = new Model3DCollection(new Model3D[] { light, model });

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = group;
            SetTransform(visual, v);

            return visual;
        }

        private void SetTransform(ModelVisual3D visual, Variation v)
        {
            switch (v["TransformTarget"])
            {
                case "Camera":
                    camera.Transform = transform;
                    break;

                case "Light":
                    light.Transform = transform;
                    break;

                case "Model":
                    model.Transform = transform;
                    break;

                case "Visual3D":
                    visual.Transform = transform;
                    break;

                case "Group":
                default:
                    visual.Content.Transform = transform;
                    break;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Camera Camera { get { return camera; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Light Light { get { return light; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Model3D Model { get { return model; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Transform3D Transform { get { return transform; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public ModelVisual3D ModelVisual { get { return modelVisual; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Model3DGroup Group { get { return (Model3DGroup)modelVisual.Content; } }

        /// <summary/>
        protected override Viewport3D CreateViewport3D()
        {
            Viewport3D viewport = new Viewport3D();
            viewport.Camera = camera;
            viewport.Children.Add(modelVisual);
            return viewport;
        }

        /// <summary/>
        protected override Viewport3DVisual CreateVisual()
        {
            Viewport3DVisual visual = new Viewport3DVisual();
            visual.Camera = camera;
            visual.Children.Add(modelVisual);
            return visual;
        }

        private Camera camera;
        private Light light;
        private Model3D model;
        private Transform3D transform;
        private ModelVisual3D modelVisual;
    }


    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class XamlTestObjects : TestObjects
    {
        /// <summary/>
        public XamlTestObjects(Variation v)
            : base(v)
        {
            v.AssertExistenceOf("Filename");
            filename = v["Filename"];
        }

        /// <summary/>
        protected override Viewport3D CreateViewport3D()
        {
            return GetViewport3DFromXamlFile();
        }

        /// <summary/>
        protected override Viewport3DVisual CreateVisual()
        {
            throw new ApplicationException("Xaml tests should not use Viewport3DVisual");
        }

        private Viewport3D GetViewport3DFromXamlFile()
        {
            FrameworkElement root = (FrameworkElement)SerializationTest.ParseXaml(filename);

            if (!(root is Viewport3D))
            {
                root = GetViewport3DFromFrameworkElement(root);
            }
            if (root == null)
            {
                throw new ApplicationException("Could not find Viewport3D in " + filename);
            }
            return (Viewport3D)root;
        }

        // Do a depth-first search on the LogicalTree and return the first
        //  Viewport3D that I find

        private FrameworkElement GetViewport3DFromFrameworkElement(FrameworkElement root)
        {
            foreach (FrameworkElement child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is Viewport3D)
                {
                    // We need to remove the Viewport3D from the LogicalTree.
                    // This is going to be the new root Visual and it cannot have a Parent.

                    if (root is Panel)
                    {
                        ((Panel)root).Children.Remove(child);
                    }
                    else if (root is Decorator)
                    {
                        ((Decorator)root).Child = null;
                    }

                    return child;
                }
                FrameworkElement value = GetViewport3DFromFrameworkElement(child);
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary/>
        public void ThawViewport()
        {
            VisualUtils.ThawVisuals(Viewport.Children);
            Viewport.Camera = Viewport.Camera.Clone();
            if (Viewport.Clip != null)
            {
                Viewport.Clip = Viewport.Clip.Clone();
            }
            if (Viewport.OpacityMask != null)
            {
                Viewport.OpacityMask = Viewport.OpacityMask.Clone();
            }
            if (Viewport.RenderTransform != null)
            {
                Viewport.RenderTransform = Viewport.RenderTransform.Clone();
            }
#pragma warning disable 0618
            if (Viewport.BitmapEffect != null)
            {
                Viewport.BitmapEffect = Viewport.BitmapEffect.Clone();
            }
            if (Viewport.BitmapEffectInput != null)
            {
                Viewport.BitmapEffectInput = Viewport.BitmapEffectInput.Clone();
            }
#pragma warning restore 0618
        }

        /// <summary/>
        public override SceneRenderer SceneRenderer
        {
            get
            {
                SceneRenderer renderer = new SceneRenderer(variation.WindowSize, Viewport, variation.BackgroundColor);

                renderer.Clip = Viewport.Clip;
                renderer.Opacity = Viewport.Opacity;
                renderer.OpacityMask = Viewport.OpacityMask;
                renderer.Transform = Viewport.RenderTransform;
#pragma warning disable 0618
                renderer.BitmapEffect = Viewport.BitmapEffect;
                renderer.BitmapEffectInput = Viewport.BitmapEffectInput;
#pragma warning restore 0618
                return renderer;
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public string Filename { get { return filename; } }

        private string filename;
    }


    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class SceneTestObjects : TestObjects
    {
        /// <summary/>
        public SceneTestObjects(Variation v)
            : base(v)
        {
            visual3Ds = FactoryParser.MakeScene(v);
            camera = FactoryParser.MakeCamera(v);
            camera.Transform = TransformFactory.MakeTransform(v["CameraTransform"]);

            ObjectUtils.NameObjects(visual3Ds);
        }

        /// <summary/>
        protected override Viewport3D CreateViewport3D()
        {
            Viewport3D viewport = new Viewport3D();
            viewport.Camera = camera;

            foreach (Visual3D visual3D in visual3Ds)
            {
                viewport.Children.Add(visual3D);
            }

            return viewport;
        }

        /// <summary/>
        protected override Viewport3DVisual CreateVisual()
        {
            Viewport3DVisual visual = new Viewport3DVisual();
            visual.Camera = camera;

            foreach (Visual3D visual3D in visual3Ds)
            {
                visual.Children.Add(visual3D);
            }
            return visual;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Visual3D[] Visual3Ds { get { return visual3Ds; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Camera Camera { get { return camera; } }

        private Visual3D[] visual3Ds;
        private Camera camera;
    }
}
