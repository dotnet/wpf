// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Creates a PackWebRequest object
//
//

#if DEBUG
#define TRACE
#endif

using System;
using System.Net;
using System.Diagnostics;               // for Assert
using MS.Internal.IO.Packaging;         // for PackageCache
using MS.Internal.PresentationCore;     // for ExceptionStringTable
using System.Security;
using MS.Internal;

namespace System.IO.Packaging
{
    /// <summary>
    /// Invoked by .NET framework when our schema is recognized during a WebRequest
    /// </summary>
    public sealed class PackWebRequestFactory : IWebRequestCreate
    {
        static PackWebRequestFactory()
        {
#if DEBUG
            _traceSwitch = new BooleanSwitch("PackWebRequest", "PackWebRequest/Response and NetStream trace messages");
#endif
        }
        
        //------------------------------------------------------
        //
        //  IWebRequestCreate
        //
        //------------------------------------------------------
        /// <summary>
        /// Create
        /// </summary>
        /// <param name="uri">uri</param>
        /// <returns>PackWebRequest</returns>
        /// <remarks>Note that this factory may or may not be "registered" with the .NET WebRequest factory as handler
        /// for "pack" scheme web requests.  Because of this, callers should be sure to use the PackUriHelper static class
        /// to prepare their Uri's.  Calling any PackUriHelper method has the side effect of registering
        /// the "pack" scheme and associating this factory class as its default handler.</remarks>
        WebRequest IWebRequestCreate.Create(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            // Ensure uri is absolute - if we don't check now, the get_Scheme property will throw 
            // InvalidOperationException which would be misleading to the caller.
            if (!uri.IsAbsoluteUri)
                throw new ArgumentException(SR.Get(SRID.UriMustBeAbsolute), "uri");

            // Ensure uri is correct scheme because we can be called directly.  Case sensitive
            // is fine because Uri.Scheme contract is to return in lower case only.
            if (String.Compare(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.Ordinal) != 0)
                throw new ArgumentException(SR.Get(SRID.UriSchemeMismatch, PackUriHelper.UriSchemePack), "uri");

#if DEBUG
            if (_traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + 
                        "PackWebRequestFactory - responding to uri: " + uri);
#endif
            // only inspect cache if part name is present because cache only contains an object, not
            // the stream it was derived from
            Uri packageUri = System.IO.Packaging.PackUriHelper.GetPackageUri(uri);
            Uri partUri = System.IO.Packaging.PackUriHelper.GetPartUri(uri);

            if (partUri != null)
            {
                // Note: we look at PreloadedPackages first before we examine the PackageStore
                //  This is to make sure that an app cannot override any predefine packages

                // match cached object by authority component only - ignore the local path (part name)
                // inspect local package cache and default to that if possible

                // All predefined packages such as a package activated by DocumentApplication,
                //  ResourceContainer, and SiteOfOriginContainer are placed in PreloadedPackages
                bool cachedPackageIsThreadSafe;
                Package c = PreloadedPackages.GetPackage(packageUri, out cachedPackageIsThreadSafe);

                // If we don't find anything in the preloaded packages, look into the PackageStore
                bool cachedPackageIsFromPublicStore = false;
                if (c == null)
                {
                    cachedPackageIsThreadSafe = false;          // always assume PackageStore packages are not thread-safe
                    cachedPackageIsFromPublicStore = true;
                    
                    // Try to get a package from the package store
                    c = PackageStore.GetPackage(packageUri);
                }
                
                // do we have a package?
                if (c != null)
                {
#if DEBUG
                    if (_traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + 
                                "PackWebRequestFactory - cache hit - returning CachedPackWebRequest");
#endif
                    // use the cached object
                    return new PackWebRequest(uri, packageUri, partUri, c, 
                        cachedPackageIsFromPublicStore, cachedPackageIsThreadSafe);   
                }
}
        
#if DEBUG
            if (_traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + 
                        "PackWebRequestFactory - spawning regular PackWebRequest");
#endif
            return new PackWebRequest(uri, packageUri, partUri);
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // CreateWebRequest: Explicitly calls Create on PackWebRequest if uri is pack scheme
        // Ideally we would want to use RegisterPrefix and WebRequest.Create.
        // However, these two functions regress 700k working set in System.dll and System.xml.dll
        //  which is mostly for logging and config.
        // This helper function provides a way to bypass the regression
        [FriendAccessAllowed]
        internal static WebRequest CreateWebRequest(Uri uri)
        {
            if (String.Compare(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.Ordinal) == 0)
            {
                return ((IWebRequestCreate) _factorySingleton).Create(uri);
            }
            else
            {
                return WpfWebRequestHelper.CreateRequest(uri);
            }
        }

#if DEBUG
        internal static bool TraceSwitchEnabled
        {
            get
            {
                return _traceSwitch.Enabled;
            }

            set
            {
                _traceSwitch.Enabled = value;
            }
        }

        internal static System.Diagnostics.BooleanSwitch _traceSwitch;
#endif

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private static PackWebRequestFactory _factorySingleton = new PackWebRequestFactory();
    }
}
