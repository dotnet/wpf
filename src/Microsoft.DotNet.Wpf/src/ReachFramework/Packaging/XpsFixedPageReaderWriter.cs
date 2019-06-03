// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


    Abstract:
        This file contains the definition for IXpsFixedPageReader
        and IXpsFixedPageWriter interfaces as well as definition
        and implementation of XpsFixedPageReaderWriter.  These
        interfaces and class are used for writing fixed page
        parts to a Xps package.


--*/
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Xml;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Printing;

using System.Windows.Xps.Serialization;

using MS.Internal;
using MS.Utility;

using MS.Internal.IO.Packaging.Extensions;
using PackageRelationship = System.IO.Packaging.PackageRelationship;
using PackUriHelper = System.IO.Packaging.PackUriHelper;

namespace System.Windows.Xps.Packaging
{
    #region IXpsFixedPageReader interface definition

    /// <summary>
    /// Interface for reading fixed page parts from the Xps package.
    /// </summary>
    /// <remarks>Interface will be internal until reading/de-serialization is implemented.</remarks>
    public  interface IXpsFixedPageReader:
                      IStoryFragmentProvider
    {
        #region Public methods

        /// <summary>
        /// This method retrieves a resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="resourceUri">
        /// The URI of the resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsResource to read
        /// the requested resource.
        /// </returns>
        XpsResource
        GetResource(
            Uri         resourceUri
            );

        /// <summary>
        /// This method retrieves a font resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the font resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsFont to read
        /// the requested font data.
        /// </returns>
        XpsFont
        GetFont(
            Uri         uri
            );

        /// <summary>
        /// This method retrieves a colorContext resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the colorContext resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsColorContext to read
        /// the requested colorContext data.
        /// </returns>
        XpsColorContext
        GetColorContext(
            Uri         uri
            );

        /// <summary>
        /// This method retrieves a ResourceDictionary resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the ResourceDictionary resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsResourceDictionary to read
        /// the requested ResourceDictionary data.
        /// </returns>
        XpsResourceDictionary
        GetResourceDictionary(
            Uri         uri
            );

        /// <summary>
        /// This method retrieves a image resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the image resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsImage to read
        /// the requested image data.
        /// </returns>
        XpsImage
        GetImage(
            Uri         uri
            );

        #endregion Public methods

        #region Public properties

        /// <summary>
        /// Gets the PrintTicket associated with this fixed page.
        /// </summary>
        /// <value>Value can be a PrintTicket or null.</value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">part is null.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">part is null.</exception>
        PrintTicket PrintTicket { get; }

        /// <summary>
        /// Gets the URI assigned to this fixed page part.
        /// </summary>
        /// <value>Value is a URI for the Metro part.</value>
        Uri Uri { get; }

        /// <summary>
        /// Gets a reference to the XmlWriter for Metro part
        /// that represent the fixed page within the package.
        /// </summary>
        XmlReader XmlReader { get; }

        /// <summary>
        /// 0 based page numbeer with in document
        /// </summary>
        int PageNumber { get; }

        /// <summary>
        /// This method retrieves a collection of font resource parts
        /// </summary>
        ICollection<XpsFont>
        Fonts{ get; }

        /// <summary>
        /// This method retrieves a collection of colorContext resource parts
        /// </summary>
        ICollection<XpsColorContext>
        ColorContexts{ get; }

        /// <summary>
        /// This method retrieves a collection of ResourceDictionary resource parts
        /// </summary>
        ICollection<XpsResourceDictionary>
        ResourceDictionaries{ get; }

        /// <summary>
        /// This method retrieves a collection of image resource parts
        /// </summary>
        ICollection<XpsImage>
        Images{ get; }

        /// <summary>
        /// thumbnail image associated with this page
        /// </summary>
        XpsThumbnail
        Thumbnail{ get; }

        /// <summary>
        /// This method retrieves the associated story fragment
        /// </summary>
        XpsStructure
        StoryFragment{ get; }
        #endregion Public properties
    }

    #endregion IXpsFixedPageReader interface definition

    #region IXpsFixedPageWriter interface definition

    /// <summary>
    /// Interface for writing fixed page parts to the xps package.
    /// </summary>
    public interface IXpsFixedPageWriter:
                     IStoryFragmentProvider
    {
        #region Public methods

        /// <summary>
        /// This method adds a resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="resourceType">
        /// The resource type being added.
        /// </param>
        /// <param name="resourceUri">
        /// The absolute path to the resouce to be added. If null
        /// a unique resoruce path will be generated
        /// </param>
        /// <returns>
        /// Returns an XpsResource instance for the newly created resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        /// <exception cref="ArgumentNullException">resourceName is null.</exception>
        /// <exception cref="ArgumentException">resourceName is an empty string.</exception>
        XpsResource
        AddResource(
            Type        resourceType,
            Uri         resourceUri
            );

        /// <summary>
        /// This method adds a font resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <returns>
        /// Returns an XpsFont instance for the newly created font resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        XpsFont
        AddFont(
            );

        /// <summary>
        /// This method adds a font resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="obfuscate">
        /// Flag indicating whether the part should be crated
        /// with obfuscated content type
        /// </param>
        /// <returns>
        /// Returns an XpsFont instance for the newly created font resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        XpsFont
        AddFont(
            bool        obfuscate
            );

        /// <summary>
        /// This method adds a font resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="obfuscate">
        /// Flag indicating whether the part should be crated
        /// with obfuscated content type
        /// </param>
        /// <param name="addRestrictedRelationship">
        /// Flag indicating whether restricted relationship is required
        /// Reference 2.1.7.2 of Xps Specifiation
        /// </param>
        /// <returns>
        /// Returns an XpsFont instance for the newly created font resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        XpsFont
        AddFont(
            bool        obfuscate,
            bool        addRestrictedRelationship
            );
        /// <summary>
        /// This method adds a colorContext resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <returns>
        /// Returns an XpsColorContext instance for the newly created colorContext resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        XpsColorContext
        AddColorContext(
            );

        /// <summary>
        /// This method adds a ResourceDictionary resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <returns>
        /// Returns an XpsResourceDictionary instance for the newly created ResourceDictionary resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        XpsResourceDictionary
        AddResourceDictionary(
            );

        /// <summary>
        /// This method adds a image resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="mimeType">
        /// The mime type of the image resource to add.
        /// </param>
        /// <returns>
        /// Returns an XpsImage instance for the newly created image resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        /// <exception cref="ArgumentNullException">imageType is null.</exception>
        /// <exception cref="ArgumentException">imageType is an empty string.</exception>
        XpsImage
        AddImage(
            string      mimeType
            );

        /// <summary>
        /// This method adds a image resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="imageType">
        /// The type of the image resource to add.
        /// </param>
        /// <returns>
        /// Returns an XpsImage instance for the newly created image resource.
        /// </returns>
        XpsImage
        AddImage(
            XpsImageType      imageType
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
       /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        XpsThumbnail
        AddThumbnail(
            XpsImageType  imageType
            );


        /// <summary>
        /// This method commits any changes not already committed for this
        /// fixed page.
        ///
        /// </summary>
        void
        Commit();

        #endregion Public methods

        #region Public properties

        /// <summary>
        /// Sets the PrintTicket associated with this fixed page.
        /// </summary>
        /// <value>
        /// The value must be a valid PrintTicket instance.
        ///
        /// Note:  The PrintTicket can only be assigned to prior to if being
        /// committed to the package.  The commit happens when a valid PrintTicket
        /// is set and a subsequent flush on the document occurs.
        /// </value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">part is null.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">part is null.</exception>
        PrintTicket PrintTicket { set; }

        /// <summary>
        /// Gets the URI assigned to this fixed page part.
        /// </summary>
        /// <value>Value is a URI for the Metro part.</value>
        Uri Uri { get; }

        /// <summary>
        /// Gets a reference to the XmlWriter for Metro part
        /// that represent the fixed page within the package.
        /// </summary>
        XmlWriter XmlWriter { get; }

        /// <summary>
        /// 0 based page numbeer with in document
        /// </summary>
        int PageNumber { get; }

         /// <summary>
        /// Gets a reference to a stream where Link Target
        /// markup can be written.  Once the page is committed,
        /// this will be placed in the appropriate section of
        /// the Fixed document.
        /// </summary>
        IList<String> LinkTargetStream { get; }

        #endregion Public properties
    }

    #endregion IXpsFixedPageWriter interface definition

    /// <summary>
    /// This class implements the reading and writing functionality for
    /// a fixed page within a Xps package.
    /// </summary>
    internal sealed class XpsFixedPageReaderWriter :
                          XpsPartBase,
                          IXpsFixedPageReader,
                          IXpsFixedPageWriter,
                          INode,
                          IDisposable
    {
        #region Constructor

        /// <summary>
        /// Internal constructor for the XpsFixedPageReaderWriter class.
        /// This class is created internally so we can keep the Xps hierarchy
        /// completely under control when using these APIs.
        /// </summary>
        /// <param name="xpsManager">
        /// The XpsManager for the current Xps package.
        /// </param>
        /// <param name="parent">
        /// The parent node of this fixed page.
        /// </param>
        /// <param name="part">
        /// The internal Metro part that represents this fixed page.
        /// </param>
        /// <param name="linkTargetStream">
        /// A reference to a stream which collects the link targets.
        /// </param>
        /// <param name="pageNumber">
        /// A 0 based nuber specifying the page number in the document
        /// </param>
        /// <exception cref="ArgumentNullException">part is null.</exception>
        internal
        XpsFixedPageReaderWriter(
            XpsManager                   xpsManager,
            XpsFixedDocumentReaderWriter parent,
            PackagePart                  part,
            IList<String>                linkTargetStream,
            int                          pageNumber
            )
            : base(xpsManager)
        {
            if (null == part)
            {
                throw new ArgumentNullException(String.Format(CultureInfo.InvariantCulture, "part"));
            }

            this.Uri = part.Uri;
            _metroPart = part;
            _partEditor = new XmlPartEditor(_metroPart);
            _linkTargetStream = linkTargetStream;
            _pageNumber = pageNumber;

#if !(RESOURCESTREAM_USING_PART)
            //
            // Setup the xml writers
            //
            _resourceStream = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            _resourceXmlWriter = new XmlTextWriter(_resourceStream);
            _resourceDictionaryStream = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            _resourceDictionaryXmlWriter = new XmlTextWriter(_resourceDictionaryStream);

            _pageStream = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            _pageXmlWriter = new XmlTextWriter(_pageStream);

#endif
            _fontCache = new Dictionary<Uri, XpsFont>(MS.Internal.UriComparer.Default);
            _colorContextCache = new Dictionary<Uri, XpsColorContext>();
            _resourceDictionaryCache = new Dictionary<Uri, XpsResourceDictionary>();
            _imageCache = new Dictionary<Uri, XpsImage>();
            _resourceCache = new Dictionary<Uri, XpsResource>();
            _parentNode = parent;
            _currentChildren = new List<INode>();
        }

        #endregion Constructor

        #region Public properties

        /// <summary>
        /// Gets or sets the PrintTicket to be stored with this fixed page.
        ///
        /// NOTE:  The PrintTicket can only be set until we have created the
        /// first resource for this fixed page.  At that point we will commit
        /// the PrintTicket for any consumers to read and any attempt to change
        /// it will cause an exception.
        /// </summary>
        /// <value>Value can be a PrintTicket or null.</value>
        /// <exception cref="SRID.ReachPackaging_PrintTicketAlreadyCommitted">part is null.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAPrintTicket">part is null.</exception>
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
        /// Gets a reference to the XmlWriter for the Metro part
        /// that represents this fixed page within the package.
        /// </summary>
        public XmlWriter XmlWriter
        {
            get
            {
                _partEditor.DoesWriteStartEndTags = false;
                return _partEditor.XmlWriter;
            }
        }

        /// <summary>
        /// Gets a reference to the XmlReader for the Metro part
        /// that represents this fixed page within the package.
        /// </summary>
        public XmlReader XmlReader
        {
            get
            {
                return _partEditor.XmlReader;
            }
        }

        /// <summary>
        /// Gets a reference to a stream for writing link targets.
        /// </summary>
        public IList<String> LinkTargetStream
        {
            get
            {
                return _linkTargetStream;
            }
        }

        /// <summary>
        /// 0 based page numbeer with in document
        /// </summary>
       public int PageNumber
        {
            get
            {
                return _pageNumber;
            }
        }

        /// <summary>
        /// Gets a reference to the XmlWriter for the Resource
        /// Dictionary of this fixed page.
        /// </summary>
        public XmlWriter ResourceDictionaryXmlWriter
        {
            get
            {
#if RESOURCESTREAM_USING_PART
                System.Xml.XmlWriter xmlWriter = null;

                if (null == m_metroResourcePart)
                {
                    m_metroResourcePart = CurrentXpsManager.GenerateUniquePart(XpsManager.ResourceDictionaryContentType);
                    if (null != m_metroResourcePart)
                    {
                        m_resourcePartEditor = new XmlPartEditor(m_metroResourcePart);
                        if (null != m_resourcePartEditor)
                        {
                            m_resourcePartEditor.DoesWriteStartEndTags = false;
                        }
                    }
                }

                if (null != m_resourcePartEditor)
                {
                    xmlWriter = m_resourcePartEditor.XmlWriter;
                }

                return xmlWriter;
#else
                return _resourceXmlWriter;
#endif
            }
        }

#if RESOURCESTREAM_USING_PART
        /// <summary>
        ///
        /// </summary>
        public Uri ResourceDictionaryUri
        {
            get
            {
                Uri uri = null;

                if (null != m_metroResourcePart)
                {
                    uri = m_metroResourcePart.Uri;
                }

                return uri;
            }
        }
#endif

        /// <summary>
        /// This method retrieves a collection of font resource parts
        /// </summary>
        public ICollection<XpsFont>
        Fonts
        {
            get
            {
                UpdateResourceCache();
                return _fontCache.Values;
            }
        }

        /// <summary>
        /// This method retrieves a collection of colorContext resource parts
        /// </summary>
        public ICollection<XpsColorContext>
        ColorContexts
        {
            get
            {
                UpdateResourceCache();
                return _colorContextCache.Values;
            }
        }


        /// <summary>
        /// This method retrieves a collection of resourceDictionary resource parts
        /// </summary>
        public ICollection<XpsResourceDictionary>
        ResourceDictionaries
        {
            get
            {
                UpdateResourceCache();
                return _resourceDictionaryCache.Values;
            }
        }


        /// <summary>
        /// This method retrieves a collection of image resource parts
        /// </summary>
        public ICollection<XpsImage>
        Images
        {
            get
            {
                UpdateResourceCache();
                return _imageCache.Values;
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

        /// <summary>
        /// This method retrieves the associated story fragment
        /// </summary>
        public
        XpsStructure
        StoryFragment
        {
            get
            {
                UpdateResourceCache();
                return _storyFragment;
            }
        }

        #endregion Public properties

        #region Public methods

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
            // Add the relationship using a relative path to this page.
            //
            string relativePath = XpsManager.MakeRelativePath(this.Uri, targetUri);

            //
            // We can not read from the file to do validation
            // when streaming
            //
            if (!CurrentXpsManager.Streaming)
            {
                foreach (PackageRelationship rel in _metroPart.GetRelationships())
                {
                    if (rel.TargetUri.Equals(relativePath))
                    {
                        //
                        // Relationship already exists
                        //
                        return;
                    }
                }
            }

            _metroPart.CreateRelationship(new Uri(relativePath, UriKind.Relative),
                                               TargetMode.Internal,
                                               relationshipName);
        }

        /// <summary>
        /// This method adds a resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="resourceType">
        /// The resource type being added.
        /// </param>
        /// <param name="resourceUri">
        /// The absolute path to the resouce to be added. If null
        /// a unique resource path will be generated
        /// </param>
        /// <returns>
        /// Returns an XpsResource instance for the newly created resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        public
        XpsResource
        AddResource(
            Type        resourceType,
            Uri         resourceUri
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }

            //
            // Create the part and writer
            //
            PackagePart metroPart = null;
            if( resourceUri != null )
            {
                metroPart = this.CurrentXpsManager.GetPart(resourceUri);
                if (metroPart == null)
                {
                    metroPart = GeneratePartForResourceType(resourceType, resourceUri);
                }
            }

            XpsResource xpsResource;
            if( resourceType == typeof( XpsImage ) )
            {
                xpsResource = AddImage(metroPart);
            }
            else if( resourceType == typeof( XpsFont ) )
            {
                xpsResource = AddFont(metroPart);
            }
            else if( resourceType == typeof( XpsColorContext ) )
            {
                xpsResource = AddColorContext(metroPart);
            }
            else if( resourceType == typeof( XpsResourceDictionary ) )
            {
                xpsResource = AddResourceDictionary(metroPart);
             }
            else
            {
                if (metroPart == null)
                {
                    if( resourceUri == null )
                    {
                        metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.ResourceContentType);
                    }
                }
                xpsResource = new XpsResource(CurrentXpsManager, this, metroPart);
                _resourceCache[xpsResource.Uri] = xpsResource;

                 //
                // Add the relationship
                //
                string resourcePath = XpsManager.MakeRelativePath(this.Uri, metroPart.Uri);
                //
                // Keep a reference to the child around
                //
                _currentChildren.Add(xpsResource);

                _metroPart.CreateRelationship(new Uri(resourcePath, UriKind.Relative),
                                           TargetMode.Internal,
                                           XpsS0Markup.ResourceRelationshipName);
}

            return xpsResource;
        }

        /// <summary>
        /// </summary>
        public
        XpsStructure
        AddStoryFragment(
            )
        {
            if (this.StoryFragment != null)
            {
                // StoryFragments already available for this FixedPage
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_MoreThanOneStoryFragment));
            }

            Uri pageUri = this.CurrentXpsManager.CreateFragmentUri(PageNumber);
            //
            // Create the part and writer
            //
            PackagePart metroPart = this.CurrentXpsManager.GeneratePart(
                        XpsS0Markup.StoryFragmentsContentType,
                        pageUri);

            _storyFragment = new XpsStructure(CurrentXpsManager, this, metroPart);

            //
            // Create the relationship between the document and the document-structure
            // Not in INode.Flush because IXpsFixedPageReader has no commit.
            //
            string storyFragmentPath = XpsManager.MakeRelativePath(this.Uri, _storyFragment.Uri);

            _metroPart.CreateRelationship(new Uri(storyFragmentPath, UriKind.Relative),
                                             TargetMode.Internal,
                                             XpsS0Markup.StoryFragmentsRelationshipName
                                           );

            return _storyFragment;
        }


        /// <summary>
        /// This method retrieves a resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="resourceUri">
        /// The URI of the resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsResource to read
        /// the requested resource.
        /// </returns>
        public
        XpsResource
        GetResource(
            Uri         resourceUri
            )
        {
            UpdateResourceCache();
            XpsResource xpsResource = null;
            if (_imageCache.ContainsKey(resourceUri))
            {
                xpsResource =  (XpsResource)_imageCache[resourceUri];
            }
            else if(_fontCache.ContainsKey(resourceUri))
            {
                xpsResource =  (XpsResource)_fontCache[resourceUri];
            }
            else if(_colorContextCache.ContainsKey(resourceUri))
            {
                xpsResource =  (XpsResource)_colorContextCache[resourceUri];
            }
            else if(_resourceDictionaryCache.ContainsKey(resourceUri))
            {
                xpsResource =  (XpsResource)_resourceDictionaryCache[resourceUri];
            }
            else if(_resourceCache.ContainsKey(resourceUri) )
            {
                xpsResource =  (XpsResource)_resourceCache[resourceUri];
            }


            return xpsResource;
        }

        /// <summary>
        /// This method adds a font resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <returns>
        /// Returns an XpsFont instance for the newly created font resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        public
        XpsFont
        AddFont(
            )
        {
            XpsFont xpsFont = AddFont(true);

            return xpsFont;
        }

        /// <summary>
        /// This method adds a font resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="obfuscate">
        /// Flag indicating whether the part should be crated
        /// with obfuscated content type
        /// </param>
        /// <returns>
        /// Returns an XpsFont instance for the newly created font resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        public
        XpsFont
        AddFont(
            bool        obfuscate
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }

            PackagePart metroPart = null;

            //
            // Create the part and writer
            //
            if( obfuscate )
            {
                metroPart = this.CurrentXpsManager.GenerateObfuscatedFontPart();
            }
            else
            {
                metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.FontContentType);
            }

            return AddFont(metroPart);
}


        /// <summary>
        /// This method adds a font resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="obfuscate">
        /// Flag indicating whether the part should be crated
        /// with obfuscated content type
        /// </param>
        /// <param name="addRestrictedRelationship">
        /// Flag indicating whether restricted relationship is required
        /// Reference 2.1.7.2 of Xps Specifiation
        /// </param>
        /// <returns>
        /// Returns an XpsFont instance for the newly created font resource.
        /// </returns>
        public
        XpsFont
        AddFont(
            bool        obfuscate,
            bool        addRestrictedRelationship
            )
        {
            XpsFont xpsFont = AddFont( obfuscate );
            if( addRestrictedRelationship )
            {
                _parentNode.AddRelationship(xpsFont.Uri, XpsS0Markup.RestrictedFontRelationshipType);
                xpsFont.IsRestricted = true;
            }
            return xpsFont;
        }



        /// <summary>
        /// This method adds a colorContext resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <returns>
        /// Returns an XpsColorContext instance for the newly created colorContext resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        public
        XpsColorContext
        AddColorContext(
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }

            //
            // Create the part and writer
            //
            PackagePart metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.ColorContextContentType);

            return AddColorContext(metroPart);
        }

        /// <summary>
        /// This method adds a resourceDictionary resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <returns>
        /// Returns an XpsResourceDictionary instance for the newly created resourceDictionary resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        public
        XpsResourceDictionary
        AddResourceDictionary(
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }

            //
            // Create the part and writer
            //
            PackagePart metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.ResourceDictionaryContentType);
            return AddResourceDictionary(metroPart);
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
       /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        public
        XpsThumbnail
        AddThumbnail(
            XpsImageType imageType
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }
            _thumbnail = CurrentXpsManager.AddThumbnail( imageType, this, Thumbnail );
            _currentChildren.Add( _thumbnail );
            _metroPart.CreateRelationship( _thumbnail.Uri,
                                           TargetMode.Internal,
                                           XpsS0Markup.ThumbnailRelationshipName);
            return _thumbnail;
        }

        /// <summary>
        /// This method retrieves a font resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the font resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsFont to read
        /// the requested font data.
        /// </returns>
        public
        XpsFont
        GetFont(
            Uri         uri
            )
        {
            UpdateResourceCache();
            XpsFont xpsFont = null;
            if (_fontCache.ContainsKey(uri))
            {
                xpsFont =  _fontCache[uri];
            }
            return xpsFont;
        }

        /// <summary>
        /// This method retrieves a colorContext resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the colorContext resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsColorContext to read
        /// the requested colorContext data.
        /// </returns>
        public
        XpsColorContext
        GetColorContext(
            Uri         uri
            )
        {
            UpdateResourceCache();
            XpsColorContext xpsColorContext = null;
            if (_colorContextCache.ContainsKey(uri))
            {
                xpsColorContext =  _colorContextCache[uri];
            }
            return xpsColorContext;
        }

        /// <summary>
        /// This method retrieves a resourceDictionary resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the resourceDictionary resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsResourceDictionary to read
        /// the requested resourceDictionary data.
        /// </returns>
        public
        XpsResourceDictionary
        GetResourceDictionary(
            Uri         uri
            )
        {
            UpdateResourceCache();
            XpsResourceDictionary xpsResourceDictionary = null;
            if (_resourceDictionaryCache.ContainsKey(uri))
            {
                xpsResourceDictionary =  _resourceDictionaryCache[uri];
            }
            return xpsResourceDictionary;
        }
        /// <summary>
        /// This method adds a image resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="imageType">
        /// The type of the image resource to add.  This equates to a mime
        /// type for the image (eg.  image/jpeg, image/png, etc).
        /// </param>
        /// <returns>
        /// Returns an XpsImage instance for the newly created image resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        /// <exception cref="ArgumentNullException">imageType is null.</exception>
        /// <exception cref="ArgumentException">imageType is an empty string.</exception>
        /// <exception cref="SRID.ReachPackaging_UnsupportedImageType">Unsupported image type</exception>
        public
        XpsImage
        AddImage(
            XpsImageType      imageType
            )
        {
            return AddImage( XpsManager.ImageTypeToString(imageType) );
        }

        /// <summary>
        /// This method adds a image resource part to the Xps package
        /// and associates it with the current fixed page.
        /// </summary>
        /// <param name="mimeType">
        /// The type of the image resource to add.  This equates to a mime
        /// type for the image (eg.  image/jpeg, image/png, etc).
        /// </param>
        /// <returns>
        /// Returns an XpsImage instance for the newly created image resource.
        /// </returns>
        /// <exception cref="ObjectDisposedException">FixedPageReader has already been disposed</exception>
        /// <exception cref="ArgumentNullException">imageType is null.</exception>
        /// <exception cref="ArgumentException">imageType is an empty string.</exception>
        /// <exception cref="SRID.ReachPackaging_UnsupportedImageType">Unsupported image type</exception>
        public
        XpsImage
        AddImage(
            string  mimeType
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }
            if (null == mimeType)
            {
                throw new ArgumentNullException("mimeType");
            }
            if (0 == mimeType.Length)
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_InvalidType));
            }

            return AddImage(new ContentType(mimeType));
        }


        internal
        XpsImage
        AddImage(
            ContentType mimeType
            )
        {
            if (null == _metroPart)
            {
                throw new ObjectDisposedException("FixedPageReader");
            }
            if (null == mimeType)
            {
                throw new ArgumentNullException("mimeType");
            }
            if (ContentType.Empty.AreTypeAndSubTypeEqual(mimeType))
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_InvalidContentType, mimeType.ToString()));
            }

            if (!XpsManager.SupportedImageType(mimeType))
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_UnsupportedImageType));
            }

            //
            // Create the part and writer
            //
            PackagePart metroPart = this.CurrentXpsManager.GenerateUniquePart(mimeType);
            return AddImage(metroPart);
}


        /// <summary>
        /// This method retrieves a image resource part from a Xps
        /// package using the given URI.
        /// </summary>
        /// <param name="uri">
        /// The URI of the image resource to retrieve.
        /// </param>
        /// <returns>
        /// Returns an instance of a XpsImage to read
        /// the requested image data.
        /// </returns>
        public
        XpsImage
        GetImage(
            Uri         uri
            )
        {
            UpdateResourceCache();
            XpsImage xpsImage = null;
            if (_imageCache.ContainsKey(uri))
            {
                xpsImage =  _imageCache[uri];
            }

            return xpsImage;
        }

        /// <summary>
        /// This method commits any changes not already committed for this
        /// fixed page.
        ///
        /// NOTE:  This commits changes on the current child being editted (if
        /// there is one).  No further changes to this child will be allowed.
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
        /// fixed page.
        /// </summary>
        internal
        override
        void
        CommitInternal(
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXCommitPageBegin);

            CommitPrintTicket();
            if (_partEditor != null)
            {
                _parentNode.CurrentPageCommitted();
                ((INode)this).Flush();
                _partEditor.Close();


#if RESOURCESTREAM_USING_PART
                m_resourcePartEditor.Close();

                m_resourcePartEditor = null;
                m_metroResourcePart = null;
#endif

                _partEditor = null;
                _metroPart = null;

                _parentNode = null;
                _currentChildren = null;
            }
#if !RESOURCESTREAM_USING_PART
            if (_pageStream != null)
            {
                _pageStream.Close();
            }
#endif
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXCommitPageEnd);
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
            // Add this Page
            //
            dependentList[Uri] = Uri;

            AddRelationshipTypes(selectorList);



            //
            // Collect this pages dependants
            //
            CollectDependents(dependentList );
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
            UpdateResourceCache();
            //
            // Add all XpsResourceDictionarys
            //
            foreach (XpsResourceDictionary resourceDictionary in _resourceDictionaryCache.Values)
            {
                xmlPartList.Add(((INode)resourceDictionary).GetPart());
            }
            if (_storyFragment != null)
            {
                xmlPartList.Add(((INode)_storyFragment).GetPart());
            }
        }

        /// <summary>
        /// Adds dependent part Uris to the passed list following the passed restrictions
        /// dependents include pages, annotaions, properties, and signatures
        /// </summary>
        internal
        void
        CollectDependents(
            Dictionary<Uri,Uri> dependents
             )
        {
            UpdateResourceCache();
            //
            // Add all XpsImages
            //
            foreach( Uri uri in _imageCache.Keys)
            {
                dependents[uri] =  uri;
            }
            //
            // Add all XpsFonts
            //
            foreach( Uri uri in _fontCache.Keys)
            {
                dependents[uri] = uri;
            }
            //
            // Add all XpsColorContexts
            //
            foreach( Uri uri in _colorContextCache.Keys)
            {
                dependents[uri] = uri;
            }
            //
            // Add all XpsResourceDictionarys
            //
            foreach( Uri uri in _resourceDictionaryCache.Keys)
            {
                dependents[uri] = uri;
            }

            //
            // Add any generic resources
            //
            foreach( Uri uri in _resourceCache.Keys)
            {
                dependents[uri] = uri;
            }

            if (_storyFragment != null)
            {
                dependents[_storyFragment.Uri] = _storyFragment.Uri;
             }


            //
            // Add thumbnail
            //
            EnsureThumbnail();
            if( _thumbnail != null )
            {
                dependents[_thumbnail.Uri] = _thumbnail.Uri;
            }
         }

        #endregion Public methods

        #region Private methods

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
        /// The is methods test whether the resource cache
        /// has been initialized and updates if necessary
        /// </summary>
        private
        void
        UpdateResourceCache()
        {
            if( !_hasParsedResources )
            {
                ParseResources();
                 _hasParsedResources = true;
           }
        }

        /// <summary>
        /// This method iterates the relationships and
        /// populates the resource cache
        /// </summary>
        private
        void
        ParseResources()
        {
            //
            // Collect Restricted Fonts
            //
            PackagePart parentPart = ((INode)_parentNode).GetPart();
            Dictionary<Uri,PackageRelationship> restictedFontRels = new Dictionary<Uri,PackageRelationship>(MS.Internal.UriComparer.Default);
            foreach(  PackageRelationship rel in parentPart.GetRelationshipsByType(XpsS0Markup.RestrictedFontRelationshipType) )
            {
               Uri absUri = PackUriHelper.ResolvePartUri(parentPart.Uri, rel.TargetUri);
               restictedFontRels[absUri] = rel;
            }
            foreach(  PackageRelationship rel in _metroPart.GetRelationships() )
            {
                Uri absUri = PackUriHelper.ResolvePartUri(Uri, rel.TargetUri);
                PackagePart part = CurrentXpsManager.GetPart(absUri);
                //
                // if its an image
                //
                if(
                     XpsManager.SupportedImageType(part.ValidatedContentType())
                     && rel.RelationshipType == XpsS0Markup.ResourceRelationshipName
                   )

                {
                    XpsImage image = new XpsImage( CurrentXpsManager, this, part );
                    _imageCache[absUri] = image;
                }
                //
                // If its a font
                //
                else if((part.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.FontContentType) ||
                        part.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.FontObfuscatedContentType))
                        && rel.RelationshipType == XpsS0Markup.ResourceRelationshipName)
                {
                    XpsFont font = new XpsFont( CurrentXpsManager, this, part );
                    _fontCache[absUri] = font;
                    if( restictedFontRels.ContainsKey(absUri ) )
                    {
                        font.IsRestricted = true;
                    }
}
                //
                // If its a colorcontext
                //
                else if (part.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.ColorContextContentType)
                        && rel.RelationshipType == XpsS0Markup.ResourceRelationshipName)
                {
                    XpsColorContext colorContext = new XpsColorContext( CurrentXpsManager, this, part );
                    _colorContextCache[absUri] = colorContext;
                }
                //
                // If its a resourceDictionary
                //
                else if (part.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.ResourceDictionaryContentType)
                        && rel.RelationshipType == XpsS0Markup.ResourceRelationshipName)
                {
                    XpsResourceDictionary resourceDictionary = new XpsResourceDictionary( CurrentXpsManager, this, part );
                    _resourceDictionaryCache[absUri] = resourceDictionary;
                }
                //
                // If its a DocumentStructure
                //
                else if (part.ValidatedContentType().AreTypeAndSubTypeEqual(XpsS0Markup.StoryFragmentsContentType)
                        && rel.RelationshipType == XpsS0Markup.StoryFragmentsRelationshipName)
                {
                    if (_storyFragment != null)
                    {
                        throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOneStoryFragment));
                    }
                    _storyFragment = new XpsStructure( CurrentXpsManager, this, part );
                }
                //
                // If its none of the above
                else
                if( rel.RelationshipType == XpsS0Markup.ResourceRelationshipName )
                {
                    XpsResource xpsResource = new XpsResource(CurrentXpsManager, this, part);
                    _resourceCache[absUri] = xpsResource;
                }
            }
        }

        private
        void
        EnsureThumbnail()
        {
            if( _thumbnail == null )
            {
                _thumbnail = CurrentXpsManager.EnsureThumbnail( this, _metroPart );
                if( _thumbnail != null )
                {
                      _currentChildren.Add( _thumbnail );
                }
            }
        }

        private
        XpsImage
        AddImage (
            PackagePart packagePart
            )
        {
            PackagePart metroPart;
            if (packagePart == null)
            {
                //default to png type
                metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.PngContentType);
            }
            else
            {
                metroPart = packagePart;
            }

            XpsImage xpsImage = new XpsImage(CurrentXpsManager, this, metroPart);

            //
            // Add the relationship
            //
            string resourcePath = XpsManager.MakeRelativePath(this.Uri, metroPart.Uri);
            _metroPart.CreateRelationship(new Uri(resourcePath, UriKind.Relative),
                                       TargetMode.Internal,
                                       XpsS0Markup.ResourceRelationshipName);

            //
            // Make sure the new resource makes it into the cached collection
            //
            _imageCache[xpsImage.Uri] = xpsImage;

            //
            // Keep a reference to the child around
            //
            _currentChildren.Add(xpsImage);


            return xpsImage;
        }

        private
        XpsFont
        AddFont(
            PackagePart packagePart
            )
       {
            PackagePart metroPart;
            if (packagePart == null)
            {
                //default to ObfuscatedFontPart
                metroPart = this.CurrentXpsManager.GenerateObfuscatedFontPart();
            }
            else
            {
                metroPart = packagePart;
            }

            XpsFont xpsFont = new XpsFont(CurrentXpsManager, this, metroPart);

            //
            // Add the relationship
            //
            string resourcePath = XpsManager.MakeRelativePath(this.Uri, metroPart.Uri);
            _metroPart.CreateRelationship(new Uri(resourcePath, UriKind.Relative),
                                       TargetMode.Internal,
                                       XpsS0Markup.ResourceRelationshipName);

            //
            // Make sure the new resource makes it into the cached collection
            //
            _fontCache[xpsFont.Uri] = xpsFont;

            //
            // Keep a reference to the child around
            //
            _currentChildren.Add(xpsFont);


            return xpsFont;
        }

        private
        void
        AddRelationshipTypes(List<PackageRelationshipSelector>   selectorList)
        {
            //
            // Add Required Resource relationship type
            //
            selectorList.Add( new PackageRelationshipSelector(
                                    Uri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.ResourceRelationshipName
                                    )
                                 );
            //
            // Add Story Fragment relationship type
            //
            selectorList.Add( new PackageRelationshipSelector(
                                    Uri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.StoryFragmentsRelationshipName
                                    )
                                 );
            //
            // Add thumnail relationship
            //
            selectorList.Add(new PackageRelationshipSelector(
                                    Uri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.ThumbnailRelationshipName
                                    )
                                 );
        }

        private
        XpsColorContext
        AddColorContext(
            PackagePart packagePart
            )
        {
            PackagePart metroPart;
            if (packagePart == null)
            {
                metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.ColorContextContentType);
            }
            else
            {
                metroPart = packagePart;
            }
            XpsColorContext xpsColorContext = new XpsColorContext(CurrentXpsManager, this, metroPart);

            //
            // Add the relationship
            //
            string resourcePath = XpsManager.MakeRelativePath(this.Uri, metroPart.Uri);
            _metroPart.CreateRelationship(new Uri(resourcePath, UriKind.Relative),
                                       TargetMode.Internal,
                                       XpsS0Markup.ResourceRelationshipName);

            //
            // Make sure the new resource makes it into the cached collection
            //
            _colorContextCache[xpsColorContext.Uri] = xpsColorContext;

            //
            // Keep a reference to the child around
            //
            _currentChildren.Add(xpsColorContext);
            return xpsColorContext;
        }

        private
        XpsResourceDictionary
        AddResourceDictionary(
            PackagePart packagePart
            )
        {
            PackagePart metroPart;
            if (packagePart == null)
            {
                metroPart = this.CurrentXpsManager.GenerateUniquePart(XpsS0Markup.ResourceDictionaryContentType);
            }
            else
            {
                metroPart = packagePart;
            }

            XpsResourceDictionary xpsResourceDictionary = new XpsResourceDictionary(CurrentXpsManager, this, metroPart);

            //
            // Add the relationship
            //
            string resourcePath = XpsManager.MakeRelativePath(this.Uri, metroPart.Uri);
            _metroPart.CreateRelationship(new Uri(resourcePath, UriKind.Relative),
                                       TargetMode.Internal,
                                       XpsS0Markup.ResourceRelationshipName);

            //
            // Make sure the new resource makes it into the cached collection
            //
            _resourceDictionaryCache[xpsResourceDictionary.Uri] = xpsResourceDictionary;

            //
            // Keep a reference to the child around
            //
            _currentChildren.Add(xpsResourceDictionary);
            return xpsResourceDictionary;
        }

        private
        PackagePart
        GeneratePartForResourceType(
            Type     resourceType,
            Uri         resourceUri
            )
        {
            if (resourceUri == null)
            {
                return null;
            }

            ContentType contentType = null;
            if( resourceType == typeof( XpsImage ) )
            {
                contentType = LookupContentTypeForImageUri(resourceUri);
            }
            else if( resourceType == typeof( XpsFont ) )
            {
                contentType = LookupContentTypeForFontUri(resourceUri);
            }
            else if( resourceType == typeof( XpsColorContext ) )
            {
                contentType = XpsS0Markup.ColorContextContentType;
            }
            else if( resourceType == typeof( XpsResourceDictionary ) )
            {
                contentType = XpsS0Markup.ResourceDictionaryContentType;
             }
            else
            {
                contentType = XpsS0Markup.ResourceContentType;
            }
            return this.CurrentXpsManager.GeneratePart(contentType,resourceUri);
}

        /// <summary>
        /// Parses the imageExtension and returns the correct ContentType
        /// </summary>
        /// <param name="imageUri">
        /// Uri of the image to find the ContentType of
        /// </param>
        /// <returns>
        /// A ContentType that matches the imageExtension
        /// </returns>
        private
        static
        ContentType
        LookupContentTypeForImageUri(
            Uri imageUri
            )
        {
            //Extract file extension
            String path = imageUri.OriginalString;
            String extension = Path.GetExtension(path).ToLower(CultureInfo.InvariantCulture);
            //remove .
            extension = extension.Substring(1);

            ContentType contentType = null;
            if (String.CompareOrdinal(extension, XpsS0Markup.JpgExtension) == 0)
            {
                contentType =  XpsS0Markup.JpgContentType;
            }
            else if (String.CompareOrdinal(extension, XpsS0Markup.PngExtension) == 0)
            {
                contentType = XpsS0Markup.PngContentType;
            }
            else if (String.CompareOrdinal(extension, XpsS0Markup.TifExtension) == 0)
            {
                contentType = XpsS0Markup.TifContentType;
            }
            else if (String.CompareOrdinal(extension, XpsS0Markup.WdpExtension) == 0)
            {
                contentType = XpsS0Markup.WdpContentType;
            }
            else
            {
                //default to PNG
                contentType = XpsS0Markup.PngContentType;
            }

            return contentType;
         }


        /// <summary>
        /// Parses the imageExtension and returns the correct ContentType
        /// </summary>
        /// <param name="fontUri">
        /// Uri of the image to find the ContentType of
        /// </param>
        /// <returns>
        /// A ContentType that matches the imageExtension
        /// </returns>
        private
        static
        ContentType
        LookupContentTypeForFontUri(
            Uri fontUri
            )
        {
            //Extract file extension
            String path = fontUri.OriginalString;
            String extension = Path.GetExtension(path).ToLower(CultureInfo.InvariantCulture);
            String fileName = Path.GetFileNameWithoutExtension(path);

            ContentType contentType = null;
            if (String.CompareOrdinal(extension, XpsS0Markup.ObfuscatedFontExt.ToLower(CultureInfo.InvariantCulture)) == 0)
            {
                // Verify that the filename is a valid GUID string
                // Until Guid has a TryParse method we will have to depend on an exception being thrown
                try
                {  
                    Guid guid = new Guid(fileName );
                }
                catch( FormatException )
                {
                    throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_ObfucatedFontNeedGuid));
                }

                contentType =  XpsS0Markup.FontObfuscatedContentType;
            }
            else
            {
                //default to PNG
                contentType = XpsS0Markup.FontContentType;
            }

            return contentType;
         }


        #endregion Private methods

        #region Private data

        private PackagePart _metroPart;
        private PrintTicket _printTicket;

        private XmlPartEditor _partEditor;

        private IList<String> _linkTargetStream;

        //
        // 0 based page numbeer with in document
        //
        private int  _pageNumber;
#if RESOURCESTREAM_USING_PART
        //
        // Metro Resource Part and XML Part Editor for
        // resource dictionary.
        //
        private PackagePart _metroResourcePart;
        private XmlPartEditor _resourcePartEditor;
#else
        private StringWriter _pageStream;
        private StringWriter _resourceStream;
        private StringWriter _resourceDictionaryStream;
        private System.Xml.XmlWriter _pageXmlWriter;
        private System.Xml.XmlWriter _resourceXmlWriter;
        private System.Xml.XmlWriter _resourceDictionaryXmlWriter;
#endif

        //
        // A cache of the resources associated with this page
        // On an existing document this will be populated on query
        // Use UpdateResourceCache to ensure these are filled
        //
        private Dictionary<Uri,XpsImage>                _imageCache;
        private Dictionary<Uri,XpsFont>                 _fontCache;
        private Dictionary<Uri,XpsColorContext>         _colorContextCache;
        private Dictionary<Uri,XpsResourceDictionary>   _resourceDictionaryCache;
        private Dictionary<Uri,XpsResource>             _resourceCache;

        //
        // This variable flags whether the PrintTicket has been
        // committed.  A writer can only commit this property once.
        //
        private bool                                    _isPrintTicketCommitted;

        //
        // These variables are used to keep track of the parent
        // and current child of this node for walking up and
        // down the current tree.  This is used be the flushing
        // policy to do interleave flushing of parts correctly.
        //
        private XpsFixedDocumentReaderWriter            _parentNode;
        private List<INode>                             _currentChildren;

        // This variable flags wheter the _resourceCache
        // has been populated by parsing the relationships
        private bool                                    _hasParsedResources;

        private XpsThumbnail                            _thumbnail;
        private XpsStructure                            _storyFragment;
        #endregion Private data

        #region Internal properties

        internal
        XpsFixedDocumentReaderWriter
        Parent
        {
            get
            {
                return _parentNode;
            }
        }

#if !RESOURCESTREAM_USING_PART
        /// <summary>
        /// Gets a reference to the XmlWriter for the fixed page data.
        /// </summary>
        internal XmlWriter PageXmlWriter
        {
            get
            {
                return _pageXmlWriter;
            }
        }
#endif

        #endregion Internal properties

        #region Internal methods

        /// <summary>
        /// This method prepares the part for being committed by
        /// joining the resource and page stream together as needed.
        ///
        /// NOTE:  This method is no longer needed once the Resource
        /// Dictionary becomes a separate part.
        /// </summary>
        internal
        void
        PrepareCommit(
            )
        {
            //
            // Flush the temp streams
            //
            _resourceXmlWriter.Flush();
            _pageXmlWriter.Flush();

            if(_resourceStream.ToString().Length > 0)
            {
                _resourceDictionaryXmlWriter.WriteStartElement(XpsS0Markup.PageResources);
                _resourceDictionaryXmlWriter.WriteStartElement(XpsS0Markup.ResourceDictionary);

                _resourceDictionaryXmlWriter.WriteRaw(_resourceStream.ToString());

                _resourceDictionaryXmlWriter.WriteEndElement();
                _resourceDictionaryXmlWriter.WriteEndElement();

                _resourceDictionaryXmlWriter.Flush();

                this.XmlWriter.WriteRaw(_resourceDictionaryStream.ToString());
            }
            //
            // Join resource and page stream into main stream
            //
            this.XmlWriter.WriteRaw(_pageStream.ToString());
        }

        #endregion Internal methods

        #region INode implementation

        void
        INode.Flush(
            )
        {
            //
            // Commit the PrintTicket (if necessary)
            //
            CommitPrintTicket();

            if ( _currentChildren!= null && _currentChildren.Count > 0)
            {
#if RESOURCESTREAM_USING_PART
                //
                // Flush the resource editor
                //
                _resourcePartEditor.Flush();
#endif
                _currentChildren.Clear();
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

    /// <summary>
    /// An enumeration of types supported in XpsDocument
    /// </summary>
    public
    enum
    XpsImageType
    {
        /// <summary>
        /// Png Format Bitmap
        /// </summary>
        PngImageType,
        /// <summary>
        /// Jpeg Format Bitmap
        /// </summary>
        JpegImageType,
        /// <summary>
        /// Tif Format Bitmap
        /// </summary>
        TiffImageType,
        /// <summary>
        /// Windows Media Photo Images
        /// </summary>
        WdpImageType
    };
}
