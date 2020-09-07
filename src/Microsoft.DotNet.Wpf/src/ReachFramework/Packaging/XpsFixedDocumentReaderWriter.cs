// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



    Abstract:
        This file contains the definition for IXpsFixedDocumentReader
        and IXpsFixedDocumentWriter interfaces as well as definition
        and implementation of XpsFixedDocumentReaderWriter.  These
        interfaces and class are used for writing fixed document
        parts to a Xps package.



--*/
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Printing;

using MS.Internal.IO.Packaging.Extensions;
using PackageRelationship = System.IO.Packaging.PackageRelationship;
using PackUriHelper = System.IO.Packaging.PackUriHelper;

namespace System.Windows.Xps.Packaging
{
    #region IXpsFixedDocumentReader interface definition

    /// <summary>
    /// Interface for reading fixed document parts from the Xps package.
    /// </summary>
    /// <remarks>Interface will be internal until reading/de-serialization is implemented.</remarks>
    public  interface IXpsFixedDocumentReader:
                      IDocumentStructureProvider
    {
        #region Public methods

        /// <summary>
        /// This method retrieves a fixed page part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="pageSource">
        /// The URI of the fixed page to retrieve.
        /// </param>
        /// <returns>
        /// Returns an interface to an IXpsFixedPageReader to read
        /// the requested fixed page.
        /// </returns>
        IXpsFixedPageReader
        GetFixedPage(
            Uri         pageSource
            );

        #endregion Public methods

        #region Public properties

        /// <summary>
        /// Gets the PrintTicket associated with this fixed document.
        /// </summary>
        /// <value>Value can be a PrintTicket or null.</value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">PrintTicket has already been committed.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">Property is not a valid PrintTicket instance.</exception>
        PrintTicket PrintTicket { get; }

        /// <summary>
        /// Gets the URI assigned to this fixed document part.
        /// </summary>
        /// <value>Value is a URI for the Metro part.</value>
        Uri Uri { get; }

        /// <summary>
        /// Gets a collection of all fixed pages that are contained within
        /// this fixed document.
        /// </summary>
        /// <value>
        /// Value is a Collection containing IXpsFixedPageReader interfaces.
        /// </value>
        ReadOnlyCollection<IXpsFixedPageReader> FixedPages { get; }

        /// <summary>
        /// 0 based document number in the document sequence
        /// </summary>
        int DocumentNumber{ get; }

        /// <summary>
        /// A list of signature definitions associated with the document
        /// </summary>
        ICollection<XpsSignatureDefinition>
        SignatureDefinitions{ get; }

        /// <summary>
        /// thumbnail image associated with this doucment
        /// </summary>
        XpsThumbnail
        Thumbnail{ get; }

        /// <summary>
        /// Document Structure associated with this doucment
        /// </summary>
        XpsStructure
        DocumentStructure{ get; }


        #endregion Public properties

        /// <summary>
        /// Adds the passed Signature Definiton to the cached
        /// Signature Definition list
        /// Will not be written until FlushSignatureDefinition
        /// is called
        /// </summary>
        /// <param name="signatureDefinition">
        /// the Signature Definition to be added
        /// </param>
        void
        AddSignatureDefinition(
            XpsSignatureDefinition signatureDefinition
            );

        /// <summary>
        /// This method removes a signature definitions associated with
        /// the FixedDocument
        /// </summary>
        /// <param name="signatureDefinition">
        /// Signature Definition to remove
        /// </param>
        void
        RemoveSignatureDefinition(XpsSignatureDefinition signatureDefinition);

        /// <summary>
        /// Writes out all modifications to Signature
        /// Definitions as well as new Signature Definitions
        /// </summary>
        void
        CommitSignatureDefinition();
    }

    #endregion IXpsFixedDocumentReader interface definition

    #region IXpsFixedDocumentWriter interface definition

    /// <summary>
    /// Interface for writing fixed document parts to the xps package.
    /// </summary>
    public interface IXpsFixedDocumentWriter:
                     IDocumentStructureProvider
    {
        #region Public methods

        /// <summary>
        /// This method adds a fixed page part to the Xps package
        /// and associates it with the current fixed document.
        /// </summary>
        /// <returns>
        /// Returns an interface to the newly created fixed page.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The FixedDocument has already been disposed</exception>
        /// <exception cref="SRID.ReachPackaging_PanelOrSequenceAlreadyOpen">FixedPage is not completed.</exception>
        IXpsFixedPageWriter
        AddFixedPage(
            );

        /// <summary>
        /// This method adds a thumbnail to the current DocumentSequence.
        ///
        /// There can only be one thumbnail attached to the DocumentSequence.
        ///  Calling this method when there
        /// is already a starting part causes InvalidOperationException to be
        /// thrown.
        /// </summary>
        /// <returns>Returns a XpsThumbnail instance.</returns>
        XpsThumbnail
        AddThumbnail(
            XpsImageType imageType
            );

        /// <summary>
        /// This method commits any changes not already committed for this
        /// fixed document.
        ///
        /// </summary>
        void
        Commit(
            );

        #endregion Public methods

        #region Public properties

        /// <summary>
        /// Sets the PrintTicket associated with this fixed document.
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
        /// Gets the URI assigned to this fixed document part.
        /// </summary>
        /// <value>Value is a URI for the Metro part.</value>
        Uri Uri { get; }

        /// <summary>
        /// 0 based document number in the document sequence
        /// </summary>
        int DocumentNumber{ get; }

        #endregion Public properties
    }

    #endregion IXpsFixedDocumentWriter interface definition

    /// <summary>
    /// This class implements the reading and writing functionality for
    /// a fixed document within a Xps package.
    /// </summary>
    internal sealed class XpsFixedDocumentReaderWriter : XpsPartBase,
                          IXpsFixedDocumentReader,
                          IXpsFixedDocumentWriter,
                          INode,
                          IDisposable
    {
        #region Constructors

        /// <summary>
        /// Internal constructor for the XpsFixedDocumentReaderWriter class.
        /// This class is created internally so we can keep the Xps hierarchy
        /// completely under control when using these APIs.
        /// </summary>
        /// <param name="xpsManager">
        /// The XpsManager for the current Xps package.
        /// </param>
        /// <param name="parent">
        /// The parent node of this fixed document.
        /// </param>
        /// <param name="part">
        /// The internal Metro part that represents this fixed document.
        /// </param>
        /// <param name="documentNumber">
        ///  The 0 base document number in the document sequence
        /// </param>
        /// <exception cref="ArgumentNullException">part is null.</exception>
        internal
        XpsFixedDocumentReaderWriter(
            XpsManager    xpsManager,
            INode           parent,
            PackagePart     part,
            int             documentNumber
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

            _pageCache = new List<IXpsFixedPageReader>();
            
            _pagesWritten = 0;

            _parentNode = parent;

            _hasParsedPages = false;

            _documentNumber = documentNumber;
       }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets or sets the PrintTicket to be stored with this fixed document.
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
                    _printTicket = CurrentXpsManager.EnsurePrintTicket( Uri );
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
        /// Gets a collection of all fixed pages that are contained within
        /// this fixed document.
        /// </summary>
        /// <value>
        /// Value is a Collection containing IXpsFixedPageReader interfaces.
        /// </value>
        /// <remarks>
        /// This is only internal until it is implemented.
        /// </remarks>
        public ReadOnlyCollection<IXpsFixedPageReader> FixedPages
        {
            get
            {
                UpdatePageCache();
                return new ReadOnlyCollection<IXpsFixedPageReader>(_pageCache );
            }
        }

        /// <summary>
        /// A list of signature definitions associated with the document
        /// </summary>
        public
        ICollection<XpsSignatureDefinition>
        SignatureDefinitions
        {
            get
            {
                EnsureSignatureDefinitions();
                return _signatureDefinitions;
            }
        }

        /// <summary>
        /// 0 based document number in the document sequence
        /// </summary>
         public int DocumentNumber
         {
            get
            {
                return _documentNumber;
            }
         }

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

        public
        XpsStructure
        DocumentStructure
        {
            get
            {
                EnsureDoucmentStructure();
                return _documentStructure;
            }
        }

        #endregion Public properties

        #region Public methods
        /// <summary>
        /// This method adds a fixed page part to the Xps package
        /// and associates it with the current fixed document.
        /// </summary>
        /// <returns>
        /// Returns an interface to the newly created fixed page.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The FixedDocument has already been disposed</exception>
        /// <exception cref="SRID.ReachPackaging_PanelOrSequenceAlreadyOpen">FixedPage is not completed.</exception>
        public
        IXpsFixedPageWriter
        AddFixedPage(
            )
        {
            if (null == _metroPart || null == CurrentXpsManager.MetroPackage)
            {
                throw new ObjectDisposedException("XpsFixedDocumentReaderWriter");
            }

            //
            // Only one page can be created/written at a time.
            //
            if (null != _currentPage)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_PanelOrSequenceAlreadyOpen));
            }

            
            _linkTargetStream = new List<String>();

            //
            // Create the part and writer
            //
            PackagePart metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.FixedPageContentType);
            XpsFixedPageReaderWriter fixedPage = new XpsFixedPageReaderWriter(CurrentXpsManager, this, metroPart, _linkTargetStream, _pagesWritten + 1);

            //
            // Make the new page the current page
            //
            _currentPage = fixedPage;

             
            //Here we used to add the fixed page to _pageCache, but _pageCache is never accessed if this object was created as an IXpsFixedDocumentWriter.
            //So instead keep a separate pagesWritten count and forget about the cache when using this method.
            _pagesWritten++;
            return fixedPage;
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
            XpsImageType imageType 
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
        /// </summary>
        public
        XpsStructure
        AddDocumentStructure(
            )
        {
            if (this.DocumentStructure != null)
            {
                // Document structure already available for this FixedDocument
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_MoreThanOneDocStructure));
            }

            Uri pageUri = this.CurrentXpsManager.CreateStructureUri();
            //
            // Create the part and writer
            //
            PackagePart metroPart = this.CurrentXpsManager.GeneratePart(
                        XpsS0Markup.DocumentStructureContentType,
                        pageUri);

            _documentStructure = new XpsStructure(CurrentXpsManager, this, metroPart);

            //
            // Create the relationship between the document and the document-structure
            // Not in INode.Flush because IXpsFixedDocumentReader has no commit.
            //

            string structurePath = XpsManager.MakeRelativePath(this.Uri, _documentStructure.Uri);

            _metroPart.CreateRelationship(new Uri(structurePath, UriKind.Relative),
                                             TargetMode.Internal,
                                             XpsS0Markup.StructureRelationshipName
                                           );

            return _documentStructure;
        }

        /// <summary>
        /// This method adds a relationship for this part that
        /// targets the specified Uri and is based on the specified
        /// content type.
        /// </summary>
        /// <param name="targetUri">
        /// Uri target for relationship.
        /// </param>
        /// <param name="relationshipName">
        /// Relationship type to add.
        /// </param>
        public
        void
        AddRelationship(
            Uri         targetUri,
            string      relationshipName
            )
        {
            //
            // We can not read from the file to do validation
            // when streaming
            //
            if( !CurrentXpsManager.Streaming )
            {
                foreach (PackageRelationship rel in _metroPart.GetRelationships())
                {
                    if (rel.TargetUri.Equals(targetUri))
                    {
                        //
                        // Relationship already exists
                        //
                        return;
                    }
                }
            }

            //
            // Add the relationship using a relative path to this page.
            //
            string relativePath = XpsManager.MakeRelativePath(this.Uri, targetUri);
            _metroPart.CreateRelationship(new Uri(relativePath, UriKind.Relative),
                                               TargetMode.Internal,
                                               relationshipName);
        }

        /// <summary>
        /// This method retrieves a fixed page part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="pageUri">
        /// The URI of the fixed page to retrieve.
        /// </param>
        /// <returns>
        /// Returns an interface to an IXpsFixedPageReader to read
        /// the requested fixed page.
        /// </returns>
        public
        IXpsFixedPageReader
        GetFixedPage(
            Uri         pageUri
            )
        {
            UpdatePageCache();
            IXpsFixedPageReader pageReader = null;

            foreach (IXpsFixedPageReader reader in _pageCache )
            {
                if( reader.Uri == pageUri )
                {
                    pageReader =  reader;
                }
            }

            return pageReader;
        }

        public
        void
        AddSignatureDefinition(
            XpsSignatureDefinition signatureDefinition
            )
        {
            EnsureSignatureDefinitions();
            _signatureDefinitions.Add( signatureDefinition );
            _sigCollectionDirty = true;
}

        public
        void
        CommitSignatureDefinition()
        {
            bool isDirty = false;

            //
            // if the collection is dirty not point in testing
            // each signature.
            //
            if( !_sigCollectionDirty )
            {
                foreach( XpsSignatureDefinition sigDef in _signatureDefinitions )
                {
                    if( sigDef.HasBeenModified )
                    {
                        isDirty = true;
                        break;
                    }
                }
            }
            if( isDirty || _sigCollectionDirty )
            {
                WriteSignatureDefinitions();
            }
        }

        /// <summary>
        /// This method removes a signature definitions associated with
        /// the FixedDocument
        /// </summary>
        /// <param name="signatureDefinition">
        /// Signature Definition to remove
        /// </param>
        public
        void
        RemoveSignatureDefinition(XpsSignatureDefinition signatureDefinition)
        {
           EnsureSignatureDefinitions();
           _signatureDefinitions.Remove( signatureDefinition );
           _sigCollectionDirty = true;
}

        /// <summary>
        /// This method commits any changes not already committed for this
        /// fixed document.
        ///
        /// NOTE:  This commits changes to all child object under this
        /// branch of the tree.  No further changes will be allowed.
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
        /// fixed document.
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
                if (null != _partEditor.XmlWriter)
                {
                    if(_partEditor.DoesWriteStartEndTags)
                    {
                        if(_partEditor.IsStartElementWritten)
                        {
                            _partEditor.XmlWriter.WriteEndElement();
                            _partEditor.XmlWriter.WriteEndDocument();
                        }
                    }
                }

                ((INode)this).Flush();
                _partEditor.Close();

 
                _partEditor     = null;
                _metroPart      = null;

                _parentNode     = null;

                _thumbnail      = null;

                _pageCache      = null;
                
                _pagesWritten     = 0;

                _hasParsedPages = false;
           }

            base.CommitInternal();
        }



        /// <summary>
        /// Adds itself and and its reationship if it exists
        /// Adds dependent part Uris to the passed list following the passed restrictions
        /// dependents include pages, annotaions, properties, and signatures
        /// </summary>
        internal
        void
        CollectSelfAndDependents(
            Dictionary<Uri,Uri>                 dependentList,
            List<PackageRelationshipSelector>   selectorList,
            XpsDigSigPartAlteringRestrictions   restrictions
            )
        {
            //
            // Add this document
            //
            dependentList[Uri] = Uri;

            //
            // Add Signature Definition if it exists
            //
            PackagePart signatureDefinitionPart =
                CurrentXpsManager.GetSignatureDefinitionPart(Uri);
            
            //
            // Add Signature Definitions
            //
            selectorList.Add( new PackageRelationshipSelector(
                                    Uri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.SignatureDefinitionRelationshipName
                                    ) 
                                 );
            

            if( signatureDefinitionPart != null )
            {
                dependentList[signatureDefinitionPart.Uri] = signatureDefinitionPart.Uri;
            }
            //
            // Add Restricted Font relationship
            //
            selectorList.Add( new PackageRelationshipSelector(
                                    Uri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.RestrictedFontRelationshipType
                                    ) 
                                 );
            //
            // Add Document Structure relationship
            //
            selectorList.Add( new PackageRelationshipSelector(
                                    Uri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.StructureRelationshipName
                                    ) 
                                 );
            //
            // Add this documents dependants
            //
            CollectDependents( dependentList, selectorList, restrictions);
}

        internal
        void
        CollectXmlPartsAndDepenedents(
            List<PackagePart> xmlPartList
            )
        {
            //
            // Add my self to be tested for V&E Markup
            //
            xmlPartList.Add(_metroPart);
            UpdatePageCache();
            //
            // Add all pages
            //
            foreach (IXpsFixedPageReader reader in _pageCache)
            {
                (reader as XpsFixedPageReaderWriter).CollectXmlPartsAndDepenedents(xmlPartList);
            }
            //
            // Add DocumentStructure
            //
            EnsureDoucmentStructure();
            if (_documentStructure != null)
            {
                //
                // Add my DocumentStructure to be tested for V&E Markup
                //
                xmlPartList.Add((_documentStructure as INode).GetPart());
            }
            //
            // Add Signature Definition if it exists
            //
            PackagePart signatureDefinitionPart =
                CurrentXpsManager.GetSignatureDefinitionPart(Uri);
            if (signatureDefinitionPart != null)
            {
                //
                // Add my signatureDefinitionPart to be tested for V&E Markup
                //
                xmlPartList.Add(signatureDefinitionPart);
            }
        }

        /// <summary>
        /// Adds dependent part Uris to the passed list
        /// </summary>
        internal
        void
        CollectDependents(
            Dictionary<Uri,Uri>                 dependents,
            List<PackageRelationshipSelector>   selectorList,
            XpsDigSigPartAlteringRestrictions   restrictions
            )
        {
            UpdatePageCache();
            //
            // Add all pages
            //
            foreach( IXpsFixedPageReader reader in _pageCache)
            {
                (reader as XpsFixedPageReaderWriter).
                    CollectSelfAndDependents( 
                    dependents,
                    selectorList,
                    restrictions
                    );
            }

            //
            // Add DocumentStructure
            //
            EnsureDoucmentStructure();
            if( _documentStructure != null )
            {
                dependents[_documentStructure.Uri] = _documentStructure.Uri;
            }
       }

        #endregion Public methods

        #region Private methods

        private
        void
        AddPageToDocument(
            Uri                 partUri,
            IList<String>       linkTargetStream
            )
        {
            _partEditor.PrepareXmlWriter(XpsS0Markup.FixedDocument, XpsS0Markup.FixedDocumentNamespace);
            XmlTextWriter xmlWriter = _partEditor.XmlWriter;
            //
            // Write <PageContent Target="partUri"/>
            //
            String relativePath = XpsManager.MakeRelativePath(Uri, partUri);

            xmlWriter.WriteStartElement(XpsS0Markup.PageContent);
            xmlWriter.WriteAttributeString(XmlTags.Source, relativePath);

            //
            // Write out link targets if necessary
            //
            if (linkTargetStream.Count != 0)
            {
                xmlWriter.WriteRaw ("<PageContent.LinkTargets>");
                foreach (String nameElement in linkTargetStream)
                {
                     xmlWriter.WriteRaw (String.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "<LinkTarget Name=\"{0}\" />", 
                        nameElement)
                        );
                }
                xmlWriter.WriteRaw ("</PageContent.LinkTargets>");
            }

            xmlWriter.WriteEndElement();
        }
        /// <summary>
        /// This method writes the PrintTicket associated with
        /// this fixed document to the Metro package.
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
                    CurrentXpsManager.WritePrintTicket(this,_metroPart, _printTicket);
                }
                else
                {
                    CurrentXpsManager.WriteEmptyPrintTicket(this, _metroPart);
                }
                _isPrintTicketCommitted = true;
            }
        }

        /// <summary>
        /// CurrentPageCommitted signals that our current page
        /// is complete and we can write out any associated link
        /// targets and end the PageContent element.
        /// </summary>
        internal
        void
        CurrentPageCommitted()
        {
            if( _currentPage != null )
            {
                //Write out the fixed page tag
                AddPageToDocument(_currentPage.Uri, _linkTargetStream);
                _currentPage = null;
            }
        }
         
        /// <summary>
        /// Test if the page cache has been initialized  and
        /// updates it if necessary
        /// </summary>
        private
        void
        UpdatePageCache()
        {
            if( !_hasParsedPages )
            {
                ParsePages();
                _hasParsedPages = true;
            }
        }
        /// <summary>
        /// This method parses the part pulling out the Page Referneces
        /// and populates the _pageCache
        /// </summary>
        private
        void
        ParsePages()
        {
            using (Stream stream = _metroPart.GetStream(FileMode.Open))
            {
                //
                // If the stream is empty there are no pages to parse
                //
                if (stream.Length > 0)
                {
                    XmlTextReader reader = new XmlTextReader(stream);

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == XpsS0Markup.PageContent)
                        {
                            string attribute = reader.GetAttribute(XmlTags.Source);
                            if (attribute != null)
                            {
                                Uri relativeUri = new Uri(attribute, UriKind.Relative);
                                AddPageToCache(PackUriHelper.ResolvePartUri(Uri, relativeUri));
                            }
                        }
                    }
                }
            }
        }

        private
        IXpsFixedPageReader
        AddPageToCache( Uri pageUri )
        {
            PackagePart pagePart = CurrentXpsManager.GetPart(pageUri);

            if (pagePart == null)
            {
                 throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_PartNotFound));
            }
            
            if (!pagePart.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.FixedPageContentType))
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotAFixedPage));
            }

            //
            // Create the reader/writer for the part
            //
            IXpsFixedPageReader pageReader = new XpsFixedPageReaderWriter(CurrentXpsManager, this, pagePart, null, _pageCache.Count+1);

            //
            // Cache the new reader/writer for later
            //
            _pageCache.Add(pageReader );

            return pageReader;
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
        void
        EnsureSignatureDefinitions()
        {
            // if _xpsSignaturs is not null we have already initialized this
            //
            if( null != _signatureDefinitions)
            {
                return;
            }
            _signatureDefinitions = new Collection<XpsSignatureDefinition>();
            PackagePart sigDefPart =
                CurrentXpsManager.GetSignatureDefinitionPart(Uri);
            if( sigDefPart != null )
            {
                ParseSignaturePart( sigDefPart, _signatureDefinitions );
            }
        }

        private
        void
        EnsureDoucmentStructure()
        {
            // if _xpsSignaturs is not null we have already initialized this
            //
            if( null != _documentStructure)
            {
                return;
            }
            PackageRelationship documentStructureRelationship = null;
            PackagePart documentStructurePart = null;

            foreach (PackageRelationship rel in _metroPart.GetRelationshipsByType(XpsS0Markup.StructureRelationshipName))
            {
                if (documentStructureRelationship != null)
                {
                    throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOneDocStructure));
                }

                documentStructureRelationship = rel;
            }

            if (documentStructureRelationship != null)
            {
                Uri documentStructureUri = PackUriHelper.ResolvePartUri(documentStructureRelationship.SourceUri, 
                                                                documentStructureRelationship.TargetUri);

                if (CurrentXpsManager.MetroPackage.PartExists(documentStructureUri))
                {
                    documentStructurePart = CurrentXpsManager.MetroPackage.GetPart(documentStructureUri);
                    _documentStructure = new XpsStructure(CurrentXpsManager, this, documentStructurePart);
                }
            }
        }
        private
        void
        ParseSignaturePart(
            PackagePart                         sigDefPart,
            Collection<XpsSignatureDefinition>  sigDefCollection
            )
        {
            using (XmlTextReader reader = new XmlTextReader(sigDefPart.GetStream(FileMode.Open)))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element &&
                        reader.Name == XpsS0Markup.SignatureDefinitions
                      )
                    {
                        ParseSignatureDefinitions(reader, sigDefCollection);
                    }
                }
            }
        }

        private
        void
        ParseSignatureDefinitions(
            XmlReader                           reader,
            Collection<XpsSignatureDefinition>  sigDefCollection
            )
        {
            bool endLoop = false;
            while (!endLoop && reader.Read())
            {
               if( reader.NodeType == XmlNodeType.Element &&
                    reader.Name == XpsS0Markup.SignatureDefinition
                  )
                {
                    XpsSignatureDefinition sigDef = new XpsSignatureDefinition();
                    sigDef.ReadXML(reader);
                    sigDefCollection.Add( sigDef );
                }

               if( reader.NodeType == XmlNodeType.EndElement &&
                    reader.Name == XpsS0Markup.SignatureDefinitions
                  )
                {
                    endLoop = true;
                }
            }
        }

        private
        void
        WriteSignatureDefinitions()
        {
            PackagePart sigDefPart =
                CurrentXpsManager.GetSignatureDefinitionPart(Uri);
            if( sigDefPart == null )
            {
                sigDefPart = CurrentXpsManager.AddSignatureDefinitionPart( _metroPart );
            }

            using (Stream stream = sigDefPart.GetStream(FileMode.Create))
            using (XmlTextWriter writer = new XmlTextWriter(stream, System.Text.Encoding.UTF8))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement(XpsS0Markup.SignatureDefinitions,
                XpsS0Markup.SignatureDefinitionNamespace);
                foreach (XpsSignatureDefinition sigDef in _signatureDefinitions)
                {
                    sigDef.WriteXML(writer);
                }
                writer.WriteEndElement();
            }

            _sigCollectionDirty = false;
        }



        #endregion Private methods

        #region Private data

        private PackagePart _metroPart;
        private PrintTicket _printTicket;

        private XmlPartEditor _partEditor;

        private IList<String> _linkTargetStream;

        private List<IXpsFixedPageReader> _pageCache;
        
        //
        // This variable is used to keep a count of pages written via AddFixedPage
        //
        private int _pagesWritten;

        //
        // This variable flags whether the PrintTicket property is
        // committed.  A writer can only commit this property once.
        //
        private bool                                _isPrintTicketCommitted;

        //
        // These variables are used to keep track of the parent
        // and current child of this node for walking up and
        // down the current tree.  This is used be the flushing
        // policy to do interleave flushing of parts correctly.
        //
        private INode                               _parentNode;

        //
        // This variable flags wehter the pageCashe
        // has been populated by parsing the part for dependent pages
        //
        private bool                                _hasParsedPages;

        //
        // 0 based document number in the document sequence
        //
        private int                                 _documentNumber;

        private XpsThumbnail                        _thumbnail;


        //
        // Since the current page may add link target information
        // we must track our current page
        // for this reason we can not handle adding new page
        // until the the current page has been committed
        //
        private XpsFixedPageReaderWriter            _currentPage;
        //
        // A cached list of Signature Definitions
        //
        private Collection<XpsSignatureDefinition>  _signatureDefinitions;
        //
        // Boolean indicating whetehr _signatureDefinitions collection
        // has been changed
        //
        private bool                                _sigCollectionDirty;
        
        private XpsStructure                        _documentStructure;
        #endregion Private data

        #region Internal properties

        /// <summary>
        /// Gets a reference to the XmlWriter for the Metro part
        /// that represents this fixed document.
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

