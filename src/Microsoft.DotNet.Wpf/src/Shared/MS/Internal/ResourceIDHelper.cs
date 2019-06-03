// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Globalization;

namespace MS.Internal
{
    internal static class ResourceIDHelper {
        //
        // Convert relative path to the right format which is used 
        // as ResourceID.
        // MarkupCompiler, ResXGen, Loader will call this method internally.
        //
        static internal string GetResourceIDFromRelativePath(string relPath)
        {
            // It is important that relPath not contain a fragment or query at this point
            //
            // Currently this will always be true because
            // 1) The resources listed in the .proj file are tested against the filesystem 
            //    to see if they exist.  If a query or fragment was present this test would fail. 
            // 2) In the new loader the container apis make sure that the fragment and query are 
            //    stripped from the Uri to form the part name which is used to load the resource
            Uri baseUri = new Uri("http://foo/");
            Uri srcUri = new Uri(baseUri, relPath.Replace("#", "%23"));

            return GetResourceIDFromUri(baseUri, srcUri);
        }

        //
        // This is the central place that returns right ResourceID for 
        // the passed SourceUri.
        //
        static private string GetResourceIDFromUri(Uri baseUri, Uri sourceUri)
        {
            string resourceID = String.Empty;

            // 
            // Extracts and returns the resource ID from a URI. 
            // If the source URI is under the baseUri, the resource ID is the URI 
            // after the baseUri and any fragment or query has been removed.
            // The resource ID is always converted to lower case.
            //
            // If the sourceUri is not relative to baseUri, Emtpy string is returned
            // as resource id.
            //
            if (baseUri.IsAbsoluteUri == false || sourceUri.IsAbsoluteUri == false)
            {
                 // 
                 // if any passed Uri is not absolute uri, return empty string here.
                 //
                 return resourceID;
            }


            if (baseUri.Scheme == sourceUri.Scheme && baseUri.Host == sourceUri.Host)
            {
                //
                // Get the escaped Path part, Path doesn't include Query and Fragment.
                //
                string basePath = baseUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
                string sourcePath = sourceUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);

                // 
                // Always lower case the Path string.
                //
                basePath = basePath.ToLower(CultureInfo.InvariantCulture);
                sourcePath = sourcePath.ToLower(CultureInfo.InvariantCulture);
 
                if (sourcePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                   resourceID = sourcePath.Substring(basePath.Length);
                } 
            }

            return resourceID;
}
}
}
