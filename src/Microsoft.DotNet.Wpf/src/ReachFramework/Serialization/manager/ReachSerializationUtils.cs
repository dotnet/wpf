// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Printing;

using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    internal struct DependencyPropertyList
    {
        public
        DependencyPropertyList(
            int capacity
            )
        {
            List = new System.Windows.DependencyProperty[capacity];
            Count = 0;
        }


        //
        // Non-lock-required Read methods
        // (Always safe to call when locking "safe" write operations are used)
        //

        public System.Windows.DependencyProperty[] List;
        public int Count;

        public
        void
        EnsureIndex(
            int index
            )
        {
            int delta = (index + 1) - Count;
            if (delta > 0)
            {
                Add(delta);
            }
        }

        public
        bool
        IsValidIndex(
            int index
            )
        {
            return (index < Count);
        }

        public
        int
        IndexOf(
            System.Windows.DependencyProperty value
            )
        {
            int index = -1;

            for (int i = 0; i < Count; i++)
            {
                if (List[i].Equals(value))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        public
        bool
        Contains(
            System.Windows.DependencyProperty value
            )
        {
            return (IndexOf(value) != -1);
        }


        //
        // Lock-required Write operations
        // "Safe" methods for Reader lock-free operation
        //

        //
        // Increase size by one, new value is provided
        //
        public
        void
        Add(
            System.Windows.DependencyProperty item
            )
        {
            // Add without Count adjustment (incr Count after valid item added)
            int index = Add(1, false);
            List[index] = item;
            Count++;
        }

        //
        // Increase size by one, new value is provided
        //
        public
        void
        Add(
            ref System.Windows.DependencyProperty item
            )
        {
            // Add without Count adjustment (incr Count after valid item added)
            int index = Add(1, false);
            List[index] = item;
            Count++;
        }

        //
        // Increase size by one, new value is default value
        //
        public
        int
        Add(
            )
        {
            return Add(1, true);
        }

        //
        // Increase size of array by delta, fill with default values
        //
        public
        int
        Add(
            int delta
            )
        {
            return Add(delta, true);
        }

        //
        // Increase size of array by delta, fill with default values
        // Allow disable of automatic Count increment so that cases where
        // non-default values are to be added to the list can be done before
        // count is changed. This is important for non-locking scenarios
        // (i.e. count is adjusted after array size changes)
        //
        private
        int
        Add(
            int delta,
            bool incrCount
            )
        {
            if (List != null)
            {
                if ((Count + delta) > List.Length)
                {
                    System.Windows.DependencyProperty[] newList = new System.Windows.DependencyProperty[Math.Max(List.Length * 2, Count + delta)];
                    List.CopyTo(newList, 0);
                    List = newList;
                }
            }
            else
            {
                List = new System.Windows.DependencyProperty[Math.Max(delta, 2)];
            }

            // New arrays auto-initialized to default entry values
            // Any resued entried have already been cleared out by Remove or Clear

            int index = Count;

            // Optional adjustment of Count
            if (incrCount)
            {
                // Adjust count after resize so that array bounds and data
                // are never invalid (good for locking writes without synchronized reads)
                Count += delta;
            }

            return index;
        }

        public
        void
        Sort(
            )
        {
            if (List != null)
            {
                Array.Sort(List, 0, Count);
            }
        }

        public
        void
        AppendTo(
            ref DependencyPropertyList destinationList
            )
        {
            for (int i = 0; i < Count; i++)
            {
                destinationList.Add(ref List[i]);
            }
        }

        public
        System.Windows.DependencyProperty[]
        ToArray(
            )
        {
            System.Windows.DependencyProperty[] array = new System.Windows.DependencyProperty[Count];
            Array.Copy(List, 0, array, 0, Count);

            return array;
        }


        //
        // Lock-required Write operations
        // "UNSafe" methods for Reader lock-free operation
        //
        // If any of these methods are called, the entire class is considered
        // unsafe for Reader lock-free operation for that point on (meaning
        // Reader locks must be taken)
        //

        public
        void
        Clear(
            )
        {
            // Return now unused entries back to default
            Array.Clear(List, 0, Count);

            Count = 0;
        }

        public
        void
        Remove(
            System.Windows.DependencyProperty value
            )
        {
            int index = IndexOf(value);
            if (index != -1)
            {
                // Shift entries down
                Array.Copy(List, index + 1, List, index, (Count - index - 1));

                // Return now unused entries back to default
                Array.Clear(List, Count - 1, 1);

                Count--;
            }
        }
    };

    internal class ReachHierarchySimulator
    {

        #region Constructor

        public
        ReachHierarchySimulator(
            PackageSerializationManager   manager,
            Object                        serializedObject
            )
        {
            if(manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if(serializedObject == null)
            {
                throw new ArgumentNullException("manager");
            }

            this._serializationManager      = manager;
            this._serializedObject          = serializedObject;
            this._documentSequenceXmlWriter = null;
            this._documentXmlWriter         = null;
            this._pageXmlWriter             = null;
        }

        #endregion Constructor

        #region Internal Methods

        internal
        void
        BeginConfirmToXPSStructure(
            bool mode
            )
        {
            //
            // In case we have a FixedPage or a Visual as a root, we have to simulate
            // a containing FixedDocument-FixedPage ... etc.
            //
            if((_serializedObject.GetType() == typeof(System.Windows.Documents.FixedDocument)) ||
               (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(_serializedObject.GetType())
                && (_serializedObject.GetType() != typeof(System.Windows.Documents.FixedDocumentSequence))))
            {
                //
                // Build the necessary wrapper for a FixedDocument
                //
                _documentSequenceXmlWriter = SimulateBeginFixedDocumentSequence();
            }
            else if(_serializedObject.GetType() == typeof(System.Windows.Documents.FixedPage))
            {
                //
                // Build the necessary wrapper for a FixedPage
                //
                _documentSequenceXmlWriter = SimulateBeginFixedDocumentSequence();
                _documentXmlWriter         = SimulateBeginFixedDocument();
                XpsSerializationPrintTicketRequiredEventArgs e =
                    new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                             0);
                ((IXpsSerializationManager)_serializationManager).OnXPSSerializationPrintTicketRequired(e);
                XpsOMSerializationManager xpsOMSerializationManager = _serializationManager as XpsOMSerializationManager;

                PrintTicket printTicket = null;
                if( e.Modified )
                {
                    printTicket =  e.PrintTicket;
                }
                Toolbox.Layout(_serializedObject as FixedPage, printTicket);

                ((IXpsSerializationManager)_serializationManager).FixedPagePrintTicket = printTicket;



            }
            else if(typeof(System.Windows.Media.Visual).IsAssignableFrom(_serializedObject.GetType()))
            {
                _documentSequenceXmlWriter = SimulateBeginFixedDocumentSequence();
                _documentXmlWriter         = SimulateBeginFixedDocument();
                if(!mode)
                {
                    _pageXmlWriter             = SimulateBeginFixedPage();
                }
            }
        }


        internal
        void
        EndConfirmToXPSStructure(
            bool mode
            )
        {
            if((_serializedObject.GetType() == typeof(System.Windows.Documents.FixedDocument)) ||
               (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(_serializedObject.GetType())
                && (_serializedObject.GetType() != typeof(System.Windows.Documents.FixedDocumentSequence))))
            {
                SimulateEndFixedDocumentSequence(_documentSequenceXmlWriter);
            }
            else if(_serializedObject.GetType() == typeof(System.Windows.Documents.FixedPage))
            {
                SimulateEndFixedDocument(_documentXmlWriter);
                SimulateEndFixedDocumentSequence(_documentSequenceXmlWriter);
            }
            else if(typeof(System.Windows.Media.Visual).IsAssignableFrom(_serializedObject.GetType()))
            {
                if(!mode)
                {
                    SimulateEndFixedPage(_pageXmlWriter);
                }
                SimulateEndFixedDocument(_documentXmlWriter);
                SimulateEndFixedDocumentSequence(_documentSequenceXmlWriter);
            }
        }

        internal
        virtual
        XmlWriter
        SimulateBeginFixedDocumentSequence(
            )
        {
            XmlWriter xmlWriter = null;

            if( _serializationManager is XpsSerializationManager)
            {
                (_serializationManager as XpsSerializationManager).RegisterDocumentSequenceStart();
            }

            //
            // Get the correct namespace
            //
            String xmlnsForType = _serializationManager.GetXmlNSForType(typeof(FixedDocumentSequence));
            //
            // Pick the correct XmlWriter
            //
            xmlWriter = _serializationManager.AcquireXmlWriter(typeof(FixedDocumentSequence));
            //
            // Write the start element and the namespace
            //
            if(xmlnsForType == null)
            {
                xmlWriter.WriteStartElement(XpsS0Markup.FixedDocumentSequence);
            }
            else
            {
                xmlWriter.WriteStartElement(XpsS0Markup.FixedDocumentSequence,
                                            xmlnsForType);
            }
            //
            // Pick the data for the PrintTicket if it existed
            //
            XpsSerializationPrintTicketRequiredEventArgs e =
            new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                             0);

            SimulatePrintTicketRaisingEvent(e);

            return xmlWriter;
        }

        internal
        virtual
        void
        SimulateEndFixedDocumentSequence(
            XmlWriter xmlWriter
            )
        {
            //
            // Write the End Element
            //
            xmlWriter.WriteEndElement();
            //
            // Release the Package writer
            //
            _serializationManager.ReleaseXmlWriter(typeof(FixedDocumentSequence));
            //
            // Inform any registered listener that the document sequence has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentSequenceWritingProgress,
                                                         0,
                                                         0,
                                                         null);
            if( _serializationManager is XpsSerializationManager)
            {
               (_serializationManager as XpsSerializationManager).RegisterDocumentSequenceEnd();
            }
            (_serializationManager as IXpsSerializationManager)?.OnXPSSerializationProgressChanged(progressEvent);
        }


        internal
        virtual
        XmlWriter
        SimulateBeginFixedDocument(
            )
        {
            XmlWriter   xmlWriter = null;
            if( _serializationManager is XpsSerializationManager)
            {
               (_serializationManager as XpsSerializationManager).RegisterDocumentStart();
            }
            //
            // Build the Image Table
            //
            _serializationManager.ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            _serializationManager.ResourcePolicy.ImageUriHashTable = new Dictionary<int, Uri>();

            //
            // Build the ColorContext Table
            //
            _serializationManager.ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();
            //
            // Get the correct namespace
            //
            String xmlnsForType = _serializationManager.GetXmlNSForType(typeof(FixedDocument));
            //
            // Pick the correct XmlWriter
            //
            xmlWriter = _serializationManager.AcquireXmlWriter(typeof(FixedDocument));
            //
            // Write the start element and the namespace
            //
            if(xmlnsForType == null)
            {
                xmlWriter.WriteStartElement("FixedDocument");
            }
            else
            {
                xmlWriter.WriteStartElement("FixedDocument",xmlnsForType);
            }

            XpsSerializationPrintTicketRequiredEventArgs e =
            new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                             0);

            SimulatePrintTicketRaisingEvent(e);

            return xmlWriter;
        }

        internal
        virtual
        void
        SimulateEndFixedDocument(
            XmlWriter xmlWriter
            )
        {
            //
            // Write the End Element
            //
            xmlWriter.WriteEndElement();
            //
            // Release the Package writer
            //
            _serializationManager.ReleaseXmlWriter(typeof(FixedDocument));
            //
            // Clear off the table from the packaging policy
            //
            _serializationManager.ResourcePolicy.ImageCrcTable = null;

            _serializationManager.ResourcePolicy.ImageUriHashTable = null;
            //
            // Clear off the table from the packaging policy
            //
            _serializationManager.ResourcePolicy.ColorContextTable = null;
            //
            // Inform any registered listener that the document has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                         0,
                                                         0,
                                                         null);
            if( _serializationManager is XpsSerializationManager)
            {
               (_serializationManager as XpsSerializationManager).RegisterDocumentEnd();
            }
            (_serializationManager as IXpsSerializationManager)?.OnXPSSerializationProgressChanged(progressEvent);
        }

        internal
        XmlWriter
        SimulateBeginFixedPage(
            )
        {
            XmlWriter   xmlWriter = null;
            (_serializationManager as IXpsSerializationManager)?.RegisterPageStart();
            //
            // Build the current Page Image Table
            //
            _serializationManager.ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Build the current Page ColorContext Table
            //
            _serializationManager.ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();
            //
            // Get the correct namespace
            //
            String xmlnsForType = _serializationManager.GetXmlNSForType(typeof(FixedPage));
            //
            // Pick the correct XmlWriter
            //
            xmlWriter = _serializationManager.AcquireXmlWriter(typeof(FixedPage));

            Visual visual = _serializedObject as Visual;
            if (visual != null)
            {
                _treeWalker = new ReachTreeWalker(xmlWriter,_serializationManager);
                _treeWalker.SerializeLinksInVisual(visual);
            }

            //
            // Write the start element and the namespace
            //
            if(xmlnsForType == null)
            {
                xmlWriter.WriteStartElement(XpsS0Markup.FixedPage);
            }
            else
            {
                xmlWriter.WriteStartElement(XpsS0Markup.FixedPage);

                xmlWriter.WriteAttributeString(XpsS0Markup.Xmlns, xmlnsForType);
                xmlWriter.WriteAttributeString(XpsS0Markup.XmlnsX, XpsS0Markup.XmlnsXSchema);
                xmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, XpsS0Markup.XmlLangValue);
            }

            //
            // Simulating the PrintTicket
            //
            XpsSerializationPrintTicketRequiredEventArgs e =
            new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                             0);

            SimulatePrintTicketRaisingEvent(e);

            //
            // Adding Width and Height being mandatory attributes on the FixedPage
            //
            Size size = SimulateFixedPageSize(visual, e.PrintTicket);

            ((IXpsSerializationManager)_serializationManager).FixedPageSize = size;

            xmlWriter.WriteAttributeString(XpsS0Markup.PageWidth,
                                           TypeDescriptor.GetConverter(size.Width).ConvertToInvariantString(size.Width));
            xmlWriter.WriteAttributeString(XpsS0Markup.PageHeight,
                                           TypeDescriptor.GetConverter(size.Height).ConvertToInvariantString(size.Height));

            return xmlWriter;
        }

        internal
        void
        SimulateEndFixedPage(
            XmlWriter  xmlWriter
            )
        {

            _serializationManager.PackagingPolicy.PreCommitCurrentPage();

            if (_treeWalker != null)
            {
                _treeWalker.CommitHyperlinks();
                _treeWalker = null;
            }

            xmlWriter.WriteEndElement();

            _serializationManager.ReleaseXmlWriter(typeof(FixedPage));
            //
            // Clear off the table from the packaging policy
            //
            _serializationManager.ResourcePolicy.CurrentPageImageTable = null;
            //
            // Clear off the table from the packaging policy
            //
            _serializationManager.ResourcePolicy.CurrentPageColorContextTable = null;
            (_serializationManager as IXpsSerializationManager)?.VisualSerializationService.ReleaseVisualTreeFlattener();
            //
            // Inform any registered listener that the page has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                         0,
                                                         0,
                                                         null);
            (_serializationManager as IXpsSerializationManager)?.OnXPSSerializationProgressChanged(progressEvent);
            (_serializationManager as IXpsSerializationManager)?.RegisterPageEnd();
        }

        #endregion Internal Methods

        #region Private Methods
        
        protected
        void
        SimulatePrintTicketRaisingEvent(
            XpsSerializationPrintTicketRequiredEventArgs e
            )
        {
            (_serializationManager as IXpsSerializationManager)?.OnXPSSerializationPrintTicketRequired(e);

            //
            // Serialize the data for the PrintTicket
            //
            if(e.Modified)
            {
                if(e.PrintTicket != null)
                {
                    PrintTicketSerializer serializer = new PrintTicketSerializer(_serializationManager);
                    serializer.SerializeObject(e.PrintTicket);
                }
            }

        }

              
        Size
        SimulateFixedPageSize(
            Visual visual,
            PrintTicket printTicket
            )
        {
            Size sz = new Size(0,0);

            //Try to cast the visual down to a UIElement so we can get the PreviousArrangeSize
            //
            UIElement element = visual as UIElement;
            if (element != null)
            {
                Rect rect = element.PreviousArrangeRect;
                sz = Toolbox.Layout(element, rect.Size, printTicket);
            }
            else
            {
                //If the visual is not a UIElement, call ValidateDocumentSize to assign defaults
                sz = Toolbox.ValidateDocumentSize(sz, printTicket);
            }

            return sz;
        }

        #endregion Private Methods

        #region Protected Data

        protected
        PackageSerializationManager   _serializationManager;

        #endregion Protected Data

        #region Private Data

        private
        Object                        _serializedObject;

        private
        XmlWriter                     _documentSequenceXmlWriter;

        private
        XmlWriter                     _documentXmlWriter;

        private
        XmlWriter                     _pageXmlWriter;

        private
        ReachTreeWalker             _treeWalker;

        #endregion Private Data
    };

    internal class ReachTreeWalker
    {
        #region Constructors

        internal
        ReachTreeWalker(
            ReachSerializer serializer
            )
        {
            _serializerXmlWriter = serializer.XmlWriter;
            _serializationManager = serializer.SerializationManager;
        }

        internal
        ReachTreeWalker(
            XmlWriter writer,
            PackageSerializationManager serializationManager)
        {
            _serializerXmlWriter = writer;
            _serializationManager = serializationManager;
        }
        #endregion

        #region Internal Methods
        internal void CommitHyperlinks()
        {
            //copy hyperlinks into stream
            if (_linkStream != null)
            {
                _linkStream.Close();
                XmlWriter.WriteRaw(_linkStream.ToString());
            }
        }

        internal void SerializeLinksInDocumentPage(DocumentPage page)
        {
            IContentHost contentHost = page as IContentHost;
            Visual root = page.Visual;

            if (contentHost != null)
            {
                SerializeLinksForIContentHost(contentHost, root);
            }
            else
            {
                // do logical tree
                SerializeLinksInLogicalTree(root, null, root);
            }

        }

        internal void SerializeLinksInFixedPage(FixedPage page)
        {
            System.Collections.IEnumerable enumerable = page.Children;
            foreach (object element in enumerable)
            {
                // Checks and sees if this element has an ID or is a Hyperlink
                if (element is IInputElement)
                {
                    SerializeLinkTargetForElement((IInputElement)element, null, page);
                }

                if (element is IContentHost)
                {
                    //recursively find links in its elements
                    SerializeLinksForIContentHost((IContentHost)element, page);
                }
                else if (element is FrameworkElement)
                {
                    // do logical tree
                    SerializeLinksInLogicalTree((DependencyObject)element, null, page);
                }
            }

        }

        internal void SerializeLinksInVisual(Visual visual)
        {
            // Checks and sees if this element has an ID or is a Hyperlink
            if (visual is IInputElement)
            {
                SerializeLinkTargetForElement((IInputElement)visual, null, visual);
            }

            IContentHost contentHost = visual as IContentHost;
            if (contentHost != null)
            {
                //recursively find links in its elements
                SerializeLinksForIContentHost(contentHost, visual);
            }
            else
            {
                // do logical tree
                SerializeLinksInLogicalTree(visual, null, visual);
            }
        }

        #endregion Internal Methods

        #region Private Methods
        private void SerializeLinksForIContentHost(IContentHost contentHost, Visual root)
        {
            System.Collections.Generic.IEnumerator<IInputElement> enumerator = contentHost.HostedElements;
            while (enumerator.MoveNext())
            {
                IInputElement element = enumerator.Current;

                // Checks and sees if this element has an ID or is a Hyperlink
                SerializeLinkTargetForElement(element, contentHost, root);

                if (element is IContentHost)
                {
                    //recursively find links in its elements
                    SerializeLinksForIContentHost((IContentHost)element, root);
                }
                else if (element is FrameworkElement)
                {
                    // do we do this for FrameworkContentElement?  We could...  But do we want to?
                    // do logical tree
                    SerializeLinksInLogicalTree((DependencyObject)element, contentHost, root);
                }
            }
        }

        private void SerializeLinksInLogicalTree(DependencyObject dependencyObject, IContentHost contentHost, Visual root)
        {
            System.Collections.IEnumerable enumerable = LogicalTreeHelper.GetChildren(dependencyObject);
            foreach (object element in enumerable)
            {
                // Checks and sees if this element has an ID or is a Hyperlink
                if (element is IInputElement)
                {
                    SerializeLinkTargetForElement((IInputElement)element, contentHost, root);
                }

                if (element is IContentHost)
                {
                    //recursively find links in its elements
                    SerializeLinksForIContentHost((IContentHost)element, root);
                }
                else if (element is FrameworkElement)
                {
                    // do we do this for FrameworkContentElement?  We could...  But do we want to?
                    // do logical tree
                    SerializeLinksInLogicalTree((DependencyObject)element, contentHost, root);
                }
            }
        }

        private void SerializeLinkTargetForElement(IInputElement element, IContentHost contentHost, Visual root)
        {
            if (element is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)element;
                string id = fe.Name;
                if (!String.IsNullOrEmpty(id))
                {
                    // No need to add overlapping path element for named FE element.
                    AddLinkTarget(id);
                }
            }
            else if (element is FrameworkContentElement && contentHost != null)
            {
                FrameworkContentElement fce = (FrameworkContentElement)element;
                string id = fce.Name;
                Uri uri = null;

                if (element is Hyperlink)
                {
                    uri = ((Hyperlink)element).NavigateUri;
                }

                if (!String.IsNullOrEmpty(id) || uri != null)
                {
                    Transform transform = Transform.Identity;
                    Visual contentVisual = contentHost as Visual;
                    if (contentVisual != null && root != null && root.IsAncestorOf(contentVisual))
                    {
                        GeneralTransform t = contentVisual.TransformToAncestor(root);

                        if (t is Transform)
                        {
                            transform = (Transform)t;
                        }
                    }

                    Transform rootTransform = root.VisualTransform;
                    if (rootTransform != null)
                    {
                        transform = new MatrixTransform(Matrix.Multiply(transform.Value, rootTransform.Value));
                    }
                    transform = new MatrixTransform(
                        Matrix.Multiply(transform.Value,
                            new TranslateTransform(root.VisualOffset.X, root.VisualOffset.Y).Value
                            )
                        );


                    PathGeometry geometry = new PathGeometry();
                    System.Collections.ObjectModel.ReadOnlyCollection<Rect> rectangles = contentHost.GetRectangles(fce);
                    foreach (Rect rect in rectangles)
                    {
                        geometry.AddGeometry(new RectangleGeometry(rect, 0, 0, transform));
                    }
                    SerializeHyperlink(geometry, id, uri);
                }
            }
        }

        private bool IsFragment(Uri uri)
        {
            return uri.OriginalString.StartsWith(FRAGMENTMARKER,StringComparison.Ordinal);
        }

        private void SerializeHyperlink(PathGeometry geometry, String id, Uri navigateUri)
        {
            String nameForType = "Path";
            XmlWriter writer = LinkXmlWriter;

            bool useID = false;
            if (!String.IsNullOrEmpty(id))
            {
                useID = AddLinkTarget(id);
            }

            if (!useID && navigateUri == null)
            {
                // don't write out this path
                return;
            }

            writer.WriteStartElement(nameForType);

            if (navigateUri != null)
            {
                if (IsFragment(navigateUri))
                {
                    XpsPackagingPolicy policy = SerializationManager.PackagingPolicy as XpsPackagingPolicy;
                    XpsOMPackagingPolicy omPolicy = SerializationManager.PackagingPolicy as XpsOMPackagingPolicy;
                    if (policy == null && omPolicy == null)
                    {
                        throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPackagingPolicy));
                    }

                    Uri documentUri = SerializationManager.PackagingPolicy.CurrentFixedDocumentUri;
                    Uri pageUri = SerializationManager.PackagingPolicy.CurrentFixedPageUri;

                    Uri relativeUri = PackUriHelper.GetRelativeUri(pageUri, documentUri);
                    string documentFragmentUri = relativeUri.OriginalString + navigateUri.OriginalString;
                    WriteAttribute(writer, "FixedPage.NavigateUri", documentFragmentUri);
                }
                else
                {
                    WriteAttribute(writer, "FixedPage.NavigateUri", navigateUri);
                }
                WriteAttribute(writer, "Fill", "#00000000");
            }
            else
            {
                WriteAttribute(writer, "Opacity", "0");
            }

            if (useID)
            {
                WriteAttribute(writer, "Name", id);
            }

            geometry.FillRule = FillRule.Nonzero;

            Size pageSize = ((IXpsSerializationManager)_serializationManager).FixedPageSize;
            VisualTreeFlattener.WritePath(writer, geometry, pageSize);

            writer.WriteEndElement();
        }

        private void WriteAttribute(XmlWriter writer, string name, object value)
        {
            writer.WriteAttributeString(name, TypeDescriptor.GetConverter(value).ConvertToInvariantString(value));
        }

        private bool AddLinkTarget(string name)
        {
            if (LinkTargetList != null && !LinkTargetList.Contains(name))
            {
                LinkTargetList.Add(name);
                return true;
            }
            return false;
        }
        #endregion Private Methods

        #region Private Properties
        private PackageSerializationManager SerializationManager
        {
            get
            {
                return _serializationManager;
            }
        }
        /// <summary>
        ///
        /// </summary>
        private
        XmlWriter
        XmlWriter
        {
            get
            {
                return _serializerXmlWriter;
            }
        }

        /// <summary>
        ///
        /// </summary>
        private
        XmlWriter
        LinkXmlWriter
        {
            get
            {
                if (_linkXmlWriter == null)
                {
                    _linkStream = new StringWriter(CultureInfo.InvariantCulture);
                    _linkXmlWriter = new XmlTextWriter(_linkStream);
                }

                return _linkXmlWriter;
            }
        }

        /// <summary>
        ///
        /// </summary>
        private
        IList<String>
        LinkTargetList
        {
            get
            {
                if (_linkTargetList == null)
                {
                    IXpsSerializationManager rsm = SerializationManager as IXpsSerializationManager;
                    if (rsm != null)
                    {
                        XmlWriter writer = this.XmlWriter; // guarantee page is created
                        _linkTargetList = SerializationManager.PackagingPolicy.AcquireStreamForLinkTargets();
                    }
                }

                return _linkTargetList;
            }
        }
        #endregion Private Properties

        #region Private Fields
        private IList<String>  _linkTargetList;
        private XmlWriter _linkXmlWriter;
        private StringWriter _linkStream;
        private XmlWriter _serializerXmlWriter;
        private PackageSerializationManager _serializationManager;
        private const string FRAGMENTMARKER = "#";
        #endregion
    };


    internal static class DoubleOperations
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct NanUnion
        {
            [FieldOffset(0)] internal double DoubleValue;
            [FieldOffset(0)] internal UInt64 UintValue;
        }

        internal
        static
        bool
        IsNaN(
            double value
            )
        {
            NanUnion t = new NanUnion();
            t.DoubleValue = value;

            UInt64 exp = t.UintValue & 0xfff0000000000000;
            UInt64 man = t.UintValue & 0x000fffffffffffff;

            return (exp == 0x7ff0000000000000 || exp == 0xfff0000000000000) && (man != 0);
        }

    }

    internal static class Toolbox
    {
        internal static void EmitEvent(EventTrace.Event evt)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Level.Info, evt);
        }

        //
        // Layout -
        // Gets the size of the FixedPage and delegates the task of Laying out the UIElement to the overloaded
        // Layout method.  If the size changes, we assign the new size to FixedPage so the properties are serialized
        // correctly.
        //
        internal
        static
        void
        Layout(
            FixedPage fixedPage,
            PrintTicket printTicket
        )
        {
            Size size = new Size(fixedPage.Width, fixedPage.Height);
            Size newSize = Layout(fixedPage, size, printTicket);

            if (size != newSize)
            {
                fixedPage.Width = newSize.Width;
                fixedPage.Height = newSize.Height;
            }
        }


        //
        // Layout - Calls ValidateDocumentSize to validate the document size or to assign defaults.
        // Lays out the visual if the size changes or if the visual has not been layed out yet.
        //
        internal
        static
        Size
        Layout(
            UIElement uiElement,
            Size elementSize,
            PrintTicket printTicket
            )
        {
            Size newSize = ValidateDocumentSize(elementSize, printTicket);

            if (uiElement.IsArrangeValid == false ||
                uiElement.IsMeasureValid == false ||
                elementSize != newSize
                )
            {
                EmitEvent(EventTrace.Event.WClientDRXLayoutBegin);

                uiElement.Measure(newSize);
                uiElement.Arrange(new Rect(new Point(), newSize));
                uiElement.UpdateLayout();

                EmitEvent(EventTrace.Event.WClientDRXLayoutEnd);
            }

            return newSize;
        }


        //
        // ValidateDocumentSize -
        // Checks to see if the document size is valid.  If not, first try to see if the desired
        // size can be determined by the printTicket.  If that fails, assign default fixedpage size
        // as last resort.
        //
        internal
        static
        Size
        ValidateDocumentSize(Size documentSize, PrintTicket printTicket)
        {
            if (documentSize.Width == 0 ||
               documentSize.Height == 0 ||
               DoubleOperations.IsNaN(documentSize.Width) ||
               DoubleOperations.IsNaN(documentSize.Height) ||
               Double.IsPositiveInfinity(documentSize.Width) ||
               Double.IsPositiveInfinity(documentSize.Height) ||
               Double.IsNegativeInfinity(documentSize.Width) ||
               Double.IsNegativeInfinity(documentSize.Height)
               )
            {
                Size sz = new Size(0, 0);

                //if print ticket definied, use printTicket dimensions
                if (printTicket != null &&
                    printTicket.PageMediaSize != null &&
                    printTicket.PageMediaSize.Width.HasValue &&
                    printTicket.PageMediaSize.Height.HasValue)
                {
                    // Swap the height and width if needed to obtain a landscape orientation.
                    if (printTicket.PageOrientation == PageOrientation.Landscape
                        || printTicket.PageOrientation == PageOrientation.ReverseLandscape)
                    {
                        sz.Width = printTicket.PageMediaSize.Height.Value;
                        sz.Height = printTicket.PageMediaSize.Width.Value;
                    }
                    else
                    {
                        sz.Width = printTicket.PageMediaSize.Width.Value;
                        sz.Height = printTicket.PageMediaSize.Height.Value;
                    }
                }
                else
                {
                    sz.Width = 96 * 8.5;
                    sz.Height = 96 * 11;
                }
                return sz;
            }

            return documentSize;
        }


        internal static DocumentPage GetPage(DocumentPaginator paginator, int index)
        {
            EmitEvent(EventTrace.Event.WClientDRXGetPageBegin);

            DocumentPage page = paginator.GetPage(index);

            EmitEvent(EventTrace.Event.WClientDRXGetPageEnd);

            return page;
        }

        internal static FixedPage GetPageRoot(object page)
        {
            FixedPage fixedPage = ((PageContent) page).GetPageRoot(false) as FixedPage;

            return fixedPage;
        }
    }
}
