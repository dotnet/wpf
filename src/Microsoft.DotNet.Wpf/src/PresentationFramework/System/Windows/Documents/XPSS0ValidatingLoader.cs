// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Xml.Schema;
using System.Net;
using System.Resources;
using System.Reflection;
using System.Security;

using MS.Internal;
using MS.Internal.IO.Packaging;

#endregion

using MS.Internal.IO.Packaging.Extensions;
using Package = System.IO.Packaging.Package;
using PackageRelationship = System.IO.Packaging.PackageRelationship;
using PackUriHelper = System.IO.Packaging.PackUriHelper;
using InternalPackUriHelper = MS.Internal.IO.Packaging.PackUriHelper;

namespace System.Windows.Documents
{
    internal class XpsValidatingLoader
    {
        internal XpsValidatingLoader()
        {
        }

        internal object Load(Stream stream, Uri parentUri, ParserContext pc, ContentType mimeType)
        {
            return Load(stream, parentUri, pc, mimeType, null);
        }

        internal void Validate(Stream stream, Uri parentUri, ParserContext pc, ContentType mimeType, string rootElement)
        {
            Load(stream, parentUri, pc, mimeType, rootElement);
        }

        /// <summary>
        /// rootElement == null: Load elements, validation of root element will occur in caller by checking object type or casting
        /// rootElement != null: Only perform validation, and expect rootElement at root of markup
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="parentUri"></param>
        /// <param name="pc"></param>
        /// <param name="mimeType"></param>
        /// <param name="rootElement"></param>
        /// <returns></returns>
        private object Load(Stream stream, Uri parentUri, ParserContext pc, ContentType mimeType, string rootElement)
        {
            object obj = null;
            List<Type> safeTypes = new List<Type> { typeof(System.Windows.ResourceDictionary) };

            if (!DocumentMode)
            {                       // Loose XAML, just check against schema, don't check content type
                if (rootElement==null)
                {
                    XmlReader reader = XmlReader.Create(stream, null, pc);
                    obj = XamlReader.Load(reader, pc, XamlParseMode.Synchronous, true, safeTypes);
                    stream.Close();
                }
            }
            else
            {                       // inside an XPS Document. Perform maximum validation
                XpsSchema schema = XpsSchema.GetSchema(mimeType);
                Uri uri = pc.BaseUri;

                Uri packageUri = PackUriHelper.GetPackageUri(uri);
                Uri partUri = PackUriHelper.GetPartUri(uri);

                Package package = PreloadedPackages.GetPackage(packageUri);

                Uri parentPackageUri = null;

                if (parentUri != null)
                {
                    parentPackageUri = PackUriHelper.GetPackageUri(parentUri);
                    if (!parentPackageUri.Equals(packageUri))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUriNotInSamePackage));
                    }
                }

                schema.ValidateRelationships(new SecurityCriticalData<Package>(package), packageUri, partUri, mimeType);

                if (schema.AllowsMultipleReferencesToSameUri(mimeType))
                {
                    _uniqueUriRef = null;
                }
                else
                {
                    _uniqueUriRef = new Hashtable(11);
                }

                Hashtable validResources = (_validResources.Count > 0 ? _validResources.Peek() : null);
                if (schema.HasRequiredResources(mimeType))
                {
                    validResources = new Hashtable(11);

                    PackagePart part = package.GetPart(partUri);
                    PackageRelationshipCollection requiredResources = part.GetRelationshipsByType(_requiredResourceRel);

                    foreach (PackageRelationship relationShip in requiredResources)
                    {
                        Uri targetUri = PackUriHelper.ResolvePartUri(partUri, relationShip.TargetUri);
                        Uri absTargetUri = PackUriHelper.Create(packageUri, targetUri);

                        PackagePart targetPart = package.GetPart(targetUri);

                        if (schema.IsValidRequiredResourceMimeType(targetPart.ValidatedContentType()))
                        {
                            if (!validResources.ContainsKey(absTargetUri))
                            {
                                validResources.Add(absTargetUri, true);
                            }
                        }
                        else
                        {
                            if (!validResources.ContainsKey(absTargetUri))
                            {
                                validResources.Add(absTargetUri, false);
                            }
                        }
                    }
                }

                XpsSchemaValidator xpsSchemaValidator = new XpsSchemaValidator(this, schema, mimeType,
                                                                                stream, packageUri, partUri);
                _validResources.Push(validResources);
                if (rootElement != null)
                {
                    xpsSchemaValidator.XmlReader.MoveToContent();

                    if (!rootElement.Equals(xpsSchemaValidator.XmlReader.Name))
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedMimeType));
                    }

                    while (xpsSchemaValidator.XmlReader.Read())
                        ;
                }
                else
                {
                    obj = XamlReader.Load(xpsSchemaValidator.XmlReader,
                                    pc,
                                    XamlParseMode.Synchronous, true, safeTypes);
                }
                _validResources.Pop();
            }

            return obj;
        }


        static internal bool DocumentMode
        {
            get
            {
                return _documentMode;
            }
        }


        static internal void AssertDocumentMode()
        {   // Once switched to document mode, we stay there
            _documentMode = true;
        }


        internal void UriHitHandler(int node,Uri uri)
        {
            if (_uniqueUriRef != null)
            {
                if (_uniqueUriRef.Contains(uri))
                {
                    if ((int)_uniqueUriRef[uri] != node)
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderDuplicateReference));
                    }
                }
                else
                {
                    _uniqueUriRef.Add(uri, node);
                }
            }
            Hashtable validResources = _validResources.Peek();
            if (validResources!=null)
            {
                if (!validResources.ContainsKey(uri))
                {
                    // The hashtable is case sensitive, packuris are not, so if we do not find in hashtable,
                    // do true comparison and add when found for next time...
                    bool found = false;
                    foreach (Uri resUri in validResources.Keys)
                    {
                        if (PackUriHelper.ComparePackUri(resUri,uri) == 0)
                        {
                            validResources.Add(uri, validResources[resUri]);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnlistedResource));
                    }
                }
                if (!(bool)validResources[uri])
                {
                    throw new FileFormatException(SR.Get(SRID.XpsValidatingLoaderUnsupportedMimeType));
                }
            }
        }

        static private Stack<Hashtable> _validResources = new Stack<Hashtable>();

        private Hashtable _uniqueUriRef;

        static
        private 
        bool _documentMode          = false;

        static 
        private 
        string _requiredResourceRel = "http://schemas.microsoft.com/xps/2005/06/required-resource";

        static private XpsS0FixedPageSchema xpsS0FixedPageSchema = new XpsS0FixedPageSchema();
        static private XpsS0ResourceDictionarySchema xpsS0ResourceDictionarySchema = new XpsS0ResourceDictionarySchema();
        static private XpsDocStructSchema xpsDocStructSchema = new XpsDocStructSchema();
    }
}

    
