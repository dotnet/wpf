// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Set of layout tree verification utilities. 
//

using System;
using System.IO;
using System.Xml;
using System.Windows;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MS.Internal.Documents;
using MS.Internal.PtsHost;

namespace MS.Internal
{
    /// <summary>
    /// Set of layout tree verification utilities.
    /// </summary>
    internal static class LayoutDump
    {
        // ------------------------------------------------------------------
        //
        //  High Level Dump Methods
        //
        // ------------------------------------------------------------------

        #region High Level Dump Methods

        /// <summary>
        /// Dump layout and visual tree starting from specified root.
        /// </summary>
        /// <param name="tagName">Name of the root XML element.</param>
        /// <param name="root">Root of the visual subtree.</param>
        internal static string DumpLayoutAndVisualTreeToString(string tagName, Visual root)
        {
            StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter writer = new XmlTextWriter(stringWriter);

            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;

            DumpLayoutAndVisualTree(writer, tagName, root);

            // Close dump file
            writer.Flush();
            writer.Close();

            return stringWriter.ToString();
        }

        /// <summary>
        /// Dump layout and visual tree starting from specified root.
        /// </summary>
        /// <param name="writer">Stream for dump output.</param>
        /// <param name="tagName">Name of the root XML element.</param>
        /// <param name="root">Root of the visual subtree.</param>
        internal static void DumpLayoutAndVisualTree(XmlTextWriter writer, string tagName, Visual root)
        {
            // Write root element
            writer.WriteStartElement(tagName);

            // Dump layout tree including all visuals
            DumpVisual(writer, root, root);

            // Write end root
            writer.WriteEndElement();
            writer.WriteRaw("\r\n");  // required for bbpack
        }


        /// <summary>
        /// Dump layout tree starting from specified root.
        /// </summary>
        /// <param name="tagName">Name of the root XML element.</param>
        /// <param name="root">Root of the layout subtree.</param>
        /// <param name="fileName">File name to dump to.</param>
        internal static void DumpLayoutTreeToFile(string tagName, UIElement root, string fileName)
        {
            string str = DumpLayoutTreeToString(tagName, root);

            StreamWriter streamWriter = new StreamWriter(fileName);

            streamWriter.Write(str);

            streamWriter.Flush();
            streamWriter.Close();
        }

        /// <summary>
        /// Dump layout tree starting from specified root.
        /// </summary>
        /// <param name="tagName">Name of the root XML element.</param>
        /// <param name="root">Root of the layout subtree.</param>
        internal static string DumpLayoutTreeToString(string tagName, UIElement root)
        {
            StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter writer = new XmlTextWriter(stringWriter);

            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;

            DumpLayoutTree(writer, tagName, root);

            // Close dump file
            writer.Flush();
            writer.Close();

            return stringWriter.ToString();
        }

        /// <summary>
        /// Dump layout tree starting from specified root.
        /// </summary>
        /// <param name="writer">Stream for dump output.</param>
        /// <param name="tagName">Name of the root XML element.</param>
        /// <param name="root">Root of the layout subtree.</param>
        internal static void DumpLayoutTree(XmlTextWriter writer, string tagName, UIElement root)
        {
            // Write root element
            writer.WriteStartElement(tagName);

            // Dump layout tree
            DumpUIElement(writer, root, root, true);

            // Write end root
            writer.WriteEndElement();
            writer.WriteRaw("\r\n");  // required for bbpack
        }

        #endregion High Level Dump Methods

        // ------------------------------------------------------------------
        //
        //  Custom Element Handling
        //
        // ------------------------------------------------------------------

        #region Custom Element Handling

        /// <summary>
        /// Add new mapping from UIElement type to dump methdod (DumpCustomUIElement).
        /// </summary>
        internal static void AddUIElementDumpHandler(Type type, DumpCustomUIElement dumper)
        {
            _elementToDumpHandler.Add(type, dumper);
        }

        /// <summary>
        /// Add new mapping from DocumentPage type to dump methdod (DumpCustomDocumentPage).
        /// </summary>
        internal static void AddDocumentPageDumpHandler(Type type, DumpCustomDocumentPage dumper)
        {
            _documentPageToDumpHandler.Add(type, dumper);
        }

        /// <summary>
        /// Dumper delegate for custom UIElements.
        /// </summary>
        internal delegate bool DumpCustomUIElement(XmlTextWriter writer, UIElement element, bool uiElementsOnly);

        /// <summary>
        /// Dumper delegate for custom DocumentPages.
        /// </summary>
        internal delegate void DumpCustomDocumentPage(XmlTextWriter writer, DocumentPage page);

        /// <summary>
        /// Mapping from UIElement type to dump methdod (DumpCustomUIElement).
        /// </summary>
        private static Hashtable _elementToDumpHandler = new Hashtable();

        /// <summary>
        /// Mapping from DocumentPage type to dump methdod (DumpCustomUIElement).
        /// </summary>
        private static Hashtable _documentPageToDumpHandler = new Hashtable();

        #endregion Custom Element Handling

        // ------------------------------------------------------------------
        //
        //  Basic UIElement/Visual Dump
        //
        // ------------------------------------------------------------------

        #region Basic UIElement/Visual Dump

        // ------------------------------------------------------------------
        // Dump content of Visual.
        // ------------------------------------------------------------------
        internal static void DumpVisual(XmlTextWriter writer, Visual visual, Visual parent)
        {
            if (visual is UIElement)
            {
                DumpUIElement(writer, (UIElement)visual, parent, false);
            }
            else
            {
                writer.WriteStartElement(visual.GetType().Name);

                // Dump visual bounds
                Rect bounds = visual.VisualContentBounds;
                if(!bounds.IsEmpty)
                {
                    DumpRect(writer, "ContentRect", bounds);
                }

                // Dump clip geometry
                Geometry clip = VisualTreeHelper.GetClip(visual);
                if (clip != null)
                {
                    DumpRect(writer, "Clip.Bounds", clip.Bounds);
                }

                // Dump transform relative to its parent
                GeneralTransform g = visual.TransformToAncestor(parent);
                Point point = new Point(0, 0);
                g.TryTransform(point, out point);
                if (point.X != 0 || point.Y != 0)
                {
                    DumpPoint(writer, "Position", point);
                }

                // Dump visual children
                DumpVisualChildren(writer, "Children", visual);
                
                writer.WriteEndElement();
            }
        }

        // ------------------------------------------------------------------
        // Dump content of UIElement.
        // ------------------------------------------------------------------
        private static void DumpUIElement(XmlTextWriter writer, UIElement element, Visual parent, bool uiElementsOnly)
        {
            writer.WriteStartElement(element.GetType().Name);

            // Dump layout information
            DumpSize(writer, "DesiredSize", element.DesiredSize);
            DumpSize(writer, "ComputedSize", element.RenderSize);
            Geometry clip = VisualTreeHelper.GetClip(element);
            if (clip != null)
            {
                DumpRect(writer, "Clip.Bounds", clip.Bounds);
            }

            // Dump transform relative to its parent
            GeneralTransform g = element.TransformToAncestor(parent);
            Point point = new Point(0, 0);
            g.TryTransform(point, out point);
            if (point.X != 0 || point.Y != 0)
            {
                DumpPoint(writer, "Position", point);
            }

            // Dump element specific information
            bool childrenHandled = false;
            Type t = element.GetType();
            DumpCustomUIElement dumpElement = null;
            while(dumpElement==null && t!=null)
            {                
                dumpElement = _elementToDumpHandler[t] as DumpCustomUIElement;
                t = t.BaseType;
            }
            if (dumpElement != null)
            {
                childrenHandled = dumpElement(writer, element, uiElementsOnly);
            }
            
            if (!childrenHandled)
            {
                if (uiElementsOnly)
                {
                    DumpUIElementChildren(writer, "Children", element);
                }
                else
                {
                    DumpVisualChildren(writer, "Children", element);
                }
            }

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump content of DocumentPage.
        // ------------------------------------------------------------------
        internal static void DumpDocumentPage(XmlTextWriter writer, DocumentPage page, Visual parent)
        {
            writer.WriteStartElement("DocumentPage");
            writer.WriteAttributeString("Type", page.GetType().FullName);

            if (page != DocumentPage.Missing)
            {
                DumpSize(writer, "Size", page.Size);

                // Dump transform relative to its parent
                GeneralTransform g = page.Visual.TransformToAncestor(parent);
                Point point = new Point(0, 0);
                g.TryTransform(point, out point);
                if (point.X != 0 || point.Y != 0)
                {
                    DumpPoint(writer, "Position", point);
                }

                // Dump page specific information
                Type t = page.GetType();
                DumpCustomDocumentPage dumpDocumentPage = null;
                while(dumpDocumentPage==null && t!=null)
                {                
                    dumpDocumentPage = _documentPageToDumpHandler[t] as DumpCustomDocumentPage;
                    t = t.BaseType;
                }
                if (dumpDocumentPage != null)
                {
                    dumpDocumentPage(writer, page);
                }                
            }

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump visual children.
        // ------------------------------------------------------------------
        private static void DumpVisualChildren(XmlTextWriter writer, string tagName, Visual visualParent)
        {
            int count = VisualTreeHelper.GetChildrenCount(visualParent);

            if (count>0)
            {
                writer.WriteStartElement(tagName);
                writer.WriteAttributeString("Count", count.ToString(CultureInfo.InvariantCulture));

                for(int i = 0; i < count; i++)
                {
                    DumpVisual(writer, visualParent.InternalGetVisualChild(i), visualParent);
                }

                writer.WriteEndElement();
            }
        }

        // ------------------------------------------------------------------
        // Dump UIELement children.
        // ------------------------------------------------------------------
        internal static void DumpUIElementChildren(XmlTextWriter writer, string tagName, Visual visualParent)
        {
            List<UIElement> uiElements = new List<UIElement>();
            GetUIElementsFromVisual(visualParent, uiElements);

            // For each found UIElement dump its layout info.
            if (uiElements.Count > 0)
            {
                // Dump UIElement children
                writer.WriteStartElement(tagName);
                writer.WriteAttributeString("Count", uiElements.Count.ToString(CultureInfo.InvariantCulture));

                for (int index = 0; index < uiElements.Count; index++)
                {
                    DumpUIElement(writer, uiElements[index], visualParent, true);
                }

                writer.WriteEndElement();
            }
        }

        // ------------------------------------------------------------------
        // Dump point.
        // ------------------------------------------------------------------
        internal static void DumpPoint(XmlTextWriter writer, string tagName, Point point)
        {
            writer.WriteStartElement(tagName);
            writer.WriteAttributeString("Left", point.X.ToString("F", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Top",  point.Y.ToString("F", CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump size.
        // ------------------------------------------------------------------
        internal static void DumpSize(XmlTextWriter writer, string tagName, Size size)
        {
            writer.WriteStartElement(tagName);
            writer.WriteAttributeString("Width",  size.Width.ToString ("F", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Height", size.Height.ToString("F", CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump rectangle.
        // ------------------------------------------------------------------
        internal static void DumpRect(XmlTextWriter writer, string tagName, Rect rect)
        {
            writer.WriteStartElement(tagName);
            writer.WriteAttributeString("Left",   rect.Left.ToString  ("F", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Top",    rect.Top.ToString   ("F", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Width",  rect.Width.ToString ("F", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Height", rect.Height.ToString("F", CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Walk visual children and find children UIElements. Performs deep 
        // walk of visual tree, but stops when runs into a UIElement.
        // All found UIElements are appended to uiElements collection.
        //
        //      visual - Visual element from which UIElements are extracted.
        //      uiElements - collection of UIElements.
        // ------------------------------------------------------------------
        internal static void GetUIElementsFromVisual(Visual visual, List<UIElement> uiElements)
        {
            int count = VisualTreeHelper.GetChildrenCount(visual);
            
            for(int i = 0; i < count; i++)
            {
                Visual child = visual.InternalGetVisualChild(i);
                if (child is UIElement)
                {
                    uiElements.Add((UIElement)(child));
                }
                else
                {
                    GetUIElementsFromVisual(child, uiElements);
                }
            }
        }

        #endregion Basic UIElement/Visual Dump

        // ------------------------------------------------------------------
        //
        //  Custom Dump Handlers
        //
        // ------------------------------------------------------------------

        #region Custom Dump Handlers

        // ------------------------------------------------------------------
        // Constructor.
        // ------------------------------------------------------------------
        static LayoutDump()
        {
            AddUIElementDumpHandler(typeof(System.Windows.Controls.TextBlock), new DumpCustomUIElement(DumpText));
            AddUIElementDumpHandler(typeof(FlowDocumentScrollViewer), new DumpCustomUIElement(DumpFlowDocumentScrollViewer));
            AddUIElementDumpHandler(typeof(FlowDocumentView), new DumpCustomUIElement(DumpFlowDocumentView));
            AddUIElementDumpHandler(typeof(DocumentPageView), new DumpCustomUIElement(DumpDocumentPageView));
            AddDocumentPageDumpHandler(typeof(FlowDocumentPage), new DumpCustomDocumentPage(DumpFlowDocumentPage));
        }

        // ------------------------------------------------------------------
        // Dump DocumentPageView specific data.
        // ------------------------------------------------------------------
        private static bool DumpDocumentPageView(XmlTextWriter writer, UIElement element, bool uiElementsOnly)
        {
            DocumentPageView dpv = element as DocumentPageView;
            Debug.Assert(dpv != null, "Dump function has to match element type.");

            // Dump text range
            if (dpv.DocumentPage != null)
            {
                DumpDocumentPage(writer, dpv.DocumentPage, element);
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Dump Text specific data.
        // ------------------------------------------------------------------
        private static bool DumpText(XmlTextWriter writer, UIElement element, bool uiElementsOnly)
        {
            System.Windows.Controls.TextBlock text = element as System.Windows.Controls.TextBlock;
            Debug.Assert(text != null, "Dump function has to match element type.");

            // Dump text range
            if (text.HasComplexContent)
            {
                DumpTextRange(writer, text.ContentStart, text.ContentEnd);
            }
            else
            {
                DumpTextRange(writer, text.Text);
            }

            // Dump baseline info
            writer.WriteStartElement("Metrics");
            writer.WriteAttributeString("BaselineOffset", ((double)text.GetValue(TextBlock.BaselineOffsetProperty)).ToString("F", CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            // Dump line array
            if (text.IsLayoutDataValid)
            {
                ReadOnlyCollection<LineResult> lines = text.GetLineResults();
                DumpLineResults(writer, lines, element);
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Dump FlowDocumentScrollViewer specific data.
        // ------------------------------------------------------------------
        private static bool DumpFlowDocumentScrollViewer(XmlTextWriter writer, UIElement element, bool uiElementsOnly)
        {
            FlowDocumentScrollViewer fdsv = element as FlowDocumentScrollViewer;
            Debug.Assert(fdsv != null, "Dump function has to match element type.");

            bool childrenHandled = false;
            if (fdsv.HorizontalScrollBarVisibility == ScrollBarVisibility.Hidden && fdsv.VerticalScrollBarVisibility == ScrollBarVisibility.Hidden)
            {
                FlowDocumentView fdv = null;
                if (fdsv.ScrollViewer != null)
                {
                    fdv = fdsv.ScrollViewer.Content as FlowDocumentView;
                    if (fdv != null)
                    {
                        DumpUIElement(writer, fdv, fdsv, uiElementsOnly);
                        childrenHandled = true;
                    }
                }
            }

            return childrenHandled;
        }

        // ------------------------------------------------------------------
        // Dump FlowDocumentView specific data.
        // ------------------------------------------------------------------
        private static bool DumpFlowDocumentView(XmlTextWriter writer, UIElement element, bool uiElementsOnly)
        {
            FlowDocumentView fdView = element as FlowDocumentView;
            Debug.Assert(fdView != null, "Dump function has to match element type.");

            // Dump scrolling information
            IScrollInfo isi = (IScrollInfo)fdView;
            if (isi.ScrollOwner != null)
            {
                Size extent = new Size(isi.ExtentWidth, isi.ExtentHeight);
                if (DoubleUtil.AreClose(extent, element.DesiredSize))
                {
                    DumpSize(writer, "Extent", extent);
                }
                Point offset = new Point(isi.HorizontalOffset, isi.VerticalOffset);
                if (!DoubleUtil.IsZero(offset.X) || !DoubleUtil.IsZero(offset.Y))
                {
                    DumpPoint(writer, "Offset", offset);
                }
            }

            FlowDocumentPage documentPage = fdView.Document.BottomlessFormatter.DocumentPage;

            // Dump transform relative to its parent
            GeneralTransform gt = documentPage.Visual.TransformToAncestor(fdView);
            Point point = new Point(0, 0);
            gt.TryTransform(point, out point);
            if (!DoubleUtil.IsZero(point.X) && !DoubleUtil.IsZero(point.Y))
            {
                DumpPoint(writer, "PagePosition", point);
            }

            DumpFlowDocumentPage(writer, documentPage);

            return false;
        }

        // ------------------------------------------------------------------
        // Dump FlowDocumentPage specific data.
        // ------------------------------------------------------------------
        private static void DumpFlowDocumentPage(XmlTextWriter writer, DocumentPage page)
        {
            FlowDocumentPage flowDocumentPage = page as FlowDocumentPage;
            Debug.Assert(flowDocumentPage != null, "Dump function has to match page type.");

            // Dump private info.
            writer.WriteStartElement("FormattedLines");
            writer.WriteAttributeString("Count", flowDocumentPage.FormattedLinesCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            // Dump columns collection
            TextDocumentView tv = (TextDocumentView)((IServiceProvider)flowDocumentPage).GetService(typeof(ITextView));
            if (tv.IsValid)
            {
                DumpColumnResults(writer, tv.Columns, page.Visual);
            }
        }

        // ------------------------------------------------------------------
        // Dump TextRange.
        // ------------------------------------------------------------------
        private static void DumpTextRange(XmlTextWriter writer, string content)
        {
            int cpStart = 0;
            int cpEnd = content.Length;

            writer.WriteStartElement("TextRange");
            writer.WriteAttributeString("Start", cpStart.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Length", (cpEnd - cpStart).ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump TextRange.
        // ------------------------------------------------------------------
        private static void DumpTextRange(XmlTextWriter writer, ITextPointer start, ITextPointer end)
        {
            int cpStart = start.TextContainer.Start.GetOffsetToPosition(start);
            int cpEnd = end.TextContainer.Start.GetOffsetToPosition(end);

            writer.WriteStartElement("TextRange");
            writer.WriteAttributeString("Start", cpStart.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Length", (cpEnd - cpStart).ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump line range.
        // ------------------------------------------------------------------
        private static void DumpLineRange(XmlTextWriter writer, int cpStart, int cpEnd, int cpContentEnd, int cpEllipses)
        {
            writer.WriteStartElement("TextRange");
            writer.WriteAttributeString("Start", cpStart.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Length", (cpEnd - cpStart).ToString(CultureInfo.InvariantCulture));
            if (cpEnd != cpContentEnd)
            {
                writer.WriteAttributeString("HiddenLength", (cpEnd - cpContentEnd).ToString(CultureInfo.InvariantCulture));
            }
            if (cpEnd != cpEllipses)
            {
                writer.WriteAttributeString("EllipsesLength", (cpEnd - cpEllipses).ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump line array.
        // ------------------------------------------------------------------
        private static void DumpLineResults(XmlTextWriter writer, ReadOnlyCollection<LineResult> lines, Visual visualParent)
        {
            if (lines != null)
            {
                // Dump line array
                writer.WriteStartElement("Lines");
                writer.WriteAttributeString("Count", lines.Count.ToString(CultureInfo.InvariantCulture));

                for (int index = 0; index < lines.Count; index++)
                {
                    writer.WriteStartElement("Line");

                    // Dump line info
                    LineResult line = lines[index];
                    DumpRect(writer, "LayoutBox", line.LayoutBox);
                    DumpLineRange(writer, line.StartPositionCP, line.EndPositionCP, line.GetContentEndPositionCP(), line.GetEllipsesPositionCP());

                    /*
                    // Dump inline objects
                    ReadOnlyCollection<UIElement> inlineObjects = line.InlineObjects;
                    if (inlineObjects != null)
                    {
                        // All inline UIElements are dumped as Children collection of UIElement 
                        // that invokes this dump method. Hence, do not dump UIElements here.
                        writer.WriteStartElement("InlineObjects");
                        writer.WriteAttributeString("Count", inlineObjects.Count.ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                    */

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        #endregion Custom Dump Handlers

        // ------------------------------------------------------------------
        // Dump paragraphs collection.
        // ------------------------------------------------------------------
        private static void DumpParagraphResults(XmlTextWriter writer, string tagName, ReadOnlyCollection<ParagraphResult> paragraphs, Visual visualParent)
        {
            if (paragraphs != null)
            {
                // Dump paragraphs array
                writer.WriteStartElement(tagName);
                writer.WriteAttributeString("Count", paragraphs.Count.ToString(CultureInfo.InvariantCulture));

                for (int index = 0; index < paragraphs.Count; index++)
                {
                    ParagraphResult paragraph = paragraphs[index];

                    if (paragraph is TextParagraphResult)
                    {
                        DumpTextParagraphResult(writer, (TextParagraphResult)paragraph, visualParent);
                    }
                    else if (paragraph is ContainerParagraphResult)
                    {
                        DumpContainerParagraphResult(writer, (ContainerParagraphResult)paragraph, visualParent);
                    }
                    else if (paragraph is TableParagraphResult)
                    {
                        DumpTableParagraphResult(writer, (TableParagraphResult)paragraph, visualParent);
                    }
                    else if (paragraph is FloaterParagraphResult)
                    {
                        DumpFloaterParagraphResult(writer, (FloaterParagraphResult)paragraph, visualParent);
                    }
                    else if (paragraph is UIElementParagraphResult)
                    {
                        DumpUIElementParagraphResult(writer, (UIElementParagraphResult)paragraph, visualParent);
                    }
                    else if (paragraph is FigureParagraphResult)
                    {
                        DumpFigureParagraphResult(writer, (FigureParagraphResult)paragraph, visualParent);
                    }
                    else if (paragraph is SubpageParagraphResult)
                    {
                        DumpSubpageParagraphResult(writer, (SubpageParagraphResult)paragraph, visualParent);
                    }
                }
                writer.WriteEndElement();
            }
        }

        // ------------------------------------------------------------------
        // Dump text paragraph result.
        // ------------------------------------------------------------------
        private static void DumpTextParagraphResult(XmlTextWriter writer, TextParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("TextParagraph");

            // Dump paragraph info
            writer.WriteStartElement("Element");
            writer.WriteAttributeString("Type", paragraph.Element.GetType().FullName);
            writer.WriteEndElement();
            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);
            DumpTextRange(writer, paragraph.StartPosition, paragraph.EndPosition);
            DumpLineResults(writer, paragraph.Lines, visual);

            DumpParagraphResults(writer, "Floaters", paragraph.Floaters, visual);
            DumpParagraphResults(writer, "Figures", paragraph.Figures, visual);

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump container paragraph result.
        // ------------------------------------------------------------------
        private static void DumpContainerParagraphResult(XmlTextWriter writer, ContainerParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("ContainerParagraph");

            // Dump paragraph info
            writer.WriteStartElement("Element");
            writer.WriteAttributeString("Type", paragraph.Element.GetType().FullName);
            writer.WriteEndElement();
            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);
            DumpParagraphResults(writer, "Paragraphs", paragraph.Paragraphs, visual);

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump floater paragraph result.
        // ------------------------------------------------------------------
        private static void DumpFloaterParagraphResult(XmlTextWriter writer, FloaterParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("Floater");

            // Dump paragraph info
            writer.WriteStartElement("Element");
            writer.WriteAttributeString("Type", paragraph.Element.GetType().FullName);
            writer.WriteEndElement();
            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);
            DumpColumnResults(writer, paragraph.Columns, visual);

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump UIElement paragraph result.
        // ------------------------------------------------------------------
        private static void DumpUIElementParagraphResult(XmlTextWriter writer, UIElementParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("BlockUIContainer");

            // Dump paragraph info
            writer.WriteStartElement("Element");
            writer.WriteAttributeString("Type", paragraph.Element.GetType().FullName);
            writer.WriteEndElement();
            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump figure paragraph result.
        // ------------------------------------------------------------------
        private static void DumpFigureParagraphResult(XmlTextWriter writer, FigureParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("Figure");

            // Dump paragraph info
            writer.WriteStartElement("Element");
            writer.WriteAttributeString("Type", paragraph.Element.GetType().FullName);
            writer.WriteEndElement();
            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);
            DumpColumnResults(writer, paragraph.Columns, visual);

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump table paragraph result.
        // ------------------------------------------------------------------
        private static void DumpTableParagraphResult(XmlTextWriter writer, TableParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("TableParagraph");

            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);

            ReadOnlyCollection<ParagraphResult> rowParagraphs = paragraph.Paragraphs;

            int count1 = VisualTreeHelper.GetChildrenCount(visual);
            for(int i = 0; i < count1; i++)
            {
                Visual rowVisual = visual.InternalGetVisualChild(i);
                int count2 = VisualTreeHelper.GetChildrenCount(rowVisual);

                ReadOnlyCollection<ParagraphResult> cellParagraphs = ((RowParagraphResult)rowParagraphs[i]).CellParagraphs;
                for(int j = 0; j < count2; j++)
                {
                    Visual cellVisual = rowVisual.InternalGetVisualChild(j);
                    DumpTableCell(writer, cellParagraphs[j], cellVisual, visual);
                }
            }           

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump subpage paragraph result.
        // ------------------------------------------------------------------
        private static void DumpSubpageParagraphResult(XmlTextWriter writer, SubpageParagraphResult paragraph, Visual visualParent)
        {
            writer.WriteStartElement("SubpageParagraph");

            // Dump paragraph info
            writer.WriteStartElement("Element");
            writer.WriteAttributeString("Type", paragraph.Element.GetType().FullName);
            writer.WriteEndElement();
            DumpRect(writer, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(writer, paragraph, visualParent);
            DumpColumnResults(writer, paragraph.Columns, visual);

            writer.WriteEndElement();
        }

        // ------------------------------------------------------------------
        // Dump column results.
        // ------------------------------------------------------------------
        private static void DumpColumnResults(XmlTextWriter writer, ReadOnlyCollection<ColumnResult> columns, Visual visualParent)
        {
            if (columns != null)
            {
                writer.WriteStartElement("Columns");
                writer.WriteAttributeString("Count", columns.Count.ToString(CultureInfo.InvariantCulture));

                for (int index = 0; index < columns.Count; index++)
                {
                    writer.WriteStartElement("Column");

                    // Dump column info
                    ColumnResult column = columns[index];
                    DumpRect(writer, "LayoutBox", column.LayoutBox);
                    DumpTextRange(writer, column.StartPosition, column.EndPosition);
                    DumpParagraphResults(writer, "Paragraphs", column.Paragraphs, visualParent);

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        // ------------------------------------------------------------------
        // Dump paragraph offset.
        // ------------------------------------------------------------------
        private static Visual DumpParagraphOffset(XmlTextWriter writer, ParagraphResult paragraph, Visual visualParent)
        {
            Type paragraphResultType = paragraph.GetType();
            System.Reflection.FieldInfo field = paragraphResultType.GetField("_paraClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object paraClient = field.GetValue(paragraph);
            Type paraClientType = paraClient.GetType();
            System.Reflection.PropertyInfo prop = paraClientType.GetProperty("Visual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Visual visual = (Visual)prop.GetValue(paraClient, null);

            // Dump transform relative to its parent
            if(visualParent.IsAncestorOf(visual))
            {
                GeneralTransform g = visual.TransformToAncestor(visualParent);
                Point point = new Point(0.0f, 0.0f);
                g.TryTransform(point, out point);

                if (point.X != 0 || point.Y != 0)
                {
                    DumpPoint(writer, "Origin", point);
                }
            }

            return visual;
        }

        // ------------------------------------------------------------------
        // Dump Table specific data: ColumnCount.
        // ------------------------------------------------------------------
        private static void DumpTableCalculatedMetrics(XmlTextWriter writer, object element)
        {
            Type type = typeof(Table);
            System.Reflection.PropertyInfo prop = type.GetProperty("ColumnCount");

            if (prop != null)
            {
                int count = (int)prop.GetValue(element, null);
                writer.WriteStartElement("ColumnCount");
                writer.WriteAttributeString("Count", count.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
        }

        // ------------------------------------------------------------------
        // Dump Cell specific data.
        // ------------------------------------------------------------------
        private static void DumpTableCell(XmlTextWriter writer, ParagraphResult paragraph, Visual cellVisual, Visual tableVisual)
        {
            Type paragraphResultType = paragraph.GetType();
            System.Reflection.FieldInfo fieldOfParaClient = paragraphResultType.GetField("_paraClient", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (fieldOfParaClient == null)
            {
                return;
            }

            CellParaClient cellParaClient = (CellParaClient) fieldOfParaClient.GetValue(paragraph);
            CellParagraph cellParagraph = cellParaClient.CellParagraph;
            TableCell cell = cellParagraph.Cell;

            writer.WriteStartElement("Cell");

            Type typeOfCell = cell.GetType();

            System.Reflection.PropertyInfo propOfColumnIndex = typeOfCell.GetProperty("ColumnIndex", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
            if (propOfColumnIndex != null)
            {
                int columnIndex = (int)propOfColumnIndex.GetValue(cell, null);
                writer.WriteAttributeString("ColumnIndex", columnIndex.ToString(CultureInfo.InvariantCulture));
            }

            System.Reflection.PropertyInfo propOfRowIndex = typeOfCell.GetProperty("RowIndex", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
            if (propOfRowIndex != null)
            {
                int rowIndex = (int)propOfRowIndex.GetValue(cell, null);
                writer.WriteAttributeString("RowIndex", rowIndex.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteAttributeString("ColumnSpan", cell.ColumnSpan.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("RowSpan", cell.RowSpan.ToString(CultureInfo.InvariantCulture));

            Rect rect = cellParaClient.Rect.FromTextDpi();
            DumpRect(writer, "LayoutBox", rect);

            bool hasTextContent;
            DumpParagraphResults(writer, "Paragraphs", cellParaClient.GetColumnResults(out hasTextContent)[0].Paragraphs, cellParaClient.Visual);

            writer.WriteEndElement();
        }
    }
}


