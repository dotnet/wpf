// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: FontResourceCache class maintains the list of font resources included with an application.
// 
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Windows.Navigation;

using MS.Internal.Resources;

namespace MS.Internal.FontCache
{
    internal static class FontResourceCache
    {
        private static void ConstructFontResourceCache(Assembly entryAssembly, Dictionary<string, List<string>> folderResourceMap)
        {
            // For entryAssembly build a set of mapping from paths to entries that describe each resource.
            HashSet<string> contentFiles = ContentFileHelper.GetContentFiles(entryAssembly);
            if (contentFiles != null)
            {
                foreach (string contentFile in contentFiles)
                {
                    AddResourceToFolderMap(folderResourceMap, contentFile);
                }
            }

            IList resourceList = new ResourceManagerWrapper(entryAssembly).ResourceList;
            if (resourceList != null)
            {
                foreach (string resource in resourceList)
                {
                    AddResourceToFolderMap(folderResourceMap, resource);
                }
            }
        }

        internal static List<string> LookupFolder(Uri uri)
        {
            // The input URI may specify a folder which is invalid for pack URIs. If so,
            // create a valid pack URI by adding a fake filename.
            bool isFolder = IsFolderUri(uri);
            if (isFolder)
            {
                uri = new Uri(uri, FakeFileName);
            }

            // The input Uri should now be a valid absolute pack application Uri.
            // The caller (FontSourceCollection) guarantees that.
            // Perform a sanity check to make sure the assumption stays the same.
            Debug.Assert(uri.IsAbsoluteUri && uri.Scheme == PackUriHelper.UriSchemePack && BaseUriHelper.IsPackApplicationUri(uri));

            Assembly uriAssembly;
            string escapedPath;

            BaseUriHelper.GetAssemblyAndPartNameFromPackAppUri(uri, out uriAssembly, out escapedPath);

            if (uriAssembly == null)
                return null;

            // If we added a fake filename to the uri, remove it from the escaped path.
            if (isFolder)
            {
                Debug.Assert(escapedPath.EndsWith(FakeFileName, StringComparison.OrdinalIgnoreCase));
                escapedPath = escapedPath.Substring(0, escapedPath.Length - FakeFileName.Length);
            }

            Dictionary<string, List<string>> folderResourceMap;

            lock (_assemblyCaches)
            {
                if (!_assemblyCaches.TryGetValue(uriAssembly, out folderResourceMap))
                {
                    folderResourceMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    ConstructFontResourceCache(uriAssembly, folderResourceMap);
                    _assemblyCaches.Add(uriAssembly, folderResourceMap);
                }
            }

            List<string> ret;
            folderResourceMap.TryGetValue(escapedPath, out ret);
            return ret;
        }

        /// <summary>
        /// Determines if a font family URI is a folder. Folder URIs are not valid pack URIs
        /// and therefore require special handling.
        /// </summary>
        private static bool IsFolderUri(Uri uri)
        {
            string path = uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
            return path.Length == 0 || path[path.Length - 1] == '/';
        }

        // Fake file name, which we add to a folder URI and then remove from the resulting
        // escaped path. This is to work around the fact that pack URIs cannot be folder URIs.
        // This string could be any valid file name that does not require escaping.
        private const string FakeFileName = "X";

        private static void AddResourceToFolderMap(Dictionary<string, List<string>> folderResourceMap, string resourceFullName)
        {
            // Split the resource path into a directory and file name part.
            string folderName;
            string fileName;
            int indexOfLastSlash = resourceFullName.LastIndexOf('/');
            if (indexOfLastSlash == -1)
            {
                // The resource is in the root directory, folderName is empty.
                folderName = String.Empty;
                fileName = resourceFullName;
            }
            else
            {
                folderName = resourceFullName.Substring(0, indexOfLastSlash + 1);
                fileName = resourceFullName.Substring(indexOfLastSlash + 1);
            }

            string extension = Path.GetExtension(fileName);

            bool isComposite;
            if (!Util.IsSupportedFontExtension(extension, out isComposite))
                return;

            // We add entries for a font file to two collections:
            // one is the folder containing the file, another is the file itself,
            // since we allow explicit file names in FontFamily syntax

            if (!folderResourceMap.ContainsKey(folderName))
                folderResourceMap[folderName] = new List<string>(1);
            folderResourceMap[folderName].Add(fileName);

            Debug.Assert(!folderResourceMap.ContainsKey(resourceFullName));
            folderResourceMap[resourceFullName] = new List<string>(1);
            folderResourceMap[resourceFullName].Add(String.Empty);
        }

        private static Dictionary<Assembly, Dictionary<string, List<string>>> _assemblyCaches
            = new Dictionary<Assembly, Dictionary<string, List<string>>>(1);
        // Set the initial capacity to 1 because a single entry assembly is the most common case.
    }
}

