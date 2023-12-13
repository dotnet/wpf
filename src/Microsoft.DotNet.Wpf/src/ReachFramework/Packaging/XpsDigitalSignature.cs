// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                                                                                           
    Abstract:
        This is a wrapper class for the Package Digital Signature
        It provides binding information between the Xps Package and the Digital Signature
        such as what is the root objects signed and what are the signing restrictions
                                   
                                                                             
--*/
using MS.Internal;
using System;
using System.Windows.Documents;
using System.IO.Packaging;
using System.Security;                                  // for SecurityCritical tag
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

using PackUriHelper = System.IO.Packaging.PackUriHelper;
namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// Wrapper class for  the Pacakge Digital Signature
    /// Provides binding information between the Xps Package and the Digital Signature
    /// </summary>
    public class XpsDigitalSignature
    {
        /// <summary>
        /// Constructs a XpsDigitalSignature instance to determine
        /// what was singed by the signature and if it is valid.
        /// </summary>
        /// <param name="packageSignature">
        /// The associated PacakgeDigitalSignature
        /// </param>
        /// <param name="package">
        /// The associated package
        /// </param>
        public 
        XpsDigitalSignature( 
            PackageDigitalSignature packageSignature, 
            XpsDocument            package 
            )
        {
            _packageSignature = packageSignature;
            _package = package;
        }

        #region Public properties
            
        /// <summary>
        /// The  Document Sequence Reader for 
        /// the signed Document seqeuence part.
        /// If this is NULL it idicates that this signature
        /// only signed pages
        /// </summary>
        /// <value>Value is the IXpsFixedDocumentSequenceReader for the signed Docuement Sequence Part</value>
        public 
        IXpsFixedDocumentSequenceReader
        SignedDocumentSequence 
        {
            get
            {
                IXpsFixedDocumentSequenceReader seqReader = _package.FixedDocumentSequenceReader;
                IXpsFixedDocumentSequenceReader returnReader = null;
                if( seqReader != null )
                {
                    Dictionary<Uri, Uri> dependentList = new Dictionary<Uri,Uri>();

                    List<PackageRelationshipSelector> selectorList =  
                        new List<PackageRelationshipSelector>();


                    _package.CollectSelfAndDependents( 
                                        dependentList,
                                        selectorList,
                                        XpsDigSigPartAlteringRestrictions.None
                                     );                
                    if( CollectionContainsCollection(_packageSignature.SignedParts,
                                                     dependentList.Keys) &&
                        CollectionContainsCollection(dependentList.Keys,
                                                     _packageSignature.SignedParts) &&
                        SelectorListContainsSelectorList(_packageSignature.SignedRelationshipSelectors,
                                                         selectorList )
                      )
                    {
                        returnReader = seqReader;
                    }
                }
                return returnReader;
            }
        }		



        /// <summary>
        /// returns true if changing the Signature Origin breaks the signature
        /// </summary>                 
        public
        bool
        SignatureOriginRestricted
        {
            get
            {   bool restrictedFlag = false;
                foreach( PackageRelationshipSelector selector in _packageSignature.SignedRelationshipSelectors )
                {
                    if( selector.SourceUri == _package.CurrentXpsManager.GetSignatureOriginUri() &&
                        selector.SelectionCriteria == XpsS0Markup.DitialSignatureRelationshipType )
                    {
                        restrictedFlag = true;
                        break;
                    }
                }
                return restrictedFlag;
            }
        }
            
        /// <summary>
        /// returns true if changing the Document Properties breaks the signature
        /// </summary>                 
        public
        bool
        DocumentPropertiesRestricted
        {
            get
            {   bool restrictedFlag = false;
                foreach( PackageRelationshipSelector selector in _packageSignature.SignedRelationshipSelectors )
                {
                    if( selector.SourceUri == MS.Internal.IO.Packaging.PackUriHelper.PackageRootUri &&
                        selector.SelectionCriteria == XpsS0Markup.CorePropertiesRelationshipType )
                    {
                        restrictedFlag = true;
                        break;
                    }
                }
                return restrictedFlag;
            }
        }


        /// <summary>
        /// Id of the signature
        /// </summary>
        public 
        Guid? 
        Id 
        {
            get
            {
                Guid? id = null;
                Signature  sig = (Signature)_packageSignature.Signature;
                if( sig != null )
                {
                    try
                    {
                        string convertedId = XmlConvert.DecodeName(sig.Id);
                        id = new Guid(convertedId);
                    }
                    catch( ArgumentNullException  )
                    {
                        id = null;
                    }
                    catch( FormatException  )
                    {
                        id = null;
                    }
                }
                return id;
            }
        }

        /// <summary>
        /// Certificate of signer embedded in container
        /// </summary>
        /// <value>null if certificate was not embedded</value>
        public 
        X509Certificate
        SignerCertificate
        {
            get
            {
                return _packageSignature.Signer;
            }
         }


        /// <summary>
        /// Time signature was created - not a trusted TimeStamp
        /// </summary>
        /// <value></value>
        public 
        DateTime 
        SigningTime
        {
            get
            {
                return _packageSignature.SigningTime;
             }
        }


        /// <summary>
        /// encrypted hash value
        /// </summary>
        /// <value></value>
        public 
        byte[] 
        SignatureValue
        {
            get
            {
                return _packageSignature.SignatureValue;
            }
        }

        /// <summary>
        /// Content Type of signature
        /// </summary>
        public 
        String 
        SignatureType
        {
            get
            {
                 return _packageSignature.SignatureType;
            }
        }

        /// <summary>
        /// True if the package contains the signatures certificate.
        /// </summary>
       public
        bool
        IsCertificateAvailable
        {
            get
            {
               
               return ( _packageSignature.CertificateEmbeddingOption == 
                    CertificateEmbeddingOption.InCertificatePart);
            }
        }
        #endregion Public properties

        #region Public methods
       
        /// <summary>
        /// Verify
        /// </summary>
        /// <remarks>cannot use this overload with signatures created without embedding their certs</remarks>
        /// <returns></returns>
        public 
        VerifyResult 
        Verify()
        {
            return _packageSignature.Verify();
        }

        /// <summary>
        /// Verify
        /// </summary>
        /// <remarks>cannot use this overload with signatures created without embedding their certs</remarks>
        /// <returns></returns>
        /// <param name="certificate">
        /// Certificate to be used to verify
        /// </param>
        public 
        VerifyResult 
        Verify(X509Certificate certificate)
        {
            return _packageSignature.Verify(certificate);
        }

        /// <summary>
        /// Verify Certificate
        /// Uses the certificate stored the signature
        /// </summary>
        /// <returns>the first error encountered when inspecting the certificate chain or NoError if the certificate is valid</returns>
        public 
        X509ChainStatusFlags 
        VerifyCertificate()
        {
            return VerifyCertificate(_packageSignature.Signer);
        }        
        /// <summary>
        /// Verify Certificate
        /// </summary>
        /// <param name="certificate">certificate to inspect</param>
        /// <returns>the first error encountered when inspecting the certificate chain or NoError if the certificate is valid</returns>
        public 
        static 
        X509ChainStatusFlags 
        VerifyCertificate(X509Certificate certificate)
        {
            return PackageDigitalSignatureManager.VerifyCertificate(certificate);
        }
        #endregion Public methods

        #region Internal property
        internal
        PackageDigitalSignature
        PackageSignature
        {
            get
            {
                return _packageSignature;            }
        }
        #endregion Internal property
        #region Private methods
        bool CollectionContainsCollection(
            ICollection<Uri> containingCollection, 
            ICollection<Uri> containedCollection
            )
        {
            bool contained = true;
            //
            // Convert the containing collection to a hash table
            Dictionary<Uri, Uri> hashTable = new Dictionary<Uri, Uri>();
            foreach( Uri uri in containingCollection )
            {
                hashTable[uri] = uri;
            }

            //
            // Iteratate the contained collection
            // and cofirm existance in the contained collection
            //
            foreach( Uri uri in containedCollection )
            {
                bool isOptional = IsOptional( uri );
                if( !hashTable.ContainsKey( uri )&& !isOptional)
                {
                    contained = false;
                    break;

                }
            }
            return contained;
        }
        
        /// <summary>
        /// This returns true if the part can optionally be signed
        /// XML Paper Specification 10.2.1.1 Signing rules
        /// </summary>
        private bool IsOptional( Uri uri )
        {
           string contentType =  _package.CurrentXpsManager.MetroPackage.GetPart(uri).ContentType;
           return( OptionalSignedParts.ContainsKey( contentType ) );
        }
         
        /// <summary>
        /// This determines if the contained collection is a subset of the containting collection
        /// For each Source Uri in the Containging collection there must be the coorisponding
        /// selection criteria (relationship types)
        /// </summary>
        /// <param name="containingCollection">The super set collection</param>
        /// <param name="containedCollection">The sub set collection</param>
        /// <returns>returns true if is the contained collection is a subset of the containing collection</returns>
        bool SelectorListContainsSelectorList(
            ReadOnlyCollection<PackageRelationshipSelector> containingCollection,
            List<PackageRelationshipSelector> containedCollection
            )
        {
            bool contained = true;
            //
            // Convert the containing collection to a hash table
            Dictionary<Uri, Dictionary<string, int>> uriHashTable = new Dictionary<Uri, Dictionary<string, int>>();
            foreach (PackageRelationshipSelector selector in containingCollection)
            {
                //
                // If the Source Uri is already in the  hash table
                // pull out the existing  relationship dictionary
                Dictionary<string, int> relHash = null;
                if( uriHashTable.ContainsKey( selector.SourceUri ) )
                {
                    relHash = uriHashTable[selector.SourceUri];
                }
                //
                // Else create a new one and add it into Uri Hash
                //
                else
                {
                    relHash = new Dictionary<string, int>();
                    uriHashTable[selector.SourceUri] = relHash;
                }
                //
                // The value is unused we are using this as a hash table
                //
                relHash[selector.SelectionCriteria] = 0;
            }

            //
            // Iteratate the contained collection
            // and cofirm existance in the contained collection
            //
            foreach (PackageRelationshipSelector selector in containedCollection)
            {
                //
                // If the source Uri is not in the hash this fails
                if (!uriHashTable.ContainsKey(selector.SourceUri))
                {
                    contained = false;
                    break;
                }
                else
                {
                    Dictionary<string, int> relHash = uriHashTable[selector.SourceUri];
                    if (!relHash.ContainsKey(selector.SelectionCriteria))
                    {
                        contained = false;
                        break;
                    }
                }

            }
            return contained;
        }
        Dictionary<string, string> OptionalSignedParts
        {
            get
            {
                if( _optionalSignedTypes == null )
                {
                    _optionalSignedTypes = new Dictionary<string, string>();
                    _optionalSignedTypes[XpsS0Markup.CoreDocumentPropertiesType.OriginalString] = "";
                    _optionalSignedTypes[XpsS0Markup.PrintTicketContentType.OriginalString] = "";
                    _optionalSignedTypes[XpsS0Markup.SigOriginContentType.OriginalString] = "";
                    _optionalSignedTypes[XpsS0Markup.SigCertContentType.OriginalString] = "";
                    _optionalSignedTypes[XpsS0Markup.DiscardContentType.OriginalString] = "";

                    // Compatibility Note:
                    // StoryFragment parts are treated as optional because managed viewer released in Vista does not sign them.
                    _optionalSignedTypes[XpsS0Markup.StoryFragmentsContentType.OriginalString] = "";

                    _optionalSignedTypes[XpsS0Markup.RelationshipContentType.OriginalString] = "";
                }
                return _optionalSignedTypes;

            }
        }
        #endregion Private methods

        #region Private data
        private PackageDigitalSignature _packageSignature; 
        private XpsDocument _package;
        static private Dictionary<string, string> _optionalSignedTypes;
        #endregion Private data       
    }
    
    /// <summary>
    /// Flags indicating which parts are to be exluded
    /// from a digital signature
    /// May be or'ed together
    /// </summary>
    [FlagsAttribute]
    public enum XpsDigSigPartAlteringRestrictions
    {
        /// <summary>
        /// all depedent parts will be signed
        /// </summary>
        None                    = 0x00000000,
        /// <summary>
        /// Meta data will be exluded
        /// </summary>
        CoreMetadata            = 0x00000001,
        /// <summary>
        /// Annotations will be exluded
        /// </summary> 
        Annotations             = 0x00000002,
        /// <summary>
        /// The signature will be exluded
        /// </summary>
        SignatureOrigin         = 0x00000004
    };
}

