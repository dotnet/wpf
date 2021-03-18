// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Collections;              // for ArrayList
using System.Collections.Generic;      // for Dictionary
using System.IO;
using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Windows.Media.Imaging;

using System.Windows.Xps.Serialization;
using System.Printing;
using System.Printing.Interop;
using System.Security;
using System.Text;
using MS.Utility;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    ///
    /// </summary>
    internal class MetroDevice0
    {
        [Flags]
        private enum DeviceState
        {
            NoChange       = 0,
            Init           = 1,
            DocStarted     = 2,
            PageStarted    = 4
        };

        private enum PushType
        {
            Opacity,
            Clip,
            Transform
        };

        private DeviceState     _state;

        private CanvasPrimitive _page;          // Page
        private CanvasPrimitive _root;          // Current root
        private BrushProxy      _opacityMask;   // Current layer OpacityMask
        private double          _opacity;       // Current layer Opacity
        private Geometry        _clip;          // Current layer Clip

        private Stack           _stack;

#if UNIT_TEST
        static private bool     s_first = true;
#endif
        /// <summary>
        ///
        /// </summary>
        public MetroDevice0()
        {
#if UNIT_TEST
            if (s_first)
            {
                s_first = false;

                RectangleIntersection.UnitTest();
            }
#endif

            _state = DeviceState.Init;
        }

        private void AssertState(DeviceState state, DeviceState next)
        {
            if ((_state & state) != state)
            {
                throw new InvalidOperationException();
            }

            if (next != DeviceState.NoChange)
            {
                _state = next;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void StartDocument()
        {
            AssertState(DeviceState.Init, DeviceState.DocStarted);
        }

        /// <summary>
        ///
        /// </summary>
        public void EndDocument()
        {
            AssertState(DeviceState.DocStarted, DeviceState.Init);
        }


        /// <summary>
        ///
        /// </summary>
        public void AbortDocument()
        {
            _state = DeviceState.Init;
        }

        /// <summary>
        ///
        /// </summary>
        public bool StartPage()
        {
            AssertState(DeviceState.DocStarted, DeviceState.PageStarted);

            _page = new CanvasPrimitive();
            _root = _page;
            _stack = new Stack();

            _opacity     = 1;
            _opacityMask = null;
            _clip        = null;

            return true;
        }

        /// <summary>
        /// Start Tree/alpha flattening and send result to ILegacyDevice interface
        /// </summary>
        public void FlushPage(ILegacyDevice sink, double width, double height, Nullable<OutputQuality> outputQuality)
        {
            AssertState(DeviceState.PageStarted, DeviceState.DocStarted);

            if (_stack.Count != 0)
            {
                throw new InvalidOperationException();
            }

            if (sink != null)
            {
                Flattener.Convert(_root, sink, width, height, 96, 96, outputQuality);
            }
        }

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            // Ignore total transparent primitive
            if (Utility.IsTransparent(_opacity) || ((brush == null) && (pen == null || pen.Brush == null)) || (geometry == null))
            {
                return;
            }

            // Split if having both pen and brush
            if ((brush != null) && (pen != null))
            // if (!Utility.IsOpaque(_opacity) || (_opacityMask != null))
            {
                // Push a canvas to handle geometry with brush + pen properly
                Push(Matrix.Identity, null, 1.0, null, Rect.Empty, false);

                DrawGeometry(brush, null, geometry);
                DrawGeometry(null, pen, geometry);

                Pop();

                return;
            }

            AssertState(DeviceState.PageStarted, DeviceState.NoChange);

            GeometryPrimitive g = new GeometryPrimitive();

            g.Geometry    = geometry;
            g.Clip        = _clip;
            g.Opacity     = _opacity;
            g.OpacityMask = _opacityMask;

            int needBounds = 0; // 1 for fill, 2 for stroke

            if (brush != null)
            {
                // Fix bug 1427695: Need bounds for non-SolidColorBrushes to enable rebuilding Brush from BrushProxy.
                if (!(brush is SolidColorBrush))
                {
                    needBounds |= 1;
                }
            }

            if ((pen != null) && (pen.Brush != null))
            {
                if (!(pen.Brush is SolidColorBrush))
                {
                    needBounds |= 2;
                }
            }

            if (g.OpacityMask != null)
            {
                if (g.OpacityMask.BrushList == null && !(g.OpacityMask.Brush is SolidColorBrush))
                {
                    if (pen != null)
                    {
                        needBounds |= 2;
                    }
                    else
                    {
                        needBounds |= 1;
                    }
                }
            }

            Rect bounds = g.GetRectBounds((needBounds & 1) != 0);

            if (brush != null)
            {
                g.Brush = BrushProxy.CreateBrush(brush, bounds);
            }

            if ((needBounds & 2) != 0)
            {
                bounds = geometry.GetRenderBounds(pen);
            }

            if ((pen != null) && (pen.Brush != null))
            {
                g.Pen = PenProxy.CreatePen(pen, bounds);
            }

            if (g.OpacityMask != null)
            {
                if (!g.OpacityMask.MakeBrushAbsolute(bounds))
                {
                    // Fix bug 1463955: Brush has become empty; replace with transparent brush.
                    g.OpacityMask = BrushProxy.CreateColorBrush(Colors.Transparent);
                }
            }

            // Optimization: Unfold primitive DrawingBrush when possible to avoid rasterizing it.
            Primitive primitive = g.UnfoldDrawingBrush();

            if (primitive != null)
            {
                _root.Children.Add(primitive);
            }
        }

        public void DrawImage(ImageSource image, Rect rectangle)
        {
            if (image == null)
                return;

            AssertState(DeviceState.PageStarted, DeviceState.NoChange);

            ImagePrimitive g = new ImagePrimitive();

            g.Image   = new ImageProxy((BitmapSource)image);

            g.DstRect     = rectangle;
            g.Clip        = _clip;
            g.Opacity     = _opacity;
            g.OpacityMask = _opacityMask;

            _root.Children.Add(g);
        }

        public void DrawGlyphRun(Brush foreground, GlyphRun glyphRun)
        {
            if (foreground == null)
                return;

            AssertState(DeviceState.PageStarted, DeviceState.NoChange);

            GlyphPrimitive g = new GlyphPrimitive();

            bool needBounds = false;

            if (foreground != null && !(foreground is SolidColorBrush))
            {
                needBounds = true;
            }

            g.GlyphRun    = glyphRun;
            g.Brush       = BrushProxy.CreateBrush(foreground, g.GetRectBounds(needBounds));
            g.Clip        = _clip;
            g.Opacity     = _opacity;
            g.OpacityMask = _opacityMask;

            // Optimization: Unfold primitive DrawingBrush when possible to avoid rasterizing it.
            Primitive primitive = g.UnfoldDrawingBrush();

            if (primitive != null)
            {
                _root.Children.Add(primitive);
            }
        }

        public void Push(Matrix transform, Geometry clip, double opacity, Brush opacityMask, Rect maskBounds, bool onePrimitive)
        {
            AssertState(DeviceState.PageStarted, DeviceState.NoChange);

            opacity = Utility.NormalizeOpacity(opacity);

            if (!Utility.IsValid(transform))
            {
                // treat as invisible subtree
                opacity = 0.0;
                transform = Matrix.Identity;
            }

            _stack.Push(_root);
            _stack.Push(_clip);
            _stack.Push(_opacity);
            _stack.Push(_opacityMask);

            bool noTrans = transform.IsIdentity;

            if (onePrimitive && noTrans && (opacityMask == null))
            {
                bool empty;

                _clip        = Utility.Intersect(_clip, clip, Matrix.Identity, out empty);
                _opacity    *= opacity;
                _opacityMask = null;

                if (empty)
                {
                    _opacity = 0;
                }
            }
            else
            {
                CanvasPrimitive c = new CanvasPrimitive();

                if (noTrans)
                {
                    c.Transform = Matrix.Identity;
                }
                else
                {
                    c.Transform = transform;

                    Matrix invertTransform = transform;
                    invertTransform.Invert();

                    _clip = Utility.TransformGeometry(_clip, invertTransform); // transform inherited clip to this level
                }

                bool empty;

                c.Clip    = Utility.Intersect(clip, _clip, Matrix.Identity, out empty); // Combined with inherited attributes
                c.Opacity = opacity * _opacity;

                if (empty)
                {
                    c.Opacity = 0; // Invisible
                }

                if (opacityMask != null)
                {
                    double op;

                    if (Utility.ExtractOpacityMaskOpacity(opacityMask, out op, maskBounds))
                    {
                        c.Opacity = opacity * _opacity * op;
                    }
                    else
                    {
                        c.OpacityMask = BrushProxy.CreateOpacityMaskBrush(opacityMask, maskBounds);
                    }
                }

                _root.Children.Add(c);

                _root = c;

                _clip        = null;
                _opacity     = 1.0;
                _opacityMask = null;
            }
        }

        public void Pop()
        {
            AssertState(DeviceState.PageStarted, DeviceState.NoChange);

            _opacityMask = _stack.Pop() as BrushProxy;
            _opacity     = (double) _stack.Pop();
            _clip        = _stack.Pop() as Geometry;
            _root        = _stack.Pop() as CanvasPrimitive;
        }

#if COMMENT
        void Comment(string message)
        {
        }
#endif
    }

    /// <summary>
    /// Main interface to NGCSerializationManager, and glue flatteners and GDIExporter together
    ///
    /// 1) Implements IMetroDrawingContext interface
    /// 2) Passes drawing primitives to MetroDevice0 to build a primitive tree
    /// 3) In FlushPage, calls tree flattener to flat primitive tree to a linear display list
    /// 4) In FlushPage, calls alpha flattener to resolve transparency and send down result on the fly to GDIExporter which implements the ILegacyDevice interface
    /// 5) In FlushPage, calls GDIExporter.EndPage to finish printing a page
    /// 6) Supporting Testing hook to serialize alpha flattening result
    /// </summary>
    internal class MetroToGdiConverter : IMetroDrawingContext
    {
        static protected object         s_TestingHook;

        protected  MetroDevice0         m_Flattener;
        protected  ILegacyDevice        m_GDIExporter;
        protected  PrintQueue           m_PrintQueue;
        protected  PrintTicketConverter m_Converter; // Expensive to create, cache it
        protected  PrintTicketCache     m_printTicketCache; // Cache for per ticket data that is expensive to fetch

        protected  byte[]             m_Devmode;

        // settings captured from current PrintTicket
        protected  double             m_PageWidth;
        protected  double             m_PageHeight;

        protected Nullable<OutputQuality> m_OutputQuality;


        // PrintTicket XML Strings start at 4KB so we don't want the keys
        // from chewing all memory.  Limit to a small amount
        private const int s_PrintTicketCacheMaxCount = 10;

        public MetroToGdiConverter(PrintQueue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            m_PrintQueue  = queue;
            m_Flattener   = new MetroDevice0();
            m_GDIExporter = queue.GetLegacyDevice();

            m_printTicketCache = new PrintTicketCache(s_PrintTicketCacheMaxCount);
        }

        /// <param name="ticket"></param>
        /// <param name="ticketXMLString">If you wish to read/write to the cache,
        /// this must be set to the result of ticket.ToXmlString().
        /// This an optional performance enhancement.</param>
        private byte[] GetDevmode(PrintTicket ticket, String ticketXMLString)
        {
            Debug.Assert(ticket != null);

            // The cached devmode is not modified by internal printing code
            // and not exposed to client code so it is safe to pass the byte array
            // by reference instead of making a copy
            byte[] result = null;

            if (ticketXMLString != null &&  m_printTicketCache.TryGetDevMode(ticketXMLString, out result))
            {
            }
            // 10ms slowpath.
            else
            {
                Toolbox.EmitEvent(EventTrace.Event.WClientDRXGetDevModeBegin);

                result = ConvertPrintTicketToDevMode(ticket);

                if (ticketXMLString != null)
                {
                    m_printTicketCache.CacheDevMode(ticketXMLString, result);
                }

                Toolbox.EmitEvent(EventTrace.Event.WClientDRXGetDevModeEnd);
            }

            return result;
        }

        /// <summary>
        /// Captures settings from complete PrintTickets.
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="ticketXMLString">If you wish to read/write to the cache,
        /// this must be set to the result of ticket.ToXmlString().
        /// This an optional performance enhancement.</param>
        /// <remarks>
        /// PrintTicket changes are communicated through StartDocument and StartPage, which receive
        /// complete tickets (the merge of tickets at all levels). Lack of change is indicated by
        /// null. Here we capture settings from PrintTicket for use in flattening and by GDIExporter.
        /// </remarks>
        private void CaptureTicketSettings(PrintTicket ticket, String ticketXMLString)
        {
            if (ticket == null)
            {
                Debug.Assert(ticketXMLString == null);

                // ticket has not changed since last capture; keep existing settings
            }
            else
            {
                m_OutputQuality = ticket.OutputQuality;

                if (ticketXMLString != null && m_printTicketCache.TryGetPageSize(ticketXMLString, out m_PageWidth, out m_PageHeight))
                {
                }
                // 100ms slowpath.
                else
                {
                    //
                    // Fix bug: 1396328, 1394678: Incorrect page dimensions used for clipping.
                    //
                    // PrintTicket contains the application-requested page dimensions. The printer
                    // may print to a page of different dimensions; that information is returned in
                    // PrintCapabilities, and is what we need to use for clipping.
                    //
                    PrintCapabilities capabilities = m_PrintQueue.GetPrintCapabilities(ticket);

                    m_PageWidth = capabilities.OrientedPageMediaWidth.GetValueOrDefault(816);
                    m_PageHeight = capabilities.OrientedPageMediaHeight.GetValueOrDefault(1056);

                    if(ticketXMLString != null)
                    {
                        m_printTicketCache.CachePageSize(ticketXMLString, m_PageWidth, m_PageHeight);
                    }
                }
            }
        }

        public int StartDocument(string jobName, PrintTicket ticket)
        {
            int jobIdentifier = 0;

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXStartDocBegin);

            if (ticket == null)
            {
                ticket = m_PrintQueue.UserPrintTicket;
            }

            // Do not cache the calculation since StartDocument is only called
            // once per document, instead of per page, and we don't want to
            // hog precious cache entries.
            CaptureTicketSettings(ticket, null/*dont cache*/);

            m_Flattener.StartDocument();

            if (s_TestingHook == null)
            {
                m_Devmode = GetDevmode(ticket, null/*dont cache*/);

                jobIdentifier = m_GDIExporter.StartDocument(m_PrintQueue.FullName, jobName, Configuration.OutputFile, m_Devmode);
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXStartDocEnd);

            return jobIdentifier;
        }

        public void EndDocument()
        {
            EndDocument(abort:false);
        }

        public void EndDocument(bool abort)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXEndDocBegin);

            if (abort)
            {
                m_Flattener.AbortDocument();
            }
            else
            {
                m_Flattener.EndDocument();
            }

            if (s_TestingHook == null)
            {
                m_GDIExporter.EndDocument();
            }

            DisposePrintTicketConverter();

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXEndDocEnd);
        }


        public void CreateDeviceContext(string jobName, PrintTicket ticket)
        {
            if (ticket == null)
            {
                ticket = m_PrintQueue.UserPrintTicket;
            }

            // Do not cache the calculation since we aren't called once per page,
            // and we don't want to hog precious cache entries.
            CaptureTicketSettings(ticket, null/*dont cache*/);

            m_Devmode = GetDevmode(ticket, null/*dont cache*/);

            m_GDIExporter.CreateDeviceContext(m_PrintQueue.FullName, jobName, m_Devmode);
        }

        public void DeleteDeviceContext()
        {
            m_GDIExporter.DeleteDeviceContext();
        }

        public void StartDocumentWithoutCreatingDC(String jobName)
        {
            m_Flattener.StartDocument();

            String printerName = m_PrintQueue.FullName;

            m_GDIExporter.StartDocumentWithoutCreatingDC(printerName, jobName, Configuration.OutputFile);
        }

        public string ExtEscGetName()
        {
            return m_GDIExporter.ExtEscGetName();
        }

        public bool ExtEscMXDWPassThru()
        {
            return m_GDIExporter.ExtEscMXDWPassThru();
        }

        public void AbortDocument()
        {
            m_Flattener.AbortDocument();

            m_GDIExporter.EndDocument();

            DisposePrintTicketConverter();
        }

        public void StartPage(PrintTicket ticket)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXStartPageBegin);

            String printTicketXMLStr = (ticket == null) ? null : ticket.ToXmlString();

            CaptureTicketSettings(ticket, printTicketXMLStr);

            if (m_Flattener.StartPage())
            {
                if (s_TestingHook == null)
                {
                    if (ticket != null)
                    {
                        m_GDIExporter.StartPage(GetDevmode(ticket, printTicketXMLStr), Configuration.RasterizationDPI);
                    }
                    else
                    {
                        m_GDIExporter.StartPage(null, Configuration.RasterizationDPI);
                    }
                }
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXStartPageEnd);
        }

        public void FlushPage()
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXFlushPageStart);

            ILegacyDevice ioc = m_GDIExporter;

            if (s_TestingHook != null)
            {
                DrawingContext dc = s_TestingHook as DrawingContext;

                if (dc != null)
                {
                    // Send alpha flattening output to a DrawingContext for testing
                    ioc = new OutputContext(dc);
                }
            }

            m_Flattener.FlushPage(ioc, m_PageWidth, m_PageHeight, m_OutputQuality);

            if (s_TestingHook == null)
            {
                Toolbox.EmitEvent(EventTrace.Event.WClientDRXEndPageBegin);
                m_GDIExporter.EndPage();
                Toolbox.EmitEvent(EventTrace.Event.WClientDRXEndPageEnd);
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXFlushPageStop);
        }

        void IMetroDrawingContext.DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            m_Flattener.DrawGeometry(brush, pen, geometry);
        }

        void IMetroDrawingContext.DrawImage(ImageSource image, Rect rectangle)
        {
            m_Flattener.DrawImage(image, rectangle);
        }

        void IMetroDrawingContext.DrawGlyphRun(Brush foreground, GlyphRun glyphRun)
        {
            m_Flattener.DrawGlyphRun(foreground, glyphRun);
        }

        void IMetroDrawingContext.Push(
            Matrix transform,
            Geometry clip,
            double opacity,
            Brush opacityMask,
            Rect maskBounds,
            bool onePrimitive,

            // serialization attributes
            String nameAttr,
            Visual node,
            Uri navigateUri,
            EdgeMode edgeMode
            )
        {
            m_Flattener.Push(transform, clip, opacity, opacityMask, maskBounds, onePrimitive);
        }

        void IMetroDrawingContext.Pop()
        {
            m_Flattener.Pop();
        }

        void IMetroDrawingContext.Comment(string message)
        {
        }

        private byte[] ConvertPrintTicketToDevMode(PrintTicket ticket)
        {
            if (m_Converter == null)
            {
                m_Converter = new PrintTicketConverter(m_PrintQueue.FullName, m_PrintQueue.ClientPrintSchemaVersion);
            }

            return m_Converter.ConvertPrintTicketToDevMode(ticket, BaseDevModeType.UserDefault);
        }


        private void DisposePrintTicketConverter()
        {
            if (m_Converter != null)
            {
                m_Converter.Dispose();
                m_Converter = null;
            }
        }

        /// <summary>
        /// Called before StartDocument to by-pass GDIExporter and send result to DrawingContext
        /// </summary>
        /// <param name="obj"></param>
        static public void TestingHook(Object obj)
        {
            if (obj != null)
            {
                s_TestingHook = obj as DrawingContext;
            }
        }
    }

    // Cache for per ticket data that is expensive to fetch
    internal class PrintTicketCache
    {
        public PrintTicketCache(int maxEntries)
        {
            if(maxEntries < 1)
            {
                throw new ArgumentOutOfRangeException("maxEntries", maxEntries, string.Empty);
            }

            this.m_innerCache = new MS.Internal.Printing.MostFrequentlyUsedCache<string, CachePacket>(maxEntries);
        }

        public void CachePageSize(string ticket, double width, double height)
        {
            if(ticket == null)
            {
                throw new ArgumentNullException("ticket");
            }

            EnsurePacketForKey(ticket).PageSize = new Size(width, height);
        }

        public void CacheDevMode(string ticket, byte [] devMode)
        {
            if(ticket == null)
            {
                throw new ArgumentNullException("ticket");
            }

            if(devMode == null)
            {
                throw new ArgumentNullException("devMode");
            }

            EnsurePacketForKey(ticket).DevMode = devMode;
        }

        public bool TryGetPageSize(string ticket, out double width, out double height)
        {
            if(ticket == null)
            {
                throw new ArgumentNullException("ticket");
            }

            CachePacket packet;
            if(this.m_innerCache.TryGetValue(ticket, out packet))
            {
                if(packet.PageSize.HasValue)
                {
                    width = packet.PageSize.Value.Width;
                    height = packet.PageSize.Value.Height;
                    return true;
                }
            }

            width = 0.0;
            height = 0.0;
            return false;
        }

        public bool TryGetDevMode(string ticket, out byte [] devMode)
        {
            if(ticket == null)
            {
                throw new ArgumentNullException("ticket");
            }

            CachePacket packet;
            if(this.m_innerCache.TryGetValue(ticket, out packet))
            {
                devMode = packet.DevMode;
                if(devMode != null)
                {
                    return true;
                }
            }

            devMode = null;
            return false;
        }

        private CachePacket EnsurePacketForKey(string ticket)
        {
            CachePacket packet;
            if(!this.m_innerCache.TryGetValue(ticket, out packet))
            {
                packet = new CachePacket();
                this.m_innerCache.CacheValue(ticket, packet);
            }

            return packet;
        }

        class CachePacket
        {
            // "null" for any field means it hasn't been set yet.
            // This implies that the fields cannot have real values of "null".
            public Nullable<Size> PageSize;
            public byte [] DevMode;
        }

        private MS.Internal.Printing.MostFrequentlyUsedCache<string, CachePacket> m_innerCache;
    }
} // end of namespace
