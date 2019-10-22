// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



    Abstract:
        This file contains the definition  and implementation
        for the XpsDocument class.  This class acts as the
        "root" of a Xps package and provides access to begin
        reading and/or writing Xps packages.


--*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Documents;
using System.Windows.Media;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Xps.Serialization;
using System.Windows.Markup;
using System.Threading;
using System.Xml;
using System.Security;
using MS.Internal;
using MS.Internal.Security;
using MS.Internal.IO.Packaging;

using MS.Internal.IO.Packaging.Extensions;
using Package = System.IO.Packaging.Package;
using PackUriHelper = System.IO.Packaging.PackUriHelper;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// This class implements the functionality to read and write Xps
    /// packages.
    /// </summary>
    public class XpsDocument : XpsPartBase, INode, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Constructs a XpsDocument instance to read from and/or write to
        /// the supplied Metro package with the default compression options
        /// and no support for rights management, resource policies, or
        /// interleaving policies.
        /// </summary>
        /// <param name="package">
        /// The target Metro package for this Xps package.
        /// </param>
        public
        XpsDocument(
            Package     package
            )
            : this(package, CompressionOption.Maximum)
        {
        }


        /// <summary>
        /// Constructs a XpsDocument instance to read from and/or write to
        /// the supplied Metro package with the supplied compression options
        /// and rights management options. 
        /// </summary>
        /// <param name="package">
        /// The target Metro package for this Xps package.
        /// </param>
        /// <param name="compressionOption">
        /// The compression option for this package.
        /// </param>
        public
        XpsDocument(
            Package                     package,
            CompressionOption           compressionOption
             )
            : this(package, compressionOption,   null)
        {
        }
        
        /// <summary>
        /// Constructs a XpsDocument instance to read from and/or write to
        /// the supplied Metro package with the supplied compression options
        /// and rights management options.  
        /// </summary>
        /// <param name="package">
        /// The target Metro package for this Xps package.
        /// </param>
        /// <param name="compressionOption">
        /// The compression option for this package.
        /// </param>
        /// <param name="path">
        /// path to xps package
        /// </param>
         public
        XpsDocument(
            Package                     package,
            CompressionOption           compressionOption,
            String                      path
            )
            : base(new XpsManager(  package,
                                    compressionOption
                                 )
                  )
        {
            if( path != null )
            {
              this.Uri = new Uri(path, UriKind.RelativeOrAbsolute);
            }
            CurrentXpsManager.XpsDocument = this;
            Initialize();
        }

        /// <summary>
        /// Constructs a XpsDocument instance to read from and/or write to
        /// the supplied Metro package with the default compression options
        /// and no support for rights management, resource policies, or
        /// interleaving policies.
        /// </summary>
        /// <param name="path">
        /// path to package
        /// </param>
        /// <param name="packageAccess">
        /// mode to the package can be accessed (i.e. Read, Write, Read/Write )
        /// </param>
         public
        XpsDocument(
            string      path,
            FileAccess  packageAccess
            ): this( path, packageAccess, CompressionOption.Maximum)
         {
         }
        
        /// <summary>
        /// Constructs a XpsDocument instance to read from and/or write to
        /// the supplied Metro package with the supplied compression options
        /// and rights management options.  
        /// </summary>
        /// <param name="path">
        /// path to package
        /// </param>
        /// <param name="packageAccess">
        /// mode to the package can be accessed (i.e. Read, Write, Read/Write )
        /// </param>
        /// <param name="compressionOption">
        /// The compression option for this package.
        /// </param>
        public
        XpsDocument(
            string                  path,
            FileAccess              packageAccess,
            CompressionOption       compressionOption
            )
            : base(new XpsManager(   path,
                                     packageAccess,
                                     compressionOption
                                     )
                  )
        {
            if( null == path )
            {
                throw new ArgumentNullException("path");
            }
            this.Uri = new Uri(path, UriKind.RelativeOrAbsolute);
            //
            //The URI has to be absolute
            //If the path passed in is relative append it to the
            //current working directory
            //
            if( !Uri.IsAbsoluteUri)
            {
                Uri = new Uri( new Uri(Directory.GetCurrentDirectory()+"/"), this.Uri );
            }

            CurrentXpsManager.XpsDocument = this;
            Initialize();
        }
        #endregion Constructors

        #region Public properties

        /// <summary>
        /// This property determines whether this instance is
        /// a writer (i.e. It can write to the package).
        /// </summary>
        /// <value>A boolean representing write capabilities.</value>
        public bool IsWriter
        {
            get
            {
                CheckDisposed();
                return CurrentXpsManager.IsWriter;
            }
        }

        /// <summary>
        /// This property determines whether this instance is
        /// a reader (i.e. It can read from the package).
        /// </summary>
        /// <value>A boolean representing read capability.</value>
        public bool IsReader
        {
            get
            {
                CheckDisposed();
                return CurrentXpsManager.IsReader;
            }
        }

        /// <summary>
        /// This method returns the root (starting part) of the Metro package as a DocumentSequenceReader.
        /// </summary>
        /// <returns>A DocumentSequenceReader representing the root.</returns>
        /// <exception cref="SRID.ReachPackaging_NotOpenForReading">Package not open for reading.</exception>
        /// <exception cref="SRID.ReachPackaging_InvalidStartingPart">Package starting part is not a valid root.</exception>
        public
        IXpsFixedDocumentSequenceReader
        FixedDocumentSequenceReader
        {
            get
            {
                CheckDisposed();
                XpsFixedDocumentSequenceReaderWriter value = null;
                PackagePart startingPart = CurrentXpsManager.StartingPart;
                if (IsReader && startingPart!= null)
                {
                    value =new XpsFixedDocumentSequenceReaderWriter(CurrentXpsManager, null, startingPart);
                }

                return value;
            }
        }

        /// <summary>
        /// A list of reach sigantures associated with the pacakge
        /// Given this list the user should be able to determine what
        /// What parts have been signed and whether the signatures are valid
        /// </summary>
        /// <value>List of associated XpsSignatures</value>
        public ReadOnlyCollection<XpsDigitalSignature> Signatures
        {
            get
            {
                CheckDisposed();
                EnsureSignatures();
                // Return a read-only collection referring to them
                // This list maintains a reference to _signatures so its enumerator will adapt
                // as _signatures is updated.  Therefore, we need not regenerate it when _signatures
                // is modified.
                if (_reachSignatureList == null)
                {
                    _reachSignatureList = new ReadOnlyCollection<XpsDigitalSignature>(_reachSignatures);
                }

                return _reachSignatureList;
           }
        }

        /// <summary>
        /// This propertert returns a class accesing the
        /// Core Document Properties Part
        /// Just accessing the property does not create the part.
        /// If the part does not exist it will be created when one of
        /// CoreDocumentProperties properties has be modified and commited.
        /// </summary>
        public
        PackageProperties
        CoreDocumentProperties
        {
            get
            {
                CheckDisposed();
                return CurrentXpsManager.MetroPackage.PackageProperties;
            }
        }

        /// <summary>
        /// thumbnail image associated with this package
        /// </summary>
        public
        XpsThumbnail
        Thumbnail
        {
            get
            {
                CheckDisposed();
                EnsureThumbnail();
                return _thumbnail;
            }

            set
            {
                CheckDisposed();
                _thumbnail = value;
            }
        }

        /// <summary>
        /// Does the document meet the policy for signing
        /// </summary>
        public
        bool
        IsSignable
        {
            get
            {         
                CheckDisposed();
                bool isSignable = true;

                //
                // List of parts containing XML that need to be checked for 
                // Version Extensiblitly
                //
                List<PackagePart> xmlPartList = new List<PackagePart>();
                
                (FixedDocumentSequenceReader as XpsFixedDocumentSequenceReaderWriter).CollectXmlPartsAndDepenedents(xmlPartList);

                foreach( PackagePart part in xmlPartList )
                {
                    using (Stream stream = part.GetStream(FileMode.Open, FileAccess.Read))
                    {
                        //
                        // An empty stream contains not version extensibility thus is valid
                        // We do create empty parts for print tickets
                        //
                        if (stream.Length == 0)
                            continue;
                        try
                        {
                            if (StreamContainsVersionExtensiblity(stream))
                            {
                                isSignable = false;
                                break;
                            }
                        }
                        catch (XmlException)
                        {
                            isSignable = false;
                            break;
                        }
                    }
                }
                return isSignable;
           }
        }

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// This method signs the supplied parts within the Metro package
        /// using the supplied cerificate.  If no certificate is supplied,
        /// UI is displayed to select a certificate.
        /// </summary>
        /// <param name="certificate">
        /// The certificate to use in signing
        /// </param>
        /// <param name="embedCertificate">
        /// Flag indicating wheter the certificate should be inbeded in the package
        /// </param>
        /// <param name="restrictions">
        /// Flags indicating what dependent parts should be excluded from the signing
        /// </param>
        /// <exception cref="ArgumentNullException">certificate is null.</exception>
        public
        XpsDigitalSignature
        SignDigitally(
            X509Certificate                         certificate,
            bool                                    embedCertificate,
            XpsDigSigPartAlteringRestrictions       restrictions
            )
        {
            CheckDisposed();
                                        
            return SignDigitally(
                 certificate,
                 embedCertificate,
                 restrictions,
                 null,
                 true
                 );
         }

        /// <summary>
        /// This method signs the supplied parts within the Metro package
        /// using the supplied cerificate.  If no certificate is supplied,
        /// UI is displayed to select a certificate.
        /// </summary>
        /// <param name="certificate">
        /// The certificate to use in signing
        /// </param>
        /// <param name="embedCertificate">
        /// Flag indicating wheter the certificate should be inbeded in the package
        /// </param>
        /// <param name="restrictions">
        /// Flags indicating what dependent parts should be excluded from the signing
        /// </param>
        /// <param name="id">
        /// Id to be assigned to the signature
        /// </param>
        /// <exception cref="ArgumentNullException">certificate is null.</exception>
        public
        XpsDigitalSignature
        SignDigitally(
            X509Certificate                         certificate,
            bool                                    embedCertificate,
            XpsDigSigPartAlteringRestrictions       restrictions,
            Guid                                    id
            )
        {
            CheckDisposed();
        
            return SignDigitally(
                 certificate,
                 embedCertificate,
                 restrictions,
                 XmlConvert.EncodeName(id.ToString()),
                 true
                 );
        }

        /// <summary>
        /// This method signs the supplied parts within the Metro package
        /// using the supplied cerificate.  If no certificate is supplied,
        /// UI is displayed to select a certificate.
        /// </summary>
        /// <param name="certificate">
        /// The certificate to use in signing
        /// </param>
        /// <param name="embedCertificate">
        /// Flag indicating wheter the certificate should be inbeded in the package
        /// </param>
        /// <param name="restrictions">
        /// Flags indicating what dependent parts should be excluded from the signing
        /// </param>
        /// <param name="id">
        /// Id to be assigned to the signature
        /// </param>
        /// <param name="testIsSignable">
        /// Flag indicating if IsSignable should be called before signing
        /// </param>
        /// <exception cref="ArgumentNullException">certificate is null.</exception>
        public
        XpsDigitalSignature
        SignDigitally(
            X509Certificate                         certificate,
            bool                                    embedCertificate,
            XpsDigSigPartAlteringRestrictions       restrictions,
            Guid                                    id,
            bool                                    testIsSignable
            )
        {
            CheckDisposed();
        
            return SignDigitally(
                 certificate,
                 embedCertificate,
                 restrictions,
                 XmlConvert.EncodeName(id.ToString()),
                 testIsSignable
                 );
        }

        /// <summary>
        /// Removes a Digital Signature
        /// </summary>
        /// <param name="signature">
        /// The signature to be removed
        /// </param>
        /// <exception cref="ArgumentNullException">signature is null.</exception>
        public
        void
        RemoveSignature(
            XpsDigitalSignature signature
            )
        {
            CheckDisposed();
        
            if (null == signature)
            {
                throw new ArgumentNullException("signature");
            }
            if (null == signature.PackageSignature)
            {
                throw new NullReferenceException("signature.PackageSignature");
            }
            if (null == signature.PackageSignature.SignaturePart)
            {
                throw new NullReferenceException("signature.PackageSignature.SignaturePart");
            }
            if( CurrentXpsManager == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_DocumentWasClosed) );
            }
            PackageDigitalSignatureManager packageSignatures = new PackageDigitalSignatureManager(CurrentXpsManager.MetroPackage);
            packageSignatures.RemoveSignature(signature.PackageSignature.SignaturePart.Uri );
            _reachSignatures = null;
            _reachSignatureList = null;
            EnsureSignatures();
}

        /// <summary>
        /// This method adds a thumbnail to the current Xps package.
        ///
        /// There can only be one thumbnail attached to the pakcage.
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
            CheckDisposed();
        
            if( CurrentXpsManager == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_DocumentWasClosed) );
            }
            _thumbnail = CurrentXpsManager.AddThumbnail(imageType, this, Thumbnail);
            Package metroPackage = CurrentXpsManager.MetroPackage;
            metroPackage.CreateRelationship( _thumbnail.Uri,
                                             TargetMode.Internal,
                                             XpsS0Markup.ThumbnailRelationshipName
                                           );
            return _thumbnail;
        }

        /// <summary>
        /// This method adds a document sequence to the current Xps package as
        /// the root (starting part) of the package and returns an instance of
        /// a writer interface for writing to the document sequence.
        ///
        /// There can only be one starting part.  Calling this method when there
        /// is already a starting part causes InvalidOperationException to be
        /// thrown.
        /// </summary>
        /// <returns>Returns a IXpsFixedDocumentSequenceWriter instance.</returns>
        /// <exception cref="SRID.ReachPackaging_AlreadyHasRootSequenceOrDocument">Package already has a root DocumentSequence.</exception>
        public
        IXpsFixedDocumentSequenceWriter
        AddFixedDocumentSequence(
            )
        {
            CheckDisposed();
        
            if( CurrentXpsManager == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_DocumentWasClosed) );
            }
            if (_isInDocumentStage)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_AlreadyHasRootSequenceOrDocument));
            }
            if ( !CurrentXpsManager.Streaming && null != CurrentXpsManager.StartingPart)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_AlreadyHasRootSequenceOrDocument));
            }

            //
            // Create the unique metro part in the package
            //
            PackagePart metroPart = CurrentXpsManager.GenerateUniquePart(XpsS0Markup.DocumentSequenceContentType);

            //
            // Create reader/writer and save a reference to it.
            //
            XpsFixedDocumentSequenceReaderWriter ds = new XpsFixedDocumentSequenceReaderWriter(CurrentXpsManager, this, metroPart);
  
            CurrentXpsManager.StartingPart = ((INode)ds).GetPart();

            return ds;
        }

        /// <summary>
        /// This method returns the root (starting part) of the Metro package.
        /// The return value can either be an instance of a document sequence or
        /// a fixed document.
        /// </summary>
        /// <returns>A XpsPartBase representing the root.</returns>
        /// <remarks>Method will be internal until reading/de-serialization is implemented.</remarks>
        /// <exception cref="SRID.ReachPackaging_PackageUriNull">XpsPakage Uri is null.  Use XpsDocument constructor that takes Uri parameter.</exception>
        /// <exception cref="SRID.ReachPackaging_InvalidStartingPart">Package starting part is not a valid root.</exception>
        /// <exception cref="SRID.ReachPackaging_NotAFixedDocumentSequence">Part Uri does not corresepond to  a Fixed Document Sequence.</exception>
        public
        FixedDocumentSequence
        GetFixedDocumentSequence(
            )
        {
            CheckDisposed();
        
            if( CurrentXpsManager == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_DocumentWasClosed) );
            }
            if (!IsReader)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotOpenForReading));
            }

            if (null == Uri)
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_PackageUriNull));
            }

            if (CurrentXpsManager.StartingPart == null)
            {
                return null;
            }

            ContentType startPartType = CurrentXpsManager.StartingPart.ValidatedContentType();
            if (!startPartType.AreTypeAndSubTypeEqual(XpsS0Markup.DocumentSequenceContentType))
            {
                 throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_InvalidStartingPart));
            }
            ParserContext parserContext = new ParserContext();

            parserContext.BaseUri = PackUriHelper.Create(Uri, CurrentXpsManager.StartingPart.Uri);

            object fixedObject = XamlReader.Load(CurrentXpsManager.StartingPart.GetStream(), parserContext);
            if (!(fixedObject is FixedDocumentSequence) )
            {
                 throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotAFixedDocumentSequence));
            }
            return fixedObject as FixedDocumentSequence;
        }

        /// <summary>
        /// This method commits the changes of the package and closes it.
        /// Further changes after a commit will cause an exception to be
        /// thrown.
        /// </summary>
        internal
        override
        void
        CommitInternal(
            )
        {
            if( CurrentXpsManager == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_DocumentWasClosed) );
            }
            base.CommitInternal();
        }

        /// <summary>
        /// Writes the close elements for both the starting part and the package
        /// </summary>
        public
        void
        Close(
            )
        {
            Dispose(true);
        }

        #endregion Public methods

        #region protected methods

        ///<Summary>
        /// Handles releasing XpsDocuments resources
        ///</Summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            
            if (disposing)
            {
                _thumbnail = null;
                _reachSignatures = null;
                _reachSignatureList = null;
                _opcPackage = null; 

                CurrentXpsManager.Close();
                CommitInternal();
            }

            GC.SuppressFinalize(this);
            _disposed = true;
        }


       #endregion
        

        #region Private methods

        /// <summary>
        /// This method is a helper method used when constructing the
        /// current instance.
        /// </summary>
        private
        void
        Initialize(
             )
        {
            _isInDocumentStage = false;
            _opcPackage = null;
        }

        private
        void
        EnsureThumbnail()
        {
            if( _thumbnail == null )
            {
                 _thumbnail = CurrentXpsManager.EnsureThumbnail(this, null );
            }
        }

        private
        void
        EnsureSignatures()
        {
            //
            // if _reachSignaturs is not null we have already initialized this
            //
            if( null != _reachSignatures )
            {
                return;
            }
            _reachSignatures   = new Collection<XpsDigitalSignature>();
            PackageDigitalSignatureManager packageSignatures = new PackageDigitalSignatureManager(CurrentXpsManager.MetroPackage);
            foreach( PackageDigitalSignature packSignature in packageSignatures.Signatures )
            {
                XpsDigitalSignature reachSignature = new XpsDigitalSignature( packSignature, this );
                //
                // Only add signatures that meet the policy for
                // signed Document Sequences
                //
                if( reachSignature.SignedDocumentSequence != null )
                {
                     _reachSignatures.Add( reachSignature );
                }
            }
        }

        internal
        void
        CollectSelfAndDependents(
            Dictionary<Uri,Uri>                     dependentList,
            List<PackageRelationshipSelector>       selectorList,
            XpsDigSigPartAlteringRestrictions       restrictions
            )
        {
            (FixedDocumentSequenceReader as XpsFixedDocumentSequenceReaderWriter).CollectSelfAndDependents(
                                                                                    dependentList, 
                                                                                    selectorList,
                                                                                    restrictions);
            //
            // Add thumbnail
            //
            EnsureThumbnail();
            if( _thumbnail != null )
            {
                dependentList[_thumbnail.Uri] = _thumbnail.Uri;
            }

            //
            // Sign Starting part relationship
            //
            selectorList.Add( new PackageRelationshipSelector(
                                    MS.Internal.IO.Packaging.PackUriHelper.PackageRootUri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.ReachPackageStartingPartRelationshipType 
                                    ) 
                             );

            //
            // Add thumnail relationship
            //
            selectorList.Add(new PackageRelationshipSelector(
                                    MS.Internal.IO.Packaging.PackUriHelper.PackageRootUri,
                                    PackageRelationshipSelectorType.Type,
                                    XpsS0Markup.ThumbnailRelationshipName
                                    )
                                 );
            //
            // Sign CoreMetadata type if the restriction flag is set
            //
            if( (restrictions & XpsDigSigPartAlteringRestrictions.CoreMetadata)!= 0 )
            {
                //We need to flush the package because the core properties are not written out
                //until the package is flushed
                CurrentXpsManager.MetroPackage.Flush();
                selectorList.Add( new PackageRelationshipSelector(
                                        MS.Internal.IO.Packaging.PackUriHelper.PackageRootUri,
                                        PackageRelationshipSelectorType.Type,
                                        XpsS0Markup.CorePropertiesRelationshipType 
                                        ) 
                                 );
            }
            //
            // Add the Signiture Origin based on the restrictions
            //
            CurrentXpsManager.CollectSignitureOriginForSigning(selectorList, restrictions );
            //
            // Add the properties based on the restrictions
            //
            CurrentXpsManager.CollectPropertiesForSigning(dependentList, restrictions );
        }

        private
        XpsDigitalSignature
        AddSignature(PackageDigitalSignature packSignature)
        {
            XpsDigitalSignature reachSignature =
                new XpsDigitalSignature( packSignature, this );

            _reachSignatures.Add( reachSignature );
            return reachSignature;
        }

        private
        bool
        StreamContainsVersionExtensiblity( Stream stream )
        {
            XmlReader xmlReader = new XmlTextReader( stream );
            bool containsVersionExtensiblity = false;
            while( xmlReader.Read() );
            if (xmlReader.NameTable.Get(XpsS0Markup.VersionExtensiblityNamespace) != null)
            {
                containsVersionExtensiblity = true;
            }  
            return containsVersionExtensiblity;
        }
        
        private
        XpsDigitalSignature
        SignDigitally(
            X509Certificate                         certificate,
            bool                                    embedCertificate,
            XpsDigSigPartAlteringRestrictions       restrictions,
            String                                  signatureId,
            bool                                    testIsSignable
            )
        {
            if (null == certificate)
            {
                throw new ArgumentNullException("certificate");
            }
            
            if( CurrentXpsManager == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_DocumentWasClosed) );
            }
            if( testIsSignable && !IsSignable )
            {
                throw new InvalidOperationException(SR.Get(SRID.ReachPackaging_SigningDoesNotMeetPolicy) );              
            }
            EnsureSignatures();
            //
            // List of RelationshipSelectors that need to be signed
            //
            List<PackageRelationshipSelector> selectorList =  
                new List<PackageRelationshipSelector>();

            //
            // This is being used as a Set class so the second Uri Value is irrelevent
            //
            Dictionary<Uri,Uri> dependentList = new Dictionary<Uri,Uri> ();
            CollectSelfAndDependents( dependentList, selectorList,  restrictions );

            PackageDigitalSignature packSignature =
                CurrentXpsManager.Sign(dependentList.Keys,
                                         certificate,
                                         embedCertificate,
                                         selectorList,
                                         signatureId
                                         );
           return AddSignature(packSignature);
        }
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        internal
        static
        XpsDocument
        CreateXpsDocument(
            Stream     dataStream
            )
        {
            // In .NET Core 3.0 System.IO.Compression's ZipArchive does not allow creation of ZipArchiveEntries when
            // a prior ZipArchiveEntry is still open.  XPS Serialization requires this as part of its implementation.
            // To get around this, XPS creation should occur in with FileAccess.ReadWrite if the underlying stream
            // supports it.  This allows multiple ZipArchiveEntries to be open concurrently.
            Package package = Package.Open(dataStream,
                                           FileMode.CreateNew,
                                           (dataStream.CanRead) ? FileAccess.ReadWrite : FileAccess.Write);
            XpsDocument document = new XpsDocument(package);

            document.OpcPackage = package;

            return document;
        }

        [MS.Internal.ReachFramework.FriendAccessAllowed]
        internal
        void
        DisposeXpsDocument(
            )
        {
            if(_opcPackage != null)
            {
                _opcPackage.Close();
            }
        }

        internal
        Package
        OpcPackage
        {
            set
            {
                _opcPackage = value;
            }
        }
        /// <summary>
        /// This is a pass thru to allow system printing access to method of the
        /// same name in WindowsBase
        /// </summary>
        internal static void SaveWithUI(IntPtr parent, Uri source, Uri target)
        {
            AttachmentService.SaveWithUI(parent, source, target );
        }

        #endregion Private methods

        #region Private data

        //
        // This variable signals that we have created the root
        // document sequence or fixed document.  Once this flag
        // is set, no more changes are allowed to either the
        // PrintTicket or XpsProperties.
        //
        private bool _isInDocumentStage;


        private Collection<XpsDigitalSignature> _reachSignatures;

        private ReadOnlyCollection<XpsDigitalSignature> _reachSignatureList;

        private XpsThumbnail   _thumbnail;

        private Package        _opcPackage; 

        bool _disposed = false;

        #endregion Private data

        #region IDisposable implementation

        void
        IDisposable.Dispose(
            )
        {
            Close();
        }

        #endregion IDisposable implementation

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("XpsDocument");
            }
        }

        #region INode implementation

        void
        INode.Flush(
            )
        {
            if (CurrentXpsManager.Streaming)
            {
                //CurrentXpsManager.MetroPackage.FlushPackageRelationships();
            }
        }

        void
        INode.CommitInternal(
            )
        {
            Close();
        }

        PackagePart
        INode.GetPart(
            )
        {
            return null;
        }

        #endregion INode implementation

        /// <summary>
        /// Creates and returns the appropriate <c>PackageSerializationManager</c>.
        /// </summary>
        /// <returns><c>PackageSerializationManager</c></returns>
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        internal
        PackageSerializationManager
        CreateSerializationManager(
            bool    bBatchMode
            )
        {
            PackageSerializationManager serializationManager = null;

            XpsPackagingPolicy packagingPolicy = new XpsPackagingPolicy(this);
            if (packagingPolicy != null)
            {
                serializationManager = new XpsSerializationManager(packagingPolicy,  bBatchMode);
            }

            return serializationManager;
        }

        /// <summary>
        /// Creates and returns the appropriate <c>MetroAsyncSerializationManager</c>.
        /// </summary>
        /// <returns><c>AsyncPackageSerializationManager</c></returns>
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        internal
        PackageSerializationManager
        CreateAsyncSerializationManager(
            bool    bBatchMode
            )
        {
            PackageSerializationManager serializationManager = null;

            XpsPackagingPolicy packagingPolicy = new XpsPackagingPolicy(this);

            serializationManager = new XpsSerializationManagerAsync(packagingPolicy, bBatchMode);

            return serializationManager;
        }

        /// <summary>
        /// Dispose a serializaiton manager
        /// </summary>
        [MS.Internal.ReachFramework.FriendAccessAllowed]
        internal
        void
        DisposeSerializationManager(
            )
        {
        }

        
        /// <summary>
        /// Creates an <c>XpsDocumentWriter</c> and initiates it against the <c>XpsDocument</c>
        /// </summary>
        /// <param name="xpsDocument">to initialize against</param>
        /// <returns><c>XPSDocumentWriter</c></returns>
        /// <remarks>XpsDocumentWriter checks XPS implementation limits as defined in Section 11.2 of XPS Specification and Reference Guide. 
        /// When violations are detected, XML comments will be inserted in the markup generated. Here are some examples:
        ///    "XPSLimit:FloatRange"        for value outside of suggested range.
        ///    "XPSLimit:PointCount"        for too many points in a geometry.
        ///    "XPSLimit:GradientStopCount" for too many stops in gradient stop collection
        ///    "XPSLimit:ResourceCount"     for too many resources in a resource dictionary
        ///    "XPSLimit:ElementCount"      for too many elements in a FixedPage
        /// </remarks>
        public
        static
        System.
        Windows.
        Xps.
        XpsDocumentWriter
        CreateXpsDocumentWriter(
            XpsDocument xpsDocument
            )
        {
            XpsDocumentWriter   writer  = new XpsDocumentWriter(xpsDocument);

            return writer;
        }

        #region Public Events


        #endregion Public Events
    }
}
