// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text; 
#if PRESENTATION_CORE
using MS.Internal.PresentationCore;  // for FriendAccessAllowed and BindUriHelper.UriToString
#else
#error Class is being used from an unknown assembly.
#endif

namespace MS.Internal
{
    [FriendAccessAllowed]
    internal static class MimeTypeMapper
    {

        static internal ContentType GetMimeTypeFromUri(Uri uriSource)
        {
            ContentType mimeType = ContentType.Empty;

            if (uriSource != null)
            {
                Uri uri = uriSource;
                if (uri.IsAbsoluteUri == false)
                {
                      uri = new Uri("http://foo/bar/");
                      uri = new Uri(uri, uriSource);
                }
               
                string completeExt = GetFileExtension(uri);

                lock (((ICollection)_fileExtensionToMimeType).SyncRoot)
                {
                    // initialize for the first time
                    if (_fileExtensionToMimeType.Count == 0)
                    {

                        // Adding the known mime types to the hash table.

                        _fileExtensionToMimeType.Add(XamlExtension, XamlMime);
                        _fileExtensionToMimeType.Add(BamlExtension, BamlMime);
                        _fileExtensionToMimeType.Add(JpgExtension, JpgMime);
                        _fileExtensionToMimeType.Add(XbapExtension, XbapMime);

                    }


                    if (!_fileExtensionToMimeType.TryGetValue(completeExt, out mimeType))
                    {
                        //
                        // If the hashtable doesn't contain the MimeType for this extension, 
                        // Call UrlMon API to get it, once UrlMon API returns a vallid MimeType,
                        // update it into the hashtable, so that the next time query for a Uri 
                        // with the same extension will be faster.
                        //
                        mimeType = GetMimeTypeFromUrlMon(uriSource);

                        if (mimeType != ContentType.Empty)
                        {
                            _fileExtensionToMimeType.Add(completeExt, mimeType);
                        }

                    }
                }
            }

            return mimeType;
        }

        //
        // Call UrlMon API to get MimeType for a given extension.
        //
        static private ContentType GetMimeTypeFromUrlMon(Uri uriSource)
        {
            ContentType mimeType = ContentType.Empty;

            if (uriSource != null)
            {
                int retValue;
                string mimeTypeString;

                retValue = MS.Win32.Compile.UnsafeNativeMethods.FindMimeFromData(null,
                                                BindUriHelper.UriToString( uriSource ) ,
                                                IntPtr.Zero,
                                                0,
                                                null,
                                                0,
                                                out mimeTypeString,
                                                0);
          
                // For PreSharp 56031, 
                // This return value must be checked as the function 
                // will not throw an exception on failure.
                // the expected return value is S_OK.
                if (retValue == 0 && mimeTypeString != null)
                {
                    mimeType = new ContentType(mimeTypeString);
                }
            }

            return mimeType;
        }

        static private string GetDocument(Uri uri)
        {
            string docstring;

            if (uri.IsFile)
            {
                // LocalPath will un-escape characters, convert a file:///
                //  URI back into a local file system path.  It will also
                //  drop any post-pended characters.  (rogerch)
                //
                // "file:///c:/Program%20Files/foo.xmf#bar.jpg"
                //          becomes
                // "C:\Program Files\foo.xmf"
                docstring = uri.LocalPath;
            }
            else
            {
                // When not using the file scheme, escaped characters need
                //  to stay there and we rely on the Uri class to take care
                //  of figuring out what's going on.
                docstring = uri.GetLeftPart(UriPartial.Path);
            }

            return docstring;
        }

        static internal string GetFileExtension(Uri uri)
        {
            string docString = GetDocument(uri);
            string extensionWithDot = Path.GetExtension(docString);
            string extension = String.Empty;

            if (String.IsNullOrEmpty(extensionWithDot) == false)
            {
                extension = extensionWithDot.Substring(1).ToLower(CultureInfo.InvariantCulture);
            }
            
            return extension;
        }

        static internal bool IsHTMLMime(ContentType contentType)
        {
            return (HtmlMime.AreTypeAndSubTypeEqual(contentType)
                || HtmMime.AreTypeAndSubTypeEqual(contentType));
        }

        // The initial size of the hashtable mapps to the initial Known mimetypes.
        // If more known mimetypes are added later, please change this number also 
        // for better perf.
        private static readonly Dictionary<string, ContentType> _fileExtensionToMimeType = new Dictionary<string, ContentType>(4);

        // Unspported MIME type
        internal static readonly ContentType OctetMime = new ContentType("application/octet-stream");
        internal static readonly ContentType TextPlainMime = new ContentType("text/plain");

        // Known file extensions
        internal const string XamlExtension      = "xaml";
        internal const string BamlExtension      = "baml";
        internal const string XbapExtension      = "xbap";
        internal const string JpgExtension       = "jpg";

        // Supported MIME types:
        internal static readonly ContentType XamlMime = new ContentType("application/xaml+xml");
        internal static readonly ContentType BamlMime = new ContentType("application/baml+xml");
        internal static readonly ContentType JpgMime = new ContentType("image/jpg");
        internal static readonly ContentType IconMime = new ContentType("image/x-icon");

        internal static readonly ContentType FixedDocumentSequenceMime = new ContentType("application/vnd.ms-package.xps-fixeddocumentsequence+xml");
        internal static readonly ContentType FixedDocumentMime = new ContentType("application/vnd.ms-package.xps-fixeddocument+xml");
        internal static readonly ContentType FixedPageMime = new ContentType("application/vnd.ms-package.xps-fixedpage+xml");
        internal static readonly ContentType ResourceDictionaryMime = new ContentType("application/vnd.ms-package.xps-resourcedictionary+xml");

        internal static readonly ContentType HtmlMime = new ContentType("text/html");
        internal static readonly ContentType HtmMime = new ContentType("text/htm");
        internal static readonly ContentType XbapMime = new ContentType("application/x-ms-xbap");
    }
}
