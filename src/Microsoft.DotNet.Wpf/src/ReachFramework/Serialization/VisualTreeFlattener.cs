// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Security;

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Microsoft.Internal.AlphaFlattener;
using System.Windows.Xps.Serialization;
using System.Windows.Xps.Packaging;

namespace MS.Internal.ReachFramework
{
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Property |
    AttributeTargets.Method |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate |
    AttributeTargets.Constructor,
    AllowMultiple = false,
    Inherited = true)
    ]
    internal sealed class FriendAccessAllowedAttribute : Attribute
    {
    }

    internal class MyColorTypeConverter : ColorTypeConverter
    {
        public
        override
        object
        ConvertTo(
            ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture,
            object value,
            Type destinationType
            )
        {
            Color color = (Color)value;

            return color.ToString(culture);
        }
    }

    internal class LooseImageSourceTypeConverter : ImageSourceTypeConverter
    {
        int m_bitmapId;
        String m_mainFile;

        public LooseImageSourceTypeConverter(String mainFile)
        {
            m_mainFile = mainFile;
        }

        public
        override
        object
        ConvertTo(
            ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture,
            object value,
            Type destinationType
            )
        {

            string bitmapName = "bitmap" + m_bitmapId;

            BitmapEncoder encoder = null;

            m_bitmapId++;

            BitmapFrame bmpd = value as BitmapFrame;

            if (bmpd != null)
            {
                BitmapCodecInfo codec = null;

                if (bmpd.Decoder != null)
                {
                    codec = bmpd.Decoder.CodecInfo;
                }

                if (codec != null)
                {
                    encoder = BitmapEncoder.Create(codec.ContainerFormat);
                    string extension = "";

                    if (encoder is BmpBitmapEncoder)
                    {
                        extension = "bmp";
                    }
                    else if (encoder is JpegBitmapEncoder)
                    {
                        extension = "jpg";
                    }
                    else if (encoder is PngBitmapEncoder)
                    {
                        extension = "png";
                    }
                    else if (encoder is TiffBitmapEncoder)
                    {
                        extension = "tif";
                    }
                    else
                    {
                        encoder = null;
                    }

                    if (encoder != null)
                    {
                        bitmapName = bitmapName + '.' + extension;
                    }
                }
            }

            if (encoder == null)
            {
                if (Microsoft.Internal.AlphaFlattener.Utility.NeedPremultiplyAlpha(value as BitmapSource))
                {
                    encoder = new WmpBitmapEncoder();

                    bitmapName = bitmapName + ".wmp";
                }
                else
                {
                    encoder = new PngBitmapEncoder();

                    bitmapName = bitmapName + ".png";
                }
            }

            if (value is BitmapFrame)
            {
                encoder.Frames.Add((BitmapFrame)value);
            }
            else
            {
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)value));
            }

            int index = m_mainFile.LastIndexOf('.');

            if (index < 0)
            {
                index = m_mainFile.Length;
            }

            string uri = m_mainFile.Substring(0, index) + "_" + bitmapName;

            Stream bitmapStreamDest = new System.IO.FileStream(uri, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

#if DEBUG
            Console.WriteLine("Saving " + uri);
#endif

            encoder.Save(bitmapStreamDest);

            bitmapStreamDest.Close();

            return new Uri(BaseUriHelper.SiteOfOriginBaseUri, uri);
        }
    }

    internal class LooseFileSerializationManager : PackageSerializationManager
    {
        String m_mainFile;
        LooseImageSourceTypeConverter m_imageConverter;

        public LooseFileSerializationManager(String mainFile)
        {
            m_mainFile = mainFile;
        }

        public override void SaveAsXaml(Object serializedObject)
        {
        }

        internal override String GetXmlNSForType(Type objectType)
        {
            return null;
        }

        internal override XmlWriter AcquireXmlWriter(Type writerType)
        {
            return null;
        }

        internal override void ReleaseXmlWriter(Type writerType)
        {
            return;
        }

        internal override XpsResourceStream AcquireResourceStream(Type resourceType)
        {
            return null;
        }

        internal override XpsResourceStream AcquireResourceStream(Type resourceType, String resourceID)
        {
            return null;
        }

        internal override void ReleaseResourceStream(Type resourceType)
        {
            return;
        }

        internal override void ReleaseResourceStream(Type resourceType, String resourceID)
        {
            return;
        }

        internal override void AddRelationshipToCurrentPage(Uri targetUri, string relationshipName)
        {
            return;
        }

        internal override BasePackagingPolicy PackagingPolicy
        {
            get
            {
                return null;
            }
        }

        internal
        override
        XpsResourcePolicy
        ResourcePolicy
        {
            get
            {
                return null;
            }
        }

        internal override TypeConverter GetTypeConverter(Object serializedObject)
        {
            return null;
        }

        internal override TypeConverter GetTypeConverter(Type objType)
        {
            if (typeof(Color).IsAssignableFrom(objType))
            {
                return new MyColorTypeConverter();
            }
            else if (typeof(BitmapSource).IsAssignableFrom(objType))
            {
                if (m_imageConverter == null)
                {
                    m_imageConverter = new LooseImageSourceTypeConverter(m_mainFile);
                }
                
                return m_imageConverter;
            }

            return null;
        }
    } 
}

namespace System.Windows.Xps.Serialization
{
#if SAMPLE
    // Sample code for serializing a Visual tree one node at a time
    public void SaveAsXml(Visual visual, System.Xml.XmlWriter resWriter, System.Xml.XmlWriter bodyWriter, string fileName)
    {
        resWriter.WriteStartElement("Canvas.Resources");

        VisualTreeFlattener flattener = new VisualTreeFlattener(resWriter, bodyWriter, fileName);

        Walk(flattener, visual);

        resWriter.WriteEndElement();
    }

    internal void Walk(VisualTreeFlattener flattener, Visual visual)
    {
        if (flattener.StartVisual(visual))
        {
            int count = VisualTreeHelper.GetChildrenCount(visual);

            for(int i = 0; i < count; i++)
            {
                Walk(flattener, VisualTreeHelper.GetChild(visual,i));
            }


            flattener.EndVisual();
        }
    }

#endif

    /// <summary>
    /// Main class for converting visual tree to fixed DrawingContext primitives
    /// </summary>
    #region public class VisualTreeFlattener
    [MS.Internal.ReachFramework.FriendAccessAllowed]
    internal class VisualTreeFlattener
    {
        #region Private Fields

        DrawingContextFlattener _dcf;
        Dictionary<String, int>      _nameList;

        //
        // Fix bug 1514270: Any VisualBrush.Visual rasterization occurs in brush-space, which
        // leads to fidelity issues if the brush is transformed to cover a large region.
        // This stores transformation above the brush to serve as hint for final size of
        // VisualBrush's content.
        //
        private Matrix _inheritedTransformHint = Matrix.Identity;

        // Depth of StartVisual call. Zero corresponds to highest level.
        private int _visualDepth = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// Transformation above this VisualTreeFlattener instance; currently only used
        /// when reducing VisualBrush to DrawingBrush.
        /// </summary>
        public Matrix InheritedTransformHint
        {
            get
            {
                return _inheritedTransformHint;
            }
            set
            {
                _inheritedTransformHint = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="pageSize">Raw size of parent fixed page in pixels.  Does not account for margins</param>
        /// <param name="treeWalkProgress">Used to detect visual tree cycles caused by VisualBrush</param>
        internal VisualTreeFlattener(IMetroDrawingContext dc, Size pageSize, TreeWalkProgress treeWalkProgress)
        {
            Debug.Assert(treeWalkProgress != null);
            
            // DrawingContextFlattener flattens calls to DrawingContext
            _dcf = new DrawingContextFlattener(dc, pageSize, treeWalkProgress);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resWriter"></param>
        /// <param name="bodyWriter"></param>
        /// <param name="manager"></param>
        /// <param name="pageSize">Raw size of parent fixed page in pixels.  Does not account for margins</param>
        internal VisualTreeFlattener(System.Xml.XmlWriter resWriter, System.Xml.XmlWriter bodyWriter, PackageSerializationManager manager, Size pageSize, TreeWalkProgress treeWalkProgress)
        {
            Debug.Assert(treeWalkProgress != null);
                        
            VisualSerializer dc = new VisualSerializer(resWriter, bodyWriter, manager);

            _dcf = new DrawingContextFlattener(dc, pageSize, treeWalkProgress);
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Evalulate complexity of a Drawing
        /// 0: empty
        /// 1: single primitive
        /// 2: complex
        /// </summary>
        static int Complexity(System.Windows.Media.Drawing drawing)
        {
            if (drawing == null)
            {
                return 0;
            }

            DrawingGroup dg = drawing as DrawingGroup;
            
            if (dg != null)
            {
                if (Utility.IsTransparent(dg.Opacity))
                {
                    return 0;
                }

                if ((dg.Transform != null) || (dg.ClipGeometry != null) || (!Utility.IsOne(dg.Opacity)))
                {
                    return 2;
                }

                DrawingCollection children = dg.Children;

                int complexity = 0;

                foreach (System.Windows.Media.Drawing d in children)
                {
                    complexity += Complexity(d);

                    if (complexity >= 2)
                    {
                        break;
                    }
                }

                return complexity;
            }

            return 1;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// S0 header/body handling for a visual
        /// </summary>
        /// <param name="visual">Visual input</param>
        /// <returns>returns true if Visual's children should be walked</returns>
        internal bool StartVisual(Visual visual)
        {
#if DEBUG
            _dcf.Comment(visual.GetType().ToString());
#endif

            System.Windows.Media.Drawing content = VisualTreeHelper.GetDrawing(visual);

            bool single = false;

            if ((VisualTreeHelper.GetChildrenCount(visual) == 0) && (Complexity(content) <= 1))
            {
                single = true;
            }

            // Get Visual properties in the order they're applied to the content.
            // Note that Effects may replace the content.
            Brush mask = VisualTreeHelper.GetOpacityMask(visual);
            double opacity = VisualTreeHelper.GetOpacity(visual);
            Effect effect = VisualTreeHelper.GetEffect(visual);
            Geometry clip = VisualTreeHelper.GetClip(visual);
            Transform trans = Utility.GetVisualTransform(visual);

            Rect bounds = VisualTreeHelper.GetDescendantBounds(visual);

            // Check for invisible Visual subtree.
            {
                // Skip invalid transformation
                if (trans != null && !Utility.IsValid(trans.Value))
                {
                    return false;
                }

                // Skip empty subtree
                if (!Utility.IsRenderVisible(bounds))
                {
                    return false;
                }

                // Skip complete clipped subtree
                if ((clip != null) && !Utility.IsRenderVisible(clip.Bounds))
                {
                    return false;
                }

                // Skip total transparent subtree
                // An Effect can overwrite content, including opacity, so
                // do not skip transparent opacity if effect applied.
                if (effect == null && (Utility.IsTransparent(opacity) || BrushProxy.IsEmpty(mask)))
                {
                    return false;
                }
            }

            // Ignore opaque opacity mask
            if ((mask != null) && Utility.IsBrushOpaque(mask))
            {
                mask = null;
            }

            FrameworkElement fe = visual as FrameworkElement;
            String nameAttr = null;
            // we will presever the name for this element if 
            // 1. It is FrameworkElement and Name is non empty.
            // 2. It is not FixedPage(because NameProperty of FixedPage is already reserved)
            // 3. It is not from template. TemplatedParent is null.
            if (fe != null && !String.IsNullOrEmpty(fe.Name) &&
                !(visual is System.Windows.Documents.FixedPage) &&
                 fe.TemplatedParent == null)
            {
                if (_nameList == null)
                {
                    _nameList = new Dictionary<String, int>();
                }
                int dummy;
                // Some classes like DocumentViewer, in its visual tree, will implicitly generate
                // some named elements. We will avoid create the duplicate names. 
                if (_nameList.TryGetValue(fe.Name, out dummy) == false)
                {
                    nameAttr = fe.Name;
                    _nameList.Add(fe.Name, 0);
                }
            }

            // Preserve FixedPage.NavigateUri.
            UIElement uiElement = visual as UIElement;
            Uri navigateUri = null;

            if (uiElement != null)
            {
                navigateUri = FixedPage.GetNavigateUri(uiElement);
            }

            // Preserve RenderOptions.EdgeMode.
            EdgeMode edgeMode = RenderOptions.GetEdgeMode(visual);

            // Rasterize all Viewport3DVisuals, and Visuals with Effects applied
            bool walkChildren;

            if (effect != null || visual is Viewport3DVisual)
            {
                // rasterization also handles opacity and children
                Matrix worldTransform = _dcf.Transform;
                if (trans != null)
                {
                    worldTransform.Prepend(trans.Value);
                }

                // get inherited clipping
                bool empty;
                Geometry inheritedClipping = _dcf.Clip;

                if (inheritedClipping == null || !inheritedClipping.IsEmpty())
                {
                    // transform current clip to world space
                    if (clip != null)
                    {
                        Matrix clipToWorldSpace = _dcf.Transform;

                        if (trans != null)
                        {
                            clipToWorldSpace.Prepend(trans.Value);
                        }

                        clip = Utility.TransformGeometry(clip, clipToWorldSpace);
                    }

                    // intersect with inherited clipping
                    clip = Utility.Intersect(inheritedClipping, clip, Matrix.Identity, out empty);

                    if (!empty)
                    {
                        // clipping is in world space
                        _dcf.DrawRasterizedVisual(
                            visual,
                            nameAttr,
                            navigateUri,
                            edgeMode,
                            trans,
                            worldTransform,
                            _inheritedTransformHint,
                            clip,
                            effect
                            );
                    }
                }

                walkChildren = false;
            }
            else
            {
                //
                // Fix bug 1576135: XPS serialization: round trip causes inflation by one Path element per page per trip
                //
                // Avoid emitting FixedPage white drawing content, since FixedPage with white background
                // is already emitted by the serializer prior to visual walk.
                //
                bool walkDrawing = true;

                if (_visualDepth == 0)
                {
                    FixedPage fixedPage = visual as FixedPage;

                    if (fixedPage != null)
                    {
                        SolidColorBrush colorBrush = fixedPage.Background as SolidColorBrush;

                        if (colorBrush != null)
                        {
                            Color color = colorBrush.Color;

                            if ((color.R == 255 && color.G == 255 && color.B == 255) ||
                                color.A == 0)
                            {
                                walkDrawing = false;
                            }
                        }
                    }
                }

                // <Canvas> could be generated here with related attributes
                _dcf.Push(trans, clip, opacity, mask, bounds, single, nameAttr, visual, navigateUri, edgeMode);

                if (walkDrawing)
                {
                    // Body content of a visual
                    DrawingWalk(content, _dcf.Transform);
                }

                walkChildren = true;
            }

            if (walkChildren)
            {
                _visualDepth++;
            }

            return walkChildren;
        }

        internal void EndVisual()
        {
            _visualDepth--;

            Debug.Assert(_visualDepth >= 0, "StartVisual/EndVisual mismatch");

            _dcf.Pop();
        }

        /// <summary>
        /// Walk of visual tree
        /// </summary>
        /// <param name="visual"></param>
        internal void VisualWalk(Visual visual)
        {
            if (StartVisual(visual))
            {
                int count = VisualTreeHelper.GetChildrenCount(visual);

                for(int i = 0; i < count; i++)
                {
                    // StartVisual() special cases Viewport3DVisual to avoid walking children.
                    VisualWalk((Visual) VisualTreeHelper.GetChild(visual,i));
                }


                EndVisual();
            }
        }

        /// <summary>
        /// Recursive walk of Drawing
        /// </summary>
        /// <param name="d"></param>
        /// <param name="drawingToWorldTransform"></param>
        internal void DrawingWalk(System.Windows.Media.Drawing d, Matrix drawingToWorldTransform)
        {
            if (d == null || !Utility.IsRenderVisible(d.Bounds))
            {
                return;
            }
            
            {
                GeometryDrawing gd = d as GeometryDrawing;

                if (gd != null)
                {
                    _dcf.DrawGeometry(gd.Brush, gd.Pen, gd.Geometry);

                    return;
                }
            }
            
            {
                GlyphRunDrawing gd = d as GlyphRunDrawing;

                if (gd != null)
                {
                    _dcf.DrawGlyphRun(gd.ForegroundBrush, gd.GlyphRun);

                    return;
                }
            }
            
            {
                ImageDrawing id = d as ImageDrawing;

                if (id != null)
                {
                    _dcf.DrawImage(id.ImageSource, id.Rect);

                    return;
                }
            }
            
            DrawingGroup dg = d as DrawingGroup;

            if (dg != null)
            {
                if (Utility.IsRenderVisible(dg))
                {
                    DrawingCollection children = dg.Children;

                    bool onePrimitive = children.Count == 1;

                    // Nesting of onePrimitive is not handled yet in VisualSerializer.Push
                    if (onePrimitive)
                    {
                        onePrimitive = !(children[0] is DrawingGroup);
                    }

                    _dcf.Push(
                        dg.Transform,
                        dg.ClipGeometry,
                        Utility.NormalizeOpacity(dg.Opacity),
                        dg.OpacityMask,
                        Rect.Empty,
                        onePrimitive,
                        null,
                        null,
                        null,
                        EdgeMode.Unspecified
                        );

                    if (dg.Transform != null)
                    {
                        drawingToWorldTransform.Prepend(dg.Transform.Value);
                    }

                    for (int i = 0; i < children.Count; i++)
                    {
                        DrawingWalk(children[i], drawingToWorldTransform);
                    }

                    _dcf.Pop();
                }
                return;
            }

#if DEBUG
            Console.WriteLine("Drawing of type '" + d.GetType() + "' not handled.");
#endif
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Serialize a visual to fixed XAML
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="resWriter"></param>
        /// <param name="bodyWriter"></param>
        /// <param name="fileName"></param>
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        static internal void SaveAsXml(Visual visual, System.Xml.XmlWriter resWriter, System.Xml.XmlWriter bodyWriter, String fileName)
        {
            // Check for testing hooks
            FrameworkElement el = visual as FrameworkElement;

            if (el != null)
            {
                bool testhook = false;

                ResourceDictionary res = el.Resources;

                foreach (DictionaryEntry e in res)
                {
                    string key = e.Key as string;

                    if (key != null)
                    {
                        if (key == "Destination")
                        {
                            MetroToGdiConverter.TestingHook(e.Value);
                            testhook = true;
                        }
                        else
                        {
                            testhook |= Microsoft.Internal.AlphaFlattener.Configuration.SetValue(key, e.Value);
                        }
                    }
                }

                if (testhook)
                {
                    return;
                }
            }

            PackageSerializationManager manager = null;

            if (fileName != null)
            {
                manager = new MS.Internal.ReachFramework.LooseFileSerializationManager(fileName);
            }

            resWriter.WriteStartElement("Canvas.Resources");
            resWriter.WriteStartElement("ResourceDictionary");
            resWriter.WriteWhitespace("\r\n");
            
            VisualTreeFlattener flattener = new VisualTreeFlattener(resWriter, bodyWriter, manager, new Size(8.5 * 96, 11 * 96), new TreeWalkProgress());

            flattener.VisualWalk(visual);
            bodyWriter.WriteWhitespace("\r\n\r\n");

            resWriter.WriteWhitespace("\r\n");
            resWriter.WriteEndElement();
            resWriter.WriteEndElement();
            resWriter.WriteWhitespace("\r\n\r\n            ");
        }

        /// <summary>
        /// Walk a visual tree and flatten it to IMetroDrawingContext
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="dc"></param>
        /// <param name="pageSize">Raw size of parent fixed page in pixels.  Does not account for margins</param>
        /// <param name="treeWalkProgress">Used to detect visual tree cycles caused by VisualBrush</param>
        static internal void Walk(Visual visual, IMetroDrawingContext dc, Size pageSize, TreeWalkProgress treeWalkProgress)
        {
            VisualTreeFlattener flattener = new VisualTreeFlattener(dc, pageSize, treeWalkProgress);

            flattener.VisualWalk(visual);
        }

        /// <summary>
        /// Write a Geometry into XmlWriter
        /// </summary>
        /// <param name="bodyWriter">Destination stream</param>
        /// <param name="geometry">Geometry to write</param>
        /// <param name="pageSize">Raw size of parent fixed page in pixels.  Does not account for margins</param>
        /// <returns>True if written as element, False if written as attribute</returns>
        static internal bool WritePath(System.Xml.XmlWriter bodyWriter, Geometry geometry, Size pageSize)
        {
            VisualSerializer vs = new VisualSerializer(null, bodyWriter, null);

            // Both fill/stroke
            return vs.WriteGeometry("Path", "Data", geometry, Matrix.Identity, false, true, true);
        }

        #endregion
    }
    #endregion
}

