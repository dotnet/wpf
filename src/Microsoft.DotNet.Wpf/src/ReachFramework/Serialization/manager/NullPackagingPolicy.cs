// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file contains the implementation of an instance of the policy
        that bypasses the physical file as the target of the serialization
        process. the intension here is to be able to profile performance
        of different components in the system.

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
using System.Security;
using System.Globalization;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// A class implementing a Xps specific packaging policy
    /// </summary>
    internal class NullPackagingPolicy :
                 BasePackagingPolicy
    {
        #region Constructor

        /// <summary>
        /// instantiate a NullPackagingPolicy class
        /// </summary>
        public
        NullPackagingPolicy(
            ):
        base()
        {
            Initialize();

            _fontResourceStream             = null;
            _imageResourceStream            = null;
            _colorContextResourceStream     = null;
            _resourceDictionaryResourceStream  = null;


            _fontAcquireMode                = ResourceAcquireMode.NoneAcquired;
            _fontsCache   = new Hashtable(11);

            InitializeResourceReferences();

            _resourcePolicy = new XpsResourcePolicy(XpsResourceSharing.NoResourceSharing);
            _resourcePolicy.RegisterService(new XpsImageSerializationService(),
                                            typeof(XpsImageSerializationService));
            _resourcePolicy.RegisterService(new XpsFontSerializationService(this),
                                            typeof(XpsFontSerializationService));
        }

        #endregion Constructor

        #region Public Methods

        /// <Summary>
        ///
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
                // We would simulate an inmemory object for the sequence and populate it with data
                //
                _currentFixedDocumentSequenceWriter = new StringWriter(CultureInfo.InvariantCulture);
                _currentDSWriter                    = new XmlTextWriter(_currentFixedDocumentSequenceWriter);
            }

            _currentDocumentSequenceWriterRef++;

            xmlWriter = _currentDSWriter as XmlWriter;

            return xmlWriter;
        }

        /// <Summary>
        ///
        /// </Summary>
        public
        override
        void
        ReleaseXmlWriterForFixedDocumentSequence(
            )
        {
            if(_currentFixedDocumentSequenceWriter!=null &&
               _currentDocumentSequenceWriterRef>0)
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
                    //_currentFixedDocumentSequenceWriter.Commit();
                    Initialize();
                    InitializeResourceReferences();
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        ///
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
                    _currentFixedDocumentWriter = new StringWriter(CultureInfo.InvariantCulture);
                    _currentFDWriter            = new XmlTextWriter(_currentFixedDocumentWriter);
                }
            }

            _currentFixedDocumentWriterRef++;

            xmlWriter = _currentFDWriter as XmlWriter;

            return xmlWriter;
        }

        /// <Summary>
        ///
        /// </Summary>
        public
        override
        void
        ReleaseXmlWriterForFixedDocument(
            )
        {
            if(_currentFixedDocumentWriter!=null &&
               _currentFixedDocumentWriterRef>0)
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
                    //_currentFixedDocumentWriter.Commit();
                    //
                    // All current lower references should be cleared
                    //
                    _currentFixedDocumentWriter  = null;
                    _currentFixedPageWriter      = null;
                    _currentFixedPageWriterRef   = 0;
                    InitializeResourceReferences();
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        ///
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
                    _currentFixedPageWriter      = new StringWriter(CultureInfo.InvariantCulture);
                    _currentFPWriter             = new XmlTextWriter(_currentFixedPageWriter);
                    _linkTargetStream            = new List <String>();
                    _resourceStream              = new StringWriter(CultureInfo.InvariantCulture);
                    _resourceXmlWriter           = new XmlTextWriter(_resourceStream);
                    _resourceDictionaryStream    = new StringWriter(CultureInfo.InvariantCulture);
                    _resourceDictionaryXmlWriter = new XmlTextWriter(_resourceDictionaryStream);
                    _pageStream                  = new StringWriter(CultureInfo.InvariantCulture);
                    _pageXmlWriter               = new XmlTextWriter(_pageStream);
                }
            }

            _currentFixedPageWriterRef++;

            xmlWriter = _currentFPWriter;

            return xmlWriter;
        }

        /// <Summary>
        ///
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
                    //_currentFixedPageWriter.Commit();
                    //
                    // All current lower references should be cleared
                    //
                    _currentFixedPageWriter      = null;
                    _linkTargetStream            = null;
                    _resourceStream              = null;
                    _resourceXmlWriter           = null;
                    _resourceDictionaryStream    = null;
                    _resourceDictionaryXmlWriter = null;
                    _pageStream                  = null;
                    _pageXmlWriter               = null;

                    InitializeResourceReferences();
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        ///
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
                    Stream fontStream = null;

                    if(_currentFixedPageWriter != null)
                    {
                        //
                        // Create a new Font Stream
                        //
                        fontStream = new MemoryStream();
                    }
                    else
                    {
                        throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFixedPageWriter));
                    }

                    //
                    // retreive the appropriate stream and uri from the reach package api layer
                    //
                    if(fontStream !=null)
                    {
                         _fontResourceStream = new XpsResourceStream(fontStream,
                                                                  new Uri("package/font",UriKind.Relative));
                         //
                         // This is to handle PSharp bug claiming we do not dispose
                         // this class.  We can not dipose because ownership has been handed off.
                         // thus we set it to null
                         //
                         fontStream = null;
                    }
                    else
                    {
                        //
                        // throw the appropriate exception
                        //
                    }
                }

                _currentXpsFontRef++;

                resourceStream = _fontResourceStream;

            }
            else
            {
                //
                // throw the appropraite exception
                //
            }

            return resourceStream;
        }

        /// <Summary>
        ///
        /// </Summary>
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
                        //XpsFont reachFont = _currentFixedPageWriter.AddFont(resourceId);

                        Stream fontStream = new MemoryStream();

                        if(fontStream !=null)
                        {
                            resourceStreamCacheItem.XpsResource = null;
                            //
                            // retreive the appropriate stream and uri from the reach package api layer
                            //
                            _fontResourceStream = new XpsResourceStream(fontStream,
                                                                     new Uri("package/font",UriKind.Relative));
                                                  
                            resourceStreamCacheItem.XpsResourceStream = _fontResourceStream;
    
                            _fontsCache[resourceId] = resourceStreamCacheItem;

                            resourceStream = _fontResourceStream;
                            //
                            // This is to handle PSharp bug claiming we do not dispose
                            // this class.  We can not dipose because ownership has been handed off.
                            // thus we set it to null
                            //
                            fontStream = null;
                        }
                        else
                        {
                            //
                            // throw the appropriate exception
                            //
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
            else
            {
                //
                // throw the appropraite exception
                //
            }

            return resourceStream;
        }

        /// <Summary>
        ///
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsFont(
            )
        {
            if(_fontAcquireMode == ResourceAcquireMode.SingleAcquired)
            {
                if(_currentXpsFontRef>0)
                {
                    _currentXpsFontRef--;

                    if(_currentXpsFontRef == 0)
                    {
                        _fontResourceStream.Initialize();
                        _fontResourceStream = null;
                        _fontAcquireMode    = ResourceAcquireMode.NoneAcquired;
                    }
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
                }
            }
            else
            {
                //
                // throw the appropriate exception
                //
            }
        }

        /// <Summary>
        ///
        /// </Summary>
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
                        //resourceStreamCacheItem.XpsResource.Commit();
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
            else
            {
                //
                // throw the appropriate exception
                //
            }
        }

        /// <Summary>
        ///
        /// </Summary>
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
                     _imageResourceStream = new XpsResourceStream(new MemoryStream(),
                                                               new Uri("package/image",UriKind.Relative));
                }
                else
                {
                    //
                    // throw the appropriate exception
                    //
                }
            }

            _currentXpsImageRef++;

            resourceStream = _imageResourceStream;

            return resourceStream;
        }

        /// <Summary>
        ///
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsImage(
            )
        {
            if(_currentXpsImageRef>0)
            {
                _currentXpsImageRef--;

                if(_currentXpsImageRef == 0)
                {
                    //_currentXpsImage.Commit();
                    _imageResourceStream.Initialize();
                    _imageResourceStream = null;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        ///
        /// </Summary>
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
                     _colorContextResourceStream = new XpsResourceStream(new MemoryStream(),
                                                               new Uri("package/colorcontext",UriKind.Relative));
                }
                else
                {
                    //
                    // throw the appropriate exception
                    //
                }
            }

            _currentXpsColorContextRef++;

            resourceStream = _colorContextResourceStream;

            return resourceStream;
        }

        /// <Summary>
        ///
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsColorContext(
            )
        {
            if(_currentXpsColorContextRef>0)
            {
                _currentXpsColorContextRef--;

                if(_currentXpsColorContextRef == 0)
                {
                    //_currentXpsColorContext.Commit();
                    _colorContextResourceStream.Initialize();
                    _colorContextResourceStream = null;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <Summary>
        ///
        /// </Summary>
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
                     _resourceDictionaryResourceStream = new XpsResourceStream(new MemoryStream(),
                                                               new Uri("package/colorcontext",UriKind.Relative));
                }
                else
                {
                    //
                    // throw the appropriate exception
                    //
                }
            }

            _currentXpsResourceDictionaryRef++;

            resourceStream = _resourceDictionaryResourceStream;

            return resourceStream;
        }

        /// <Summary>
        ///
        /// </Summary>
        public
        override
        void
        ReleaseResourceStreamForXpsResourceDictionary(
            )
        {
            if(_currentXpsResourceDictionaryRef>0)
            {
                _currentXpsResourceDictionaryRef--;

                if(_currentXpsResourceDictionaryRef == 0)
                {
                    //_currentXpsResourceDictionary.Commit();
                    _resourceDictionaryResourceStream.Initialize();
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
            Uri targetUri,
            string relationshipName
            )
        {
        }

        /// <summary>
        /// This method adds a relationship to the current active
        /// document using the specified target and relationship name.
        /// </summary>
        /// <param name="targetUri">
        /// Uri to Target for relationship.
        /// </param>
        public
        override
        void
        RelateRestrictedFontToCurrentDocument(
            Uri targetUri
            )
        {
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
                    _pagePrintTicket = printTicket.Clone();
                }
                else if(_currentFixedDocumentWriter != null)
                {
                    _documentPrintTicket = printTicket.Clone();
                }
                else if(_currentFixedDocumentSequenceWriter != null)
                {
                    _documentSequencePrintTicket = printTicket.Clone();
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForPage(
            )
        {
            return _pageXmlWriter;
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        PreCommitCurrentPage(
            )
        {
            _resourceXmlWriter.Flush();
            _pageXmlWriter.Flush();

            if(_resourceXmlWriter.ToString().Length > 0)
            {
                _resourceDictionaryXmlWriter.WriteStartElement(XpsS0Markup.PageResources);
                _resourceDictionaryXmlWriter.WriteStartElement(XpsS0Markup.ResourceDictionary);

                _resourceDictionaryXmlWriter.WriteRaw(_resourceStream.ToString());

                _resourceDictionaryXmlWriter.WriteEndElement();
                _resourceDictionaryXmlWriter.WriteEndElement();

                _resourceDictionaryXmlWriter.Flush();

                _currentFPWriter.WriteRaw(_resourceDictionaryStream.ToString());
            }


            //
            // Join resource and page stream into main stream
            //
            _currentFPWriter.WriteRaw(_pageStream.ToString());
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
            return _resourceDictionaryXmlWriter;
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
            return _linkTargetStream;
        }
        
        #endregion Public Methods

        #region Public Properties

        /// <summary>
        ///
        /// </summary>
        public
        override
        Uri
        CurrentFixedDocumentUri
        {
            get
            {
                //return _currentFixedDocumentWriter.Uri;
                return null;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        Uri
        CurrentFixedPageUri
        {
            get
            {
                //return _currentFixedPageWriter.Uri;
                return null;
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
            _currentFixedDocumentWriter          = null;
            _currentFixedPageWriter              = null;
            _currentDocumentSequenceWriterRef    = 0;
            _currentFixedDocumentWriterRef       = 0;
            _currentFixedPageWriterRef           = 0;
        }

        private
        void
        InitializeResourceReferences(
            )
        {
            _currentXpsFontRef            = 0;
            _currentXpsImageRef           = 0;
            _currentXpsColorContextRef    = 0;
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


            _fontAcquireMode = ResourceAcquireMode.NoneAcquired;
        }

      
        #endregion Private Methods

        #region Private Data

        private
        StringWriter                _currentFixedDocumentSequenceWriter;

        private
        StringWriter                _currentFixedDocumentWriter;

        private
        StringWriter                _currentFixedPageWriter;

        private
        int                         _currentDocumentSequenceWriterRef;

        private
        int                         _currentFixedDocumentWriterRef;

        private
        int                         _currentFixedPageWriterRef;

        private
        XmlTextWriter               _currentDSWriter;

        private
        XmlWriter                   _currentFDWriter;

        private
        XmlWriter                   _currentFPWriter;

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


        //
        // ------------------------- Extra Page Resources -------------------
        //
        private
        IList<String>                _linkTargetStream;

        private
        StringWriter                _pageStream;

        private
        StringWriter                _resourceStream;

        private
        StringWriter                _resourceDictionaryStream;

        private
        System.Xml.XmlWriter        _pageXmlWriter;

        private
        System.Xml.XmlWriter        _resourceXmlWriter;

        private
        System.Xml.XmlWriter        _resourceDictionaryXmlWriter;

        private
        PrintTicket                 _documentSequencePrintTicket;

        private
        PrintTicket                 _documentPrintTicket;

        private
        PrintTicket                 _pagePrintTicket;

        private
        Hashtable                   _fontsCache;

        private
        XpsResourcePolicy         _resourcePolicy;


        internal enum ResourceAcquireMode
        {
            NoneAcquired     = 0,
            SingleAcquired   = 1,
            MultipleAcquired = 2
        };

        #endregion Private Data
    };
}
