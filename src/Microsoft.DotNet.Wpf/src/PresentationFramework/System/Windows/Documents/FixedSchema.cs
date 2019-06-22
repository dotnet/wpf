// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Xml.Schema;
using System.Net;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Security;
using MS.Internal;

#endregion

using InternalPackUriHelper = MS.Internal.IO.Packaging.PackUriHelper;

namespace System.Windows.Documents
{
    internal class XpsSchemaValidator
    {
        private class XmlEncodingEnforcingTextReader : XmlTextReader
        {
            public XmlEncodingEnforcingTextReader(Stream objectStream)
                : base(objectStream)
            {
            }

            public override bool Read()
            {
                bool result = base.Read();

                if (result && !_encodingChecked)
                {
                    if (base.NodeType == XmlNodeType.XmlDeclaration)
                    {
                        string encoding = base["encoding"];

                        if (encoding != null)
                        {
                            if (!encoding.Equals(Encoding.Unicode.WebName, StringComparison.OrdinalIgnoreCase) &&
                                        !encoding.Equals(Encoding.UTF8.WebName, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedEncoding));
                            }
                        }
                    }

                    if (!(base.Encoding is UTF8Encoding) && !(base.Encoding is UnicodeEncoding))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedEncoding));
                    }

                    _encodingChecked = true;
                }

                return result;
            }

            private bool _encodingChecked;
        }

        public
        XpsSchemaValidator(
            XpsValidatingLoader loader,
            XpsSchema schema, 
            ContentType mimeType,
            Stream  objectStream,
            Uri packageUri,
            Uri baseUri
            )
        {
            XmlTextReader xmlTextReader = new XmlEncodingEnforcingTextReader(objectStream);

            xmlTextReader.ProhibitDtd = true;
            xmlTextReader.Normalization = true;

            XmlReader xmlReader = xmlTextReader;

            string [] predefinedNamespaces = _predefinedNamespaces;
            if ( !string.IsNullOrEmpty(schema.RootNamespaceUri) )
            {
                predefinedNamespaces = new string[_predefinedNamespaces.Length + 1];
                predefinedNamespaces[0] = schema.RootNamespaceUri;
                _predefinedNamespaces.CopyTo(predefinedNamespaces, 1);
            }

            xmlReader = new XmlCompatibilityReader(xmlReader, predefinedNamespaces);
            xmlReader = XmlReader.Create(xmlReader, schema.GetXmlReaderSettings());

            if (schema.HasUriAttributes(mimeType) && packageUri != null && baseUri != null)
            {
                xmlReader = new RootXMLNSAndUriValidatingXmlReader(loader, schema, 
                                                        xmlReader, packageUri, baseUri);
            }
            else
            {
                xmlReader = new RootXMLNSAndUriValidatingXmlReader(loader, schema, xmlReader);
            }

            _compatReader = xmlReader;
        }

        public
        XmlReader
        XmlReader
        {
            get
            {
                return _compatReader;
            }
        }

        private
        XmlReader               _compatReader;
        static private string [] _predefinedNamespaces = new string [1] { 
            XamlReaderHelper.DefinitionMetroNamespaceURI
        };

        private class RootXMLNSAndUriValidatingXmlReader : XmlWrappingReader
        {
            public RootXMLNSAndUriValidatingXmlReader(
                        XpsValidatingLoader loader,
                        XpsSchema schema,
                        XmlReader xmlReader,
                        Uri packageUri,
                        Uri baseUri)
                        : base(xmlReader)
            {
                _loader = loader;
                _schema = schema;
                _packageUri = packageUri;
                _baseUri = baseUri;
            }

            public RootXMLNSAndUriValidatingXmlReader(
                        XpsValidatingLoader loader,
                        XpsSchema schema,
                        XmlReader xmlReader )
                : base(xmlReader)
            {
                _loader = loader;
                _schema = schema;
            }

            private void CheckUri(string attr)
            {
                CheckUri(Reader.LocalName, attr);
            }

            private void CheckUri(string localName, string attr)
            {
                if (!object.ReferenceEquals(attr, _lastAttr))      // Check for same string object, not for equality!
                {
                    _lastAttr = attr;
                    string [] uris = _schema.ExtractUriFromAttr(localName, attr);
                    if (uris != null)
                    {
                        foreach (string uriAttr in uris)
                        {
                            if (uriAttr.Length > 0)
                            {
                                Uri targetUri = PackUriHelper.ResolvePartUri(_baseUri, new Uri(uriAttr, UriKind.Relative));
                                Uri absTargetUri = PackUriHelper.Create(_packageUri, targetUri);
                                _loader.UriHitHandler(_node,absTargetUri);
                            }
                        }
                    }
                }
            }

            public override string Value                
            { 
                get 
                {
                    CheckUri(Reader.Value);
                    return Reader.Value;
                }
            }

            public override string GetAttribute( string name ) 
            {
                string attr= Reader.GetAttribute( name );
                CheckUri(name,attr);
                return attr;
            }

            public override string GetAttribute( string name, string namespaceURI ) 
            {
                string attr = Reader.GetAttribute(name, namespaceURI);
                CheckUri(attr);
                return attr;
            }

            public override string GetAttribute( int i ) 
            {
                string attr = Reader.GetAttribute( i );
                CheckUri(attr);
                return attr;
            }

            public override bool Read() 
            {
                bool result;
                _node++;
                result = Reader.Read();

                if ( (Reader.NodeType == XmlNodeType.Element) && !_rootXMLNSChecked )
                {
                    if (!_schema.IsValidRootNamespaceUri(Reader.NamespaceURI))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedRootNamespaceUri));
                    }
                    _rootXMLNSChecked = true;
                }

                return result;
            }

            private XpsValidatingLoader _loader;
            private XpsSchema _schema;
            private Uri _packageUri;
            private Uri _baseUri;
            private string _lastAttr;
            private int _node;
            private bool _rootXMLNSChecked;
        }
    }


    internal class XpsSchema
    {
        protected XpsSchema()
        {
        }

        static protected void RegisterSchema(XpsSchema schema, ContentType[] handledMimeTypes)
        {
            foreach (ContentType mime in handledMimeTypes)
            {
                _schemas.Add(mime, schema);
            }
        }

        protected void RegisterRequiredResourceMimeTypes(ContentType[] requiredResourceMimeTypes)
        {
            if (requiredResourceMimeTypes != null)
            {
                foreach (ContentType type in requiredResourceMimeTypes)
                {
                    _requiredResourceMimeTypes.Add(type, true);
                }
            }
        }

        public virtual XmlReaderSettings GetXmlReaderSettings()
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();

            xmlReaderSettings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessIdentityConstraints | System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings;

            return xmlReaderSettings;
        }


        public virtual void ValidateRelationships(SecurityCriticalData<Package> package, Uri packageUri, Uri partUri, ContentType mimeType)
        {
        }

        public virtual bool HasRequiredResources(ContentType mimeType)
        {
            return false;
        }

        public virtual bool HasUriAttributes(ContentType mimeType)
        {
            return false;
        }

        public virtual bool AllowsMultipleReferencesToSameUri(ContentType mimeType)
        {
            return true;
        }

        public virtual bool IsValidRootNamespaceUri(string namespaceUri)
        {
            return false;
        }

        public virtual string RootNamespaceUri
        {
            get
            {
                return "";
            }
        }

        public bool IsValidRequiredResourceMimeType(ContentType mimeType)
        {
            foreach (ContentType ct in _requiredResourceMimeTypes.Keys)
            {
                if (ct.AreTypeAndSubTypeEqual(mimeType))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual string [] ExtractUriFromAttr(string attrName, string attrValue)
        {
            return null;
        }

        static public XpsSchema GetSchema(ContentType mimeType)
        {
            XpsSchema schema = null;

            if (!_schemas.TryGetValue(mimeType, out schema))
            {
                throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedMimeType));
            }

            return schema;
        }

        static private readonly Dictionary<ContentType, XpsSchema> _schemas = new Dictionary<ContentType, XpsSchema>(new ContentType.StrongComparer());
        private Hashtable _requiredResourceMimeTypes = new Hashtable(11);
    }

    internal class XpsS0Schema:XpsSchema
    {
        // When creating a new schema, add a static member to XpsSchemaValidator to register it.
        protected
        XpsS0Schema()
        {
        }

        public override XmlReaderSettings GetXmlReaderSettings()
        {
            if (_xmlReaderSettings == null)
            {
                _xmlReaderSettings = new XmlReaderSettings();

                _xmlReaderSettings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessIdentityConstraints | System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings;

                MemoryStream xpsSchemaStream = new MemoryStream(XpsS0Schema.S0SchemaBytes);
                MemoryStream dictionarySchemaStream = new MemoryStream(XpsS0Schema.DictionarySchemaBytes);

                XmlResolver resolver = new XmlUrlResolver();

                _xmlReaderSettings.ValidationType = ValidationType.Schema;
                _xmlReaderSettings.Schemas.XmlResolver = resolver;

                _xmlReaderSettings.Schemas.Add(_xpsS0SchemaNamespace,
                                            new XmlTextReader(xpsSchemaStream));
                _xmlReaderSettings.Schemas.Add(null,
                                            new XmlTextReader(dictionarySchemaStream));
            }

            return _xmlReaderSettings;
        }

        public override bool HasRequiredResources(ContentType mimeType)
        {
            if (_fixedPageContentType.AreTypeAndSubTypeEqual(mimeType))
            {
                return true;
            }

            return false;
        }

        public override bool HasUriAttributes(ContentType mimeType)
        {
            // All of the root elements for content types supported by this schema have Uri attributes that need to be checked
            return true;
        }

        public override bool AllowsMultipleReferencesToSameUri(ContentType mimeType)
        {
            if (_fixedDocumentSequenceContentType.AreTypeAndSubTypeEqual(mimeType) ||
                _fixedDocumentContentType.AreTypeAndSubTypeEqual(mimeType))
            {
                // FixedDocumentSequence - FixedDocument - FixedPage must form a tree. Cannot share elements
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool IsValidRootNamespaceUri(string namespaceUri)
        {
            return namespaceUri.Equals(_xpsS0SchemaNamespace, StringComparison.Ordinal);
        }

        public override string RootNamespaceUri
        {
            get
            {
                return _xpsS0SchemaNamespace;
            }
        }

        public override string[] ExtractUriFromAttr(string attrName, string attrValue)
        {
            // Note: Do not check for "FixedPage.NavigateUri", because external references are allowed.
            if (attrName.Equals("Source", StringComparison.Ordinal) ||
                attrName.Equals("FontUri", StringComparison.Ordinal))
            {
                return new string[] { attrValue };
            }
            else if (attrName.Equals("ImageSource", StringComparison.Ordinal))
            {
                if (attrValue.StartsWith(_colorConvertedBitmap, StringComparison.Ordinal))
                {
                    attrValue = attrValue.Substring(_colorConvertedBitmap.Length);
                    string[] pieces = attrValue.Split(new char[] { ' ', '}' });

                    return pieces;
                }
                else
                {
                    return new string[] { attrValue };
                }
            }
            else if (attrName.Equals("Color", StringComparison.Ordinal) ||
                attrName.Equals("Fill", StringComparison.Ordinal) ||
                attrName.Equals("Stroke", StringComparison.Ordinal))
            {
                attrValue = attrValue.Trim();
                if (attrValue.StartsWith(_contextColor, StringComparison.Ordinal))
                {
                    attrValue = attrValue.Substring(_contextColor.Length);
                    attrValue = attrValue.Trim();
                    string[] tokens = attrValue.Split(new char[] { ' ' });
                    if (tokens.GetLength(0) >= 1)
                    {
                        return new string[] { tokens[0] };
                    }
                }
            }

            return null;
        }

        static
        private
        byte[] 
        S0SchemaBytes
        {
            get
            {
                ResourceManager resourceManager = new ResourceManager( "Schemas_S0", Assembly.GetAssembly(typeof(XpsS0Schema)));
                return (byte[])resourceManager.GetObject("s0schema.xsd");
            }
        }

        static
        private
        byte[] 
        DictionarySchemaBytes
        {
            get
            {
                ResourceManager resourceManager = new ResourceManager( "Schemas_S0", Assembly.GetAssembly(typeof(XpsS0Schema)));
                return (byte[])resourceManager.GetObject("rdkey.xsd");
            }
        }

        static
        protected
        ContentType _fontContentType = new ContentType("application/vnd.ms-opentype");

        static
        protected
        ContentType _colorContextContentType = new ContentType("application/vnd.ms-color.iccprofile");

        static
        protected
        ContentType _obfuscatedContentType = new ContentType("application/vnd.ms-package.obfuscated-opentype");

        static
        protected
        ContentType _jpgContentType = new ContentType("image/jpeg");

        static
        protected
        ContentType _pngContentType = new ContentType("image/png");

        static
        protected
        ContentType _tifContentType = new ContentType("image/tiff");

        static
        protected
        ContentType _wmpContentType = new ContentType("image/vnd.ms-photo");

        static
        protected
        ContentType _fixedDocumentSequenceContentType = new ContentType("application/vnd.ms-package.xps-fixeddocumentsequence+xml");

        static
        protected
        ContentType _fixedDocumentContentType = new ContentType("application/vnd.ms-package.xps-fixeddocument+xml");

        static
        protected
        ContentType _fixedPageContentType = new ContentType("application/vnd.ms-package.xps-fixedpage+xml");

        static
        protected
        ContentType _resourceDictionaryContentType = new ContentType("application/vnd.ms-package.xps-resourcedictionary+xml");

        static
        protected
        ContentType _printTicketContentType = new ContentType("application/vnd.ms-printing.printticket+xml");

        static
        protected
        ContentType _discardControlContentType = new ContentType("application/vnd.ms-package.xps-discard-control+xml");

        private
        const
        String _xpsS0SchemaNamespace = "http://schemas.microsoft.com/xps/2005/06";

        private
        const
        string _contextColor = "ContextColor ";

        private
        const
        string _colorConvertedBitmap = "{ColorConvertedBitmap ";

        static
        private
        XmlReaderSettings _xmlReaderSettings;
    }

    internal sealed class XpsS0FixedPageSchema : XpsS0Schema
    {
        public
        XpsS0FixedPageSchema()
        {
            RegisterSchema(this,
                new ContentType[] {  _fixedDocumentSequenceContentType, 
                                _fixedDocumentContentType,
                                _fixedPageContentType
                                }
                    );
            RegisterRequiredResourceMimeTypes(
                new ContentType[] {
                                _resourceDictionaryContentType,
                                _fontContentType, 
                                _colorContextContentType, 
                                _obfuscatedContentType,
                                _jpgContentType,
                                _pngContentType,
                                _tifContentType,
                                _wmpContentType
                                }
                    );
        }

        public override void ValidateRelationships(SecurityCriticalData<Package> package, Uri packageUri, Uri partUri, ContentType mimeType)
        {
            PackagePart part = package.Value.GetPart(partUri);
            PackageRelationshipCollection checkRels;
            int count;

            // Can only have 0 or 1 PrintTicket per FDS, FD or FP part
            checkRels = part.GetRelationshipsByType(_printTicketRel);
            count = 0;
            foreach (PackageRelationship rel in checkRels)
            {
                count++;
                if (count > 1)
                {
                    throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderMoreThanOnePrintTicketPart));
                }

                // Also check for existence and type
                Uri targetUri = PackUriHelper.ResolvePartUri(partUri, rel.TargetUri);
                Uri absTargetUri = PackUriHelper.Create(packageUri, targetUri);

                PackagePart targetPart = package.Value.GetPart(targetUri);

                if (!_printTicketContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)))
                {
                    throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderPrintTicketHasIncorrectType));
                }
            }

            checkRels = part.GetRelationshipsByType(_thumbnailRel);
            count = 0;
            foreach (PackageRelationship rel in checkRels)
            {
                count++;
                if (count > 1)
                {
                    throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderMoreThanOneThumbnailPart));
                }

                // Also check for existence and type
                Uri targetUri = PackUriHelper.ResolvePartUri(partUri, rel.TargetUri);
                Uri absTargetUri = PackUriHelper.Create(packageUri, targetUri);

                PackagePart targetPart = package.Value.GetPart(targetUri);

                if (!_jpgContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)) &&
                    !_pngContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)))
                {
                    throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderThumbnailHasIncorrectType));
                }
            }

            // FixedDocument only has restricted font relationships
            if (_fixedDocumentContentType.AreTypeAndSubTypeEqual(mimeType))
            {
                // Check if target of restricted font relationship is present and is actually a font
                checkRels = part.GetRelationshipsByType(_restrictedFontRel);
                foreach (PackageRelationship rel in checkRels)
                {
                    // Check for existence and type
                    Uri targetUri = PackUriHelper.ResolvePartUri(partUri, rel.TargetUri);
                    Uri absTargetUri = PackUriHelper.Create(packageUri, targetUri);

                    PackagePart targetPart = package.Value.GetPart(targetUri);

                    if (!_fontContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)) &&
                            !_obfuscatedContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderRestrictedFontHasIncorrectType));
                    }
                }
            }

            // check constraints for XPS fixed payload start part
            if (_fixedDocumentSequenceContentType.AreTypeAndSubTypeEqual(mimeType))
            {
                // This is the XPS payload root part. We also should check if the Package only has at most one discardcontrol...
                checkRels = package.Value.GetRelationshipsByType(_discardControlRel);
                count = 0;
                foreach (PackageRelationship rel in checkRels)
                {
                    count++;
                    if (count > 1)
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderMoreThanOneDiscardControlInPackage));
                    }

                    // Also check for existence and type
                    Uri targetUri = PackUriHelper.ResolvePartUri(partUri, rel.TargetUri);
                    Uri absTargetUri = PackUriHelper.Create(packageUri, targetUri);

                    PackagePart targetPart = package.Value.GetPart(targetUri);

                    if (!_discardControlContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderDiscardControlHasIncorrectType));
                    }
                }

                // This is the XPS payload root part. We also should check if the Package only has at most one thumbnail...
                checkRels = package.Value.GetRelationshipsByType(_thumbnailRel);
                count = 0;
                foreach (PackageRelationship rel in checkRels)
                {
                    count++;
                    if (count > 1)
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderMoreThanOneThumbnailInPackage));
                    }

                    // Also check for existence and type
                    Uri targetUri = PackUriHelper.ResolvePartUri(partUri, rel.TargetUri);
                    Uri absTargetUri = PackUriHelper.Create(packageUri, targetUri);

                    PackagePart targetPart = package.Value.GetPart(targetUri);

                    if (!_jpgContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)) &&
                        !_pngContentType.AreTypeAndSubTypeEqual(new ContentType(targetPart.ContentType)))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderThumbnailHasIncorrectType));
                    }
                }
            }
        }

        private
        const
        string _printTicketRel = "http://schemas.microsoft.com/xps/2005/06/printticket";

        private
        const
        string _discardControlRel = "http://schemas.microsoft.com/xps/2005/06/discard-control";

        private
        const
        string _restrictedFontRel = "http://schemas.microsoft.com/xps/2005/06/restricted-font";

        private
        const
        string _thumbnailRel = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail";
    }

    internal sealed class XpsS0ResourceDictionarySchema : XpsS0Schema
    {
        // When creating a new schema, add a static member to XpsSchemaValidator to register it.
        public
        XpsS0ResourceDictionarySchema()
        {
            RegisterSchema(this,
                new ContentType[] {  _resourceDictionaryContentType
                                }
                    );
        }

        public override string [] ExtractUriFromAttr(string attrName, string attrValue)
        {
            if (attrName.Equals("Source", StringComparison.Ordinal))      // Cannot chain remote ResourceDictionary parts.
            {
                throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedMimeType));
            }

            return base.ExtractUriFromAttr(attrName, attrValue);
        }
    }

    internal sealed class XpsDocStructSchema : XpsSchema
    {
        // When creating a new schema, add a static member to XpsSchemaValidator to register it.
        public
        XpsDocStructSchema()
        {
            RegisterSchema(this, new ContentType[] { _documentStructureContentType,
                                            _storyFragmentsContentType } );
        }

        public override XmlReaderSettings GetXmlReaderSettings()
        {
            if (_xmlReaderSettings == null)
            {
                _xmlReaderSettings = new XmlReaderSettings();

                _xmlReaderSettings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessIdentityConstraints | System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings;

                MemoryStream xpsSchemaStream = new MemoryStream(XpsDocStructSchema.SchemaBytes);

                XmlResolver resolver = new XmlUrlResolver();

                _xmlReaderSettings.ValidationType = ValidationType.Schema;
                _xmlReaderSettings.Schemas.XmlResolver = resolver;

                _xmlReaderSettings.Schemas.Add(_xpsDocStructureSchemaNamespace,
                                            new XmlTextReader(xpsSchemaStream));
            }

            return _xmlReaderSettings;
        }

        public override bool IsValidRootNamespaceUri(string namespaceUri)
        {
            return namespaceUri.Equals(_xpsDocStructureSchemaNamespace, StringComparison.Ordinal);
        }

        public override string RootNamespaceUri
        {
            get
            {
                return _xpsDocStructureSchemaNamespace;
            }
        }

        static
        private
        byte[]
        SchemaBytes
        {
            get
            {
                ResourceManager resourceManager = new ResourceManager("Schemas_DocStructure", Assembly.GetAssembly(typeof(XpsDocStructSchema)));
                return (byte[])resourceManager.GetObject("DocStructure.xsd");
            }
        }

        static
        private
        ContentType _documentStructureContentType = new ContentType("application/vnd.ms-package.xps-documentstructure+xml");

        static
        private
        ContentType _storyFragmentsContentType    = new ContentType("application/vnd.ms-package.xps-storyfragments+xml");
        
        private
        const
        String _xpsDocStructureSchemaNamespace = "http://schemas.microsoft.com/xps/2005/06/documentstructure";

        static
        private
        XmlReaderSettings _xmlReaderSettings;
    }
}

    
