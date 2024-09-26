// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
// Description: The FontSourceCollection class represents a collection of font files.
//
//

using System.IO;

using Microsoft.Win32;
using MS.Internal.Text.TextInterface;

namespace MS.Internal.FontCache
{
    internal class FontSourceCollectionFactory : IFontSourceCollectionFactory
    {
        public FontSourceCollectionFactory() { }

        public IFontSourceCollection Create(string uriString)
        {
            return new FontSourceCollection(new Uri(uriString));
        }
    }

    /// <summary>
    /// FontSourceCollection class represents a collection of font files.
    /// </summary>
    internal class FontSourceCollection : IFontSourceCollection
    {
        public FontSourceCollection(Uri folderUri)
        {
            Initialize(folderUri, false);
        }

        public FontSourceCollection(Uri folderUri, bool tryGetCompositeFontsOnly)
        {
            Initialize(folderUri, tryGetCompositeFontsOnly);
        }

        private void Initialize(Uri folderUri, bool tryGetCompositeFontsOnly)
        {
            _uri = folderUri;
            _tryGetCompositeFontsOnly = tryGetCompositeFontsOnly;

            // Check whether the given uri is a font file. In some cases we will construct a DWrite Font Collection by passing
            // a file path and not a directory path. In this case we need to construct a FontCollection that only holds this
            // file.
            bool isSingleSupportedFile = Util.IsSupportedFontExtension(Util.GetUriExtension(_uri), out bool isComposite);
            if (isSingleSupportedFile || !Util.IsEnumerableFontUriScheme(_uri))
            {
                _fontSources = new List<Text.TextInterface.IFontSource>(1);                
                _fontSources.Add(new FontSource(_uri, isComposite));
            }
            else
            {
                InitializeDirectoryProperties();
            }
        }

        private void InitializeDirectoryProperties()
        {
            _isFileSystemFolder = false;

            if (_uri.IsFile)
            {
                // Get the local path
                string localPath = _uri.LocalPath;

                // Decide if it's a file or folder based on syntax, not contents of file system
                _isFileSystemFolder = localPath[localPath.Length - 1] == Path.DirectorySeparatorChar;
            }
        }

        private void SetFontSources()
        {
            if (_fontSources != null)
                return;

            lock (this)
            {
                List<IFontSource> fontSources;
                if (_uri.IsFile)
                {              
                    if (_isFileSystemFolder)
                    {
                        if (_tryGetCompositeFontsOnly)
                        {
                            string[] files = Directory.GetFiles(_uri.LocalPath, "*" + Util.CompositeFontExtension);
                            fontSources = new List<IFontSource>(files.Length);

                            foreach (string file in files)
                                fontSources.Add(new FontSource(new Uri(file, UriKind.Absolute), _isWindowsFonts, true));
                        }
                        else
                        {
                            fontSources = new List<IFontSource>(8);

                            foreach (string file in Util.EnumerateFontsInDirectory(_uri.LocalPath))
                            {
                                bool isComposite = Util.IsCompositeFont(file);
                                fontSources.Add(new FontSource(new Uri(file, UriKind.Absolute), _isWindowsFonts, isComposite));
                            }
                        }
                    }
                    else
                    {
                        fontSources = new List<IFontSource>(1);
                        if (Util.IsSupportedFontExtension(Path.GetExtension(_uri.LocalPath.AsSpan()), out bool isComposite))
                            fontSources.Add(new FontSource(new Uri(_uri.LocalPath, UriKind.Absolute), _isWindowsFonts, isComposite));
                    }
                }
                else
                {
                    List<string> resourceEntries = FontResourceCache.LookupFolder(_uri);

                    if (resourceEntries is not null)
                    {
                        // Enumerate application resources, content files and container structure.
                        fontSources = new List<IFontSource>(resourceEntries.Count);

                        foreach (string resourceName in resourceEntries)
                        {
                            // If resourceName is an empty string, this means that the _uri is a full file name;
                            // otherwise resourceName is a file name within a folder.
                            if (string.IsNullOrEmpty(resourceName))
                            {
                                bool isComposite = Util.IsCompositeFont(Path.GetExtension(_uri.AbsoluteUri.AsSpan()));
                                fontSources.Add(new FontSource(_uri, isComposite));
                            }
                            else
                            {
                                bool isComposite = Util.IsCompositeFont(Path.GetExtension(resourceName.AsSpan()));
                                fontSources.Add(new FontSource(new Uri(_uri, resourceName), isComposite));
                            }
                        }
                    }
                    else
                    {
                        fontSources = new List<IFontSource>(0);
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
    

        private Uri                         _uri;

        // _isFileSystemFolder flag makes sense only when _uri.IsFile is set to true.
        private bool                                           _isFileSystemFolder;
        private volatile IList<Text.TextInterface.IFontSource> _fontSources;

        // Flag to indicate that only composite fonts in the provided URI location should be retrieved.        
        private bool                               _tryGetCompositeFontsOnly;
    }
}
