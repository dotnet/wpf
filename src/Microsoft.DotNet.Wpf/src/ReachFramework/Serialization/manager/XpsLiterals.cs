// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
                                                                              
                                                                              
    Abstract:
        This file contains the definition of all literals / strings
        used either to represent the S0 markup or to denote certain
        named properties or attributes of the Xps Serialization
                
                                                                       
--*/

using MS.Internal;

namespace System.Windows.Xps.Packaging
{
    internal static class XpsNamedProperties
    {
        public
        static
        String
        PrintTicketProperty
        {
            get
            {
                return _printTicketProperty;
            }
        }

        public
        static
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

    internal static class XpsS0Markup
    {
        public
        static
        String
        PackageRelationshipUri
        {
            get
            {
                return _packageRelationshipUri;
            }
        }

        public
        static
        String
        ObfuscatedFontExt
        {
            get
            {
                return _obfuscatedFontExt;
            }
        }

        public
        static
        String
        PageWidth
        {
            get
            {
                return _pageWidth;
            }
        }

        public
        static
        String
        PageHeight
        {
            get
            {
                return _pageHeight;
            }
        }

        public
        static
        String
        FixedPage
        {
            get
            {
                return _fixedPage;
            }
        }

        public
        static
        String
        FixedDocument
        {
            get
            {
                return _fixedDocument;
            }
        }

        public
        static
        String
        FixedDocumentSequence
        {
            get
            {
                return _fixedDocumentSequence;
            }
        }

        public
        static
        String
        PageContent
        {
            get
            {
                return _pageContent;
            }
        }

        public
        static
        String
        DocumentReference
        {
            get
            {
                return _documentReference;
            }
        }

        public
        static
        String
        StoryFragments
        {
            get
            {
                return _storyFragments;
            }
        }


 
        public
        static
        String
        Xmlns
        {
            get
            {
                return _xmlns;
            }
        }

        public
        static
        String
        XmlnsX
        {
            get
            {
                return _xmlnsX;
            }
        }

        public
        static
        String
        XmlLang
        {
            get
            {
                return _xmlLang;
            }
        }

        public
        static
        String
        XmlnsXSchema
        {
            get
            {
                return _xmlnsXSchema;
            }
        }

        public
        static
        String
        XmlLangValue
        {
            get
            {
                return _xmlLangValue;
            }
        }

        public
        static
        String
        XmlEngLangValue
        {
            get
            {
                return _xmlEngLangValue;
            }
        }

        public
        static
        String
        ImageUriPlaceHolder
        {
            get
            {
                return _imageUriPlaceHolder;
            }
        }

        public
        static
        String
        ColorContextUriPlaceHolder
        {
            get
            {
                return _colorContextUriPlaceHolder;
            }
        }

        public
        static
        String
        ResourceDictionaryUriPlaceHolder
        {
            get
            {
                return _resourceDictionaryUriPlaceHolder;
            }
        }

        public
        static
        String
        FontUriPlaceHolder
        {
            get
            {
                return _fontUriPlaceHolder;
            }
        }

        public
        static
        String
        ResourceDictionary
        {
            get
            {
                return _resourceDictionary;
            }
        }

        public
        static
        String
        PageResources
        {
            get
            {
                return _pageResources;
            }
        }


        public
        static
        String
        SignatureDefinition
        {
            get
            {
                return _signatureDefinition;
            }
        }

        public
        static
        String
        SignatureDefinitions
        {
            get
            {
                return _signatureDefinitions;
            }
        }

        public
        static
        String
        RequestedSigner
        {
            get
            {
                return _requestedSigner;
            }
        }

        public
        static
        String
        SpotLocation
        {
            get
            {
                return _spotLocation;
            }
        }

        public
        static
        String
        PageUri
        {
            get
            {
                return _pageUri;
            }
        }

        public
        static
        String
        StartX
        {
            get
            {
                return _startX;
            }
        }

        public
        static
        String
        StartY
        {
            get
            {
                return _startY;
            }
        }

        public
        static
        String
        Intent
        {
            get
            {
                return _intent;
            }
        }

        public
        static
        String
        SignBy
        {
            get
            {
                return _signBy;
            }
        }

        public
        static
        String
        SigningLocale
        {
            get
            {
                return _signingLocale;
            }
        }

        public
        static
        String
        SpotId
        {
            get
            {
                return _spotId;
            }
        }

        public
        static
        String
        GetXmlnsUri(
            int index
            )
        {
            return _xmlnsUri[index];
        }

        public
        static
        XmlnsUriContainer
        XmlnsUri
        {
            get
            {
                return _xmlnsUriContainer;
            }
        }

        public
        static
        ContentType
        ApplicationXaml
        {
            get
            {
                return  _applicationXaml;
            }
        }

        public
        static
        ContentType
        DocumentSequenceContentType
        {
            get
            {
                return  _documentSequenceContentType;
            }

        }


        public
        static
        ContentType
        FixedDocumentContentType      
        {
            get
            {
                return  _fixedDocumentContentType;
            }
        }


        public
        static
        ContentType
        FixedPageContentType          
        {
            get
            {
                return  _fixedPageContentType;
            }
        }


        public
        static
        ContentType
        DocumentStructureContentType          
        {
            get
            {
                return  _documentStructureContentType;
            }
        }


        public
        static
        ContentType
        StoryFragmentsContentType
        {
            get
            {
                return  _storyFragmentsContentType;
            }
        }



        public
        static
        ContentType
        SignatureDefintionType          
        {
            get
            {
                return  _signatureDefinitionType;
            }
        }

        public
        static
        ContentType
        CoreDocumentPropertiesType          
        {
            get
            {
                return  _coreDocumentPropertiesContentType;
            }
        }


        public
        static
        ContentType
        PrintTicketContentType       
        {
            get
            {
                return  _printTicketContentType;
            }
        }

        public
        static
        ContentType
        ResourceContentType          
        {
            get
            {
                return  _resourceContentType;
            }
        }

        public
        static
        ContentType
        FontContentType              
        {
            get
            {
                return  _fontContentType;
            }
        }

        public
        static
        ContentType
        FontObfuscatedContentType              
        {
            get
            {
                return  _obfuscatedContentType;
            }
        }

        public
        static
        ContentType
        ColorContextContentType              
        {
            get
            {
                return  _colorContextContentType;
            }
        }



        public
        static
        ContentType
        JpgContentType              
        {
            get
            {
                return  _jpgContentType;
            }
        }

        public
        static
        ContentType
        SigOriginContentType              
        {
            get
            {
                return  _sigOriginContentType;
            }
        }

        public
        static
        ContentType
        SigCertContentType              
        {
            get
            {
                return  _sigCertContentType;
            }
        }

        public
        static
        ContentType
        DiscardContentType              
        {
            get
            {
                return  _discardContentType;
            }
        }
        
        public
        static
        ContentType
        RelationshipContentType              
        {
            get
            {
                return  _relationshipContentType;
            }
        }

        public
        static
        String
        JpgExtension              
        {
            get
            {
                return  _jpgExtension;
            }
        }

        public
        static
        ContentType
        PngContentType              
        {
            get
            {
                return  _pngContentType;
            }
        }

        public
        static
        String
        PngExtension              
        {
            get
            {
                return  _pngExtension;
            }
        }
        
        public
        static
        ContentType
        TifContentType              
        {
            get
            {
                return  _tifContentType;
            }
        }

        public
        static
        String
        TifExtension              
        {
            get
            {
                return  _tifExtension;
            }
        }
        
        public
        static
        ContentType
        WdpContentType              
        {
            get
            {
                return  _wdpContentType;
            }
        }

        public
        static
        String
        WdpExtension              
        {
            get
            {
                return  _wdpExtension;
            }
        }

        public
        static
        ContentType
        WmpContentType
        {
            get
            {
                return  _wmpContentType;
            }
        }

        public
        static
        ContentType
        ResourceDictionaryContentType
        {
            get
            {
                return  _resourceDictionaryContentType;
            }
        }

        public
        static
        String
        DocumentSequenceNamespace
        {
            get
            {
                return  _documentSequenceNamespace;
            }
        }

        public
        static
        String
        FixedDocumentNamespace
        {
            get
            {
                return  _fixedDocumentNamespace;
            }
        }

        public
        static
        String
        SignatureDefinitionNamespace
        {
            get
            {
                return  _signatureDefinitionNamespace;
            }
        }

        public
        static
        String
        CorePropertiesRelationshipType
        {
            get
            {
                return _coreDocumentPropertiesRelationshipType;
            }
        }

        public
        static
        String
        StructureRelationshipName
        {
            get
            {
                return  _structureRelationshipName;
            }
        }

        public
        static
        String
        StoryFragmentsRelationshipName
        {
            get
            {
                return _storyFragmentsRelationshipName;
            }
        }

        public
        static
        String
        ReachPackageStartingPartRelationshipType
        {
            get
            {
                return _reachPackageStartingPartRelationshipType;
            }
        }
        
        public
        static
        String
        ResourceRelationshipName
        {
            get
            {
                return  _resourceRelationshipName;
            }
        }

        public
        static
        String
        PrintTicketRelationshipName
        {
            get
            {
                return  _printTicketRelationshipName;
            }
        }

        public
        static
        String
        SignatureDefinitionRelationshipName
        {
            get
            {
                return  _signatureDefinitionRelationshipName;
            }
        }

        public
        static
        String
        RestrictedFontRelationshipType
        {
            get
            {
                return  _restrictedFontRelationshipType;
            }
        }

        public
        static
        String
        DitialSignatureRelationshipType
        {
            get
            {
                return  _ditialSignatureRelationshipType;
            }
        }


        public
        static
        String
        ThumbnailRelationshipName
        {
            get
            {
                return  _thumbnailRelationshipName;
            }
        }

        public
        static
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

        private
        static
        readonly
        String[]  _xmlnsUri                         = {"http://schemas.microsoft.com/xps/2005/06",
                                                       "http://schemas.microsoft.com/xps/2005/06",
                                                       "http://schemas.microsoft.com/xps/2005/06"};

        private
        static
        ContentType _applicationXaml                     = new ContentType("application/xaml+xml");

        private
        static
        ContentType _documentSequenceContentType         = new ContentType("application/vnd.ms-package.xps-fixeddocumentsequence+xml");

        private
        static
        ContentType _fixedDocumentContentType            = new ContentType("application/vnd.ms-package.xps-fixeddocument+xml");

        private
        static
        ContentType _fixedPageContentType                = new ContentType("application/vnd.ms-package.xps-fixedpage+xml");

        private
        static
        ContentType _documentStructureContentType        = new ContentType("application/vnd.ms-package.xps-documentstructure+xml");

        private
        static
        ContentType _storyFragmentsContentType           = new ContentType("application/vnd.ms-package.xps-storyfragments+xml");

        private
        static
        ContentType _printTicketContentType              = new ContentType("application/vnd.ms-printing.printticket+xml");

        private
        static
        ContentType _signatureDefinitionType             = new ContentType("application/xml");

        private
        static
        ContentType _coreDocumentPropertiesContentType   = new ContentType("application/vnd.openxmlformats-package.core-properties+xml");

        private
        static
        ContentType _resourceContentType                 = new ContentType("application/resource-PLACEHOLDER");

        private
        static
        ContentType _fontContentType                     = new ContentType("application/vnd.ms-opentype");

        private
        static
        ContentType _colorContextContentType             = new ContentType("application/vnd.ms-color.iccprofile");

        private
        static
        ContentType _obfuscatedContentType               = new ContentType("application/vnd.ms-package.obfuscated-opentype");

        private
        static
        ContentType _jpgContentType                      = new ContentType("image/jpeg");

        private
        static
        ContentType _sigOriginContentType                      = new ContentType("application/vnd.openxmlformats-package.digital-signature-origin");

        private
        static
        ContentType _sigCertContentType                      = new ContentType("application/vnd.openxmlformats-package.digital-signature-certificate");

        private
        static
        ContentType _discardContentType                      = new ContentType("application/vnd.ms-package.xps-discard-control+xml");

        private
        static
        ContentType _relationshipContentType                 = new ContentType("application/vnd.openxmlformats-package.relationships+xml");

        private
        const
        string _jpgExtension                             = "jpg";

        private
        static
        ContentType _pngContentType                      = new ContentType("image/png");

        private
        const
        string _pngExtension                             = "png";

        private
        static
        ContentType _tifContentType                      = new ContentType("image/tiff");

        private
        static
        string _tifExtension                             = "tif";

        private
        static
        ContentType _wdpContentType                      = new ContentType("image/vnd.ms-photo");

        private
        const
        string _wdpExtension                             = "wdp";

        private
        static
        ContentType _wmpContentType                      = new ContentType("image/vnd.ms-photo");

        private
        static
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
   
        private
        static
        XmlnsUriContainer  _xmlnsUriContainer            = new XmlnsUriContainer();

        private
        const
        string  _versionExtensiblityNamespace           ="http://schemas.openxmlformats.org/markup-compatibility/2006";
    };

    internal static class XmlTags
    {
        public
        static
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

