// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Text;
using System.Threading; 
using System.Windows.Threading;
using Microsoft.Test.Layout;
using System.Reflection;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Layout.PropertyDump
{
    // ----------------------------------------------------------------------
    // FrameworkElements specific dumping functions.
    // ----------------------------------------------------------------------
    internal sealed class FrameworkElements
    {
        private static PropertyDumpCore core;

        // ------------------------------------------------------------------
        // Initialize framework elements dumpers.
        // ------------------------------------------------------------------
        internal static void Init(PropertyDumpCore propDumpCore)
        {
            core = propDumpCore;
            // All work is done in the constructor, but Init call is required
            // to invoke it.
        }

        // ------------------------------------------------------------------
        // Static constructor.
        // ------------------------------------------------------------------
        static FrameworkElements()
        {
            PropertyDumpCore.AddUIElementDumpHandler(typeof(TextBlock), new PropertyDumpCore.DumpCustomUIElement(DumpText));
            PropertyDumpCore.AddUIElementDumpHandler(typeof(FlowDocumentScrollViewer), new PropertyDumpCore.DumpCustomUIElement(DumpTextPanel));
            PropertyDumpCore.AddUIElementDumpHandler(typeof(DocumentPageView), new PropertyDumpCore.DumpCustomUIElement(DumpDocumentPageView));

            PropertyDumpCore.AddUIElementDumpHandler(typeof(Viewbox), new PropertyDumpCore.DumpCustomUIElement(DumpViewbox));

            AddDocumentPageDumpHandler(ReflectionHelper.GetTypeFromName("MS.Internal.PtsHost.FlowDocumentPage"), new DumpCustomDocumentPage(DumpFlowDocumentPage));
        }

        /// <summary>
        /// Mapping from DocumentPage type to dump methdod (DumpCustomUIElement).
        /// </summary>
        internal delegate void DumpCustomDocumentPage(XmlNode writer, DocumentPage page);

        /// <summary>
        /// Add new mapping from DocumentPage type to dump methdod (DumpCustomDocumentPage).
        /// </summary>
        internal static void AddDocumentPageDumpHandler(Type type, DumpCustomDocumentPage dumper)
        {
            _documentPageToDumpHandler.Add(type, dumper);
        }

        // ------------------------------------------------------------------
        // Mapping from DocumentPage type to dump methdod (DumpCustomUIElement).
        // ------------------------------------------------------------------
        private static Hashtable _documentPageToDumpHandler = new Hashtable();

        // ------------------------------------------------------------------
        // Dump TextBlock element specific data.
        // ------------------------------------------------------------------
        public static void DumpText(XmlNode writer, UIElement element)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(element == null) {
                throw new ArgumentNullException("element");
            }
            
            TextBlock text = (TextBlock)element;

            // Dump text range
            DumpTextRange(writer, text.ContentStart, text.ContentEnd);

            // Dump baseline info
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("Metrics");
            writer.AppendChild(node);

            AppendAttribute(node, "BaselineOffset", text.BaselineOffset);

            // Dump line array
            TextParagraphViewW tpv = TextParagraphViewW.FromIServiceProvider((IServiceProvider)text);
            DumpLineResults(writer, tpv.Lines, element);
        }

        // ------------------------------------------------------------------
        // Dump FlowDocumentScrollViewer specific data.
        // ------------------------------------------------------------------
        static Type ITextViewType = ReflectionHelper.GetTypeFromName("System.Windows.Documents.ITextView");
        public static void DumpTextPanel(XmlNode writer, UIElement element)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(element == null) {
                throw new ArgumentNullException("element");
            }
            
            FlowDocumentScrollViewer fdsv = (FlowDocumentScrollViewer)element;
            
            /*
            DispatcherFrame frame = new DispatcherFrame(true);
            fdsv.Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle, 
                new DispatcherOperationCallback(
                    delegate { 
                        frame.Continue = false;
            */
                        ReflectionHelper dptv = ReflectionHelper.WrapObject(((IServiceProvider)fdsv).GetService(ITextViewType));
                        DumpDocumentPage(writer, (DocumentPage)dptv.GetField("_page"), (Visual)dptv.GetProperty("RenderScope"));
            /*
                        return null; 
                    }
                ),
                null
            );
            Dispatcher.PushFrame(frame);
            */
        }

        // ------------------------------------------------------------------
        // Dump DocumentPageView specific data.
        // ------------------------------------------------------------------
        private static void DumpDocumentPageView(XmlNode writer, UIElement element)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(element == null) {
                throw new ArgumentNullException("element");
            }
            
            DocumentPageView dpv = (DocumentPageView)element;

            DumpDocumentPage(writer, dpv.DocumentPage, element);
        }

        // ------------------------------------------------------------------
        // Dump content of the specified DocumentPage.
        //
        //      writer - stream to be used for dump
        //      element - element to be dumped
        //      parent - parent element
        // ------------------------------------------------------------------
        internal static void DumpDocumentPage(XmlNode writer, DocumentPage page, Visual parent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(page == null) {
                throw new ArgumentNullException("page");
            }
            
            if(parent == null) {
                throw new ArgumentNullException("parent");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("DocumentPage");
            writer.AppendChild(node);

            AppendAttribute(node, "Type", page.GetType().FullName);

            if (page == DocumentPage.Missing) {
                AppendAttribute(node, "Missing", "True");
            }
            else
            {
                DumpSize(writer, "Size", page.Size);

                // Dump transform relative to its parent
                Matrix m;
                System.Windows.Media.GeneralTransform gt  = page.Visual.TransformToAncestor(parent); 
                System.Windows.Media.Transform t = (System.Windows.Media.Transform)gt;
                if(t==null)
                {
	                throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
                }
                m = t.Value;
                Point point = new Point(0, 0) * m;
                if (point.X != 0 || point.Y != 0)
                {
                    DumpPoint(writer, "Position", point);
                }


                //check for registered handler for this document page
                DumpCustomDocumentPage dumpDocumentPage = _documentPageToDumpHandler[page.GetType()] as DumpCustomDocumentPage;

                //Hack: need to itentify if test code should be using the Debug Class
                //Debug.Assert(dumpDocumentPage == null, String.Format("Unknown documentpage of type {0}", page.GetType()));
                if (dumpDocumentPage != null)
                {
                    dumpDocumentPage(node, page);
                }
            }
        }

        // ------------------------------------------------------------------
        // Dump FlowDocumentPage specific data.
        // ------------------------------------------------------------------
        private static void DumpFlowDocumentPage(XmlNode writer, DocumentPage page)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(page == null) {
                throw new ArgumentNullException("page");
            }
            
            FlowDocumentPageW flowDocumentPage = new FlowDocumentPageW(page);

            // Dump columns collection
            TextDocumentViewW tv = TextDocumentViewW.FromIServiceProvider((IServiceProvider)(flowDocumentPage.InnerObject));
            DumpTextDocumentView(writer, tv, page.Visual);
        }

        // ------------------------------------------------------------------
        // Dump paragraph results array.
        // ------------------------------------------------------------------
        private static void DumpParagraphResults(XmlNode writer, ParagraphResultListW paragraphs, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            if (paragraphs != null)
            {
                // Dump paragraphs array
                XmlNode node = PropertyDumpCore.xmldoc.CreateElement("Paragraphs");
                writer.AppendChild(node);

                XmlAttribute attr = PropertyDumpCore.xmldoc.CreateAttribute("Count");
                node.Attributes.Append(attr);

                int paragraphCount = 0;
                foreach (ParagraphResultW paragraph in paragraphs)
                {
                    paragraphCount++;

                    if (paragraph is TextParagraphResultW)
                    {
                        DumpTextParagraphResult(node, (TextParagraphResultW)paragraph, visualParent);
                    }
                    else if (paragraph is ContainerParagraphResultW)
                    {
                        DumpContainerParagraphResult(node, (ContainerParagraphResultW)paragraph, visualParent);
                    }
                    else if (paragraph is UIElementParagraphResultW)
                    {
                        DumpUIElementParagraphResult(node, (UIElementParagraphResultW)paragraph, visualParent);
                    }
                    else if (paragraph is TableParagraphResultW)
                    {
                        DumpTableParagraphResult(node, (TableParagraphResultW)paragraph, visualParent);
                    }
                    else if (paragraph is RowParagraphResultW)
                    {
                        DumpRowParagraphResult(node, (RowParagraphResultW)paragraph, visualParent);
                    }
                    else if (paragraph is FloaterParagraphResultW)
                    {
                        DumpFloaterParagraphResult(node, (FloaterParagraphResultW)paragraph, visualParent);
                    }
                    else if (paragraph is FigureParagraphResultW)
                    {
                        DumpFigureParagraphResult(node, (FigureParagraphResultW)paragraph, visualParent);
                    }
                    else if(paragraph is SubpageParagraphResultW)
                    {
                        DumpSubpageParagraphResult(node, (SubpageParagraphResultW)paragraph, visualParent);
                    }
                    else
                    {
                        //Hack: need to itentify if test code should be using the Debug Class
                        /*Debug.Assert
                        (
                            false,
                            String.Format
                            (
                                "Dont know how to dump ParagraphResult wrapper of type {0} wrapping object of type {1}",
                                paragraph.GetType(),
                                paragraph.InnerObject.GetType()
                            )
                        );*/
                    }
                }

                attr.Value = paragraphCount.ToString(CultureInfo.InvariantCulture);
            }
        }

        // ------------------------------------------------------------------
        // Dump text paragraph result.
        // ------------------------------------------------------------------
        private static void DumpTextParagraphResult(XmlNode writer, TextParagraphResultW paragraph, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(paragraph == null) {
                throw new ArgumentNullException("paragraph");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("TextParagraph");
            writer.AppendChild(node);

            DumpRect(node, "LayoutBox", paragraph.LayoutBox);
            DumpTextRange(node, paragraph.StartPosition, paragraph.EndPosition);
            DumpLineResults(node, paragraph.Lines, visualParent);            
            
            ParagraphResultListW floatedObjects = paragraph.Floaters;                                    
            if (floatedObjects != null)
            {                
                DumpParagraphResults(writer, floatedObjects, visualParent);                
            }

            floatedObjects = paragraph.Figures;
            if (floatedObjects != null)
            {
                DumpParagraphResults(writer, floatedObjects, visualParent);
            }
        }
        // ------------------------------------------------------------------
        // Dump container paragraph result.
        // ------------------------------------------------------------------
        private static void DumpContainerParagraphResult(XmlNode writer, ContainerParagraphResultW paragraph, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(paragraph == null) {
                throw new ArgumentNullException("paragraph");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("ContainerParagraph");
            writer.AppendChild(node);
            // Dump paragraph info
            DumpRect(node, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(node, paragraph, visualParent);
            DumpParagraphResults(node, paragraph.Paragraphs, visual);
        }

        // ------------------------------------------------------------------
        // Dump UIElement paragraph result.
        // ------------------------------------------------------------------
        private static void DumpUIElementParagraphResult(XmlNode writer, UIElementParagraphResultW paragraph, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(paragraph == null) {
                throw new ArgumentNullException("paragraph");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("UIElementParagraph");
            writer.AppendChild(node);
            // Dump paragraph info

            DumpRect(node, "LayoutBox", paragraph.LayoutBox);
            DumpParagraphOffset(node, paragraph, visualParent);

            XmlNode node1 = PropertyDumpCore.xmldoc.CreateElement("ElementOwner");
            node.AppendChild(node1);
            AppendAttribute(node1, "Type", paragraph.Element.GetType());            

            FrameworkElements.core.InternalXmlDump(paragraph.Element, node1);            
        }

        // ------------------------------------------------------------------
        // Dump table paragraph result.
        // ------------------------------------------------------------------
        private static void DumpTableParagraphResult(XmlNode writer, TableParagraphResultW paragraph, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(paragraph == null) {
                throw new ArgumentNullException("paragraph");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("TableParagraphResult");
            writer.AppendChild(node);

            DumpRect(node, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(node, paragraph, visualParent);

            //Dump Paragraph results for Table content
            DumpParagraphResults(node, paragraph.Paragraphs, visual);          
        }

        // ------------------------------------------------------------------
        // Dump table row paragraph result.
        // ------------------------------------------------------------------
        private static void DumpRowParagraphResult(XmlNode writer, RowParagraphResultW paragraph, Visual visualParent)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (paragraph == null)
            {
                throw new ArgumentNullException("paragraph");
            }

            if (visualParent == null)
            {
                throw new ArgumentNullException("visualParent");
            }

            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("RowParagraphResult");
            writer.AppendChild(node);

            DumpRect(node, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(node, paragraph, visualParent);

            //Dump ParagraphResults for TableRow           
            DumpParagraphResults(node, paragraph.CellParagraphs, visual);           
        }
       
        // ------------------------------------------------------------------
        // Dump subpage paragraph result.
        // ------------------------------------------------------------------
        private static void DumpSubpageParagraphResult(XmlNode writer, SubpageParagraphResultW paragraph, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(paragraph == null) {
                throw new ArgumentNullException("paragraph");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }

            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("SubpageParagraph");
            writer.AppendChild(node);

            DumpRect(node, "LayoutBox", paragraph.LayoutBox);
            Visual visual = DumpParagraphOffset(node, paragraph, visualParent);
            DumpColumnResults(node, paragraph.Columns, visual);
        }

        // ------------------------------------------------------------------
        // Dump paragraph offset.
        // ------------------------------------------------------------------
        private static Visual DumpParagraphOffset(XmlNode writer, ParagraphResultW paragraph, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(paragraph == null) {
                throw new ArgumentNullException("paragraph");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            object paraClient = paragraph.GetField("_paraClient");
            Type paraClientType = paraClient.GetType();
            //PropertyInfoSW prop = TypeSW.Wrap(paraClientType).GetProperty("Visual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            PropertyInfo prop = paraClientType.GetProperty("Visual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Visual visual = (Visual)prop.GetValue(paraClient, null);

            // Dump transform relative to its parent
            Matrix m;
            System.Windows.Media.GeneralTransform gt  = visual.TransformToAncestor(visualParent);
            System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
            if(t==null)
            {
	            //throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
                GlobalLog.LogEvidence(new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change"));
            }
            m = t.Value;
            Point point = new Point(0.0f, 0.0f) * m;

            if (point.X != 0 || point.Y != 0)
            {
                DumpPoint(writer, "Origin", point);
            }

            return visual;
        }

        // ------------------------------------------------------------------
        // Dump column results.
        // ------------------------------------------------------------------
        private static void DumpColumnResults(XmlNode writer, ColumnResultListW columns, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            if (columns != null)
            {
                XmlNode node = PropertyDumpCore.xmldoc.CreateElement("Columns");
                writer.AppendChild(node);

                XmlAttribute attr = PropertyDumpCore.xmldoc.CreateAttribute("Count");
                node.Attributes.Append(attr);
                int columnCount = 0;
                foreach (ColumnResultW column in columns)
                {
                    columnCount++;
                    XmlNode node1 = PropertyDumpCore.xmldoc.CreateElement("Column");
                    node.AppendChild(node1);
                    // Dump column info
                    DumpRect(node1, "LayoutBox", column.LayoutBox);
                    DumpTextRange(node1, column.StartPosition, column.EndPosition);
                    DumpParagraphResults(node1, column.Paragraphs, visualParent);
                }
                attr.Value = columnCount.ToString(CultureInfo.InvariantCulture);
            }
        }

        // ------------------------------------------------------------------
        // Dump line array.
        // ------------------------------------------------------------------
        private static void DumpLineResults(XmlNode writer, LineResultListW lines, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(lines == null) {
                throw new ArgumentNullException("lines");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            // Dump line array
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("Lines");
            writer.AppendChild(node);

            XmlAttribute attr = PropertyDumpCore.xmldoc.CreateAttribute("Count");
            node.Attributes.Append(attr);
            int lineCount = 0;
            foreach (LineResultW line in lines)
            {
                lineCount++;
                XmlNode node1 = PropertyDumpCore.xmldoc.CreateElement("Line");
                node.AppendChild(node1);
                // Dump line info
                DumpRect(node1, "LayoutBox", line.LayoutBox);
                DumpLineRange(node1, line.StartPosition, line.EndPosition, line.GetContentEndPosition(), line.GetEllipsesPosition());
            }
            attr.Value = lineCount.ToString(CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------
        // Dump line range.
        // ------------------------------------------------------------------
        private static void DumpLineRange(XmlNode writer, TextPointer start, TextPointer end, TextPointer contentEndPosition, TextPointer ellipsesPosition)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(start == null) {
                throw new ArgumentNullException("start");
            }
            
            if(end == null) {
                throw new ArgumentNullException("end");
            }
            
            TextPointer containerStart = null;
            TextPointer containerEnd = null;
            GetContainerRange(start, out containerStart, out containerEnd) ;

            int cpStart = containerStart.GetOffsetToPosition(start);
            int cpEnd = containerStart.GetOffsetToPosition(end);
            int cpContentEnd = containerStart.GetOffsetToPosition(contentEndPosition);

            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("TextRange");
            writer.AppendChild(node);
            AppendAttribute(node, "Start", cpStart);
            AppendAttribute(node, "Length", cpEnd - cpStart);

            if (cpEnd != cpContentEnd)
            {
                AppendAttribute(node, "HiddenLength", cpEnd - cpContentEnd);
            }
            if (ellipsesPosition != null)
            {
                int cpEllipses = containerStart.GetOffsetToPosition(ellipsesPosition);
                AppendAttribute(node, "EllipsesLength", cpEnd - cpEllipses);
            }
        }

        // ------------------------------------------------------------------
        // Dump TextRange.
        // ------------------------------------------------------------------
        private static void DumpTextRange(XmlNode writer, TextPointer start, TextPointer end)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(start == null) {
                throw new ArgumentNullException("start");
            }
            
            if(end == null) {
                throw new ArgumentNullException("end");
            }
            
            int cpStart;
            int cpEnd;
            TextPointer containerStart = null;
            TextPointer containerEnd = null;

            GetContainerRange(start, out containerStart, out containerEnd) ;

            cpStart = containerStart.GetOffsetToPosition(start);
            cpEnd = containerStart.GetOffsetToPosition(end);

            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("TextRange");
            writer.AppendChild(node);

            AppendAttribute(node, "Start", cpStart);
            AppendAttribute(node, "Length", cpEnd - cpStart);
        }

        public static void DumpTextDocumentView(XmlNode writer, TextDocumentViewW tdv, Visual visualParent)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(tdv == null) {
                throw new ArgumentNullException("tdv");
            }
            
            if(visualParent == null) {
                throw new ArgumentNullException("visualParent");
            }
            
            if (tdv.IsValid)
            {
                DumpColumnResults(writer, tdv.Columns, visualParent);
            }
        }

        // ------------------------------------------------------------------
        // Dump point.
        // ------------------------------------------------------------------
        internal static void DumpPoint(XmlNode writer, string tagName, Point point)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(tagName == null) {
                throw new ArgumentNullException("tagName");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement(tagName);
            writer.AppendChild(node);

            AppendAttribute(node, "Left", point.X);
            AppendAttribute(node, "Top", point.Y);
        }

        // ------------------------------------------------------------------
        // Dump size.
        // ------------------------------------------------------------------
        internal static void DumpSize(XmlNode writer, string tagName, Size size)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(tagName == null) {
                throw new ArgumentNullException("tagName");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement(tagName);
            writer.AppendChild(node);

            AppendAttribute(node, "Width", size.Width);
            AppendAttribute(node, "Height", size.Height);
        }

        // ------------------------------------------------------------------
        // Dump rectangle.
        // ------------------------------------------------------------------
        internal static void DumpRect(XmlNode writer, string tagName, Rect rect)
        {
            if(writer == null) {
                throw new ArgumentNullException("writer");
            }
            
            if(tagName == null) {
                throw new ArgumentNullException("tagName");
            }
            
            XmlNode node = PropertyDumpCore.xmldoc.CreateElement(tagName);
            writer.AppendChild(node);

            AppendAttribute(node, "Left", rect.Left);
            AppendAttribute(node, "Top", rect.Top);
            AppendAttribute(node, "Width", rect.Width);
            AppendAttribute(node, "Height", rect.Height);
        }

        private static void GetContainerRange(TextPointer tp, out TextPointer start, out TextPointer end)
        {
            if(tp == null) {
                throw new ArgumentNullException("tp");
            }
            
            DependencyObject element = GetContainer(tp);

            if (element is TextBlock)
            {
                start = ((TextBlock)element).ContentStart;
                end = ((TextBlock)element).ContentEnd;
            }
            else if (element is FlowDocumentScrollViewer)
            {
                start = ((FlowDocumentScrollViewer)element).Document.ContentStart;
                end = ((FlowDocumentScrollViewer)element).Document.ContentEnd;
            }
            else if (element is FlowDocument)
            {
                start = ((FlowDocument)element).ContentStart;
                end = ((FlowDocument)element).ContentEnd;
            }
            else if (element is RichTextBox)
            {
                start = ((RichTextBox)element).Document.ContentStart;
                end = ((RichTextBox)element).Document.ContentEnd;
            }
            else
            {
                throw new InvalidOperationException("Only Text, TextPanel, FlowDocument and RichTextBox are supported");
            }
        }

        private static DependencyObject GetContainer(TextPointer pointer)
        {
            if(pointer == null) {
                throw new ArgumentNullException("pointer");
            }
            
            DependencyObject obj = pointer.Parent;

            while (obj is TextElement)
            {
               obj = ((TextElement)obj).Parent;
            }

            //Hack: need to itentify if test code should be using the Debug Class
            //Debug.Assert(obj != null, "Unable to obtain the container for given Textpointer");

            return obj;
        }

        public static void AppendAttribute(XmlNode node, string attrName, string attrValue)
        {
            if(node == null) {
                throw new ArgumentNullException("node");
            }
            
            if(attrName == null) {
                throw new ArgumentNullException("attrName");
            }
            
            if(attrValue == null) {
                throw new ArgumentNullException("attrValue");
            }
            
            XmlAttribute attr = PropertyDumpCore.xmldoc.CreateAttribute(attrName);
            attr.Value = attrValue;
            node.Attributes.Append(attr);
        }

        public static void AppendAttribute(XmlNode node, string attrName, double attrValue)
        {
            if(node == null) {
                throw new ArgumentNullException("node");
            }
            
            if(attrName == null) {
                throw new ArgumentNullException("attrName");
            }
            
            AppendAttribute(node, attrName, attrValue.ToString("F", CultureInfo.InvariantCulture));
        }

        public static void AppendAttribute(XmlNode node, string attrName, float attrValue)
        {
            if(node == null) {
                throw new ArgumentNullException("node");
            }
            
            if(attrName == null) {
                throw new ArgumentNullException("attrName");
            }
            
            AppendAttribute(node, attrName, attrValue.ToString("F", CultureInfo.InvariantCulture));
        }

        public static void AppendAttribute(XmlNode node, string attrName, int attrValue)
        {
            if(node == null) {
                throw new ArgumentNullException("node");
            }
            
            if(attrName == null) {
                throw new ArgumentNullException("attrName");
            }
            
            AppendAttribute(node, attrName, attrValue.ToString(CultureInfo.InvariantCulture));
        }

        public static void AppendAttribute(XmlNode node, string attrName, Type attrValue)
        {
            if(node == null) {
                throw new ArgumentNullException("node");
            }
            
            if(attrName == null) {
                throw new ArgumentNullException("attrName");
            }
            
            if(attrValue == null) {
                throw new ArgumentNullException("attrValue");
            }
            
            AppendAttribute(node, attrName, attrValue.ToString());
        }

        public static void AppendAttribute(XmlNode node, string attrName, object attrValue)
        {
            if(node == null) {
                throw new ArgumentNullException("node");
            }
            
            if(attrName == null) {
                throw new ArgumentNullException("attrName");
            }
            
            if(attrValue == null) {
                throw new ArgumentNullException("attrValue");
            }
            
            Console.WriteLine("******!!!!!!!!PANIC!!!!!!!!!!!********* dumping attribute of type {0} not handled", attrValue.GetType());
            AppendAttribute(node, attrName, attrValue.ToString());
        }

        // ------------------------------------------------------------------
        // Dump figure paragraph result.
        // ------------------------------------------------------------------
        private static void DumpFigureParagraphResult(XmlNode writer, FigureParagraphResultW paragraph, Visual visualParent)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (paragraph == null)
            {
                throw new ArgumentNullException("paragraph");
            }

            if (visualParent == null)
            {
                throw new ArgumentNullException("visualParent");
            }

            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("FigureParagraph");
            writer.AppendChild(node);
            // Dump LayoutBox info
            DumpRect(node, "LayoutBox", paragraph.LayoutBox);

            //Dump ColumnResults for Figure 
            object paraClient = paragraph.GetField("_paraClient");
            Type paraClientType = paraClient.GetType();
            PropertyInfo prop = paraClientType.GetProperty("Visual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Visual visual = (Visual)prop.GetValue(paraClient, null);
            DumpColumnResults(node, paragraph.Columns, visual);
        }

        // ------------------------------------------------------------------
        // Dump floater paragraph result.
        // ------------------------------------------------------------------
        private static void DumpFloaterParagraphResult(XmlNode writer, FloaterParagraphResultW paragraph, Visual visualParent)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (paragraph == null)
            {
                throw new ArgumentNullException("paragraph");
            }

            if (visualParent == null)
            {
                throw new ArgumentNullException("visualParent");
            }

            XmlNode node = PropertyDumpCore.xmldoc.CreateElement("FloaterParagraph");
            writer.AppendChild(node);
            // Dump LayoutBox info
            DumpRect(node, "LayoutBox", paragraph.LayoutBox);

            //Dump ColumnResults for Floater 
            object paraClient = paragraph.GetField("_paraClient");
            Type paraClientType = paraClient.GetType();
            PropertyInfo prop = paraClientType.GetProperty("Visual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Visual visual = (Visual)prop.GetValue(paraClient, null);
            DumpColumnResults(node, paragraph.Columns, visual);
        }
        
        /// <summary>
        /// Dump because Viwebox first child is ContainerVisual not UIElement.  So, get the child of Viewbox and dump that...
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="element"></param>
        public static void DumpViewbox(XmlNode writer, UIElement element)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            Viewbox viewbox = (Viewbox)element;

            if(viewbox.Child != null)
                FrameworkElements.core.InternalXmlDump(viewbox.Child, writer);
        }

    }
}

