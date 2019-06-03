// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


    Abstract:
        This file contains the definition  and implementation
        for the XpsManager class.  This class is the actual
        control class for creating all things in the Xps
        package.  All layers of the Xps package must go
        through this layer to talk to the Metro packaging APIs.



--*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media;
using System.Windows.Xps.Serialization;
using System.Text;              // for StringBuilder
using System.Windows;
using System.Windows.Xps;
using System.Globalization;
using System.Printing;

using MS.Internal;

using MS.Internal.IO.Packaging.Extensions;
using Package = System.IO.Packaging.Package;
using PackUriHelper = System.IO.Packaging.PackUriHelper;
using PackageRelationship = System.IO.Packaging.PackageRelationship;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// This class implements all the functionality necessary to
    /// directly manipulate a Metro package for the purpose of
    /// creating and reading Xps packages.
    /// </summary>
    internal class XpsManager : IDisposable
    {
        #region Constructors


        /// <summary>
        /// This internal constructor exists so that XpsOMPackagingPolicy can create an empty
        /// instance that does not own or manage any actual package, but that still provides
        /// functionality to generate the Uris for each Xps part
        /// </summary>
        internal
        XpsManager()
        {
            _ownsPackage = false;
            _xpsDocument = null;
            _metroPackage = null;
            _compressionOption = CompressionOption.NotCompressed;
            _streaming = false;

            _contentTypes = new Dictionary<string, int>(11);
            _cachedParts = new Dictionary<Uri, PackagePart>(11);
        }

        /// <summary>
        /// Internal Constructor to create and initialize the
        /// XpsManager class.
        /// </summary>
        /// <param name="metroPackage">
        /// The Metro package to operate on.
        /// </param>
        /// <param name="compressionOption">
        /// The compression options used when creating new parts
        /// in the Metro package.
        /// </param>
        internal
        XpsManager(
            Package                     metroPackage,
            CompressionOption           compressionOption
            )
        {
            bool streaming = false;
            _ownsPackage = false;
            if( metroPackage != null )
            {
                //streaming = metroPackage.InStreamingCreation();
            }

            Initialize( metroPackage,
                        compressionOption,
                        streaming);
        }


        /// <summary>
        /// Internal Constructor to create and initialize the
        /// XpsManager class.
        /// </summary>
        /// <param name="path">
        /// Path to package to operate on.
        /// </param>
        /// <param name="packageAccess">
        /// mode to the package can be accessed (i.e. Read, Write, Read/Write )
        /// </param>
        /// <param name="compressionOption">
        /// The compression options used when creating new parts
        /// in the Metro package.
        /// </param>
        internal
        XpsManager(
            string                      path,
            FileAccess                  packageAccess,
            CompressionOption           compressionOption
            )
        {
            _ownsPackage = true;
            
            _uri = new Uri(path, UriKind.RelativeOrAbsolute);
            //
            //The URI has to be absolute
            //If the path passed in is relative append it to the
            //current working directory
            //
            if( !_uri.IsAbsoluteUri)
            {
                _uri = new Uri( new Uri(Directory.GetCurrentDirectory()+"/"), path );
            }

            Package package =  PackageStore.GetPackage( _uri );
            // Consider collapsing these since we don't have streaming anymore.
            bool streaming = false;
            if( package == null )
            {
                if( packageAccess == FileAccess.Write )
                {
                    package = Package.Open(path,
                                           FileMode.Create,
                                           packageAccess,
                                           FileShare.None);
                    streaming = true;
                }
                else
                {
                    package = Package.Open(path,
                                          (packageAccess== FileAccess.Read) ?  FileMode.Open: FileMode.OpenOrCreate,
                                           packageAccess,
                                          (packageAccess== FileAccess.Read) ?  FileShare.Read: FileShare.None
                                          );
                }
                    
                AddPackageToCache( _uri, package );               
            }
            else
            {
                //
                // If either the previous opened package or
                // this open request is not File Access Read
                // throw UnauthorizedAccessException
                //
                if( packageAccess != FileAccess.Read ||
                    package.FileOpenAccess != FileAccess.Read )
                {
                    throw new UnauthorizedAccessException();
                }
                AddPackageReference( _uri );
            }
            Initialize( package,
                        compressionOption,
                        streaming);
        }

        static XpsManager()
        {
            _globalLock = new Object();
            _packageCache = new Dictionary<Uri,int>();
        }
        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets or sets the Starting Part for the Metro package.
        /// You can only set the Starting Part once.  Subsequence
        /// attempts to set the Starting Part once it has a root will
        /// cause an InvalidOperationException to be thrown.
        /// </summary>
        /// <value>A Metro Part representing the root.</value>
        public PackagePart StartingPart
        {
            get
            {
                PackagePart part = null;
                if (null != _metroPackage)
                {
                    part = GetXpsDocumentStartingPart(_metroPackage);
                }

                return part;
            }
            set
            {
                if (null == _metroPackage)
                {
                    throw new ObjectDisposedException("XpsManager");
                }
                if ( !Streaming && null != GetXpsDocumentStartingPart(_metroPackage))
                {
                    throw new XpsPackagingException( SR.Get( SRID.ReachPackaging_AlreadyHasStartingPart ) );
                }

                SetXpsDocumentStartingPart(_metroPackage, value);
            }
        }

        /// <summary>
        /// Gets the XpsDocument that owns the XpsManager
        /// </summary>
        /// <value>A boolean flag.</value>
        public
        XpsDocument
        XpsDocument
        {
            set
            {
                _xpsDocument = value;
            }

            get
            {
                return _xpsDocument;
            }
        }

        /// <summary>
        /// Gets a flag specifying whether this package is writable.
        /// </summary>
        /// <value>A boolean flag.</value>
        public bool IsWriter
        {
            get
            {
                if (null == _metroPackage)
                {
                    return false ;
                }

                return _metroPackage.FileOpenAccess != System.IO.FileAccess.Read;
            }
        }

        /// <summary>
        /// Gets a flag specifying whether this package is readable.
        /// </summary>
        /// <value>A boolean flag.</value>
        public bool IsReader
        {
            get
            {
                if (null == _metroPackage)
                {
                    return false;
                }

                return _metroPackage.FileOpenAccess != System.IO.FileAccess.Write;
            }
        }


        public Package MetroPackage
        {
            get
            {
                return _metroPackage;
            }
        }

        public bool Streaming
        {
            get
            {
                return _streaming;
            }
        }

        #endregion Public properties

        #region Public methods
        /// <summary>
        /// Generate a unique Part for the content type and add it to the package.
        /// Adding to any relationships or selector/sequence markup is not done.
        /// </summary>
        public
        PackagePart
        GeneratePart(
            ContentType contentType,
            Uri      	partUri
            )
       {
            if (null == _metroPackage)
            {
                throw new ObjectDisposedException("XpsManager");
            }
            if (!IsWriter)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_OnlyWriters));
            }
            if (null == contentType)
            {
                throw new ArgumentNullException("contentType");
            }
            if (0 == contentType.ToString().Length)
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_InvalidContentType, contentType), "contentType");
            }
            
            //Do not compress image Content Types
            CompressionOption compressionOption = _compressionOption;

            if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.JpgContentType) ||
                contentType.AreTypeAndSubTypeEqual(XpsS0Markup.PngContentType) ||
                contentType.AreTypeAndSubTypeEqual(XpsS0Markup.TifContentType) ||
                contentType.AreTypeAndSubTypeEqual(XpsS0Markup.WdpContentType))
            {
                compressionOption = CompressionOption.NotCompressed;
            }

            PackagePart metroPart = _metroPackage.CreatePart(partUri,
                                                             contentType.ToString(),
                                                             compressionOption);

            //
            // Cache the newly generated part
            //
            _cachedParts[partUri] = metroPart;

            return metroPart;
        }

        /// <summary>
        /// Generate a unique Part for the content type and add it to the package.
        /// Adding to any relationships or selector/sequence markup is not done.
        /// </summary>
        /// <param name="contentType">
        /// The content type of the Part that will be created in the Metro package.
        /// </param>
        /// <returns>
        /// Returns the newly created Metro Part.
        /// </returns>
        public
        PackagePart
        GenerateUniquePart(
            ContentType contentType
            )
        {
            if (null == _metroPackage)
            {
                throw new ObjectDisposedException("XpsManager");
            }
            if (!IsWriter)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_OnlyWriters));
            }
            if (null == contentType)
            {
                throw new ArgumentNullException("contentType");
            }
            if (ContentType.Empty.AreTypeAndSubTypeEqual(contentType))
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_InvalidType));
            }

            //
            // Generate a unique part Uri
            //
            System.Uri partUri = GenerateUniqueUri(contentType);

            return GeneratePart( contentType, partUri);
       }

        public
        PrintTicket
        EnsurePrintTicket(
            Uri partUri
            )
        {
            PrintTicket printTicket = null;
            PackagePart printTicketPart = GetPrintTicketPart(partUri);
            if( printTicketPart != null )
            {
                 printTicket = new PrintTicket(printTicketPart.GetStream());
            }
            return printTicket;
        }

        /// <summary>
        /// Generate a unique obfusated Font Part add it to the package.
        /// Adding to any relationships or selector/sequence markup is not done.
        /// </summary>
        /// <returns>
        /// Returns the newly created Metro Part.
        /// </returns>
        public
        PackagePart
        GenerateObfuscatedFontPart(
            )
        {
            if (null == _metroPackage)
            {
                throw new ObjectDisposedException("XpsManager");
            }
            if (!IsWriter)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_OnlyWriters));
            }

            //
            // Generate a unique part Uri
            //
            String uniqueUri = "/Resources/" + Guid.NewGuid().ToString() + XpsS0Markup.ObfuscatedFontExt;
            System.Uri partUri = PackUriHelper.CreatePartUri(new Uri(uniqueUri, UriKind.Relative));

            PackagePart metroPart = _metroPackage.CreatePart(partUri,
                                                             XpsS0Markup.FontObfuscatedContentType.ToString(),
                                                             _compressionOption);

            //
            // Cache the newly generated part
            //
            _cachedParts[partUri] = metroPart;

            return metroPart;
        }

        /// <summary>
        /// This method writes a print ticket part and its contents to the
        /// Metro package and adds a relationship associate the print ticket
        /// part with the specified Metro part.
        /// </summary>
        /// <param name="relatedPart">
        /// The Xps part that will be associated with this print ticket.
        /// </param>
        /// <param name="metroPart">
        /// The Metro part that will be associated with this print ticket.
        /// </param>
        /// <param name="printTicket">
        /// The print ticket data to be saved to the print ticket part.
        /// </param>
        public
        void
        WritePrintTicket(
            XpsPartBase     relatedPart,
            PackagePart     metroPart,
            PrintTicket     printTicket
            )
        {
            if (null == relatedPart)
            {
                throw new ArgumentNullException("relatedPart");
            }
            if (null == metroPart)
            {
                throw new ArgumentNullException("metroPart");
            }
            if (null == printTicket)
            {
                throw new ArgumentNullException("printTicket");
            }

            //
            // Generate Uri
            //
            Uri printTicketUri = GeneratePrintTicketUri(relatedPart);
            //
            // Generate a part for the ticket
            //
            PackagePart printTicketPart = GeneratePart(XpsS0Markup.PrintTicketContentType, printTicketUri);

            //
            // Add a relationship to link ticket to supplied part
            //
            string relativePath = XpsManager.MakeRelativePath(metroPart.Uri, printTicketPart.Uri);
            metroPart.CreateRelationship(new Uri(relativePath, UriKind.Relative),
                                        TargetMode.Internal,
                                        XpsS0Markup.PrintTicketRelationshipName);

            //
            // Write print ticket to print ticket part data stream
            //
            Stream partDataStream;
            if (_metroPackage.FileOpenAccess == FileAccess.Write)
            {
                partDataStream = printTicketPart.GetStream(FileMode.Create);
            }
            else
            {
                partDataStream = printTicketPart.GetStream(FileMode.OpenOrCreate);
            }
            printTicket.SaveTo(partDataStream);
            partDataStream.Close();
        }

        /// <summary>
        /// This method writes an empty print ticket part  and adds a relationship 
        /// associate the print ticket part with the specified Metro part. It only
        /// does so when the document is streaming.
        /// </summary>
        /// <param name="relatedPart">
        /// The Xps part that will be associated with this print ticket.
        /// </param>
        /// <param name="metroPart">
        /// The Metro part that will be associated with this print ticket.
        /// </param>
        public
        void
        WriteEmptyPrintTicket(
            XpsPartBase     relatedPart,
            PackagePart     metroPart
            )
        {
            if (null == relatedPart)
            {
                throw new ArgumentNullException("relatedPart");
            }
            if (null == metroPart)
            {
                throw new ArgumentNullException("metroPart");
            }
            if( Streaming )
            {
                // Generate Uri
                //
                Uri printTicketUri = GeneratePrintTicketUri(relatedPart);
                //
                // Generate a part for the ticket
                //
                PackagePart printTicketPart = GeneratePart(XpsS0Markup.PrintTicketContentType, printTicketUri);

                //
                // Add a relationship to link ticket to supplied part
                //
                string relativePath = XpsManager.MakeRelativePath(metroPart.Uri, printTicketPart.Uri);
                metroPart.CreateRelationship(new Uri(relativePath, UriKind.Relative),
                                            TargetMode.Internal,
                                            XpsS0Markup.PrintTicketRelationshipName);
                Stream stream = printTicketPart.GetStream(FileMode.Create);
                PrintTicket printTicket = new PrintTicket();
                printTicket.SaveTo(stream);
                stream.Close();
            }
         }

        /// <summary>
        /// This method retrieves a part from the package.
        /// </summary>
        /// <param name="uri">
        /// The URI of the part to retrieve.
        /// </param>
        /// <returns>
        /// Returns a Metro Part for the specified URI if one exists.
        /// Otherwise, throws an exception.
        /// </returns>
        public
        PackagePart
        GetPart(
            Uri         uri
            )
        {
            if (null == _metroPackage)
            {
                throw new ObjectDisposedException("XpsManager");
            }

            if (_cachedParts.ContainsKey(uri))
            {
                return _cachedParts[uri];
            }

            if (!_metroPackage.PartExists(uri))
            {
                return null;
            }

            PackagePart part = _metroPackage.GetPart(uri);
            _cachedParts[uri] = part;

            return part;
        }

        public
        PackagePart
        AddSignatureDefinitionPart(PackagePart documentPart)
        {
            //
            // If a part exists return the existing part
            //
            PackagePart sigDefPart = GetSignatureDefinitionPart(documentPart.Uri);

            if( sigDefPart == null )
            {
                sigDefPart = GenerateUniquePart(XpsS0Markup.SignatureDefintionType );
                documentPart.CreateRelationship(
                    sigDefPart.Uri,
                    TargetMode.Internal,
                    XpsS0Markup.SignatureDefinitionRelationshipName
                    );
            }
            return sigDefPart;
        }

        public
        PackagePart
        GetSignatureDefinitionPart(Uri documentUri)
        {
            PackagePart documentPart = _metroPackage.GetPart( documentUri );
            PackagePart sigDefPart = null;

            if( documentPart == null )
            {
              throw new InvalidDataException(SR.Get(SRID.ReachPackaging_InvalidDocUri));
            }

            ContentType SignitureDefType =
                XpsS0Markup.SignatureDefintionType;
            PackageRelationship SigDefRel = null;

            foreach (PackageRelationship rel in
                     documentPart.GetRelationshipsByType(XpsS0Markup.SignatureDefinitionRelationshipName))
            {
                if (SigDefRel != null)
                {
                    throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOneSigDefParts));
                }

                SigDefRel = rel;
            }
            if (SigDefRel != null)
            {
                sigDefPart = _metroPackage.GetPart(PackUriHelper.ResolvePartUri(SigDefRel.SourceUri, SigDefRel.TargetUri));
            }
            return sigDefPart;
        }

        public
        PackagePart
        GetDocumentPropertiesPart()
        {
            PackageRelationship propertiesPartRelationship =
                   GetDocumentPropertiesReationship();

            PackagePart propertiesPart = null;
            Package package = _metroPackage;


            if (propertiesPartRelationship != null)
            {
                Uri propertiesPartUri = 
                    PackUriHelper.ResolvePartUri(propertiesPartRelationship.SourceUri, 
                                                 propertiesPartRelationship.TargetUri);

                if (package.PartExists(propertiesPartUri))
                {
                    propertiesPart = package.GetPart(propertiesPartUri);
                }
            }

            return propertiesPart;
        }

        public
        PackagePart
        GetThumbnailPart(PackagePart parent)
        {
            PackageRelationship thumbNailRel = null;
            PackagePart         thumbNailPart = null;
            PackageRelationshipCollection thumbnailCollection = null;
            if( parent != null )
            {
                thumbnailCollection = parent.
                    GetRelationshipsByType(XpsS0Markup.ThumbnailRelationshipName);
            }
            else
            {
                thumbnailCollection = _metroPackage.
                    GetRelationshipsByType(XpsS0Markup.ThumbnailRelationshipName);
            }

            foreach( PackageRelationship rel in thumbnailCollection )
            {
                if( thumbNailRel != null )
                {
                    throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOneThumbnailPart));
                }
                thumbNailRel =  rel;
            }
            if (thumbNailRel != null)
            {
                thumbNailPart = _metroPackage.GetPart(PackUriHelper.ResolvePartUri(thumbNailRel.SourceUri, thumbNailRel.TargetUri));
            }
            return thumbNailPart;
        }

        public
        PackagePart
        GetPrintTicketPart(Uri documentUri)
        {
            PackagePart documentPart = _metroPackage.GetPart( documentUri );
            PackagePart printTicketPart = null;

            if( documentPart == null )
            {
              throw new InvalidDataException(SR.Get(SRID.ReachPackaging_InvalidDocUri));
            }

            string printTicketRelName =
                XpsS0Markup.PrintTicketRelationshipName;
            PackageRelationship printTicketRel = null;

            foreach (PackageRelationship rel in
                     documentPart.GetRelationshipsByType(printTicketRelName))
            {
                if (printTicketRel != null)
                {
                    throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOnePrintTicketPart ));
                }

                printTicketRel = rel;
            }
            if (printTicketRel != null)
            {
                Uri printTicketUri = PackUriHelper.ResolvePartUri(documentUri, printTicketRel.TargetUri);
                printTicketPart = _metroPackage.GetPart(printTicketUri);
            }
            return printTicketPart;
        }

        public
        PackagePart
        AddDocumentPropertiesPart()
        {
            //
            // If a part exists return the existing part
            //
            PackagePart propertiesPart = GetDocumentPropertiesPart();

            if( propertiesPart == null )
            {
                propertiesPart = GenerateUniquePart(XpsS0Markup.CoreDocumentPropertiesType );
                _metroPackage.CreateRelationship(propertiesPart.Uri, TargetMode.Internal, XpsS0Markup.CorePropertiesRelationshipType );
            }
            return propertiesPart;
        }

        public
        XpsThumbnail
        AddThumbnail(XpsImageType imageType, INode parent, XpsThumbnail oldThumbnail )
        {
            XpsThumbnail newThumbnail = null;
            if( oldThumbnail != null )
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_AlreadyHasThumbnail));
            }    
            if( !( imageType == XpsImageType.JpegImageType ||
                    imageType == XpsImageType.PngImageType ) )
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_UnsupportedThumbnailImageType));
            }   
            newThumbnail = new XpsThumbnail(this,
                                        parent,
                                        GenerateUniquePart(ImageTypeToString(imageType))
                                        );
            return newThumbnail;
        }

        public
        XpsThumbnail
        EnsureThumbnail( INode parent, PackagePart part )
        {
            XpsThumbnail thumbnail = null;
            PackagePart thumbNailPart = GetThumbnailPart(part);
            if( thumbNailPart != null )
            {
                thumbnail = new XpsThumbnail(this,
                                            parent,
                                            thumbNailPart
                                            );
            }
            return thumbnail;
        }

        /// <summary>
        /// This method will add the DocumentPropertiesPart
        /// to the dependent collection based on the restrictions.
        /// If the properties do not exist then a blank property part will be
        /// created so that it can be modified at a later time with
        /// affeting the relationship part.
        /// </summary>
        /// <param name="dependents">
        /// A dictionary of URIs to be signed.
        /// </param>
        /// <param name="restrictions">
        /// Flags indicating what parts are to be added to the signatures.
        /// Thus defining what changes are restricted after signing.
        /// </param>
        public
        void
        CollectPropertiesForSigning(
            Dictionary<Uri,Uri> dependents,
            XpsDigSigPartAlteringRestrictions restrictions
            )
        {
            PackagePart propertiesPart = GetDocumentPropertiesPart();
            if( (restrictions & XpsDigSigPartAlteringRestrictions.CoreMetadata) != 0 )
            {
                if( propertiesPart != null )
                {
                    dependents[propertiesPart.Uri] = propertiesPart.Uri;
                }
            }
        }

        public
        Uri
        GetSignatureOriginUri()
        {
            PackageDigitalSignatureManager digSigMgr = new PackageDigitalSignatureManager(_metroPackage);
            Uri SigOriginRelUri = digSigMgr.SignatureOrigin;

            return SigOriginRelUri;
        }

        /// <summary>
        /// This method will add the Signiture Origin
        /// to the dependent collection based on the restrictions.
        /// </summary>
        /// <param name="selectorList">
        /// A dictionary of URIs to be signed.
        /// </param>
        /// <param name="restrictions">
        /// Flags indicating what parts are to be added to the signatures.
        /// Thus defining what changes are restricted after signing.
        /// </param>
        public
        void
        CollectSignitureOriginForSigning(
            List<PackageRelationshipSelector> selectorList,
            XpsDigSigPartAlteringRestrictions restrictions
            )
        {
            if( (restrictions & XpsDigSigPartAlteringRestrictions.SignatureOrigin) != 0 )
            {
                Uri SigOriginUri = GetSignatureOriginUri();
                selectorList.Add(new PackageRelationshipSelector(
                                        SigOriginUri,
                                        PackageRelationshipSelectorType.Type,
                                        XpsS0Markup.DitialSignatureRelationshipType
                                        )
                                     );
            }
        }

        /// <summary>
        /// This method signs the Parts specified in the collection
        /// of URIs with the specified certificate.
        /// </summary>
        /// <param name="partList">
        /// A collection of URI to pakage parts to be signed.
        /// </param>
        /// <param name="certificate">
        /// The certificate used for signing.  If this is null then
        /// a UI is shown to select the certificate.
        /// </param>
        /// <param name="embedCertificate">
        /// Flag indicating whether to embed the certificate.
        /// The certificate will be embeded in its own part
        /// </param>
        /// <param name="relationshipSelectors">
        /// list of relationshipSelectors to be signed
        /// </param>
        /// <param name="id">
        /// Id assigned to signature
        /// </param>
        public
        PackageDigitalSignature
        Sign(
            IEnumerable<System.Uri>                     partList,
            X509Certificate                             certificate,
            bool                                        embedCertificate,
            IEnumerable<PackageRelationshipSelector>    relationshipSelectors,
            string                                      id
            )
        {
            PackageDigitalSignature signature = null;
            if (null == _metroPackage)
            {
                throw new ObjectDisposedException("XpsManager");
            }

            PackageDigitalSignatureManager dsm = new PackageDigitalSignatureManager(_metroPackage);
            if( embedCertificate )
            {
                dsm.CertificateOption = CertificateEmbeddingOption.InCertificatePart;
            }
            else
            {
                dsm.CertificateOption = CertificateEmbeddingOption.NotEmbedded;
            }

            if( id != null )
            {
               signature = dsm.Sign(partList,  certificate, relationshipSelectors, id );
            }
            else
            {
                signature = dsm.Sign(partList,  certificate, relationshipSelectors );
            }
            return signature;
        }

        /// <summary>
        /// Writes the close elements for both the starting part and the package
        /// </summary>
        public
        void
        Close(
            )
        {
            if (null == _metroPackage)
            {
                return;
            }

            if (IsWriter)
            {
                // Only flush if writing
                _metroPackage.Flush();
            }

            if( _ownsPackage )
            {
               RemovePackageReference(_uri, _metroPackage);
            }

            GC.SuppressFinalize(this);
            _metroPackage = null;
            _cachedParts = null;
            _contentTypes = null;
        }

        #endregion Public methods

        /// <summary>
        /// This method is used to create unique Uri for document structure.
        /// The output is "/Document/_docNumber/Structure/DocStructure.struct"
        /// </summary>
        internal
        Uri
        CreateStructureUri
        (
        )
        {
            string docContentKey = GetContentCounterKey(XpsS0Markup.FixedDocumentContentType);
             int docCounter = 0;
 
            if (_contentTypes.ContainsKey(docContentKey))
            {
                docCounter = _contentTypes[docContentKey]-1;
            }

            return new Uri("/Documents/" + docCounter + "/Structure/DocStructure.struct",
                           UriKind.Relative);
      }

        /// <summary>
        /// This method is used to create unique Uri for document structure.
        /// The output is "/Document/_docNumber/Structure/DocStructure.struct"
        /// </summary>\
        internal
        Uri
        CreateFragmentUri
        (
            int pageNumber
        )
        {
            string docContentKey = GetContentCounterKey(XpsS0Markup.FixedDocumentContentType);
             int docCounter = 0;
 
            if (_contentTypes.ContainsKey(docContentKey))
            {
                docCounter = _contentTypes[docContentKey]-1;
            }

            return new Uri("/Documents/" + docCounter + "/Structure/Fragments/"+pageNumber+".frag",
                           UriKind.Relative);
      }
        #region Private methods

        /// <summary>
        /// This method is used to initialize a newly created
        /// XpsManager.
        /// </summary>
        /// <param name="metroPackage">
        /// The Metro package to associate with this manager.
        /// </param>
        /// <param name="compressionOption">
        /// The compression options for newly created parts.
        /// </param>
        /// <param name="streaming">
        /// Flag indicating that file will written foward only streaming.
        /// </param>
        private
        void
        Initialize(
            Package                     metroPackage,
            CompressionOption           compressionOption,
            bool                        streaming
            )
        {
            if (null == metroPackage)
            {
                throw new ArgumentNullException("metroPackage");
            }

            _xpsDocument = null;
            _metroPackage = metroPackage;
            _compressionOption = compressionOption;
            _streaming = streaming;
 

            _contentTypes = new Dictionary<string, int>(11);
            _cachedParts = new Dictionary<Uri, PackagePart>(11);
        }

        /// <summary>
        /// Generates a unique Uri for a print ticket
        /// Placing it in the proper diretory and following
        /// the naming pattern by which part it associated with
        /// PT associated with the entire job (i.e. DocSeq.)
        /// go in a /MetaData directory
        /// The rest go in /Documents/n/MetaData where
        /// n in document it is associated with
        /// document level print tickets are call Document_PT
        /// page level print tickets are called Pagen_PT where
        /// n is the page number
        /// </summary>
        private
        Uri
        GeneratePrintTicketUri(
            object relatedPart
            )
        {
            if (null == relatedPart)
            {
                throw new ArgumentNullException("relatedPart");
            }
            string uniqueUri = "";
            
            if( relatedPart is XpsFixedDocumentSequenceReaderWriter )
            {
               uniqueUri = "/MetaData/Job_PT.xml"; 
            }
            else if( relatedPart is XpsFixedDocumentReaderWriter )
            {
                XpsFixedDocumentReaderWriter doc = relatedPart as XpsFixedDocumentReaderWriter;
                uniqueUri = "/Documents/" + doc.DocumentNumber + "/Document_PT.xml"; 
            }
            else if( relatedPart is XpsFixedPageReaderWriter )
            {
                XpsFixedPageReaderWriter page = relatedPart as XpsFixedPageReaderWriter;
                XpsFixedDocumentReaderWriter doc = (relatedPart as XpsFixedPageReaderWriter).Parent as XpsFixedDocumentReaderWriter;
                uniqueUri = "/Documents/" + doc.DocumentNumber + "/Page" + page.PageNumber+ "_PT.xml"; 
            }
                
            return PackUriHelper.CreatePartUri(new Uri(uniqueUri, UriKind.Relative));
       }

        /// <summary>
        /// Generates a unique Uri for a print ticket
        /// Based on ContentType using the content type counters set
        /// in GenerateUniqueUri
        /// </summary>
        internal
        Uri
        GeneratePrintTicketUri(
            ContentType contentType
            )
        {
            if (null == contentType)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            string uniqueUri = "";

            if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.DocumentSequenceContentType))
            {
                uniqueUri = "/MetaData/Job_PT.xml";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FixedDocumentContentType))
            {
                string contentKey = GetContentCounterKey(XpsS0Markup.FixedDocumentContentType);
                int docNumber = _contentTypes[contentKey] - 1;
                uniqueUri = "/Documents/" + docNumber + "/Document_PT.xml";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FixedPageContentType))
            {
                string documentContentKey = GetContentCounterKey(XpsS0Markup.FixedDocumentContentType);
                string pageContentKey = GetContentCounterKey(XpsS0Markup.FixedPageContentType);
                int docNumber = _contentTypes[documentContentKey] - 1;
                int pageNumber = _contentTypes[pageContentKey] - 1;
                uniqueUri = "/Documents/" + docNumber + "/Page" + pageNumber + "_PT.xml";
            }

            return PackUriHelper.CreatePartUri(new Uri(uniqueUri, UriKind.Relative));
        }
            

        /// <summary>
        /// Generates a unique Uri based on the content-type
        /// supplied.
        /// </summary>
        /// <param name="contentType">
        /// Content-type of part to be added.
        /// </param>
        /// <returns>
        /// A Uri that is unique to the package.
        /// </returns>
        internal
        Uri
        GenerateUniqueUri(
            ContentType      contentType
            )
        {
            string contentKey = GetContentCounterKey(contentType);
            string docContentKey = GetContentCounterKey(XpsS0Markup.FixedDocumentContentType);
            int counter = _contentTypes[contentKey];
            int docCounter = 0;
            Guid   uniqueName = Guid.NewGuid();
            string uniqueUri;

            if (_contentTypes.ContainsKey(docContentKey))
            {
                docCounter = _contentTypes[docContentKey]-1;
            }

            if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.DocumentSequenceContentType))
            {
                 uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "{0}.fdseq",
                                          new object[] { contentKey });
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FixedDocumentContentType))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Documents/{0}/FixedDocument.fdoc",
                                          new object[] {  counter });
                string pageContentKey = GetContentCounterKey(XpsS0Markup.FixedPageContentType);
                _contentTypes[pageContentKey] = 1;
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FixedPageContentType))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Documents/{0}/Pages/{1}.fpage",
                                          new object[] { docCounter, counter });
            }
            else if (contentKey.Equals("Dictionary", StringComparison.OrdinalIgnoreCase))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Resources/{0}.dict",
                                          new object[] { uniqueName });
            }
            else if (contentKey.Equals("Font", StringComparison.OrdinalIgnoreCase))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Resources/{0}.ttf",
                                          new object[] { uniqueName });
            }
            else if (contentKey.Equals("ColorContext", StringComparison.OrdinalIgnoreCase))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Resources/{0}.icc",
                                          new object[] { uniqueName });
            }
            else if (contentKey.Equals("ResourceDictionary", StringComparison.OrdinalIgnoreCase))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Resources/{0}.dict",
                                          new object[] { uniqueName });
            }
            else if (contentKey.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/Resources/{0}.{1}",
                                          new object[] { uniqueName, LookupImageExtension(contentType) });
            }
            else
            {
                uniqueUri = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "/{0}s/{0}_{1}.xaml",
                                          new object[] { contentKey, counter });
            }

            //
            // Update the cached counter
            //
            counter++;
            _contentTypes.Remove(contentKey);
            _contentTypes[contentKey] = counter;

            return PackUriHelper.CreatePartUri(new Uri(uniqueUri, UriKind.Relative));
        }

        /// <summary>
        /// Retrieves the content counter key for the given
        /// content-type.  This counter key is stored in a
        /// cache and is used as a key into a hashtable for
        /// keeping track of part Uri naming.
        /// </summary>
        /// <param name="contentType">
        /// Content-type of key to retrieve.
        /// </param>
        /// <returns>
        /// A string containing the counter key.
        /// </returns>
        private
        string
        GetContentCounterKey(
            ContentType  contentType
            )
        {
            if (ContentType.Empty.AreTypeAndSubTypeEqual(contentType))
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_InvalidContentType, contentType), "contentType");
            }

            string key;

            if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.DocumentSequenceContentType))
            {
                key = "FixedDocumentSequence";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FixedDocumentContentType))
            {
                key = "FixedDocument";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FixedPageContentType))
            {
                key = "FixedPage";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.DocumentStructureContentType))
            {
                key = "DocumentStructure";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.PrintTicketContentType))
            {
                key = "PrintTicket";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.FontContentType))
            {
                key = "Font";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.ColorContextContentType))
            {
                key = "ColorContext";
            }
            else if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.ResourceDictionaryContentType))
            {
                key = "ResourceDictionary";
            }
            else
            {
                if (String.CompareOrdinal(contentType.TypeComponent.ToUpper(CultureInfo.InvariantCulture),
                                          "Image".ToUpper(CultureInfo.InvariantCulture))==0)
                {
                    key = "Image";
                }
                else
                {
                    key = contentType.SubTypeComponent;
                }                
            }

            //
            // If the contentType counter has not been cached already then
            // cache a new counter and start it at 1.
            //
            if (!_contentTypes.ContainsKey(key))
            {
                _contentTypes[key] = 1;
            }

            return key;
        }

        private
        PackageRelationship
        GetDocumentPropertiesReationship()
        {
            PackageRelationship propertiesPartRelationship = null;
            foreach (PackageRelationship rel in _metroPackage.GetRelationshipsByType(XpsS0Markup.CorePropertiesRelationshipType))
            {
                if (propertiesPartRelationship != null)
                {
                    throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOneMetaDataParts ));
                }

                propertiesPartRelationship = rel;
            }
            return propertiesPartRelationship;
        }

        private
        void
        AddPackageToCache(Uri uri, Package package )
        {
            lock (_globalLock)
            {
                _packageCache[uri] = 1;
            }
            PackageStore.AddPackage( uri, package );
        }

        private
        void
        AddPackageReference( Uri uri )
        {
            lock (_globalLock)
            {
                _packageCache[uri] = _packageCache[uri]+1;
            }
        }

        private
        void
        RemovePackageReference( Uri uri, Package package )
        {
            int reference = 0;
            lock (_globalLock)
            {
                reference = _packageCache[uri];

                reference -= 1;
                
                if(reference > 0 )
                {
                    _packageCache[uri] = reference;
                }
                else
                {
                    // do the _packageCache manipulation inside the lock,
                    //   but defer additional cleanup work to do outside of the lock.
                    _packageCache.Remove( uri );
                }
            }

            // outside of the context of the lock, perform any necessary additional cleanup
            if (reference <= 0)
            {
                PackageStore.RemovePackage( uri );
                package.Close();
            }
        }
        #endregion Private methods

        #region Private data

        private XpsDocument                     _xpsDocument;
        private Package                         _metroPackage;
        private Uri                             _uri;

        private CompressionOption               _compressionOption;


        private Dictionary<string, int>         _contentTypes;
        private Dictionary<Uri, PackagePart>    _cachedParts;
        private bool                            _streaming;
        private bool                            _ownsPackage;

        internal static Dictionary<Uri, int>    _packageCache;
        internal static Object                  _globalLock;
        
        #endregion Private data

        #region IDisposable implementation

        void
        IDisposable.Dispose(
            )
        {
            Close();
        }

        #endregion IDisposable implementation

        #region Private static methods

        /// <summary>
        /// Parses the content-type and determines the extension
        /// that should be used for the given content.
        /// </summary>
        /// <param name="contentType">
        /// Content-Type for content.
        /// </param>
        /// <returns>
        /// A string containing the extension.
        /// </returns>
        private
        static
        string
        LookupImageExtension(
            ContentType contentType
            )
        {
            string extention = XpsS0Markup.PngExtension;
            if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.JpgContentType))
            {
                extention = XpsS0Markup.JpgExtension;
            }
            else
                if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.PngContentType))
            {
                extention = XpsS0Markup.PngExtension;
            }
            else
                if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.TifContentType))
            {
                extention = XpsS0Markup.TifExtension;
            }
            else
                if (contentType.AreTypeAndSubTypeEqual(XpsS0Markup.WdpContentType))
            {
                extention = XpsS0Markup.WdpExtension;
            }
            return extention;                
        }
        
        #endregion Private static methods

        #region Internal static data

        #endregion Internal static data

        #region Internal static methods

        /// <summary>
        /// Gets the starting part of the package.
        /// </summary>
        /// <param name="package">
        /// The package to find the starting part on.
        /// </param>
        /// <returns>
        /// PackagingPart instance of starting part.
        /// </returns>
        internal
        static
        PackagePart
        GetXpsDocumentStartingPart(
            Package         package
            )
        {
            Debug.Assert(package != null, "package cannot be null");
            PackageRelationship startingPartRelationship = null;
            PackagePart startingPart = null;

            foreach (PackageRelationship rel in package.GetRelationshipsByType(XpsS0Markup.ReachPackageStartingPartRelationshipType))
            {
                if (startingPartRelationship != null)
                {
                    throw new InvalidDataException(SR.Get(SRID.ReachPackaging_MoreThanOneStartingParts));
                }

                startingPartRelationship = rel;
            }

            if (startingPartRelationship != null)
            {
                Uri startPartUri = PackUriHelper.ResolvePartUri(startingPartRelationship.SourceUri, 
                                                                startingPartRelationship.TargetUri);

                if (package.PartExists(startPartUri))
                {
                    startingPart = package.GetPart(startPartUri);
                }
            }

            return startingPart;
        }

        /// <summary>
        /// Sets/Replaces the starting part for a given package.
        /// </summary>
        /// <param name="package">
        /// Package to set the starting part for.
        /// </param>
        /// <param name="startingPart">
        /// Part to set as starting part on the package.
        /// </param>
        internal
        static
        void
        SetXpsDocumentStartingPart(
            Package         package,
            PackagePart     startingPart
            )
        {
            Debug.Assert(package != null, "package cannot be null");

            //
            // null is a valid value for startingPart; it will effectively remove starting part
            // relationship to the existing starting Part; However, the existing startingPart
            // won't be removed from the package
            //

            if (package.FileOpenAccess == FileAccess.Read)
            {
                throw new IOException(SR.Get(SRID.ReachPackaging_CannotModifyReadOnlyContainer));
            }

            //
            // Throw If the part provided is null
            //
            if (startingPart == null)
            {
                throw new ArgumentNullException("startingPart");
            }

            //
            // Throw If the part provided is from a different container
            //
            if (startingPart.Package != package)
            {
                throw new ArgumentException(SR.Get(SRID.ReachPackaging_PartFromDifferentContainer));
            }

                package.CreateRelationship(startingPart.Uri, TargetMode.Internal, XpsS0Markup.ReachPackageStartingPartRelationshipType);
        }

        #endregion Internal static methods

        #region Public static methods

        /// <summary>
        /// This method generates a relative URI path based on the base URI
        /// and the absolute URI.
        /// </summary>
        /// <param name="baseUri">
        /// The base uri for the part.
        /// </param>
        /// <param name="fileUri">
        /// The absolute path URI to be converted to relative.
        /// </param>
        /// <returns>
        /// Returns a relative URI for the supplied file URI using the
        /// supplied base URI.
        /// </returns>
        public
        static
        string
        MakeRelativePath(
            Uri         baseUri,
            Uri         fileUri
            )
        {
            Uri dummyAbsoluteUri = new Uri("http://dummy");

            if (!baseUri.IsAbsoluteUri)
            {// If not absolute, fake it
                baseUri = new Uri(dummyAbsoluteUri, baseUri);
            }

            if (!fileUri.IsAbsoluteUri)
            {// If not absolute, fake it
                fileUri = new Uri(dummyAbsoluteUri, fileUri);
            }


            Uri relativeUri = baseUri.MakeRelativeUri(fileUri);
            Uri unescapedUri = new Uri(relativeUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), UriKind.RelativeOrAbsolute);
            
            return unescapedUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
        }
        public
        static
        bool
        SupportedImageType( ContentType imageContentType )
        {
            bool result = false;
            if (imageContentType.AreTypeAndSubTypeEqual(XpsS0Markup.JpgContentType) ||
                imageContentType.AreTypeAndSubTypeEqual(XpsS0Markup.PngContentType) ||
                imageContentType.AreTypeAndSubTypeEqual(XpsS0Markup.TifContentType) ||
                imageContentType.AreTypeAndSubTypeEqual(XpsS0Markup.WdpContentType)
              )
            {
                result = true;
            }
            return result;
        }

        public
        static
        ContentType
        ImageTypeToString( XpsImageType imageType )
        {
            ContentType imageContentType = new ContentType("");
            switch( imageType )
            {
                case XpsImageType.JpegImageType:
                    imageContentType = XpsS0Markup.JpgContentType;
                    break;
                    
                case XpsImageType.PngImageType:
                    imageContentType = XpsS0Markup.PngContentType;
                    break;
                    
                case XpsImageType.TiffImageType:
                    imageContentType = XpsS0Markup.TifContentType;
                    break;

                case XpsImageType.WdpImageType:
                    imageContentType = XpsS0Markup.WdpContentType;
                    break;
            }
            return imageContentType;
        }
        #endregion Public static methods
    }
}
