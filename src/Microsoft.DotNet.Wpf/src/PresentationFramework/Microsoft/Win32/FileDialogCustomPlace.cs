// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A union of KnownFolders and file paths that can be placed into FileDialog's Favorites pane.
    /// </summary>
    public sealed class FileDialogCustomPlace
    {
        /// <summary>
        /// Create a new FileDialogCustomPlace from a known folder guid.
        /// </summary>
        /// <remarks>
        /// Guids provided here may be gotten from the KnownFolders.h file included in the Windows SDK.
        /// New guids can also be registered by an application.
        /// </remarks>
        public FileDialogCustomPlace(Guid knownFolder)
        {
            KnownFolder = knownFolder;
        }

        /// <summary>
        /// Create a new FileDialogCustomPlace from a file path.
        /// </summary>
        public FileDialogCustomPlace(string path)
        {
            Path = path ?? "";
        }

        public Guid KnownFolder { get; private set; }
        public string Path { get; private set; }
    }
}

