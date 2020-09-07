// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implementation of XmlDataProvider object.
//
// Specs:       Avalon DataProviders.mht
//              XmlDataSource.mht
//

using System;
using System.IO;                    // Stream
using System.Collections;
using System.ComponentModel;        // ISupportInitialize, AsyncCompletedEventHandler, [DesignerSerialization*], [DefaultValue]
using System.Diagnostics;
using System.IO.Packaging;          // PackUriHelper
using System.Globalization;         // CultureInfo
using System.Net;                   // WebRequest, IWebRequestCreate
using System.Threading;             // ThreadPool, WaitCallback
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;     // IXmlSerializable
using System.Xml.XPath;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;     // Dispatcher*
using System.Windows.Markup; // IUriContext, [XamlDesignerSerializer]
using MS.Internal;                  // CriticalExceptions
using MS.Internal.Utility;          // BindUriHelper
using MS.Internal.Data;             // XmlDataCollection

namespace System.Windows.Data
{
    /// <summary>
    /// XmlDataProvider class, gets XmlNodes to use as source in data binding
    /// </summary>
    /// <ExternalAPI/>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    [ContentProperty("XmlSerializer")]
    public class XmlDataProvider : DataSourceProvider, IUriContext
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Instantiates a new instance of a XmlDataProvider
        /// </summary>
        public XmlDataProvider()
        {
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Source property, indicated the Uri of the source Xml data file, if this
        /// property is set, then any inline xml data is discarded.
        /// </summary>
        /// <remarks>
        /// Setting this property will implicitly cause this DataProvider to refresh.
        /// When changing multiple refresh-causing properties, the use of
        /// <seealso cref="DataSourceProvider.DeferRefresh"/> is recommended.
        /// </remarks>
        public Uri Source
        {
            get { return _source; }
            set
            {
                if ((_domSetDocument != null) || _source != value)
                {
                    _domSetDocument = null;
                    _source = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("Source"));

                    if (!IsRefreshDeferred)
                        Refresh();
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeSource()
        {
            return (_domSetDocument == null) && (_source != null);
        }

        /// <summary>
        /// Document Property, returns the current XmlDocument that this data source
        /// is using, if set the Source property is cleared and any inline xml data is
        /// discarded
        /// </summary>
        /// <remarks>
        /// Setting this property will implicitly cause this DataProvider to refresh.
        /// When changing multiple refresh-causing properties, the use of
        /// <seealso cref="DataSourceProvider.DeferRefresh"/> is recommended.
        /// </remarks>
        // this property cannot be serialized since the produced XAML/XML wouldn't be parseable anymore;
        // instead, a user-set DOM is serialized as InlineData
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public XmlDocument Document
        {
            get { return _document; }
            set
            {
                if ((_domSetDocument == null) || _source != null || _document != value)
                {
                    _domSetDocument = value;

                    _source = null;
                    OnPropertyChanged(new PropertyChangedEventArgs("Source"));

                    ChangeDocument(value); // set immediately so that next get_Document returns this value,
                                       // even when data provider is in deferred or asynch mode

                    if (!IsRefreshDeferred)
                        Refresh();
                }
            }
        }

        /// <summary>
        /// XPath property, the XPath query used for generating the DataCollection
        /// </summary>
        /// <remarks>
        /// Setting this property will implicitly cause this DataProvider to refresh.
        /// When changing multiple refresh-causing properties, the use of
        /// <seealso cref="DataSourceProvider.DeferRefresh"/> is recommended.
        /// </remarks>
        [DesignerSerializationOptions(DesignerSerializationOptions.SerializeAsAttribute)]
        public string XPath
        {
            get { return _xPath; }
            set
            {
                if (_xPath != value)
                {
                    _xPath = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("XPath"));

                    if (!IsRefreshDeferred)
                        Refresh();
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeXPath()
        {
            return (_xPath != null) && (_xPath.Length != 0);
        }

        /// <summary>
        /// XmlNamespaceManager property, XmlNamespaceManager used for executing XPath queries.
        /// in order to set this property using markup, an XmlDataNamespaceManager resource can be used.
        /// </summary>
        /// <remarks>
        /// Setting this property will implicitly cause this DataProvider to refresh.
        /// When changing multiple refresh-causing properties, the use of
        /// <seealso cref="DataSourceProvider.DeferRefresh"/> is recommended.
        /// </remarks>
        [DefaultValue(null)]
        public XmlNamespaceManager XmlNamespaceManager
        {
            get { return _nsMgr; }
            set
            {
                if (_nsMgr != value)
                {
                    _nsMgr = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("XmlNamespaceManager"));

                    if (!IsRefreshDeferred)
                        Refresh();
                }
            }
        }

        /// <summary>
        /// If true object creation will be performed in a worker
        /// thread, otherwise will be done in active context.
        /// </summary>
        [DefaultValue(true)]
        public bool IsAsynchronous
        {
            get { return _isAsynchronous; }
            set { _isAsynchronous = value; }
        }

        /// <summary>
        /// The content property for inline Xml data.
        /// Used by the parser to compile the literal content of the embedded XML island.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IXmlSerializable XmlSerializer
        {
            get
            {
                if (_xmlSerializer == null)
                {
                    _xmlSerializer = new XmlIslandSerializer(this);
                }
                return _xmlSerializer;
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeXmlSerializer()
        {
            // serialize InlineData only if the Xml DOM used was originally a inline Xml data island
            return (DocumentForSerialization != null);
        }

        #endregion


        #region IUriContext

        /// <summary>
        ///     Provides the base uri of the current context.
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get { return BaseUri; }
            set { BaseUri = value; }
        }

        /// <summary>
        ///     Implementation for BaseUri.
        /// </summary>
        protected virtual Uri BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        #endregion IUriContext

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Prepare loading either the external XML or inline XML and
        /// produce Xml node collection.
        /// Execution is either immediately or on a background thread, see IsAsynchronous.
        /// Called by base class from InitialLoad or Refresh
        /// </summary>
        protected override void BeginQuery()
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.ProviderQuery))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.BeginQuery(
                                        TraceData.Identify(this),
                                        IsAsynchronous ? "asynchronous" : "synchronous"));
            }

            if (_source != null)
            {
                // load DOM from external source
                Debug.Assert(_domSetDocument == null, "Should not be possible to be using Source and user-set Doc at the same time.");
                DiscardInline();
                LoadFromSource(); // will execute synch or asycnh, depends on IsAsynchronous
            }
            else
            {
                XmlDocument doc = null;
                if (_domSetDocument != null)
                {
                    DiscardInline();
                    doc = _domSetDocument;
                }
                else // inline doc
                {
                    // Did we already parse the inline DOM?
                    // Don't do this during EndInit - it duplicates effort of Parse
                    if (_inEndInit)
                        return;

                    doc = _savedDocument;
                }

                // Doesn't matter if the doc is set programmatically or from inline,
                // here we create a new collection for it and make it active.
                if (IsAsynchronous && doc != null)
                {
                    // process node collection on a worker thread ?
                    ThreadPool.QueueUserWorkItem(new WaitCallback(BuildNodeCollectionAsynch),
                                                 doc);
                }
                else if (doc != null || Data != null)
                {
                    // process the doc synchronously if we're in synchronous mode,
                    // or if the doc is empty.  But don't process an empty doc
                    // if the data is already null, to avoid unnecessary work
                    BuildNodeCollection(doc);
                }
            }
        }


        /// <summary>
        ///     Initialization of this element has completed;
        ///     this causes a Refresh if no other deferred refresh is outstanding
        /// </summary>
        protected override void EndInit()
        {
            // inhibit re-parsing of inline doc (from BeginQuery)
            try
            {
                _inEndInit = true;
                base.EndInit();
            }
            finally
            {
                _inEndInit = false;
            }
        }


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Preparing DOM

        // Load XML from a URI; this runs on caller thread of BeginQuery (Refresh/InitialLoad)
        private void LoadFromSource()
        {
            // convert the Source into an absolute URI
            Uri sourceUri = this.Source;
            if (sourceUri.IsAbsoluteUri == false)
            {
                Uri baseUri = (_baseUri != null) ? _baseUri : BindUriHelper.BaseUri;
                sourceUri = BindUriHelper.GetResolvedUri(baseUri, sourceUri);
            }

            // create a request to load the content
            // Ideally we would want to use RegisterPrefix and WebRequest.Create.
            // However, these two functions regress 700k working set in System.dll and System.xml.dll
            //  which is mostly for logging and config.
            // Call PackWebRequestFactory.CreateWebRequest to bypass the regression if possible
            //  by calling Create on PackWebRequest if uri is pack scheme
            WebRequest request = PackWebRequestFactory.CreateWebRequest(sourceUri);

            if (request == null)
            {
                throw new Exception(SR.Get(SRID.WebRequestCreationFailed));
            }

            // load it on a worker thread ?
            if (IsAsynchronous)
                ThreadPool.QueueUserWorkItem(new WaitCallback(CreateDocFromExternalSourceAsynch),
                                             request);
            else
                CreateDocFromExternalSource(request);
        }

        #region Content Parsing implementation

        private class XmlIslandSerializer : IXmlSerializable
        {
            internal XmlIslandSerializer(XmlDataProvider host)
            {
                _host = host;
            }
            public XmlSchema GetSchema()
            {
                // IXmlSerializable spec unclear what to return if no schema known
                return null;
            }

            public void WriteXml(XmlWriter writer)
            {
                XmlDocument doc = _host.DocumentForSerialization;
                if (doc != null)
                    doc.Save(writer);
            }

            public void ReadXml(XmlReader reader)
            {
                _host.ParseInline(reader);
            }

            private XmlDataProvider _host;
        }

        /// <summary>
        /// Parse method,
        /// </summary>
        /// <param name="xmlReader"></param>
        private void ParseInline(XmlReader xmlReader)
        {
            if ((_source == null) && (_domSetDocument == null) && _tryInlineDoc)
            {
                // load it on a worker thread ?
                if (IsAsynchronous)
                {
                    _waitForInlineDoc = new ManualResetEvent(false); // tells serializer to wait until _document is ready
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CreateDocFromInlineXmlAsync),
                                                     xmlReader);
                }
                else
                    CreateDocFromInlineXml(xmlReader);
            }
        }

        private XmlDocument DocumentForSerialization
        {
            get
            {
                // allow inline serialization only if the original XML data was inline
                // or the user has assigned own DOM to our Document property
                if (_tryInlineDoc || (_savedDocument != null) || (_domSetDocument != null))
                {
                    // if inline or assigned doc hasn't been parsed yet, wait for it
                    if (_waitForInlineDoc != null)
                        _waitForInlineDoc.WaitOne();
                    return _document;
                }
                return null;
            }
        }

        #endregion //Content Parsing implementation

        // this method can run on a worker thread!
        private void CreateDocFromInlineXmlAsync(object arg)
        {
            XmlReader xmlReader = (XmlReader) arg;
            CreateDocFromInlineXml(xmlReader);
        }

        // this method can run on a worker thread!
        private void CreateDocFromInlineXml(XmlReader xmlReader)
        {
            // Maybe things have changed and we don't want to use inline doc anymore
            if (!_tryInlineDoc)
            {
                _savedDocument = null;
                if (_waitForInlineDoc != null)
                    _waitForInlineDoc.Set();
                return;
            }

            XmlDocument doc;
            Exception ex = null;

            try
            {
                doc = new XmlDocument();
                try
                {
                    if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.XmlProvider))
                    {
                        TraceData.Trace(TraceEventType.Warning,
                                            TraceData.XmlLoadInline(
                                                TraceData.Identify(this),
                                                Dispatcher.CheckAccess() ? "synchronous" : "asynchronous"));
                    }

                    // Load the inline doc from the reader
                    doc.Load(xmlReader);
                }
                catch (XmlException xmle)
                {
                    if (TraceData.IsEnabled)
                        TraceData.Trace(TraceEventType.Error, TraceData.XmlDPInlineDocError, xmle);
                    ex = xmle;
                }

                if (ex == null)
                {
                    // Save a copy of the inline document to be used in future
                    // queries, and by serialization.
                    _savedDocument = (XmlDocument)doc.Clone();
                }
            }
            finally
            {
                xmlReader.Close();
                // Whether or not parsing was successful, unblock the serializer thread.

                // If serializer had to wait for the inline doc, it's available now.
                // If there was an error, null will be returned for DocumentForSerialization.
                if (_waitForInlineDoc != null)
                    _waitForInlineDoc.Set();
            }

            // warn the user if the default xmlns wasn't set explicitly (bug 1006946)
            if (TraceData.IsEnabled)
            {
                XmlNode root = doc.DocumentElement;
                if (root != null && root.NamespaceURI == xmlReader.LookupNamespace(String.Empty))
                {
                    TraceData.Trace(TraceEventType.Error, TraceData.XmlNamespaceNotSet);
                }
            }

            if (ex == null)
            {
                // Load succeeded.  Create the node collection.  (This calls
                // OnQueryFinished to reset the Document and Data properties).
                BuildNodeCollection(doc);
            }
            else
            {
                if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.ProviderQuery))
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.QueryFinished(
                                            TraceData.Identify(this),
                                            Dispatcher.CheckAccess() ? "synchronous" : "asynchronous",
                                            TraceData.Identify(null),
                                            TraceData.IdentifyException(ex)));
                }

                // Load failed.  Report the error, and reset
                // Data and Document properties to null.
                OnQueryFinished(null, ex, CompletedCallback, null);
            }
        }

        // this method can run on a worker thread!
        private void CreateDocFromExternalSourceAsynch(object arg)
        {
            WebRequest request = (WebRequest) arg;
            CreateDocFromExternalSource(request);
        }

        // this method can run on a worker thread!
        private void CreateDocFromExternalSource(WebRequest request)
        {
            bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.XmlProvider);

            XmlDocument doc = new XmlDocument();
            Exception ex = null;
            // request the content from the URI
            try
            {
                if (isExtendedTraceEnabled)
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.XmlLoadSource(
                                            TraceData.Identify(this),
                                            Dispatcher.CheckAccess() ? "synchronous" : "asynchronous",
                                            TraceData.Identify(request.RequestUri.ToString())));
                }

                WebResponse response = WpfWebRequestHelper.GetResponse(request);
                if (response == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.GetResponseFailed));
                }

                // Get Stream and content type from WebResponse.
                Stream stream = response.GetResponseStream();

                if (isExtendedTraceEnabled)
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.XmlLoadDoc(
                                            TraceData.Identify(this)));
                }

                // load the XML from the stream
                doc.Load(stream);
                stream.Close();
            }
            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e))
                {
                    throw;
                }
                ex = e;
                if (TraceData.IsEnabled)
                {
                    TraceData.Trace(TraceEventType.Error, TraceData.XmlDPAsyncDocError, Source, ex);
                }
            }
            //FXCop Fix: CatchNonClsCompliantExceptionsInGeneralHandlers
            catch
            {
                throw;
            }

            if (ex != null)
            {
                if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.ProviderQuery))
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.QueryFinished(
                                            TraceData.Identify(this),
                                            Dispatcher.CheckAccess() ? "synchronous" : "asynchronous",
                                            TraceData.Identify(null),
                                            TraceData.IdentifyException(ex)));
                }

                // we're done if we got an error up to this point
                // both .Data and .Document properties will be reset to null
                OnQueryFinished(null, ex, CompletedCallback, null);
                return;  // have an error, no processing of DOM
            }

            BuildNodeCollection(doc);
            // above method also calls OnQueryFinished to push new property values
        }


        // this method can run on a worker thread!
        private void BuildNodeCollectionAsynch(object arg)
        {
            XmlDocument doc = (XmlDocument) arg;
            BuildNodeCollection(doc);
        }


        // this method can run on a worker thread!
        private void BuildNodeCollection(XmlDocument doc)
        {
            XmlDataCollection collection = null;
            if (doc != null)
            {
                if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.XmlBuildCollection))
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.XmlBuildCollection(
                                            TraceData.Identify(this)));
                }

                XmlNodeList nodes = GetResultNodeList(doc);

                //we always create a new DataCollection
                collection = new XmlDataCollection(this);

                if (nodes != null)
                {
                    collection.SynchronizeCollection(nodes);
                }
            }

            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.ProviderQuery))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.QueryFinished(
                                        TraceData.Identify(this),
                                        Dispatcher.CheckAccess() ? "synchronous" : "asynchronous",
                                        TraceData.Identify(collection),
                                        TraceData.IdentifyException(null)));
            }

            OnQueryFinished(collection, null, CompletedCallback, doc);
        }

        // this callback will execute on the UI thread;
        // OnQueryFinished marshals back to UI thread if necessary
        private object OnCompletedCallback(object arg)
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.ProviderQuery))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.QueryResult(
                                        TraceData.Identify(this),
                                        TraceData.Identify(Data)));
            }

            ChangeDocument((XmlDocument) arg);
            return null;
        }

        // change Document property, and update event listeners accordingly
        private void ChangeDocument(XmlDocument doc)
        {
            if (_document != doc)
            {
                if (_document != null)
                    UnHook();

                _document = doc;

                if (_document != null)
                    Hook();

                OnPropertyChanged(new PropertyChangedEventArgs("Document"));
            }
        }

        #endregion Preparing DOM


        // Point of no return: do not ever try to use the inline XML again.
        private void DiscardInline()
        {
            _tryInlineDoc = false;
            _savedDocument = null;
            if (_waitForInlineDoc != null)
                _waitForInlineDoc.Set();
        }

        private void Hook()
        {
            if (!_isListening)
            {
                _document.NodeInserted += NodeChangeHandler;
                _document.NodeRemoved += NodeChangeHandler;
                _document.NodeChanged += NodeChangeHandler;
                _isListening = true;
            }
        }

        private void UnHook()
        {
            if (_isListening)
            {
                _document.NodeInserted -= NodeChangeHandler;
                _document.NodeRemoved -= NodeChangeHandler;
                _document.NodeChanged -= NodeChangeHandler;
                _isListening = false;
            }
        }

        private void OnNodeChanged(object sender, XmlNodeChangedEventArgs e)
        {
            if (XmlDataCollection == null)
                return;

            UnHook();

            XmlNodeList nodes = GetResultNodeList((XmlDocument) sender);

            // Compare the entire new list with the old,
            // and make all the necessary insert/remove changes.
            XmlDataCollection.SynchronizeCollection(nodes);

            Hook();
        }

        private XmlNodeList GetResultNodeList(XmlDocument doc)
        {
            Debug.Assert(doc != null);

            XmlNodeList nodes = null;
            if (doc.DocumentElement != null)
            {
                // if no XPath is specified, use the root node by default
                string xpath = (string.IsNullOrEmpty(XPath)) ? "/" : XPath;

                try
                {
                    if (XmlNamespaceManager != null)
                    {
                        nodes = doc.SelectNodes(xpath, XmlNamespaceManager);
                    }
                    else
                    {
                        nodes = doc.SelectNodes(xpath);
                    }
                }
                catch (XPathException xe)
                {
                    if (TraceData.IsEnabled)
                        TraceData.Trace(TraceEventType.Error, TraceData.XmlDPSelectNodesFailed, xpath, xe);
                }
            }

            return nodes;
        }


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        private XmlDataCollection  XmlDataCollection
        {
            get { return (XmlDataCollection) this.Data; }
        }

        private DispatcherOperationCallback CompletedCallback
        {
            get
            {
                if (_onCompletedCallback == null)
                    _onCompletedCallback = new DispatcherOperationCallback(OnCompletedCallback);
                return _onCompletedCallback;
            }
        }

        private XmlNodeChangedEventHandler NodeChangeHandler
        {
            get
            {
                if (_nodeChangedHandler == null)
                    _nodeChangedHandler = new XmlNodeChangedEventHandler(OnNodeChanged);
                return _nodeChangedHandler;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private XmlDocument         _document;  // the active XML DOM document
        private XmlDocument         _domSetDocument;  // a DOM set by user
        private XmlDocument         _savedDocument; // stored copy of Inline Xml for rollback
        private ManualResetEvent    _waitForInlineDoc; // serializer waits on this for inline doc
        private XmlNamespaceManager _nsMgr;
        private Uri     _source;
        private Uri     _baseUri;
        private string  _xPath = string.Empty;
        private bool    _tryInlineDoc = true;
        private bool    _isListening = false;
        private XmlIslandSerializer _xmlSerializer;
        bool            _isAsynchronous = true;
        bool            _inEndInit;
        private DispatcherOperationCallback _onCompletedCallback;
        private XmlNodeChangedEventHandler _nodeChangedHandler;
    }
}

