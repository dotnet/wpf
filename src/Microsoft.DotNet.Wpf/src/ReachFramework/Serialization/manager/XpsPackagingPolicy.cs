// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file contains the definition of a theclass that controls
        the serialization packaging policy. It is the buffering layer
        between the serializer and the package
--*/
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
using System.Windows.Media.Imaging;
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class encapsulating both a Stream and Uri reprenting
    /// the main important elements in a Resource
    /// </summary>
    public class XpsResourceStream
    {
        #region Constructor

        /// <summary>
        /// Construct resource stream with the stream and uri
        /// </summary>
        /// <param name="stream">Stream to initialze the Resource Stream with</param>
        /// <param name="uri">Uri of the stream</param>
        public
        XpsResourceStream(
            Stream stream,
            Uri    uri
            )
        {
            this._stream = stream;
            this._uri    = uri;
        }

        #endregion Constructor

        #region Public Properties

        /// <summary>
        /// return the stream back to the caller.
        /// </summary>
        public
        Stream
        Stream
        {
            get
            {
                return _stream;
            }
        }

        /// <summary>
        /// return the uri back to the caller.
        /// </summary>
        public
        Uri
        Uri
        {
            get
            {
                return _uri;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Initializes resource stream
        /// </summary>
        public
        void
        Initialize(
            )
        {
            _uri    = null;
            _stream = null;
        }

        #endregion Public Methods

        private
        Stream  _stream;

        private
        Uri     _uri;
    };

    /// <summary>
    /// This class is created in order to allow sharing of resources
    /// across different parts of the package. It keeps an internal
    /// ref count of the resource usage.
    /// </summary>
    internal class ResourceStreamCacheItem
    {
        #region Constructor

        internal
        ResourceStreamCacheItem(
            )
        {
            this._resourceStreamRefCount = 1;
        }

        #endregion Constructor

        #region Internal Properties

        internal
        XpsResourceStream
        XpsResourceStream
        {
            get
            {
                return _resourceStream;
            }

            set
            {
                _resourceStream = value;
            }
        }

        internal
        XpsResource
        XpsResource
        {
            get
            {
                return _reachResource;
            }

            set
            {
                _reachResource = value;
            }
        }

        #endregion Internal Properties

        #region Internal Methods

        internal
        void
        IncRef(
            )
        {
            _resourceStreamRefCount++;
        }

        internal
        int
        Release(
            )
        {
            if(_resourceStreamRefCount>0)
            {
                _resourceStreamRefCount--;
            }

            return _resourceStreamRefCount;
        }

        #endregion Internal Methods


        #region Private Data

        private
        XpsResourceStream  _resourceStream;

        private
        XpsResource   _reachResource;

        private
        int             _resourceStreamRefCount;

        #endregion Private Data
    };


    /// <summary>
    /// Base Class representing a packaging policy. It mainly
    /// defines methods that help acquiring readers / writers
    /// to different part types in the reach package.
    /// </summary>
    public abstract class BasePackagingPolicy :
                                 IDisposable
    {
        #region Constructor

        /// <summary>
        /// Instantiates a BasePackagingPolicy
        /// </summary>
        protected
        BasePackagingPolicy(
            )
        {

        }

        #endregion Constructor


        #region Public Base methods

         /// <summary>
         /// Acquire an Xml Writer for a DocumentSequence
         /// </summary>
        public
        abstract
        XmlWriter
        AcquireXmlWriterForFixedDocumentSequence(
            );

         /// <summary>
         /// Release a reference to the current DocumentSequence
         /// </summary>
        public
        abstract
        void
        ReleaseXmlWriterForFixedDocumentSequence(
            );

         /// <summary>
         /// Acquire a XmlWriter for a FixedDocument 
         /// </summary>
        public
        abstract
        XmlWriter
        AcquireXmlWriterForFixedDocument(
            );

         /// <summary>
         /// Release a reference to the current FixedDocument
         /// </summary>
        public
        abstract
        void
        ReleaseXmlWriterForFixedDocument(
            );

         /// <summary>
         /// Acquire an XmlWriter for a FixedPage
         /// </summary>
        public
        abstract
        XmlWriter
        AcquireXmlWriterForFixedPage(
            );

         /// <summary>
         /// Release a reference to the current XmlWriter
         /// </summary>
        public
        abstract
        void
        ReleaseXmlWriterForFixedPage(
            );

            
        /// <summary>
        ///
        /// </summary>
        public
        abstract
        void
        RelateResourceToCurrentPage(
            Uri     targetUri,
            string  relationshipName
            );


        /// <summary>
        /// This method adds a relationship to the current active
        /// document using the specified target and relationship name.
        /// </summary>
        /// <param name="targetUri">
        /// Uri to Target for relationship.
        /// </param>
        public
        abstract
        void
        RelateRestrictedFontToCurrentDocument(
            Uri targetUri
            );

        /// <summary>
        ///
        /// </summary>
        public
        abstract
        void
        PersistPrintTicket(
            PrintTicket printTicket
            );

        /// <summary>
        ///
        /// </summary>
        public
        abstract
        XmlWriter
        AcquireXmlWriterForPage(
            );


        /// <summary>
        ///
        /// </summary>
        public
        abstract
        XmlWriter
        AcquireXmlWriterForResourceDictionary(
            );

        /// <summary>
        ///
        /// </summary>
        public
        abstract
        IList<String>
        AcquireStreamForLinkTargets(
            );



        /// <summary>
        ///
        /// </summary>
        public
        abstract
        void
        PreCommitCurrentPage(
            );

        /// <summary>
        /// Acquire a ResourceStream for a XpsFont
        /// </summary>
        public
        abstract
        XpsResourceStream
        AcquireResourceStreamForXpsFont(
            );

        /// <summary>
        /// Acquire a ResourceStream for a XpsFont
        /// </summary>
        /// <param name="resourceId">
        /// Id of Font
        /// </param>
        public
        abstract
        XpsResourceStream
        AcquireResourceStreamForXpsFont(
            String resourceId
            );

        /// <summary>
        /// Release a reference to the current XpsFont ResourceStream
        /// </summary>
        public
        abstract
        void
        ReleaseResourceStreamForXpsFont(
            );     

        /// <summary>
        /// Release a reference to the current XpsFont ResourceStream
        /// </summary>
        /// <param name="resourceId">
        /// Id of Font to release
        /// </param>
        public
        abstract
        void
        ReleaseResourceStreamForXpsFont(
            String resourceId
            );

        /// <summary>
        /// Acquire a ResourceStream for a XpsImage
        /// </summary>
        /// <param name="resourceId">
        /// Id of XpsImage
        /// </param>
        public
        abstract
        XpsResourceStream
        AcquireResourceStreamForXpsImage(
            String resourceId
            );

        /// <summary>
        /// Release a reference of the current XpsImage ResourceStream
        /// </summary>
        public
        abstract
        void
        ReleaseResourceStreamForXpsImage(
            );    

        /// <Summary>
        /// Acquire a ResourceSTream for a XpsColorContext
        /// </Summary>
        /// <param name="resourceId">
        /// A id for the ResourceStream
        /// </param>
        public
        abstract
        XpsResourceStream
        AcquireResourceStreamForXpsColorContext(
            String resourceId
            );

        /// <Summary>
        /// Release a reference to the current XpsColorContext ResourceStream
        /// </Summary>
        public
        abstract
        void
        ReleaseResourceStreamForXpsColorContext(
            );

        /// <Summary>
        /// Acquire a ResoureSTream for a XpsResourceDictionary
        /// </Summary>
        /// <param name="resourceId">
        /// A resource Id for the ResourceStream
        /// </param>
        public
        abstract
        XpsResourceStream
        AcquireResourceStreamForXpsResourceDictionary(
            String resourceId
            );

        /// <Summary>
        /// Release a reference to the current XpsResourceDictionary
        /// </Summary>
        public
        abstract
        void
        ReleaseResourceStreamForXpsResourceDictionary(
            );    

        #endregion Public Base Methods

        #region Public Properties

         /// <summary>
        ///
        /// </summary>
        public
        abstract
        Uri
        CurrentFixedDocumentUri
        {
            get;
        }

        /// <summary>
        ///
        /// </summary>
        public
        abstract
        Uri
        CurrentFixedPageUri
        {
            get;
        }
        #endregion Public Properties

        void
        IDisposable.Dispose(
            )
        {
            GC.SuppressFinalize(this);
        }
    };


    /// <summary>
    /// A class implementing a Xps specific packaging policy
    /// </summary>
    public class XpsPackagingPolicy :
                 BasePackagingPolicy
    {
        #region Constructor

        /// <summary>
        /// Instantiate a XpsPackagingPolicy class
        /// </summary>
        /// <exception cref="ArgumentNullException">reachPackage is NULL.</exception>
        public
        XpsPackagingPolicy(
            System.Windows.Xps.Packaging.XpsDocument xpsPackage
            ): this( xpsPackage, PackageInterleavingOrder.None )
        {
        }

        /// <summary>
        /// Instantiate a XpsPackagingPolicy class
        /// </summary>
        /// <exception cref="ArgumentNullException">reachPackage is NULL.</exception>
        public
        XpsPackagingPolicy(
            System.Windows.Xps.Packaging.XpsDocument xpsPackage,
            PackageInterleavingOrder                 interleavingType
            ):
        base()
        {
            if( xpsPackage == null)
            {
                throw new ArgumentNullException("xpsPackage");
            }                
            
            this._reachPackage = xpsPackage;
            Initialize();

            _interleavingPolicy = new XpsInterleavingPolicy(interleavingType, true);
            _interleavingPolicy.AddItem((INode)xpsPackage,0,null);
            
            _fontAcquireMode = ResourceAcquireMode.NoneAcquired;
            _fontsCache = new Hashtable(11);

            _fontResourceStream = null;
            _imageResourceStream = null;
            _colorContextResourceStream = null;
            _resourceDictionaryResourceStream = null;

            InitializeResourceReferences();
        }

        #endregion Constructor

        #region Public Methods

        /// <Summary>
        /// Acquire an Xml Writer for a DocumentSequence
        /// </Summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForFixedDocumentSequence(
            )
        {
            XmlWriter xmlWriter = null;

            if(_currentDocumentSequenceWriterRef == 0)
            {
                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the writer
                //
                _currentFixedDocumentSequenceWriter = _reachPackage.AddFixedDocumentSequence();

                //
                // retreive the appropriate writer from the reach package api layer
                //
                if(_currentFixedDocumentSequenceWriter != null)
                {
                     _currentDSWriter = ((XpsFixedDocumentSequenceReaderWriter)_currentFixedDocumentSequenceWriter).XmlWriter;
                     _interleavingPolicy.AddItem((INode)_currentFixedDocumentSequenceWriter, 0, (INode) _reachPackage);
                }
            }

            _currentDocumentSequenceWriterRef++;

            xmlWriter = _currentDSWriter;

            return xmlWriter;
        }

        /// <Summary>
        /// Release a reference to the current DocumentSequence
        /// </Summary>
        public
        override
        void
        ReleaseXmlWriterForFixedDocumentSequence(
            )
        {
            if(_currentFixedDocumentSequenceWriter != null &&
               _currentDocumentSequenceWriterRef > 0)
            {
                _currentDocumentSequenceWriterRef--;

                if(_currentDocumentSequenceWriterRef == 0)
                {
                    //
                    // if any of the other low level writer exist, then
                    // throw an exception or is it better if we just close
                    // them and consider that if any additional call on them
                    // would be the one that throws the expcetion
                    //
                    _interleavingPolicy.Commit((INode) _currentFixedDocumentSequenceWriter);
                    Initialize();
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        /// Acquire a XmlWriter for a FixedDocument 
        /// </Summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForFixedDocument(
            )
        {
            XmlWriter xmlWriter = null;

            if(_currentFixedDocumentWriterRef == 0)
            {
                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the writer
                //
                if(_currentFixedDocumentSequenceWriter != null)
                {
                    _currentFixedDocumentWriter = _currentFixedDocumentSequenceWriter.AddFixedDocument();
                    _interleavingPolicy.AddItem((INode)_currentFixedDocumentWriter, 0, (INode) _currentFixedDocumentSequenceWriter);
                }
                //
                // retreive the appropriate writer from the reach package api layer
                //
                if(_currentFixedDocumentWriter!=null)
                {
                     _currentFDWriter = ((XpsFixedDocumentReaderWriter)_currentFixedDocumentWriter).XmlWriter;
                }
            }

            _currentFixedDocumentWriterRef++;

            xmlWriter = _currentFDWriter;

            return xmlWriter;
        }

        /// <Summary>
        /// Release a reference to the current FixedDocument
        /// </Summary>
        public
        override
        void
        ReleaseXmlWriterForFixedDocument(
            )
        {
            if(_currentFixedDocumentWriter != null &&
               _currentFixedDocumentWriterRef > 0)
            {
                _currentFixedDocumentWriterRef--;

                if(_currentFixedDocumentWriterRef == 0)
                {
                    //
                    // if any of the other low level writer exist, then
                    // throw an exception or is it better if we just close
                    // them and consider that if any additional call on them
                    // would be the one that throws the expcetion
                    //
                    _interleavingPolicy.Commit((INode) _currentFixedDocumentWriter);
                    //
                    // All current lower references should be cleared
                    //
                    _currentFixedDocumentWriter  = null;
                    _currentFixedPageWriter      = null;
                    _currentFixedPageWriterRef   = 0;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        /// Acquire an XmlWriter for a FixedPage
        /// </Summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForFixedPage(
            )
        {
            XmlWriter xmlWriter = null;

            if(_currentFixedPageWriterRef == 0)
            {
                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the writer
                //
                if(_currentFixedDocumentWriter != null)
                {
                    _currentFixedPageWriter = _currentFixedDocumentWriter.AddFixedPage();
                    _interleavingPolicy.AddItem((INode)_currentFixedPageWriter, 0, (INode)_currentFixedDocumentWriter);
                }

                //
                // retreive the appropriate writer from the reach package api layer
                //
                if(_currentFixedPageWriter != null)
                {
                     _currentFPWriter = ((XpsFixedPageReaderWriter)_currentFixedPageWriter).XmlWriter;
                }
            }

            _currentFixedPageWriterRef++;

            xmlWriter = _currentFPWriter;

            return xmlWriter;
        }

        /// <Summary>
        /// Release a reference to the current XmlWriter
        /// </Summary>
        public
        override
        void
        ReleaseXmlWriterForFixedPage(
            )
        {
            if(_currentFixedPageWriter!=null &&
               _currentFixedPageWriterRef>0)
            {
                _currentFixedPageWriterRef--;

                if(_currentFixedPageWriterRef == 0)
                {
                    //
                    // if any of the other low level writer exist, then
                    // throw an exception or is it better if we just close
                    // them and consider that if any additional call on them
                    // would be the one that throws the expcetion
                    //
                    _interleavingPolicy.Commit((INode)_currentFixedPageWriter);
                    //
                    // All current lower references should be cleared
                    //
                    _currentFixedPageWriter = null;
                    //
                    // Notify current document that the page is complete so it
                    // flush the link target information.  This normally would 
                    // occur when the page is committed but this may be delayed 
                    // due to font subsetting.  Nothing negative occurs if this called multiple times
                    //
                    (_currentFixedDocumentWriter as XpsFixedDocumentReaderWriter).CurrentPageCommitted();
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        /// Acquire a ResourceStream for a XpsFont
        /// </Summary>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsFont(
            )
        {
            XpsResourceStream resourceStream = null;

            if(_fontAcquireMode != ResourceAcquireMode.MultipleAcquired)
            {
                if(_fontAcquireMode == ResourceAcquireMode.NoneAcquired)
                {
                    _fontAcquireMode = ResourceAcquireMode.SingleAcquired;
                }

                if(_currentXpsFontRef == 0)
                {
                    //
                    // We need to create the corresponding part in the Xps package
                    // and then acquire the Stream
                    //
                    if(_currentFixedPageWriter != null)
                    {
                        _currentXpsFont = _currentFixedPageWriter.AddFont();
                        _interleavingPolicy.AddItem((INode)_currentXpsFont, 0, (INode)_currentFixedPageWriter);
                    }
                    else
                    {
                        throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPageWriter));
                    }

                    //
                    // retreive the appropriate stream and uri from the reach package api layer
                    //
                    if(_currentXpsFont != null)
                    {
                         _fontResourceStream = new XpsResourceStream(_currentXpsFont.GetStream(),
                                                                  _currentXpsFont.Uri);
                    }
                }

                _currentXpsFontRef++;

                resourceStream = _fontResourceStream;

            }

            return resourceStream;
        }

        /// <Summary>
        /// Acquire a ResourceStream for a XpsFont
        /// </Summary>
        /// <param name="resourceId">
        /// Id of Resource
        /// </param>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsFont(
            String resourceId
            )
        {
            XpsResourceStream resourceStream = null;

            if(_fontAcquireMode != ResourceAcquireMode.SingleAcquired)
            {
                if(_fontAcquireMode == ResourceAcquireMode.NoneAcquired)
                {
                    _fontAcquireMode = ResourceAcquireMode.MultipleAcquired;
                }

                ResourceStreamCacheItem resourceStreamCacheItem = (ResourceStreamCacheItem)_fontsCache[resourceId];

                if(resourceStreamCacheItem == null)
                {
                    resourceStreamCacheItem = new ResourceStreamCacheItem();

                    //
                    // We need to create the corresponding part in the Xps package
                    // and then acquire the Stream
                    //
                    if(_currentFixedPageWriter != null)
                    {
                        XpsFont reachFont = _currentFixedPageWriter.AddFont();
    
                        if(reachFont != null)
                        {
                            _interleavingPolicy.AddItem((INode)reachFont, 0, (INode) _currentFixedPageWriter);
                            resourceStreamCacheItem.XpsResource = (XpsResource)reachFont;
                            //
                            // retreive the appropriate stream and uri from the reach package api layer
                            //
                            _fontResourceStream = new XpsResourceStream(reachFont.GetStream(),
                                                                     reachFont.Uri);
                                                  
                            resourceStreamCacheItem.XpsResourceStream = _fontResourceStream;
    
                            _fontsCache[resourceId] = resourceStreamCacheItem;

                            resourceStream = _fontResourceStream;
                        }
                    }
                    else
                    {
                        throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPageWriter));
                    }
                }
                else
                {
                    resourceStream = resourceStreamCacheItem.XpsResourceStream;
                    resourceStreamCacheItem.IncRef();
                }
            }

            return resourceStream;
        }

        /// <Summary>
        /// Release a reference to the current XpsFont ResourceStream
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsFont(
            )
        {
            if(_fontAcquireMode == ResourceAcquireMode.SingleAcquired)
            {
                if(_currentXpsFont != null &&
                   _currentXpsFontRef > 0)
                {
                    _currentXpsFontRef--;

                    if(_currentXpsFontRef == 0)
                    {
                        _interleavingPolicy.Commit((INode)_currentXpsFont);
                        _fontResourceStream.Initialize();
                        _currentXpsFont   = null;
                        _fontResourceStream = null;
                        _fontAcquireMode    = ResourceAcquireMode.NoneAcquired;
                    }
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
                }
            }
        }

        /// <Summary>
        /// Release a reference to the current XpsFont ResourceStream
        /// </Summary>
        /// <param name="resourceId">
        /// Id of Resource to release
        /// </param>
        public
        override
        void
        ReleaseResourceStreamForXpsFont(
            String resourceId
            )
        {
            if(_fontAcquireMode == ResourceAcquireMode.MultipleAcquired)
            {
                ResourceStreamCacheItem resourceStreamCacheItem = (ResourceStreamCacheItem)_fontsCache[resourceId];

                if(resourceStreamCacheItem != null)
                {
                    if(resourceStreamCacheItem.Release() == 0)
                    {
                        _interleavingPolicy.Commit((INode)resourceStreamCacheItem.XpsResource);
                        _fontsCache.Remove(resourceId);

                        if(_fontsCache.Count == 0)
                        {
                            _fontAcquireMode = ResourceAcquireMode.NoneAcquired;
                        }
                    }
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
                }
            }
        }

        /// <Summary>
        /// Acquire a ResourceStream for a XpsImage
        /// </Summary>
        /// <param name="resourceId">
        /// Id of Resource
        /// </param>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsImage(
            String resourceId
            )
        {
            XpsResourceStream resourceStream = null;

            if(_currentXpsImageRef == 0)
            {
                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the Stream
                //
                if(_currentFixedPageWriter != null)
                {
                    _currentXpsImage = _currentFixedPageWriter.AddImage(resourceId);
                    _interleavingPolicy.AddItem((INode)_currentXpsImage, 0, (INode) _currentFixedPageWriter);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPageWriter));
                }

                //
                // retreive the appropriate stream and uri from the reach package api layer
                //
                if(_currentXpsImage != null)
                {
                    _imageResourceStream = new XpsResourceStream(_currentXpsImage.GetStream(),
                                                               _currentXpsImage.Uri);
                }
            }

            _currentXpsImageRef++;

            resourceStream = _imageResourceStream;

            return resourceStream;
        }

        /// <Summary>
        /// Release a reference of the current XpsImage ResourceStream
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsImage(
            )
        {
            if(_currentXpsImage != null &&
               _currentXpsImageRef > 0)
            {
                _currentXpsImageRef--;

                if(_currentXpsImageRef == 0)
                {
                    _interleavingPolicy.Commit((INode)_currentXpsImage);
                    _imageResourceStream.Initialize();
                    _currentXpsImage = null;
                    _imageResourceStream = null;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        /// Acquire a ResourceSTream for a XpsColorContext
        /// </Summary>
        /// <param name="resourceId">
        /// A id for the ResourceStream
        /// </param>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsColorContext(
            String resourceId
            )
        {
            XpsResourceStream resourceStream = null;

            if(_currentXpsColorContextRef == 0)
            {
                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the Stream
                //
                if(_currentFixedPageWriter != null)
                {
                    _currentXpsColorContext = _currentFixedPageWriter.AddColorContext();
                    _interleavingPolicy.AddItem((INode)_currentXpsColorContext, 0, (INode) _currentFixedPageWriter);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPageWriter));
                }

                //
                // retreive the appropriate stream and uri from the reach package api layer
                //
                if(_currentXpsColorContext != null)
                {
                     _colorContextResourceStream = new XpsResourceStream(_currentXpsColorContext.GetStream(),
                                                               _currentXpsColorContext.Uri);
                }
            }

            _currentXpsColorContextRef++;

            resourceStream = _colorContextResourceStream;

            return resourceStream;
        }

        /// <Summary>
        /// Release a reference to the current XpsColorContext ResourceStream
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsColorContext(
            )
        {
            if(_currentXpsColorContext != null &&
               _currentXpsColorContextRef > 0)
            {
                _currentXpsColorContextRef--;

                if(_currentXpsColorContextRef == 0)
                {
                    _interleavingPolicy.Commit((INode) _currentXpsColorContext);
                    _colorContextResourceStream.Initialize();
                    _currentXpsColorContext = null;
                    _colorContextResourceStream = null;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        /// Acquire a ResoureSTream for a XpsResourceDictionary
        /// </Summary>
        /// <param name="resourceId">
        /// A resource Id for the ResourceStream
        /// </param>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsResourceDictionary(
            String resourceId
            )
        {
            XpsResourceStream resourceStream = null;

            if(_currentXpsResourceDictionaryRef == 0)
            {
                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the Stream
                //
                if(_currentFixedPageWriter != null)
                {
                    _currentXpsResourceDictionary = _currentFixedPageWriter.AddResourceDictionary();
                    _interleavingPolicy.AddItem((INode) _currentXpsResourceDictionary, 0, (INode) _currentFixedPageWriter);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPageWriter));
                }

                //
                // retreive the appropriate stream and uri from the reach package api layer
                //
                if(_currentXpsResourceDictionary != null)
                {
                     _resourceDictionaryResourceStream = new XpsResourceStream(_currentXpsResourceDictionary.GetStream(),
                                                               _currentXpsResourceDictionary.Uri);
                }
            }

            _currentXpsResourceDictionaryRef++;

            resourceStream = _resourceDictionaryResourceStream;

            return resourceStream;
        }

        /// <Summary>
        /// Release a reference to the current XpsResourceDictionary
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsResourceDictionary(
            )
        {
            if(_currentXpsResourceDictionary != null &&
               _currentXpsResourceDictionaryRef > 0)
            {
                _currentXpsResourceDictionaryRef--;

                if(_currentXpsResourceDictionaryRef == 0)
                {
                    _interleavingPolicy.Commit((INode)_currentXpsResourceDictionary);
                    _resourceDictionaryResourceStream.Initialize();
                    _currentXpsResourceDictionary   = null;
                    _resourceDictionaryResourceStream = null;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <summary>
        /// This method adds a relationship to the current active
        /// page using the specified target and relationship name.
        /// </summary>
        /// <param name="targetUri">
        /// Uri to Target for relationship.
        /// </param>
        /// <param name="relationshipName">
        /// Relationship name to add.
        /// </param>
        public
        override
        void
        RelateResourceToCurrentPage(
            Uri     targetUri,
            string  relationshipName
            )
        {
            if (_currentFixedPageWriter != null)
            {
                ((XpsFixedPageReaderWriter)_currentFixedPageWriter).AddRelationship(targetUri, relationshipName);
            }
        }

        /// <summary>
        /// This method adds a relationship to the current active
        /// document using the specified target and relationship name.
        /// </summary>
        /// <param name="targetUri">
        /// Uri to Target for relationship.
        /// </param>
        /// 
        public
        override
        void
        RelateRestrictedFontToCurrentDocument(
            Uri targetUri
            )     
        {
            if (_currentFixedDocumentWriter != null)
            {
               ((XpsFixedDocumentReaderWriter)_currentFixedDocumentWriter).AddRelationship(targetUri, XpsS0Markup.RestrictedFontRelationshipType);
     
                //
                // This code is in for restricted fonts and should be
                // enabled later when we figure out a way to commit
                // the special relation for that font
                //
                //_currentFixedDocumentWriter.Commit();
                //
            }
        }

        /// <summary>
        /// Persists the PrintTicket to the packaging layer
        /// </summary>
        /// <exception cref="ArgumentNullException">printTicket is NULL.</exception>
        /// <param name="printTicket">
        /// Caller supplied PrintTicket.
        /// </param>
        public
        override
        void
        PersistPrintTicket(
            PrintTicket printTicket
            )
        {
            if(printTicket == null)
            {
                throw new ArgumentNullException("printTicket");
            }
            else
            {
                //
                // We need to figure out at which level of the package
                // is this printTicket targeted
                //
                if(_currentFixedPageWriter != null)
                {
                    _currentFixedPageWriter.PrintTicket = printTicket;
                }
                else if(_currentFixedDocumentWriter != null)
                {
                    _currentFixedDocumentWriter.PrintTicket = printTicket;
                }
                else if(_currentFixedDocumentSequenceWriter != null)
                {
                    _currentFixedDocumentSequenceWriter.PrintTicket = printTicket;
                }
            }
        }

        /// <summary>
        /// Acquire a XmlWriter fro the current page
        /// </summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForPage(
            )
        {
            if (_currentFixedPageWriter == null)
            {
                throw new InvalidOperationException("CurrentFixedPageWriter uninitialized");
            }
            return ((XpsFixedPageReaderWriter)_currentFixedPageWriter).PageXmlWriter;
        }

        /// <summary>
        /// Prepare to commit the current page.
        /// </summary>
        public
        override
        void
        PreCommitCurrentPage(
            )
        {
            if (_currentFixedPageWriter == null)
            {
                throw new InvalidOperationException("CurrentFixedPageWriter uninitialized");
            }
            ((XpsFixedPageReaderWriter)_currentFixedPageWriter).PrepareCommit();
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForResourceDictionary(
            )
        {
            if (_currentFixedPageWriter == null)
            {
                throw new InvalidOperationException("CurrentFixedPageWriter uninitialized");
            }
            return ((XpsFixedPageReaderWriter)_currentFixedPageWriter).ResourceDictionaryXmlWriter;
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        IList<String>
        AcquireStreamForLinkTargets(
            )
        {
            if (_currentFixedPageWriter == null)
            {
                throw new ArgumentNullException("CurrentFixedPageWriter");
            }
            return ((XpsFixedPageReaderWriter)_currentFixedPageWriter).LinkTargetStream;
        }


        #endregion Public Methods

        #region Public Properties


        /// <summary>
        /// Get the Uri of the Current FixedDocumentWriter. Null is returned if there is 
        /// not a current FixedDocumentWriter.
        /// </summary>
        public
        override
        Uri
        CurrentFixedDocumentUri
        {
            get
            {
                if (_currentFixedDocumentWriter != null)
                {
                    return _currentFixedDocumentWriter.Uri;                  
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get the Uri of the Current FixedPageWriter.  Null is returned if there is 
        /// not a current FixedPageWriter
        /// </summary>
        public
        override
        Uri
        CurrentFixedPageUri
        {
            get
            {
                if (_currentFixedPageWriter != null)
                {
                    return _currentFixedPageWriter.Uri;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <summary>
        ///
        /// </summary>
        public
        event
        PackagingProgressEventHandler PackagingProgressEvent
        {
            add{ InterleavingPolicy.PackagingProgressEvent += value; }
            remove{ InterleavingPolicy.PackagingProgressEvent -= value; }
        }
        internal
        XpsInterleavingPolicy
        InterleavingPolicy
        {
            get
            {
                return _interleavingPolicy;
            }
        }

        #endregion Public Properties
        


        #region Private Methods

        private
        void
        Initialize(
            )
        {
            _currentFixedDocumentSequenceWriter  = null;
            _currentFixedDocumentWriter = null;
            _currentFixedPageWriter = null;
            
            _currentDocumentSequenceWriterRef = 0;
            _currentFixedDocumentWriterRef = 0;
            _currentFixedPageWriterRef = 0;
        }

        private
        void
        InitializeResourceReferences(
            )
        {
            _currentXpsFont = null;
            _currentXpsImage = null;
            _currentXpsColorContext = null;
            _currentXpsResourceDictionary = null;
            
            _currentXpsFontRef = 0;
            _currentXpsImageRef = 0;
            _currentXpsColorContextRef = 0;
            _currentXpsResourceDictionaryRef = 0;

            if(_fontResourceStream!=null)
            {
                _fontResourceStream.Initialize();
            }

            if(_imageResourceStream!=null)
            {
                _imageResourceStream.Initialize();
            }

            if(_colorContextResourceStream!=null)
            {
                _colorContextResourceStream.Initialize();
            }

            if(_resourceDictionaryResourceStream!=null)
            {
                _resourceDictionaryResourceStream.Initialize();
            }


        }

       

        #endregion Private Methods

        #region Private Data

        private
        System.
        Windows.
        Xps.
        Packaging.
        XpsDocument                _reachPackage;

        private
        XpsInterleavingPolicy       _interleavingPolicy;
        
        private
        IXpsFixedDocumentSequenceWriter     _currentFixedDocumentSequenceWriter;

        private
        IXpsFixedDocumentWriter        _currentFixedDocumentWriter;

        private
        IXpsFixedPageWriter            _currentFixedPageWriter;

        private
        int                         _currentDocumentSequenceWriterRef;

        private
        int                         _currentFixedDocumentWriterRef;

        private
        int                         _currentFixedPageWriterRef;

        private
        XmlWriter                   _currentDSWriter;

        private
        XmlWriter                   _currentFDWriter;

        private
        XmlWriter                   _currentFPWriter;

        private
        XpsFont                   _currentXpsFont;

        private
        XpsImage                  _currentXpsImage;

        private
        XpsColorContext           _currentXpsColorContext;

        private
        XpsResourceDictionary           _currentXpsResourceDictionary;

        private
        int                         _currentXpsFontRef;

        private
        int                         _currentXpsImageRef;

        private
        int                         _currentXpsColorContextRef;

        private
        int                         _currentXpsResourceDictionaryRef;

        private
        XpsResourceStream           _fontResourceStream;

        private
        XpsResourceStream           _imageResourceStream;

        private
        XpsResourceStream           _colorContextResourceStream;
        
        private
        XpsResourceStream           _resourceDictionaryResourceStream;

        private
        ResourceAcquireMode         _fontAcquireMode;

        private
        Hashtable                   _fontsCache;

        private enum ResourceAcquireMode
        {
            NoneAcquired     = 0,
            SingleAcquired   = 1,
            MultipleAcquired = 2
        };

        #endregion Private Data
    };
}
