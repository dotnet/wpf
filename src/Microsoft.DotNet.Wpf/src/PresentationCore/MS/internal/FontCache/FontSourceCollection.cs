// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: The FontSourceCollection class represents a collection of font files.
//
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Win32;

using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.IO.Packaging;

using MS.Internal.PresentationCore;
using MS.Internal.Text.TextInterface;

namespace MS.Internal.FontCache
{
    internal class FontSourceCollectionFactory : IFontSourceCollectionFactory
    {
        public FontSourceCollectionFactory() { }

        /// <SecurityNote>
        /// Critical - Calls FontSourceCollection security critical ctor
        /// Safe     - The uriString is a user input and should not expose security critical info
        ///            since the user provides this info. Also we never call the ctor with 
        ///            isWindowsFonts = true. Thus it is safe to construct a FontSourceCollection 
        ///            from a user string because the user string is treated as untrusted.
        /// </SecurityNote>
        public IFontSourceCollection Create(string uriString)
        {
            return new FontSourceCollection(new Uri(uriString), false);
        }
    }

    /// <summary>
    /// FontSourceCollection class represents a collection of font files.
    /// </summary>
    internal class FontSourceCollection : IFontSourceCollection
    {
        /// <SecurityNote>
        /// Critical - Calls into security critical Initialize() method.
        /// </SecurityNote>
        public FontSourceCollection(Uri folderUri, bool isWindowsFonts)
        {
            Initialize(folderUri, isWindowsFonts, false);
        }

        /// <SecurityNote>
        /// Critical - Calls into security critical Initialize() method.
        /// </SecurityNote>
        public FontSourceCollection(Uri folderUri, bool isWindowsFonts, bool tryGetCompositeFontsOnly)
        {
            Initialize(folderUri, isWindowsFonts, tryGetCompositeFontsOnly);
        }

        /// <SecurityNote>
        /// Critical - The folderUri parameter is critical as it may contain privileged information 
        ///            about the file system (i.e., location of Windows Fonts); it is assigned to 
        ///            the _uri field which is declared critical.
        /// 
        ///            The isWindowsFonts parameter is critical for set as it is used to make a 
        ///            security decision (i.e., whether to assert read access); it is assigned to 
        ///            the _isWindowsFonts field which is declared critical.
        /// </SecurityNote>
        private void Initialize(Uri folderUri, bool isWindowsFonts, bool tryGetCompositeFontsOnly)
        {
            _uri = folderUri;
            _isWindowsFonts = isWindowsFonts;
            _tryGetCompositeFontsOnly = tryGetCompositeFontsOnly;

            bool isComposite = false;

            // Check whether the given uri is a font file. In some cases we will construct a DWrite Font Collection by passing
            // a file path and not a directory path. In this case we need to construct a FontCollection that only holds this
            // file.
            bool isSingleSupportedFile = Util.IsSupportedFontExtension(Util.GetUriExtension(_uri), out isComposite);
            if (isSingleSupportedFile || !Util.IsEnumerableFontUriScheme(_uri))
            {
                _fontSources = new List<Text.TextInterface.IFontSource>(1);                
                _fontSources.Add(new FontSource(_uri, false, isComposite));
            }
            else
            {
                InitializeDirectoryProperties();
            }
        }

        /// <SecurityNote>
        ///     Critical: accesses critical _uri and WindowsFontsUriObject
        ///     TreatAsSafe: only does comparisons to determine if _uri is the system dir
        /// </SecurityNote>
        private void InitializeDirectoryProperties()
        {
            _isFileSystemFolder = false;

            if (_uri.IsFile)
            {
                if (_isWindowsFonts)
                {
                    if (object.ReferenceEquals(_uri, Util.WindowsFontsUriObject))
                    {
                        // We know the local path and that it's a folder
                        _isFileSystemFolder = true;
                    }
                    else
                    {
                        // It's a file within the Windows Fonts folder
                        _isFileSystemFolder = false;
                    }
                }
                else
                {
                    // Get the local path
                    string localPath = _uri.LocalPath;

                    // Decide if it's a file or folder based on syntax, not contents of file system
                    _isFileSystemFolder = localPath[localPath.Length - 1] == Path.DirectorySeparatorChar;
                }
            }
        }

        /// <SecurityNote>
        /// Critical - as this allows you list files in windows font directory
        ///            and returns the file list.
        /// Safe     - This only does an elevation for WindowsFont directory which is safe,
        ///            and relevant data is already protected via FontSource methods.       
        ///            
        /// </SecurityNote>
        private void SetFontSources()
        {
            if (_fontSources != null)
                return;

            lock (this)
            {
                List<Text.TextInterface.IFontSource> fontSources;
                if (_uri.IsFile)
                {
                    ICollection<string> files;
                    bool isOnlyCompositeFontFiles = false;
                    if (_isFileSystemFolder)
                    {
                        if (_isWindowsFonts)
                        {
                            PermissionSet permissionSet = new PermissionSet(null);

                            // Read and path discovery permission for the %windir%\font path.
                            permissionSet.AddPermission(new FileIOPermission(
                                FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery,
                                Util.WindowsFontsUriObject.LocalPath));

                            // Registry read permissions for the Fonts system registry entry.
                            permissionSet.AddPermission(new RegistryPermission(
                                RegistryPermissionAccess.Read,
                                InstalledWindowsFontsRegistryKeyFullPath));

                            permissionSet.Assert(); // BlessedAssert

                            try
                            {
                                if (_tryGetCompositeFontsOnly)
                                {
                                    files = Directory.GetFiles(_uri.LocalPath, "*" + Util.CompositeFontExtension);
                                    isOnlyCompositeFontFiles = true;
                                }
                                else
                                {
                                    // fontPaths accumulates font file paths obtained from the registry and the file system
                                    // This collection is a set, i.e. only keys matter, not values.
                                    Dictionary<string, object> fontPaths = new Dictionary<string, object>(512, StringComparer.OrdinalIgnoreCase);

                                    using (RegistryKey fontsKey = Registry.LocalMachine.OpenSubKey(InstalledWindowsFontsRegistryKey))
                                    {
                                        // The registry key should be present on a valid Windows installation.
                                        Invariant.Assert(fontsKey != null);

                                        foreach (string fontValue in fontsKey.GetValueNames())
                                        {
                                            string fileName = fontsKey.GetValue(fontValue) as string;
                                            if (fileName != null)
                                            {
                                                // See if the path doesn't contain any directory information.
                                                // Shell uses the same method to determine whether to prepend the path with %windir%\fonts.
                                                if (Path.GetFileName(fileName) == fileName)
                                                    fileName = Path.Combine(Util.WindowsFontsLocalPath, fileName);

                                                fontPaths[fileName] = null;
                                            }
                                        }
                                    }

                                    foreach (string file in Directory.GetFiles(_uri.LocalPath))
                                    {
                                        fontPaths[file] = null;
                                    }
                                    files = fontPaths.Keys;
                                }
                            }
                            finally
                            {
                                if (_isWindowsFonts)
                                    CodeAccessPermission.RevertAssert();
                            }
}
                        else
                        {
                            if (_tryGetCompositeFontsOnly)
                            {
                                files = Directory.GetFiles(_uri.LocalPath, "*" + Util.CompositeFontExtension);
                                isOnlyCompositeFontFiles = true;                               
                            }
                            else
                            {
                                files = Directory.GetFiles(_uri.LocalPath);
                            }
                        }
                    }
                    else
                    {
                        files = new string[1] {_uri.LocalPath};
                    }

                    fontSources = new List<Text.TextInterface.IFontSource>(files.Count);
                    if (isOnlyCompositeFontFiles)
                    {
                        foreach (string file in files)
                        {
                            fontSources.Add(new FontSource(new Uri(file, UriKind.Absolute), _isWindowsFonts, true));
                        }
                    }
                    else
                    {
                        bool isComposite;
                        foreach (string file in files)
                        {                            
                            if (Util.IsSupportedFontExtension(Path.GetExtension(file), out isComposite))
                                fontSources.Add(new FontSource(new Uri(file, UriKind.Absolute), _isWindowsFonts, isComposite));
                        }
                    }
                }
                else
                {
                    List<string> resourceEntries = FontResourceCache.LookupFolder(_uri);

                    if (resourceEntries == null)
                        fontSources = new List<Text.TextInterface.IFontSource>(0);
                    else
                    {
                        bool isComposite = false;

                        // Enumerate application resources, content files and container structure.
                        fontSources = new List<Text.TextInterface.IFontSource>(resourceEntries.Count);

                        foreach (string resourceName in resourceEntries)
                        {
                            // If resourceName is an empty string, this means that the _uri is a full file name;
                            // otherwise resourceName is a file name within a folder.
                            if (String.IsNullOrEmpty(resourceName))
                            {
                                isComposite = Util.IsCompositeFont(Path.GetExtension(_uri.AbsoluteUri));
                                fontSources.Add(new FontSource(_uri, _isWindowsFonts, isComposite));
                            }
                            else
                            {
                                isComposite = Util.IsCompositeFont(Path.GetExtension(resourceName));
                                fontSources.Add(new FontSource(new Uri(_uri, resourceName), _isWindowsFonts, isComposite));
                            }
                        }
                    }
                }

                _fontSources = fontSources;
            }
        }

        
        #region IEnumerable<FontSource> Members

        IEnumerator<Text.TextInterface.IFontSource> System.Collections.Generic.IEnumerable<Text.TextInterface.IFontSource>.GetEnumerator()
        {
            SetFontSources();
            return (IEnumerator<Text.TextInterface.IFontSource>)_fontSources.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            SetFontSources();
            return _fontSources.GetEnumerator();
        }

        #endregion
    

        /// <SecurityNote>
        /// Critical - fontUri can contain information about local file system.
        /// </SecurityNote>
        private Uri                         _uri;

        /// <SecurityNote>
        /// Critical - this value is used to make security decisions (i.e., whether to do an Assert)
        ///            so it can only be set by critical code.
        /// </SecurityNote>
        private bool                        _isWindowsFonts;

        // _isFileSystemFolder flag makes sense only when _uri.IsFile is set to true.
        private bool                                           _isFileSystemFolder;
        private volatile IList<Text.TextInterface.IFontSource> _fontSources;

        // Flag to indicate that only composite fonts in the provided URI location should be retrieved.        
        private bool                               _tryGetCompositeFontsOnly;

        private const string InstalledWindowsFontsRegistryKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";
        private const string InstalledWindowsFontsRegistryKeyFullPath = @"HKEY_LOCAL_MACHINE\" + InstalledWindowsFontsRegistryKey;
}
}

