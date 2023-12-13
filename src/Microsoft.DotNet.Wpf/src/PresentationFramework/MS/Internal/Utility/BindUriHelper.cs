// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//  Description:    BindUriHelper class. Allows bindToObject, bindToStream
//

using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text; 
using MS.Win32; 

#if PRESENTATIONFRAMEWORK

using System.Windows;
using System.Windows.Navigation;
using System.Windows.Media;
using MS.Internal.PresentationFramework;
using MS.Internal.AppModel;
using System.Windows.Controls;
using MS.Internal ; 
using System.Security; 
using System.IO.Packaging; 
using System.Reflection;
using MS.Internal.Utility;
using System.Net;

// In order to avoid generating warnings about unknown message numbers and 
// unknown pragmas when compiling your C# source code with the actual C# compiler, 
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691


namespace MS.Internal.Utility
{
    // A BindUriHelper class
    // See also WpfWebRequestHelper.
    internal  static partial  class BindUriHelper
    {
        private const string PLACEBOURI = "http://microsoft.com/";
        static private Uri placeboBase = new Uri(PLACEBOURI);
        private const string FRAGMENTMARKER = "#";
        
        static internal Uri GetResolvedUri(Uri originalUri)
        {
            return GetResolvedUri(null, originalUri);
        }                
       
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        ///     Relative Uri resolution logic
        ///     
        ///     if baseUriString != ""
        ///     {
        ///         if (baseUriString is absolute uri)
        ///         {
        ///             determine uriToNavigate as baseUriString + inputUri
        ///         }
        ///         else
        ///         {
        ///             determine uri to navigate wrt application's base uri + baseUriString + inputUri
        ///         }
        ///     }
        ///     else
        ///     {
        ///         Get the element's NavigationService 
        ///         if(NavigationService.CurrentSource is absolute uri)
        ///         {
        ///             determine uriToNavigate as NavigationService.CurrentSource + inputUri
        ///         }
        ///         else // this will be more common
        ///         {
        ///             determine uriToNavigate wrt application's base uri (pack://application,,,/) + NavigationService.CurrentSource + inputUri
        ///         }
        ///             
        ///         
        ///         If no ns in tree, resolve against the application's base
        ///     }
        /// </remarks>
        /// <param name="element"></param>
        /// <param name="baseUri"></param>
        /// <param name="inputUri"></param>
        /// <returns></returns>
        static internal Uri GetUriToNavigate(DependencyObject element, Uri baseUri, Uri inputUri)
        {
            Uri uriToNavigate = inputUri;

            if ((inputUri == null) || (inputUri.IsAbsoluteUri == true))
            {
                return uriToNavigate;
            }

            // BaseUri doesn't contain the last part of the path: filename,
            // so when the inputUri is fragment we cannot resolve with BaseUri, instead 
            // we should resolve with the element's NavigationService's CurrentSource.            
            if (StartWithFragment(inputUri))
            {
                baseUri = null;
            }

            if (baseUri != null)
            {
                if (baseUri.IsAbsoluteUri == false)
                {
                    uriToNavigate = GetResolvedUri(BindUriHelper.GetResolvedUri(null, baseUri), inputUri);
                }
                else
                {
                    uriToNavigate = GetResolvedUri(baseUri, inputUri);
                }
            }
            else // we're in here when baseUri is not set i.e. it's null 
            {
                Uri currentSource = null;                                               

                // if the it is an INavigator (Frame, NavWin), we should use its CurrentSource property.
                // Otherwise we need to get NavigationService of the container that this element is hosted in,
                // and use its CurrentSource.
                if (element != null)
                {
                    INavigator navigator = element as INavigator;
                    if (navigator != null)
                    {
                        currentSource = navigator.CurrentSource;
                    }
                    else
                    {
                        NavigationService ns = null;
                        ns = element.GetValue(NavigationService.NavigationServiceProperty) as NavigationService;
                        currentSource = (ns == null) ? null : ns.CurrentSource;
                    }
                }

                if (currentSource != null)
                {
                    if (currentSource.IsAbsoluteUri)
                    {
                        uriToNavigate = GetResolvedUri(currentSource, inputUri);
                    }
                    else
                    {
                        uriToNavigate = GetResolvedUri(GetResolvedUri(null, currentSource), inputUri);
                    }
                }
                else
                {
                    // For now we resolve to Application's base
                    uriToNavigate = BindUriHelper.GetResolvedUri(null, inputUri);
                }
            }
            return uriToNavigate;
        }

        static internal bool StartWithFragment(Uri uri)
        {
            return uri.OriginalString.StartsWith(FRAGMENTMARKER, StringComparison.Ordinal);
        }

        // Return Fragment string for a given uri without the leading #
        static internal string GetFragment(Uri uri)
        {
            Uri workuri = uri;
            string fragment = String.Empty;
            string frag;

            if (uri.IsAbsoluteUri == false)
            {
                // this is a relative uri, and Fragement() doesn't work with relative uris.  The base uri is completley irrelevant 
                // here and will never affect the returned fragment, but the method requires something to be there.  Therefore, 
                // we will use "http://microsoft.com" as a convenient substitute.
                workuri = new Uri(placeboBase, uri);
            }

            frag = workuri.Fragment;
            if (frag != null && frag.Length > 0)
            {
                // take off the pound
                fragment = frag.Substring(1);
            }

            return fragment;
        }
        
        // In NavigationService we do not want to show users pack://application,,,/ with the
        // Source property or any event arguments.
        static internal Uri GetUriRelativeToPackAppBase(Uri original)
        {
            if (original == null)
            {
                return null;
            }

            Uri resolved = GetResolvedUri(original);
            Uri packUri = BaseUriHelper.PackAppBaseUri;
            Uri relative = packUri.MakeRelativeUri(resolved);
            
            return relative;
        }

        static internal bool IsXamlMimeType(ContentType mimeType)
        {
            if (MimeTypeMapper.XamlMime.AreTypeAndSubTypeEqual(mimeType)
                || MimeTypeMapper.FixedDocumentSequenceMime.AreTypeAndSubTypeEqual(mimeType) 
                || MimeTypeMapper.FixedDocumentMime.AreTypeAndSubTypeEqual(mimeType)
                || MimeTypeMapper.FixedPageMime.AreTypeAndSubTypeEqual(mimeType))
            {
                return true;
            }

            return false;
        }
    }
}

#endif

