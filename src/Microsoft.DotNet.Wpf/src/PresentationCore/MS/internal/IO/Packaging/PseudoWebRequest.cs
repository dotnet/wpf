// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  WebRequest class to handle requests for pack-specific URI's that can be satisfied
//  from the PackageStore.
//
//

#if DEBUG
#define TRACE
#endif

using System;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Net.Cache;                 // for RequestCachePolicy
using System.Runtime.Serialization;
using System.Diagnostics;               // For Assert
using MS.Utility;                       // for EventTrace
using MS.Internal.IO.Packaging;         // for PackageCacheEntry
using MS.Internal.PresentationCore;     // for SRID exception strings
using MS.Internal;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// pack-specific WebRequest handler for cached packages
    /// </summary>
    internal class PseudoWebRequest : WebRequest
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Cached instance constructor
        /// </summary>
        /// <param name="uri">uri to resolve</param>
        /// <param name="packageUri">uri of the package</param>
        /// <param name="partUri">uri of the part - may be null</param>
        /// <param name="cacheEntry">cache entry to base this response on</param>
        internal PseudoWebRequest(Uri uri, Uri packageUri, Uri partUri, Package cacheEntry)
        {
            Debug.Assert(uri != null, "PackWebRequest uri cannot be null");
            Debug.Assert(packageUri != null, "packageUri cannot be null");
            Debug.Assert(partUri != null, "partUri cannot be null");

            // keep these
            _uri = uri;
            _innerUri = packageUri;
            _partName = partUri;
            _cacheEntry = cacheEntry;

            // set defaults
            SetDefaults();
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
        /// Do not call
        /// </summary>
        /// <returns>null</returns>
        public override WebResponse GetResponse()
        {
            Invariant.Assert(false, "PackWebRequest must handle this method.");
            return null;
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
                Invariant.Assert(false, "PackWebRequest must handle this method.");
                return null;
            }
            set
            {
                Invariant.Assert(false, "PackWebRequest must handle this method.");
            }
        }

        /// <summary>
        /// ConnectionGroupName
        /// </summary>
        /// <remarks>String.Empty is the default value</remarks>
        public override string ConnectionGroupName
        {
            get
            {
                return _connectionGroupName;
            }
            set
            {
                _connectionGroupName = value;
            }
        }


        /// <summary>
        /// ContentLength
        /// </summary>
        /// <value>length of RequestStream</value>
        public override long ContentLength
        {
            get
            {
                return _contentLength;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// ContentType
        /// </summary>
        public override string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                // null is explicitly allowed
                _contentType = value;
            }
        }


        /// <summary>
        /// Credentials
        /// </summary>
        /// <value>Credentials to use when authenticating against the resource</value>
        /// <remarks>null is the default value.</remarks>
        public override ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }


        /// <summary>
        /// Headers
        /// </summary>
        /// <value>collection of header name/value pairs associated with the request</value>
        /// <remarks>Default is an empty collection.  Null is not a valid value.</remarks>
        public override WebHeaderCollection Headers
        {
            get
            {
                // lazy init
                if (_headers == null)
                    _headers = new WebHeaderCollection();

                return _headers;
            }
            set
            {
                if (value == null)
                   throw new ArgumentNullException("value");

                _headers = value;
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
                return _method;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _method = value;
            }
        }


        /// <summary>
        /// PreAuthenticate
        /// </summary>
        /// <remarks>default is false</remarks>
        public override bool PreAuthenticate
        {
            get
            {
                return _preAuthenticate;
            }
            set
            {
                _preAuthenticate = value;
            }
        }


        /// <summary>
        /// Proxy
        /// </summary>
        public override IWebProxy Proxy
        {
            get
            {
                // lazy init
                if (_proxy == null)
                    _proxy = WebRequest.DefaultWebProxy;

                return _proxy;
            }
            set
            {
                _proxy = value;
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
                return _timeout;
            }
            set
            {
                // negative time that is not -1 (infinite) is an error case
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                    throw new ArgumentOutOfRangeException("value");

                _timeout = value;
            }
        }

        /// <summary>
        /// UseDefaultCredentials
        /// </summary>
        /// <remarks>This is an odd case where http acts "normally" but ftp throws NotSupportedException.</remarks>
        public override bool UseDefaultCredentials
        {
            get
            {
                // ftp throws on this
                if (IsScheme(Uri.UriSchemeFtp))
                    throw new NotSupportedException();

                return _useDefaultCredentials;
            }
            set
            {
                // ftp throws on this
                if (IsScheme(Uri.UriSchemeFtp))
                    throw new NotSupportedException();

                _useDefaultCredentials = value;
            }
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
        private bool IsScheme(String schemeName)
        {
            return (String.CompareOrdinal(_innerUri.Scheme, schemeName) == 0);
        }

        /// <summary>
        /// Non-value members are lazy-initialized if possible
        /// </summary>
        private void SetDefaults()
        {
            // set defaults
            _connectionGroupName = String.Empty;                // http default
            _contentType = null;                                // default
            _credentials = null;                                // actual default
            _headers = null;                                    // lazy init
            _preAuthenticate = false;                           // http default
            _proxy = null;                                      // lazy init

            if (IsScheme(Uri.UriSchemeHttp))
            {
                _timeout = 100000;                              // http default - 100s
                _method = WebRequestMethods.Http.Get;           // http default
            }
            else
                _timeout = System.Threading.Timeout.Infinite;   // ftp default and appropriate for cached file

            if (IsScheme(Uri.UriSchemeFtp))
                _method = WebRequestMethods.Ftp.DownloadFile;   // ftp default

            _useDefaultCredentials = false;                     // http default
            _contentLength = -1;                                // we don't support upload
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private Uri                 _uri;                   // pack uri
        private Uri                 _innerUri;              // inner uri extracted from the pack uri
        private Uri                 _partName;              // name of PackagePart (if any) - null for full-container references
        private Package             _cacheEntry;            // cached package

        // local copies of public members
        private string              _connectionGroupName;
        private string              _contentType;           // value of [CONTENT-TYPE] in WebHeaderCollection - provided by server
        private int                 _contentLength;         // length of data to upload - should be -1
        private string              _method;
        private ICredentials        _credentials;           // default is null
        private WebHeaderCollection _headers;               // empty is default
        private bool                _preAuthenticate;       // default to false
        private IWebProxy           _proxy;
        private int                 _timeout;               // timeout
        private bool                _useDefaultCredentials; // default is false for HTTP, exception for FTP
    }
}
