// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



    Abstract:
        This file contains the definition for IXpsFixedDocumentSequenceReader
        and IXpsFixedDocumentSequenceWriter interfaces as well as definition
        and implementation of XpsFixedDocumentSequenceReaderWriter.  These
        interfaces and class are used for writing document sequence
        parts to a Xps package.


--*/
using System;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Printing;

using MS.Internal.IO.Packaging.Extensions;
using PackUriHelper = System.IO.Packaging.PackUriHelper;

namespace System.Windows.Xps.Packaging
{
    #region IXpsFixedDocumentSequenceReader interface definition

    /// <summary>
    /// Interface for reading document sequence parts from a Xps package.
    /// </summary>
    /// <remarks>Class will be internal until reading/de-serialization is implemented.</remarks>
    public interface IXpsFixedDocumentSequenceReader
    {
        #region Public methods

        /// <summary>
        /// This method retrieves a fixed document part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="documentSource">
        /// The URI of the fixed document to retrieve.
        /// </param>
        /// <returns>
        /// Returns an interface to an IXpsFixedDocumentReader to read
        /// the requested fixed document.
        /// </returns>
        IXpsFixedDocumentReader
        GetFixedDocument(
            Uri         documentSource
            );

        #endregion Public methods

        #region Public properties

        /// <summary>
        /// Gets the PrintTicket associated with this document sequence.
        /// </summary>
        /// <value>Value can be a PrintTicket or null.</value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">PrintTicket has already been committed.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">Property is not a valid PrintTicket instance.</exception>
        PrintTicket PrintTicket { get; }

        /// <summary>
        /// Gets the URI assigned to this document sequence part.
        /// </summary>
        /// <value>Value is a URI for the Metro part.</value>
        Uri Uri { get; }

        /// <summary>
        /// Gets a collection of all fixed documents that are contained within
        /// this document sequence.
        /// </summary>
        /// <value>
        /// Value is a Collection containing IXpsFixedDocumentReader interfaces.
        /// </value>
        ReadOnlyCollection<IXpsFixedDocumentReader>  FixedDocuments { get; }
        /// <summary>
        /// thumbnail image associated with this sequence
        /// </summary>
        XpsThumbnail
        Thumbnail{ get; }

        #endregion Public properties
    }

    #endregion IXpsFixedDocumentSequenceReader interface definition

    #region IXpsFixedDocumentSequenceWriter interface definition

    /// <summary>
    /// Interface for writing a document sequence to the Xps package.
    /// </summary>
    public interface IXpsFixedDocumentSequenceWriter
    {
        #region Public methods

        /// <summary>
        /// This method adds a fixed document part to the Xps package
        /// and associates it with the current document sequence.
        /// </summary>
        /// <returns>
        /// Returns an interface to the newly created fixed document.
        /// </returns>
        /// <exception cref="SRID.ReachPackaging_PanelOrSequenceAlreadyOpen">Current DocumentSequence not completed.</exception>
        IXpsFixedDocumentWriter
        AddFixedDocument(
            );

        /// <summary>
        /// This method adds a thumbnail to the current DocumentSequence.
        ///
        /// There can only be one thumbnail attached to the DocumentSequence.
        ///  Calling this method when there
        /// is already a starting part causes InvalidOperationException to be
        /// thrown.
        /// </summary>
        XpsThumbnail
        AddThumbnail(
            XpsImageType  imageType 
            );

        /// <summary>
        /// This method commits any changes not already committed for this
        /// document sequence.
        ///
        /// </summary>
        void
        Commit(
            );

        #endregion Public methods

        #region Public properties

        /// <summary>
        /// Sets the PrintTicket associated with this document sequence.
        /// </summary>
        /// <value>
        /// The value must be a valid PrintTicket instance.
        ///
        /// Note:  The PrintTicket can only be assigned to prior to if being
        /// committed to the package.  The commit happens when a valid PrintTicket
        /// is set and a subsequent flush on the document occurs.
        /// </value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">PrintTicket has already been committed.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">Property is not a valid PrintTicket instance.</exception>
        PrintTicket PrintTicket { set; }

        /// <summary>
        /// Gets the URI assigned to this document sequence part.
        /// </summary>
        /// <value>Value is a URI for the Metro part.</value>
        Uri Uri { get; }

        #endregion Public properties
    }

    #endregion IXpsFixedDocumentSequenceWriter interface definition

    /// <summary>
    /// This class implements the reading and writing functionality for
    /// a document sequence within a Xps package.
    /// </summary>
    internal sealed class XpsFixedDocumentSequenceReaderWriter : XpsPartBase,
                        IXpsFixedDocumentSequenceReader,
                        IXpsFixedDocumentSequenceWriter,
                        INode,
                        IDisposable
    {
        #region Constructors

        /// <summary>
        /// Internal constructor for the XpsFixedDocumentSequenceReaderWriter class.
        /// This class is created internally so we can keep the Xps hierarchy
        /// completely under control when using these APIs.
        /// </summary>
        /// <param name="xpsManager">
        /// The XpsManager for the current Xps package.
        /// </param>
        /// <param name="parent">
        /// The parent node of this document.
        /// </param>
        /// <param name="part">
        /// The internal Metro part that represents this document sequence.
        /// </param>
        /// <exception cref="ArgumentNullException">part is null.</exception>
        internal
        XpsFixedDocumentSequenceReaderWriter(
            XpsManager    xpsManager,
            INode           parent,
            PackagePart     part
            )
            : base(xpsManager)
        {
            if (null == part)
            {
                throw new ArgumentNullException("part");
            }

            this.Uri = part.Uri;
            _metroPart = part;
            _partEditor = new XmlPartEditor(_metroPart);
            _documentCache = new List<IXpsFixedDocumentReader>();
            _documentsWritten = 0;
            _parentNode = parent;
            _hasParsedDocuments = false;
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets or sets the PrintTicket to be stored with this document
        /// sequence.
        ///
        /// NOTE:  The PrintTicket can only be assigned to prior to if being
        /// committed to the package.  The commit happens when a valid PrintTicket
        /// is set and a subsequent flush on the document occurs.
        /// </summary>
        /// <value>Value can be a PrintTicket or null.</value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">PrintTicket has already been committed.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">Property is not a valid PrintTicket instance.</exception>
        public PrintTicket PrintTicket
        {
            get
            {
                if( _printTicket == null )
                {
                    _printTicket = CurrentXpsManager.EnsurePrintTicket(Uri);
                }
                return _printTicket;
            }
            set
            {
                if(value != null)
                {
                    if (_isPrintTicketCommitted)
                    {
                        throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_PrintTicketAlreadyCommitted));
                    }
                    if (!value.GetType().Equals(typeof(PrintTicket)))
                    {
                        throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotAPrintTicket));
                    }
                    _printTicket = value.Clone();
                }
                else
                {
                    _printTicket = null;
                }
            }
        }

        /// <summary>
        /// Gets a collection of all fixed documents that are contained within
        /// this document sequence.
        /// </summary>
        /// <value>
        /// Value is a Collection containing IXpsFixedDocumentReader interfaces.
        /// </value>
        /// <remarks>
        /// This is only internal until it is implemented.
        /// </remarks>
        public ReadOnlyCollection<IXpsFixedDocumentReader> FixedDocuments
        {
            get
            {
                UpdateDocumentCache();
                return new ReadOnlyCollection<IXpsFixedDocumentReader>(_documentCache);
            }
        }

        /// <summary>
        /// thumbnail image associated with this sequence
        /// </summary>
        public
        XpsThumbnail
        Thumbnail
        {
            get
            {
                EnsureThumbnail();
                return _thumbnail;
            }
        }
        #endregion Public properties

        #region Public methods

        /// <summary>
        /// This method adds a fixed document part to the Xps package
        /// and associates it with the current document sequence.
        /// </summary>
        /// <returns>
        /// Returns an interface to the newly created fixed document.
        /// </returns>
        /// <exception cref="SRID.ReachPackaging_PanelOrSequenceAlreadyOpen">Current DocumentSequence not completed.</exception>
        public
        IXpsFixedDocumentWriter
        AddFixedDocument(
            )
        {
            //
            // Create the part and coresponding writer
            //
            PackagePart metroPart = CurrentXpsManager.GenerateUniquePart(XpsS0Markup.FixedDocumentContentType);
            XpsFixedDocumentReaderWriter fixedDocument = new XpsFixedDocumentReaderWriter(CurrentXpsManager, this, metroPart,_documentsWritten + 1);

             
            //Here we used to add the fixed document to _documentCache, but _documentCache is never accessed if this object was created as an IXpsFixedDocumentSequenceWriter.
            //So instead keep a separate documentsWritten count and forget about the cache when using this method.
            _documentsWritten++;

            //
            // Write out FixedDocument markup
            //
            AddDocumentToSequence(fixedDocument.Uri);


            return fixedDocument;
        }

        /// <summary>
        /// This method adds a thumbnail to the current DocumentSequence.
        ///
        /// There can only be one thumbnail attached to the DocumentSequence.
        ///  Calling this method when there
        /// is already a starting part causes InvalidOperationException to be
        /// thrown.
        /// </summary>
        /// <returns>Returns a XpsThumbnail instance.</returns>
        public
        XpsThumbnail
        AddThumbnail(
            XpsImageType  imageType 
            )
        {
            _thumbnail = CurrentXpsManager.AddThumbnail( imageType, this, Thumbnail );
            _metroPart.CreateRelationship( _thumbnail.Uri,
                                             TargetMode.Internal,
                                             XpsS0Markup.ThumbnailRelationshipName
                                           );
            return _thumbnail;
        }

        /// <summary>
        /// This method retrieves a fixed document part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="documentUri">
        /// The URI of the fixed document to retrieve.
        /// </param>
        /// <returns>
        /// Returns an interface to an IXpsFixedDocumentReader to read
        /// the requested fixed document.
        /// </returns>
        public
        IXpsFixedDocumentReader
        GetFixedDocument(
            Uri         documentUri
            )
        {
            UpdateDocumentCache();
            IXpsFixedDocumentReader fixedDocument  = null;
            //
            // Check the cache for the requested document
            //
            foreach (IXpsFixedDocumentReader reader in _documentCache)
            {
                if( documentUri == reader.Uri )
                {
                    fixedDocument = reader;                }
            }

            return fixedDocument;
        }

        /// <summary>
        /// This method commits any changes not already committed for this
        /// document sequence.
        ///
        /// NOTE:  This commits changes on all child objects underneath this
        /// branch.  No further changes will be allowed.
        /// </summary>
        public
        void
        Commit(
            )
        {
            CommitInternal();
        }

        /// <summary>
        /// This method closes streams and frees memory for this
        /// document sequence.
        /// </summary>
        internal
        override
        void
        CommitInternal(
            )
        {
            CommitPrintTicket();
            if (null != _partEditor)
            {
                ((INode)this).Flush();
                _partEditor.Close();


                _partEditor     = null;
                _metroPart      = null;

                _documentCache  = null;
                
                _documentsWritten = 0;

                _thumbnail      = null;

                _parentNode     = null;
                _hasParsedDocuments = false;
           }
        }



        /// <summary>
        /// Adds itself and and its reationship if it exists
        /// Adds dependent part Uris to the passed list following
        /// the passed restrictions dependents
        /// include pages, annotaions, properties, and signatures
        /// </summary>
        internal
        void
        CollectSelfAndDependents(
            Dictionary<Uri,Uri>                     dependentList,
            List<PackageRelationshipSelector>       selectorList,
            XpsDigSigPartAlteringRestrictions       restrictions
            )
        {
            //
            // Add my self
            //
            dependentList[Uri] = Uri;

            //
            //  Add my dependents
            //
            CollectDependents( dependentList, selectorList,  restrictions);
}

        internal
        void
        CollectXmlPartsAndDepenedents(
            List<PackagePart>                       xmlPartList
            )
        {
            //
            // Add my self to be tested for V&E Markup
            //
            xmlPartList.Add(_metroPart);
            UpdateDocumentCache();
            // Add all documents
            foreach (IXpsFixedDocumentReader reader in _documentCache)
            {
                (reader as XpsFixedDocumentReaderWriter).CollectXmlPartsAndDepenedents(xmlPartList);
            }
        }

        #endregion Public methods

        #region Private methods

        private
        void
        AddDocumentToSequence(
            Uri         partUri
            )
        {
            _partEditor.PrepareXmlWriter(XpsS0Markup.FixedDocumentSequence, XpsS0Markup.DocumentSequenceNamespace);
            XmlTextWriter xmlWriter = _partEditor.XmlWriter;
            //
            // Write <Item Target="partUri"/>
            //
            String relativePath = XpsManager.MakeRelativePath(Uri, partUri);

            xmlWriter.WriteStartElement(XpsS0Markup.DocumentReference);
            xmlWriter.WriteAttributeString(XmlTags.Source, relativePath);
            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// This method writes the PrintTicket associated with
        /// this document sequence to the Metro package.
        /// </summary>
        private
        void
        CommitPrintTicket(
            )
        {
            //
            // Flush the PrintTicket if needed
            //
            if (!_isPrintTicketCommitted )
            {
                if(null != _printTicket)
                {
                    CurrentXpsManager.WritePrintTicket(this, _metroPart, _printTicket);
                }
                else
                {
                    CurrentXpsManager.WriteEmptyPrintTicket(this, _metroPart);
                }
                _isPrintTicketCommitted = true;
            }
        }

        /// <summary>
        /// Test if the document cache has initialized and
        /// Updates if necessary
        /// </summary>
        private
        void
        UpdateDocumentCache()
        {
           if( !_hasParsedDocuments )
            {
                ParseDocuments();
                _hasParsedDocuments = true;
            }
        }

        /// <summary>
        /// This method parses the part pulling out the Document Referneces
        /// and populates the _documentCache
        /// </summary>
        private
        void
        ParseDocuments()
        {
            using (Stream stream = _metroPart.GetStream(FileMode.Open))
            {
                //
                // If the stream is empty there are no documents to parse
                //
                if (stream.Length > 0)
                {
                    XmlTextReader reader = new XmlTextReader(stream);

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == XpsS0Markup.DocumentReference)
                        {
                            string attribute = reader.GetAttribute(XmlTags.Source);
                            if (attribute != null)
                            {
                                Uri relativeUri = new Uri(attribute, UriKind.Relative);
                                //This routine properly adds DocumentReaderWriter to the _documentCache
                                AddDocumentToCache(PackUriHelper.ResolvePartUri(Uri, relativeUri));
                            }
                        }
                    }
                }
            }
             
        }

        /// <summary>
        /// Adds dependent part Uris to the passed list
        /// </summary>
        private
        void
        CollectDependents(
            Dictionary<Uri,Uri>                     dependents,
            List<PackageRelationshipSelector>       selectorList,
            XpsDigSigPartAlteringRestrictions       restrictions
            )
        {
            UpdateDocumentCache();
            // Add all documents
            foreach( IXpsFixedDocumentReader reader in _documentCache)
            {
                (reader as XpsFixedDocumentReaderWriter).
                    CollectSelfAndDependents( 
                        dependents,
                        selectorList,
                        restrictions);
            }
        }

        private
        void
        EnsureThumbnail()
        {
            if( _thumbnail == null )
            {
                _thumbnail = CurrentXpsManager.EnsureThumbnail( this, _metroPart );
            }
        }

        private
        IXpsFixedDocumentReader
        AddDocumentToCache(Uri documentUri)
        {
            //
            // Retrieve the requested part from the package
            //
            PackagePart documentPart = CurrentXpsManager.GetPart(documentUri);
            if (documentPart == null)
            {
                 throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_PartNotFound));
            }

            //
            // If the part is not a fixed document then throw an exception
            //
            if (!documentPart.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.FixedDocumentContentType))
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotAFixedDocument));
            }

            //
            // Create the reader/writer for the part
            //
            IXpsFixedDocumentReader  fixedDocument = new XpsFixedDocumentReaderWriter(CurrentXpsManager, null, documentPart, _documentCache.Count+1);

            //
            // Cache the new reader/writer for later
            //
            _documentCache.Add( fixedDocument );
            return fixedDocument;
        }


        #endregion Private methods

        #region Private data

        private PackagePart _metroPart;
        private PrintTicket _printTicket;

        private XmlPartEditor _partEditor;

        private List<IXpsFixedDocumentReader>_documentCache;

        //
        // This variable is used to keep a count of pages written via AddFixedDocument
        //
        private int _documentsWritten = 0;

        //
        // This variable flags whether the PrintTicket property is
        // committed.  A writer can only commit this property once.
        //
        private bool _isPrintTicketCommitted;

        //
        // These variables are used to keep track of the parent
        // and current child of this node for walking up and
        // down the current tree.  This is used be the flushing
        // policy to do interleave flushing of parts correctly.
        //
        private INode _parentNode;

        //
        // This variable flags wehter the documentCashe
        // has been populated by parsing the part for dependent documents
        private bool _hasParsedDocuments;

        private XpsThumbnail _thumbnail;

        #endregion Private data

        #region Internal properties

        /// <summary>
        /// Gets a reference to the XmlWriter for the internal Metro
        /// part that represents this document sequence.
        /// </summary>
        internal System.Xml.XmlWriter XmlWriter
        {
            get
            {
                _partEditor.DoesWriteStartEndTags = false;
                return _partEditor.XmlWriter;
            }
        }

        #endregion Internal properties

        #region INode implementation

        void
        INode.Flush(
            )
        {
            //
            // Commit the PrintTicket (if necessary)
            //
            CommitPrintTicket();

            //
            // Create the relationship between the package and the tumbnail
            //
            if( _thumbnail != null )
            {
                _thumbnail = null;
            }
}

        void
        INode.CommitInternal(
            )
        {
            CommitInternal();
        }

        PackagePart
        INode.GetPart(
            )
        {
            return _metroPart;
        }

        #endregion INode implementation

        #region IDisposable implementation

        void
        IDisposable.Dispose(
            )
        {
            CommitInternal();
        }

        #endregion IDisposable implementation
    }
}
