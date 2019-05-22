// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                                              
    Abstract:
        This file contains the definition of all literals / strings
        used either to represent the S0 markup or to denote certain
        named properties or attributes of the Xps Serialization
                
                                                                       
--*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media;
using System.Windows.Xps.Serialization;

using MS.Internal;

namespace System.Windows.Xps.Packaging
{
    internal class XpsNamedProperties
    {
        private
        XpsNamedProperties(
            )
        {
        }

        static
        public
        String
        PrintTicketProperty
        {
            get
            {
                return _printTicketProperty;
            }
        }

        static
        public
        String
        ClrProperty
        {
            get
            {
                return _clrProperty;
            }
        }


        private
        const
        String  _printTicketProperty = "PrintTicket";

        private
        const
        String  _clrProperty         = "Property";
    };

    internal class XpsS0Markup
    {
        static
        public
        String
        PackageRelationshipUri
        {
            get
            {
                return _packageRelationshipUri;
            }
        }

        static
        public
        String
        ObfuscatedFontExt
        {
            get
            {
                return _obfuscatedFontExt;
            }
        }

        static
        public
        String
        PageWidth
        {
            get
            {
                return _pageWidth;
            }
        }

        static
        public
        String
        PageHeight
        {
            get
            {
                return _pageHeight;
            }
        }

        static
        public
        String
        FixedPage
        {
            get
            {
                return _fixedPage;
            }
        }

        static
        public
        String
        FixedDocument
        {
            get
            {
                return _fixedDocument;
            }
        }

        static
        public
        String
        FixedDocumentSequence
        {
            get
            {
                return _fixedDocumentSequence;
            }
        }

        static
        public
        String
        PageContent
        {
            get
            {
                return _pageContent;
            }
        }

        static
        public
        String
        DocumentReference
        {
            get
            {
                return _documentReference;
            }
        }

        static
        public
        String
        StoryFragments
        {
            get
            {
                return _storyFragments;
            }
        }


 
        static
        public
        String
        Xmlns
        {
            get
            {
                return _xmlns;
            }
        }

        static
        public
        String
        XmlnsX
        {
            get
            {
                return _xmlnsX;
            }
        }

        static
        public
        String
        XmlLang
        {
            get
            {
                return _xmlLang;
            }
        }

        static
        public
        String
        XmlnsXSchema
        {
            get
            {
                return _xmlnsXSchema;
            }
        }

        static
        public
        String
        XmlLangValue
        {
            get
            {
                return _xmlLangValue;
            }
        }

        static
        public
        String
        XmlEngLangValue
        {
            get
            {
                return _xmlEngLangValue;
            }
        }

        static
        public
        String
        ImageUriPlaceHolder
        {
            get
            {
                return _imageUriPlaceHolder;
            }
        }

        static
        public
        String
        ColorContextUriPlaceHolder
        {
            get
            {
                return _colorContextUriPlaceHolder;
            }
        }

        static
        public
        String
        ResourceDictionaryUriPlaceHolder
        {
            get
            {
                return _resourceDictionaryUriPlaceHolder;
            }
        }

        static
        public
        String
        FontUriPlaceHolder
        {
            get
            {
                return _fontUriPlaceHolder;
            }
        }

        static
        public
        String
        ResourceDictionary
        {
            get
            {
                return _resourceDictionary;
            }
        }

        static
        public
        String
        PageResources
        {
            get
            {
                return _pageResources;
            }
        }


        static
        public
        String
        SignatureDefinition
        {
            get
            {
                return _signatureDefinition;
            }
        }

        static
        public
        String
        SignatureDefinitions
        {
            get
            {
                return _signatureDefinitions;
            }
        }

        static
        public
        String
        RequestedSigner
        {
            get
            {
                return _requestedSigner;
            }
        }

        static
        public
        String
        SpotLocation
        {
            get
            {
                return _spotLocation;
            }
        }

        static
        public
        String
        PageUri
        {
            get
            {
                return _pageUri;
            }
        }

        static
        public
        String
        StartX
        {
            get
            {
                return _startX;
            }
        }

        static
        public
        String
        StartY
        {
            get
            {
                return _startY;
            }
        }

        static
        public
        String
        Intent
        {
            get
            {
                return _intent;
            }
        }

        static
        public
        String
        SignBy
        {
            get
            {
                return _signBy;
            }
        }

        static
        public
        String
        SigningLocale
        {
            get
            {
                return _signingLocale;
            }
        }

        static
        public
        String
        SpotId
        {
            get
            {
                return _spotId;
            }
        }

        static
        public
        String
        GetXmlnsUri(
            int index
            )
        {
            return _xmlnsUri[index];
        }

        static
        public
        XmlnsUriContainer
        XmlnsUri
        {
            get
            {
                return _xmlnsUriContainer;
            }
        }

        static
        public
        ContentType
        ApplicationXaml
        {
            get
            {
                return  _applicationXaml;
            }
        }

        static
        public
        ContentType
        DocumentSequenceContentType
        {
            get
            {
                return  _documentSequenceContentType;
            }

        }


        static
        public
        ContentType
        FixedDocumentContentType      
        {
            get
            {
                return  _fixedDocumentContentType;
            }
        }


        static
        public
        ContentType
        FixedPageContentType          
        {
            get
            {
                return  _fixedPageContentType;
            }
        }


        static
        public
        ContentType
        DocumentStructureContentType          
        {
            get
            {
                return  _documentStructureContentType;
            }
        }


        static
        public
        ContentType
        StoryFragmentsContentType
        {
            get
            {
                return  _storyFragmentsContentType;
            }
        }



        static
        public
        ContentType
        SignatureDefintionType          
        {
            get
            {
                return  _signatureDefinitionType;
            }
        }

        static
        public
        ContentType
        CoreDocumentPropertiesType          
        {
            get
            {
                return  _coreDocumentPropertiesContentType;
            }
        }


        static
        public
        ContentType
        PrintTicketContentType       
        {
            get
            {
                return  _printTicketContentType;
            }
        }

        static
        public
        ContentType
        ResourceContentType          
        {
            get
            {
                return  _resourceContentType;
            }
        }

        static
        public
        ContentType
        FontContentType              
        {
            get
            {
                return  _fontContentType;
            }
        }

        static
        public
        ContentType
        FontObfuscatedContentType              
        {
            get
            {
                return  _obfuscatedContentType;
            }
        }

        static
        public
        ContentType
        ColorContextContentType              
        {
            get
            {
                return  _colorContextContentType;
            }
        }



        static
        public
        ContentType
        JpgContentType              
        {
            get
            {
                return  _jpgContentType;
            }
        }

        static
        public
        ContentType
        SigOriginContentType              
        {
            get
            {
                return  _sigOriginContentType;
            }
        }

        static
        public
        ContentType
        SigCertContentType              
        {
            get
            {
                return  _sigCertContentType;
            }
        }

        static
        public
        ContentType
        DiscardContentType              
        {
            get
            {
                return  _discardContentType;
            }
        }
        
        static
        public
        ContentType
        RelationshipContentType              
        {
            get
            {
                return  _relationshipContentType;
            }
        }

        static
        public
        String
        JpgExtension              
        {
            get
            {
                return  _jpgExtension;
            }
        }

        static
        public
        ContentType
        PngContentType              
        {
            get
            {
                return  _pngContentType;
            }
        }

        static
        public
        String
        PngExtension              
        {
            get
            {
                return  _pngExtension;
            }
        }
        
        static
        public
        ContentType
        TifContentType              
        {
            get
            {
                return  _tifContentType;
            }
        }

        static
        public
        String
        TifExtension              
        {
            get
            {
                return  _tifExtension;
            }
        }
        
        static
        public
        ContentType
        WdpContentType              
        {
            get
            {
                return  _wdpContentType;
            }
        }

        static
        public
        String
        WdpExtension              
        {
            get
            {
                return  _wdpExtension;
            }
        }

        static
        public
        ContentType
        WmpContentType
        {
            get
            {
                return  _wmpContentType;
            }
        }

        static
        public
        ContentType
        ResourceDictionaryContentType
        {
            get
            {
                return  _resourceDictionaryContentType;
            }
        }

        static
        public
        String
        DocumentSequenceNamespace
        {
            get
            {
                return  _documentSequenceNamespace;
            }
        }

        static
        public
        String
        FixedDocumentNamespace
        {
            get
            {
                return  _fixedDocumentNamespace;
            }
        }

        static
        public
        String
        SignatureDefinitionNamespace
        {
            get
            {
                return  _signatureDefinitionNamespace;
            }
        }

        static
        public
        String
        CorePropertiesRelationshipType
        {
            get
            {
                return _coreDocumentPropertiesRelationshipType;
            }
        }

        static
        public
        String
        StructureRelationshipName
        {
            get
            {
                return  _structureRelationshipName;
            }
        }

        static
        public
        String
        StoryFragmentsRelationshipName
        {
            get
            {
                return _storyFragmentsRelationshipName;
            }
        }

        static
        public
        String
        ReachPackageStartingPartRelationshipType
        {
            get
            {
                return _reachPackageStartingPartRelationshipType;
            }
        }
        
        static
        public
        String
        ResourceRelationshipName
        {
            get
            {
                return  _resourceRelationshipName;
            }
        }

        static
        public
        String
        PrintTicketRelationshipName
        {
            get
            {
                return  _printTicketRelationshipName;
            }
        }

        static
        public
        String
        SignatureDefinitionRelationshipName
        {
            get
            {
                return  _signatureDefinitionRelationshipName;
            }
        }

        static
        public
        String
        RestrictedFontRelationshipType
        {
            get
            {
                return  _restrictedFontRelationshipType;
            }
        }

        static
        public
        String
        DitialSignatureRelationshipType
        {
            get
            {
                return  _ditialSignatureRelationshipType;
            }
        }


        static
        public
        String
        ThumbnailRelationshipName
        {
            get
            {
                return  _thumbnailRelationshipName;
            }
        }

        static
        public
        String
        VersionExtensiblityNamespace
        {
            get
            {
                return  _versionExtensiblityNamespace;
            }
        }
        internal class XmlnsUriContainer
        {
            
            public
            XmlnsUriContainer(
                )
            {
            }

            public
            String this[int index]
            {
                get
                {
                    return XpsS0Markup.GetXmlnsUri(index);
                }
            }
        };

        private
        const
        String  _packageRelationshipUri             = "/_rels/.rels";

        private
        const
        String  _obfuscatedFontExt 		            = ".ODTTF";

        private
        const
        String  _pageWidth              	        = "Width";

        private
        const
        String  _pageHeight                         = "Height";

        private
        const
        String  _fixedPage                          = "FixedPage";

        private
        const
        String  _fixedDocument                      = "FixedDocument";

        private
        const
        String  _fixedDocumentSequence              = "FixedDocumentSequence";

        private
        const
        String  _pageContent                        = "PageContent";

        private
        const
        String  _documentReference                  = "DocumentReference";

        private
        const
        String _storyFragments                      = "StoryFragments";

        private
        const
        String  _xmlns                              = "xmlns";

        private
        const
        String  _xmlnsX                             = "xmlns:x";

        private
        const
        String  _xmlLang                            = "xml:lang";

        private
        const
        String  _xmlLangValue                       = "und";

        private
        const
        String _xmlEngLangValue                     = "en-us";

        private
        const
        String  _xmlnsXSchema                       = "http://schemas.microsoft.com/xps/2005/06/resourcedictionary-key";

        private
        const
        String  _imageUriPlaceHolder                = "placeholder";

        private
        const
        String  _colorContextUriPlaceHolder         = "placeholder";

        private
        const
        String  _resourceDictionaryUriPlaceHolder   = "placeholder";

        private
        const
        String  _fontUriPlaceHolder                 = "placeholder";

        private
        const
        String  _resourceDictionary                 = "ResourceDictionary";

        private
        const
        String  _pageResources                      = "FixedPage.Resources";

        private
        const
        String  _signatureDefinitions               = "SignatureDefinitions";

        private
        const
        String  _signatureDefinition                = "SignatureDefinition";

        private
        const
        String  _requestedSigner                    = "SignerName";

        private
        const
        String  _spotLocation                       = "SpotLocation";

        private
        const
        String  _pageUri                            = "PageURI";

        private
        const
        String  _startX                             = "StartX";

        private
        const
        String  _startY                             = "StartY";

        private
        const
        String  _intent                             = "Intent";

        private
        const
        String  _signBy                             = "SignBy";

        private
        const
        String  _signingLocale                      = "SigningLocation";

        private
        const
        String  _spotId                             = "SpotID";

        static
        private
        readonly
        String[]  _xmlnsUri                         = {"http://schemas.microsoft.com/xps/2005/06",
                                                       "http://schemas.microsoft.com/xps/2005/06",
                                                       "http://schemas.microsoft.com/xps/2005/06"};

        static
        private
        ContentType _applicationXaml                     = new ContentType("application/xaml+xml");

        static
        private
        ContentType _documentSequenceContentType         = new ContentType("application/vnd.ms-package.xps-fixeddocumentsequence+xml");

        static
        private
        ContentType _fixedDocumentContentType            = new ContentType("application/vnd.ms-package.xps-fixeddocument+xml");

        static
        private
        ContentType _fixedPageContentType                = new ContentType("application/vnd.ms-package.xps-fixedpage+xml");

        static
        private
        ContentType _documentStructureContentType        = new ContentType("application/vnd.ms-package.xps-documentstructure+xml");

        static
        private
        ContentType _storyFragmentsContentType           = new ContentType("application/vnd.ms-package.xps-storyfragments+xml");

        static
        private
        ContentType _printTicketContentType              = new ContentType("application/vnd.ms-printing.printticket+xml");

        static
        private
        ContentType _signatureDefinitionType             = new ContentType("application/xml");

        static
        private
        ContentType _coreDocumentPropertiesContentType   = new ContentType("application/vnd.openxmlformats-package.core-properties+xml");

        static
        private
        ContentType _resourceContentType                 = new ContentType("application/resource-PLACEHOLDER");

        static
        private
        ContentType _fontContentType                     = new ContentType("application/vnd.ms-opentype");

        static
        private
        ContentType _colorContextContentType             = new ContentType("application/vnd.ms-color.iccprofile");

        static
        private
        ContentType _obfuscatedContentType               = new ContentType("application/vnd.ms-package.obfuscated-opentype");

        static
        private
        ContentType _jpgContentType                      = new ContentType("image/jpeg");

        static
        private
        ContentType _sigOriginContentType                      = new ContentType("application/vnd.openxmlformats-package.digital-signature-origin");

        static
        private
        ContentType _sigCertContentType                      = new ContentType("application/vnd.openxmlformats-package.digital-signature-certificate");

        static
        private
        ContentType _discardContentType                      = new ContentType("application/vnd.ms-package.xps-discard-control+xml");

        static
        private
        ContentType _relationshipContentType                 = new ContentType("application/vnd.openxmlformats-package.relationships+xml");

        private
        const
        string _jpgExtension                             = "jpg";

        static
        private
        ContentType _pngContentType                      = new ContentType("image/png");

        private
        const
        string _pngExtension                             = "png";

        static
        private
        ContentType _tifContentType                      = new ContentType("image/tiff");

        static
        private
        string _tifExtension                             = "tif";

        static
        private
        ContentType _wdpContentType                      = new ContentType("image/vnd.ms-photo");

        private
        const
        string _wdpExtension                             = "wdp";

        static
        private
        ContentType _wmpContentType                      = new ContentType("image/vnd.ms-photo");

        static
        private
        ContentType _resourceDictionaryContentType       = new ContentType("application/vnd.ms-package.xps-resourcedictionary+xml");

        private
        const
        string _documentSequenceNamespace                = "http://schemas.microsoft.com/xps/2005/06";

        private
        const
        string _fixedDocumentNamespace                   = "http://schemas.microsoft.com/xps/2005/06";

        private
        const
        string _signatureDefinitionNamespace             = "http://schemas.microsoft.com/xps/2005/06/signature-definitions";

        private
        const
        string _resourceRelationshipName                 = "http://schemas.microsoft.com/xps/2005/06/required-resource";

        private
        const
        string _structureRelationshipName                = "http://schemas.microsoft.com/xps/2005/06/documentstructure";

        private
        const
        string _storyFragmentsRelationshipName           = "http://schemas.microsoft.com/xps/2005/06/storyfragments";

        private
        const
        string _printTicketRelationshipName              = "http://schemas.microsoft.com/xps/2005/06/printticket";

        private
        const
        string _signatureDefinitionRelationshipName      = "http://schemas.microsoft.com/xps/2005/06/signature-definitions";

        private
        const
        string _thumbnailRelationshipName                = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail";

        private
        const
        string _coreDocumentPropertiesRelationshipType   = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";

        private
        const
        string _reachPackageStartingPartRelationshipType = "http://schemas.microsoft.com/xps/2005/06/fixedrepresentation";
 
        private
        const
        string _restrictedFontRelationshipType           = "http://schemas.microsoft.com/xps/2005/06/restricted-font";

        private
        const
        string _ditialSignatureRelationshipType           = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/signature";
   
        static
        private
        XmlnsUriContainer  _xmlnsUriContainer            = new XmlnsUriContainer();

        private
        const
        string  _versionExtensiblityNamespace           ="http://schemas.openxmlformats.org/markup-compatibility/2006";
    };

    internal class XmlTags
    {
        private
        XmlTags(
            )
        {
        }

        static
        public
        String
        Source
        {
            get
            {
                return _source;
            }
        }                      

        private
        const
        String  _source = "Source";
    };
}

