// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// Helper functions for Visuals
    /// </summary>
    public class VisualUtils
    {
        /// <summary/>
        public static Visual3D GetChild(Visual3D visual, params int[] indices)
        {
            if (indices == null)
            {
                throw new ArgumentNullException("indices cannot be null");
            }
            Visual3D result = visual;
            foreach (int index in indices)
            {
                // Assumes a tree full of ModelVisual3Ds and no indices out of range.
                result = ((ModelVisual3D)result).Children[index];
            }
            return result;
        }

        /// <summary/>
        public static void SetChildContent(Visual3D visual, Model3D model, params int[] indices)
        {
            if (indices == null)
            {
                throw new ArgumentNullException("indices cannot be null");
            }

            Visual3D iterator = visual;
            foreach (int index in indices)
            {
                // Assumes a tree full of ModelVisual3Ds and no indices out of range.
                iterator = ((ModelVisual3D)iterator).Children[index];
            }

            ((ModelVisual3D)iterator).Content = model;
        }

        /// <summary/>
        public static void SetChildTransform(Visual3D visual, Transform3D transform, params int[] indices)
        {
            if (indices == null)
            {
                throw new ArgumentNullException("indices cannot be null");
            }

            Visual3D iterator = visual;
            foreach (int index in indices)
            {
                // Assumes a tree full of ModelVisual3Ds and no indices out of range.
                iterator = ((ModelVisual3D)iterator).Children[index];
            }

            ((ModelVisual3D)iterator).Transform = transform;
        }

        /// <summary/>
        public static bool IsAncestorOf(DependencyObject ancestor, DependencyObject child)
        {
            if (ancestor == null || child == null)
            {
                return false;
            }
            if (object.ReferenceEquals(child, ancestor))
            {
                return true;
            }
            return IsAncestorOf(ancestor, VisualTreeHelper.GetParent(child));
        }

        /// <summary/>
        public static DependencyObject GetLeastCommonAncestor(DependencyObject visual1, DependencyObject visual2)
        {
            MarkAncestors(visual1);
            DependencyObject result = FindFirstMarkedVisual(visual2);
            UnmarkAncestors(visual1);
            return result;
        }

        private static void MarkAncestors(DependencyObject visual)
        {
            if (visual is Visual || visual is Visual3D)
            {
                ObjectUtils.SetName(visual, marker);
                MarkAncestors(VisualTreeHelper.GetParent(visual));
            }
        }

        private static void UnmarkAncestors(DependencyObject visual)
        {
            if (visual is Visual || visual is Visual3D)
            {
                visual.ClearValue(Const.NameProperty);
                UnmarkAncestors(VisualTreeHelper.GetParent(visual));
            }
        }

        private static DependencyObject FindFirstMarkedVisual(DependencyObject visual)
        {
            if (visual is Visual || visual is Visual3D)
            {
                if (ObjectUtils.GetName(visual) == marker)
                {
                    return visual;
                }
                return FindFirstMarkedVisual(VisualTreeHelper.GetParent(visual));
            }
            return null;
        }

        /// <summary/>
        public static void ThawVisuals(IEnumerable<Visual3D> visuals)
        {
            foreach (Visual3D visual in visuals)
            {
                if (visual is ModelVisual3D)
                {
                    ModelVisual3D v = (ModelVisual3D)visual;
                    if (v.Content != null)
                    {
                        v.Content = v.Content.Clone();
                    }
                    if (v.Transform != null)
                    {
                        v.Transform = v.Transform.Clone();
                    }
                    ThawVisuals(v.Children);
                }
            }
        }

        /// <summary/>
        public static ControlTemplate ToTemplate(Viewport3D viewport)
        {
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Viewport3D));
            factory.SetValue(Viewport3D.CameraProperty, viewport.Camera);
            factory.SetValue(Viewport3D.WidthProperty, MathEx.FallbackIfNaN(viewport.Width, viewport.ActualWidth));
            factory.SetValue(Viewport3D.HeightProperty, MathEx.FallbackIfNaN(viewport.Height, viewport.ActualHeight));
            AddChildren(factory, viewport.Children);

            ControlTemplate template = new ControlTemplate();
            template.VisualTree = factory;
            return template;
        }

        private static void AddChildren(FrameworkElementFactory factory, Visual3DCollection visuals)
        {
            foreach (Visual3D visual in visuals)
            {
                ModelVisual3D model = visual as ModelVisual3D;
                if (model == null)
                {
                    continue;
                }
                FrameworkElementFactory f = new FrameworkElementFactory(typeof(ModelVisual3D));
                f.SetValue(ModelVisual3D.ContentProperty, model.Content);
                f.SetValue(ModelVisual3D.TransformProperty, model.Transform);
                AddChildren(f, model.Children);

                factory.AppendChild(f);
            }
        }

        /// <summary/>
        public static string DebugVisualTree(IEnumerable<Visual3D> visuals)
        {
            return Visual3DCollectionToString(visuals, string.Empty);
        }

        private static string Visual3DCollectionToString(IEnumerable<Visual3D> visuals, string indent)
        {
            string result = string.Empty;
            foreach (Visual3D visual in visuals)
            {
                result += Visual3DToString((ModelVisual3D)visual, indent) + "\r\n";
            }
            return result;
        }

        private static string Visual3DToString(ModelVisual3D visual, string indent)
        {
            return string.Format("{0}{1}\r\n" +
                                    "{0}{1}.Transform: {2}\r\n" +
                                    "{0}{1}.Content:\r\n" +
                                    "{3}\r\n" +
                                    "{0}{1}.Children:\r\n" +
                                    "{4}",
                                    indent,
                                    ObjectUtils.GetName(visual),
                                    Transform3DToString(visual.Transform),
                                    Model3DToString(visual.Content, indent + singleTab),
                                    Visual3DCollectionToString(visual.Children, indent + singleTab));
        }

        private static string Model3DToString(Model3D model, string indent)
        {
            if (model == null)
            {
                return indent + "(null)";
            }
            else if (model is Model3DGroup)
            {
                return string.Format("{0}{1} : Model3DGroup\r\n" +
                                        "{0}{1}.Transform: {2}\r\n" +
                                        "{0}{1}.Children:\r\n" +
                                        "{3}",
                                        indent,
                                        ObjectUtils.GetName(model),
                                        Transform3DToString(model.Transform),
                                        Model3DCollectionToString(((Model3DGroup)model).Children, indent + singleTab));
            }
            else
            {
                return string.Format("{0}{1} : {3}\r\n" +
                                        "{0}{1}.Transform: {2}",
                                        indent,
                                        ObjectUtils.GetName(model),
                                        Transform3DToString(model.Transform),
                                        model.GetType().Name);
            }
        }

        private static string Model3DCollectionToString(Model3DCollection models, string indent)
        {
            string result = string.Empty;
            foreach (Model3D model in models)
            {
                result += Model3DToString(model, indent) + "\r\n";
            }
            return result;
        }

        private static string Transform3DToString(Transform3D transform)
        {
            if (transform == null)
            {
                return "(null)";
            }
            else if (transform is TranslateTransform3D)
            {
                return "Translate: " +
                        ((TranslateTransform3D)transform).OffsetX + "," +
                        ((TranslateTransform3D)transform).OffsetY + "," +
                        ((TranslateTransform3D)transform).OffsetZ;
            }
            else if (transform is RotateTransform3D)
            {
                // TODO: add centerx/y/z to string
                return "Rotate: " + ((RotateTransform3D)transform).Rotation;
            }
            else if (transform is ScaleTransform3D)
            {
                // TODO: add centerx/y/z to string
                return "Scale: " +
                    ((ScaleTransform3D)transform).ScaleX + "," +
                    ((ScaleTransform3D)transform).ScaleY + "," +
                    ((ScaleTransform3D)transform).ScaleZ;
            }
            else if (transform is MatrixTransform3D)
            {
                return "Matrix: " + ((MatrixTransform3D)transform).Matrix;
            }
            else if (transform is Transform3DGroup)
            {
                return "TransformGroup: " + transform.Value;
            }
            return string.Empty;
        }

        private const string singleTab = "    ";
        private const string marker = "VisualMarker";
    }
}
