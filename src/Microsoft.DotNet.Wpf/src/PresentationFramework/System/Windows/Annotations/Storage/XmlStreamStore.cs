// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     An AnnotationStore subclass based on XML streams.  Processes XML
//     streams and returns CAF 2.0 OM objects.  Useful to other
//     AnnotationStore subclass who can get an XML stream of their
//     content.
//     Spec: CAF Storage Spec.doc
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Annotations;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Serialization;
using MS.Internal;
using MS.Internal.Annotations;
using MS.Internal.Annotations.Storage;
using MS.Utility;
using System.Windows.Markup;
using MS.Internal.Controls.StickyNote;
using System.Windows.Controls;

namespace System.Windows.Annotations.Storage
{
    /// <summary>
    ///     An AnnotationStore subclass based on XML streams.  Processes XML
    ///     streams and returns CAF 2.0 OM objects.  Useful to other
    ///     AnnotationStore subclass who can get an XML stream of their
    ///     content.
    /// </summary>
    public sealed class XmlStreamStore : AnnotationStore
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// This ctor initializes the Dictionary with predefined namespases
        /// and their compatibility
        /// </summary>
        static XmlStreamStore()
        {
            _predefinedNamespaces = new Dictionary<Uri, IList<Uri>>(6);
            _predefinedNamespaces.Add(new Uri(AnnotationXmlConstants.Namespaces.CoreSchemaNamespace), null);
            _predefinedNamespaces.Add(new Uri(AnnotationXmlConstants.Namespaces.BaseSchemaNamespace), null);
            _predefinedNamespaces.Add(new Uri(XamlReaderHelper.DefaultNamespaceURI), null);
        }

        /// <summary>
        ///     Creates an instance using the XML stream passed in as the
        ///     content. The XML in the stream must be valid XML and conform
        ///     to the CAF 2.0 schema.
        /// </summary>
        /// <param name="stream">stream containing annotation data in XML format</param>
        /// <exception cref="ArgumentNullException">stream is null</exception>
        /// <exception cref="XmlException">stream contains invalid XML</exception>
        public XmlStreamStore(Stream stream)
            : base()
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanSeek)
                throw new ArgumentException(SR.Get(SRID.StreamDoesNotSupportSeek));

            SetStream(stream, null);
        }

        /// <summary>
        ///     Creates an instance using the XML stream passed in as the
        ///     content. The XML in the stream must be valid XML and conform
        ///     to the Annotations V1 schema or a valid future version XML which
        ///     compatibility rules are that when applied they will produce
        ///     a valid Annotations V1 XML. This .ctor allows registration of
        ///     application specific known namespaces.
        /// </summary>
        /// <param name="stream">stream containing annotation data in XML format</param>
        /// <param name="knownNamespaces">A dictionary with known and compatible namespaces. The keys in
        /// this dictionary are known namespaces. The value of each key is a list of namespaces that are compatible with
        /// the key one, i.e. each of the namespaces in the value list will be transformed to the
        /// key namespace while reading the input XML.</param>
        /// <exception cref="ArgumentNullException">stream is null</exception>
        /// <exception cref="XmlException">stream contains invalid XML</exception>
        /// <exception cref="ArgumentException">duplicate namespace in knownNamespaces dictionary</exception>
        /// <exception cref="ArgumentException">null key in knownNamespaces dictionary</exception>
        public XmlStreamStore(Stream stream, IDictionary<Uri, IList<Uri>> knownNamespaces)
            : base()
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            SetStream(stream, knownNamespaces);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Add a new annotation to this store.  The new annotation's Id
        ///     is set to a new value.
        /// </summary>
        /// <param name="newAnnotation">the annotation to be added to the store</param>
        /// <exception cref="ArgumentNullException">newAnnotation is null</exception>
        /// <exception cref="ArgumentException">newAnnotation already exists in this store, as determined by its Id</exception>
        /// <exception cref="InvalidOperationException">if no stream has been set on the store</exception>
        /// <exception cref="ObjectDisposedException">if object has been Disposed</exception>
        public override void AddAnnotation(Annotation newAnnotation)
        {
            if (newAnnotation == null)
                throw new ArgumentNullException("newAnnotation");

            // We are going to modify internal data. Lock the object
            // to avoid modifications from other threads
            lock (SyncRoot)
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAnnotationBegin);
                try
                {
                    CheckStatus();

                    XPathNavigator editor = GetAnnotationNodeForId(newAnnotation.Id);

                    // we are making sure that the newAnnotation doesn't already exist in the store
                    if (editor != null)
                        throw new ArgumentException(SR.Get(SRID.AnnotationAlreadyExists), "newAnnotation");

                    // we are making sure that the newAnnotation doesn't already exist in the store map
                    if (_storeAnnotationsMap.FindAnnotation(newAnnotation.Id) != null)
                        throw new ArgumentException(SR.Get(SRID.AnnotationAlreadyExists), "newAnnotation");

                    // simply add the annotation to the map to save on performance
                    // notice that we need to tell the map that this instance of the annotation is dirty
                    _storeAnnotationsMap.AddAnnotation(newAnnotation, true);
                }
                finally
                {
                    //fire trace event
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAnnotationEnd);
                }
			}

            OnStoreContentChanged(new StoreContentChangedEventArgs(StoreContentAction.Added, newAnnotation));
        }

        /// <summary>
        ///     Delete the specified annotation.
        /// </summary>
        /// <param name="annotationId">the Id of the annotation to be deleted</param>
        /// <returns>the annotation that was deleted, or null if no annotation
        /// with the specified Id was found</returns>
        /// <exception cref="InvalidOperationException">if no stream has been set on the store</exception>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        public override Annotation DeleteAnnotation(Guid annotationId)
        {
            Annotation annotation = null;

            // We are now going to modify internal data. Lock the object
            // to avoid modifications from other threads
            lock (SyncRoot)
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeleteAnnotationBegin);

                try
                {
                    CheckStatus();

                    annotation = _storeAnnotationsMap.FindAnnotation(annotationId);

                    XPathNavigator editor = GetAnnotationNodeForId(annotationId);
                    if (editor != null)
                    {
                        // Only deserialize the annotation if its not already in our map
                        if (annotation == null)
                        {
                            annotation = (Annotation)_serializer.Deserialize(editor.ReadSubtree());
                        }
                        editor.DeleteSelf();
                    }

                    // Remove the instance from the map
                    _storeAnnotationsMap.RemoveAnnotation(annotationId);

                    // notice that in Add we add the annotation to the map only
                    // but in delete we delete it from both to the Xml and the map
                }
                finally
                {
                    //fire trace event
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeleteAnnotationEnd);
                }
            }

            // Only fire notification if we actually removed an annotation
            if (annotation != null)
            {
                OnStoreContentChanged(new StoreContentChangedEventArgs(StoreContentAction.Deleted, annotation));
            }

            return annotation;
        }

        /// <summary>
        ///     Queries the Xml stream for annotations that have an anchor
        ///     that contains a locator that begins with the locator parts
        ///     in anchorLocator.
        /// </summary>
        /// <param name="anchorLocator">the locator we are looking for</param>
        /// <returns>
        ///    A list of annotations that have locators in their anchors
        ///    starting with the same locator parts list as of the input locator
        ///    If no such annotations an empty list will be returned. The method
        ///    never returns null.
        /// </returns>
        /// <exception cref="ObjectDisposedException">if object has been Disposed</exception>
        /// <exception cref="InvalidOperationException">the stream is null</exception>
        public override IList<Annotation> GetAnnotations(ContentLocator anchorLocator)
        {
            // First we generate the XPath expression
            if (anchorLocator == null)
                throw new ArgumentNullException("anchorLocator");

            if (anchorLocator.Parts == null)
                throw new ArgumentNullException("anchorLocator.Parts");

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.GetAnnotationByLocBegin);
            IList<Annotation> annotations = null;
            try
            {
                string query = @"//" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + ":" + AnnotationXmlConstants.Elements.ContentLocator;

                if (anchorLocator.Parts.Count > 0)
                {
                    query += @"/child::*[1]/self::";
                    for (int i = 0; i < anchorLocator.Parts.Count; i++)
                    {
                        if (anchorLocator.Parts[i] != null)
                        {
                            if (i > 0)
                            {
                                query += @"/following-sibling::";
                            }

                            string fragment = anchorLocator.Parts[i].GetQueryFragment(_namespaceManager);

                            if (fragment != null)
                            {
                                query += fragment;
                            }
                            else
                            {
                                query += "*";
                            }
                        }
                    }
                }

                query += @"/ancestor::" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + ":Anchors/ancestor::" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + ":Annotation";

                annotations = InternalGetAnnotations(query, anchorLocator);
            }
            finally
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.GetAnnotationByLocEnd);
            }

            return annotations;
        }

        /// <summary>
        /// Returns a list of all annotations in the store
        /// </summary>
        /// <returns>annotations list. Can return an empty list, but never null.</returns>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        public override IList<Annotation> GetAnnotations()
        {
            IList<Annotation> annotations = null;
            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.GetAnnotationsBegin);
            try
            {
                string query = "//" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + ":Annotation";

                annotations = InternalGetAnnotations(query, null);
            }
            finally
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.GetAnnotationsEnd);
            }
            return annotations;
        }

        /// <summary>
        /// Finds annotation by Id
        /// </summary>
        /// <param name="annotationId">annotation id</param>
        /// <returns>The annotation. Null if the annotation does not exists</returns>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        public override Annotation GetAnnotation(Guid annotationId)
        {
            lock (SyncRoot)
            {
                Annotation annotation = null;
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.GetAnnotationByIdBegin);
                try
                {
                    CheckStatus();

                    annotation = _storeAnnotationsMap.FindAnnotation(annotationId);

                    // If there is no pre-existing instance, we deserialize and create an instance.
                    if (annotation != null)
                    {
                        return annotation;
                    }

                    XPathNavigator editor = GetAnnotationNodeForId(annotationId);
                    if (editor != null)
                    {
                        annotation = (Annotation)_serializer.Deserialize(editor.ReadSubtree());

                        // Add the new instance to the map
                        _storeAnnotationsMap.AddAnnotation(annotation, false);
                    }
                }
                finally
                {
                    //fire trace event
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.GetAnnotationByIdEnd);
                }
                return annotation;
            }
        }

        /// <summary>
        ///     Causes any buffered data to be written to the underlying
        ///     storage mechanism.  Gets called after each operation if
        ///     AutoFlush is set to true.  The stream is truncated to
        ///     the length of the data written out by the store.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">stream cannot be written to</exception>
        /// <exception cref="InvalidOperationException">if no stream has been set on the store</exception>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        /// <seealso cref="AutoFlush"/>
        public override void Flush()
        {
            lock (SyncRoot)
            {
                CheckStatus();
                if (!_stream.CanWrite)
                {
                    throw new UnauthorizedAccessException(SR.Get(SRID.StreamCannotBeWritten));
                }

                if (_dirty)
                {
                    SerializeAnnotations();
                    _stream.Position = 0;
                    _stream.SetLength(0);
                    _document.PreserveWhitespace = true;
                    _document.Save(_stream);
                    _stream.Flush();
                    _dirty = false;
                }
            }
        }

        /// <summary>
        /// Returns a list of namespaces that are compatible with an  input namespace
        /// </summary>
        /// <param name="name">namespace</param>
        /// <returns>a list of compatible namespaces. Can be null</returns>
        /// <remarks>This method works only with built-in AnnotationFramework namespaces.
        /// For any other input namespace the return value will be null even if it is
        /// registered with the XmlStreamStore ctor</remarks>
        public static IList<Uri> GetWellKnownCompatibleNamespaces(Uri name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (_predefinedNamespaces.ContainsKey(name))
                return _predefinedNamespaces[name];
            return null;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     When set to true an implementation should call Flush()
        ///     as a side-effect after each operation.
        /// </summary>
        /// <value>
        ///     true if the implementation is set to call Flush() after
        ///     each operation; false otherwise
        /// </value>
        public override bool AutoFlush
        {
            get
            {
                lock (SyncRoot)
                {
                    return _autoFlush;
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _autoFlush = value;

                    // Commit anything that needs to be committed up to this point
                    if (_autoFlush)
                    {
                        Flush();
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of the namespaces that are ignored while loading
        /// the Xml stream
        /// </summary>
        /// <remarks>The value is never null, but can be an empty list if nothing has been ignored</remarks>
        public IList<Uri> IgnoredNamespaces
        {
            get
            {
                return _ignoredNamespaces;
            }
        }

        /// <summary>
        /// Returns a list of all namespaces that are internaly used by the framework
        /// </summary>
        public static IList<Uri> WellKnownNamespaces
        {
            get
            {
                Uri[] res = new Uri[_predefinedNamespaces.Keys.Count];
                _predefinedNamespaces.Keys.CopyTo(res, 0);
                return res;
            }
        }


        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///     Disposes of the resources (other than memory) used by the store.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged
        /// resources; false to release only unmanaged resources</param>
        protected override void Dispose(bool disposing)
        {
            //call the base class first to set _disposed to false
            //in order to avoid working with the store when the resources
            //are released
            base.Dispose(disposing);

            if (disposing)
            {
                Cleanup();
            }
        }

        /// <summary>
        ///     Called after every annotation action on the store.  We override it
        ///     to update the dirty state of the store.
        /// </summary>
        /// <param name="e">arguments for the event to fire</param>
        protected override void OnStoreContentChanged(StoreContentChangedEventArgs e)
        {
            lock (SyncRoot)
            {
                _dirty = true;
            }

            base.OnStoreContentChanged(e);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Applies the specified XPath expression to the store and returns
        ///     the results.
        /// </summary>
        /// <param name="queryExpression">the XPath expression to be applied to the store</param>
        /// <returns>
        ///     An IList containing zero or more annotations that match the
        ///     criteria in the XPath expression; never will return null.  If
        ///     no annotations meet the criteria, an empty list is returned.
        /// </returns>
        /// <exception cref="InvalidOperationException">if no stream has been set on the store</exception>
        /// <exception cref="ObjectDisposedException">if object has been Disposed</exception>
        /// <exception cref="ArgumentNullException">queryExpression is null</exception>
        /// <exception cref="ArgumentException">queryExpression is empty string</exception>
        private List<Guid> FindAnnotationIds(string queryExpression)
        {
            Invariant.Assert(queryExpression != null && queryExpression.Length > 0,
                          "Invalid query expression");

            Guid annId;
            List<Guid> retObj = null;

            // Lock the object so nobody can change the document
            // while the query is executed
            lock (SyncRoot)
            {
                CheckStatus();

                XPathNavigator navigator = _document.CreateNavigator();
                XPathNodeIterator iterator = navigator.Select(queryExpression, _namespaceManager);

                if (iterator != null && iterator.Count > 0)
                {
                    retObj = new List<Guid>(iterator.Count);
                    foreach (XPathNavigator node in iterator)
                    {
                        string nodeId = node.GetAttribute("Id", "");
                        if (String.IsNullOrEmpty(nodeId))
                        {
                            throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.Id, AnnotationXmlConstants.Elements.Annotation));
                        }

                        try
                        {
                            annId = XmlConvert.ToGuid(nodeId);
                        }
                        catch (FormatException fe)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.CannotParseId), fe);
                        }

                        retObj.Add(annId);
                    }
                }
                else
                {
                    retObj = new List<Guid>(0);
                }
            }

            return retObj;
        }

        /// <summary>
        ///     Used as AuthorChanged event handler for all annotations
        ///     handed out by the map.
        /// </summary>
        /// <param name="sender">annotation that sent the event</param>
        /// <param name="e">args for the event</param>
        private void HandleAuthorChanged(object sender, AnnotationAuthorChangedEventArgs e)
        {
            lock (SyncRoot)
            {
                _dirty = true;
            }

            OnAuthorChanged(e);
        }

        /// <summary>
        ///     Used as AnchorChanged event handler for all annotations
        ///     handed out by the map.
        /// </summary>
        /// <param name="sender">annotation that sent the event</param>
        /// <param name="e">args for the event</param>
        private void HandleAnchorChanged(object sender, AnnotationResourceChangedEventArgs e)
        {
            lock (SyncRoot)
            {
                _dirty = true;
            }

            OnAnchorChanged(e);
        }

        /// <summary>
        ///     Used as CargoChanged event handler for all annotations
        ///     handed out by the map.
        /// </summary>
        /// <param name="sender">annotation that sent the event</param>
        /// <param name="e">args for the event</param>
        private void HandleCargoChanged(object sender, AnnotationResourceChangedEventArgs e)
        {
            lock (SyncRoot)
            {
                _dirty = true;
            }

            OnCargoChanged(e);
        }

        /// <summary>
        /// 1- Merge Annotations queried from both the map and the Xml stream
        /// 2- Add the annotations found in the Xml stream to the map
        /// </summary>
        /// <param name="mapAnnotations">A dictionary of map annotations</param>
        /// <param name="storeAnnotationsId">A list of annotation ids from the Xml stream</param>
        /// <returns></returns>
        private IList<Annotation> MergeAndCacheAnnotations(Dictionary<Guid, Annotation> mapAnnotations, List<Guid> storeAnnotationsId)
        {
            // first put all annotations from the map in the return list
            List<Annotation> annotations = new List<Annotation>((IEnumerable<Annotation>)mapAnnotations.Values);

            // there three possible conditions
            // 1- An annotation exists in xml and in the store map results
            // 2- An annotation exists in xml and not in the store map results
            //      2-1- The annotation is found in the map
            //      2-2- The annotation is not found in the map

            // Now, we need to find annotations in the store that are not in the map results
            // and verify that they should be serialized
            foreach (Guid annotationId in storeAnnotationsId)
            {
                Annotation annot;
                bool foundInMapResults = mapAnnotations.TryGetValue(annotationId, out annot);
                if (!foundInMapResults)
                {
                    // it is not in the map - get it from the store
                    annot = GetAnnotation(annotationId);
                    annotations.Add(annot);
                }
            }

            return annotations;
        }

        /// <summary>
        /// Do the GetAnnotations work inside a lock statement for thread safety reasons
        /// </summary>
        /// <param name="query"></param>
        /// <param name="anchorLocator"></param>
        /// <returns></returns>
        private IList<Annotation> InternalGetAnnotations(string query, ContentLocator anchorLocator)
        {
            // anchorLocator being null is handled appropriately below
            Invariant.Assert(query != null, "Parameter 'query' is null.");

            lock (SyncRoot)
            {
                CheckStatus();

                List<Guid> annotationIds = FindAnnotationIds(query);
                Dictionary<Guid, Annotation> annotations = null;

                // Now, get the annotations in the map that satisfies the query criterion
                if (anchorLocator == null)
                {
                    annotations = _storeAnnotationsMap.FindAnnotations();
                }
                else
                {
                    annotations = _storeAnnotationsMap.FindAnnotations(anchorLocator);
                }

                // merge both query results
                return MergeAndCacheAnnotations(annotations, annotationIds);
            }
        }


        /// <summary>
        ///     Loads the current stream into the XmlDocument used by this
        ///     store as a backing store.  If the stream doesn't contain any
        ///     data we load up a default XmlDocument for the Annotations schema.
        ///     The "http://schemas.microsoft.com/windows/annotations/2003/11/core"
        ///     namespace is registered with "anc" prefix and the
        ///    "http://schemas.microsoft.com/windows/annotations/2003/11/base" namespace
        ///     is registered with "anb" prefix as global namespaces.
        ///     We also select from the newly loaded document the new top
        ///     level node.  This is used later for insertions, etc.
        /// </summary>
        /// <exception cref="XmlException">if the stream contains invalid XML</exception>
        private void LoadStream(IDictionary<Uri, IList<Uri>> knownNamespaces)
        {
            //check input data first
            CheckKnownNamespaces(knownNamespaces);

            lock (SyncRoot)
            {
                _document = new XmlDocument();
                _document.PreserveWhitespace = false;
                if (_stream.Length == 0)
                {
                    _document.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?> <" +
                        AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + ":Annotations xmlns:" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + "=\"" +
                        AnnotationXmlConstants.Namespaces.CoreSchemaNamespace + "\" xmlns:" + AnnotationXmlConstants.Prefixes.BaseSchemaPrefix + "=\"" + AnnotationXmlConstants.Namespaces.BaseSchemaNamespace + "\" />");
                }
                else
                {
                    _xmlCompatibilityReader = SetupReader(knownNamespaces);
                    _document.Load(_xmlCompatibilityReader);
                }

                _namespaceManager = new XmlNamespaceManager(_document.NameTable);
                _namespaceManager.AddNamespace(AnnotationXmlConstants.Prefixes.CoreSchemaPrefix, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
                _namespaceManager.AddNamespace(AnnotationXmlConstants.Prefixes.BaseSchemaPrefix, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

                // This use of an iterator is necessary because SelectSingleNode isn't available in the PD3
                // drop of the CLR.  Eventually we should be able to call SelectSingleNode and not have to
                // use an iterator to get a single node.

                XPathNavigator navigator = _document.CreateNavigator();
                XPathNodeIterator iterator = navigator.Select("//" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + ":Annotations", _namespaceManager);
                Invariant.Assert(iterator.Count == 1, "More than one annotation returned for the query");

                iterator.MoveNext();
                _rootNavigator = (XPathNavigator)iterator.Current;
            }
        }

        /// <summary>
        /// Checks if passed external known namespaces are valid
        /// </summary>
        /// <param name="knownNamespaces">namespaces dictionary</param>
        /// <remarks>We do not allow internal namespases in this dictionary nor
        /// duplicates</remarks>
        private void CheckKnownNamespaces(IDictionary<Uri, IList<Uri>> knownNamespaces)
        {
            if (knownNamespaces == null)
                return;

            IList<Uri> allNamespaces = new List<Uri>();

            //add AnnotationFramework namespaces
            foreach (Uri name in _predefinedNamespaces.Keys)
            {
                allNamespaces.Add(name);
            }

            //add external namespaces
            foreach (Uri knownNamespace in knownNamespaces.Keys)
            {
                if (knownNamespace == null)
                {
                    throw new ArgumentException(SR.Get(SRID.NullUri), "knownNamespaces");
                }
                if (allNamespaces.Contains(knownNamespace))
                {
                    throw new ArgumentException(SR.Get(SRID.DuplicatedUri), "knownNamespaces");
                }
                allNamespaces.Add(knownNamespace);
            }

            // check compatible namespaces
            foreach (KeyValuePair<Uri, IList<Uri>> item in knownNamespaces)
            {
                if (item.Value != null)
                {
                    foreach (Uri name in item.Value)
                    {
                        if (name == null)
                        {
                            throw new ArgumentException(SR.Get(SRID.NullUri), "knownNamespaces");
                        }

                        if (allNamespaces.Contains(name))
                        {
                            throw new ArgumentException(SR.Get(SRID.DuplicatedCompatibleUri), "knownNamespaces");
                        }
                        allNamespaces.Add(name);
                    }//foreach
                }//if
            }//foreach
        }

        /// <summary>
        /// Creates and initializes the XmlCompatibilityReader
        /// </summary>
        /// <param name="knownNamespaces">Dictionary of external known namespaces</param>
        /// <returns>The XmlCompatibilityReader</returns>
        private XmlCompatibilityReader SetupReader(IDictionary<Uri, IList<Uri>> knownNamespaces)
        {
            IList<string> supportedNamespaces = new List<string>();

            //add AnnotationFramework namespaces
            foreach (Uri name in _predefinedNamespaces.Keys)
            {
                supportedNamespaces.Add(name.ToString());
            }

            //add external namespaces
            if (knownNamespaces != null)
            {
                foreach (Uri knownNamespace in knownNamespaces.Keys)
                {
                    Debug.Assert(knownNamespace != null, "null knownNamespace");
                    supportedNamespaces.Add(knownNamespace.ToString());
                }
            }

            //create XmlCompatibilityReader first
            XmlCompatibilityReader reader = new XmlCompatibilityReader(new XmlTextReader(_stream),
            new IsXmlNamespaceSupportedCallback(IsXmlNamespaceSupported), supportedNamespaces);

            // Declare compatibility.
            // Skip the Framework ones because they are all null in this version
            if (knownNamespaces != null)
            {
                foreach (KeyValuePair<Uri, IList<Uri>> item in knownNamespaces)
                {
                    if (item.Value != null)
                    {
                        foreach (Uri name in item.Value)
                        {
                            Debug.Assert(name != null, "null compatible namespace");
                            reader.DeclareNamespaceCompatibility(item.Key.ToString(), name.ToString());
                        }//foreach
                    }//if
                }//foreach
            }//if

            //cleanup the _ignoredNamespaces
            _ignoredNamespaces.Clear();
            return reader;
        }

        /// <summary>
        /// A callback for XmlCompatibilityReader
        /// </summary>
        /// <param name="xmlNamespace">inquired namespace</param>
        /// <param name="newXmlNamespace">the newer subsumming namespace</param>
        /// <returns>true if the namespace is known, false if not</returns>
        /// <remarks>This API always returns false because all known namespaces are registered
        /// before loading the Xml. It stores the namespace as ignored.</remarks>
        private bool IsXmlNamespaceSupported(string xmlNamespace, out string newXmlNamespace)
        {
            //store the namespace if needed
            if (!String.IsNullOrEmpty(xmlNamespace))
            {
                if (!Uri.IsWellFormedUriString(xmlNamespace, UriKind.RelativeOrAbsolute))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidNamespace, xmlNamespace), "xmlNamespace");
                }
                Uri namespaceUri = new Uri(xmlNamespace, UriKind.RelativeOrAbsolute);
                if (!_ignoredNamespaces.Contains(namespaceUri))
                    _ignoredNamespaces.Add(namespaceUri);
            }

            newXmlNamespace = null;
            return false;
        }



        /// <summary>
        ///     Selects the annotation XmlElement from the current document
        ///     with the given id.
        /// </summary>
        /// <param name="id">the id to query for in the XmlDocument</param>
        /// <returns>the XmlElement representing the annotation with the given id</returns>
        private XPathNavigator GetAnnotationNodeForId(Guid id)
        {
            XPathNavigator navigator = null;

            lock (SyncRoot)
            {
                XPathNavigator tempNavigator = _document.CreateNavigator();

                // This use of an iterator is necessary because SelectSingleNode isn't available in the PD3
                // drop of the CLR.  Eventually we should be able to call SelectSingleNode and not have to
                // use an iterator to get a single node.

                // We use XmlConvert.ToString to turn the Guid into a string because
                // that's what is used by the Annotation's serialization methods.
                XPathNodeIterator iterator = tempNavigator.Select(@"//" + AnnotationXmlConstants.Prefixes.CoreSchemaPrefix + @":Annotation[@Id=""" + XmlConvert.ToString(id) + @"""]", _namespaceManager);
                if (iterator.MoveNext())
                {
                    navigator = (XPathNavigator)iterator.Current;
                }
            }

            return navigator;
        }

        /// <summary>
        ///    Verifies the store is in a valid state.  Throws exceptions otherwise.
        /// </summary>
        private void CheckStatus()
        {
            lock (SyncRoot)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(null, SR.Get(SRID.ObjectDisposed_StoreClosed));

                if (_stream == null)
                    throw new InvalidOperationException(SR.Get(SRID.StreamNotSet));
            }
        }

        /// <summary>
        /// Called from flush to serialize all annotations in the map
        /// notice that delete takes care of the delete action both in the map
        /// and in the store
        /// </summary>
        private void SerializeAnnotations()
        {
            List<Annotation> mapAnnotations = _storeAnnotationsMap.FindDirtyAnnotations();
            foreach (Annotation annotation in mapAnnotations)
            {
                XPathNavigator editor = GetAnnotationNodeForId(annotation.Id);
                if (editor == null)
                {
                    editor = (XPathNavigator)_rootNavigator.CreateNavigator();
                    XmlWriter writer = editor.AppendChild();
                    _serializer.Serialize(writer, annotation);
                    writer.Close();
                }
                else
                {
                    XmlWriter writer = editor.InsertBefore();
                    _serializer.Serialize(writer, annotation);
                    writer.Close();
                    editor.DeleteSelf();
                }
            }
            _storeAnnotationsMap.ValidateDirtyAnnotations();
        }

        /// <summary>
        ///    Discards changes and cleans up references.
        /// </summary>
        private void Cleanup()
        {
            lock (SyncRoot)
            {
                _xmlCompatibilityReader = null;
                _ignoredNamespaces = null;
                _stream = null;
                _document = null;
                _rootNavigator = null;
                _storeAnnotationsMap = null;
            }
        }

        /// <summary>
        ///     Sets the stream for this store.  Assumes Cleanup has
        ///     been previously called (or this is a new store).
        /// </summary>
        /// <param name="stream">new stream for this store</param>
        /// <param name="knownNamespaces">List of known and compatible namespaces used to initialize
        /// the XmlCompatibilityReader</param>
        private void SetStream(Stream stream, IDictionary<Uri, IList<Uri>> knownNamespaces)
        {
            try
            {
                lock (SyncRoot)
                {
                    _storeAnnotationsMap = new StoreAnnotationsMap(HandleAuthorChanged, HandleAnchorChanged, HandleCargoChanged);
                    _stream = stream;
                    LoadStream(knownNamespaces);
                }
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Dirty bit for the store's in-memory cache
        private bool _dirty;
        // Boolean flag represents whether the store should perform a flush
        // after every operation or not
        private bool _autoFlush;
        // XmlDocument used as an in-memory cache of the stream's XML content
        private XmlDocument _document;
        // Namespace manager used for all queries.
        private XmlNamespaceManager _namespaceManager;
        // Stream passed in by the creator of this instance.
        private Stream _stream;
        // The xpath navigator used to navigate the annotations Xml stream
        private XPathNavigator _rootNavigator;
        // map that holds AnnotationId->Annotation
        StoreAnnotationsMap _storeAnnotationsMap;
        //list of ignored namespaces during XmlLoad
        List<Uri> _ignoredNamespaces = new List<Uri>();

        //XmlCompatibilityReader - we need to hold that one open, so the underlying stream stays open too
        // if the store is disposed the reader will be disposed and the stream will be closed too.
        XmlCompatibilityReader _xmlCompatibilityReader;

        ///
        ///Static fields
        ///

        //predefined namespaces
        private static readonly Dictionary<Uri, IList<Uri>> _predefinedNamespaces;
        // Serializer for Annotations
        private static readonly Serializer _serializer = new Serializer(typeof(Annotation));

#endregion Private Fields
    }
}
