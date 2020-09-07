// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//  Description:
//      A helper class for access to HTTP cookies and attaching cookies to HttpWebRequests and storing
//      cookies from HttpWebResponses. 
//
//      In standalone WPF applications, the WinInet cookie store is used. PresentationHost intercepts calls
//      to the WinInet cookie functions and delegates them to the browser. See host\DLL\CookieShim.hxx.
//
//

using System;
using System.Net;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

using System.Windows;
using System.Windows.Interop;
using MS.Win32;
using MS.Internal.PresentationCore;

namespace MS.Internal.AppModel
{
static class CookieHandler
{
    internal static void HandleWebRequest(WebRequest request)
    {
        HttpWebRequest httpRequest = request as HttpWebRequest;
        if (httpRequest != null)
        {
            try
            {
                string cookies = GetCookie(httpRequest.RequestUri, false/*throwIfNoCookie*/);
                if(!string.IsNullOrEmpty(cookies))
                {
                    if (httpRequest.CookieContainer == null)
                    {
                        httpRequest.CookieContainer = new CookieContainer();
                    }
                    // CookieContainer.SetCookies() expects multiple cookie definitions to be separated by 
                    // comma, but GetCookie() returns them separated by ';', so we change that.
                    // Comma is generally not valid within a cookie (except in the 'expires' date setting, but 
                    // we don't get that from GetCookie()). 
                    // ClickOnce does the same in System.Deployment.Application.SystemNetDownloader.DownloadSingleFile().
                    httpRequest.CookieContainer.SetCookies(httpRequest.RequestUri, cookies.Replace(';', ','));
                }
            }
            catch (Exception ex) // Attaching cookies shouldn't fail a web request.
            {
                if (CriticalExceptions.IsCriticalException(ex))
                    throw;
            }
        }
    }

    /// <summary>
    /// Extracts cookies from a (Http)WebResponse and stores them.
    /// </summary>
    internal static void HandleWebResponse(WebResponse response)
    {
        HttpWebResponse httpResponse = response as HttpWebResponse;
        if (httpResponse != null)
        {
            // Not relying on httpResponse.Cookies, because the original cookie header is needed, with all
            // attributes. (A CookieCollection can be stuffed in a CookieContainer, but CookieContainer.
            // GetCookieHeader() returns only name=value pairs.)
            WebHeaderCollection headers = httpResponse.Headers;
            // Further complication: headers["Set-cookie"] returns all cookies comma-separated. Splitting them
            // is not trivial, because expiration dates have commas. 
            // Plan B fails too: headers.GetValues("Set-Cookie") returns the cookies broken: It does some 
            // "normalization" and munging and apparently confuses the commas in cookie expiration dates for
            // cookie separators... 
            // The working solution is to find the index of the header and get all individual raw values 
            // associated with it. (WebHeaderCollection's internal storage is a string->ArrayList(of string) map.)
            for (int i = headers.Count-1; i >= 0; i--)
            {
                if (string.Compare(headers.Keys[i], "Set-Cookie", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string p3pHeader = httpResponse.Headers["P3P"];
                    foreach (string cookie in headers.GetValues(i))
                    {
                        try
                        {
                            SetCookieUnsafe(httpResponse.ResponseUri, cookie, p3pHeader);
                        }
                        catch (Exception ex) // A malformed cookie shouldn't fail the whole web request.
                        {
                            if (CriticalExceptions.IsCriticalException(ex))
                                throw;
                        }
                    }

                    break;
                }
            }
        }
    }

    [FriendAccessAllowed] // called by PF.Application.GetCookie()
    [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", 
        Justification="It's okay now. Be careful on change.")]
    internal static string GetCookie(Uri uri, bool throwIfNoCookie)
    {
        UInt32 size = 0;
        string uriString = BindUriHelper.UriToString(uri);
        if (UnsafeNativeMethods.InternetGetCookieEx(uriString, null, null, ref size, 0, IntPtr.Zero))
        {
            Debug.Assert(size > 0);
            size++;
            System.Text.StringBuilder sb = new System.Text.StringBuilder((int)size);
            // PresentationHost intercepts InternetGetCookieEx(). It will set the INTERNET_COOKIE_THIRD_PARTY
            // flag if necessary.
            if (UnsafeNativeMethods.InternetGetCookieEx(uriString, null, sb, ref size, 0, IntPtr.Zero))
            {
                return sb.ToString();
            }
        }
        if (!throwIfNoCookie && Marshal.GetLastWin32Error() == NativeMethods.ERROR_NO_MORE_ITEMS)
            return null;
        throw new Win32Exception(/*uses last error code*/);
    }

    [FriendAccessAllowed] // called by PF.Application.SetCookie()
    internal static bool SetCookie(Uri uri, string cookieData)
    {
        return SetCookieUnsafe(uri, cookieData, null);
    }

    private static bool SetCookieUnsafe(Uri uri, string cookieData, string p3pHeader)
    {
        string uriString = BindUriHelper.UriToString(uri);
        // PresentationHost intercepts InternetSetCookieEx(). It will set the INTERNET_COOKIE_THIRD_PARTY
        // flag if necessary. (This doesn't look very elegant but is much simpler than having to make the 
        // 3rd party decision here as well or calling into the native code (from PresentationCore).)
        uint res = UnsafeNativeMethods.InternetSetCookieEx(
            uriString, null, cookieData, UnsafeNativeMethods.INTERNET_COOKIE_EVALUATE_P3P, p3pHeader);
        if(res == 0)
            throw new Win32Exception(/*uses last error code*/);
        return res != UnsafeNativeMethods.COOKIE_STATE_REJECT;
    }
};
}
