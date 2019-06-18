// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  WebRequest class to handle pack-specific URI's


#if DEBUG
#define TRACE
#endif

using System;
using System.IO;
using System.Net;
using System.Net.Cache;                 // for RequestCachePolicy
using System.Runtime.Serialization;
using System.Diagnostics;               // For Assert
using MS.Utility;                       // for EventTrace
using MS.Internal.IO.Packaging;         // for PackageCacheEntry
using MS.Internal.PresentationCore;     // for SRID exception strings
using System.Security;                  // for SecurityCritical
using MS.Internal;

namespace System.IO.Packaging
{
    /// <summary>
    /// pack-specific WebRequest handler
    /// </summary>
    /// <remarks>
    /// This WebRequest overload exists to handle pack-specific URI's based on the "pack" custom schema.
    /// Note that this schema may or may not be "registered" with the .NET WebRequest factory so callers
    /// should be sure to use the PackUriHelper static class to prepare their Uri's.  PackUriHelper use has the
    /// side effect of registering the "pack" scheme and associating the PackWebRequest class as its default handler.
    /// </remarks>
    public sealed class PackWebRequest : WebRequest
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uri">uri to resolve</param>
        /// <param name="packageUri">uri of the package</param>
        /// <param name="partUri">uri of the part - may be null</param>
        /// <remarks>This should only be called by PackWebRequestFactory </remarks>
        /// <exception cref="ArgumentException">Will throw an ArgumentException if the given URI is not of the correct scheme</exception>
        internal PackWebRequest(Uri uri, Uri packageUri, Uri partUri)
            : this(uri, packageUri, partUri, null, false, false)
        {
        }

        /// <summary>
        /// Cached instance constructor
        /// </summary>
        /// <param name="cacheEntry">cache entry to base this response on</param>
        /// <param name="uri">uri to resolve</param>
        /// <param name="packageUri">uri of the package</param>
        /// <param name="partUri">uri of the part - may be null</param>
        /// <param name="respectCachePolicy">should we throw if cache policy conflicts?</param>
        /// <param name="cachedPackageIsThreadSafe">is the cacheEntry thread-safe?</param>
        /// <remarks>This should only be called by PackWebRequestFactory</remarks>
        /// <exception cref="ArgumentException">Will throw an ArgumentException if the given URI is not of the correct scheme</exception>
        internal PackWebRequest(Uri uri, Uri packageUri, Uri partUri, Package cacheEntry,
            bool respectCachePolicy, bool cachedPackageIsThreadSafe)
        {
            Debug.Assert(uri != null, "PackWebRequest uri cannot be null");
            Debug.Assert(packageUri != null, "packageUri cannot be null");

            // keep these
            _uri = uri;
            _innerUri = packageUri;
            _partName = partUri;
            _cacheEntry = cacheEntry;
            _respectCachePolicy = respectCachePolicy;
            _cachedPackageIsThreadSafe = cachedPackageIsThreadSafe;
            _cachePolicy = _defaultCachePolicy;         // always use default and then let them change it

#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled && (cacheEntry != null))
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + 
                        "PackWebRequest - working from Package Cache");
#endif
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region WebRequest - Sync
        /// <summary>
        /// GetRequestStream
        /// </summary>
        /// <returns>stream</returns>
        /// <exception cref="NotSupportedException">writing not supported</exception>
        public override Stream GetRequestStream()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a WebResponse object
        /// </summary>
        /// <returns>PackWebResponse</returns>
        /// <remarks>Caller must eventually call Close() to avoid leaking resources.</remarks>
        public override WebResponse GetResponse()
        {
            bool cachedPackageAvailable = IsCachedPackage;
            
            // if there is no cached package or it is from the public PackageStore, we must respect CachePolicy
            if (!cachedPackageAvailable || (cachedPackageAvailable && _respectCachePolicy))
            {
                // inspect and act on CachePolicy
                RequestCacheLevel policy = _cachePolicy.Level;
                if (policy == RequestCacheLevel.Default)
                    policy = _defaultCachePolicy.Level;

                switch (policy)
                {
                    case RequestCacheLevel.BypassCache:
                        {
                            // ignore cache entry
                            cachedPackageAvailable = false;
                        } break;

                    case RequestCacheLevel.CacheOnly:
                        {
                            // only use cached value
                            if (!cachedPackageAvailable)
                                throw new WebException(SR.Get(SRID.ResourceNotFoundUnderCacheOnlyPolicy));
                        } break;

                    case RequestCacheLevel.CacheIfAvailable:
                        {
                            // use cached value if possible - we need take no explicit action here
                        } break;

                    default:
                        {
                            throw new WebException(SR.Get(SRID.PackWebRequestCachePolicyIllegal));
                        }
                }
            }
            
            if (cachedPackageAvailable)
            {
#if DEBUG
                if (PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + 
                        "PackWebRequest - Getting response from Package Cache");
#endif
                return new PackWebResponse(_uri, _innerUri, _partName, _cacheEntry, _cachedPackageIsThreadSafe);
            }
            else
            {
                // only return a real WebRequest instance - throw on a PseudoWebRequest
                WebRequest request = GetRequest(false);
                if (_webRequest == null || _webRequest is PseudoWebRequest)
                    throw new InvalidOperationException(SR.Get(SRID.SchemaInvalidForTransport));

#if DEBUG
                if (PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + 
                        "PackWebRequest - Getting new response");
#endif
                // Create a new response for every call
                return new PackWebResponse(_uri, _innerUri, _partName, request);
            }
        }
        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Properties

        /// <summary>
        /// CachePolicy for the PackWebRequest
        /// </summary>
        /// <remarks>This value is distinct from the CachePolicy of the InnerRequest.</remarks>
        /// <value></value>
        public override RequestCachePolicy CachePolicy
        {
            get
            {
                return _cachePolicy;
            }
            set
            {
                if (value == null)
                    _cachePolicy = _defaultCachePolicy;
                else
                {
                    switch (value.Level)
                    {
                        case RequestCacheLevel.BypassCache: break;
                        case RequestCacheLevel.CacheOnly: break;
                        case RequestCacheLevel.CacheIfAvailable: break;
                        default:
                            throw new WebException(SR.Get(SRID.PackWebRequestCachePolicyIllegal));
                    }

                    _cachePolicy = value;
                }
            }
        }

        /// <summary>
        /// ConnectionGroupName
        /// </summary>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        /// <value>name of current connection group</value>
        public override string ConnectionGroupName
        {
            get
            {
                return GetRequest().ConnectionGroupName;
            }
            set
            {
                GetRequest().ConnectionGroupName = value;
            }
        }


        /// <summary>
        /// ContentLength
        /// </summary>
        /// <value>length of RequestStream</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        /// <exception cref="NotSupportedException">Set is not supported as PackWebRequest is read-only.</exception>
        public override long ContentLength
        {
            get
            {
                return GetRequest().ContentLength;
            }
            set
            {
                // we don't upload so no reason to support this
                throw new NotSupportedException();
            }
        }


        /// <summary>
        /// ContentType
        /// </summary>
        /// <value>Content type of the request data being sent or data being requested.  Null is explicitly allowed.</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        public override string ContentType
        {
            get
            {
                string contentType = GetRequest().ContentType;
                if (contentType == null)
                    return contentType;
                else
                    //We call the ContentType constructor to validate the grammar for
                    //content type string.
                    return new MS.Internal.ContentType(contentType).ToString();
            }
            set
            {
                // this property can indicate to the server what content type we prefer
                GetRequest().ContentType = value;
            }
        }


        /// <summary>
        /// Credentials
        /// </summary>
        /// <value>Credentials to use when authenticating against the resource</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        public override ICredentials Credentials
        {
            get
            {
                return GetRequest().Credentials;
            }
            set
            {
                GetRequest().Credentials = value;
            }
        }


        /// <summary>
        /// Headers
        /// </summary>
        /// <value>collection of header name/value pairs associated with the request</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        public override WebHeaderCollection Headers
        {
            get
            {
                return GetRequest().Headers;
            }
            set
            {
                GetRequest().Headers = value;
            }
        }

        /// <summary>
        /// Method
        /// </summary>
        /// <value>protocol method to use in this request</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        public override string Method
        {
            get
            {
                return GetRequest().Method;
            }
            set
            {
                GetRequest().Method = value;
            }
        }


        /// <summary>
        /// PreAuthenticate
        /// </summary>
        /// <value>indicates whether to preauthenticate the request</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        public override bool PreAuthenticate
        {
            get
            {
                return GetRequest().PreAuthenticate;
            }
            set
            {
                GetRequest().PreAuthenticate = value;
            }
        }


        /// <summary>
        /// Proxy
        /// </summary>
        /// <value>network proxy to use to access this Internet resource</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        public override IWebProxy Proxy
        {
            get
            {
                return GetRequest().Proxy;
            }
            set
            {
                GetRequest().Proxy = value;
            }
        }


        /// <summary>
        /// RequestUri
        /// </summary>
        /// <value>URI of the Internet resource associated with the request</value>
        public override Uri RequestUri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Timeout
        /// </summary>
        /// <value>length of time before the request times out</value>
        /// <remarks>This value is shared with the InnerRequest.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Value must be >= -1</exception>
        public override int Timeout
        {
            get
            {
                return GetRequest().Timeout;
            }
            set
            {
                // negative time that is not -1 (infinite) is an error case
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                    throw new ArgumentOutOfRangeException("value");

                GetRequest().Timeout = value;
            }
        }

        /// <summary>
        /// UseDefaultCredentials
        /// </summary>
        public override bool UseDefaultCredentials
        {
            get
            {
                return GetRequest().UseDefaultCredentials;
            }
            set
            {
                GetRequest().UseDefaultCredentials = value;
            }
        }

        #endregion

        #region New Properties
        /// <summary>
        /// GetInnerRequest
        /// </summary>
        /// <value>Inner WebRequest object.</value>
        /// <exception cref="NotSupportedException">Inner uri is not resolvable to a valid transport protocol (such as
        /// ftp or http) and the request cannot be satisfied from the PackageStore.</exception>
        /// <remarks>The inner WebRequest is provided for advanced scenarios only and 
        /// need not be accessed in most cases.</remarks>
        /// <returns>A WebRequest created using the inner-uri or null if the inner uri is not resolvable and we
        /// have a valid PackageStore entry that can be used to provide data.</returns>
        public WebRequest GetInnerRequest()
        {
            WebRequest request = GetRequest(false);
            if (request == null || request is PseudoWebRequest)
                return null;

            return request;
        }
        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Returns a WebRequest - should be called from properties
        /// </summary>
        /// <returns>Actual WebRequest or PseudoWebRequest</returns>
        /// <exception cref="NotSupportedException">protocol does not have a registered handler</exception>
        private WebRequest GetRequest()
        {
            return GetRequest(true);
        }

        /// <summary>
        /// Returns a WebRequest - can be called from properties and from GetInnerRequest()
        /// </summary>
        /// <param name="allowPseudoRequest">if this is false, caller will not accept a PseudoWebRequest</param>
        /// <returns>Actual WebRequest or PseudoWebRequest</returns>
        /// <exception cref="NotSupportedException">protocol does not have a registered handler</exception>
        private WebRequest GetRequest(bool allowPseudoRequest)
        {
            if (_webRequest == null)
            {
                // Don't even attempt to create if we know it will fail.  This does not eliminate all failure cases
                // but most and is very common so let's save an expensive exception.
                // We still create a webRequest if possible even if we have a potential cacheEntry 
                // because the caller may still specify BypassCache policy before calling GetResponse() that will force us to hit the server.
                if (!IsPreloadedPackage)
                {
                    // Need inner request so we can get/set properties or create a real WebResponse.
                    // Note: WebRequest.Create throws NotSupportedException for schemes that it does not recognize.
                    // We need to be open-ended in our support of schemes, so we need to always try to create.
                    // If WebRequest throws NotSupportedException then we catch and ignore if the WebRequest can be
                    // satisfied from the cache.  If the cache entry is missing then we simply re-throw because the request
                    // cannot possibly succeed.
                    try
                    {
                        _webRequest = WpfWebRequestHelper.CreateRequest(_innerUri);

                        // special optimization for ftp - Passive mode won't return lengths on ISA servers
                        FtpWebRequest ftpWebRequest = _webRequest as FtpWebRequest;
                        if (ftpWebRequest != null)
                        {
                            ftpWebRequest.UsePassive = false;  // default but allow override
                        }
}
                    catch (NotSupportedException)
                    {
                        // If the inner Uri does not match any cache entry then we throw.
                        if (!IsCachedPackage)
                            throw;
                    }
                }

                // Just return null if caller cannot accept a PseudoWebRequest
                if (_webRequest == null && allowPseudoRequest)
                {
                    // We get here if the caller can accept a PseudoWebRequest (based on argument to the function)
                    // and one of these two cases is true:
                    // 1. We have a package from the PreloadedPackages collection
                    // 2. If WebRequest.Create() failed and we have a cached package
                    // In either case, we create a pseudo request to house property values.
                    // In case 1, we know there will never be a cache bypass (we ignore cache policy for PreloadedPackages)
                    // In case 2, the caller is using a schema that we cannot use for transport (not on of ftp, http, file, etc)
                    // and we will silently accept and ignore their property modifications/queries.  
                    // Note that if they change the cache policy to BypassCache they will get an exception when they call
                    // GetResponse().  If they leave cache policy intact, and call GetResponse()
                    // they will get data from the cached package.
                    _webRequest = new PseudoWebRequest(_uri, _innerUri, _partName, _cacheEntry);
                }
            }

            return _webRequest;
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// True if we have a package from either that PreloadedPackages or PackageStore
        /// </summary>
        private bool IsCachedPackage
        {
            get
            {
                return (_cacheEntry != null);
            }
        }

        /// <summary>
        /// True only if we have a package from PreloadedPackages
        /// </summary>
        private bool IsPreloadedPackage
        {
            get
            {
                // _respectCachePolicy is only false for packages retrieved from PreloadedPackages
                return ((_cacheEntry != null) && (!_respectCachePolicy));
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private Uri                 _uri;                   // pack uri
        private Uri                 _innerUri;              // inner uri extracted from the pack uri
        private Uri                 _partName;              // name of PackagePart (if any) - null for full-container references
        private WebRequest          _webRequest;            // our "real" webrequest counterpart - may be a PseudoWebRequest
        private Package             _cacheEntry;            // non-null if we found this in a cache
        private bool                _respectCachePolicy;    // do we throw if cache policy conflicts?
        private bool                _cachedPackageIsThreadSafe; // pass to WebResponse so it can safely return streams
        private RequestCachePolicy  _cachePolicy;           // outer cache-policy

        // statics
        static private RequestCachePolicy _defaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);

        // These are "cached" inner Uri's taken from the available application: and SiteOfOrigin: Uri's.  
        // They are kept in statics to eliminate overhead of reparsing them on every request.
        // We are essentially extracting the "application://" out of "pack://application:,,"
        static private Uri _siteOfOriginUri = PackUriHelper.GetPackageUri(System.Windows.Navigation.BaseUriHelper.SiteOfOriginBaseUri);
        static private Uri _appBaseUri = PackUriHelper.GetPackageUri(System.Windows.Navigation.BaseUriHelper.PackAppBaseUri);
    }
}
