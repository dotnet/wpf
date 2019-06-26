// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//  Description:
//      Helper class for handling all web requests/responses in the framework. Using it ensures consisent
//      handling and support for special features: cookies, NTLM authentication, caching, inferring MIME 
//      type from filename.
//

using System;
using System.Net;
using System.Net.Cache;
using System.Security;
using System.IO;

using System.Windows.Navigation;
using System.IO.Packaging;
using MS.Internal.AppModel;
using MS.Internal.Utility;
using MS.Internal.PresentationCore;

//From Presharp documentation:
//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. 
#pragma warning disable 1634, 1691

namespace MS.Internal
{
/// <summary>
/// Helper class for handling all web requests/responses in the framework. Using it ensures consisent handling 
/// and support for special features: cookies, NTLM authentication, caching, inferring MIME type from filename.
/// 
/// Only two methods are mandatory: 
///   - CreateRequest. (PackWebRequestFactory.CreateWebRequest is an allowed alternative. It delegates to 
///     this CreateRequest for non-pack URIs.)
///   - HandleWebResponse. 
/// The remaining methods just automate the entire request process, up to the point of getting the response
/// stream. Using the SecurityTreatAsSafe ones helps avoid making other code SecurityCritical.
/// 
/// Related types:
///   - BaseUriHelper
///   - BindUriHelper (built into Framework, subset into Core)
///   - PackWebRequestFactory
///   - MimeObjectFactory
/// </summary>
static class WpfWebRequestHelper
{
    [FriendAccessAllowed]
    internal static WebRequest CreateRequest(Uri uri)
    {
        // Ideally we would want to use RegisterPrefix and WebRequest.Create.
        // However, these two functions regress 700k working set in System.dll and System.xml.dll
        //  which is mostly for logging and config.
        // Call PackWebRequestFactory.CreateWebRequest to bypass the regression if possible
        //  by calling Create on PackWebRequest if uri is pack scheme
        if (string.Compare(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.Ordinal) == 0)
        {
            return PackWebRequestFactory.CreateWebRequest(uri);
            // The PackWebRequest may end up creating a "real" web request as its inner request.
            // It will then call this method again.
        }

            // Work around the issue with FileWebRequest not handling #.
            // FileWebRequest doesn't support the concept of query and fragment.
            if (uri.IsFile)
        {
            uri = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        WebRequest request = WebRequest.Create(uri);

        // It is not clear whether WebRequest.Create() can ever return null, but v1 code make this check in
        // a couple of places, so it is still done here, just in case.
        if(request == null)
        {
            // Unfortunately, there is no appropriate exception string in PresentationCore, and for v3.5 
            // we have a total resource freeze. So just report WebExceptionStatus.RequestCanceled:
            // "The request was canceled, the WebRequest.Abort method was called, or an unclassifiable error 
            // occurred. This is the default value for Status."
            Uri requestUri = BaseUriHelper.PackAppBaseUri.MakeRelativeUri(uri);
            throw new WebException(requestUri.ToString(), WebExceptionStatus.RequestCanceled);
            //throw new IOException(SR.Get(SRID.GetResponseFailed, requestUri.ToString()));
        }
        
        HttpWebRequest httpRequest = request as HttpWebRequest;
        if (httpRequest != null)
        {
            if (string.IsNullOrEmpty(httpRequest.UserAgent))
            {
                httpRequest.UserAgent = DefaultUserAgent;
            }

            CookieHandler.HandleWebRequest(httpRequest);

            if (String.IsNullOrEmpty(httpRequest.Referer))
            {
                httpRequest.Referer = BindUriHelper.GetReferer(uri);
            }

            CustomCredentialPolicy.EnsureCustomCredentialPolicy();

            // Enable NTLM authentication.
            // This is safe to do thanks to the CustomCredentialPolicy.
            httpRequest.UseDefaultCredentials = true;
        }

        return request;
    }

    /// <remarks> ******
    /// This method was factored out of the defunct BindUriHelper.ConfigHttpWebRequest().
    /// Ideally, all framework-created web requests should use the same caching settings, but in order not to 
    /// change behavior in SP1/v3.5, ConfigCachePolicy() is called separately by the code that previously
    /// relied on ConfigHttpWebRequest().
    /// </remarks>
    [FriendAccessAllowed]
    static internal void ConfigCachePolicy(WebRequest request, bool isRefresh)
    {
        HttpWebRequest httpRequest = request as HttpWebRequest;
        if (httpRequest != null)
        {
            // Setting CachePolicy to the default level if it is null.
            if (request.CachePolicy == null || request.CachePolicy.Level != RequestCacheLevel.Default)
            {
                if (isRefresh)
                {
                    if (_httpRequestCachePolicyRefresh == null)
                    {
                        _httpRequestCachePolicyRefresh = new HttpRequestCachePolicy(HttpRequestCacheLevel.Refresh);
                    }
                    request.CachePolicy = _httpRequestCachePolicyRefresh;
                }
                else
                {
                    if (_httpRequestCachePolicy == null)
                    {
                        _httpRequestCachePolicy = new HttpRequestCachePolicy();
                    }
                    request.CachePolicy = _httpRequestCachePolicy;
                }
            }
        }
    }
    private static HttpRequestCachePolicy _httpRequestCachePolicy;
    private static HttpRequestCachePolicy _httpRequestCachePolicyRefresh;

    internal static string DefaultUserAgent
    {
        get
        {
            if (_defaultUserAgent == null)
            {
                _defaultUserAgent = MS.Win32.UnsafeNativeMethods.ObtainUserAgentString();
            }
            return _defaultUserAgent;
        }
        set // set by ApplicationProxyInternal for browser hosting
        {
            _defaultUserAgent = value;
        }
    }
    private static string _defaultUserAgent;

    [FriendAccessAllowed]
    internal static void HandleWebResponse(WebResponse response)
    {
        CookieHandler.HandleWebResponse(response);
    }

    [FriendAccessAllowed]
    internal static Stream CreateRequestAndGetResponseStream(Uri uri)
    {
        WebRequest request = CreateRequest(uri);
        return GetResponseStream(request);
    }
    [FriendAccessAllowed]
    internal static Stream CreateRequestAndGetResponseStream(Uri uri, out ContentType contentType)
    {
        WebRequest request = CreateRequest(uri);
        return GetResponseStream(request, out contentType);
    }

    [FriendAccessAllowed]
    internal static WebResponse CreateRequestAndGetResponse(Uri uri)
    {
        WebRequest request = CreateRequest(uri);
        return GetResponse(request);
    }

    [FriendAccessAllowed]
    internal static WebResponse GetResponse(WebRequest request)
    {
        WebResponse response = request.GetResponse();
        
        // 'is' is used here instead of exact type checks to allow for the (remote) possibility that an internal
        // implementation type derived from HttpWebRequest/Response may be used by System.Net.
        if (response is HttpWebResponse && !(request is HttpWebRequest))
            throw new ArgumentException();
        
        // It is not clear whether WebRequest.GetRespone() can ever return null, but some of the v1 code had
        // this check, so it is added here just in case.
        if (response == null)
        {
            Uri requestUri = BaseUriHelper.PackAppBaseUri.MakeRelativeUri(request.RequestUri);
            throw new IOException(SR.Get(SRID.GetResponseFailed, requestUri.ToString()));
        }
        
        HandleWebResponse(response);
        return response;
    }
    [FriendAccessAllowed]
    internal static WebResponse EndGetResponse(WebRequest request, IAsyncResult ar)
    {
        WebResponse response = request.EndGetResponse(ar);
        if (response is HttpWebResponse && !(request is HttpWebRequest))
            throw new ArgumentException();
        // It is not clear whether WebRequest.GetRespone() can ever return null, but some of the v1 code had
        // this check, so it is added here just in case.
        if (response == null)
        {
            Uri requestUri = BaseUriHelper.PackAppBaseUri.MakeRelativeUri(request.RequestUri);
            throw new IOException(SR.Get(SRID.GetResponseFailed, requestUri.ToString()));
        }
        HandleWebResponse(response);
        return response;
    }

    [FriendAccessAllowed]
    internal static Stream GetResponseStream(WebRequest request)
    {
        WebResponse response = GetResponse(request);
        return response.GetResponseStream();
    }
    /// <summary>
    /// Gets the response from the given request and determines the content type using the special rules 
    /// implemented in GetContentType().
    /// </summary>
    [FriendAccessAllowed]
    internal static Stream GetResponseStream(WebRequest request, out ContentType contentType)
    {
        WebResponse response = GetResponse(request);
        contentType = GetContentType(response);
        return response.GetResponseStream();
    }


    // [Apr'07. Code moved here from BaseUriHelper.GetContentType().]
    /// <summary>
    /// Tries hard to obtain the content type of the given WebResponse. Special cases:
    /// - The ContentType property may throw if not implemented. 
    /// - Unconfigured web servers don't return the right type for WPF content. This method does lookup based on
    ///   file extension.
    /// </summary>
    [FriendAccessAllowed]
    internal static ContentType GetContentType(WebResponse response)
    {
        ContentType contentType = ContentType.Empty;

            // FileWebResponse returns a generic mime type for all files regardless of extension.
            // We have requested fix but it's postponed to orcas. This is a work around for that.
            // In the future, we should remove this code once we have a fix from the CLR team to fix
            // FileWebRequest to return the correct content type.
            if (!(response is FileWebResponse))
        {
            // For all other cases use the WebResponse's ContentType if it is available
            try
            {
                contentType = new ContentType(response.ContentType);

                // If our content type is octet-stream or text/plain, we might be dealing with an unconfigured server.
                // If the extension is .xaml or .xbap we ignore the server's content type and determine
                // the content type based on the URI's extension.
                if (MimeTypeMapper.OctetMime.AreTypeAndSubTypeEqual(contentType, true) ||
                    MimeTypeMapper.TextPlainMime.AreTypeAndSubTypeEqual(contentType, true))
                {
                    string extension = MimeTypeMapper.GetFileExtension(response.ResponseUri);
                    if ((String.Compare(extension, MimeTypeMapper.XamlExtension, StringComparison.OrdinalIgnoreCase) == 0) ||
                            (String.Compare(extension, MimeTypeMapper.XbapExtension, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        contentType = ContentType.Empty;  // Will cause GetMimeTypeFromUri to be called below
                    }
                }
            }
#pragma warning disable 6502
            catch (NotImplementedException)
            {
                // this is a valid result and indicates that the subclass chose not to implement this property
            }
            catch (NotSupportedException)
            {
                // this is a valid result and indicates that the subclass chose not to implement this property
            }
#pragma warning restore 6502
        }

        // DevDiv2 29007 - IE9 - pack://siteoforigin:,,,/ Uris fail for standalone applications
        // Something about the contentType object we get when IE9 is installed breaks file-system pack://siteoforigin:,,, Uris 
        // when we get here with a blank ContentType object.  
        // Workaround: Since we're only checking that content type hasn't been determined yet, checking the public properties versus
        // ContentType.Empty's copy of the same gets the same result, and also works for all versions of IE.
        if (contentType.TypeComponent == ContentType.Empty.TypeComponent &&
            contentType.OriginalString == ContentType.Empty.OriginalString &&
            contentType.SubTypeComponent == ContentType.Empty.SubTypeComponent)
        {
            contentType = MimeTypeMapper.GetMimeTypeFromUri(response.ResponseUri);
        }

        return contentType;
    }
};
}
