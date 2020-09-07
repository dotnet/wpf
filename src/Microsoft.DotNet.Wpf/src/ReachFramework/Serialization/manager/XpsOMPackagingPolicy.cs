// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using MS.Internal;
using MS.Internal.PrintWin32Thunk;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Text;
using System.Windows.Xps.Serialization;
using System.Windows.Xps.Serialization.RCW;
using System.Xml;

namespace System.Windows.Xps.Packaging
{
    internal class XpsOMPackagingPolicy : BasePackagingPolicy
    {
        private const int INITIAL_FONTCACHE_CAPACITY = 11;

        #region Constructor

        internal
        XpsOMPackagingPolicy(
            IXpsDocumentPackageTarget packageTarget
            )
        {
            if (packageTarget == null)
            {
                throw new ArgumentNullException(nameof(packageTarget));
            }
            try
            {
                _xpsManager = new XpsManager();
                _packageTarget = packageTarget;
                _xpsOMFactory = _packageTarget.GetXpsOMFactory();

                _xpsPartResources = _xpsOMFactory.CreatePartResources();

            }
            catch (COMException)
            {
                Invalidate();
                throw new PrintingCanceledException();
            }

            _fontsCache = new Hashtable(INITIAL_FONTCACHE_CAPACITY);
            _isValid = true;
            Initialize();
        }

        #endregion Constructor

        #region internal XPSOM Methods

        internal
        void
        EnsureXpsOMPackageWriter()
        {

            if (_currentDocumentSequenceWriterRef == 0)
            {
                try
                {
                    IOpcPartUri partUri = GenerateIOpcPartUri(XpsS0Markup.DocumentSequenceContentType);
                    _currentFixedDocumentSequenceWriter = _packageTarget.GetXpsOMPackageWriter(partUri, null);
                    if (_printQueue != null)
                    {
                        ((PrintQueue)_printQueue).XpsOMPackageWriter = _currentFixedDocumentSequenceWriter;
                    }
                }
                catch(COMException)
                {
                    Invalidate();
                    throw new PrintingCanceledException();
                }
            }

            _currentDocumentSequenceWriterRef++;

        }

        internal
        void
        CloseXpsOMPackageWriter(
            )
        {
            if (_currentFixedDocumentSequenceWriter != null &&
               _currentDocumentSequenceWriterRef > 0)
            {
                _currentDocumentSequenceWriterRef--;

                if (_currentDocumentSequenceWriterRef == 0)
                {
                    Initialize();
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        internal
        void
        StartNewDocument()
        {
            try
            {
                Uri uri = _xpsManager.GenerateUniqueUri(XpsS0Markup.FixedDocumentContentType);
                _currentFixedDocumentUri = uri;
                IOpcPartUri partUri = GenerateIOpcPartUri(uri);
                if (_currentDocumentPrintTicket == null)
                {
                    _currentDocumentPrintTicket = new PrintTicket();
                }
                IXpsOMPrintTicketResource printTicketResource = GeneratePrintTicketResource(XpsS0Markup.FixedDocumentContentType, _currentDocumentPrintTicket);
                _currentFixedDocumentSequenceWriter.StartNewDocument(partUri, printTicketResource, null, null, null);
                _currentFixedDocumentWriterRef++;
            }
            catch (COMException)
            {
                Invalidate();
                throw new PrintingCanceledException();
            }
        }

        internal
        void
        ReleaseXpsOMWriterForFixedDocument()
        {
            if (_currentFixedDocumentWriterRef > 0)
            {
                _currentFixedDocumentWriterRef--;

                if (_currentFixedDocumentWriterRef == 0)
                {
                    _currentDocumentPrintTicket = null;
                    _currentFixedDocumentUri = null;
                }
            }
        }

        #endregion

        #region internal properties

        internal
        Size
        FixedPageSize
        {
            set
            {
                _currentPageSize = value;
            }
        }

        internal
        bool
        IsValid
        {
            get
            {
                return _isValid;
            }
        }

        internal
        object
        PrintQueueReference
        {
            set
            {
                _printQueue = value;
            }
        }

        #endregion

        #region BasePackagingPolicy methods

        /// <summary>
        /// This method must be implemented from abstract class BasePackagingPolicy,
        /// Since we don't use XmlWriters in XpsOM printing for FixedDocumentSequence we should never
        /// be calling this method, if we do, then it is a mistake and we should get an exception
        /// </summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForFixedDocumentSequence()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method must be implemented from abstract class BasePackagingPolicy,
        /// Since we don't use XmlWriters in XpsOM printing for FixedDocumentSequence we should never
        /// be calling this method, if we do, then it is a mistake and we should get an exception
        /// </summary>
        public
        override
        void
        ReleaseXmlWriterForFixedDocumentSequence()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method must be implemented from abstract class BasePackagingPolicy,
        /// Since we don't use XmlWriters in XpsOM printing for FixedDocument we should never
        /// be calling this method, if we do, then it is a mistake and we should get an exception
        /// </summary>
        public
        override
        XmlWriter
        AcquireXmlWriterForFixedDocument()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method must be implemented from abstract class BasePackagingPolicy,
        /// Since we don't use XmlWriters in XpsOM printing for FixedDocument we should never
        /// be calling this method, if we do, then it is a mistake and we should get an exception
        /// </summary>
        public
        override
        void
        ReleaseXmlWriterForFixedDocument()
        {
            throw new NotImplementedException();
        }

        public
        override
        XmlWriter
        AcquireXmlWriterForFixedPage()
        {
            XmlWriter xmlWriter = null;

            if (_currentFixedPageWriterRef == 0)
            {
                _currentPageContentStream = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
                _currentResourceStream = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

                _currentResourceXmlWriter = new XmlTextWriter(_currentResourceStream);
                _currentPageContentXmlWriter = new XmlTextWriter(_currentPageContentStream);

                _currentFixedPagePrintStream = XpsPrintStream.CreateXpsPrintStream();
                _currentFixedPageXmlWriter = new XmlTextWriter(_currentFixedPagePrintStream, Encoding.UTF8);

                _currentFixedPageUri = _xpsManager.GenerateUniqueUri(XpsS0Markup.FixedPageContentType);

                _currentFixedPageLinkTargetStream = new List<String>();
            }

            _currentFixedPageWriterRef++;

            xmlWriter = _currentFixedPageXmlWriter;

            return xmlWriter;
        }

        public
        override
        void
        ReleaseXmlWriterForFixedPage()
        {
            if (_currentFixedPageXmlWriter != null &&
               _currentFixedPageWriterRef > 0)
            {
                _currentFixedPageWriterRef--;

                if (_currentFixedPageWriterRef == 0)
                {
                    AddCurrentPageToPackageWriter();

                    //
                    // All current lower references should be cleared
                    //
                    _currentFixedPageWriter = null;
                    _currentPagePrintTicket = null;
                    _currentPageSize = new Size(0, 0);

                    _currentFixedPagePrintStream.Dispose();
                    _currentFixedPagePrintStream = null;
                    _currentFixedPageXmlWriter = null;

                    _currentResourceStream = null;
                    _currentResourceXmlWriter = null;

                    _currentPageContentStream = null;
                    _currentPageContentXmlWriter = null;

                    _currentFixedPageUri = null;
                    _currentFixedPageLinkTargetStream = null;
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        /// <summary>
        /// When creating a new resource a relationship must be created between the resource
        /// and the page that references it, XpsOM does this automatically for us when we reference
        /// the resource, the serialization engine will still try to call into us, so no-op
        /// </summary>
        public
        override
        void
        RelateResourceToCurrentPage(
            Uri targetUri,
            string relationshipName
            )
        {
            return;
        }

        /// <summary>
        /// WPF has never had support for restricted fonts, XpsPackagingPolicy's implementation
        /// is essentially a no-op, so we'll do the same
        /// </summary>
        public
        override
        void
        RelateRestrictedFontToCurrentDocument(
            Uri targetUri
            )
        {
            return;
        }

        public
        override
        XmlWriter
        AcquireXmlWriterForPage()
        {
            if (_currentFixedPageXmlWriter == null)
            {
                throw new InvalidOperationException("CurrentFixedPageWriter uninitialized");
            }
            return _currentPageContentXmlWriter;
        }

        public
        override
        XmlWriter
        AcquireXmlWriterForResourceDictionary()
        {
            if (_currentFixedPageXmlWriter == null)
            {
                throw new InvalidOperationException("CurrentFixedPageWriter uninitialized");
            }
            return _currentResourceXmlWriter;
        }

        public
        override
        IList<string>
        AcquireStreamForLinkTargets()
        {
            return _currentFixedPageLinkTargetStream;
        }

        public
        override
        void
        PreCommitCurrentPage()
        {
            _currentResourceXmlWriter.Flush();
            _currentPageContentXmlWriter.Flush();
            
            if (_currentResourceStream.ToString().Length > 0)
            {
                _currentFixedPageXmlWriter.WriteStartElement(XpsS0Markup.PageResources);
                _currentFixedPageXmlWriter.WriteStartElement(XpsS0Markup.ResourceDictionary);

                _currentFixedPageXmlWriter.WriteRaw(_currentResourceStream.ToString());

                _currentFixedPageXmlWriter.WriteEndElement();
                _currentFixedPageXmlWriter.WriteEndElement();

            }

            _currentFixedPageXmlWriter.WriteRaw(_currentPageContentStream.ToString());
        }


        public
        override
        void
        PersistPrintTicket(
            PrintTicket printTicket
            )
        {
            if (printTicket == null)
            {
                throw new ArgumentNullException(nameof(printTicket));
            }
            else
            {
                //
                // We need to figure out at which level of the package
                // is this printTicket targeted, if the document ref 
                // count is 0, that means we're about to start a new 
                // document, otherwise we assume it is a page print ticket
                // We don't support setting FixedDocumentSequence print ticket via serialization,
                // since it can only be set when starting the print job
                if (_currentFixedDocumentSequenceWriter != null)
                {
                    if (_currentFixedDocumentWriterRef == 0)
                    {
                        _currentDocumentPrintTicket = printTicket;
                    }
                    else
                    {
                        _currentPagePrintTicket = printTicket;
                    }
                }
            }
        }

        /// <summary>
        /// This is never called during printing, but we must implement it from
        /// abstract class BasePackagingPolicy
        /// </summary>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsFont()
        {
            throw new NotImplementedException();
        }

        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsFont(
            string resourceId
            )
        {
            XpsResourceStream resourceStream = null;
            
            ResourceStreamCacheItem resourceStreamCacheItem = (ResourceStreamCacheItem)_fontsCache[resourceId];

            if (resourceStreamCacheItem == null)
            {
                resourceStreamCacheItem = new ResourceStreamCacheItem();

                //
                // We need to create the corresponding part in the Xps package
                // and then acquire the Stream
                //
                if (_currentFixedPageXmlWriter != null)
                {
                    try
                    {
                        Uri uri = GenerateUriForObfuscatedFont();
                        IOpcPartUri partUri = GenerateIOpcPartUri(uri);

                        XpsPrintStream fontStreamWrapper = XpsPrintStream.CreateXpsPrintStream();

                        IStream fontIStream = fontStreamWrapper.GetManagedIStream();

                        IXpsOMFontResource fontResource = _xpsOMFactory.CreateFontResource(fontIStream, XPS_FONT_EMBEDDING.XPS_FONT_EMBEDDING_OBFUSCATED, partUri, 1);
                        IXpsOMFontResourceCollection fontCollection = _xpsPartResources.GetFontResources();
                        fontCollection.Append(fontResource);

                        XpsResourceStream fontResourceStream = new XpsResourceStream(fontStreamWrapper, uri);
                        resourceStreamCacheItem.XpsResourceStream = fontResourceStream;

                        _fontsCache[resourceId] = resourceStreamCacheItem;

                        resourceStream = fontResourceStream;
                    }
                    catch (COMException)
                    {
                        Invalidate();
                        throw new PrintingCanceledException();
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

            return resourceStream;
        }

        /// <summary>
        /// This is never called during printing, but we must implement it from
        /// abstract class BasePackagingPolicy
        /// </summary>
        public
        override
        void
        ReleaseResourceStreamForXpsFont()
        {
            throw new NotImplementedException(); 
        }

        public
        override
        void
        ReleaseResourceStreamForXpsFont(
            string resourceId
            )
        {
            ResourceStreamCacheItem resourceStreamCacheItem = (ResourceStreamCacheItem)_fontsCache[resourceId];

            if (resourceStreamCacheItem != null)
            {
                if (resourceStreamCacheItem.Release() == 0)
                {
                    resourceStreamCacheItem.XpsResourceStream.Stream.Dispose();
                    resourceStreamCacheItem.XpsResourceStream.Initialize();
                    _fontsCache.Remove(resourceId);
                }
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CannotReleaseXmlWriter));
            }
        }

        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsImage(
            string resourceId
            )
        {
            XpsResourceStream resourceStream = null;

            if (resourceId == null)
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            ContentType contentType = new ContentType(resourceId);

            if (ContentType.Empty.AreTypeAndSubTypeEqual(contentType))
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_InvalidContentType,contentType.ToString()));
            }

            if (_currentXpsImageRef == 0)
            {
                try
                {
                    _currentImageType = GetXpsImageTypeFromContentType(contentType);
                    XpsPrintStream imageStreamWrapper = XpsPrintStream.CreateXpsPrintStream();
                    Uri imageUri = _xpsManager.GenerateUniqueUri(contentType);
                    _imageResourceStream = new XpsResourceStream(imageStreamWrapper, imageUri);
                    IStream imageIStream = imageStreamWrapper.GetManagedIStream();

                    IOpcPartUri partUri = GenerateIOpcPartUri(imageUri);
                    IXpsOMImageResource imageResource = _xpsOMFactory.CreateImageResource(imageIStream, _currentImageType, partUri);
                    IXpsOMImageResourceCollection imageCollection = _xpsPartResources.GetImageResources();
                    imageCollection.Append(imageResource);
                }
                catch (COMException)
                {
                    Invalidate();
                    throw new PrintingCanceledException();
                }
            }

            _currentXpsImageRef++;
            resourceStream = _imageResourceStream;
            return resourceStream;
        }

        public
        override
        void
        ReleaseResourceStreamForXpsImage()
        {
            if (_imageResourceStream != null &&
                _currentXpsImageRef > 0)
            {
                _currentXpsImageRef--;
                if(_currentXpsImageRef == 0)
                {
                    _imageResourceStream.Stream.Dispose();
                    _imageResourceStream = null;
                }
            }
        }

        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsColorContext(
            string resourceId
            )
        {
            XpsResourceStream resourceStream = null;

            if(_currentXpsColorContextRef == 0)
            {
                try
                {
                    XpsPrintStream colorContextStreamWrapper = XpsPrintStream.CreateXpsPrintStream();
                    Uri colorContextUri = _xpsManager.GenerateUniqueUri(XpsS0Markup.ColorContextContentType);
                    _colorContextResourceStream = new XpsResourceStream(colorContextStreamWrapper, colorContextUri);
                    IStream _colorIStream = colorContextStreamWrapper.GetManagedIStream();

                    IOpcPartUri partUri = GenerateIOpcPartUri(colorContextUri);
                    IXpsOMColorProfileResource colorResource = _xpsOMFactory.CreateColorProfileResource(_colorIStream, partUri);
                    IXpsOMColorProfileResourceCollection colorCollection = _xpsPartResources.GetColorProfileResources();
                    colorCollection.Append(colorResource);
                }
                catch (COMException)
                {
                    Invalidate();
                    throw new PrintingCanceledException();
                }
            }

            _currentXpsColorContextRef++;

            resourceStream = _colorContextResourceStream;

            return resourceStream;
        }

        public
        override
        void
        ReleaseResourceStreamForXpsColorContext()
        {
            if (_colorContextResourceStream != null &&
                _currentXpsColorContextRef > 0)
            {
                _currentXpsColorContextRef--;
                if (_currentXpsColorContextRef == 0)
                {
                    _colorContextResourceStream.Stream.Dispose();
                    _colorContextResourceStream = null;
                }
            }
        }

        /// <summary>
        /// This is never called during printing, but we must implement it from
        /// abstract class BasePackagingPolicy
        /// </summary>
        public
        override
        XpsResourceStream
        AcquireResourceStreamForXpsResourceDictionary(
            string resourceId
            )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is never called during printing, but we must implement it from
        /// abstract class BasePackagingPolicy
        /// </summary>
        public
        override
        void
        ReleaseResourceStreamForXpsResourceDictionary()
        {
            throw new NotImplementedException();
        }

        public
        override
        Uri
        CurrentFixedDocumentUri
        {
            get { return _currentFixedDocumentUri; }
        }

        public
        override
        Uri
        CurrentFixedPageUri
        {
            get { return _currentFixedPageUri;  }
        }

        #endregion

        #region private methods
        private
        void
        Initialize(
            )
        {
            _currentFixedDocumentSequenceWriter = null;
            _currentFixedPageWriter = null;

            _currentDocumentSequenceWriterRef = 0;
            _currentFixedDocumentWriterRef = 0;
            _currentFixedPageWriterRef = 0;

            _currentFixedPageLinkTargetStream = null;

            _currentFixedDocumentUri = null;
            _currentFixedPageUri = null;

            _currentXpsImageRef = 0;
            if (_imageResourceStream != null)
            {
                _imageResourceStream.Stream.Dispose();
            }
            _imageResourceStream = null;

            _currentXpsColorContextRef = 0;
            if (_colorContextResourceStream != null)
            {
                _colorContextResourceStream.Stream.Dispose();
            }
            _colorContextResourceStream = null;

            _currentPageContentStream = null;
            _currentResourceStream = null;

            if (_currentFixedPagePrintStream != null)
            {
                _currentFixedPagePrintStream.Dispose();
            }
            _currentFixedPagePrintStream = null;
            _currentPageContentXmlWriter = null;
            _currentResourceXmlWriter = null;
            _currentFixedPageXmlWriter = null;

            _currentPagePrintTicket = null;
            _currentDocumentPrintTicket = null;

        }

        private
        void
        Invalidate()
        {
            Initialize();
            _isValid = false;
        }

        private
        IOpcPartUri
        GenerateIOpcPartUri(
            ContentType contentType
            )
        {
            Uri uri = _xpsManager.GenerateUniqueUri(contentType);
            return GenerateIOpcPartUri(uri);
        }

        private
        IOpcPartUri
        GenerateIOpcPartUri(
            Uri uri
            )
        {
            try
            {
                IOpcPartUri partUri = _xpsOMFactory.CreatePartUri(uri.ToString());
                return partUri;
            }
            catch (COMException)
            {
                Invalidate();
                throw new PrintingCanceledException();
            }
        }

        private
        Uri
        GenerateUriForObfuscatedFont()
        {
            String uniqueUri = "/Resources/" + Guid.NewGuid().ToString() + XpsS0Markup.ObfuscatedFontExt;
            Uri uri = PackUriHelper.CreatePartUri(new Uri(uniqueUri, UriKind.Relative));
            return uri;
        }

        private
        IXpsOMPrintTicketResource
        GeneratePrintTicketResource(
            ContentType contentType,
            PrintTicket printTicket
            )
        {
            IXpsOMPrintTicketResource printTicketResource = null;
            XpsPrintStream printTicketXpsStream = XpsPrintStream.CreateXpsPrintStream();
            printTicket.SaveTo(printTicketXpsStream);
            IStream printTicketStream = printTicketXpsStream.GetManagedIStream();

            Uri printTicketUri = _xpsManager.GeneratePrintTicketUri(contentType);
            try
            {
                IOpcPartUri printTicketPart = GenerateIOpcPartUri(printTicketUri);
                printTicketResource = _xpsOMFactory.CreatePrintTicketResource(printTicketStream, printTicketPart);
            }
            catch (COMException)
            {
                Invalidate();
                throw new PrintingCanceledException();
            }

            return printTicketResource;
        }

        private 
        XPS_IMAGE_TYPE
        GetXpsImageTypeFromContentType(
            ContentType contentType
            )
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.JpgContentType))
            {
                return XPS_IMAGE_TYPE.XPS_IMAGE_TYPE_JPEG;
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.PngContentType))
            {
                return XPS_IMAGE_TYPE.XPS_IMAGE_TYPE_PNG;
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.TifContentType))
            {
                return XPS_IMAGE_TYPE.XPS_IMAGE_TYPE_TIFF;
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.WdpContentType))
            {
                return XPS_IMAGE_TYPE.XPS_IMAGE_TYPE_WDP;
            }
            else
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_UnsupportedImageType));
            }
        }

        private
        void
        AddCurrentPageToPackageWriter()
        {
            try
            {
                _currentFixedPageXmlWriter.Flush();
                IStream pageMarkupStream = _currentFixedPagePrintStream.GetManagedIStream();
                IOpcPartUri partUri = GenerateIOpcPartUri(_currentFixedPageUri);

                if (_currentPagePrintTicket == null)
                {
                    _currentPagePrintTicket = new PrintTicket();
                }

                IXpsOMPrintTicketResource printTicketResource = GeneratePrintTicketResource(XpsS0Markup.FixedPageContentType, _currentPagePrintTicket);
                _currentFixedPagePrintStream.Seek(0, SeekOrigin.Begin);
                _currentFixedPageWriter = _xpsOMFactory.CreatePageFromStream(pageMarkupStream, partUri, _xpsPartResources, 0);

                SetHyperlinkTargetsForCurrentPage();

                XPS_SIZE xpsSize = new XPS_SIZE() { width = (float)_currentPageSize.Width, height = (float)_currentPageSize.Height };
                _currentFixedDocumentSequenceWriter.AddPage(_currentFixedPageWriter, xpsSize, null, null, printTicketResource, null);
            }
            catch (COMException)
            {
                Invalidate();
                throw new PrintingCanceledException();
            }
        }

        private
        void
        SetHyperlinkTargetsForCurrentPage()
        {
            try
            {
                IXpsOMVisualCollection visuals = _currentFixedPageWriter.GetVisuals();
                uint visualCount = visuals.GetCount();

                for (uint i = 0; i < visualCount; i++)
                {
                    IXpsOMVisual visual = visuals.GetAt(i);
                    string name = visual.GetName();
                    if (!String.IsNullOrEmpty(name))
                    {
                        visual.SetIsHyperlinkTarget(TRUE);
                    }
                }
            }
            catch (COMException)
            {
                Invalidate();
                throw new PrintingCanceledException();
            }
            
        }

        #endregion

        #region private data

        // COM Interfaces
        private IXpsDocumentPackageTarget _packageTarget;
        private IXpsOMObjectFactory _xpsOMFactory;
        private IXpsOMPartResources _xpsPartResources;
        private IXpsOMPackageWriter _currentFixedDocumentSequenceWriter;
        private IXpsOMPage _currentFixedPageWriter;
        private XPS_IMAGE_TYPE _currentImageType;

        // Writer reference counts
        private int _currentDocumentSequenceWriterRef;
        private int _currentFixedDocumentWriterRef;
        private int _currentFixedPageWriterRef;

        // Resource reference counts
        private int _currentXpsImageRef;
        private int _currentXpsColorContextRef;

        // Resource streams
        private XpsResourceStream _imageResourceStream;
        private XpsResourceStream _colorContextResourceStream;

        private Hashtable _fontsCache;

        private IList<String> _currentFixedPageLinkTargetStream;

        private Uri _currentFixedDocumentUri;
        private Uri _currentFixedPageUri;

        // Page Streams
        private StringWriter _currentPageContentStream;
        private StringWriter _currentResourceStream;
        private XpsPrintStream _currentFixedPagePrintStream;

        // Page Xml writers
        private XmlWriter _currentPageContentXmlWriter;
        private XmlWriter _currentResourceXmlWriter;
        private XmlWriter _currentFixedPageXmlWriter;

        private Size _currentPageSize;
        private PrintTicket _currentPagePrintTicket;
        private PrintTicket _currentDocumentPrintTicket;

        private XpsManager _xpsManager;

        private const int TRUE = 1;
        private const int FALSE = 0;

        private bool _isValid;

        private object _printQueue;
        
        #endregion


    }
}
