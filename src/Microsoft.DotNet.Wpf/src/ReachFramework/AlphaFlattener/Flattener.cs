// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Collections;              // for ArrayList
using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Globalization;

using System.Windows.Xps.Serialization;
using System.Printing;

using System.Security;
using MS.Utility;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    /// Tree flattener and Alpha flattener.
    /// </summary>
    internal class Flattener
    {
        #region Constructors

        public Flattener(bool disJoint, double width, double height)
        {
            _dl = new DisplayList(disJoint, width, height);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// General routine for adding a Primitive to DisplayList
        /// 1) Apply transformation to Geometry, Brush, Pen
        /// 2) Optimize Primitive
        /// 3) Add to DisplayList
        /// </summary>
        /// <param name="p"></param>
        public void AddPrimitive(Primitive p)
        {
            if (p.IsTransparent)
            {
                return;
            }

            p.ApplyTransform();

            if (!p.Optimize())
            {
                return;
            }

            GeometryPrimitive gp = p as GeometryPrimitive;

            if (gp != null)
            {
                if ((gp.Pen != null) && (gp.Pen.StrokeBrush.Brush is DrawingBrush))
                {
                    gp.Widen();
                }
            }

            _dl.RecordPrimitive(p);
        }

        /// <summary>
        /// Flatten the structure of a primitive tree by push clip/transform/opacity onto each leaf node.
        /// Build an index in a DisplayList.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="clip"></param>
        /// <param name="transform"></param>
        /// <param name="opacity"></param>
        /// <param name="opacityMask"></param>
        public void TreeFlatten(Primitive tree, Geometry clip, Matrix transform, double opacity, BrushProxy opacityMask)
        {
        More:
            if (tree == null)
            {
                return;
            }

            Debug.Assert(Utility.IsValid(opacity) && Utility.IsValid(tree.Opacity), "Invalid opacity encountered, should've been normalized in conversion to Primitive");

            // Non-invertible transforms may arise from brush unfolding, where brush content is huge but
            // we need to scale down significantly to fill target region. Allow such transforms.

            CanvasPrimitive canvas = tree as CanvasPrimitive;

            if (canvas != null)
            {
                ArrayList children = canvas.Children;

                // No children, nothing to do
                if ((children == null) || (children.Count == 0))
                {
                    return;
                }

                opacity *= tree.Opacity;

                // transform opacity mask into current primitive space
                if (opacityMask != null)
                {
                    Matrix worldToPrimitiveTransform = tree.Transform;
                    worldToPrimitiveTransform.Invert();

                    opacityMask.ApplyTransform(worldToPrimitiveTransform);
                }

                opacityMask = BrushProxy.BlendBrush(opacityMask, tree.OpacityMask);

                // Skip the subtree if it's transparent enough
                if (Utility.IsTransparent(opacity))
                {
                    return;
                }

                transform = tree.Transform * transform;

                Geometry transclip = Utility.TransformGeometry(tree.Clip, transform);

                bool empty;

                clip = Utility.Intersect(clip, transclip, Matrix.Identity, out empty);

                if (empty)
                {
                    return;
                }

                // For single child, just push transform/clip/opacity onto it.
                if (children.Count == 1)
                {
                    tree = children[0] as Primitive;

                    // Save a recursive call
                    goto More;
                }

                if (Configuration.BlendAlphaWithWhite || Configuration.ForceAlphaOpaque ||
                    (Utility.IsOpaque(opacity) && (opacityMask == null))) // For opaque subtree, just push trasform/clip into it.
                {
                    foreach (Primitive p in children)
                    {
                        TreeFlatten(p, clip, transform, opacity, opacityMask);
                    }
                }
                else
                {
                    // A semi-transparent sub-tree with more than one child
                    Flattener fl = new Flattener(true, _dl.m_width, _dl.m_height);

                    Primitive ntree   = tree;
                    ntree.Clip        = null;
                    ntree.Transform   = Matrix.Identity;
                    ntree.Opacity     = 1.0;
                    ntree.OpacityMask = null;

#if DEBUG
                    if (Configuration.Verbose >= 2)
                    {
                        Console.WriteLine("TreeFlatten for subtree");
                    }
#endif

                    if (opacityMask != null)
                    {
                        opacityMask.ApplyTransform(transform);
                    }

                    // Flatten sub-tree structure into a new DisplayList
                    fl.TreeFlatten(ntree, clip, transform, 1.0, null);

                    // Remove alpha in the sub-tree and add to the current display list

#if DEBUG
                    if (Configuration.Verbose >= 2)
                    {
                        Console.WriteLine("end TreeFlatten for subtree");
                        Console.WriteLine("AlphaFlatten for subtree");
                    }
#endif
                    fl.AlphaFlatten(new DisplayListDrawingContext(this, opacity, opacityMask, Matrix.Identity, null), true);

#if DEBUG
                    if (Configuration.Verbose >= 2)
                    {
                        Console.WriteLine("end AlphaFlatten for subtree");
                    }
#endif
                }
            }
            else
            {
                GeometryPrimitive gp = tree as GeometryPrimitive;

                if (gp != null && gp.Brush != null && gp.Pen != null &&
                    (!gp.IsOpaque || !Utility.IsOpaque(opacity)))
                {
                    //
                    // As an optimization we split fill from stroke, however doing so requires
                    // an intermediate canvas to handle translucent fill/stroke, otherwise
                    // the translucent stroke and fill will overlap.
                    //
                    CanvasPrimitive splitCanvas = new CanvasPrimitive();

                    GeometryPrimitive fill = (GeometryPrimitive)gp;
                    GeometryPrimitive stroke = (GeometryPrimitive)gp.Clone();

                    fill.Pen = null;
                    stroke.Brush = null;

                    splitCanvas.Children.Add(fill);
                    splitCanvas.Children.Add(stroke);

                    tree = splitCanvas;
                    goto More;
                }

                // Push transform/clip/opacity to leaf node
                tree.Transform = tree.Transform * transform;

                if (tree.Clip == null)
                {
                    tree.Clip = clip;
                }
                else
                {
                    Geometry transclip = Utility.TransformGeometry(tree.Clip, transform);

                    bool empty;

                    tree.Clip = Utility.Intersect(transclip, clip, Matrix.Identity, out empty);

                    if (!empty)
                    {
                        empty = Utility.IsEmpty(tree.Clip, Matrix.Identity);
                    }

                    if (empty)
                    {
                        return;
                    }
                }

                tree.PushOpacity(opacity, opacityMask);

                if (gp != null)
                {
                    // Split fill and stroke into separate primitives if no opacity involved.
                    // Intermediate Canvas not needed due to opaqueness.
                    if ((gp.Brush != null) && (gp.Pen != null))
                    {
                        GeometryPrimitive fill = gp.Clone() as GeometryPrimitive;

                        fill.Pen = null;
                        AddPrimitive(fill);     // Fill only first

                        // Stroke is flattend to fill only when needed
                        gp.Brush = null;
                        AddPrimitive(gp); // Followed by stroke only
                    }
                    else if ((gp.Pen != null) || (gp.Brush != null))
                    {
                        AddPrimitive(gp);
                    }
                }
                else
                {
                    // Record it
                    AddPrimitive(tree);
                }
            }
        }

        /// <summary>
        /// Resolve object overlapping in a primitive tree.
        /// Send broken down drawing primitives to _dc.
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="disjoint">True if all output primitives need to be disjoint</param>
        public void AlphaFlatten(IProxyDrawingContext dc, bool disjoint)
        {
            List<PrimitiveInfo> commands = _dl.Commands;

            if (commands == null)
            {
                return;
            }

            int count = commands.Count;

            _dc = dc;

            bool needFlattening = true;

            if (Configuration.BlendAlphaWithWhite || Configuration.ForceAlphaOpaque)
            {
                needFlattening = false;
            }
            else if (!disjoint)
            {
                needFlattening = false;

                for (int i = 0; i < count; i++)
                {
                    PrimitiveInfo info = commands[i];

                    if (!info.primitive.IsOpaque)
                    {
                        needFlattening = true;
                        break;
                    }
                }
            }

            if (needFlattening)
            {
#if DEBUG
                Console.WriteLine();
                Console.WriteLine("Stage 2: Calculating intersections using bounding boxes");
                Console.WriteLine();

#endif
                // Still need all the primitive, for removal by opaque covering and white primitive removal
                _dl.CalculateIntersections(count);
            }

#if DEBUG
            if (Configuration.Verbose >= 2)
            {
                Console.WriteLine();
                Console.WriteLine("Original display list");
                Console.WriteLine();

                DisplayList.PrintPrimitive(null, -1, true);

                for (int i = 0; i < count; i ++)
                {
                    DisplayList.PrintPrimitive(commands[i], i, true);
                }

                Console.WriteLine();

                Console.WriteLine("Primitives in display list: {0}", count);

                Console.WriteLine();
            }
#endif

            if (needFlattening)
            {
                DisplayListOptimization(commands, count, disjoint);
            }

#if DEBUG
            for (int i = 0; i < count; i ++)
            {
                if (commands[i] != null)
                {
                    commands[i].SetID(i);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Stage 4: Alpha flattening");
            Console.WriteLine();

#endif
            for (int i = 0; i < count; i++)
            {
                PrimitiveInfo info = commands[i];

                if (info == null)
                {
                    continue;
                }

                String desp = null;
#if DEBUG
                if (Configuration.Verbose >= 2)
                {
                    Console.Write(i);
                    Console.Write(": ");
                }

                desp = info.id;
#endif

                if (info.m_cluster != null)
                {
                    info.m_cluster.Render(commands, dc);
                }
                else
                {
                    AlphaRender(info.primitive, info.overlap, info.overlapHasTransparency, disjoint, desp);
                }

#if DEBUG
                if (Configuration.Verbose >= 2)
                {
                    Console.WriteLine("");
                }
#endif
            }

            _dc = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Remove a primitive from display list
        /// </summary>
        /// <param name="i"></param>
        private void DeleteCommand(int i)
        {
#if DEBUG
            Console.WriteLine("Delete command {0}", i);
#endif

            PrimitiveInfo pi = _dl.Commands[i];

            if (pi.overlap != null)
            {
                foreach (int j in pi.overlap)
                {
                    _dl.Commands[j].underlay.Remove(i);
                }
            }

            if (pi.underlay != null)
            {
                bool trans = ! pi.primitive.IsOpaque;

                foreach (int j in pi.underlay)
                {
                    if (trans)
                    {
                        _dl.Commands[j].overlapHasTransparency--;
                    }

                    _dl.Commands[j].overlap.Remove(i);
                }
            }

            _dl.Commands[i] = null;
        }

#if DEBUG

        static int vipID; // = 0;

        static void SerializeVisual(Visual visual, double width, double height, String filename)
        {
            FileStream    stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
            XmlTextWriter writer = new System.Xml.XmlTextWriter(stream, System.Text.Encoding.UTF8);

            writer.Formatting  = System.Xml.Formatting.Indented;
            writer.Indentation = 4;
            writer.WriteStartElement("FixedDocument");
            writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            writer.WriteAttributeString("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");

            writer.WriteStartElement("PageContent");
            writer.WriteStartElement("FixedPage");
            writer.WriteAttributeString("Width",  width.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Height", height.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Background", "White");
            writer.WriteStartElement("Canvas");

            System.IO.StringWriter resString = new StringWriter(CultureInfo.InvariantCulture);

            System.Xml.XmlTextWriter resWriter = new System.Xml.XmlTextWriter(resString);
            resWriter.Formatting = System.Xml.Formatting.Indented;
            resWriter.Indentation = 4;

            System.IO.StringWriter bodyString = new StringWriter(CultureInfo.InvariantCulture);

            System.Xml.XmlTextWriter bodyWriter = new System.Xml.XmlTextWriter(bodyString);
            bodyWriter.Formatting = System.Xml.Formatting.Indented;
            bodyWriter.Indentation = 4;

            VisualTreeFlattener.SaveAsXml(visual, resWriter, bodyWriter, filename);

            resWriter.Close();
            bodyWriter.Close();

            writer.Flush();
            writer.WriteRaw(resString.ToString());
            writer.WriteRaw(bodyString.ToString());

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement(); // FixedDocument
            writer.Close();
            stream.Close();
        }
#endif

        private static bool BlendCommands(PrimitiveInfo pi, PrimitiveInfo pj)
        {
            GeometryPrimitive gi = pi.primitive as GeometryPrimitive;
            GeometryPrimitive gj = pj.primitive as GeometryPrimitive;

            if ((gi != null) && (gi.Brush != null) &&
                (gj != null) && (gj.Brush != null))
            {
                // get brushes in world space
                BrushProxy bi = gi.Brush.ApplyTransformCopy(gi.Transform);
                BrushProxy bj = gj.Brush.ApplyTransformCopy(gj.Transform);

                gi.Brush = bi.BlendBrush(bj);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Switch commands [i] [j], when i is covered by j
        /// </summary>
        /// <param name="commands">Display list</param>
        /// <param name="i">first command</param>
        /// <param name="pi">commands[i]</param>
        /// <param name="j">second command</param>
        /// <param name="pj">command[j]</param>
        /// <param name="disconnect">Disconnect (i,j) overlap/underlay relationship</param>
        static private void SwitchCommands(List<PrimitiveInfo> commands, int i, PrimitiveInfo pi, int j, PrimitiveInfo pj, bool disconnect)
        {
            if ((pi != null) && (pj != null) && disconnect)
            {
                pi.overlap.Remove(j);

                if (!pj.primitive.IsOpaque)
                {
                    pi.overlapHasTransparency--;
                }

                pj.underlay.Remove(i);
            }

            if (pi != null)
            {
                if (pi.overlap != null)
                {
                    foreach (int k in pi.overlap)
                    {
                        commands[k].underlay.Remove(i);
                        commands[k].underlay.Add(j);
                    }
                }

                if (pi.underlay != null)
                {
                    foreach (int k in pi.underlay)
                    {
                        commands[k].overlap.Remove(i);
                        commands[k].overlap.Add(j);
                    }
                }
            }

            if (pj != null)
            {
                if (pj.overlap != null)
                {
                    foreach (int k in pj.overlap)
                    {
                        commands[k].underlay.Remove(j);
                        commands[k].underlay.Add(i);
                    }
                }

                if (pj.underlay != null)
                {
                    foreach (int k in pj.underlay)
                    {
                        commands[k].overlap.Remove(j);
                        commands[k].overlap.Add(i);
                    }
                }
            }

            commands[i] = pj;
            commands[j] = pi;
        }

        /// <summary>
        /// Bug 1687865
        /// Special optimization for annotation type of visual: lots of glyph runs covered by a single transparency geometry
        ///    t1 ... tn g   => t1 ... tn-1 g tn'
        ///                  => g t1' ... tn'
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="j"></param>
        private static void PushTransparencyDown(List<PrimitiveInfo> commands, int j)
        {
            PrimitiveInfo pj = commands[j];

            GeometryPrimitive gj = pj.primitive as GeometryPrimitive;

            if ((gj == null) || (pj.underlay == null) || (pj.underlay.Count == 0))
            {
                return;
            }

            for (int n = pj.underlay.Count - 1; n >= 0; n --)
            {
                int i = pj.underlay[n];

                PrimitiveInfo pi = commands[i];

                if (pi == null)
                {
                    continue;
                }

                GeometryPrimitive gi = pi.primitive as GeometryPrimitive;

                if ((gi != null) && (gi.Pen == null) && (pi.overlap.Count == 1) && pj.FullyCovers(pi))
                {
                    // c[i] ... c[j] => ... c[j] c[i]'
                    if (BlendCommands(pi, pj)) // pi.Brush = Blend(pi.Brush, pj.Brush)
                    {
                        pj.underlay.Remove(i);

                        pi.overlap                = null;
                        pi.overlapHasTransparency = 0;

                        while (i < j)
                        {
                            SwitchCommands(commands, i, commands[i], i + 1, commands[i + 1], false);

                            i ++;
                        }

                        j --;
                    }
                }
            }
        }


        /// <summary>
        /// Optimization: If a transparent primitive is covered underneath immediately by an opaque primitive,
        /// or has nothing underneath, convert it to opaque primitive
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="i"></param>
        private static bool ConvertTransparentOnOpaque(List<PrimitiveInfo> commands, int i)
        {
            PrimitiveInfo pi = commands[i];

            GeometryPrimitive gp = pi.primitive as GeometryPrimitive;

            if (gp != null)
            {
                PrimitiveInfo qi = null;

                if ((pi.underlay != null) && (pi.underlay.Count != 0))
                {
                    qi = commands[pi.underlay[pi.underlay.Count - 1]];
                }

                if ((qi == null) || (qi.primitive.IsOpaque && qi.FullyCovers(pi)))
                {
                    BrushProxy under = BrushProxy.CreateColorBrush(Colors.White);

                    if (qi != null)
                    {
                        GeometryPrimitive qp = qi.primitive as GeometryPrimitive;

                        if (qp != null)
                        {
                            under = qp.Brush;
                        }
                    }

                    if (under != null)
                    {
                        // Blend it with brush underneath
                        BrushProxy blendedBrush = gp.Brush;
                        BrushProxy blendedPenBrush = gp.Pen == null ? null : gp.Pen.StrokeBrush;

                        if (blendedBrush != null)
                        {
                            blendedBrush = under.BlendBrush(blendedBrush);
                        }
                        else if (blendedPenBrush != null)
                        {
                            blendedPenBrush = under.BlendBrush(blendedPenBrush);
                        }

                        //
                        // Fix bug 1293500:
                        // Allow blending to proceed only if we did not generate pen stroke
                        // brush that is a brush list. Reason: Such a case would have to be
                        // handled during rendering by stroking the object with each brush
                        // in the list. But we're already rendering brushes of underlying
                        // objects, so the optimization is pointless.
                        //
                        bool proceedBlending = true;

                        if (blendedPenBrush != null && blendedPenBrush.BrushList != null)
                        {
                            proceedBlending = false;
                        }

                        if (proceedBlending)
                        {
                            gp.Brush = blendedBrush;
                            if (gp.Pen != null)
                            {
                                gp.Pen.StrokeBrush = blendedPenBrush;
                            }
                        }

                        if (proceedBlending && pi.primitive.IsOpaque)
                        {
#if DEBUG
                            Console.WriteLine("Make {0} opaque", i);

#endif

                            if (pi.underlay != null)
                            {
                                for (int k = 0; k < pi.underlay.Count; k++)
                                {
                                    commands[pi.underlay[k]].overlapHasTransparency--;
                                }
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Optimization: If a transparent primitive is covered underneath by an opaque primitive, cut its ties with all primitives before it
        /// </summary>
        /// <param name="pi"></param>
        /// <param name="commands"></param>
        /// <param name="i"></param>
        private static void ReduceTie(PrimitiveInfo pi, List<PrimitiveInfo> commands, int i)
        {
            if ((pi.underlay != null) && !pi.primitive.IsOpaque)
            {
                int len = pi.underlay.Count;

                for (int j = len - 1; j >= 0; j--)
                {
                    PrimitiveInfo qi = commands[pi.underlay[j]];

                    if (qi.primitive.IsOpaque && qi.FullyCovers(pi))
                    {
                        for (int k = j - 1; k >= 0; k--)
                        {
                            int under = pi.underlay[k];

                            commands[under].overlap.Remove(i);
                            commands[under].overlapHasTransparency--;

                            pi.underlay.Remove(under);
                        }

                        break;
                    }
                }
            }
        }

        private static List<int>[] CopyUnderlay(int count, List<PrimitiveInfo> commands)
        {
            List<int>[] oldUnderlay = new List<int>[count];

            for (int i = 0; i < count; i++)
            {
                List<int> l = commands[i].underlay;

                if (l != null)
                {
                    oldUnderlay[i] = new List<int>(l.Count);

                    for (int j = 0; j < l.Count; j++)
                    {
                        oldUnderlay[i].Add(l[j]);
                    }
                }
            }

            return oldUnderlay;
        }

        /// <summary>
        /// Optimization phase
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="count"></param>
        /// <param name="disjoint"></param>
        private void DisplayListOptimization(List<PrimitiveInfo> commands, int count, bool disjoint)
        {
#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Start 3: Display list optimization");
            Console.WriteLine();
#endif
            List<int> [] oldUnderlay = null;

            if (!disjoint) // If not in a subtree which needs full flattening
            {
                // The following optimization may change PrimitiveInfo.underlay, but this is needed
                // for cluster calcuation. So we make a copy of it for use within this routine only
                oldUnderlay = CopyUnderlay(count, commands);

                // These optimizations need to run in a seperate pass, because they may affect other primitives
                for (int i = 0; i < count; i++)
                {
                repeat:
                    PrimitiveInfo pi = commands[i];

                    if (pi == null)
                        continue;

                    // Optimization: If a primitive is covered by an opaque primtive, delete it
                    if (pi.overlap != null)
                    {
                        bool deleted = false;

                        for (int j = 0; j < pi.overlap.Count; j++)
                        {
                            PrimitiveInfo qi = commands[pi.overlap[j]];

                            if (qi.primitive.IsOpaque && qi.FullyCovers(pi))
                            {
                                DeleteCommand(i);
                                deleted = true;
                                break;
                            }
                        }

                        if (deleted)
                        {
                            continue;
                        }
                    }

                    // Optimization: If a primitive is covered by overlap[0], blend brush and switch order
                    // This results in smaller area being rendered as blending of two brushes.
                    if ((pi.overlap != null) && (pi.overlap.Count != 0))
                    {
                        int j = pi.overlap[0]; // first overlapping primitive

                        PrimitiveInfo pj = commands[j];

                        // Do not attempt to blend if both primitives cover exactly same area, since blending
                        // one into the other provides no benefits.
                        if ((pj.underlay[pj.underlay.Count - 1] == i) && pj.FullyCovers(pi) && !pi.FullyCovers(pj))
                        {
                            if (BlendCommands(pi, pj))
                            {
                                SwitchCommands(commands, i, pi, j, pj, true);
                                goto repeat; // pj at position i needs to be processed
                            }
                        }
                    }

                    // Optimization: Delete white primitives with nothing underneath
                    if ((pi.underlay == null) && DisplayList.IsWhitePrimitive(pi.primitive))
                    {
                        DeleteCommand(i);

                        continue;
                    }

                    // Optimization: If a transparent primitive is covered underneath by an opaque primitive, cut its ties with all primitives before it
                    ReduceTie(pi, commands, i);

                    // Transparent primitive
                    if (!pi.primitive.IsOpaque)
                    {
                        // Optimization: If a transparent primitive is covered underneath immediately by an opaque primitive,
                        // or has nothing underneath, convert it to opaque primitive
                        if (! ConvertTransparentOnOpaque(commands, i))
                        {
                            PushTransparencyDown(commands, i);
                        }
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    PrimitiveInfo pi = commands[i];

                    if (pi == null)
                    {
                        continue;
                    }

                    // Optimization: If a primitive is covered by all opaque primitives, cut its ties with primitive on top of it.

                    // This check is also implemented in PrimitiveRender.FindIntersection,
                    // in which it is on a remaing items in overlapping list.
                    // With overlapHasTransparency flag, it can be moved forward.

                    if ((pi.overlap != null) && (pi.overlapHasTransparency == 0))
                    {
                        foreach (int j in pi.overlap)
                        {
                            commands[j].underlay.Remove(i);
                        }

                        pi.overlap = null;
                    }

                    // Optimization: If an opaque primitive is covered by all opaque primitives, cut its ties with primitives under it.
                    if ((pi.underlay != null) && (pi.overlapHasTransparency == 0) && pi.primitive.IsOpaque)
                    {
                        foreach (int j in pi.underlay)
                        {
                            commands[j].overlap.Remove(i);
                        }

                        pi.underlay = null;
                    }
                }
            }

            List<Cluster> transparentCluster = Cluster.CalculateCluster(commands, count, disjoint, oldUnderlay);

            Cluster.CheckForRasterization(transparentCluster, commands);

#if DEBUG
            if (HasUnmanagedCodePermission())
            {
                LogInterestingPrimitives(commands, count, transparentCluster);
                SaveInterestingPrimitives(commands, count, transparentCluster);
            }
#endif
        }


#if DEBUG

        static bool HasUnmanagedCodePermission()
        {
            return true;
        }

        internal void LogInterestingPrimitives(List<PrimitiveInfo> commands, int count, List<Cluster> transparentCluster)
        {
            if (Configuration.Verbose >= 1)
            {
                // Display only interesting primitives

                DisplayList.PrintPrimitive(null, -1, false);

                Rect target = Rect.Empty;

                int vip = 0;

                for (int i = 0; i < count; i++)
                {
                    PrimitiveInfo pi = commands[i];

                    if ((pi != null) && ((pi.overlapHasTransparency != 0) || !pi.primitive.IsOpaque))
                    {
                        if (!pi.primitive.IsOpaque)
                        {
                            target.Union(pi.bounds);
                        }

                        DisplayList.PrintPrimitive(commands[i], i, false);
                        vip++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Interesting primitives: {0}", vip);
                Console.WriteLine("Area with transparency: {0}", DisplayList.LeftPad(target, 0));
                Console.WriteLine();

                for (int i = 0; i < transparentCluster.Count; i++)
                {
                    Console.WriteLine(
                        "Cluster {0}: {1} {2} {3}",
                        i + 1,
                        DisplayList.LeftPad(transparentCluster[i].DebugBounds, 0),
                        DisplayList.LeftPad(transparentCluster[i].DebugPrimitives, 0),
                        transparentCluster[i].DebugRasterize);
                }

                Console.WriteLine();
            }
        }

        internal void SaveInterestingPrimitives(List<PrimitiveInfo> commands, int count, List<Cluster> transparentCluster)
        {
            if (Configuration.SerializePrimitives)
            {
                // render primitives to DrawingVisual
                DrawingVisual dv = new DrawingVisual();

                using (DrawingContext ctx = dv.RenderOpen())
                {
                    Pen black = new Pen(Brushes.Black, 0.8);
                    black.DashStyle = DashStyles.Dash;

                    for (int i = 0; i < count; i++)
                    {
                        PrimitiveInfo pi = commands[i];

                        if ((pi != null) && ((pi.overlapHasTransparency != 0) || !pi.primitive.IsOpaque))
                        {
                            pi.primitive.OnRender(ctx);

                            if (!pi.primitive.IsOpaque)
                            {
                                ctx.DrawRectangle(null, black, pi.bounds);
                            }

                            //                          Rect bounds = pi.bounds;

                            //                          Console.WriteLine("<RectangleGeometry Canvas.Left=\"{0}\" Canvas.Top=\"{1}\" Width=\"{2}\" Height=\"{3}\" Fill=\"#FFFFFFFF\" />",
                            //                              bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                        }
                    }

                    Pen pen = new Pen(Brushes.Blue, 0.8);

                    pen.DashStyle = DashStyles.Dot;

                    for (int i = 0; i < transparentCluster.Count; i++)
                    {
                        ctx.DrawRectangle(null, pen, transparentCluster[i].DebugBounds);
                    }
                }

                // save visual to xaml
                string name = "vip.xaml";

                for (int i = 0; (name != null) && (i < 10); i++)
                {
                    try
                    {
                        if (vipID != 0)
                        {
                            name = "vip" + vipID + ".xaml";
                        }

                        SerializeVisual(dv, _dl.m_width, _dl.m_height, name);

                        Console.WriteLine("Serialized primitives to " + name);

                        name = null;
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.ToString());

                        name = "vip" + vipID + ".xaml";
                    }

                    vipID++;
                }
            }
        }
#endif

        /// <summary>
        /// Resolve overlapping for a single primitive
        /// </summary>
        /// <param name="primitive"></param>
        /// <param name="overlapping"></param>
        /// <param name="overlapHasTransparency"></param>
        /// <param name="disjoint"></param>
        /// <param name="desp"></param>
        private void AlphaRender(Primitive primitive, List<int> overlapping, int overlapHasTransparency, bool disjoint, string desp)
        {
            if (primitive == null)
            {
                return;
            }

            // Skip alpha flattening when there are too many layer of transparency
            if (overlapHasTransparency > Configuration.MaximumTransparencyLayer)
            {
                overlapping = null;
            }

            PrimitiveRenderer ri = new PrimitiveRenderer();

            ri.Clip        = primitive.Clip;
            ri.Brush       = null;
            ri.Pen         = null;
            ri.Overlapping = overlapping;
            ri.Commands    = _dl.Commands;
            ri.DC          = _dc;
            ri.Disjoint    = disjoint;

            GeometryPrimitive p = primitive as GeometryPrimitive;

            if (p != null)
            {
                ri.Brush = p.Brush;
                ri.Pen   = p.Pen;

                bool done = false;

                Geometry g = p.Geometry;

                GlyphPrimitive gp = p as GlyphPrimitive;

                if (gp != null)
                {
                    done = ri.DrawGlyphs(gp.GlyphRun, gp.GetRectBounds(true), p.Transform, desp);

                    if (!done)
                    {
                        g = p.WidenGeometry;
                    }
                }

                if (!done)
                {
                    if (!p.Transform.IsIdentity)
                    {
                        // Should not occur; GeometryPrimitive.ApplyTransform will push transform
                        // to geometry and brush.
                        g = Utility.TransformGeometry(g, p.Transform);

                        if (ri.Brush != null)
                        {
                            ri.Brush = ri.Brush.ApplyTransformCopy(p.Transform);
                        }

                        if (ri.Pen != null && ri.Pen.StrokeBrush != null)
                        {
                            ri.Pen = ri.Pen.Clone();
                            ri.Pen.StrokeBrush = ri.Pen.StrokeBrush.ApplyTransformCopy(p.Transform);
                        }
                    }

                    ri.DrawGeometry(g, desp, p);
                }

                return;
            }

            ImagePrimitive ip = primitive as ImagePrimitive;

            if (ip != null)
            {
                ri.RenderImage(ip.Image, ip.DstRect, ip.Clip, ip.Transform, desp);
            }
            else
            {
                Debug.Assert(false, "Wrong Primitive type");
            }
        }

        #endregion

        #region Private Fields

        private IProxyDrawingContext _dc;
        private DisplayList          _dl;

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Flatten Primitive to ILegacyDevice
        /// </summary>
        public static void Convert(Primitive tree, ILegacyDevice dc, double width, double height, double dpix, double dpiy,
            Nullable<OutputQuality> quality)
        {
            // Change Flattener quality setting based on OutputQualityValue
            if (quality != null)
            {
                switch (quality)
                {
                    case OutputQuality.Unknown:
                    case OutputQuality.Automatic:
                        break;

                    case OutputQuality.Draft:
                    case OutputQuality.Fax:
                    case OutputQuality.Text:
                        Configuration.RasterizationDPI = 96;
                        Configuration.MaximumTransparencyLayer = 8;
                        Configuration.GradientDecompositionDensity = 0.75;
                        Configuration.DecompositionDepth = 2;
                        break;

                    case OutputQuality.Normal:
                        Configuration.RasterizationDPI = 150;
                        Configuration.MaximumTransparencyLayer = 12;
                        Configuration.GradientDecompositionDensity = 1;
                        Configuration.DecompositionDepth = 3;
                        break;

                    case OutputQuality.High:
                        Configuration.RasterizationDPI = 300;
                        Configuration.MaximumTransparencyLayer = 16;
                        Configuration.GradientDecompositionDensity = 1.25;
                        Configuration.DecompositionDepth = 4;
                        break;

                    case OutputQuality.Photographic:
                        uint dcDpi = Math.Max(PrintQueue.GetDpiX(dc), PrintQueue.GetDpiY(dc));
                        Configuration.RasterizationDPI = (int)(Math.Max(dcDpi, 300));
                        Configuration.MaximumTransparencyLayer = 16;
                        Configuration.GradientDecompositionDensity = 1.25;
                        Configuration.DecompositionDepth = 4;
                        break;
                }
            }

#if DEBUG
            if (Configuration.Verbose >= 2)
            {
                Console.WriteLine();
                Console.WriteLine("\r\nStage 1: Tree Flattening");
                Console.WriteLine();
            }
#endif

            // Paper dimension as clipping
            Geometry clip = null;

            if ((width != 0) && (height != 0))
            {
                clip = new RectangleGeometry(new Rect(0, 0, width, height));
            }

            // Transform to device resolution
            Matrix transform = Matrix.Identity;

            transform.Scale(dpix / 96, dpiy / 96);

            Flattener fl = new Flattener(false, width, height);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXTreeFlattenBegin);

            fl.TreeFlatten(tree, clip, transform, 1.0, null);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXTreeFlattenEnd);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXAlphaFlattenBegin);

            fl.AlphaFlatten(new BrushProxyDecomposer(dc), false);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXAlphaFlattenEnd);

#if DEBUG
            if (Configuration.Verbose >= 2)
            {
                Console.WriteLine();
                Console.WriteLine("End AlphaFlattening");
                Console.WriteLine();
            }
#endif
        }

        #endregion
    } // end of class Flattener

    /// <summary>
    /// Implement ILegacyDevice using DrawingContext
    /// </summary>
    internal class OutputContext : ILegacyDevice
    {
        #region Private Fields

        private DrawingContext _ctx;

        #endregion

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public OutputContext(DrawingContext context)
        {
            _ctx = context;
        }

        #endregion

        #region ILegacyDevice Members

        void ILegacyDevice.PopClip()
        {
            _ctx.Pop();
        }

        void ILegacyDevice.PopTransform()
        {
            _ctx.Pop();
        }

        void ILegacyDevice.PushClip(Geometry clipGeometry)
        {
            _ctx.PushClip(clipGeometry);
        }

        void ILegacyDevice.PushTransform(Matrix transform)
        {
            _ctx.PushTransform(new MatrixTransform(transform));
        }

        int ILegacyDevice.StartDocument(string printerName, string jobName, string filename, byte[] devmode)
        {
            throw new InvalidOperationException();
        }

        void ILegacyDevice.StartDocumentWithoutCreatingDC(string printerName, string jobName, string filename)
        {
            throw new InvalidOperationException();
        }

        void ILegacyDevice.EndDocument()
        {
            throw new InvalidOperationException();
        }

        void ILegacyDevice.CreateDeviceContext(string printerName, string jobName, byte[] devmode)
        {
            throw new InvalidOperationException();
        }

        void ILegacyDevice.DeleteDeviceContext()
        {
            throw new InvalidOperationException();
        }

        String ILegacyDevice.ExtEscGetName()
        {
            throw new InvalidOperationException();
        }

        bool ILegacyDevice.ExtEscMXDWPassThru()
        {
            throw new InvalidOperationException();
        }

        void ILegacyDevice.StartPage(byte[] devmode, int rasterizationDPI)
        {
            throw new InvalidOperationException();
        }

/*      void ILegacyDevice.PushOpacity(double opacity, Brush opacityMask)
        {
            Debug.Assert(opacityMask == null);

            _ctx.PushOpacity(opacity);
        }
*/
        void ILegacyDevice.DrawGeometry(Brush brush, Pen pen, Brush strokeBrush, Geometry geometry)
        {
            if (pen != null)
            {
                if (strokeBrush == null)
                {
                    pen = null;
                }
                else
                {
                    pen = pen.CloneCurrentValue();
                    pen.Brush = strokeBrush;
                }
            }

            _ctx.DrawGeometry(brush, pen, geometry);
        }

        void ILegacyDevice.DrawImage(BitmapSource source, Byte[] buffer, Rect rc)
        {
            if (buffer != null)
            {
                source = BitmapSource.Create(source.PixelWidth, source.PixelHeight,
                            96, 96, PixelFormats.Pbgra32, null, buffer, source.PixelWidth * 4);
            }

            _ctx.DrawImage(source, rc);
        }

        void ILegacyDevice.DrawGlyphRun(Brush foreground, GlyphRun glyphRun)
        {
            _ctx.DrawGlyphRun(foreground, glyphRun);
        }

        void ILegacyDevice.Comment(string message)
        {
        }

        void ILegacyDevice.EndPage()
        {
            throw new InvalidOperationException();
        }

        #endregion
    }
} // end of namespace
