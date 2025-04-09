// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using MS.Internal.Text.TextInterface;

namespace MS.Internal.FontCache;

/// <summary>
/// FontSourceCollection class represents a collection of font files.
/// </summary>
internal class FontSourceCollection : IFontSourceCollection
{
    // _isFileSystemFolder flag makes sense only when _uri.IsFile is set to true.
    private bool _isFileSystemFolder;
    private volatile IList<IFontSource> _fontSources;

    /// <summary>
    /// The location of the source collection, usually we expect a folder URI location.
    /// </summary>
    private readonly Uri _uri;
    /// <summary>
    /// Flag to indicate that only composite fonts in the provided URI location should be retrieved. 
    /// </summary>
    private readonly bool _tryGetCompositeFontsOnly;

    public FontSourceCollection(Uri folderUri) : this(folderUri, false) { }

    public FontSourceCollection(Uri folderUri, bool tryGetCompositeFontsOnly)
    {
        _uri = folderUri;
        _tryGetCompositeFontsOnly = tryGetCompositeFontsOnly;

        // Check whether the given uri is a font file. In some cases we will construct a DWrite Font Collection by passing
        // a file path and not a directory path. In this case we need to construct a FontCollection that only holds this
        // file.
        bool isSingleSupportedFile = Util.IsSupportedFontExtension(Util.GetUriExtension(_uri), out bool isComposite);
        if (isSingleSupportedFile || !Util.IsEnumerableFontUriScheme(_uri))
        {
            _fontSources = new List<IFontSource>(1) { new FontSource(_uri, isComposite) };
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
            _isFileSystemFolder = localPath[^1] == Path.DirectorySeparatorChar;
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
                        string[] files = Directory.GetFiles(_uri.LocalPath, $"*{Util.CompositeFontExtension}");
                        fontSources = new List<IFontSource>(files.Length);

                        foreach (string file in files)
                            fontSources.Add(new FontSource(new Uri(file, UriKind.Absolute), true));
                    }
                    else
                    {
                        fontSources = new List<IFontSource>(8);

                        foreach (string file in Util.EnumerateFontsInDirectory(_uri.LocalPath))
                        {
                            bool isComposite = Util.IsCompositeFont(file);
                            fontSources.Add(new FontSource(new Uri(file, UriKind.Absolute), isComposite));
                        }
                    }
                }
                else
                {
                    fontSources = new List<IFontSource>(1);
                    if (Util.IsSupportedFontExtension(Path.GetExtension(_uri.LocalPath.AsSpan()), out bool isComposite))
                        fontSources.Add(new FontSource(new Uri(_uri.LocalPath, UriKind.Absolute), isComposite));
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

    IEnumerator<IFontSource> IEnumerable<IFontSource>.GetEnumerator()
    {
        SetFontSources();
        return _fontSources.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        SetFontSources();
        return _fontSources.GetEnumerator();
    }
}
