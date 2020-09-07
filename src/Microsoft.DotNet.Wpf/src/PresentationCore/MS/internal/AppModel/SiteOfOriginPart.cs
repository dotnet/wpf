// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
// SiteOfOriginPart is an implementation of the abstract PackagePart class. It contains an override for GetStreamCore.
//


using System;
using System.Net;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.IO;
using System.Resources;
using System.Globalization;
using MS.Internal.PresentationCore;

namespace MS.Internal.AppModel
{
    /// <summary>
    /// SiteOfOriginPart is an implementation of the abstract PackagePart class. It contains an override for GetStreamCore.
    /// </summary>
    internal class SiteOfOriginPart : System.IO.Packaging.PackagePart
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        internal SiteOfOriginPart(Package container, Uri uri) :
                base(container, uri)
        {
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        protected override Stream GetStreamCore(FileMode mode, FileAccess access)
        {
#if DEBUG
            if (SiteOfOriginContainer._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + 
                        ": SiteOfOriginPart: Getting stream.");
#endif
            return GetStreamAndSetContentType(false);
        }

        protected override string GetContentTypeCore()
        {
#if DEBUG
            if (SiteOfOriginContainer._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + 
                        ": SiteOfOriginPart: Getting content type.");
#endif
            
            GetStreamAndSetContentType(true);
            return _contentType.ToString();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private Stream GetStreamAndSetContentType(bool onlyNeedContentType)
        {
            lock (_globalLock)
            {
                if (onlyNeedContentType && _contentType != MS.Internal.ContentType.Empty)
                {
#if DEBUG
                    if (SiteOfOriginContainer._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + 
                                ": SiteOfOriginPart: Getting content type and using previously determined value");
#endif
                    return null;
                }
                
                // If GetContentTypeCore is called before GetStream() 
                // then we need to retrieve the stream to get the mime type.
                // That stream is then stored as _cacheStream and returned
                // the next time GetStreamCore() is called.
                if (_cacheStream != null)
                {
#if DEBUG
                    if (SiteOfOriginContainer._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId +
                                "SiteOfOriginPart: Using Cached stream");
#endif
                    Stream temp = _cacheStream;
                    _cacheStream = null;
                    return temp;
                }

                if (_absoluteLocation == null)
                {
#if DEBUG
                    if (SiteOfOriginContainer._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + 
                                ": SiteOfOriginPart: Determining absolute uri for this resource");
#endif
                    string original = Uri.ToString();
                    Invariant.Assert(original[0] == '/');
                    string uriMinusInitialSlash = original.Substring(1); // trim leading '/'
                    _absoluteLocation = new Uri(SiteOfOriginContainer.SiteOfOrigin, uriMinusInitialSlash);
                }

#if DEBUG
                if (SiteOfOriginContainer._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation(
                            DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                            System.Threading.Thread.CurrentThread.ManagedThreadId + 
                            ": SiteOfOriginPart: Making web request to " + _absoluteLocation);
#endif
                
                // For performance reasons it is better to open local files directly
                // rather than make a FileWebRequest.
                Stream responseStream;
                if (SecurityHelper.AreStringTypesEqual(_absoluteLocation.Scheme, Uri.UriSchemeFile))
                {
                    responseStream = HandleFileSource(onlyNeedContentType);
                }
                else
                {
                    responseStream = HandleWebSource(onlyNeedContentType);
                }
                
                return responseStream;
            }
        }

        private Stream HandleFileSource(bool onlyNeedContentType)
        {
#if DEBUG
            if (SiteOfOriginContainer._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + 
                        ": Opening local file " + _absoluteLocation);
#endif
            if (_contentType == MS.Internal.ContentType.Empty)
            {
                _contentType = MS.Internal.MimeTypeMapper.GetMimeTypeFromUri(Uri);                
            }

            if (!onlyNeedContentType)
            {
                return File.OpenRead(_absoluteLocation.LocalPath);
            }
            return null;
        }

        private Stream HandleWebSource(bool onlyNeedContentType)
        {
            WebResponse response = WpfWebRequestHelper.CreateRequestAndGetResponse(_absoluteLocation);
            Stream responseStream = response.GetResponseStream();

#if DEBUG
            if (SiteOfOriginContainer._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + 
                        ": Successfully retrieved stream from " + _absoluteLocation);
#endif

            if (_contentType == MS.Internal.ContentType.Empty)
            {
#if DEBUG
                if (SiteOfOriginContainer._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation(
                            DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                            System.Threading.Thread.CurrentThread.ManagedThreadId + 
                            ": SiteOfOriginPart: Setting _contentType");
#endif                    

                _contentType = WpfWebRequestHelper.GetContentType(response);
            }

            if (onlyNeedContentType)
            {
                _cacheStream = responseStream;
            }

            return responseStream;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        Uri _absoluteLocation = null;
        ContentType _contentType = MS.Internal.ContentType.Empty;
        Stream _cacheStream = null;
        private Object _globalLock = new Object();

        #endregion Private Members
    }
}

