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
using System.Windows.Xps.Serialization;
using MS.Utility;

namespace Microsoft.Internal.AlphaFlattener
{
    internal class Cluster
    {
        #region Private Fields

        private List<int> m_primitives;
        private Rect m_bounds;

#if DEBUG
        private bool      m_debugRasterize;
#endif

        private int m_lowestPrimitive;     // lowest index in m_primitives

        #endregion

        #region Constructors

        public Cluster()
        {
            m_primitives = new List<int>();
            m_bounds     = Rect.Empty;

            m_lowestPrimitive = int.MaxValue;
        }

        #endregion

        #region Public Methods

        public void Render(List<PrimitiveInfo> commands, IProxyDrawingContext dc)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterStart);

            int width  = (int)Math.Round(m_bounds.Width * Configuration.RasterizationDPI / 96);
            int height = (int)Math.Round(m_bounds.Height * Configuration.RasterizationDPI / 96);

            if ((width >= 1) && (height >= 1)) // Skip shape which is too small
            {
                Matrix mat = Utility.CreateMappingTransform(m_bounds, width, height);

                RenderTargetBitmap brushImage = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                DrawingVisual visual = new DrawingVisual();

                // clip image to the primitives it contains
                Geometry clip = null;

                using (DrawingContext ctx = visual.RenderOpen())
                {
                    ctx.PushTransform(new MatrixTransform(mat));

                    m_primitives.Sort();

                    foreach (int i in m_primitives)
                    {
                        Primitive primitive = commands[i].primitive;

                        // Fix bug 1396393: Can't use AddToCluster without fixing that bug.
                        // We need to clip cluster to primitives in case primitives have clipping.
                        Debug.Assert(GetPrimitiveIntersectAction() == PrimitiveIntersectAction.ClipToCluster);

                        bool empty;
                        Geometry geometry = Utility.Intersect(primitive.GetShapeGeometry(), primitive.Clip, Matrix.Identity, out empty);

                        if (!empty)
                        {
                            primitive.OnRender(ctx);

                            // clip cluster to this clipped primitive. we must have geometry information,
                            // otherwise the primitive will be clipped away.
                            Debug.Assert(geometry != null, "No geometry available for primitive");

                            if (clip == null)
                            {
                                clip = geometry;
                            }
                            else
                            {
                                CombinedGeometry cg = new CombinedGeometry();

                                // Opt-out of inheritance through the new Freezable.
                                cg.CanBeInheritanceContext = false;
                                cg.GeometryCombineMode     = GeometryCombineMode.Union;
                                cg.Geometry1               = clip;
                                cg.Geometry2               = geometry;

                                clip = cg;
                            }
                        }

                        commands[i] = null; // Command can be deleted after cluster is rendered
                    }

                    ctx.Pop();
                }

                brushImage.Render(visual);

                Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterEnd);

                dc.DrawImage(new ImageProxy(brushImage), m_bounds, clip, Matrix.Identity);
            }
        }

        #endregion

        #region Public Static Methods

        public static void CheckForRasterization(List<Cluster> clusters, List<PrimitiveInfo> commands)
        {
            foreach (Cluster c in clusters)
            {
                if (c.BetterRasterize(commands))
                {
#if DEBUG
                    c.m_debugRasterize = true;
#endif

                    foreach (int i in c.m_primitives)
                    {
                        commands[i].m_cluster = c;
                    }
                }
            }
        }

        public static List<Cluster> CalculateCluster(List<PrimitiveInfo> commands, int count, bool disjoint, List<int>[] oldUnderlay)
        {
            List<Cluster> transparentCluster = new List<Cluster>();

            // indicates which primitives have been added to any cluster
            bool[] addedPrimitives = new bool[commands.Count];

            // calculate clusters until cluster bounds stabilize
            while (true)
            {
                bool clusterBoundsChanged = CalculateClusterCore(
                    commands,
                    count,
                    disjoint,
                    oldUnderlay,
                    transparentCluster,
                    addedPrimitives
                    );

                if (!clusterBoundsChanged || GetPrimitiveIntersectAction() != PrimitiveIntersectAction.AddToCluster)
                    break;

                //
                // Cluster bounds have changed somewhere, need to examine all primitives that haven't
                // been added to a cluster and test for intersection with cluster. We add primitives
                // that intersect and rendered before the cluster.
                //
                // Note that here we check even opaque primitives, since they might get covered
                // by a transparent cluster, and thus need to be rasterized with a cluster if intersection
                // exists.
                //
                for (int primIndex = 0; primIndex < addedPrimitives.Length; primIndex++)
                {
                    if (!addedPrimitives[primIndex] && commands[primIndex] != null)
                    {
                        PrimitiveInfo primitive = commands[primIndex];

                        for (int clusterIndex = 0; clusterIndex < transparentCluster.Count; clusterIndex++)
                        {
                            Cluster cluster = transparentCluster[clusterIndex];

                            if (primitive.GetClippedBounds().IntersectsWith(cluster.m_bounds) &&
                                primIndex < cluster.m_lowestPrimitive)
                            {
                                // primitive intersects this cluster, add to cluster
                                cluster.Add(
                                    primIndex,
                                    oldUnderlay,
                                    commands,
                                    addedPrimitives
                                );
                            }
                        }
                    }
                }
            }

            return transparentCluster;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// What action to take to handle cases where primitives intersect a cluster's bounding box,
        /// but does not touch any of the cluster's primitives.
        /// </summary>
        private enum PrimitiveIntersectAction
        {
            /// <summary>
            /// Don't do anything, behavior corresponds to
            /// bug 1267200: MGC - ToolBarTray element does not print in some cases.
            /// </summary>
            None,

            /// <summary>
            /// Adds the primitive to the cluster for rasterization.
            /// </summary>
            AddToCluster,

            /// <summary>
            /// Clips the cluster rasterized image to the primitives it contains.
            /// </summary>
            ClipToCluster,
        }

        private static PrimitiveIntersectAction GetPrimitiveIntersectAction()
        {
            //
            // Fix bug 1396393: Printing: Background pictures are only partially printed for an xCal calendar
            //
            // The bug involves printing a calendar. What happens is the January ImagePrimitive is clipped
            // to the rectangle for January, but it's rasterized as part of a cluster with a color fill of
            // that same rectangle. Cluster rasterization loses the clipping on the picture, which effectively
            // makes the cluster grow to overlap April below January. This causes April to appear clipped.
            //
            // Fixing involves clipping clusters to its clipped primitives. As such, AddToCluster cannot
            // be used without further fixes.
            //
            return PrimitiveIntersectAction.ClipToCluster;
        }

        /// <summary>
        /// Adds primitive to this cluster.
        /// </summary>
        /// <param name="i">primitive index</param>
        /// <param name="underlay">underlay information</param>
        /// <param name="commands">list of primitives</param>
        /// <param name="addedPrimitives">indicates which primitives have been added to any cluster</param>
        private void Add(
            int i,
            List<int>[] underlay,
            List<PrimitiveInfo> commands,
            bool[] addedPrimitives
            )
        {
            // clip bounds to avoid generating excessively large cluster
            Rect bounds = commands[i].GetClippedBounds();

            m_bounds.Union(bounds);
            m_primitives.Add(i);
            addedPrimitives[i] = true;

            if (i < m_lowestPrimitive)
            {
                m_lowestPrimitive = i;
            }

            // Add all primitives covered by i recursively
            if ((underlay != null) && (underlay[i] != null))
            {
                foreach (int j in underlay[i])
                {
                    if ((commands[j] != null) && m_primitives.IndexOf(j) < 0)
                    {
                        Add(j, underlay, commands, addedPrimitives);
                    }
                }
            }
        }

        private void MergeWith(Cluster c)
        {
            m_bounds.Union(c.m_bounds);

            for (int i = 0; i < c.m_primitives.Count; i++)
            {
                if (m_primitives.IndexOf(c.m_primitives[i]) < 0)
                {
                    m_primitives.Add(c.m_primitives[i]);
                }
            }
        }

        /// <summary>
        /// Check if the whole cluster is complex enough such that rasterizing the whole thing is better than flattening it
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        private bool BetterRasterize(List<PrimitiveInfo> commands)
        {
            double clustersize = m_bounds.Width * m_bounds.Height;

            double diff = - Configuration.RasterizationCost(m_bounds.Width, m_bounds.Height);

            // Estimate cost of geometry operations (intersecting)
            double pathComplexity = 1;

            foreach (int i in m_primitives)
            {
                PrimitiveInfo pi = commands[i];

                Primitive p = pi.primitive;

                GeometryPrimitive gp = p as GeometryPrimitive;

                Rect bounds = pi.GetClippedBounds();

                bool rasterize = true;

                if (gp != null)
                {
                    double complexity = 1;

                    Geometry geo = gp.Geometry;

                    if (geo != null)
                    {
                        complexity = Utility.GetGeometryPointCount(geo);

                        // weight down the complexity of small region
                        complexity *= bounds.Width * bounds.Height / clustersize;
                    }

                    BrushProxy bp = gp.Brush;

                    if (bp == null)
                    {
                        bp = gp.Pen.StrokeBrush;

                        // Widen path would at least double the points
                        complexity *= 3;
                    }

                    if (complexity > 1)
                    {
                        pathComplexity *= complexity;

                        if (pathComplexity > 100000) // 333 x 333
                        {
                            return true;
                        }
                    }

                    if (bp != null)
                    {
                        Brush b = bp.Brush;

                        if ((b != null) && ((b is SolidColorBrush) || (b is LinearGradientBrush)))
                        {
                            // SolidColorBrush does not need full rasterization
                            // Vertical/Horizontal linear gradient brush does not need full rasterization
                            rasterize = false;
                        }
                    }
                }

                if (rasterize)
                {
                    diff += Configuration.RasterizationCost(bounds.Width, bounds.Height);

                    if (diff > 0)
                    {
                        break;
                    }
                }
            }

            return diff > 0;
        }

        /// <summary>
        /// Calculates list of transparent clusters, returning true if clusters added or bounding rectangles changed.
        /// </summary>
        private static bool CalculateClusterCore(
            List<PrimitiveInfo> commands,
            int count,
            bool disjoint,
            List<int> [] oldUnderlay,
            List<Cluster> transparentCluster,
            bool[] addedPrimitives      // primitives added to clusters
            )
        {
            bool clusterBoundsChanged = false;

            // Build clusters of transparent primitives
            for (int i = 0; i < count; i++)
            {
                PrimitiveInfo pi = commands[i];

                // When disjoint is true (flattening a subtree), add all primitives to a single cluster
                if ((pi != null) && (disjoint || !pi.primitive.IsOpaque) && !addedPrimitives[i])
                {
                    Rect bounds = pi.GetClippedBounds();

                    Cluster home = null;

                    for (int j = 0; j < transparentCluster.Count; j++)
                    {
                        Cluster c = transparentCluster[j];

                        if (disjoint || bounds.IntersectsWith(c.m_bounds))
                        {
                            home = c;
                            break;
                        }
                    }

                    if (home == null)
                    {
                        home = new Cluster();
                        transparentCluster.Add(home);
                    }

                    Rect oldClusterBounds = home.m_bounds;

                    home.Add(i, oldUnderlay, commands, addedPrimitives);

                    if (!clusterBoundsChanged && oldClusterBounds != home.m_bounds)
                    {
                        // cluster bounds have changed
                        clusterBoundsChanged = true;
                    }
                }
            }

            // Merges clusters which touch each other
            bool changed;

            do
            {
                changed = false;

                for (int i = 0; i < transparentCluster.Count; i++)
                    for (int j = i + 1; j < transparentCluster.Count; j++)
                    {
                        if (transparentCluster[i].m_bounds.IntersectsWith(transparentCluster[j].m_bounds))
                        {
                            transparentCluster[i].MergeWith(transparentCluster[j]);

                            // cluster bounds have changed since merging two clusters
                            clusterBoundsChanged = true;

                            transparentCluster.RemoveAt(j);
                            changed = true;
                            break;
                        }
                    }
            }
            while (changed);

            return clusterBoundsChanged;
        }

        #endregion

        #if DEBUG

        internal List<int> DebugPrimitives
        {
            get
            {
                return m_primitives;
            }
        }

        internal Rect DebugBounds
        {
            get
            {
                return m_bounds;
            }
        }

        internal bool DebugRasterize
        {
            get
            {
                return m_debugRasterize;
            }
        }

        #endif
    }
} // end of namespace
