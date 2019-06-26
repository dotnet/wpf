// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
//
// Description: The DWriteFactory class represents a shared DWrite factory 
//              object.
//
//

using System;
using System.Security;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MS.Internal.PresentationCore;

namespace MS.Internal.FontCache
{
    internal static class DWriteFactory
    {
        static DWriteFactory()
        {
            _factory = Text.TextInterface.Factory.Create(
                Text.TextInterface.FactoryType.Shared, 
                new FontSourceCollectionFactory(),
                new FontSourceFactory());

            Text.TextInterface.LocalizedErrorMsgs.EnumeratorNotStarted = SR.Get(SRID.Enumerator_NotStarted);
            Text.TextInterface.LocalizedErrorMsgs.EnumeratorReachedEnd = SR.Get(SRID.Enumerator_ReachedEnd);
        }

        internal static Text.TextInterface.Factory Instance
        {
            get
            {
                return _factory;
            }
        }

        internal static Text.TextInterface.FontCollection SystemFontCollection
        {
            get
            {
                if (_systemFontCollection == null)
                {
                    lock(_systemFontCollectionLock)
                    {
                        if (_systemFontCollection == null)
                        {
                            _systemFontCollection = DWriteFactory.Instance.GetSystemFontCollection();
                        }
                    }
                }
                return _systemFontCollection;
            }
        }

        private static Text.TextInterface.FontCollection GetFontCollectionFromFileOrFolder(Uri fontCollectionUri, bool isFolder)
        {
            if (Text.TextInterface.Factory.IsLocalUri(fontCollectionUri))
            {
                string localPath;
                if (!isFolder)
                {
                    // get the parent directory of the file.
                    localPath = Directory.GetParent(fontCollectionUri.LocalPath).FullName + Path.DirectorySeparatorChar;
                }
                else
                {
                    localPath = fontCollectionUri.LocalPath;
                }

                // If the directory specifed is the windows fonts directory then no need to reenumerate system fonts.
                if (String.Compare(((localPath.Length > 0 && localPath[localPath.Length - 1] != Path.DirectorySeparatorChar) ? localPath + Path.DirectorySeparatorChar : localPath), Util.WindowsFontsUriObject.LocalPath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return SystemFontCollection;
                }
                // Perf Descision:
                // Create a new FontCollection that has all the fonts in the directory.
                // The user will most likely use other fonts in a custom fonts directory.
                // A typical scenario is that a user will store all the fonts his/her App needs 
                // in one directory. If we were not to follow this approach then we would create
                // a FontCollection for every font the user demands which may hurt performance.
                else
                {
                    return DWriteFactory.Instance.GetFontCollection(new Uri(localPath));
                }
            }

            // This isn't a local path so we create a FontCollection that only holds the desired font.
            // We follow a different approach here, as opposed to local files, where we only load the file
            // requested since file download and network latency cost becomes higher and loading all fonts 
            // in a network path might hurt perf instead.
            return DWriteFactory.Instance.GetFontCollection(fontCollectionUri);            
        }

        internal static Text.TextInterface.FontCollection GetFontCollectionFromFolder(Uri fontCollectionUri)
        {
            return GetFontCollectionFromFileOrFolder(fontCollectionUri, true);
        }

        internal static Text.TextInterface.FontCollection GetFontCollectionFromFile(Uri fontCollectionUri)
        {
            return GetFontCollectionFromFileOrFolder(fontCollectionUri, false);
        }

        private static Text.TextInterface.Factory _factory;
        private static Text.TextInterface.FontCollection _systemFontCollection = null;
        private static object _systemFontCollectionLock = new object();
    }
}
